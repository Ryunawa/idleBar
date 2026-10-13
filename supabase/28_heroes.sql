create table if not exists public.role_kinds (
  id text primary key
);

insert into public.role_kinds (id) values ('warrior'), ('tank'), ('archer'), ('mage'), ('support')
on conflict (id) do nothing;

alter table public.role_kinds enable row level security;
revoke all on public.role_kinds from anon, authenticated;

alter table public.frontiers
  add column if not exists armor integer not null default 0 check (armor >= 0),
  add column if not exists hero_name text check (hero_name is null or char_length(hero_name) between 3 and 16),
  add column if not exists hero_role text not null default 'warrior' references public.role_kinds (id),
  add column if not exists role_retreats integer not null default 0 check (role_retreats >= 0),
  add column if not exists hero_skin smallint not null default 0 check (hero_skin between 0 and 3),
  add column if not exists hero_hair smallint not null default 0 check (hero_hair between 0 and 2),
  add column if not exists hero_hair_color smallint not null default 0 check (hero_hair_color between 0 and 5),
  add column if not exists hero_outfit smallint not null default 0 check (hero_outfit between 0 and 7);

create table if not exists private.banned_words (
  word text primary key,
  kind text not null check (kind in ('word', 'prefix', 'part'))
);

alter table private.banned_words enable row level security;
revoke all on private.banned_words from anon, authenticated;

insert into private.banned_words (word, kind) values
  ('con', 'word'), ('cons', 'word'), ('conne', 'word'), ('connes', 'word'),
  ('connard', 'prefix'), ('conard', 'prefix'), ('connas', 'prefix'), ('conasse', 'prefix'),
  ('pute', 'word'), ('putes', 'word'), ('putain', 'word'), ('salope', 'prefix'), ('salaud', 'prefix'),
  ('encul', 'prefix'), ('batard', 'prefix'), ('bite', 'word'), ('bites', 'word'), ('couille', 'prefix'),
  ('pd', 'word'), ('pede', 'word'), ('pedes', 'word'), ('tapette', 'prefix'), ('gouine', 'prefix'), ('negre', 'prefix'),
  ('negro', 'prefix'), ('bougnoul', 'prefix'), ('youpin', 'prefix'), ('bicot', 'prefix'), ('fdp', 'word'),
  ('ntm', 'word'), ('niqu', 'prefix'), ('nique', 'prefix'), ('enfoir', 'prefix'), ('tg', 'word'),
  ('tagueule', 'word'), ('poufias', 'prefix'), ('catin', 'prefix'), ('grognass', 'prefix'),
  ('branleur', 'prefix'), ('branlette', 'prefix'), ('suceur', 'prefix'), ('suceuse', 'prefix'), ('nazi', 'prefix'),
  ('hitler', 'prefix'), ('fuck', 'part'), ('shit', 'prefix'), ('bitch', 'prefix'), ('cunt', 'prefix'),
  ('asshole', 'prefix'), ('dick', 'word'), ('dicks', 'word'), ('dickhead', 'prefix'), ('nigger', 'prefix'),
  ('nigga', 'prefix'), ('faggot', 'prefix'), ('fag', 'word'), ('fags', 'word'), ('whore', 'prefix'),
  ('slut', 'prefix'), ('bastard', 'prefix'), ('wanker', 'prefix'), ('retarded', 'prefix')
on conflict (word) do update set kind = excluded.kind;

create or replace function private.name_key(p_word text) returns text
language sql immutable
set search_path = ''
as $$
  select regexp_replace(
    regexp_replace(
      translate(lower(p_word), 'àâäáãåçéèêëíìîïñóòôöõúùûüýÿœæ0134@$5!7', 'aaaaaaceeeeiiiinooooouuuuyyoaoieaassit'),
      '[^a-z]', '', 'g'),
    '(.)\1{2,}', '\1\1', 'g');
$$;

create or replace function private.hero_name_valid(p_name text) returns boolean
language sql immutable
set search_path = ''
as $$
  select coalesce(char_length(p_name) between 3 and 16 and p_name ~ '^[A-Za-z0-9À-ÖØ-öø-ÿŒœ ''_-]+$', false);
$$;

create or replace function private.hero_name_clean(p_name text) returns boolean
language sql stable
set search_path = ''
as $$
  select not exists (
    select 1
    from unnest(regexp_split_to_array(p_name, '[ ''_-]+') || p_name) token
    cross join lateral (select private.name_key(token) as key) keyed
    cross join lateral unnest(array[keyed.key, regexp_replace(keyed.key, '(.)\1+', '\1', 'g')]) candidate
    join private.banned_words banned
      on (banned.kind = 'word' and candidate = banned.word)
      or (banned.kind = 'prefix' and starts_with(candidate, banned.word))
      or (banned.kind = 'part' and position(banned.word in candidate) > 0)
    where candidate <> '');
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

create or replace function public.set_hero(
  p_session uuid, p_name text, p_role text, p_skin integer, p_hair integer, p_hair_color integer, p_outfit integer)
returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player uuid := auth.uid();
  v_row public.frontiers;
  v_name text := btrim(regexp_replace(coalesce(p_name, ''), '\s+', ' ', 'g'));
begin
  select * into v_row from public.frontiers where player_id = v_player for update;
  if not found then
    return private.frontier_reply(v_player, 'missing');
  end if;

  if v_row.session_id is distinct from p_session then
    return private.frontier_reply(v_player, 'elsewhere');
  end if;

  if not private.hero_name_valid(v_name) then
    return private.frontier_reply(v_player, 'invalid_name');
  end if;

  if not private.hero_name_clean(v_name) then
    return private.frontier_reply(v_player, 'banned_name');
  end if;

  if not coalesce(p_skin between 0 and 3 and p_hair between 0 and 2 and p_hair_color between 0 and 5
    and p_outfit between 0 and 7 and exists (select 1 from public.role_kinds kind where kind.id = p_role), false) then
    return private.frontier_reply(v_player, 'refused');
  end if;

  if v_row.hero_name is not null and p_role <> v_row.hero_role and v_row.retreats <= v_row.role_retreats then
    return private.frontier_reply(v_player, 'role_locked');
  end if;

  update public.frontiers
  set role_retreats = case when hero_name is null or p_role <> hero_role then retreats else role_retreats end,
      hero_name = v_name,
      hero_role = p_role,
      hero_skin = p_skin,
      hero_hair = p_hair,
      hero_hair_color = p_hair_color,
      hero_outfit = p_outfit,
      last_seen = now()
  where player_id = v_player;

  return private.frontier_reply(v_player, 'ok');
end;
$$;

drop function if exists public.save_frontier(integer, uuid, double precision, double precision, bigint, integer, integer,
  integer, integer, integer, boolean, integer[], text[], integer[], double precision[], boolean, boolean, text[], bigint, bigint,
  bigint, bigint, bigint);

create or replace function public.save_frontier(
  p_revision integer, p_session uuid,
  p_gold double precision, p_lifetime_gold double precision, p_souls bigint, p_retreats integer,
  p_stage integer, p_max_stage integer, p_best_stage integer, p_kills integer, p_boss_held boolean,
  p_fighters integer[], p_plots text[], p_levels integer[], p_cooldowns double precision[],
  p_intendant_unlocked boolean, p_intendant_on boolean, p_achievements text[],
  p_monsters_killed bigint, p_bosses_killed bigint, p_chickens bigint, p_clicks bigint, p_skills_used bigint,
  p_armor integer)
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
    and p_armor >= 0 and p_armor <= 1000000
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
      armor = p_armor,
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

revoke all on function public.set_hero(uuid, text, text, integer, integer, integer, integer) from public, anon;
revoke all on function public.save_frontier(integer, uuid, double precision, double precision, bigint, integer, integer,
  integer, integer, integer, boolean, integer[], text[], integer[], double precision[], boolean, boolean, text[], bigint, bigint,
  bigint, bigint, bigint, integer) from public, anon;
grant execute on function public.set_hero(uuid, text, text, integer, integer, integer, integer) to authenticated;
grant execute on function public.save_frontier(integer, uuid, double precision, double precision, bigint, integer, integer,
  integer, integer, integer, boolean, integer[], text[], integer[], double precision[], boolean, boolean, text[], bigint, bigint,
  bigint, bigint, bigint, integer) to authenticated;

update private.settings set min_client_version = '0.5.0';
