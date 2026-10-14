alter table private.settings
  add column if not exists titan_boss_weight integer not null default 10 check (titan_boss_weight > 0);

update private.settings set titan_hp_per_player = 15000, titan_min_hp = 60000, titan_kills_per_minute = 60;

create table if not exists public.titan_tiers (
  points bigint primary key check (points > 0),
  percent integer not null check (percent > 0),
  hours integer not null check (hours > 0)
);

insert into public.titan_tiers (points, percent, hours) values (500, 25, 4), (3000, 50, 8), (10000, 100, 12)
on conflict (points) do update set percent = excluded.percent, hours = excluded.hours;

alter table public.titan_tiers enable row level security;
revoke all on public.titan_tiers from anon, authenticated;

update public.titans titan
set hp = greatest(titan.hp, settings.titan_min_hp, settings.titan_hp_per_player * (
  select count(*) from public.frontiers where last_seen > now() - interval '7 days'))
from private.settings settings
where titan.week = private.titan_week() and titan.fallen_at is null;

create or replace function private.titan_reward(p_monsters bigint) returns integer[]
language sql stable
set search_path = ''
as $$
  select array[percent, hours] from public.titan_tiers where points <= p_monsters order by points desc limit 1;
$$;

create or replace function private.frontier_feeds_titan() returns trigger
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_titan public.titans;
  v_settings private.settings;
  v_mine bigint;
  v_gift bigint;
  v_guild uuid;
  v_bosses bigint := greatest(0, new.bosses_killed - old.bosses_killed);
  v_minutes numeric := greatest(0, extract(epoch from new.saved_at - old.saved_at) / 60);
begin
  if new.monsters_killed <= old.monsters_killed then
    return null;
  end if;

  v_titan := private.current_titan();
  if v_titan.fallen_at is not null then
    return null;
  end if;

  select * into v_settings from private.settings;
  select coalesce(max(monsters), 0) into v_mine
  from public.titan_contributions where week = v_titan.week and player_id = new.player_id;
  v_gift := least(
    greatest(0, new.monsters_killed - old.monsters_killed - v_bosses) + v_bosses * v_settings.titan_boss_weight,
    floor((v_minutes + 1) * v_settings.titan_kills_per_minute)::bigint,
    greatest(0, v_titan.hp / 2 - v_mine),
    greatest(0, v_titan.hp - v_titan.damage));
  if v_gift <= 0 then
    return null;
  end if;

  insert into public.titan_contributions (week, player_id, monsters) values (v_titan.week, new.player_id, v_gift)
  on conflict (week, player_id) do update set monsters = public.titan_contributions.monsters + excluded.monsters;

  update public.titans
  set damage = damage + v_gift,
      fallen_at = case when damage + v_gift >= hp then coalesce(fallen_at, now()) else fallen_at end
  where week = v_titan.week;

  select guild_id into v_guild from public.guild_members where player_id = new.player_id;
  if v_guild is not null then
    insert into public.guild_titan (week, guild_id, monsters) values (v_titan.week, v_guild, v_gift)
    on conflict (week, guild_id) do update set monsters = public.guild_titan.monsters + excluded.monsters;
  end if;

  return null;
end;
$$;

create or replace function private.social_titan(p_player uuid, p_titan public.titans) returns jsonb
language sql stable
set search_path = ''
as $$
  select jsonb_build_object(
    'week', p_titan.week,
    'kind', extract(week from p_titan.week)::integer % 4,
    'hp', p_titan.hp,
    'damage', p_titan.damage,
    'fallen', p_titan.fallen_at is not null,
    'ends_in', extract(epoch from (p_titan.week + 7)::timestamp at time zone 'utc' - now()),
    'fighters', (select count(*) from public.titan_contributions where week = p_titan.week),
    'mine', coalesce((
      select monsters from public.titan_contributions where week = p_titan.week and player_id = p_player), 0),
    'tiers', coalesce((
      select jsonb_agg(jsonb_build_object('points', points, 'percent', percent, 'hours', hours) order by points)
      from public.titan_tiers), '[]'::jsonb),
    'last', (
      select jsonb_build_object(
        'kind', extract(week from last.week)::integer % 4,
        'fallen', last.fallen_at is not null,
        'mine', coalesce((
          select monsters from public.titan_contributions where week = last.week and player_id = p_player), 0))
      from public.titans last
      where last.week = p_titan.week - 7),
    'claimable', (
      select jsonb_build_object('percent', reward[1], 'hours', reward[2])
      from (select private.titan_reward((private.titan_claimable(p_player)).monsters) as reward) claim
      where claim.reward is not null),
    'ranking', coalesce((
      select jsonb_agg(jsonb_build_object('rank', ranked.rank, 'name', ranked.name, 'monsters', ranked.monsters, 'mine', ranked.mine)
        order by ranked.rank)
      from (
        select rank() over (order by score.monsters desc) as rank, guild.name, score.monsters,
          exists (select 1 from public.guild_members member where member.guild_id = guild.id and member.player_id = p_player) as mine
        from public.guild_titan score
        join public.guilds guild on guild.id = score.guild_id
        where score.week = p_titan.week) ranked
      where ranked.rank <= 10 or ranked.mine), '[]'::jsonb));
$$;
