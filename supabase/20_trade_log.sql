-- Journal des achats et des ventes : chaque achat ou vente contre des écus (marché, comptoir,
-- commandes spéciales, chefs-d'œuvre) est noté avec son prix. Les achats répétés de la même
-- marchandise au même endroit en moins de deux minutes sont regroupés en une ligne.
-- À exécuter après 19_special_order_pricing.sql.

alter table private.settings add column if not exists trade_log_merge_seconds integer not null default 120 check (trade_log_merge_seconds >= 0);
alter table private.settings add column if not exists trade_log_days integer not null default 14 check (trade_log_days > 0);

create table if not exists public.trade_log (
  id bigint generated always as identity primary key,
  player_id uuid not null references public.players (player_id) on delete cascade,
  town_id text not null references public.towns (id),
  good_id text not null references public.goods (id),
  side text not null check (side in ('buy', 'sell')),
  source text not null check (source in ('marche', 'comptoir', 'commande', 'chef_oeuvre')),
  quantity integer not null check (quantity > 0),
  total bigint not null check (total >= 0),
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);

create index if not exists trade_log_player on public.trade_log (player_id, updated_at desc);

alter table public.trade_log enable row level security;

drop policy if exists "Voir ses achats et ventes" on public.trade_log;
create policy "Voir ses achats et ventes" on public.trade_log
  for select to authenticated
  using ((select auth.uid()) = player_id);

revoke all on table public.trade_log from anon, authenticated;
grant select on table public.trade_log to authenticated;

create or replace function private.log_trade(
  p_player_id uuid,
  p_town_id text,
  p_good_id text,
  p_side text,
  p_quantity integer,
  p_total bigint,
  p_source text
) returns void
language plpgsql
set search_path = ''
as $$
declare
  v_settings private.settings;
  v_id bigint;
begin
  if p_player_id is null or coalesce(p_quantity, 0) < 1 then
    return;
  end if;

  select * into v_settings from private.settings;
  select id into v_id from public.trade_log
  where player_id = p_player_id and town_id = p_town_id and good_id = p_good_id and side = p_side and source = p_source
    and updated_at > now() - make_interval(secs => v_settings.trade_log_merge_seconds)
  order by updated_at desc
  limit 1
  for update;
  if v_id is null then
    insert into public.trade_log (player_id, town_id, good_id, side, source, quantity, total)
    values (p_player_id, p_town_id, p_good_id, p_side, p_source, p_quantity, greatest(p_total, 0));
  else
    update public.trade_log
    set quantity = quantity + p_quantity, total = total + greatest(p_total, 0), updated_at = now()
    where id = v_id;
  end if;

  delete from public.trade_log
  where player_id = p_player_id and updated_at < now() - make_interval(days => v_settings.trade_log_days);
end;
$$;

create or replace function private.trade_log_state(p_player_id uuid) returns jsonb
language sql stable
set search_path = ''
as $$
  select jsonb_build_object('trades', (
    select coalesce(jsonb_agg(listed.entry order by listed.updated_at desc, listed.id desc), '[]'::jsonb)
    from (
      select jsonb_build_object(
        'id', id, 'town_id', town_id, 'good_id', good_id, 'side', side, 'source', source,
        'quantity', quantity, 'total', total, 'created_at', created_at, 'updated_at', updated_at
      ) as entry, updated_at, id
      from public.trade_log
      where player_id = p_player_id
      order by updated_at desc, id desc
      limit 40
    ) listed
  ));
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
  perform private.log_trade(v_player.player_id, v_town, p_good_id, 'buy', v_quantity, v_total, 'marche');

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
  perform private.log_trade(v_player.player_id, v_town, p_good_id, 'sell', v_quantity, v_earned, 'marche');

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
  v_stored integer;
  v_from_warehouse integer;
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
    v_stored := case when private.is_itinerant(v_player.craft_id) then coalesce((
      select quantity from public.warehouses
      where player_id = v_player.player_id and town_id = v_town and good_id = v_offer.want_good_id
    ), 0) else 0 end;
    if v_stored + private.held(v_player, v_town, v_offer.want_good_id) < v_offer.want_quantity then
      raise exception 'Il te manque de la marchandise pour conclure cet échange.';
    end if;

    v_from_warehouse := least(v_offer.want_quantity, v_stored);
    if v_from_warehouse > 0 then
      perform private.change_warehouse(v_player.player_id, v_town, v_offer.want_good_id, -v_from_warehouse);
    end if;

    if v_offer.want_quantity > v_from_warehouse then
      perform private.change_holdings(v_player, v_town, v_offer.want_good_id, v_from_warehouse - v_offer.want_quantity);
    end if;
  end if;

  if v_offer.give_good_id is not null and private.warehouse_free(v_player.player_id, v_town) < v_offer.give_quantity then
    raise exception 'Ton entrepôt de cette ville n''a pas la place.';
  end if;

  perform private.credit(v_player.player_id, v_town, v_offer.give_good_id, v_offer.give_quantity);
  perform private.credit(v_offer.seller_id, v_town, v_offer.want_good_id, v_offer.want_quantity);
  if v_offer.give_good_id is null and v_offer.want_good_id is not null then
    perform private.log_trade(v_player.player_id, v_town, v_offer.want_good_id, 'sell', v_offer.want_quantity, v_offer.give_quantity, 'comptoir');
    perform private.log_trade(v_offer.seller_id, v_town, v_offer.want_good_id, 'buy', v_offer.want_quantity, v_offer.give_quantity, 'comptoir');
  elsif v_offer.give_good_id is not null and v_offer.want_good_id is null then
    perform private.log_trade(v_player.player_id, v_town, v_offer.give_good_id, 'buy', v_offer.give_quantity, v_offer.want_quantity, 'comptoir');
    perform private.log_trade(v_offer.seller_id, v_town, v_offer.give_good_id, 'sell', v_offer.give_quantity, v_offer.want_quantity, 'comptoir');
  end if;
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

create or replace function private.deliver_special_order(p_order public.special_orders) returns void
language plpgsql
set search_path = ''
as $$
begin
  perform private.change_warehouse(p_order.player_id, p_order.town_id, p_order.good_id, -p_order.quantity);
  update public.players set coins = coins + p_order.quantity * p_order.unit_price where player_id = p_order.player_id;
  update public.special_orders set status = 'delivered', closed_at = now() where id = p_order.id;
  perform private.log_trade(p_order.player_id, p_order.town_id, p_order.good_id, 'sell', p_order.quantity, p_order.quantity * p_order.unit_price, 'commande');
  perform private.gain_reputation(p_order.player_id, p_order.town_id, (p_order.quantity * p_order.unit_price)::numeric / (select reputation_delivery_value from private.settings));
  perform private.log_event(p_order.player_id, 'commande', now(), 'good', format('Commande livrée : %s %s pour %s écus.',
    p_order.quantity, lower((select name from public.goods where id = p_order.good_id)), p_order.quantity * p_order.unit_price));
end;
$$;

create or replace function public.sell_masterpiece(p_masterpiece_id bigint) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player public.players;
  v_piece public.masterpieces;
  v_price integer;
begin
  v_player := private.lock_player();
  select * into v_piece from public.masterpieces
  where id = p_masterpiece_id and owner_id = v_player.player_id and sold_at is null
  for update;
  if not found then
    raise exception 'Ce chef-d''œuvre n''est plus à toi.';
  end if;

  perform private.acting_town(v_player, v_piece.town_id);
  v_price := private.masterpiece_price(v_piece.good_id, v_piece.town_id);
  update public.players set coins = coins + v_price
  where player_id = v_player.player_id;
  perform private.log_trade(v_player.player_id, v_piece.town_id, v_piece.good_id, 'sell', 1, v_price, 'chef_oeuvre');
  update public.masterpieces set sold_at = now() where id = v_piece.id;
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
      || private.reputation_state(null) || private.odd_jobs_state(null) || private.supply_requests(null) || private.trade_log_state(null);
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
    || private.mastery_state(v_player.player_id) || private.reputation_state(v_player.player_id)
    || private.odd_jobs_state(v_player.player_id) || private.supply_requests(v_player.player_id)
    || private.trade_log_state(v_player.player_id);
end;
$$;

revoke all on all functions in schema private from public, anon, authenticated;
revoke all on function
  public.get_state(),
  public.buy_goods(text, integer, text),
  public.sell_goods(text, integer, text),
  public.accept_offer(bigint),
  public.sell_masterpiece(bigint)
from public, anon;
grant execute on function
  public.get_state(),
  public.buy_goods(text, integer, text),
  public.sell_goods(text, integer, text),
  public.accept_offer(bigint),
  public.sell_masterpiece(bigint)
to authenticated;

notify pgrst, 'reload schema';