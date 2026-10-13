alter table private.settings add column if not exists frontier_gold_rate numeric not null default 100000
  check (frontier_gold_rate > 0);

create or replace function private.frontier_souls_for_gold(p_gold double precision) returns numeric
language sql immutable
set search_path = ''
as $$
  select case when p_gold is null or p_gold <= 0 then 0 else floor(5 * power(p_gold::numeric / 1000000, 0.23)) end;
$$;

create or replace function private.frontier_hp(p_stage numeric) returns numeric
language sql immutable
set search_path = ''
as $$
  select 24 * power(1.35::numeric, least(greatest(p_stage, 1), 50) - 1)
    * power(1.19::numeric, greatest(least(p_stage, 100) - 50, 0))
    * power(1.13::numeric, greatest(p_stage - 100, 0));
$$;

create or replace function private.frontier_plausible(
  p_row public.frontiers, p_stage integer, p_max_stage integer, p_best_stage integer, p_souls bigint, p_retreats integer,
  p_lifetime_gold double precision)
returns boolean
language plpgsql stable
set search_path = ''
as $$
declare
  v_settings private.settings;
  v_best numeric := greatest(p_stage, p_max_stage, p_best_stage);
  v_minutes numeric := greatest(0, extract(epoch from now() - p_row.saved_at) / 60);
  v_reach numeric;
begin
  select * into v_settings from private.settings;
  v_reach := p_row.best_stage + v_settings.frontier_stage_margin + v_minutes * v_settings.frontier_stage_rate;
  return v_best <= v_reach
    and p_best_stage >= p_row.best_stage
    and p_retreats >= p_row.retreats
    and p_retreats - p_row.retreats <= 1 + v_minutes / v_settings.frontier_retreat_minutes
    and p_souls >= p_row.souls
    and p_souls <= greatest(p_row.souls, private.frontier_souls_for_gold(p_lifetime_gold))
    and p_lifetime_gold::numeric <= p_row.lifetime_gold::numeric
      + (v_minutes + 1) * v_settings.frontier_gold_rate * private.frontier_hp(least(v_reach, 20000))
        * (1 + 0.025 * greatest(p_souls, p_row.souls));
end;
$$;

create or replace function public.save_frontier(
  p_revision integer, p_session uuid,
  p_gold double precision, p_lifetime_gold double precision, p_souls bigint, p_retreats integer,
  p_stage integer, p_max_stage integer, p_best_stage integer, p_kills integer, p_boss_held boolean,
  p_fighters integer[], p_plots text[], p_levels integer[], p_cooldowns double precision[],
  p_intendant_unlocked boolean, p_intendant_on boolean, p_achievements text[],
  p_monsters_killed bigint, p_bosses_killed bigint, p_chickens bigint, p_clicks bigint, p_skills_used bigint)
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
    and p_stage >= 1 and p_stage <= p_max_stage and p_max_stage <= p_best_stage
    and p_kills >= 0 and p_kills < 100
    and (p_intendant_unlocked or not p_intendant_on)
    and p_monsters_killed >= v_row.monsters_killed
    and p_bosses_killed >= v_row.bosses_killed
    and p_chickens >= v_row.chickens
    and p_clicks >= v_row.clicks
    and p_skills_used >= v_row.skills_used
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
      intendant_unlocked = p_intendant_unlocked,
      intendant_on = p_intendant_on,
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
  set level = p_fighters[kind.position + 1]
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

drop function if exists private.frontier_plausible(public.frontiers, integer, integer, integer, bigint, integer);
drop function if exists private.frontier_souls_for(numeric);

update private.settings set min_client_version = '0.4.6';
