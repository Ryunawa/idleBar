drop function if exists public.buy_goods(text, integer);
drop function if exists public.sell_goods(text, integer);

create or replace function public.buy_goods(p_good_id text, p_quantity integer, p_town_id text default null) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player public.players;
  v_market public.markets;
  v_town text;
  v_pressure numeric;
  v_quantity integer;
  v_total bigint;
  v_discount numeric;
begin
  if p_quantity is null or p_quantity < 1 then
    raise exception 'Quantité invalide.';
  end if;

  v_player := private.lock_player();
  v_town := private.acting_town(v_player, p_town_id);

  select * into v_market from public.markets where town_id = v_town and good_id = p_good_id for update;
  if not found then
    raise exception 'Cette marchandise n''est pas vendue ici.';
  end if;

  if not (select market_sells from public.goods where id = p_good_id) then
    raise exception 'Le marché n''en vend pas : achète-en à un artisan, au comptoir.';
  end if;

  v_quantity := least(p_quantity, private.free_space(v_player, v_town));
  if v_quantity < 1 then
    raise exception '%', private.full_message(v_player);
  end if;

  v_pressure := private.current_pressure(v_market.pressure, v_market.pressure_at);
  v_discount := 1 - private.bonus(v_player.player_id, 'haggle');
  while v_quantity > 0 and ceil(private.buy_total(v_market.base_price, v_pressure, v_quantity) * v_discount) > v_player.coins loop
    v_quantity := v_quantity - 1;
  end loop;
  if v_quantity < 1 then
    raise exception 'Pas assez d''écus.';
  end if;

  v_total := ceil(private.buy_total(v_market.base_price, v_pressure, v_quantity) * v_discount);
  update public.players set coins = coins - v_total where player_id = v_player.player_id;
  perform private.change_holdings(v_player, v_town, p_good_id, v_quantity);

  update public.markets
  set pressure = greatest(v_pressure - v_quantity, -300), pressure_at = now()
  where town_id = v_market.town_id and good_id = v_market.good_id;

  return public.get_state();
end;
$$;

create or replace function public.sell_goods(p_good_id text, p_quantity integer, p_town_id text default null) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player public.players;
  v_market public.markets;
  v_town text;
  v_owned integer;
  v_earned bigint;
  v_pressure numeric;
  v_quantity integer;
begin
  if p_quantity is null or p_quantity < 1 then
    raise exception 'Quantité invalide.';
  end if;

  v_player := private.lock_player();
  v_town := private.acting_town(v_player, p_town_id);
  v_owned := private.held(v_player, v_town, p_good_id);
  if v_owned < 1 then
    raise exception 'Tu n''as pas cette marchandise.';
  end if;

  select * into v_market from public.markets where town_id = v_town and good_id = p_good_id for update;
  if not found then
    raise exception 'Personne n''achète cette marchandise ici.';
  end if;

  v_quantity := least(p_quantity, v_owned);
  v_pressure := private.current_pressure(v_market.pressure, v_market.pressure_at);
  v_earned := floor(private.sell_total(v_market.base_price, v_pressure, v_quantity) * (1 + private.bonus(v_player.player_id, 'haggle')));
  update public.players
  set coins = coins + v_earned
  where player_id = v_player.player_id;
  perform private.change_holdings(v_player, v_town, p_good_id, -v_quantity);
  if private.craft_family(v_player.craft_id) = 'negociant' then
    perform private.gain_trade_xp(v_player.player_id, v_earned);
  end if;

  update public.markets
  set pressure = least(v_pressure + v_quantity, 300), pressure_at = now()
  where town_id = v_market.town_id and good_id = v_market.good_id;

  return public.get_state();
end;
$$;

revoke all on function public.buy_goods(text, integer, text), public.sell_goods(text, integer, text) from public, anon;
grant execute on function public.buy_goods(text, integer, text), public.sell_goods(text, integer, text) to authenticated;
