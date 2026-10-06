create or replace function private.current_pressure(p_pressure numeric, p_since timestamptz) returns numeric
language sql stable
set search_path = ''
as $$
  select p_pressure * exp(-greatest(extract(epoch from (now() - p_since)), 0) / 14400.0);
$$;

create or replace function private.unit_price(p_base numeric, p_pressure numeric) returns numeric
language sql immutable
set search_path = ''
as $$
  select p_base * exp(-0.01 * greatest(least(p_pressure, 300), -300));
$$;

create or replace function private.buy_total(p_base numeric, p_pressure numeric, p_quantity integer) returns bigint
language sql immutable
set search_path = ''
as $$
  select ceil(private.unit_price(p_base, p_pressure - p_quantity / 2.0) * 1.08 * p_quantity)::bigint;
$$;

create or replace function private.sell_total(p_base numeric, p_pressure numeric, p_quantity integer) returns bigint
language sql immutable
set search_path = ''
as $$
  select greatest(floor(private.unit_price(p_base, p_pressure + p_quantity / 2.0) * 0.92 * p_quantity), p_quantity)::bigint;
$$;

create or replace function private.buy_quote(p_base numeric, p_pressure numeric) returns numeric
language sql immutable
set search_path = ''
as $$
  select round(private.unit_price(p_base, p_pressure - 0.5) * 1.08, 1);
$$;

create or replace function private.sell_quote(p_base numeric, p_pressure numeric) returns numeric
language sql immutable
set search_path = ''
as $$
  select greatest(round(private.unit_price(p_base, p_pressure + 0.5) * 0.92, 1), 1);
$$;

create or replace function private.capacity(p_wagons integer) returns integer
language sql immutable
set search_path = ''
as $$
  select 20 + 20 * p_wagons;
$$;

create or replace function private.wagon_price(p_wagon_number integer) returns integer
language sql immutable
set search_path = ''
as $$
  select (round(300 * power(1.7, p_wagon_number - 2) / 10) * 10)::integer;
$$;

create or replace function private.workshop_speed(p_level integer) returns numeric
language sql immutable
set search_path = ''
as $$
  select 1 + 0.25 * (p_level - 1);
$$;

create or replace function private.storage_capacity(p_level integer) returns integer
language sql immutable
set search_path = ''
as $$
  select 40 + 20 * p_level;
$$;

create or replace function private.max_queue(p_level integer) returns integer
language sql immutable
set search_path = ''
as $$
  select 4 + 2 * p_level;
$$;

create or replace function private.workshop_price(p_level integer) returns integer
language sql immutable
set search_path = ''
as $$
  select (round(400 * power(1.8, p_level - 2) / 10) * 10)::integer;
$$;

create or replace function private.is_itinerant(p_craft_id text) returns boolean
language sql stable
set search_path = ''
as $$
  select coalesce((select kind = 'itinerant' from public.crafts where id = p_craft_id), false);
$$;

create or replace function private.cargo_load(p_player_id uuid) returns integer
language sql stable
set search_path = ''
as $$
  select coalesce(sum(quantity), 0)::integer from public.cargo where player_id = p_player_id;
$$;

create or replace function private.warehouse_load(p_player_id uuid, p_town_id text) returns integer
language sql stable
set search_path = ''
as $$
  select coalesce(sum(quantity), 0)::integer from public.warehouses where player_id = p_player_id and town_id = p_town_id;
$$;

create or replace function private.change_cargo(p_player_id uuid, p_good_id text, p_delta integer) returns void
language plpgsql
set search_path = ''
as $$
declare
  v_held integer;
begin
  select quantity into v_held from public.cargo where player_id = p_player_id and good_id = p_good_id for update;
  if coalesce(v_held, 0) + p_delta < 0 then
    raise exception 'Il n''y en a pas assez dans la cale.';
  elsif coalesce(v_held, 0) + p_delta = 0 then
    delete from public.cargo where player_id = p_player_id and good_id = p_good_id;
  elsif v_held is null then
    insert into public.cargo (player_id, good_id, quantity) values (p_player_id, p_good_id, p_delta);
  else
    update public.cargo set quantity = quantity + p_delta where player_id = p_player_id and good_id = p_good_id;
  end if;
end;
$$;

create or replace function private.change_warehouse(p_player_id uuid, p_town_id text, p_good_id text, p_delta integer) returns void
language plpgsql
set search_path = ''
as $$
declare
  v_held integer;
begin
  select quantity into v_held from public.warehouses
  where player_id = p_player_id and town_id = p_town_id and good_id = p_good_id
  for update;
  if coalesce(v_held, 0) + p_delta < 0 then
    raise exception 'Il n''y en a pas assez dans l''entrepôt.';
  elsif coalesce(v_held, 0) + p_delta = 0 then
    delete from public.warehouses where player_id = p_player_id and town_id = p_town_id and good_id = p_good_id;
  elsif v_held is null then
    insert into public.warehouses (player_id, town_id, good_id, quantity) values (p_player_id, p_town_id, p_good_id, p_delta);
  else
    update public.warehouses set quantity = quantity + p_delta
    where player_id = p_player_id and town_id = p_town_id and good_id = p_good_id;
  end if;
end;
$$;

create or replace function private.lock_player() returns public.players
language plpgsql
set search_path = ''
as $$
declare
  v_player public.players;
begin
  select * into v_player from public.players where player_id = auth.uid() for update;
  if not found then
    raise exception 'Installe-toi d''abord dans une ville.';
  end if;

  return v_player;
end;
$$;

create or replace function private.lock_caravan_in_town() returns public.caravans
language plpgsql
set search_path = ''
as $$
declare
  v_caravan public.caravans;
begin
  select * into v_caravan from public.caravans where player_id = auth.uid() for update;
  if not found then
    raise exception 'Ton métier ne voyage pas : ton atelier reste dans ta ville.';
  end if;

  if v_caravan.arrives_at is not null and v_caravan.arrives_at > now() then
    raise exception 'Ta caravane est encore en route.';
  end if;

  return v_caravan;
end;
$$;

create or replace function private.lock_workshop() returns public.workshops
language plpgsql
set search_path = ''
as $$
declare
  v_workshop public.workshops;
  v_recipe public.recipes;
  v_done integer;
begin
  select * into v_workshop from public.workshops where player_id = auth.uid() for update;
  if not found then
    raise exception 'Ton métier n''a pas d''atelier : ta caravane voyage pour toi.';
  end if;

  if v_workshop.queued = 0 then
    return v_workshop;
  end if;

  v_done := least(v_workshop.queued, floor(extract(epoch from (now() - v_workshop.started_at)) / v_workshop.batch_seconds)::integer);
  if v_done <= 0 then
    return v_workshop;
  end if;

  select * into v_recipe from public.recipes where id = v_workshop.recipe_id;
  perform private.change_warehouse(v_workshop.player_id, v_workshop.town_id, v_recipe.output_good_id, v_done * v_recipe.output_quantity);

  if v_done = v_workshop.queued then
    update public.workshops
    set queued = 0, recipe_id = null, batch_seconds = null, started_at = null
    where player_id = v_workshop.player_id
    returning * into v_workshop;
  else
    update public.workshops
    set queued = queued - v_done, started_at = started_at + make_interval(secs => v_done * batch_seconds)
    where player_id = v_workshop.player_id
    returning * into v_workshop;
  end if;

  return v_workshop;
end;
$$;

revoke all on all functions in schema private from public, anon, authenticated;
