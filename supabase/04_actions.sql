create or replace function private.whole(p_value jsonb) returns integer
language sql immutable
set search_path = ''
as $$
  select case when jsonb_typeof(p_value) = 'number' then greatest(0, least(floor((p_value #>> '{}')::numeric), 1000000))::integer else 0 end;
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
      'tier', tier, 'requires', requires, 'value', value) order by sort) from public.upgrades));
$$;

create or replace function public.get_state() returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
begin
  if auth.uid() is null then
    raise exception 'Connecte-toi pour jouer.';
  end if;

  if not exists (select 1 from public.taverns where player_id = auth.uid()) then
    return jsonb_build_object('server_time', now(), 'tavern', null);
  end if;

  perform private.lock_tavern();
  perform private.settle_absence(auth.uid());
  perform private.ensure_goals(auth.uid());
  update public.taverns set last_seen = now() where player_id = auth.uid();
  return private.state(auth.uid());
end;
$$;

create or replace function public.found_tavern(p_name text) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_name text := btrim(coalesce(p_name, ''));
begin
  if auth.uid() is null then
    raise exception 'Connecte-toi pour jouer.';
  end if;

  if char_length(v_name) not between 2 and 24 then
    raise exception 'Le nom de la taverne doit faire entre 2 et 24 caractères.';
  end if;

  if exists (select 1 from public.taverns where player_id = auth.uid()) then
    raise exception 'Ta taverne est déjà ouverte.';
  end if;

  insert into public.taverns (player_id, name, friend_code) values (auth.uid(), v_name, private.new_friend_code());
  perform private.ensure_goals(auth.uid());
  return private.state(auth.uid());
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
  v_room integer;
  v_drink text;
  v_count integer;
  v_served integer := 0;
  v_accepted jsonb := '{}';
  v_perfect integer;
  v_parting integer;
  v_coins bigint;
begin
  v_tavern := private.lock_tavern();
  select * into v_settings from private.settings;
  v_menu := private.menu(v_tavern.player_id);
  v_room := private.stools(v_tavern.player_id) * (1 + floor(
    least(extract(epoch from now() - v_tavern.reported_at), v_settings.report_window_seconds) / v_settings.serve_cycle_seconds))::integer;

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

  perform private.ensure_goals(v_tavern.player_id);
  perform private.advance_goals(v_tavern.player_id, v_served, v_perfect, v_accepted);
  return private.state(v_tavern.player_id);
end;
$$;

create or replace function public.buy_upgrade(p_upgrade_id text) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_tavern public.taverns;
  v_upgrade public.upgrades;
begin
  v_tavern := private.lock_tavern();
  select * into v_upgrade from public.upgrades where id = p_upgrade_id;
  if not found then
    raise exception 'Cette amélioration n''existe pas.';
  end if;

  if exists (select 1 from public.tavern_upgrades where player_id = v_tavern.player_id and upgrade_id = v_upgrade.id) then
    raise exception 'Tu as déjà « % ».', v_upgrade.name;
  end if;

  if v_upgrade.requires is not null
    and not exists (select 1 from public.tavern_upgrades where player_id = v_tavern.player_id and upgrade_id = v_upgrade.requires) then
    raise exception 'Il faut d''abord « % ».', (select name from public.upgrades where id = v_upgrade.requires);
  end if;

  if private.tier_of(v_tavern.renown) < v_upgrade.tier then
    raise exception 'Ta taverne doit d''abord devenir « % ».', (select name from public.renown_tiers where tier = v_upgrade.tier);
  end if;

  if v_tavern.coins < v_upgrade.price then
    raise exception 'Il te manque % écus.', v_upgrade.price - v_tavern.coins;
  end if;

  update public.taverns set coins = coins - v_upgrade.price where player_id = v_tavern.player_id;
  insert into public.tavern_upgrades (player_id, upgrade_id) values (v_tavern.player_id, v_upgrade.id);
  return private.state(v_tavern.player_id);
end;
$$;

create or replace function public.collect_tip_jar() returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_tavern public.taverns;
begin
  v_tavern := private.lock_tavern();
  perform private.settle_absence(v_tavern.player_id);
  select * into v_tavern from public.taverns where player_id = v_tavern.player_id;
  if v_tavern.tip_jar < 1 then
    raise exception 'Le pot à pourboires est vide.';
  end if;

  update public.taverns set coins = coins + tip_jar, tip_jar = 0 where player_id = v_tavern.player_id;
  return private.state(v_tavern.player_id);
end;
$$;

revoke all on function public.get_world() from public, anon;
revoke all on function public.get_state() from public, anon;
revoke all on function public.found_tavern(text) from public, anon;
revoke all on function public.report_service(jsonb) from public, anon;
revoke all on function public.buy_upgrade(text) from public, anon;
revoke all on function public.collect_tip_jar() from public, anon;
grant execute on function public.get_world() to authenticated;
grant execute on function public.get_state() to authenticated;
grant execute on function public.found_tavern(text) to authenticated;
grant execute on function public.report_service(jsonb) to authenticated;
grant execute on function public.buy_upgrade(text) to authenticated;
grant execute on function public.collect_tip_jar() to authenticated;
