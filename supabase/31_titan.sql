alter table private.settings
  add column if not exists titan_hp_per_player integer not null default 5000 check (titan_hp_per_player > 0),
  add column if not exists titan_min_hp integer not null default 20000 check (titan_min_hp > 0),
  add column if not exists titan_kills_per_minute integer not null default 40 check (titan_kills_per_minute > 0);

create table if not exists public.titans (
  week date primary key,
  hp bigint not null check (hp > 0),
  damage bigint not null default 0 check (damage >= 0),
  fallen_at timestamptz,
  created_at timestamptz not null default now()
);

create table if not exists public.titan_contributions (
  week date not null references public.titans (week) on delete cascade,
  player_id uuid not null references public.frontiers (player_id) on delete cascade,
  monsters bigint not null default 0 check (monsters >= 0),
  claimed_at timestamptz,
  boost_percent integer check (boost_percent is null or boost_percent > 0),
  boost_ends_at timestamptz,
  primary key (week, player_id)
);

create table if not exists public.guild_titan (
  week date not null references public.titans (week) on delete cascade,
  guild_id uuid not null references public.guilds (id) on delete cascade,
  monsters bigint not null default 0 check (monsters >= 0),
  primary key (week, guild_id)
);

create index if not exists guild_titan_ranking on public.guild_titan (week, monsters desc);

alter table public.titans enable row level security;
alter table public.titan_contributions enable row level security;
alter table public.guild_titan enable row level security;
revoke all on public.titans, public.titan_contributions, public.guild_titan from anon, authenticated;

create or replace function private.titan_week() returns date
language sql stable
set search_path = ''
as $$
  select date_trunc('week', now() at time zone 'utc')::date;
$$;

create or replace function private.titan_reward(p_monsters bigint) returns integer[]
language sql immutable
set search_path = ''
as $$
  select case
    when p_monsters >= 5000 then array[100, 8]
    when p_monsters >= 1000 then array[50, 4]
    when p_monsters >= 200 then array[25, 2]
  end;
$$;

create or replace function private.current_titan() returns public.titans
language plpgsql
set search_path = ''
as $$
declare
  v_week date := private.titan_week();
  v_titan public.titans;
begin
  select * into v_titan from public.titans where week = v_week;
  if found then
    return v_titan;
  end if;

  insert into public.titans (week, hp)
  select v_week, greatest(settings.titan_min_hp, settings.titan_hp_per_player * (
    select count(*) from public.frontiers where last_seen > now() - interval '7 days'))
  from private.settings settings
  on conflict (week) do nothing;

  select * into v_titan from public.titans where week = v_week;
  return v_titan;
end;
$$;

create or replace function private.frontier_feeds_titan() returns trigger
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_titan public.titans;
  v_rate integer;
  v_mine bigint;
  v_gift bigint;
  v_guild uuid;
  v_minutes numeric := greatest(0, extract(epoch from new.saved_at - old.saved_at) / 60);
begin
  if new.monsters_killed <= old.monsters_killed then
    return null;
  end if;

  v_titan := private.current_titan();
  if v_titan.fallen_at is not null then
    return null;
  end if;

  select titan_kills_per_minute into v_rate from private.settings;
  select coalesce(max(monsters), 0) into v_mine
  from public.titan_contributions where week = v_titan.week and player_id = new.player_id;
  v_gift := least(
    new.monsters_killed - old.monsters_killed,
    floor((v_minutes + 1) * v_rate)::bigint,
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

create or replace trigger frontier_feeds_titan
after update of monsters_killed on public.frontiers
for each row execute function private.frontier_feeds_titan();

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
    select jsonb_build_object(
      'source', 'titan',
      'gold', contribution.boost_percent,
      'damage', contribution.boost_percent,
      'ends_in', extract(epoch from contribution.boost_ends_at - now()))
    from public.titan_contributions contribution
    where contribution.player_id = p_player and contribution.boost_ends_at > now()) boosts;
$$;

create or replace function private.titan_claimable(p_player uuid) returns public.titan_contributions
language sql stable
set search_path = ''
as $$
  select contribution.*
  from public.titan_contributions contribution
  join public.titans titan on titan.week = contribution.week
  where contribution.player_id = p_player
    and titan.fallen_at is not null
    and contribution.claimed_at is null
    and contribution.week >= private.titan_week() - 7
    and private.titan_reward(contribution.monsters) is not null
  order by contribution.week desc
  limit 1;
$$;

create or replace function private.social_titan(p_player uuid, p_titan public.titans) returns jsonb
language sql stable
set search_path = ''
as $$
  select jsonb_build_object(
    'kind', extract(week from p_titan.week)::integer % 4,
    'hp', p_titan.hp,
    'damage', p_titan.damage,
    'fallen', p_titan.fallen_at is not null,
    'ends_in', extract(epoch from (p_titan.week + 7)::timestamp at time zone 'utc' - now()),
    'mine', coalesce((
      select monsters from public.titan_contributions where week = p_titan.week and player_id = p_player), 0),
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

create or replace function private.social_reply(p_player uuid, p_status text) returns jsonb
language plpgsql
set search_path = ''
as $$
begin
  return jsonb_build_object(
    'status', p_status,
    'server_time', now(),
    'guild', private.social_guild(p_player),
    'rejoin_in', coalesce((
      select greatest(0, extract(epoch from departure.left_at + make_interval(mins => settings.guild_rejoin_minutes) - now()))
      from public.guild_departures departure, private.settings settings
      where departure.player_id = p_player), 0),
    'help', private.social_help(p_player),
    'titan', private.social_titan(p_player, private.current_titan()));
end;
$$;

create or replace function public.get_social() returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
begin
  return private.social_reply(auth.uid(),
    case when exists (select 1 from public.frontiers where player_id = auth.uid()) then 'ok' else 'missing' end);
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
begin
  select * into v_claim from private.titan_claimable(v_player);
  if v_claim.week is null then
    return private.social_reply(v_player, 'nothing');
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

  return private.social_reply(v_player, 'ok');
end;
$$;

revoke all on function public.claim_titan() from public, anon;
grant execute on function public.claim_titan() to authenticated;
