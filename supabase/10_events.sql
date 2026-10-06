create or replace function private.order_choice(p_player_id uuid, p_kind_id text) returns text
language sql stable
set search_path = ''
as $$
  select coalesce(
    (select choice_id from public.standing_orders where player_id = p_player_id and kind_id = p_kind_id),
    (select id from public.event_choices where kind_id = p_kind_id and is_default)
  );
$$;

create or replace function private.log_event(p_player_id uuid, p_kind_id text, p_at timestamptz, p_tone text, p_detail text) returns void
language sql
set search_path = ''
as $$
  insert into public.event_log (player_id, kind_id, happened_at, title, detail, tone)
  select p_player_id, p_kind_id, p_at, name, p_detail, p_tone from public.event_kinds where id = p_kind_id;
$$;

create or replace function private.delay_text(p_minutes integer) returns text
language sql immutable
set search_path = ''
as $$
  select case when p_minutes > 0 then format(' Retard : %s min.', p_minutes) else '' end;
$$;

drop function if exists private.trip_delay(uuid, numeric);

create or replace function private.trip_delay(p_player_id uuid, p_base_seconds numeric, p_share numeric) returns integer
language plpgsql
set search_path = ''
as $$
declare
  v_seconds numeric := ceil(p_base_seconds * p_share * greatest(1 - private.bonus(p_player_id, 'endurance'), 0));
begin
  update public.caravans set arrives_at = arrives_at + make_interval(secs => v_seconds) where player_id = p_player_id;
  return ceil(v_seconds / 60.0)::integer;
end;
$$;

create or replace function private.lose_cargo(p_player_id uuid, p_most_valuable boolean, p_share numeric) returns text
language plpgsql
set search_path = ''
as $$
declare
  v_good_id text;
  v_name text;
  v_held integer;
  v_lost integer;
begin
  select goods.id, goods.name, cargo.quantity into v_good_id, v_name, v_held
  from public.cargo
  join public.goods on goods.id = cargo.good_id
  where cargo.player_id = p_player_id
  order by case when p_most_valuable then cargo.quantity * goods.base_price else cargo.quantity end desc, goods.sort_order
  limit 1;
  if v_good_id is null then
    return null;
  end if;

  v_lost := ceil(v_held * p_share);
  perform private.change_cargo(p_player_id, v_good_id, -v_lost);
  return format('%s %s', v_lost, lower(v_name));
end;
$$;

create or replace function private.find_goods(p_player_id uuid, p_town text) returns text
language plpgsql
set search_path = ''
as $$
declare
  v_good public.goods;
  v_quantity integer;
begin
  select * into v_good from public.goods where not crafted order by random() limit 1;
  v_quantity := least(
    3 + floor(random() * 6)::integer,
    private.caravan_capacity(p_player_id) - private.cargo_load(p_player_id)
  );
  if v_quantity < 1 then
    return format('Une charrette abandonnée sur la route de %s, mais ta cale est pleine.', p_town);
  end if;

  perform private.change_cargo(p_player_id, v_good.id, v_quantity);
  return format('Une charrette abandonnée sur la route de %s : tu récupères %s %s.', p_town, v_quantity, lower(v_good.name));
end;
$$;

create or replace function private.roll_trip_event(
  p_player_id uuid,
  p_departed_at timestamptz,
  p_arrives_at timestamptz,
  p_biome text,
  p_minutes integer
) returns void
language plpgsql
set search_path = ''
as $$
declare
  v_settings private.settings;
  v_kind text;
begin
  delete from public.pending_events
  where player_id = p_player_id and kind_id in (select id from public.event_kinds where scope = 'road');

  select * into v_settings from private.settings;
  if not v_settings.events_enabled
    or random() >= (1 - exp(-p_minutes::numeric / v_settings.road_event_minutes)) * greatest(1 - private.bonus(p_player_id, 'scout'), 0) then
    return;
  end if;

  select kind_id into v_kind from public.road_event_odds
  where biome = p_biome and weight > 0
  order by -ln(1 - random()) / weight
  limit 1;
  if v_kind is null then
    return;
  end if;

  insert into public.pending_events (player_id, kind_id, occurs_at)
  values (p_player_id, v_kind, p_departed_at + (p_arrives_at - p_departed_at) * (0.2 + 0.6 * random()));
end;
$$;

create or replace function private.directive_choice(p_player_id uuid, p_kind_id text) returns text
language sql stable
set search_path = ''
as $$
  select coalesce(
    (select directive_choices.choice_id from public.caravans
      join public.directive_choices on directive_choices.directive_id = caravans.directive_id and directive_choices.kind_id = p_kind_id
      where caravans.player_id = p_player_id),
    (select id from public.event_choices where kind_id = p_kind_id and is_default)
  );
$$;

create or replace function private.apply_road_event(
  p_player_id uuid,
  p_kind_id text,
  p_choice_id text,
  p_base_seconds numeric,
  p_at timestamptz
) returns void
language plpgsql
set search_path = ''
as $$
declare
  v_caravan public.caravans;
  v_coins bigint;
  v_town text;
  v_amount integer;
  v_lost text;
  v_detail text;
  v_tone text := 'bad';
begin
  select * into v_caravan from public.caravans where player_id = p_player_id for update;
  select coins into v_coins from public.players where player_id = p_player_id for update;
  v_town := (select name from public.towns where id = v_caravan.town_id);

  if p_kind_id = 'bandits' and p_choice_id = 'fuir' then
    v_lost := private.lose_cargo(p_player_id, true, 0.25 * greatest(1 - private.bonus(p_player_id, 'strongbox'), 0));
    v_detail := format('Tu échappes aux bandits sur la route de %s', v_town)
      || coalesce(' en abandonnant ' || v_lost, '') || '.'
      || private.delay_text(private.trip_delay(p_player_id, p_base_seconds, 0.1));
  elsif p_kind_id = 'bandits' then
    v_amount := floor(least(floor(v_coins * 0.08), 150) * greatest(1 - private.bonus(p_player_id, 'strongbox'), 0))::integer;
    update public.players set coins = coins - v_amount where player_id = p_player_id;
    v_detail := case
      when v_amount = 0 then format('Des bandits barrent la route de %s, mais ta bourse est vide : ils te laissent filer.', v_town)
      else format('Des bandits barrent la route de %s : tu paies %s écus pour passer.', v_town, v_amount)
    end;
  elsif p_kind_id = 'orage' and p_choice_id = 'forcer' then
    v_lost := case when private.bonus(p_player_id, 'weatherproof') > 0 then null else private.lose_cargo(p_player_id, false, 0.1) end;
    v_detail := format('Tu traverses l''orage vers %s', v_town)
      || coalesce(', mais ' || v_lost || ' prend l''eau', ' sans dommage') || '.'
      || private.delay_text(private.trip_delay(p_player_id, p_base_seconds, 0.05));
  elsif p_kind_id = 'orage' then
    v_detail := format('Un orage sur la route de %s : tu t''abrites en attendant l''éclaircie.', v_town)
      || private.delay_text(private.trip_delay(p_player_id, p_base_seconds,
        case when private.bonus(p_player_id, 'weatherproof') > 0 then 0.15 else 0.3 end));
  elsif p_kind_id = 'peage' then
    v_amount := 3 * v_caravan.wagons + private.cargo_load(p_player_id) / 10;
    if p_choice_id = 'payer' and v_coins >= v_amount then
      update public.players set coins = coins - v_amount where player_id = p_player_id;
      v_detail := format('Péage sur la route de %s : %s écus.', v_town, v_amount);
      v_tone := 'neutral';
    else
      v_detail := format('Tu contournes le péage sur la route de %s.', v_town)
        || private.delay_text(private.trip_delay(p_player_id, p_base_seconds, 0.35));
    end if;
  else
    v_detail := private.find_goods(p_player_id, v_town);
    v_tone := 'good';
  end if;

  perform private.log_event(p_player_id, p_kind_id, p_at, v_tone, v_detail);
end;
$$;

create or replace function private.resolve_trip_event(p_event public.trip_events, p_choice_id text, p_decided_at timestamptz) returns void
language plpgsql
set search_path = ''
as $$
begin
  delete from public.trip_events where player_id = p_event.player_id;
  if p_decided_at < p_event.decide_by then
    update public.caravans set arrives_at = arrives_at - (p_event.decide_by - p_decided_at) where player_id = p_event.player_id;
  end if;

  perform private.apply_road_event(p_event.player_id, p_event.kind_id, p_choice_id, p_event.planned_seconds, p_decided_at);
end;
$$;

create or replace function private.settle_trip(p_player_id uuid) returns void
language plpgsql
set search_path = ''
as $$
declare
  v_event public.pending_events;
  v_caravan public.caravans;
  v_open public.trip_events;
  v_window interval;
begin
  perform 1 from public.caravans where player_id = p_player_id for update;
  select pending_events.* into v_event from public.pending_events
  join public.event_kinds on event_kinds.id = pending_events.kind_id
  where pending_events.player_id = p_player_id and event_kinds.scope = 'road' and pending_events.occurs_at <= now()
  limit 1
  for update of pending_events;

  if v_event.player_id is not null then
    delete from public.pending_events where player_id = p_player_id and kind_id = v_event.kind_id;
    select * into v_caravan from public.caravans where player_id = p_player_id for update;
    if v_caravan.departed_at is null then
      null;
    elsif not exists (select 1 from public.event_choices where kind_id = v_event.kind_id) then
      perform private.apply_road_event(p_player_id, v_event.kind_id, null,
        extract(epoch from (v_caravan.arrives_at - v_caravan.departed_at)), v_event.occurs_at);
    else
      v_window := make_interval(secs => (select decision_minutes * 60 * travel_time_factor from private.settings));
      insert into public.trip_events (player_id, kind_id, occurred_at, decide_by, planned_seconds)
      values (p_player_id, v_event.kind_id, v_event.occurs_at, v_event.occurs_at + v_window,
        extract(epoch from (v_caravan.arrives_at - v_caravan.departed_at)))
      on conflict (player_id) do nothing;
      update public.caravans set arrives_at = arrives_at + v_window where player_id = p_player_id;
    end if;
  end if;

  select * into v_open from public.trip_events where player_id = p_player_id and decide_by <= now() for update;
  if v_open.player_id is not null then
    perform private.resolve_trip_event(v_open, private.directive_choice(p_player_id, v_open.kind_id), v_open.decide_by);
  end if;
end;
$$;

create or replace function private.roll_breakdown(p_player_id uuid, p_from timestamptz, p_seconds numeric) returns void
language plpgsql
set search_path = ''
as $$
declare
  v_settings private.settings;
begin
  select * into v_settings from private.settings;
  if not v_settings.events_enabled
    or exists (select 1 from public.pending_events where player_id = p_player_id and kind_id = 'panne')
    or random() >= (1 - exp(-p_seconds / v_settings.craft_time_factor / 3600 / v_settings.breakdown_hours))
      * greatest(1 - private.bonus(p_player_id, 'sturdy'), 0) then
    return;
  end if;

  insert into public.pending_events (player_id, kind_id, occurs_at)
  values (p_player_id, 'panne', p_from + make_interval(secs => p_seconds * (0.1 + 0.8 * random())));
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
    v_detail || format(' L''atelier s''arrête %s min.', ceil(v_minutes)));
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
  v_quantity := least(greatest(round(v_settings.special_order_value / v_market_price), 1), 20)::integer;
  v_price := greatest(floor(v_buy_quote * v_settings.special_order_price_ratio), 1)::integer;
  insert into public.special_orders (player_id, town_id, good_id, quantity, unit_price, deadline)
  values (p_player.player_id, p_player.home_town_id, v_good.id, v_quantity, v_price,
    now() + make_interval(secs => v_settings.special_order_window_hours * 3600 * v_settings.craft_time_factor));
  perform private.log_event(p_player.player_id, 'commande', now(), 'neutral', format(
    'Un client de %s veut %s %s à %s écus pièce, sous %s h.',
    (select name from public.towns where id = p_player.home_town_id), v_quantity, lower(v_good.name), v_price,
    replace(trim_scale(round(v_settings.special_order_window_hours * v_settings.craft_time_factor, 1))::text, '.', ',')));
end;
$$;

create or replace function private.settle_events(p_player_id uuid) returns void
language plpgsql
set search_path = ''
as $$
declare
  v_player public.players;
  v_order public.special_orders;
  v_limit timestamptz;
begin
  delete from public.event_log where player_id = p_player_id and happened_at < now() - interval '7 days';
  select * into v_player from public.players where player_id = p_player_id for update;
  if not found or private.is_itinerant(v_player.craft_id) then
    return;
  end if;

  select * into v_order from public.special_orders where player_id = p_player_id and status = 'open' for update;
  if v_order.id is not null then
    v_limit := least(now(), v_order.deadline);
    if exists (select 1 from public.pending_events where player_id = p_player_id and kind_id = 'panne' and occurs_at <= v_limit) then
      perform private.settle_breakdown(p_player_id);
    end if;

    perform private.produce_until(p_player_id, v_limit);
    if private.order_choice(p_player_id, 'commande') = 'livrer'
      and coalesce((select quantity from public.warehouses
        where player_id = p_player_id and town_id = v_order.town_id and good_id = v_order.good_id), 0) >= v_order.quantity then
      perform private.deliver_special_order(v_order);
    elsif v_order.deadline <= now() then
      update public.special_orders set status = 'expired', closed_at = deadline where id = v_order.id;
      perform private.log_event(p_player_id, 'commande', v_order.deadline, 'bad', format('La commande de %s %s n''a pas été livrée à temps.',
        v_order.quantity, lower((select name from public.goods where id = v_order.good_id))));
    end if;
  end if;

  perform private.roll_special_order(v_player);
end;
$$;

create or replace function private.events_state(p_player_id uuid) returns jsonb
language sql stable
set search_path = ''
as $$
  select jsonb_build_object(
    'orders', (
      select coalesce(jsonb_agg(jsonb_build_object('kind_id', kinds.id, 'choice_id', private.order_choice(p_player_id, kinds.id))
        order by kinds.sort_order), '[]'::jsonb)
      from public.event_kinds kinds
      where kinds.scope = 'workshop' and exists (select 1 from public.event_choices where kind_id = kinds.id)
    ),
    'journal', (
      select coalesce(jsonb_agg(listed.entry order by listed.happened_at desc, listed.id desc), '[]'::jsonb)
      from (
        select jsonb_build_object(
          'id', id, 'kind_id', kind_id, 'happened_at', happened_at, 'title', title, 'detail', detail, 'tone', tone, 'seen', seen
        ) as entry, happened_at, id
        from public.event_log
        where player_id = p_player_id
        order by happened_at desc, id desc
        limit 20
      ) listed
    ),
    'special_orders', (
      select coalesce(jsonb_agg(jsonb_build_object(
        'id', id, 'town_id', town_id, 'good_id', good_id, 'quantity', quantity, 'unit_price', unit_price,
        'created_at', created_at, 'deadline', deadline, 'status', status, 'closed_at', closed_at
      ) order by created_at desc, id desc), '[]'::jsonb)
      from public.special_orders
      where player_id = p_player_id and (status = 'open' or closed_at > now() - interval '24 hours')
    ),
    'journal_unseen', (select count(*) from public.event_log where player_id = p_player_id and not seen)
  );
$$;

revoke all on all functions in schema private from public, anon, authenticated;

create or replace function public.set_standing_order(p_kind_id text, p_choice_id text) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player public.players;
begin
  v_player := private.lock_player();
  if (select scope from public.event_kinds where id = p_kind_id) = 'road' then
    raise exception 'En route, c''est la directive du voyage qui décide : choisis-la au départ.';
  end if;

  if not exists (select 1 from public.event_choices where kind_id = p_kind_id and id = p_choice_id) then
    raise exception 'Cette consigne n''existe pas.';
  end if;

  insert into public.standing_orders (player_id, kind_id, choice_id) values (v_player.player_id, p_kind_id, p_choice_id)
  on conflict (player_id, kind_id) do update set choice_id = excluded.choice_id;
  return public.get_state();
end;
$$;

create or replace function public.fulfill_special_order(p_order_id bigint) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player public.players;
  v_order public.special_orders;
begin
  v_player := private.lock_player();
  if not private.is_itinerant(v_player.craft_id) then
    perform private.lock_workshop();
  end if;

  select * into v_order from public.special_orders where id = p_order_id and player_id = v_player.player_id for update;
  if not found then
    raise exception 'Cette commande n''existe pas.';
  end if;

  if v_order.status <> 'open' then
    raise exception 'Cette commande est déjà close.';
  end if;

  if coalesce((select quantity from public.warehouses
      where player_id = v_player.player_id and town_id = v_order.town_id and good_id = v_order.good_id), 0) < v_order.quantity then
    raise exception 'Il te faut % % à l''entrepôt pour livrer cette commande.',
      v_order.quantity, lower((select name from public.goods where id = v_order.good_id));
  end if;

  perform private.deliver_special_order(v_order);
  return public.get_state();
end;
$$;

create or replace function public.decline_special_order(p_order_id bigint) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player public.players;
begin
  v_player := private.lock_player();
  update public.special_orders set status = 'declined', closed_at = now()
  where id = p_order_id and player_id = v_player.player_id and status = 'open';
  if not found then
    raise exception 'Cette commande n''est plus ouverte.';
  end if;

  return public.get_state();
end;
$$;

create or replace function public.answer_event(p_choice_id text) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player public.players;
  v_open public.trip_events;
begin
  v_player := private.lock_player();
  select * into v_open from public.trip_events where player_id = v_player.player_id for update;
  if not found then
    raise exception 'Aucun événement n''attend ta réponse.';
  end if;

  if not exists (select 1 from public.event_choices where kind_id = v_open.kind_id and id = p_choice_id) then
    raise exception 'Cette réponse ne convient pas à cet événement.';
  end if;

  perform private.resolve_trip_event(v_open, p_choice_id, now());
  return public.get_state();
end;
$$;

create or replace function public.mark_journal_seen() returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
begin
  if auth.uid() is null then
    raise exception 'Connexion requise.';
  end if;

  update public.event_log set seen = true where player_id = auth.uid() and not seen;
  return public.get_state();
end;
$$;

revoke all on function
  public.set_standing_order(text, text),
  public.fulfill_special_order(bigint),
  public.decline_special_order(bigint),
  public.answer_event(text),
  public.mark_journal_seen()
from public, anon;
grant execute on function
  public.set_standing_order(text, text),
  public.fulfill_special_order(bigint),
  public.decline_special_order(bigint),
  public.answer_event(text),
  public.mark_journal_seen()
to authenticated;

notify pgrst, 'reload schema';
