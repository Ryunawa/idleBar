create table if not exists public.reputation_tiers (
  level smallint primary key check (level >= 0),
  id text not null unique,
  name text not null,
  points integer not null check (points >= 0),
  price_rate numeric not null default 0 check (price_rate between 0 and 0.5),
  storage_bonus integer not null default 0 check (storage_bonus >= 0),
  freight_discount numeric not null default 0 check (freight_discount between 0 and 1),
  order_bonus numeric not null default 0 check (order_bonus >= 0),
  description text not null
);

insert into public.reputation_tiers (level, id, name, points, price_rate, storage_bonus, freight_discount, order_bonus, description) values
  (0, 'inconnu', 'Inconnu', 0, 0, 0, 0, 0, 'Personne ne te connaît encore ici.'),
  (1, 'connu', 'Connu', 200, 0.01, 0, 0, 0, 'Achat 1 % moins cher et vente 1 % plus chère au marché.'),
  (2, 'estime', 'Estimé', 800, 0.01, 20, 0, 0, 'Prix à 1 % et 20 places de plus à l''entrepôt.'),
  (3, 'notable', 'Notable', 3000, 0.02, 20, 0.25, 0, 'Prix à 2 %, 20 places de plus et transporteur du jeu 25 % moins cher depuis ou vers la ville.'),
  (4, 'bourgeois', 'Bourgeois', 10000, 0.03, 20, 0.25, 0.5, 'Prix à 3 %, transporteur 25 % moins cher et commandes spéciales 50 % plus grosses.'),
  (5, 'patricien', 'Patricien', 30000, 0.04, 80, 0.25, 0.5, 'Prix à 4 %, 80 places de plus, transporteur 25 % moins cher et commandes 50 % plus grosses.')
on conflict (level) do update set
  id = excluded.id,
  name = excluded.name,
  points = excluded.points,
  price_rate = excluded.price_rate,
  storage_bonus = excluded.storage_bonus,
  freight_discount = excluded.freight_discount,
  order_bonus = excluded.order_bonus,
  description = excluded.description;

create table if not exists public.reputations (
  player_id uuid not null references public.players (player_id) on delete cascade,
  town_id text not null references public.towns (id),
  points numeric not null default 0 check (points >= 0),
  primary key (player_id, town_id)
);

alter table public.reputation_tiers enable row level security;
alter table public.reputations enable row level security;

drop policy if exists "Lire les paliers de réputation" on public.reputation_tiers;
create policy "Lire les paliers de réputation" on public.reputation_tiers for select to authenticated using (true);

drop policy if exists "Voir sa réputation" on public.reputations;
create policy "Voir sa réputation" on public.reputations
  for select to authenticated
  using ((select auth.uid()) = player_id);

revoke all on table public.reputation_tiers, public.reputations from anon, authenticated;
grant select on table public.reputation_tiers, public.reputations to authenticated;

alter table private.settings add column if not exists reputation_trade_value integer not null default 20 check (reputation_trade_value > 0);
alter table private.settings add column if not exists reputation_delivery_value integer not null default 10 check (reputation_delivery_value > 0);

insert into public.event_kinds (id, scope, name, description, sort_order) values
  ('reputation', 'mastery', 'Réputation', 'Ta réputation grandit dans une ville.', 9)
on conflict (id) do update set scope = excluded.scope, name = excluded.name, description = excluded.description, sort_order = excluded.sort_order;

create or replace function private.reputation_tier(p_player_id uuid, p_town_id text) returns public.reputation_tiers
language sql stable
set search_path = ''
as $$
  select tiers.* from public.reputation_tiers tiers
  where tiers.points <= coalesce((select points from public.reputations where player_id = p_player_id and town_id = p_town_id), 0)
  order by tiers.level desc
  limit 1;
$$;

create or replace function private.town_rate(p_player_id uuid, p_town_id text) returns numeric
language sql stable
set search_path = ''
as $$
  select coalesce((private.reputation_tier(p_player_id, p_town_id)).price_rate, 0);
$$;

create or replace function private.gain_reputation(p_player_id uuid, p_town_id text, p_points numeric) returns void
language plpgsql
set search_path = ''
as $$
declare
  v_before public.reputation_tiers;
  v_after public.reputation_tiers;
begin
  if p_player_id is null or p_town_id is null or coalesce(p_points, 0) <= 0
    or not exists (select 1 from public.players where player_id = p_player_id) then
    return;
  end if;

  v_before := private.reputation_tier(p_player_id, p_town_id);
  insert into public.reputations (player_id, town_id, points) values (p_player_id, p_town_id, p_points)
  on conflict (player_id, town_id) do update set points = public.reputations.points + excluded.points;
  v_after := private.reputation_tier(p_player_id, p_town_id);
  if v_after.level > v_before.level then
    perform private.log_event(p_player_id, 'reputation', now(), 'good', format('Tu es désormais %s à %s. %s',
      lower(v_after.name), (select name from public.towns where id = p_town_id), v_after.description));
  end if;
end;
$$;

create or replace function private.reputation_state(p_player_id uuid) returns jsonb
language sql stable
set search_path = ''
as $$
  select jsonb_build_object('reputation', jsonb_build_object(
    'tiers', (
      select coalesce(jsonb_agg(jsonb_build_object(
        'level', level, 'id', id, 'name', name, 'points', points, 'price_rate', price_rate,
        'storage_bonus', storage_bonus, 'freight_discount', freight_discount, 'order_bonus', order_bonus,
        'description', description
      ) order by level), '[]'::jsonb)
      from public.reputation_tiers
    ),
    'towns', (
      select coalesce(jsonb_agg(jsonb_build_object('town_id', town_id, 'points', floor(points)::integer) order by town_id), '[]'::jsonb)
      from public.reputations
      where player_id = p_player_id
    )
  ));
$$;

create or replace function private.warehouse_free(p_player_id uuid, p_town_id text) returns integer
language sql stable
set search_path = ''
as $$
  select private.warehouse_capacity(p_player_id)
    + coalesce((private.reputation_tier(p_player_id, p_town_id)).storage_bonus, 0)
    - private.warehouse_load(p_player_id, p_town_id);
$$;

create or replace function private.duration_text(p_minutes numeric) returns text
language sql immutable
set search_path = ''
as $$
  select case
    when p_minutes < 1 then 'quelques secondes'
    when p_minutes < 60 then ceil(p_minutes)::integer || ' min'
    when ceil(p_minutes)::integer % 60 = 0 then (ceil(p_minutes)::integer / 60) || ' h'
    else (ceil(p_minutes)::integer / 60) || ' h ' || lpad((ceil(p_minutes)::integer % 60)::text, 2, '0')
  end;
$$;

create or replace function private.delay_text(p_minutes integer) returns text
language sql immutable
set search_path = ''
as $$
  select case when p_minutes > 0 then format(' Retard : %s.', private.duration_text(p_minutes)) else '' end;
$$;

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
  v_discount := 1 - private.bonus(v_player.player_id, 'haggle') - private.town_rate(v_player.player_id, v_town);
  while v_quantity > 0 and ceil(private.buy_total(v_market.base_price, v_pressure, v_quantity) * v_discount) > v_player.coins loop
    v_quantity := v_quantity - 1;
  end loop;
  if v_quantity < 1 then
    raise exception 'Pas assez d''écus.';
  end if;

  v_total := ceil(private.buy_total(v_market.base_price, v_pressure, v_quantity) * v_discount);
  update public.players set coins = coins - v_total where player_id = v_player.player_id;
  perform private.gain_reputation(v_player.player_id, v_town, v_total::numeric / (select reputation_trade_value from private.settings));
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
  v_earned := floor(private.sell_total(v_market.base_price, v_pressure, v_quantity) * (1 + private.bonus(v_player.player_id, 'haggle') + private.town_rate(v_player.player_id, v_town)));
  update public.players
  set coins = coins + v_earned
  where player_id = v_player.player_id;
  perform private.gain_reputation(v_player.player_id, v_town, v_earned::numeric / (select reputation_trade_value from private.settings));
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
  perform private.gain_reputation(v_player.player_id, v_town, private.offer_value(v_offer)::numeric / (select reputation_trade_value from private.settings));
  perform private.gain_reputation(v_offer.seller_id, v_town, private.offer_value(v_offer)::numeric / (select reputation_trade_value from private.settings));
  if private.craft_family(v_player.craft_id) = 'negociant' then
    perform private.gain_trade_xp(v_player.player_id, private.offer_value(v_offer));
  end if;

  if private.craft_family((select craft_id from public.players where player_id = v_offer.seller_id)) = 'negociant' then
    perform private.gain_trade_xp(v_offer.seller_id, private.offer_value(v_offer));
  end if;

  return public.get_state();
end;
$$;

create or replace function private.reward_delivery(p_contract public.transport_contracts) returns void
language plpgsql
set search_path = ''
as $$
declare
  v_minutes integer := private.journey_minutes(p_contract.origin_town_id, p_contract.destination_town_id);
  v_share numeric := least(1, p_contract.deposit::numeric / (select contract_xp_value from private.settings));
begin
  perform private.gain_xp(p_contract.carrier_id, round(v_minutes * v_share)::integer);
  perform private.gain_reputation(p_contract.carrier_id, p_contract.destination_town_id, p_contract.deposit::numeric / (select reputation_delivery_value from private.settings));
  perform private.gain_reputation(p_contract.shipper_id, p_contract.origin_town_id, p_contract.deposit::numeric / (select reputation_delivery_value from private.settings));
  if private.craft_family((select craft_id from public.players where player_id = p_contract.shipper_id)) = 'negociant' then
    perform private.gain_trade_xp(p_contract.shipper_id, p_contract.deposit);
  end if;
end;
$$;

create or replace function private.deliver_special_order(p_order public.special_orders) returns void
language plpgsql
set search_path = ''
as $$
begin
  perform private.change_warehouse(p_order.player_id, p_order.town_id, p_order.good_id, -p_order.quantity);
  update public.players set coins = coins + p_order.quantity * p_order.unit_price where player_id = p_order.player_id;
  update public.special_orders set status = 'delivered', closed_at = now() where id = p_order.id;
  perform private.gain_reputation(p_order.player_id, p_order.town_id, (p_order.quantity * p_order.unit_price)::numeric / (select reputation_delivery_value from private.settings));
  perform private.log_event(p_order.player_id, 'commande', now(), 'good', format('Commande livrée : %s %s pour %s écus.',
    p_order.quantity, lower((select name from public.goods where id = p_order.good_id)), p_order.quantity * p_order.unit_price));
end;
$$;

create or replace function private.roll_special_order(p_player public.players) returns void
language plpgsql
set search_path = ''
as $$
declare
  v_settings private.settings;
  v_hours numeric;
  v_good public.goods;
  v_market_price numeric;
  v_buy_quote numeric;
  v_quantity integer;
  v_price integer;
begin
  select * into v_settings from private.settings;
  v_hours := extract(epoch from (now() - p_player.orders_checked_at)) / 3600 / v_settings.craft_time_factor;
  update public.players set orders_checked_at = now() where player_id = p_player.player_id;
  if not v_settings.events_enabled
    or exists (select 1 from public.special_orders where player_id = p_player.player_id and status = 'open')
    or random() >= 1 - exp(-v_hours * (1 + private.bonus(p_player.player_id, 'renown')) / v_settings.special_order_hours) then
    return;
  end if;

  select goods.* into v_good from public.goods
  join public.markets on markets.good_id = goods.id and markets.town_id = p_player.home_town_id
  where goods.id in (select output_good_id from public.recipes where craft_id = p_player.craft_id)
    or (not exists (select 1 from public.recipes where craft_id = p_player.craft_id) and goods.market_sells and markets.base_price >= goods.base_price * 0.8)
  order by random()
  limit 1;
  if v_good.id is null then
    return;
  end if;

  select markets.base_price, private.buy_quote(markets.base_price, private.current_pressure(markets.pressure, markets.pressure_at))
  into v_market_price, v_buy_quote
  from public.markets where town_id = p_player.home_town_id and good_id = v_good.id;
  v_quantity := least(greatest(round(v_settings.special_order_value
    * (1 + coalesce((private.reputation_tier(p_player.player_id, p_player.home_town_id)).order_bonus, 0)) / v_market_price), 1), 30)::integer;
  v_price := greatest(floor(v_buy_quote * v_settings.special_order_price_ratio), 1)::integer;
  insert into public.special_orders (player_id, town_id, good_id, quantity, unit_price, deadline)
  values (p_player.player_id, p_player.home_town_id, v_good.id, v_quantity, v_price,
    now() + make_interval(secs => v_settings.special_order_window_hours * 3600 * v_settings.craft_time_factor));
  perform private.log_event(p_player.player_id, 'commande', now(), 'neutral', format(
    'Un client de %s veut %s %s à %s écus pièce, sous %s.',
    (select name from public.towns where id = p_player.home_town_id), v_quantity, lower(v_good.name), v_price,
    private.duration_text(v_settings.special_order_window_hours * v_settings.craft_time_factor * 60)));
end;
$$;

create or replace function private.settle_breakdown(p_player_id uuid) returns void
language plpgsql
set search_path = ''
as $$
declare
  v_occurs_at timestamptz;
  v_workshop public.workshops;
  v_cost integer;
  v_minutes numeric;
  v_detail text;
begin
  delete from public.pending_events
  where player_id = p_player_id and kind_id = 'panne' and occurs_at <= now()
  returning occurs_at into v_occurs_at;
  if v_occurs_at is null then
    return;
  end if;

  v_workshop := private.produce_until(p_player_id, v_occurs_at);
  if v_workshop.queued = 0 then
    return;
  end if;

  v_cost := 20 * v_workshop.level;
  if private.order_choice(p_player_id, 'panne') = 'reparer'
    and (select coins from public.players where player_id = p_player_id) >= v_cost then
    update public.players set coins = coins - v_cost where player_id = p_player_id;
    v_minutes := 10;
    v_detail := format('Un outil casse : un réparateur le remet en état pour %s écus.', v_cost);
  else
    v_minutes := 60;
    v_detail := 'Un outil casse : tu le répares toi-même.';
  end if;

  v_minutes := v_minutes * (select craft_time_factor from private.settings);
  update public.workshops
  set started_at = started_at + make_interval(secs => v_minutes * 60),
      paused_until = v_occurs_at + make_interval(secs => v_minutes * 60)
  where player_id = p_player_id;
  perform private.log_event(p_player_id, 'panne', v_occurs_at, 'bad',
    v_detail || format(' L''atelier s''arrête %s.', private.duration_text(v_minutes)));
end;
$$;

create or replace function public.post_contract(
  p_good_id text,
  p_quantity integer,
  p_destination_id text,
  p_reward integer,
  p_town_id text default null
) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player public.players;
  v_settings private.settings;
  v_town text;
  v_stored integer;
  v_minutes integer;
  v_fee integer;
  v_escrow integer;
  v_takeover_at timestamptz;
begin
  if p_quantity is null or p_quantity < 1 then
    raise exception 'Quantité invalide.';
  end if;

  if p_reward is null or p_reward < 1 then
    raise exception 'La récompense doit être d''au moins 1 écu.';
  end if;

  v_player := private.lock_player();
  select * into v_settings from private.settings;
  v_town := private.acting_town(v_player, p_town_id);
  if not exists (select 1 from public.towns where id = p_destination_id) then
    raise exception 'Cette ville n''existe pas.';
  end if;

  if p_destination_id = v_town then
    raise exception 'Choisis une destination différente de la ville de départ.';
  end if;

  if not private.can_collect_in(v_player, p_destination_id) then
    raise exception 'Personne ne pourrait recevoir ces marchandises là-bas : il te faut un comptoir dans cette ville.';
  end if;

  if (select count(*) from public.transport_contracts
      where shipper_id = v_player.player_id and status in ('open', 'carried', 'shipped')) >= v_settings.max_open_contracts then
    raise exception 'Tu as déjà % contrats en cours.', v_settings.max_open_contracts;
  end if;

  select quantity into v_stored from public.warehouses
  where player_id = v_player.player_id and town_id = v_town and good_id = p_good_id;
  if coalesce(v_stored, 0) < p_quantity then
    raise exception 'Il n''y en a pas assez dans ton entrepôt de cette ville.';
  end if;

  v_minutes := private.journey_minutes(v_town, p_destination_id);
  if v_minutes is null then
    raise exception 'Aucune route ne mène là.';
  end if;

  v_fee := greatest(ceil(private.freight_fee(p_good_id, p_quantity, v_minutes) * (1 - private.bonus(v_player.player_id, 'logistics'))
    * (1 - greatest(coalesce((private.reputation_tier(v_player.player_id, v_town)).freight_discount, 0),
      coalesce((private.reputation_tier(v_player.player_id, p_destination_id)).freight_discount, 0)))), 1)::integer;
  v_escrow := greatest(p_reward, v_fee);
  if v_player.coins < v_escrow then
    raise exception 'Il faut % écus pour garantir le transport.', v_escrow;
  end if;

  v_takeover_at := now() + make_interval(secs => v_settings.takeover_minutes * 60 * v_settings.travel_time_factor);
  update public.players set coins = coins - v_escrow where player_id = v_player.player_id;
  perform private.change_warehouse(v_player.player_id, v_town, p_good_id, -p_quantity);
  insert into public.transport_contracts (
    shipper_id, origin_town_id, destination_town_id, good_id, quantity,
    reward, game_fee, deposit, takeover_at, game_arrives_at
  ) values (
    v_player.player_id, v_town, p_destination_id, p_good_id, p_quantity,
    p_reward, v_fee, p_quantity * (select base_price from public.goods where id = p_good_id), v_takeover_at,
    v_takeover_at + make_interval(secs => v_minutes * 60 * v_settings.travel_time_factor * v_settings.game_carrier_slowness)
  );
  return public.get_state();
end;
$$;

create or replace function public.get_state() returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player public.players;
  v_caravan public.caravans;
  v_workshop public.workshops;
  v_settings private.settings;
  v_present text[];
  v_branches integer;
  v_haggle numeric;
begin
  if auth.uid() is null then
    raise exception 'Connexion requise.';
  end if;

  perform private.settle_player(auth.uid());
  select * into v_player from public.players where player_id = auth.uid();
  if not found then
    return jsonb_build_object(
      'server_time', now(), 'player', null, 'caravan', null, 'cargo', '[]'::jsonb,
      'workshop', null, 'warehouses', '[]'::jsonb, 'branches', '[]'::jsonb, 'market', '[]'::jsonb, 'trip_event', null
    ) || private.exchange_state(null, array[]::text[]) || private.events_state(null) || private.mastery_state(null)
      || private.reputation_state(null);
  end if;

  if exists (select 1 from public.workshops where player_id = v_player.player_id) then
    v_workshop := private.lock_workshop();
    select * into v_player from public.players where player_id = v_player.player_id;
  end if;

  select * into v_caravan from public.caravans where player_id = v_player.player_id;
  select * into v_settings from private.settings;
  select count(*) into v_branches from public.branches where player_id = v_player.player_id;
  v_present := private.present_towns(v_player.player_id);
  v_haggle := private.bonus(v_player.player_id, 'haggle');

  return jsonb_build_object(
    'server_time', now(),
    'player', jsonb_build_object(
      'name', v_player.name,
      'craft_id', v_player.craft_id,
      'home_town_id', v_player.home_town_id,
      'coins', v_player.coins,
      'next_branch_price', case
        when (select opens_branches from public.crafts where id = v_player.craft_id)
          and v_branches < v_settings.max_branches + private.bonus(v_player.player_id, 'network')
        then private.branch_price(v_branches + 1)
      end
    ),
    'caravan', case when v_caravan.player_id is null then null else jsonb_build_object(
      'wagons', v_caravan.wagons,
      'capacity', private.caravan_capacity(v_caravan.player_id),
      'load', private.cargo_load(v_caravan.player_id),
      'next_wagon_price', case when v_caravan.wagons < v_settings.max_wagons then private.wagon_price(v_caravan.wagons + 1) end,
      'town_id', v_caravan.town_id,
      'from_town_id', v_caravan.from_town_id,
      'departed_at', v_caravan.departed_at,
      'arrives_at', v_caravan.arrives_at,
      'directive_id', v_caravan.directive_id
    ) end,
    'cargo', (
      select coalesce(jsonb_agg(jsonb_build_object('good_id', cargo.good_id, 'quantity', cargo.quantity) order by goods.sort_order), '[]'::jsonb)
      from public.cargo
      join public.goods on goods.id = cargo.good_id
      where cargo.player_id = v_player.player_id
    ),
    'workshop', case when v_workshop.player_id is null then null else jsonb_build_object(
      'town_id', v_workshop.town_id,
      'level', v_workshop.level,
      'speed', private.workshop_speed(v_workshop.level) * (1 + private.bonus(v_player.player_id, 'speed')),
      'storage_capacity', private.warehouse_capacity(v_player.player_id),
      'max_queue', private.max_queue(v_workshop.level),
      'next_level_price', case when v_workshop.level < v_settings.max_workshop_level then private.workshop_price(v_workshop.level + 1) end,
      'recipe_id', v_workshop.recipe_id,
      'queued', v_workshop.queued,
      'batch_seconds', v_workshop.batch_seconds,
      'started_at', v_workshop.started_at,
      'paused_until', v_workshop.paused_until
    ) end,
    'warehouses', (
      select coalesce(jsonb_agg(jsonb_build_object(
        'town_id', warehouses.town_id, 'good_id', warehouses.good_id, 'quantity', warehouses.quantity
      ) order by warehouses.town_id, goods.sort_order), '[]'::jsonb)
      from public.warehouses
      join public.goods on goods.id = warehouses.good_id
      where warehouses.player_id = v_player.player_id
    ),
    'trip_event', (
      select jsonb_build_object(
        'kind_id', kind_id,
        'occurred_at', occurred_at,
        'decide_by', decide_by,
        'directive_choice_id', private.directive_choice(player_id, kind_id)
      )
      from public.trip_events where player_id = v_player.player_id
    ),
    'branches', (
      select coalesce(jsonb_agg(branches.town_id order by branches.opened_at, branches.town_id), '[]'::jsonb)
      from public.branches
      where branches.player_id = v_player.player_id
    ),
    'market', (
      select coalesce(jsonb_agg(jsonb_build_object(
        'town_id', markets.town_id,
        'good_id', markets.good_id,
        'buy_price', round(private.buy_quote(markets.base_price, private.current_pressure(markets.pressure, markets.pressure_at)) * (1 - v_haggle - private.town_rate(v_player.player_id, markets.town_id)), 1),
        'sell_price', round(private.sell_quote(markets.base_price, private.current_pressure(markets.pressure, markets.pressure_at)) * (1 + v_haggle + private.town_rate(v_player.player_id, markets.town_id)), 1)
      ) order by markets.town_id, goods.sort_order), '[]'::jsonb)
      from public.markets
      join public.goods on goods.id = markets.good_id
      where markets.town_id = any (v_present)
    )
  ) || private.exchange_state(v_player.player_id, v_present) || private.events_state(v_player.player_id)
    || private.mastery_state(v_player.player_id) || private.reputation_state(v_player.player_id);
end;
$$;

revoke all on function
  public.get_state(),
  public.buy_goods(text, integer, text),
  public.sell_goods(text, integer, text),
  public.accept_offer(bigint),
  public.post_contract(text, integer, text, integer, text)
from public, anon;
grant execute on function
  public.get_state(),
  public.buy_goods(text, integer, text),
  public.sell_goods(text, integer, text),
  public.accept_offer(bigint),
  public.post_contract(text, integer, text, integer, text)
to authenticated;
