drop function if exists public.post_offer(text, integer, text, integer, text);

create or replace function public.post_offer(
  p_give_good_id text,
  p_give_quantity integer,
  p_want_good_id text,
  p_want_quantity integer,
  p_town_id text default null,
  p_from_warehouse boolean default false
) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player public.players;
  v_town text;
  v_settings private.settings;
begin
  if p_give_quantity is null or p_give_quantity < 1 or p_want_quantity is null or p_want_quantity < 1 then
    raise exception 'Quantité invalide.';
  end if;

  if p_give_good_id is null and p_want_good_id is null then
    raise exception 'Échanger des écus contre des écus n''a pas de sens.';
  end if;

  if p_give_good_id = p_want_good_id then
    raise exception 'Choisis deux marchandises différentes.';
  end if;

  if (p_give_good_id is not null and not exists (select 1 from public.goods where id = p_give_good_id))
    or (p_want_good_id is not null and not exists (select 1 from public.goods where id = p_want_good_id)) then
    raise exception 'Cette marchandise n''existe pas.';
  end if;

  v_player := private.lock_player();
  select * into v_settings from private.settings;
  if (select count(*) from public.offers where seller_id = v_player.player_id and status = 'open') >= v_settings.max_open_offers then
    raise exception 'Tu as déjà % offres au comptoir : retires-en une d''abord.', v_settings.max_open_offers;
  end if;

  v_town := private.acting_town(v_player, p_town_id);
  if p_give_good_id is null then
    if v_player.coins < p_give_quantity then
      raise exception 'Pas assez d''écus.';
    end if;

    update public.players set coins = coins - p_give_quantity where player_id = v_player.player_id;
  elsif p_from_warehouse then
    if coalesce((select quantity from public.warehouses
        where player_id = v_player.player_id and town_id = v_town and good_id = p_give_good_id), 0) < p_give_quantity then
      raise exception 'Tu n''en as pas assez dans ton entrepôt pour cette offre.';
    end if;

    perform private.change_warehouse(v_player.player_id, v_town, p_give_good_id, -p_give_quantity);
  else
    if private.held(v_player, v_town, p_give_good_id) < p_give_quantity then
      raise exception 'Tu n''en as pas assez pour cette offre.';
    end if;

    perform private.change_holdings(v_player, v_town, p_give_good_id, -p_give_quantity);
  end if;

  insert into public.offers (town_id, seller_id, give_good_id, give_quantity, want_good_id, want_quantity, expires_at)
  values (v_town, v_player.player_id, p_give_good_id, p_give_quantity, p_want_good_id, p_want_quantity,
    now() + make_interval(hours => v_settings.offer_hours));
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
  v_stored integer := 0;
  v_from_warehouse integer;
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
  if private.is_itinerant(v_player.craft_id) then
    v_stored := coalesce((select quantity from public.warehouses
      where player_id = v_player.player_id and town_id = v_town and good_id = p_good_id), 0);
  end if;

  if v_owned + v_stored < 1 then
    raise exception 'Tu n''as pas cette marchandise.';
  end if;

  select * into v_market from public.markets where town_id = v_town and good_id = p_good_id for update;
  if not found then
    raise exception 'Personne n''achète cette marchandise ici.';
  end if;

  v_quantity := least(p_quantity, v_owned + v_stored);
  v_pressure := private.current_pressure(v_market.pressure, v_market.pressure_at);
  v_earned := floor(private.sell_total(v_market.base_price, v_pressure, v_quantity) * (1 + private.bonus(v_player.player_id, 'haggle')));
  update public.players
  set coins = coins + v_earned
  where player_id = v_player.player_id;
  v_from_warehouse := least(v_quantity, v_stored);
  if v_from_warehouse > 0 then
    perform private.change_warehouse(v_player.player_id, v_town, p_good_id, -v_from_warehouse);
  end if;

  if v_quantity > v_from_warehouse then
    perform private.change_holdings(v_player, v_town, p_good_id, v_from_warehouse - v_quantity);
  end if;

  if private.craft_family(v_player.craft_id) = 'negociant' then
    perform private.gain_trade_xp(v_player.player_id, v_earned);
  end if;

  update public.markets
  set pressure = least(v_pressure + v_quantity, 300), pressure_at = now()
  where town_id = v_market.town_id and good_id = v_market.good_id;

  return public.get_state();
end;
$$;

create or replace function public.attach_wagon() returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_caravan public.caravans;
  v_stored integer;
begin
  perform private.lock_player();
  v_caravan := private.lock_caravan_in_town();
  if v_caravan.wagons >= (select max_wagons from private.settings) then
    raise exception 'Ta caravane ne peut pas tirer plus de chariots.';
  end if;

  v_stored := coalesce((select quantity from public.warehouses
    where player_id = v_caravan.player_id and town_id = v_caravan.town_id and good_id = 'chariot'), 0);
  if v_stored > 0 then
    perform private.change_warehouse(v_caravan.player_id, v_caravan.town_id, 'chariot', -1);
  elsif coalesce((select quantity from public.cargo where player_id = v_caravan.player_id and good_id = 'chariot'), 0) > 0 then
    perform private.change_cargo(v_caravan.player_id, 'chariot', -1);
  else
    raise exception 'Il te faut un chariot du charron dans ta cale ou dans ton entrepôt de cette ville.';
  end if;

  update public.caravans set wagons = wagons + 1 where player_id = v_caravan.player_id;
  return public.get_state();
end;
$$;

revoke all on function
  public.post_offer(text, integer, text, integer, text, boolean),
  public.sell_goods(text, integer, text),
  public.attach_wagon()
from public, anon;
grant execute on function
  public.post_offer(text, integer, text, integer, text, boolean),
  public.sell_goods(text, integer, text),
  public.attach_wagon()
to authenticated;
