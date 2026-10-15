alter table private.settings
  add column if not exists skill_point_stages integer not null default 10 check (skill_point_stages > 0),
  add column if not exists skill_reset_stage integer not null default 10 check (skill_reset_stage >= 1);

create table if not exists public.skill_nodes (
  id text primary key,
  branch text not null check (branch in ('lightning', 'fury', 'fortune')),
  position smallint not null check (position between 1 and 5),
  cost smallint not null check (cost >= 1),
  max_level smallint not null check (max_level >= 1),
  active boolean not null,
  unique (branch, position)
);

insert into public.skill_nodes (id, branch, position, cost, max_level, active) values
  ('smite', 'lightning', 1, 1, 1, true),
  ('thunder', 'lightning', 2, 1, 3, false),
  ('storm', 'lightning', 3, 1, 1, true),
  ('lightning_rod', 'lightning', 4, 1, 3, false),
  ('judgment', 'lightning', 5, 2, 1, true),
  ('fury', 'fury', 1, 1, 1, true),
  ('lasting_rage', 'fury', 2, 1, 3, false),
  ('holy_shield', 'fury', 3, 1, 1, true),
  ('rally', 'fury', 4, 1, 1, true),
  ('tenacity', 'fury', 5, 1, 3, false),
  ('gold_rain', 'fortune', 1, 1, 1, true),
  ('downpour', 'fortune', 2, 1, 3, false),
  ('harvest', 'fortune', 3, 1, 1, true),
  ('lure', 'fortune', 4, 1, 1, true),
  ('prosperity', 'fortune', 5, 1, 3, false)
on conflict (id) do update
set branch = excluded.branch, position = excluded.position, cost = excluded.cost, max_level = excluded.max_level, active = excluded.active;

create table if not exists public.frontier_skills (
  player_id uuid not null references public.frontiers (player_id) on delete cascade,
  node text not null references public.skill_nodes (id),
  level smallint not null check (level >= 0),
  seconds double precision not null default 0 check (seconds >= 0 and seconds < 'Infinity'),
  placed boolean not null default false,
  primary key (player_id, node)
);

create table if not exists public.frontier_slots (
  player_id uuid not null references public.frontiers (player_id) on delete cascade,
  slot smallint not null check (slot between 0 and 2),
  node text,
  primary key (player_id, slot),
  constraint frontier_slots_one_each unique (player_id, node) deferrable initially deferred,
  constraint frontier_slots_learned foreign key (player_id, node)
    references public.frontier_skills (player_id, node) deferrable initially deferred
);

alter table public.skill_nodes enable row level security;
alter table public.frontier_skills enable row level security;
alter table public.frontier_slots enable row level security;
revoke all on public.skill_nodes, public.frontier_skills, public.frontier_slots from anon, authenticated;

create or replace function private.grant_skills(p_player uuid, p_cooldowns double precision[]) returns void
language sql
set search_path = ''
as $$
  insert into public.frontier_skills (player_id, node, level, seconds, placed)
  select frontier.player_id, legacy.node, 1, coalesce(p_cooldowns[legacy.slot + 1], 0), true
  from public.frontiers frontier
  cross join (values (0, 'smite'), (1, 'fury'), (2, 'gold_rain')) as legacy(slot, node)
  where frontier.player_id = p_player
    and (frontier.best_stage - 1) / (select skill_point_stages from private.settings) > legacy.slot
  on conflict (player_id, node) do nothing;

  insert into public.frontier_slots (player_id, slot, node)
  select frontier.player_id, legacy.slot,
    case when (frontier.best_stage - 1) / (select skill_point_stages from private.settings) > legacy.slot then legacy.node end
  from public.frontiers frontier
  cross join (values (0, 'smite'), (1, 'fury'), (2, 'gold_rain')) as legacy(slot, node)
  where frontier.player_id = p_player
  on conflict (player_id, slot) do nothing;
$$;

do $$
declare
  v_player uuid;
begin
  if to_regclass('public.frontier_cooldowns') is null then
    return;
  end if;

  for v_player in
    select frontier.player_id from public.frontiers frontier
    where not exists (select 1 from public.frontier_slots slot where slot.player_id = frontier.player_id)
  loop
    perform private.grant_skills(v_player, array(
      select coalesce(cooldown.seconds, 0)
      from (values (0, 'smite'), (1, 'fury'), (2, 'gold_rain')) as legacy(slot, skill)
      left join public.frontier_cooldowns cooldown on cooldown.player_id = v_player and cooldown.skill = legacy.skill
      order by legacy.slot));
  end loop;
end;
$$;

create or replace function private.frontier_shape_ok(
  p_fighters integer[], p_plots text[], p_levels integer[], p_cooldowns double precision[], p_achievements text[],
  p_max_stage integer)
returns boolean
language sql stable
set search_path = ''
as $$
  select cardinality(p_fighters) = (select count(*) from public.fighter_kinds)
    and cardinality(p_plots) = (select count(*) from public.plots)
    and cardinality(p_levels) = cardinality(p_plots)
    and cardinality(p_cooldowns) = 3
    and p_fighters[1] >= 1
    and not exists (select 1 from unnest(p_fighters) level where level is null or level < 0)
    and not exists (select 1 from unnest(p_cooldowns) seconds where not coalesce(private.frontier_finite(seconds), false))
    and not exists (
      select 1
      from unnest(p_plots, p_levels) with ordinality as entry(building, level, position)
      join public.plots plot on plot.plot = entry.position - 1
      where entry.level is null
        or entry.level < 0
        or (entry.building is null) <> (entry.level = 0)
        or (entry.building is not null and plot.unlock_stage > p_max_stage)
        or (entry.building is not null and not exists (select 1 from public.building_kinds kind where kind.id = entry.building)))
    and (select count(building) = count(distinct building) from unnest(p_plots) building)
    and coalesce(cardinality(p_achievements), 0) <= 200
    and not exists (select 1 from unnest(p_achievements) achievement where achievement is null or achievement !~ '^[a-z0-9-]{1,40}$');
$$;

drop function if exists private.skills_ok(uuid, text[], integer[], text[], integer, integer, integer);
drop function if exists private.skills_ok(uuid, text[], integer[], double precision[], text[], integer, integer, integer);
drop function if exists private.skills_ok(uuid, text[], integer[], double precision[], boolean[], text[], integer, integer, integer);

create or replace function private.skills_ok(
  p_player uuid, p_skills text[], p_skill_levels integer[], p_skill_cooldowns double precision[], p_placed boolean[], p_slots text[],
  p_best_stage integer, p_max_stage integer, p_retreats integer)
returns boolean
language sql stable
set search_path = ''
as $$
  with learned as (
    select entry.node, entry.level, entry.seconds, entry.placed, node.branch, node.position, node.cost, node.max_level, node.active
    from unnest(p_skills, p_skill_levels, p_skill_cooldowns, p_placed) as entry(node, level, seconds, placed)
    left join public.skill_nodes node on node.id = entry.node
  )
  select cardinality(p_skills) = cardinality(p_skill_levels)
    and cardinality(p_skill_cooldowns) = cardinality(p_skills)
    and cardinality(p_placed) = cardinality(p_skills)
    and cardinality(p_slots) = 3
    and not exists (
      select 1 from learned
      where learned.branch is null or learned.level is null or learned.level < 0 or learned.level > learned.max_level
        or learned.seconds is null or not coalesce(private.frontier_finite(learned.seconds), false) or learned.placed is null
        or (not learned.active and (learned.level < 1 or learned.seconds <> 0 or learned.placed))
        or (learned.level = 0 and learned.placed))
    and (select count(*) = count(distinct learned.node) from learned)
    and not exists (
      select 1 from learned
      where learned.level >= 1 and learned.position > 1
        and not exists (
          select 1 from learned before
          where before.branch = learned.branch and before.position = learned.position - 1 and before.level >= 1))
    and (select coalesce(sum(learned.cost * learned.level), 0) from learned)
      <= greatest(p_best_stage - 1, 0) / (select skill_point_stages from private.settings)
    and not exists (
      select 1 from unnest(p_slots) as slot(node)
      where slot.node is not null
        and not exists (select 1 from learned where learned.node = slot.node and learned.active and learned.level >= 1 and learned.placed))
    and (select count(slot.node) = count(distinct slot.node) from unnest(p_slots) as slot(node))
    and (p_max_stage <= (select skill_reset_stage from private.settings)
      or exists (
        select 1 from public.frontiers frontier
        where frontier.player_id = p_player
          and (p_retreats > frontier.retreats or frontier.max_stage <= (select skill_reset_stage from private.settings)))
      or not exists (
        select 1 from public.frontier_skills stored
        where stored.player_id = p_player
          and stored.level > coalesce((select learned.level from learned where learned.node = stored.node), 0)));
$$;

create or replace function public.start_frontier(p_session uuid) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player uuid := auth.uid();
begin
  if v_player is null then
    raise exception 'Connecte-toi pour prendre la route.';
  end if;

  insert into public.frontiers (player_id, session_id) values (v_player, p_session)
  on conflict (player_id) do update set session_id = excluded.session_id, last_seen = now();
  insert into public.frontier_fighters (player_id, fighter, level)
  select v_player, kind.id, case when kind.position = 0 then 1 else 0 end from public.fighter_kinds kind
  on conflict (player_id, fighter) do nothing;
  insert into public.frontier_plots (player_id, plot)
  select v_player, plot.plot from public.plots plot
  on conflict (player_id, plot) do nothing;
  insert into public.frontier_slots (player_id, slot)
  select v_player, slot from generate_series(0, 2) slot
  on conflict (player_id, slot) do nothing;
  return private.frontier_reply(v_player, 'ok');
end;
$$;

create or replace function private.frontier_reply(p_player_id uuid, p_status text) returns jsonb
language sql stable
set search_path = ''
as $$
  select jsonb_build_object(
    'status', p_status,
    'server_time', now(),
    'frontier', (
      select jsonb_build_object(
        'revision', frontier.revision,
        'saved_by', frontier.saved_by,
        'saved_at', frontier.saved_at,
        'away_seconds', greatest(0, extract(epoch from now() - frontier.saved_at)),
        'gold', frontier.gold,
        'lifetime_gold', frontier.lifetime_gold,
        'souls', frontier.souls,
        'retreats', frontier.retreats,
        'stage', frontier.stage,
        'max_stage', frontier.max_stage,
        'best_stage', frontier.best_stage,
        'kills', frontier.kills,
        'boss_held', frontier.boss_held,
        'monsters_killed', frontier.monsters_killed,
        'bosses_killed', frontier.bosses_killed,
        'chickens', frontier.chickens,
        'clicks', frontier.clicks,
        'skills_used', frontier.skills_used,
        'armor', frontier.armor,
        'hero_name', frontier.hero_name,
        'hero_role', frontier.hero_role,
        'role_retreats', frontier.role_retreats,
        'hero_skin', frontier.hero_skin,
        'hero_hair', frontier.hero_hair,
        'hero_hair_color', frontier.hero_hair_color,
        'hero_outfit', frontier.hero_outfit,
        'fighters', (
          select jsonb_agg(coalesce(fighter.level, 0) order by kind.position)
          from public.fighter_kinds kind
          left join public.frontier_fighters fighter on fighter.fighter = kind.id and fighter.player_id = frontier.player_id),
        'xp', (
          select jsonb_agg(coalesce(fighter.xp, 0) order by kind.position)
          from public.fighter_kinds kind
          left join public.frontier_fighters fighter on fighter.fighter = kind.id and fighter.player_id = frontier.player_id),
        'companions', (
          select jsonb_agg(fighter.role order by kind.position)
          from public.fighter_kinds kind
          left join public.frontier_fighters fighter on fighter.fighter = kind.id and fighter.player_id = frontier.player_id
          where kind.position > 0),
        'plots', (
          select jsonb_agg(owned.building order by plot.plot)
          from public.plots plot
          left join public.frontier_plots owned on owned.plot = plot.plot and owned.player_id = frontier.player_id),
        'levels', (
          select jsonb_agg(coalesce(owned.level, 0) order by plot.plot)
          from public.plots plot
          left join public.frontier_plots owned on owned.plot = plot.plot and owned.player_id = frontier.player_id),
        'cooldowns', (
          select jsonb_agg(coalesce(learned.seconds, 0) order by legacy.slot)
          from (values (0, 'smite'), (1, 'fury'), (2, 'gold_rain')) as legacy(slot, node)
          left join public.frontier_skills learned on learned.node = legacy.node and learned.player_id = frontier.player_id),
        'skill_cooldowns', coalesce((
          select jsonb_agg(learned.seconds order by learned.node)
          from public.frontier_skills learned
          where learned.player_id = frontier.player_id), '[]'::jsonb),
        'placed', coalesce((
          select jsonb_agg(learned.placed order by learned.node)
          from public.frontier_skills learned
          where learned.player_id = frontier.player_id), '[]'::jsonb),
        'slots', (
          select jsonb_agg(equipped.node order by place.slot)
          from generate_series(0, 2) as place(slot)
          left join public.frontier_slots equipped on equipped.slot = place.slot and equipped.player_id = frontier.player_id),
        'skills', coalesce((
          select jsonb_agg(learned.node order by learned.node)
          from public.frontier_skills learned
          where learned.player_id = frontier.player_id), '[]'::jsonb),
        'skill_levels', coalesce((
          select jsonb_agg(learned.level order by learned.node)
          from public.frontier_skills learned
          where learned.player_id = frontier.player_id), '[]'::jsonb),
        'achievements', coalesce((
          select jsonb_agg(unlocked.achievement order by unlocked.achievement)
          from public.frontier_achievements unlocked
          where unlocked.player_id = frontier.player_id), '[]'::jsonb))
      from public.frontiers frontier
      where frontier.player_id = p_player_id));
$$;

drop function if exists public.save_frontier(integer, uuid, double precision, double precision, bigint, integer, integer,
  integer, integer, integer, boolean, integer[], text[], integer[], double precision[], text[], bigint, bigint,
  bigint, bigint, bigint, integer, text[], double precision[]);
drop function if exists public.save_frontier(integer, uuid, double precision, double precision, bigint, integer, integer,
  integer, integer, integer, boolean, integer[], text[], integer[], double precision[], text[], bigint, bigint,
  bigint, bigint, bigint, integer, text[], double precision[], text[], integer[], text[]);
drop function if exists public.save_frontier(integer, uuid, double precision, double precision, bigint, integer, integer,
  integer, integer, integer, boolean, integer[], text[], integer[], double precision[], text[], bigint, bigint,
  bigint, bigint, bigint, integer, text[], double precision[], text[], integer[], text[], boolean[]);

create or replace function public.save_frontier(
  p_revision integer, p_session uuid,
  p_gold double precision, p_lifetime_gold double precision, p_souls bigint, p_retreats integer,
  p_stage integer, p_max_stage integer, p_best_stage integer, p_kills integer, p_boss_held boolean,
  p_fighters integer[], p_plots text[], p_levels integer[], p_cooldowns double precision[], p_achievements text[],
  p_monsters_killed bigint, p_bosses_killed bigint, p_chickens bigint, p_clicks bigint, p_skills_used bigint,
  p_armor integer, p_companions text[] default null, p_xp double precision[] default null,
  p_skills text[] default null, p_skill_levels integer[] default null, p_slots text[] default null, p_placed boolean[] default null,
  p_skill_cooldowns double precision[] default null)
returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player uuid := auth.uid();
  v_row public.frontiers;
begin
  select * into v_row from public.frontiers where player_id = v_player for update;
  if not found then
    return private.frontier_reply(v_player, 'missing');
  end if;

  if v_row.session_id is distinct from p_session then
    return private.frontier_reply(v_player, 'elsewhere');
  end if;

  if v_row.revision <> p_revision then
    return private.frontier_reply(v_player, 'conflict');
  end if;

  if not coalesce(
    private.frontier_finite(p_gold)
    and private.frontier_finite(p_lifetime_gold)
    and p_lifetime_gold >= v_row.lifetime_gold
    and p_gold <= v_row.gold + (p_lifetime_gold - v_row.lifetime_gold) + 1 + p_lifetime_gold * 1e-9
    and p_stage >= 1 and p_stage <= p_max_stage and p_max_stage <= p_best_stage
    and p_kills >= 0 and p_kills < 100
    and p_armor >= 0 and p_armor <= 1000000
    and p_monsters_killed >= v_row.monsters_killed
    and p_bosses_killed >= v_row.bosses_killed
    and p_chickens >= v_row.chickens
    and p_clicks >= v_row.clicks
    and p_skills_used >= v_row.skills_used
    and private.companions_ok(p_companions)
    and private.xp_ok(p_xp)
    and private.frontier_shape_ok(p_fighters, p_plots, p_levels, p_cooldowns, p_achievements, p_max_stage)
    and ((p_skills is null and p_skill_levels is null and p_slots is null and p_placed is null and p_skill_cooldowns is null)
      or private.skills_ok(v_player, p_skills, p_skill_levels, p_skill_cooldowns, p_placed, p_slots, p_best_stage, p_max_stage, p_retreats))
    and private.frontier_plausible(v_row, p_stage, p_max_stage, p_best_stage, p_souls, p_retreats, p_lifetime_gold), false) then
    return private.frontier_reply(v_player, 'refused');
  end if;

  update public.frontiers
  set revision = revision + 1,
      gold = p_gold,
      lifetime_gold = p_lifetime_gold,
      souls = p_souls,
      retreats = p_retreats,
      stage = p_stage,
      max_stage = p_max_stage,
      best_stage = p_best_stage,
      kills = p_kills,
      boss_held = p_boss_held,
      armor = p_armor,
      monsters_killed = p_monsters_killed,
      bosses_killed = p_bosses_killed,
      chickens = p_chickens,
      clicks = p_clicks,
      skills_used = p_skills_used,
      saved_by = p_session,
      saved_at = now(),
      last_seen = now()
  where player_id = v_player;

  update public.frontier_fighters fighter
  set level = p_fighters[kind.position + 1],
      role = case when kind.position > 0 and p_fighters[kind.position + 1] > 0 then p_companions[kind.position] end,
      xp = coalesce(p_xp[kind.position + 1], 0)
  from public.fighter_kinds kind
  where fighter.player_id = v_player and fighter.fighter = kind.id;

  update public.frontier_plots owned
  set building = p_plots[owned.plot + 1], level = p_levels[owned.plot + 1]
  where owned.player_id = v_player;

  if p_skills is null then
    update public.frontier_skills learned
    set seconds = legacy.seconds
    from unnest(array['smite', 'fury', 'gold_rain'], p_cooldowns) as legacy(node, seconds)
    where learned.player_id = v_player and learned.node = legacy.node;
  else
    delete from public.frontier_skills learned
    where learned.player_id = v_player and not (learned.node = any (p_skills));
    insert into public.frontier_skills (player_id, node, level, seconds, placed)
    select v_player, entry.node, entry.level, entry.seconds, entry.placed
    from unnest(p_skills, p_skill_levels, p_skill_cooldowns, p_placed) as entry(node, level, seconds, placed)
    on conflict (player_id, node) do update set level = excluded.level, seconds = excluded.seconds, placed = excluded.placed;
    insert into public.frontier_slots (player_id, slot, node)
    select v_player, entry.position - 1, entry.node
    from unnest(p_slots) with ordinality as entry(node, position)
    on conflict (player_id, slot) do update set node = excluded.node;
  end if;

  insert into public.frontier_achievements (player_id, achievement)
  select v_player, achievement from unnest(p_achievements) achievement
  where exists (select 1 from public.achievement_kinds kind where kind.id = achievement)
  on conflict (player_id, achievement) do nothing;

  return private.frontier_reply(v_player, 'ok');
end;
$$;

revoke all on function public.save_frontier(integer, uuid, double precision, double precision, bigint, integer, integer,
  integer, integer, integer, boolean, integer[], text[], integer[], double precision[], text[], bigint, bigint,
  bigint, bigint, bigint, integer, text[], double precision[], text[], integer[], text[], boolean[], double precision[]) from public, anon;
grant execute on function public.save_frontier(integer, uuid, double precision, double precision, bigint, integer, integer,
  integer, integer, integer, boolean, integer[], text[], integer[], double precision[], text[], bigint, bigint,
  bigint, bigint, bigint, integer, text[], double precision[], text[], integer[], text[], boolean[], double precision[]) to authenticated;

drop table if exists public.frontier_cooldowns;
drop table if exists public.skill_kinds;

update private.settings set min_client_version = '0.10.0';
