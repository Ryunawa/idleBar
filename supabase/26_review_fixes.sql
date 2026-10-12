alter table public.frontiers add column if not exists saved_by uuid;

create table if not exists public.achievement_kinds (
  id text primary key
);

insert into public.achievement_kinds (id) values
  ('monstres-100'), ('monstres-1000'), ('monstres-10000'), ('monstres-100000'), ('boss-1'), ('boss-10'), ('boss-50'),
  ('niveau-25'), ('niveau-50'), ('niveau-100'), ('niveau-150'), ('niveau-200'), ('or-1e4'), ('or-1e6'), ('or-1e9'),
  ('or-1e12'), ('or-1e15'), ('ferme-25'), ('batiment-100'), ('village-complet'), ('heros-100'), ('compagnie'),
  ('poule-1'), ('poule-10'), ('poule-50'), ('repli-1'), ('repli-5'), ('repli-10'), ('clics-1000'), ('competences-50')
on conflict (id) do nothing;

alter table public.achievement_kinds enable row level security;
revoke all on public.achievement_kinds from anon, authenticated;

delete from public.frontier_achievements unlocked
where not exists (select 1 from public.achievement_kinds kind where kind.id = unlocked.achievement);

do $$
begin
  if not exists (select 1 from pg_constraint where conname = 'frontier_achievements_known') then
    alter table public.frontier_achievements
      add constraint frontier_achievements_known foreign key (achievement) references public.achievement_kinds (id);
  end if;
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

update private.settings set min_client_version = '0.4.2';
