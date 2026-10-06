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
  select (
    coalesce((select sum(quantity) from public.cargo where player_id = p_player_id), 0)
    + coalesce((select sum(quantity) from public.transport_contracts where carrier_id = p_player_id and status = 'carried'), 0)
  )::integer;
$$;

create or replace function private.warehouse_load(p_player_id uuid, p_town_id text) returns integer
language sql stable
set search_path = ''
as $$
  select coalesce(sum(quantity), 0)::integer from public.warehouses where player_id = p_player_id and town_id = p_town_id;
$$;

create or replace function private.random_round(p_value numeric) returns integer
language sql volatile
set search_path = ''
as $$
  select (floor(p_value) + case when random() < p_value - floor(p_value) then 1 else 0 end)::integer;
$$;

create or replace function private.craft_family(p_craft_id text) returns text
language sql stable
set search_path = ''
as $$
  select family from public.crafts where id = p_craft_id;
$$;

create or replace function private.bonus(p_player_id uuid, p_effect text) returns numeric
language sql stable
set search_path = ''
as $$
  select
    coalesce((select sum(talents.amount) from public.player_talents
      join public.talents on talents.id = player_talents.talent_id
      where player_talents.player_id = p_player_id and talents.effect = p_effect), 0)
    + coalesce((select sum(fittings.amount) from public.caravan_fittings
      join public.fittings on fittings.id = caravan_fittings.fitting_id
      where caravan_fittings.player_id = p_player_id and fittings.effect = p_effect), 0);
$$;

create or replace function private.caravan_capacity(p_player_id uuid) returns integer
language sql stable
set search_path = ''
as $$
  select private.capacity(wagons) + private.bonus(p_player_id, 'capacity')::integer
  from public.caravans where player_id = p_player_id;
$$;

create or replace function private.warehouse_capacity(p_player_id uuid) returns integer
language sql stable
set search_path = ''
as $$
  select coalesce(
    (select private.storage_capacity(level) from public.workshops where player_id = p_player_id),
    (select depot_capacity from private.settings)
  ) + private.bonus(p_player_id, 'storage')::integer;
$$;

create or replace function private.warehouse_free(p_player_id uuid, p_town_id text) returns integer
language sql stable
set search_path = ''
as $$
  select private.warehouse_capacity(p_player_id) - private.warehouse_load(p_player_id, p_town_id);
$$;

create or replace function private.branch_price(p_branch_number integer) returns integer
language sql immutable
set search_path = ''
as $$
  select (round(300 * power(1.8, p_branch_number - 1) / 10) * 10)::integer;
$$;

create or replace function private.journey_minutes(p_from_town_id text, p_to_town_id text) returns integer
language sql stable
set search_path = ''
as $$
  with recursive paths (town_id, minutes, visited) as (
    select p_from_town_id, 0, array[p_from_town_id]
    union all
    select routes.to_town_id, paths.minutes + routes.minutes, paths.visited || routes.to_town_id
    from paths
    join public.routes on routes.from_town_id = paths.town_id
    where paths.town_id <> p_to_town_id and routes.to_town_id <> all (paths.visited)
  )
  select min(minutes)::integer from paths where town_id = p_to_town_id;
$$;

create or replace function private.freight_fee(p_good_id text, p_quantity integer, p_minutes integer) returns integer
language sql stable
set search_path = ''
as $$
  select greatest(ceil(goods.base_price * p_quantity * (settings.freight_base_rate + settings.freight_hourly_rate * p_minutes / 60.0)), 1)::integer
  from public.goods
  cross join private.settings
  where goods.id = p_good_id;
$$;

create or replace function private.present_towns(p_player_id uuid) returns text[]
language sql stable
set search_path = ''
as $$
  select case
    when caravans.player_id is null then array[players.home_town_id] || array(
      select branches.town_id from public.branches
      where branches.player_id = players.player_id
      order by branches.opened_at, branches.town_id
    )
    when caravans.arrives_at is null or caravans.arrives_at <= now() then array[caravans.town_id]
    else array[]::text[]
  end
  from public.players
  left join public.caravans on caravans.player_id = players.player_id
  where players.player_id = p_player_id;
$$;

create or replace function private.can_collect_in(p_player public.players, p_town_id text) returns boolean
language sql stable
set search_path = ''
as $$
  select private.is_itinerant(p_player.craft_id)
    or p_town_id = p_player.home_town_id
    or exists (select 1 from public.branches where player_id = p_player.player_id and town_id = p_town_id);
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
  perform private.settle_player(auth.uid());
  if exists (select 1 from public.workshops where player_id = auth.uid()) then
    perform private.lock_workshop();
  end if;

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
begin
  select * into v_workshop from public.workshops where player_id = auth.uid() for update;
  if not found then
    raise exception 'Ton métier n''a pas d''atelier : ta caravane voyage pour toi.';
  end if;

  perform private.settle_breakdown(v_workshop.player_id);
  return private.produce_until(v_workshop.player_id, now());
end;
$$;

create or replace function private.produce_until(p_player_id uuid, p_moment timestamptz) returns public.workshops
language plpgsql
set search_path = ''
as $$
declare
  v_workshop public.workshops;
  v_recipe public.recipes;
  v_done integer;
begin
  select * into v_workshop from public.workshops where player_id = p_player_id for update;
  if v_workshop.queued = 0 then
    return v_workshop;
  end if;

  v_done := least(v_workshop.queued, floor(extract(epoch from (p_moment - v_workshop.started_at)) / v_workshop.batch_seconds)::integer);
  if v_done <= 0 then
    return v_workshop;
  end if;

  select * into v_recipe from public.recipes where id = v_workshop.recipe_id;
  perform private.change_warehouse(v_workshop.player_id, v_workshop.town_id, v_recipe.output_good_id, v_done * v_recipe.output_quantity);
  perform private.reward_production(v_workshop.player_id, v_recipe, v_done, v_workshop.town_id);

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

create or replace function private.acting_town(p_player public.players, p_town_id text) returns text
language plpgsql
set search_path = ''
as $$
declare
  v_caravan public.caravans;
begin
  if private.is_itinerant(p_player.craft_id) then
    v_caravan := private.lock_caravan_in_town();
    if p_town_id is not null and p_town_id <> v_caravan.town_id then
      raise exception 'Ta caravane n''est pas dans cette ville.';
    end if;

    return v_caravan.town_id;
  end if;

  perform private.lock_workshop();
  if p_town_id is null or p_town_id = p_player.home_town_id then
    return p_player.home_town_id;
  end if;

  if not exists (select 1 from public.branches where player_id = p_player.player_id and town_id = p_town_id) then
    raise exception 'Tu n''as pas de comptoir dans cette ville.';
  end if;

  return p_town_id;
end;
$$;

create or replace function private.held(p_player public.players, p_town_id text, p_good_id text) returns integer
language sql stable
set search_path = ''
as $$
  select coalesce(case
    when private.is_itinerant(p_player.craft_id) then
      (select quantity from public.cargo where player_id = p_player.player_id and good_id = p_good_id)
    else
      (select quantity from public.warehouses where player_id = p_player.player_id and town_id = p_town_id and good_id = p_good_id)
  end, 0);
$$;

create or replace function private.free_space(p_player public.players, p_town_id text) returns integer
language sql stable
set search_path = ''
as $$
  select case
    when private.is_itinerant(p_player.craft_id) then
      private.caravan_capacity(p_player.player_id) - private.cargo_load(p_player.player_id)
    else
      private.warehouse_free(p_player.player_id, p_town_id)
  end;
$$;

create or replace function private.full_message(p_player public.players) returns text
language sql stable
set search_path = ''
as $$
  select case when private.is_itinerant(p_player.craft_id) then 'Ta cale est pleine.' else 'Ton entrepôt est plein.' end;
$$;

create or replace function private.change_holdings(p_player public.players, p_town_id text, p_good_id text, p_delta integer) returns void
language plpgsql
set search_path = ''
as $$
begin
  if private.is_itinerant(p_player.craft_id) then
    perform private.change_cargo(p_player.player_id, p_good_id, p_delta);
  else
    perform private.change_warehouse(p_player.player_id, p_town_id, p_good_id, p_delta);
  end if;
end;
$$;

create or replace function private.credit(p_player_id uuid, p_town_id text, p_good_id text, p_quantity integer) returns void
language plpgsql
set search_path = ''
as $$
begin
  if p_good_id is null then
    update public.players set coins = coins + p_quantity where player_id = p_player_id;
  else
    perform private.change_warehouse(p_player_id, p_town_id, p_good_id, p_quantity);
  end if;
end;
$$;

create or replace function private.settle_offers(p_player_id uuid) returns void
language plpgsql
set search_path = ''
as $$
declare
  v_offer public.offers;
begin
  for v_offer in
    select * from public.offers
    where seller_id = p_player_id and status = 'open' and expires_at <= now()
    order by id
    for update
  loop
    perform private.credit(v_offer.seller_id, v_offer.town_id, v_offer.give_good_id, v_offer.give_quantity);
    update public.offers set status = 'expired', closed_at = expires_at, seller_seen = false where id = v_offer.id;
  end loop;
end;
$$;

create or replace function private.settle_contracts(p_player_id uuid) returns void
language plpgsql
set search_path = ''
as $$
declare
  v_contract public.transport_contracts;
  v_arrived_at timestamptz;
  v_carrier uuid;
begin
  for v_carrier in
    select distinct carrier_id from public.transport_contracts
    where shipper_id = p_player_id and status = 'carried' and carrier_id is not null
    order by carrier_id
  loop
    perform private.settle_trip(v_carrier);
  end loop;

  for v_contract in
    select * from public.transport_contracts
    where p_player_id in (shipper_id, carrier_id) and status in ('open', 'carried', 'shipped')
    order by id
    for update
  loop
    if v_contract.status = 'open' and v_contract.takeover_at <= now() then
      update public.players
      set coins = coins + greatest(v_contract.reward, v_contract.game_fee) - v_contract.game_fee
      where player_id = v_contract.shipper_id;
      update public.transport_contracts set status = 'shipped' where id = v_contract.id returning * into v_contract;
    end if;

    if v_contract.status = 'shipped' and v_contract.game_arrives_at <= now() then
      perform private.change_warehouse(v_contract.shipper_id, v_contract.destination_town_id, v_contract.good_id, v_contract.quantity);
      update public.transport_contracts
      set status = 'delivered', closed_at = game_arrives_at, shipper_seen = false
      where id = v_contract.id;
      perform private.reward_delivery(v_contract);
    elsif v_contract.status = 'carried' then
      select arrives_at into v_arrived_at from public.caravans
      where player_id = v_contract.carrier_id
        and town_id = v_contract.destination_town_id
        and arrives_at > v_contract.carried_at
        and arrives_at <= least(now(), v_contract.deadline);

      if v_arrived_at is not null then
        perform private.change_warehouse(v_contract.shipper_id, v_contract.destination_town_id, v_contract.good_id, v_contract.quantity);
        update public.players set coins = coins + v_contract.reward + v_contract.deposit where player_id = v_contract.carrier_id;
        update public.transport_contracts
        set status = 'delivered', closed_at = v_arrived_at, shipper_seen = false, carrier_seen = false
        where id = v_contract.id;
        perform private.reward_delivery(v_contract);
      elsif v_contract.deadline <= now() then
        update public.players set coins = coins + v_contract.reward + v_contract.deposit where player_id = v_contract.shipper_id;
        if v_contract.carrier_id is not null then
          perform private.change_cargo(v_contract.carrier_id, v_contract.good_id, v_contract.quantity);
        end if;

        update public.transport_contracts
        set status = 'failed', closed_at = deadline, shipper_seen = false, carrier_seen = false
        where id = v_contract.id;
      end if;
    end if;
  end loop;
end;
$$;

drop function if exists private.settle_exchanges(uuid);

create or replace function private.settle_player(p_player_id uuid) returns void
language plpgsql
set search_path = ''
as $$
begin
  if p_player_id is null then
    return;
  end if;

  perform private.settle_trip(p_player_id);
  perform private.settle_offers(p_player_id);
  perform private.settle_contracts(p_player_id);
  perform private.settle_journey(p_player_id);
  perform private.settle_events(p_player_id);
end;
$$;

revoke all on all functions in schema private from public, anon, authenticated;
