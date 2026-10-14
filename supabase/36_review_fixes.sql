alter table private.settings
  add column if not exists reward_burst_minutes integer not null default 30 check (reward_burst_minutes > 0),
  add column if not exists guild_tries_per_hour integer not null default 10 check (guild_tries_per_hour > 0);

create table if not exists private.reward_credits (
  player_id uuid primary key references public.frontiers (player_id) on delete cascade,
  titan_at timestamptz,
  loot_at timestamptz
);

create table if not exists private.guild_attempts (
  player_id uuid not null references public.frontiers (player_id) on delete cascade,
  tried_at timestamptz not null default now()
);

create index if not exists guild_attempts_by_player on private.guild_attempts (player_id, tried_at);

alter table private.reward_credits enable row level security;
alter table private.guild_attempts enable row level security;
revoke all on private.reward_credits, private.guild_attempts from anon, authenticated;

create or replace function private.spend_credit(p_player uuid, p_titan boolean, p_wanted bigint, p_rate numeric) returns bigint
language plpgsql
set search_path = ''
as $$
declare
  v_floor timestamptz := now() - make_interval(mins => (select reward_burst_minutes from private.settings));
  v_from timestamptz;
  v_given bigint;
begin
  if p_wanted <= 0 then
    return 0;
  end if;

  insert into private.reward_credits (player_id) values (p_player) on conflict (player_id) do nothing;
  select greatest(coalesce(case when p_titan then titan_at else loot_at end, v_floor), v_floor) into v_from
  from private.reward_credits where player_id = p_player for update;
  v_given := least(p_wanted, floor(extract(epoch from now() - v_from) / 60 * p_rate)::bigint);
  if v_given <= 0 then
    return 0;
  end if;

  update private.reward_credits
  set titan_at = case when p_titan then v_from + make_interval(secs => v_given / p_rate * 60) else titan_at end,
      loot_at = case when p_titan then loot_at else v_from + make_interval(secs => v_given / p_rate * 60) end
  where player_id = p_player;
  return v_given;
end;
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
begin
  if new.monsters_killed <= old.monsters_killed then
    return null;
  end if;

  select * into v_titan from public.titans where week = (private.current_titan()).week for update;
  if v_titan.fallen_at is not null then
    return null;
  end if;

  select * into v_settings from private.settings;
  select coalesce(max(monsters), 0) into v_mine
  from public.titan_contributions where week = v_titan.week and player_id = new.player_id;
  v_gift := private.spend_credit(new.player_id, true, least(
    greatest(0, new.monsters_killed - old.monsters_killed - v_bosses) + v_bosses * v_settings.titan_boss_weight,
    greatest(0, v_titan.hp / 2 - v_mine),
    greatest(0, v_titan.hp - v_titan.damage)), v_settings.titan_kills_per_minute);
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

create or replace function private.frontier_drops_loot() returns trigger
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_settings private.settings;
  v_bosses integer;
begin
  if new.bosses_killed <= old.bosses_killed then
    return null;
  end if;

  select * into v_settings from private.settings;
  v_bosses := private.spend_credit(new.player_id, false, new.bosses_killed - old.bosses_killed, v_settings.loot_bosses_per_minute);
  for v_roll in 1 .. v_bosses loop
    if random() < v_settings.loot_chance and private.roll_item(new.player_id, new.max_stage, null, 'boss') is null then
      exit;
    end if;
  end loop;

  return null;
end;
$$;

create or replace function public.join_guild(p_code text) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player uuid := auth.uid();
  v_status text := private.guild_entry_status(v_player);
  v_guild public.guilds;
begin
  if v_status is not null then
    return private.social_reply(v_player, v_status);
  end if;

  if (select count(*) from private.guild_attempts where player_id = v_player and tried_at > now() - interval '1 hour')
    >= (select guild_tries_per_hour from private.settings) then
    return private.social_reply(v_player, 'too_many_tries');
  end if;

  select * into v_guild from public.guilds where code = upper(btrim(coalesce(p_code, ''))) for update;
  if not found then
    delete from private.guild_attempts where player_id = v_player and tried_at <= now() - interval '1 hour';
    insert into private.guild_attempts (player_id) values (v_player);
    return private.social_reply(v_player, 'unknown_code');
  end if;

  if (select count(*) from public.guild_members where guild_id = v_guild.id) >= (select guild_max_members from private.settings) then
    return private.social_reply(v_player, 'full');
  end if;

  insert into public.guild_members (player_id, guild_id) values (v_player, v_guild.id);
  return private.social_reply(v_player, 'ok');
end;
$$;

create or replace function public.kick_member(p_member bigint) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player uuid := auth.uid();
  v_me public.guild_members;
  v_kicked uuid;
begin
  select * into v_me from public.guild_members where player_id = v_player;
  if not found then
    return private.social_reply(v_player, 'not_member');
  end if;

  if not v_me.leader then
    return private.social_reply(v_player, 'not_leader');
  end if;

  delete from public.guild_members where id = p_member and guild_id = v_me.guild_id and player_id <> v_player
  returning player_id into v_kicked;
  if v_kicked is null then
    return private.social_reply(v_player, 'unknown_member');
  end if;

  insert into public.guild_departures (player_id, left_at) values (v_kicked, now())
  on conflict (player_id) do update set left_at = excluded.left_at;
  return private.social_reply(v_player, 'ok');
end;
$$;

create or replace function public.claim_titan() returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player uuid := auth.uid();
  v_claim public.titan_contributions;
  v_reward integer[];
  v_level integer;
begin
  select * into v_claim from private.titan_claimable(v_player);
  if v_claim.week is null then
    return private.social_reply(v_player, 'nothing');
  end if;

  if (select count(*) from public.items where owner_id = v_player) >= (select bag_size from private.settings) then
    return private.social_reply(v_player, 'bag_full');
  end if;

  v_reward := private.titan_reward(v_claim.monsters);
  update public.titan_contributions
  set claimed_at = now(),
      boost_percent = v_reward[1],
      boost_ends_at = now() + make_interval(hours => v_reward[2])
  where week = v_claim.week and player_id = v_player and claimed_at is null;
  if not found then
    return private.social_reply(v_player, 'nothing');
  end if;

  select best_stage into v_level from public.frontiers where player_id = v_player;
  perform private.roll_item(v_player, v_level,
    case when v_reward[1] >= 100 then 'legendary' when v_reward[1] >= 50 then 'epic' else 'rare' end, 'titan');
  return private.social_reply(v_player, 'ok');
end;
$$;

create or replace function private.social_boosts(p_player uuid) returns jsonb
language sql stable
set search_path = ''
as $$
  select coalesce(jsonb_agg(boost), '[]'::jsonb)
  from (
    select jsonb_build_object(
      'source', 'convoy',
      'gold', settings.convoy_gold_percent,
      'damage', 0,
      'ends_in', extract(epoch from private.convoy_end(p_player) - now())) as boost
    from private.settings settings
    where private.convoy_end(p_player) is not null
    union all
    select * from (
      select jsonb_build_object(
        'source', 'titan',
        'gold', contribution.boost_percent,
        'damage', contribution.boost_percent,
        'ends_in', extract(epoch from contribution.boost_ends_at - now()))
      from public.titan_contributions contribution
      where contribution.player_id = p_player and contribution.boost_ends_at > now()
      order by contribution.boost_percent desc, contribution.boost_ends_at desc
      limit 1) strongest) boosts;
$$;

update private.settings set titan_hp_per_player = 12000, titan_min_hp = 36000;

update public.titans titan
set hp = target.hp,
    fallen_at = case when titan.damage >= target.hp then now() end
from (
  select greatest(settings.titan_min_hp, settings.titan_hp_per_player * (
    select count(*) from public.frontiers where last_seen > now() - interval '7 days')) as hp
  from private.settings settings) target
where titan.week = private.titan_week() and titan.fallen_at is null;
