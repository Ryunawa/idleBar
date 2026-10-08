alter table private.settings add column if not exists helper_hourly integer[] not null default '{0,40,120,300}';

create or replace function private.tier_of(p_renown bigint) returns integer
language sql stable
set search_path = ''
as $$
  select coalesce(max(tier), 0) from public.renown_tiers where renown <= p_renown;
$$;

create or replace function private.owned_values(p_player_id uuid, p_kind text) returns text[]
language sql stable
set search_path = ''
as $$
  select coalesce(array_agg(upgrades.value), '{}')
  from public.tavern_upgrades owned
  join public.upgrades on upgrades.id = owned.upgrade_id
  where owned.player_id = p_player_id and upgrades.kind = p_kind;
$$;

create or replace function private.stools(p_player_id uuid) returns integer
language sql stable
set search_path = ''
as $$
  select greatest(settings.base_stools, coalesce((select max(value::integer) from unnest(private.owned_values(p_player_id, 'stool')) value), 0))
  from private.settings;
$$;

create or replace function private.helper_level(p_player_id uuid) returns integer
language sql stable
set search_path = ''
as $$
  select coalesce(max(value::integer), 0) from unnest(private.owned_values(p_player_id, 'helper')) value;
$$;

create or replace function private.helper_hourly(p_player_id uuid) returns integer
language sql stable
set search_path = ''
as $$
  select floor(settings.helper_hourly[private.helper_level(p_player_id) + 1] * private.stools(p_player_id)::numeric / settings.base_stools)::integer
  from private.settings;
$$;

create or replace function private.menu(p_player_id uuid) returns text[]
language sql stable
set search_path = ''
as $$
  select array_agg(drinks.id order by drinks.sort)
  from public.drinks
  where drinks.id not in (select value from public.upgrades where kind = 'station')
     or drinks.id = any (private.owned_values(p_player_id, 'station'));
$$;

create or replace function private.lock_tavern() returns public.taverns
language plpgsql
set search_path = ''
as $$
declare
  v_tavern public.taverns;
begin
  if auth.uid() is null then
    raise exception 'Connecte-toi pour jouer.';
  end if;

  select * into v_tavern from public.taverns where player_id = auth.uid() for update;
  if not found then
    raise exception 'Ouvre d''abord ta taverne.';
  end if;

  return v_tavern;
end;
$$;

create or replace function private.settle_absence(p_player_id uuid) returns void
language plpgsql
set search_path = ''
as $$
declare
  v_settings private.settings;
  v_gap numeric;
  v_hourly integer;
begin
  select * into v_settings from private.settings;
  select extract(epoch from now() - reported_at) into v_gap from public.taverns where player_id = p_player_id;
  if v_gap < v_settings.absence_seconds then
    return;
  end if;

  v_hourly := private.helper_hourly(p_player_id);
  update public.taverns
  set tip_jar = greatest(tip_jar, least(floor(v_hourly * v_settings.tip_jar_cap_hours)::integer, tip_jar + floor(v_gap / 3600 * v_hourly)::integer)),
      reported_at = now()
  where player_id = p_player_id;
end;
$$;

create or replace function private.ensure_goals(p_player_id uuid) returns void
language sql
set search_path = ''
as $$
  insert into public.tavern_goals (player_id, day, goal_id)
  select p_player_id, current_date, kinds.id
  from public.goal_kinds kinds
  where not exists (select 1 from public.tavern_goals where player_id = p_player_id and day = current_date)
    and (kinds.drink is null or kinds.drink = any (private.menu(p_player_id)))
  order by md5(p_player_id::text || current_date::text || kinds.id)
  limit (select goals_per_day from private.settings)
  on conflict do nothing;
$$;

create or replace function private.advance_goals(p_player_id uuid, p_served integer, p_perfect integer, p_drinks jsonb) returns integer
language plpgsql
set search_path = ''
as $$
declare
  v_reward integer;
begin
  update public.tavern_goals goals
  set progress = least(kinds.target, goals.progress + case kinds.measure
    when 'served' then p_served
    when 'perfect' then p_perfect
    else coalesce((p_drinks ->> kinds.drink)::integer, 0)
  end)
  from public.goal_kinds kinds
  where goals.player_id = p_player_id and goals.day = current_date and goals.goal_id = kinds.id and goals.done_at is null;

  with finished as (
    update public.tavern_goals goals
    set done_at = now()
    from public.goal_kinds kinds
    where goals.player_id = p_player_id and goals.day = current_date and goals.goal_id = kinds.id
      and goals.done_at is null and goals.progress >= kinds.target
    returning kinds.reward
  )
  select coalesce(sum(reward), 0) into v_reward from finished;

  update public.taverns set coins = coins + v_reward where player_id = p_player_id;
  return v_reward;
end;
$$;

create or replace function private.new_friend_code() returns text
language plpgsql
set search_path = ''
as $$
declare
  v_alphabet constant text := 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
  v_code text;
begin
  loop
    select string_agg(substr(v_alphabet, 1 + floor(random() * length(v_alphabet))::integer, 1), '')
    into v_code
    from generate_series(1, 6);
    exit when not exists (select 1 from public.taverns where friend_code = v_code);
  end loop;

  return v_code;
end;
$$;

create or replace function private.state(p_player_id uuid) returns jsonb
language sql stable
set search_path = ''
as $$
  select jsonb_build_object(
    'server_time', now(),
    'tavern', (
      select jsonb_build_object(
        'name', taverns.name,
        'friend_code', taverns.friend_code,
        'coins', taverns.coins,
        'renown', taverns.renown,
        'tier', private.tier_of(taverns.renown),
        'served', taverns.served,
        'perfect', taverns.perfect,
        'stools', private.stools(taverns.player_id),
        'menu', to_jsonb(private.menu(taverns.player_id)),
        'helper', private.helper_level(taverns.player_id),
        'upgrades', coalesce((select jsonb_agg(upgrade_id order by upgrade_id) from public.tavern_upgrades where player_id = taverns.player_id), '[]'),
        'tip_jar', jsonb_build_object(
          'amount', taverns.tip_jar,
          'hourly', private.helper_hourly(taverns.player_id),
          'cap', floor(private.helper_hourly(taverns.player_id) * settings.tip_jar_cap_hours)),
        'goals', coalesce((
          select jsonb_agg(jsonb_build_object(
            'id', kinds.id, 'label', kinds.label, 'progress', goals.progress, 'target', kinds.target,
            'reward', kinds.reward, 'done', goals.done_at is not null) order by kinds.reward, kinds.id)
          from public.tavern_goals goals
          join public.goal_kinds kinds on kinds.id = goals.goal_id
          where goals.player_id = taverns.player_id and goals.day = current_date), '[]'))
      from public.taverns, private.settings
      where taverns.player_id = p_player_id));
$$;
