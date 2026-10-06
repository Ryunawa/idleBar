create or replace function public.buy_goods(p_good_id text, p_quantity integer) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player public.players;
  v_caravan public.caravans;
  v_workshop public.workshops;
  v_market public.markets;
  v_town text;
  v_free integer;
  v_pressure numeric;
  v_quantity integer;
  v_total bigint;
begin
  if p_quantity is null or p_quantity < 1 then
    raise exception 'Quantité invalide.';
  end if;

  v_player := private.lock_player();
  if private.is_itinerant(v_player.craft_id) then
    v_caravan := private.lock_caravan_in_town();
    v_town := v_caravan.town_id;
    v_free := private.capacity(v_caravan.wagons) - private.cargo_load(v_player.player_id);
  else
    v_workshop := private.lock_workshop();
    v_town := v_workshop.town_id;
    v_free := private.storage_capacity(v_workshop.level) - private.warehouse_load(v_player.player_id, v_town);
  end if;

  select * into v_market from public.markets where town_id = v_town and good_id = p_good_id for update;
  if not found then
    raise exception 'Cette marchandise n''est pas vendue ici.';
  end if;

  v_quantity := least(p_quantity, v_free);
  if v_quantity < 1 then
    raise exception '%', case when v_caravan.player_id is null then 'Ton entrepôt est plein.' else 'Ta cale est pleine.' end;
  end if;

  v_pressure := private.current_pressure(v_market.pressure, v_market.pressure_at);
  while v_quantity > 0 and private.buy_total(v_market.base_price, v_pressure, v_quantity) > v_player.coins loop
    v_quantity := v_quantity - 1;
  end loop;
  if v_quantity < 1 then
    raise exception 'Pas assez d''écus.';
  end if;

  v_total := private.buy_total(v_market.base_price, v_pressure, v_quantity);
  update public.players set coins = coins - v_total where player_id = v_player.player_id;
  if v_caravan.player_id is null then
    perform private.change_warehouse(v_player.player_id, v_town, p_good_id, v_quantity);
  else
    perform private.change_cargo(v_player.player_id, p_good_id, v_quantity);
  end if;

  update public.markets
  set pressure = greatest(v_pressure - v_quantity, -300), pressure_at = now()
  where town_id = v_market.town_id and good_id = v_market.good_id;

  return public.get_state();
end;
$$;

create or replace function public.sell_goods(p_good_id text, p_quantity integer) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player public.players;
  v_caravan public.caravans;
  v_workshop public.workshops;
  v_market public.markets;
  v_town text;
  v_owned integer;
  v_pressure numeric;
  v_quantity integer;
begin
  if p_quantity is null or p_quantity < 1 then
    raise exception 'Quantité invalide.';
  end if;

  v_player := private.lock_player();
  if private.is_itinerant(v_player.craft_id) then
    v_caravan := private.lock_caravan_in_town();
    v_town := v_caravan.town_id;
    select quantity into v_owned from public.cargo where player_id = v_player.player_id and good_id = p_good_id;
  else
    v_workshop := private.lock_workshop();
    v_town := v_workshop.town_id;
    select quantity into v_owned from public.warehouses
    where player_id = v_player.player_id and town_id = v_town and good_id = p_good_id;
  end if;

  if coalesce(v_owned, 0) < 1 then
    raise exception 'Tu n''as pas cette marchandise.';
  end if;

  select * into v_market from public.markets where town_id = v_town and good_id = p_good_id for update;
  if not found then
    raise exception 'Personne n''achète cette marchandise ici.';
  end if;

  v_quantity := least(p_quantity, v_owned);
  v_pressure := private.current_pressure(v_market.pressure, v_market.pressure_at);
  update public.players
  set coins = coins + private.sell_total(v_market.base_price, v_pressure, v_quantity)
  where player_id = v_player.player_id;

  if v_caravan.player_id is null then
    perform private.change_warehouse(v_player.player_id, v_town, p_good_id, -v_quantity);
  else
    perform private.change_cargo(v_player.player_id, p_good_id, -v_quantity);
  end if;

  update public.markets
  set pressure = least(v_pressure + v_quantity, 300), pressure_at = now()
  where town_id = v_market.town_id and good_id = v_market.good_id;

  return public.get_state();
end;
$$;

revoke all on function public.buy_goods(text, integer), public.sell_goods(text, integer) from public, anon;
grant execute on function public.buy_goods(text, integer), public.sell_goods(text, integer) to authenticated;
