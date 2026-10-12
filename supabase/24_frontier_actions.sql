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
  insert into public.frontier_cooldowns (player_id, skill)
  select v_player, kind.id from public.skill_kinds kind
  on conflict (player_id, skill) do nothing;
  return private.frontier_reply(v_player, 'ok');
end;
$$;

create or replace function public.claim_frontier(p_session uuid) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
begin
  update public.frontiers set session_id = p_session, last_seen = now() where player_id = auth.uid();
  return private.frontier_reply(auth.uid(), 'ok');
end;
$$;

create or replace function public.rejoin_frontier(p_session uuid) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_idle integer;
begin
  select frontier_idle_seconds into v_idle from private.settings;
  update public.frontiers
  set session_id = p_session, last_seen = now()
  where player_id = auth.uid()
    and (session_id is null or session_id = p_session or last_seen < now() - make_interval(secs => v_idle));
  if not found and exists (select 1 from public.frontiers where player_id = auth.uid()) then
    return private.frontier_reply(auth.uid(), 'elsewhere');
  end if;

  return private.frontier_reply(auth.uid(), 'ok');
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
    and private.frontier_plausible(v_row, p_stage, p_max_stage, p_best_stage, p_souls, p_retreats), false) then
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
  on conflict (player_id, achievement) do nothing;

  return private.frontier_reply(v_player, 'ok');
end;
$$;

revoke all on function public.start_frontier(uuid) from public, anon;
revoke all on function public.claim_frontier(uuid) from public, anon;
revoke all on function public.rejoin_frontier(uuid) from public, anon;
revoke all on function public.save_frontier(integer, uuid, double precision, double precision, bigint, integer, integer,
  integer, integer, integer, boolean, integer[], text[], integer[], double precision[], boolean, boolean, text[], bigint,
  bigint, bigint, bigint, bigint) from public, anon;
grant execute on function public.start_frontier(uuid) to authenticated;
grant execute on function public.claim_frontier(uuid) to authenticated;
grant execute on function public.rejoin_frontier(uuid) to authenticated;
grant execute on function public.save_frontier(integer, uuid, double precision, double precision, bigint, integer, integer,
  integer, integer, integer, boolean, integer[], text[], integer[], double precision[], boolean, boolean, text[], bigint,
  bigint, bigint, bigint, bigint) to authenticated;
