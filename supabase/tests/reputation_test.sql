\set ON_ERROR_STOP on

insert into auth.users (id) values
  ('a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1'),
  ('b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2'),
  ('c3c3c3c3-c3c3-c3c3-c3c3-c3c3c3c3c3c3');

update private.settings set travel_time_factor = 1, craft_time_factor = 1, events_enabled = false;

do $$
begin
  assert private.duration_text(0.2) = 'quelques secondes', 'a few seconds';
  assert private.duration_text(45) = '45 min', 'minutes under an hour';
  assert private.duration_text(180) = '3 h', 'whole hours';
  assert private.duration_text(90) = '1 h 30', 'hours and minutes';
  assert private.delay_text(120) = ' Retard : 2 h.', 'delays read in hours';
end;
$$;

create function pg_temp.as_player(p_player_id text) returns void
language sql
as $$
  select set_config('request.jwt.claim.sub', p_player_id, false);
$$;

create function pg_temp.points(p_state jsonb, p_town_id text) returns integer
language sql
as $$
  select coalesce((
    select (town ->> 'points')::int from jsonb_array_elements(p_state -> 'reputation' -> 'towns') town
    where town ->> 'town_id' = p_town_id
  ), 0);
$$;

create function pg_temp.quote(p_state jsonb, p_town_id text, p_good_id text, p_side text) returns numeric
language sql
as $$
  select (quote ->> p_side)::numeric from jsonb_array_elements(p_state -> 'market') quote
  where quote ->> 'town_id' = p_town_id and quote ->> 'good_id' = p_good_id;
$$;

create function pg_temp.stored(p_state jsonb, p_town_id text, p_good_id text) returns integer
language sql
as $$
  select coalesce((
    select (line ->> 'quantity')::int from jsonb_array_elements(p_state -> 'warehouses') line
    where line ->> 'town_id' = p_town_id and line ->> 'good_id' = p_good_id
  ), 0);
$$;

create function pg_temp.coins(p_state jsonb) returns bigint
language sql
as $$
  select (p_state -> 'player' ->> 'coins')::bigint;
$$;

create function pg_temp.set_points(p_player_id uuid, p_town_id text, p_points numeric) returns void
language sql
as $$
  insert into public.reputations (player_id, town_id, points) values (p_player_id, p_town_id, p_points)
  on conflict (player_id, town_id) do update set points = excluded.points;
$$;

grant execute on function
  pg_temp.as_player(text), pg_temp.points(jsonb, text), pg_temp.quote(jsonb, text, text, text),
  pg_temp.stored(jsonb, text, text), pg_temp.coins(jsonb)
to authenticated;

set role authenticated;

select pg_temp.as_player('b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2');
select public.found_player('Oscar', 'caravanier', 'port-sable') is not null;
select pg_temp.as_player('a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1');

do $$
declare
  v_state jsonb := public.found_player('Roxane', 'caravanier', 'port-sable');
  v_after jsonb;
begin
  assert jsonb_array_length(v_state -> 'reputation' -> 'tiers') = 6, 'the state lists the reputation tiers';
  assert v_state -> 'reputation' -> 'tiers' -> 1 ->> 'name' = 'Connu', 'the first tier is Connu';
  assert (v_state -> 'reputation' -> 'tiers' -> 1 ->> 'points')::int = 200, 'Connu needs 200 points';
  assert pg_temp.points(v_state, 'port-sable') = 0, 'a new player is unknown';

  v_after := public.buy_goods('sel', 40);
  assert pg_temp.points(v_after, 'port-sable') = floor((pg_temp.coins(v_state) - pg_temp.coins(v_after)) / 20.0),
    'buying earns 1 point per 20 coins in the town';
end;
$$;

reset role;
select pg_temp.set_points('a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1', 'port-sable', 200);
set role authenticated;

do $$
declare
  v_known jsonb := public.get_state();
  v_stranger jsonb;
  v_known_buy numeric := pg_temp.quote(v_known, 'port-sable', 'epices', 'buy_price');
  v_stranger_buy numeric;
begin
  perform pg_temp.as_player('b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2');
  v_stranger := public.get_state();
  perform pg_temp.as_player('a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1');
  v_stranger_buy := pg_temp.quote(v_stranger, 'port-sable', 'epices', 'buy_price');
  assert v_known_buy between v_stranger_buy * 0.985 and v_stranger_buy * 0.995,
    format('a known trader buys 1 %% cheaper: %s against %s', v_known_buy, v_stranger_buy);
  assert pg_temp.quote(v_known, 'port-sable', 'epices', 'sell_price') > pg_temp.quote(v_stranger, 'port-sable', 'epices', 'sell_price'),
    'a known trader sells dearer';
end;
$$;

reset role;
select pg_temp.set_points('a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1', 'port-sable', 799);
update public.players set coins = 5000 where player_id = 'a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1';
set role authenticated;

do $$
declare
  v_state jsonb := public.sell_goods('sel', 40);
begin
  assert pg_temp.points(v_state, 'port-sable') >= 800, 'selling earns reputation too';
  assert exists (select 1 from jsonb_array_elements(v_state -> 'journal') entry
      where entry ->> 'kind_id' = 'reputation' and entry ->> 'detail' like 'Tu es désormais estimé à Port-Sable.%'),
    'reaching a tier is written in the journal';

  perform public.buy_goods('sel', 40);
  perform public.deposit_goods('sel', 40);
  perform public.buy_goods('sel', 40);
  v_state := public.deposit_goods('sel', 40);
  assert pg_temp.stored(v_state, 'port-sable', 'sel') = 80, 'an esteemed trader stores 20 more goods in the town';
end;
$$;

select pg_temp.as_player('b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2');

do $$
declare
  v_seller_before int := pg_temp.points(public.get_state(), 'port-sable');
  v_buyer_before int;
  v_state jsonb := public.post_offer(null, 200, 'sel', 5);
  v_offer bigint := (v_state -> 'my_offers' -> 0 ->> 'id')::bigint;
begin
  perform pg_temp.as_player('a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1');
  perform public.withdraw_goods('sel', 5);
  v_buyer_before := pg_temp.points(public.get_state(), 'port-sable');
  v_state := public.accept_offer(v_offer);
  assert pg_temp.points(v_state, 'port-sable') >= v_buyer_before + 10, 'accepting an offer earns 1 point per 20 coins traded';
  perform pg_temp.as_player('b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2');
  assert pg_temp.points(public.get_state(), 'port-sable') >= v_seller_before + 10, 'the seller earns the same reputation';
end;
$$;

select pg_temp.as_player('c3c3c3c3-c3c3-c3c3-c3c3-c3c3c3c3c3c3');
select public.found_player('Léonie', 'negociant', 'port-sable') is not null;

reset role;
update public.players set coins = 5000 where player_id = 'c3c3c3c3-c3c3-c3c3-c3c3-c3c3c3c3c3c3';
set role authenticated;

create temp table fees (name text primary key, fee int not null, id bigint not null);
grant all on fees to authenticated;

do $$
declare
  v_state jsonb;
begin
  perform public.open_branch('hautecombe');
  perform public.buy_goods('sel', 50);
  v_state := public.post_contract('sel', 25, 'hautecombe', 1);
  insert into fees values ('stranger', (v_state -> 'my_contracts' -> 0 ->> 'game_fee')::int, (v_state -> 'my_contracts' -> 0 ->> 'id')::bigint);
end;
$$;

reset role;
select pg_temp.set_points('c3c3c3c3-c3c3-c3c3-c3c3-c3c3c3c3c3c3', 'port-sable', 3000);
set role authenticated;

do $$
declare
  v_state jsonb := public.post_contract('sel', 25, 'hautecombe', 1);
  v_fee int := (v_state -> 'my_contracts' -> 0 ->> 'game_fee')::int;
  v_base int := (select fee from fees where name = 'stranger');
begin
  assert v_fee between ceil((v_base - 1) * 0.75) and ceil(v_base * 0.75),
    format('a notable pays the game carrier 25 %% less: %s against %s', v_fee, v_base);
end;
$$;

reset role;
update public.transport_contracts
set takeover_at = now() - interval '2 minutes', game_arrives_at = now() - interval '1 minute'
where id = (select id from fees where name = 'stranger');
set role authenticated;

do $$
declare
  v_before int := pg_temp.points(public.get_state(), 'port-sable');
begin
  perform public.mark_exchanges_seen();
  assert pg_temp.points(public.get_state(), 'port-sable') between v_before and v_before + 1,
    'reading the state twice gives nothing more';
end;
$$;

do $$
declare
  v_state jsonb := public.get_state();
begin
  assert (select contract ->> 'status' from jsonb_array_elements(v_state -> 'my_contracts') contract
      where (contract ->> 'id')::bigint = (select id from fees where name = 'stranger')) = 'delivered',
    'the game carrier delivered the first shipment';
  assert pg_temp.points(v_state, 'port-sable') >= 3000 + 25, 'a delivered shipment earns 1 point per 10 coins of cargo in its origin town';
end;
$$;

reset role;
select pg_temp.set_points('c3c3c3c3-c3c3-c3c3-c3c3-c3c3c3c3c3c3', 'port-sable', 10000);
update private.settings set events_enabled = true;
update public.players set orders_checked_at = now() - interval '10000 hours' where player_id = 'c3c3c3c3-c3c3-c3c3-c3c3-c3c3c3c3c3c3';
set role authenticated;
select public.get_state() is not null;
reset role;

do $$
declare
  v_order public.special_orders;
  v_price numeric;
begin
  select * into v_order from public.special_orders where player_id = 'c3c3c3c3-c3c3-c3c3-c3c3-c3c3c3c3c3c3';
  assert v_order.id is not null, 'a long absence brings a special order';
  select base_price into v_price from public.markets where town_id = 'port-sable' and good_id = v_order.good_id;
  assert v_order.quantity = least(greatest(round(600 / v_price), 1), 30),
    format('a burgher gets 50 %% bigger orders: %s %s', v_order.quantity, v_order.good_id);
  assert (select detail from public.event_log where player_id = v_order.player_id and kind_id = 'commande' order by id desc limit 1) like '%sous 6 h.',
    'the order window reads in hours';
end;
$$;

update private.settings set events_enabled = false;

select 'All reputation tests passed' as result;
