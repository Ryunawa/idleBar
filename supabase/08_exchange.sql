create or replace function private.offer_json(p_offer public.offers) returns jsonb
language sql immutable
set search_path = ''
as $$
  select jsonb_build_object(
    'id', p_offer.id,
    'town_id', p_offer.town_id,
    'give_good_id', p_offer.give_good_id,
    'give_quantity', p_offer.give_quantity,
    'want_good_id', p_offer.want_good_id,
    'want_quantity', p_offer.want_quantity,
    'status', p_offer.status,
    'expires_at', p_offer.expires_at,
    'closed_at', p_offer.closed_at
  );
$$;

create or replace function private.contract_json(p_contract public.transport_contracts) returns jsonb
language sql immutable
set search_path = ''
as $$
  select jsonb_build_object(
    'id', p_contract.id,
    'origin_town_id', p_contract.origin_town_id,
    'destination_town_id', p_contract.destination_town_id,
    'good_id', p_contract.good_id,
    'quantity', p_contract.quantity,
    'reward', p_contract.reward,
    'game_fee', p_contract.game_fee,
    'deposit', p_contract.deposit,
    'status', p_contract.status,
    'takeover_at', p_contract.takeover_at,
    'game_arrives_at', p_contract.game_arrives_at,
    'carried_at', p_contract.carried_at,
    'deadline', p_contract.deadline,
    'closed_at', p_contract.closed_at
  );
$$;

create or replace function private.exchange_state(p_player_id uuid, p_present text[]) returns jsonb
language sql stable
set search_path = ''
as $$
  select jsonb_build_object(
    'offers', (
      select coalesce(jsonb_agg(listed.entry order by listed.created_at desc, listed.id desc), '[]'::jsonb)
      from (
        select private.offer_json(offers) || jsonb_build_object('seller', sellers.name) as entry, offers.created_at, offers.id
        from public.offers
        join public.players sellers on sellers.player_id = offers.seller_id
        where offers.status = 'open'
          and offers.expires_at > now()
          and offers.town_id = any (p_present)
          and offers.seller_id <> p_player_id
        order by offers.created_at desc, offers.id desc
        limit 50
      ) listed
    ),
    'my_offers', (
      select coalesce(jsonb_agg(listed.entry order by listed.created_at desc, listed.id desc), '[]'::jsonb)
      from (
        select private.offer_json(offers) || jsonb_build_object('buyer', buyers.name, 'seen', offers.seller_seen) as entry, offers.created_at, offers.id
        from public.offers
        left join public.players buyers on buyers.player_id = offers.buyer_id
        where offers.seller_id = p_player_id
          and (offers.status = 'open' or offers.closed_at > now() - interval '48 hours')
        order by offers.created_at desc, offers.id desc
        limit 30
      ) listed
    ),
    'contracts', (
      select coalesce(jsonb_agg(listed.entry order by listed.takeover_at), '[]'::jsonb)
      from (
        select private.contract_json(contracts) || jsonb_build_object('shipper', shippers.name) as entry, contracts.takeover_at
        from public.transport_contracts contracts
        join public.players shippers on shippers.player_id = contracts.shipper_id
        where contracts.status = 'open'
          and contracts.takeover_at > now()
          and contracts.shipper_id <> p_player_id
          and private.is_itinerant((select craft_id from public.players where player_id = p_player_id))
        order by contracts.takeover_at
        limit 50
      ) listed
    ),
    'my_contracts', (
      select coalesce(jsonb_agg(listed.entry order by listed.created_at desc, listed.id desc), '[]'::jsonb)
      from (
        select private.contract_json(contracts) || jsonb_build_object(
          'role', case when contracts.shipper_id = p_player_id then 'shipper' else 'carrier' end,
          'shipper', shippers.name,
          'carrier', carriers.name,
          'seen', case when contracts.shipper_id = p_player_id then contracts.shipper_seen else contracts.carrier_seen end
        ) as entry, contracts.created_at, contracts.id
        from public.transport_contracts contracts
        join public.players shippers on shippers.player_id = contracts.shipper_id
        left join public.players carriers on carriers.player_id = contracts.carrier_id
        where p_player_id in (contracts.shipper_id, contracts.carrier_id)
          and (contracts.status in ('open', 'carried', 'shipped') or contracts.closed_at > now() - interval '48 hours')
        order by contracts.created_at desc, contracts.id desc
        limit 30
      ) listed
    ),
    'news', jsonb_build_object(
      'concluded', (
        select count(*) from public.offers
        where seller_id = p_player_id and status = 'concluded' and not seller_seen
      ),
      'expired', (
        select count(*) from public.offers
        where seller_id = p_player_id and status = 'expired' and not seller_seen
      ),
      'delivered', (
        select count(*) from public.transport_contracts
        where status = 'delivered'
          and ((shipper_id = p_player_id and not shipper_seen) or (carrier_id = p_player_id and not carrier_seen))
      ),
      'failed', (
        select count(*) from public.transport_contracts
        where status = 'failed'
          and ((shipper_id = p_player_id and not shipper_seen) or (carrier_id = p_player_id and not carrier_seen))
      )
    )
  );
$$;

revoke all on all functions in schema private from public, anon, authenticated;

create or replace function public.deposit_goods(p_good_id text, p_quantity integer) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player public.players;
  v_town text;
  v_owned integer;
  v_quantity integer;
begin
  if p_quantity is null or p_quantity < 1 then
    raise exception 'Quantité invalide.';
  end if;

  v_player := private.lock_player();
  if not private.is_itinerant(v_player.craft_id) then
    raise exception 'Tes marchandises sont déjà rangées dans ton entrepôt.';
  end if;

  v_town := private.acting_town(v_player, null);
  v_owned := private.held(v_player, v_town, p_good_id);
  if v_owned < 1 then
    raise exception 'Tu n''as pas cette marchandise.';
  end if;

  v_quantity := least(p_quantity, v_owned, private.warehouse_free(v_player.player_id, v_town));
  if v_quantity < 1 then
    raise exception 'Ton entrepôt est plein.';
  end if;

  perform private.change_cargo(v_player.player_id, p_good_id, -v_quantity);
  perform private.change_warehouse(v_player.player_id, v_town, p_good_id, v_quantity);
  return public.get_state();
end;
$$;

create or replace function public.withdraw_goods(p_good_id text, p_quantity integer) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player public.players;
  v_town text;
  v_stored integer;
  v_quantity integer;
begin
  if p_quantity is null or p_quantity < 1 then
    raise exception 'Quantité invalide.';
  end if;

  v_player := private.lock_player();
  if not private.is_itinerant(v_player.craft_id) then
    raise exception 'Ton métier ne voyage pas : tes marchandises restent à l''entrepôt.';
  end if;

  v_town := private.acting_town(v_player, null);
  select quantity into v_stored from public.warehouses
  where player_id = v_player.player_id and town_id = v_town and good_id = p_good_id;
  if coalesce(v_stored, 0) < 1 then
    raise exception 'Cette marchandise n''est pas dans ton entrepôt.';
  end if;

  v_quantity := least(p_quantity, v_stored, private.free_space(v_player, v_town));
  if v_quantity < 1 then
    raise exception 'Ta cale est pleine.';
  end if;

  perform private.change_warehouse(v_player.player_id, v_town, p_good_id, -v_quantity);
  perform private.change_cargo(v_player.player_id, p_good_id, v_quantity);
  return public.get_state();
end;
$$;

create or replace function public.open_branch(p_town_id text) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player public.players;
  v_count integer;
  v_price integer;
begin
  v_player := private.lock_player();
  if not coalesce((select opens_branches from public.crafts where id = v_player.craft_id), false) then
    raise exception 'Seul le négociant ouvre des succursales.';
  end if;

  if not exists (select 1 from public.towns where id = p_town_id) then
    raise exception 'Cette ville n''existe pas.';
  end if;

  if private.can_collect_in(v_player, p_town_id) then
    raise exception 'Tu as déjà un comptoir dans cette ville.';
  end if;

  select count(*) into v_count from public.branches where player_id = v_player.player_id;
  if v_count >= (select max_branches from private.settings) + private.bonus(v_player.player_id, 'network') then
    raise exception 'Tu as déjà ouvert toutes tes succursales.';
  end if;

  v_price := private.branch_price(v_count + 1);
  if v_player.coins < v_price then
    raise exception 'Pas assez d''écus.';
  end if;

  update public.players set coins = coins - v_price where player_id = v_player.player_id;
  insert into public.branches (player_id, town_id) values (v_player.player_id, p_town_id);
  return public.get_state();
end;
$$;

create or replace function public.post_offer(
  p_give_good_id text,
  p_give_quantity integer,
  p_want_good_id text,
  p_want_quantity integer,
  p_town_id text default null
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

create or replace function public.cancel_offer(p_offer_id bigint) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player public.players;
  v_offer public.offers;
begin
  v_player := private.lock_player();
  select * into v_offer from public.offers where id = p_offer_id and seller_id = v_player.player_id for update;
  if not found then
    raise exception 'Cette offre n''existe pas.';
  end if;

  if v_offer.status <> 'open' then
    raise exception 'Cette offre est déjà close.';
  end if;

  if v_offer.give_good_id is not null and private.warehouse_free(v_player.player_id, v_offer.town_id) < v_offer.give_quantity then
    raise exception 'Ton entrepôt de % n''a pas la place de reprendre cette marchandise.', (select name from public.towns where id = v_offer.town_id);
  end if;

  perform private.credit(v_offer.seller_id, v_offer.town_id, v_offer.give_good_id, v_offer.give_quantity);
  update public.offers set status = 'cancelled', closed_at = now() where id = v_offer.id;
  return public.get_state();
end;
$$;

create or replace function public.accept_offer(p_offer_id bigint) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player public.players;
  v_offer public.offers;
  v_town text;
begin
  v_player := private.lock_player();
  select * into v_offer from public.offers where id = p_offer_id for update;
  if not found or v_offer.status <> 'open' or v_offer.expires_at <= now() then
    raise exception 'Cette offre n''est plus disponible.';
  end if;

  if v_offer.seller_id = v_player.player_id then
    raise exception 'C''est ta propre offre : retire-la si tu n''en veux plus.';
  end if;

  v_town := private.acting_town(v_player, v_offer.town_id);
  if v_offer.want_good_id is null then
    if v_player.coins < v_offer.want_quantity then
      raise exception 'Pas assez d''écus.';
    end if;

    update public.players set coins = coins - v_offer.want_quantity where player_id = v_player.player_id;
  else
    if private.held(v_player, v_town, v_offer.want_good_id) < v_offer.want_quantity then
      raise exception 'Il te manque de la marchandise pour conclure cet échange.';
    end if;

    perform private.change_holdings(v_player, v_town, v_offer.want_good_id, -v_offer.want_quantity);
  end if;

  if v_offer.give_good_id is not null and private.warehouse_free(v_player.player_id, v_town) < v_offer.give_quantity then
    raise exception 'Ton entrepôt de cette ville n''a pas la place.';
  end if;

  perform private.credit(v_player.player_id, v_town, v_offer.give_good_id, v_offer.give_quantity);
  perform private.credit(v_offer.seller_id, v_town, v_offer.want_good_id, v_offer.want_quantity);
  update public.offers
  set status = 'concluded', buyer_id = v_player.player_id, closed_at = now(), seller_seen = false
  where id = v_offer.id;
  if private.craft_family(v_player.craft_id) = 'negociant' then
    perform private.gain_trade_xp(v_player.player_id, private.offer_value(v_offer));
  end if;

  if private.craft_family((select craft_id from public.players where player_id = v_offer.seller_id)) = 'negociant' then
    perform private.gain_trade_xp(v_offer.seller_id, private.offer_value(v_offer));
  end if;

  return public.get_state();
end;
$$;

revoke all on function
  public.deposit_goods(text, integer),
  public.withdraw_goods(text, integer),
  public.open_branch(text),
  public.post_offer(text, integer, text, integer, text),
  public.cancel_offer(bigint),
  public.accept_offer(bigint)
from public, anon;
grant execute on function
  public.deposit_goods(text, integer),
  public.withdraw_goods(text, integer),
  public.open_branch(text),
  public.post_offer(text, integer, text, integer, text),
  public.cancel_offer(bigint),
  public.accept_offer(bigint)
to authenticated;
