drop function if exists private.advance_goals(uuid, integer, integer, jsonb);

create or replace function private.advance_goals(p_player_id uuid, p_served integer, p_perfect integer, p_drinks jsonb, p_regulars integer) returns integer
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
    when 'regular' then p_regulars
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

create or replace function private.advance_regulars(p_player_id uuid, p_regulars jsonb, p_room integer, p_budget integer) returns integer
language plpgsql
set search_path = ''
as $$
declare
  v_regular record;
  v_served integer;
  v_perfect integer;
  v_total integer := 0;
  v_before integer;
  v_after integer;
begin
  for v_regular in
    select regulars.id, entries.value
    from jsonb_each(coalesce(p_regulars, '{}')) entries
    join public.regulars on regulars.id = entries.key
    where jsonb_typeof(entries.value) = 'object'
  loop
    v_served := least(private.whole(v_regular.value -> 'served'), p_room, p_budget - v_total);
    if v_served <= 0 then
      continue;
    end if;

    v_perfect := least(private.whole(v_regular.value -> 'perfect'), v_served);
    v_total := v_total + v_served;
    select coalesce((select chapter from public.tavern_regulars where player_id = p_player_id and regular_id = v_regular.id), 0) into v_before;

    insert into public.tavern_regulars as known (player_id, regular_id, friendship, visits, last_visit)
    values (p_player_id, v_regular.id, v_served + v_perfect, v_served, now())
    on conflict (player_id, regular_id) do update
    set friendship = known.friendship + excluded.friendship,
        visits = known.visits + excluded.visits,
        last_visit = now();

    update public.tavern_regulars known
    set chapter = coalesce((
      select max(chapters.chapter) from public.regular_chapters chapters
      where chapters.regular_id = known.regular_id and chapters.friendship <= known.friendship), 0)
    where known.player_id = p_player_id and known.regular_id = v_regular.id
    returning chapter into v_after;

    if v_before < 5 and v_after = 5 then
      update public.taverns set coins = coins + (select souvenir_reward from private.settings) where player_id = p_player_id;
    end if;
  end loop;

  return v_total;
end;
$$;

create or replace function public.report_service(p_report jsonb) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_tavern public.taverns;
  v_settings private.settings;
  v_menu text[];
  v_cycles integer;
  v_room integer;
  v_drink text;
  v_count integer;
  v_served integer := 0;
  v_accepted jsonb := '{}';
  v_perfect integer;
  v_parting integer;
  v_coins bigint;
  v_regulars integer;
begin
  v_tavern := private.lock_tavern();
  select * into v_settings from private.settings;
  v_menu := private.menu(v_tavern.player_id);
  v_cycles := 1 + floor(least(extract(epoch from now() - v_tavern.reported_at), v_settings.report_window_seconds) / v_settings.serve_cycle_seconds)::integer;
  v_room := private.stools(v_tavern.player_id) * v_cycles;

  for v_drink, v_count in
    select key, private.whole(value) from jsonb_each(coalesce(p_report -> 'drinks', '{}')) where key = any (v_menu)
  loop
    v_count := least(v_count, v_room - v_served);
    v_served := v_served + v_count;
    v_accepted := v_accepted || jsonb_build_object(v_drink, v_count);
  end loop;

  v_perfect := least(private.whole(p_report -> 'perfect'), v_served);
  v_parting := least(private.whole(p_report -> 'parting'), v_room);
  v_coins := least(private.whole(p_report -> 'coins'), v_served * v_settings.max_drink_coins + v_parting);

  perform private.settle_absence(v_tavern.player_id);
  update public.taverns
  set coins = coins + v_coins,
      served = served + v_served,
      perfect = perfect + v_perfect,
      renown = renown + v_served + v_perfect,
      reported_at = now(),
      last_seen = now()
  where player_id = v_tavern.player_id;

  v_regulars := private.advance_regulars(
    v_tavern.player_id,
    case when jsonb_typeof(p_report -> 'regulars') = 'object' then p_report -> 'regulars' end,
    v_cycles,
    v_served);
  perform private.ensure_goals(v_tavern.player_id);
  perform private.advance_goals(v_tavern.player_id, v_served, v_perfect, v_accepted, v_regulars);
  return private.state(v_tavern.player_id);
end;
$$;

create or replace function private.regulars_state(p_player_id uuid) returns jsonb
language sql stable
set search_path = ''
as $$
  select coalesce(jsonb_agg(jsonb_build_object(
    'id', regular_id, 'friendship', friendship, 'chapter', chapter, 'visits', visits) order by regular_id), '[]')
  from public.tavern_regulars
  where player_id = p_player_id;
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
          where goals.player_id = taverns.player_id and goals.day = current_date), '[]'),
        'regulars', private.regulars_state(taverns.player_id))
      from public.taverns, private.settings
      where taverns.player_id = p_player_id));
$$;

create or replace function public.get_world() returns jsonb
language sql stable
security definer
set search_path = ''
as $$
  select jsonb_build_object(
    'tiers', (select jsonb_agg(jsonb_build_object('tier', tier, 'name', name, 'renown', renown) order by tier) from public.renown_tiers),
    'drinks', (select jsonb_agg(jsonb_build_object('id', id, 'name', name, 'price', price) order by sort) from public.drinks),
    'upgrades', (select jsonb_agg(jsonb_build_object(
      'id', id, 'kind', kind, 'name', name, 'description', description, 'price', price,
      'tier', tier, 'requires', requires, 'value', value) order by sort) from public.upgrades),
    'regulars', (select jsonb_agg(jsonb_build_object(
      'id', regulars.id, 'name', regulars.name, 'title', regulars.title, 'drink', regulars.drink,
      'condition', regulars.condition, 'hint', regulars.hint, 'souvenir', regulars.souvenir,
      'chapters', (select jsonb_agg(jsonb_build_object('chapter', chapter, 'friendship', friendship, 'line', line) order by chapter)
        from public.regular_chapters where regular_id = regulars.id)) order by regulars.sort)
      from public.regulars));
$$;

revoke all on function public.report_service(jsonb) from public, anon;
revoke all on function public.get_world() from public, anon;
grant execute on function public.report_service(jsonb) to authenticated;
grant execute on function public.get_world() to authenticated;
