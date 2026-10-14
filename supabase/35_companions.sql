alter table public.frontier_fighters
  add column if not exists role text references public.role_kinds (id),
  add column if not exists xp double precision not null default 0 check (xp >= 0);

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
          select jsonb_agg(coalesce(cooldown.seconds, 0) order by kind.position)
          from public.skill_kinds kind
          left join public.frontier_cooldowns cooldown on cooldown.skill = kind.id and cooldown.player_id = frontier.player_id),
        'achievements', coalesce((
          select jsonb_agg(unlocked.achievement order by unlocked.achievement)
          from public.frontier_achievements unlocked
          where unlocked.player_id = frontier.player_id), '[]'::jsonb))
      from public.frontiers frontier
      where frontier.player_id = p_player_id));
$$;

create or replace function private.xp_ok(p_xp double precision[]) returns boolean
language sql immutable
set search_path = ''
as $$
  select p_xp is null or (
    coalesce(cardinality(p_xp), 0) <= 3
    and not exists (select 1 from unnest(p_xp) value where value is null or value < 0 or not private.frontier_finite(value)));
$$;

create or replace function private.companions_ok(p_companions text[]) returns boolean
language sql stable
set search_path = ''
as $$
  select p_companions is null or (
    coalesce(cardinality(p_companions), 0) <= 2
    and not exists (
      select 1 from unnest(p_companions) role
      where role is not null and not exists (select 1 from public.role_kinds kind where kind.id = role)));
$$;

drop function if exists public.save_frontier(integer, uuid, double precision, double precision, bigint, integer, integer,
  integer, integer, integer, boolean, integer[], text[], integer[], double precision[], boolean, boolean, text[], bigint, bigint,
  bigint, bigint, bigint, integer);

create or replace function public.save_frontier(
  p_revision integer, p_session uuid,
  p_gold double precision, p_lifetime_gold double precision, p_souls bigint, p_retreats integer,
  p_stage integer, p_max_stage integer, p_best_stage integer, p_kills integer, p_boss_held boolean,
  p_fighters integer[], p_plots text[], p_levels integer[], p_cooldowns double precision[], p_achievements text[],
  p_monsters_killed bigint, p_bosses_killed bigint, p_chickens bigint, p_clicks bigint, p_skills_used bigint,
  p_armor integer, p_companions text[] default null, p_xp double precision[] default null)
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

  update public.frontier_cooldowns cooldown
  set seconds = p_cooldowns[kind.position + 1]
  from public.skill_kinds kind
  where cooldown.player_id = v_player and cooldown.skill = kind.id;

  insert into public.frontier_achievements (player_id, achievement)
  select v_player, achievement from unnest(p_achievements) achievement
  where exists (select 1 from public.achievement_kinds kind where kind.id = achievement)
  on conflict (player_id, achievement) do nothing;

  return private.frontier_reply(v_player, 'ok');
end;
$$;

revoke all on function public.save_frontier(integer, uuid, double precision, double precision, bigint, integer, integer,
  integer, integer, integer, boolean, integer[], text[], integer[], double precision[], text[], bigint, bigint,
  bigint, bigint, bigint, integer, text[], double precision[]) from public, anon;
grant execute on function public.save_frontier(integer, uuid, double precision, double precision, bigint, integer, integer,
  integer, integer, integer, boolean, integer[], text[], integer[], double precision[], text[], bigint, bigint,
  bigint, bigint, bigint, integer, text[], double precision[]) to authenticated;

alter table public.frontiers
  drop column if exists intendant_on,
  drop column if exists intendant_unlocked;

update private.settings set min_client_version = '0.8.0';
