-- Approvisionnement : une offre « je donne des écus contre une marchandise » est une commande
-- que tout caravanier peut voir depuis n'importe quelle ville, puis livrer dans la ville où elle est publiée.
-- Livrer puise d'abord dans l'entrepôt de la ville, puis dans la cale.
-- À exécuter après 17_odd_jobs.sql.

create or replace function private.supply_requests(p_player_id uuid) returns jsonb
language sql stable
set search_path = ''
as $$
  select jsonb_build_object('supply_requests', (
    select coalesce(jsonb_agg(listed.entry order by listed.expires_at, listed.id), '[]'::jsonb)
    from (
      select private.offer_json(offers) || jsonb_build_object('seller', sellers.name) as entry, offers.expires_at, offers.id
      from public.offers
      join public.players sellers on sellers.player_id = offers.seller_id
      where offers.status = 'open'
        and offers.expires_at > now()
        and offers.give_good_id is null
        and offers.want_good_id is not null
        and offers.seller_id <> p_player_id
      order by offers.expires_at, offers.id
      limit 50
    ) listed
  ));
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
      || private.reputation_state(null) || private.odd_jobs_state(null) || private.supply_requests(null);
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
    || private.odd_jobs_state(v_player.player_id) || private.supply_requests(v_player.player_id);
end;
$$;

revoke all on function public.get_state(), public.accept_offer(bigint) from public, anon;
grant execute on function public.get_state(), public.accept_offer(bigint) to authenticated;

notify pgrst, 'reload schema';