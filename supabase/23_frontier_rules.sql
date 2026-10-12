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
        'intendant_unlocked', frontier.intendant_unlocked,
        'intendant_on', frontier.intendant_on,
        'monsters_killed', frontier.monsters_killed,
        'bosses_killed', frontier.bosses_killed,
        'chickens', frontier.chickens,
        'clicks', frontier.clicks,
        'skills_used', frontier.skills_used,
        'fighters', (
          select jsonb_agg(coalesce(fighter.level, 0) order by kind.position)
          from public.fighter_kinds kind
          left join public.frontier_fighters fighter on fighter.fighter = kind.id and fighter.player_id = frontier.player_id),
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

create or replace function private.frontier_finite(p_value double precision) returns boolean
language sql immutable
set search_path = ''
as $$
  select p_value >= 0 and p_value < 'Infinity'::double precision;
$$;

create or replace function private.frontier_souls_for(p_stage numeric) returns numeric
language sql immutable
set search_path = ''
as $$
  select case when p_stage < 50 then 0 else floor(power(p_stage / 10, 2)) end;
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
    and cardinality(p_cooldowns) = (select count(*) from public.skill_kinds)
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

create or replace function private.frontier_plausible(
  p_row public.frontiers, p_stage integer, p_max_stage integer, p_best_stage integer, p_souls bigint, p_retreats integer)
returns boolean
language plpgsql stable
set search_path = ''
as $$
declare
  v_settings private.settings;
  v_best numeric := greatest(p_stage, p_max_stage, p_best_stage);
  v_minutes numeric := greatest(0, extract(epoch from now() - p_row.saved_at) / 60);
begin
  select * into v_settings from private.settings;
  return v_best <= p_row.best_stage + v_settings.frontier_stage_margin + v_minutes * v_settings.frontier_stage_rate
    and p_best_stage >= p_row.best_stage
    and p_retreats >= p_row.retreats
    and p_retreats - p_row.retreats <= 1 + v_minutes / v_settings.frontier_retreat_minutes
    and p_souls >= p_row.souls
    and p_souls - p_row.souls <= (p_retreats - p_row.retreats) * private.frontier_souls_for(v_best);
end;
$$;
