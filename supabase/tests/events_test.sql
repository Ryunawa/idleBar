\set ON_ERROR_STOP on

insert into auth.users (id) values
  ('99999999-9999-9999-9999-999999999999'),
  ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'),
  ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb');

update private.settings set travel_time_factor = 1, craft_time_factor = 1, events_enabled = true, road_event_minutes = 1;

create function pg_temp.expect_refusal(p_call text, p_message text) returns void
language plpgsql
as $$
begin
  execute p_call;
  raise exception 'Expected "%" to be refused with "%"', p_call, p_message using errcode = 'XX000';
exception
  when raise_exception then
    if sqlerrm not like '%' || p_message || '%' then
      raise exception 'Expected "%" to be refused with "%", got "%"', p_call, p_message, sqlerrm using errcode = 'XX000';
    end if;
end;
$$;

create function pg_temp.as_player(p_player_id text) returns void
language sql
as $$
  select set_config('request.jwt.claim.sub', p_player_id, false);
$$;

create function pg_temp.loaded(p_state jsonb, p_good_id text) returns integer
language sql
as $$
  select coalesce((select (line ->> 'quantity')::int from jsonb_array_elements(p_state -> 'cargo') line where line ->> 'good_id' = p_good_id), 0);
$$;

create function pg_temp.stored(p_state jsonb, p_good_id text) returns integer
language sql
as $$
  select coalesce((select (line ->> 'quantity')::int from jsonb_array_elements(p_state -> 'warehouses') line where line ->> 'good_id' = p_good_id), 0);
$$;

create function pg_temp.arrival(p_state jsonb) returns timestamptz
language sql
as $$
  select (p_state -> 'caravan' ->> 'arrives_at')::timestamptz;
$$;

create function pg_temp.only_road_event(p_kind_id text) returns void
language sql
as $$
  update public.road_event_odds set weight = case when kind_id = p_kind_id then 1 else 0 end;
$$;

create function pg_temp.trigger_road_event(p_player_id uuid) returns void
language sql
as $$
  update public.pending_events set occurs_at = now() - interval '1 second' where player_id = p_player_id;
$$;

create function pg_temp.arrive(p_player_id uuid) returns void
language sql
as $$
  delete from public.pending_events where player_id = p_player_id;
  delete from public.trip_events where player_id = p_player_id;
  update public.caravans set arrives_at = now() - interval '1 second', departed_at = now() - interval '1 hour' where player_id = p_player_id;
$$;

grant execute on function
  pg_temp.expect_refusal(text, text), pg_temp.as_player(text), pg_temp.loaded(jsonb, text),
  pg_temp.stored(jsonb, text), pg_temp.arrival(jsonb)
to authenticated;

select pg_temp.only_road_event('orage');

set role authenticated;
select pg_temp.as_player('99999999-9999-9999-9999-999999999999');

do $$
declare
  v_world jsonb := public.get_world();
  v_state jsonb := public.found_player('Ulric', 'caravanier', 'port-sable');
begin
  assert jsonb_array_length(v_world -> 'event_kinds') = 9, 'the world lists every event';
  assert (select jsonb_array_length(kind -> 'choices') from jsonb_array_elements(v_world -> 'event_kinds') kind where kind ->> 'id' = 'bandits') = 2,
    'each event offers its reactions';
  assert jsonb_array_length(v_world -> 'directives') = 3, 'the world lists the trip directives';
  assert jsonb_array_length(v_state -> 'orders') = 2, 'standing orders only cover the workshop';
  assert v_state -> 'caravan' ->> 'directive_id' = 'prudence', 'a new caravan travels with prudence';
  assert (v_state -> 'journal_unseen')::int = 0, 'the journal starts empty';
end;
$$;

select pg_temp.expect_refusal($$select public.set_standing_order('orage', 'forcer')$$, 'directive du voyage');
select pg_temp.expect_refusal($$select public.depart('hautecombe', 'temerite')$$, 'directive n''existe pas');
select pg_temp.expect_refusal($$select public.answer_event('payer')$$, 'Aucun événement');
select public.buy_goods('sel', 20) is not null;

do $$
declare
  v_state jsonb := public.depart('hautecombe', 'rapidite');
begin
  assert jsonb_array_length(v_state -> 'journal') = 0, 'an upcoming event stays hidden';
  assert v_state -> 'trip_event' = 'null'::jsonb, 'nothing waits for an answer before the event';
  assert v_state -> 'caravan' ->> 'directive_id' = 'rapidite', 'the directive is chosen at departure';
end;
$$;

reset role;
do $$
begin
  assert (select count(*) from public.pending_events where player_id = '99999999-9999-9999-9999-999999999999') = 1, 'a road event is rolled at departure';
end;
$$;
select pg_temp.trigger_road_event('99999999-9999-9999-9999-999999999999');
set role authenticated;

do $$
declare
  v_before timestamptz := (select arrives_at from public.caravans);
  v_state jsonb := public.get_state();
begin
  assert v_state -> 'trip_event' ->> 'kind_id' = 'orage', 'the storm waits for an answer';
  assert v_state -> 'trip_event' ->> 'directive_choice_id' = 'forcer', 'the speed directive would force through';
  assert pg_temp.arrival(v_state) = v_before + interval '60 minutes', 'the caravan stops while waiting, an hour at worst';
  assert jsonb_array_length(v_state -> 'journal') = 0, 'nothing is decided yet';
  assert pg_temp.loaded(v_state, 'sel') = 20, 'no goods are lost before the answer';
end;
$$;

select pg_temp.expect_refusal($$select public.answer_event('payer')$$, 'ne convient pas');

do $$
declare
  v_planned timestamptz := (select arrives_at from public.caravans) - interval '60 minutes';
  v_state jsonb := public.answer_event('abriter');
  v_entry jsonb := v_state -> 'journal' -> 0;
begin
  assert v_state -> 'trip_event' = 'null'::jsonb, 'the answer closes the event';
  assert pg_temp.loaded(v_state, 'sel') = 20, 'sheltering keeps the goods dry';
  assert pg_temp.arrival(v_state) between v_planned + interval '9 minutes' and v_planned + interval '9 minutes 10 seconds',
    'a quick answer gives back the waiting time and only costs the shelter';
  assert v_entry ->> 'title' = 'Orage' and v_entry ->> 'detail' like '%Retard : 9 min.%', 'the answer is written in the journal';
  assert (v_state -> 'journal_unseen')::int = 1, 'the new entry is unseen';
  v_state := public.mark_journal_seen();
  assert (v_state -> 'journal_unseen')::int = 0, 'the journal is marked as seen';
end;
$$;

reset role;
select pg_temp.arrive('99999999-9999-9999-9999-999999999999');
select pg_temp.only_road_event('bandits');
update public.players set coins = 1000 where player_id = '99999999-9999-9999-9999-999999999999';
set role authenticated;
select public.depart('port-sable', 'economie') is not null;
reset role;
select pg_temp.trigger_road_event('99999999-9999-9999-9999-999999999999');
set role authenticated;
select public.get_state() is not null;
reset role;
update public.trip_events set decide_by = now() - interval '1 second' where player_id = '99999999-9999-9999-9999-999999999999';
set role authenticated;

do $$
declare
  v_coins int := (select coins from public.players);
  v_state jsonb := public.get_state();
begin
  assert v_state -> 'trip_event' = 'null'::jsonb, 'without an answer the directive decides after an hour';
  assert pg_temp.loaded(v_state, 'sel') = 15, 'the economy directive flees and leaves a quarter of the most valuable load';
  assert (v_state -> 'player' ->> 'coins')::int = v_coins, 'fleeing keeps the purse';
  assert exists (select 1 from jsonb_array_elements(v_state -> 'journal') entry where entry ->> 'detail' like 'Tu échappes aux bandits%'),
    'the flight is written in the journal';
end;
$$;

select pg_temp.expect_refusal($$select public.answer_event('payer')$$, 'Aucun événement');

reset role;
select pg_temp.arrive('99999999-9999-9999-9999-999999999999');
set role authenticated;
select public.depart('hautecombe', 'prudence') is not null;
reset role;
select pg_temp.trigger_road_event('99999999-9999-9999-9999-999999999999');
create temp table ransom as
select least(floor((coins + private.cargo_value(player_id)) * 0.08), 150)::int as amount, coins
from public.players where player_id = '99999999-9999-9999-9999-999999999999';
grant select on ransom to authenticated;
set role authenticated;

do $$
declare
  v_state jsonb := public.answer_event('payer');
begin
  assert (v_state -> 'player' ->> 'coins')::int = (select coins - amount from ransom), 'paying the bandits costs 8 % of the purse and the cargo';
  assert v_state -> 'journal' -> 0 ->> 'detail' like '%' || (select amount from ransom) || ' écus%', 'the journal tells what was paid';
end;
$$;

reset role;
select pg_temp.arrive('99999999-9999-9999-9999-999999999999');
select pg_temp.only_road_event('peage');
set role authenticated;
select public.depart('port-sable') is not null;
reset role;
select pg_temp.trigger_road_event('99999999-9999-9999-9999-999999999999');
set role authenticated;
select public.get_state() is not null;
reset role;
update public.trip_events set decide_by = now() - interval '1 second' where player_id = '99999999-9999-9999-9999-999999999999';
set role authenticated;

do $$
declare
  v_coins int := (select coins from public.players);
  v_state jsonb := public.get_state();
begin
  assert (v_state -> 'player' ->> 'coins')::int = v_coins - 4, 'the prudent caravan pays 3 coins per wagon and 1 per ten goods';
end;
$$;

reset role;
select pg_temp.arrive('99999999-9999-9999-9999-999999999999');
update public.players set coins = 0 where player_id = '99999999-9999-9999-9999-999999999999';
set role authenticated;
select public.depart('hautecombe') is not null;
reset role;
select pg_temp.trigger_road_event('99999999-9999-9999-9999-999999999999');
set role authenticated;

do $$
declare
  v_state jsonb := public.answer_event('payer');
begin
  assert v_state -> 'journal' -> 0 ->> 'detail' like 'Tu contournes%', 'without coins the caravan goes around the toll';
end;
$$;

reset role;
select pg_temp.arrive('99999999-9999-9999-9999-999999999999');
select pg_temp.only_road_event('trouvaille');
set role authenticated;
select public.depart('port-sable') is not null;
reset role;
select pg_temp.trigger_road_event('99999999-9999-9999-9999-999999999999');
set role authenticated;

do $$
declare
  v_state jsonb := public.get_state();
begin
  assert v_state -> 'trip_event' = 'null'::jsonb, 'a find needs no answer';
  assert (v_state -> 'caravan' ->> 'load')::int between 18 and 23, 'an abandoned cart gives 3 to 8 goods';
  assert exists (select 1 from jsonb_array_elements(v_state -> 'journal') entry where entry ->> 'kind_id' = 'trouvaille' and entry ->> 'tone' = 'good'),
    'a find is good news';
end;
$$;

reset role;
select pg_temp.arrive('99999999-9999-9999-9999-999999999999');
update public.caravans set town_id = 'port-sable' where player_id = '99999999-9999-9999-9999-999999999999';
update public.players set coins = 5000 where player_id = '99999999-9999-9999-9999-999999999999';
set role authenticated;

select pg_temp.as_player('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb');
select public.found_player('Wendel', 'negociant', 'port-sable');
reset role;
update public.players set coins = 5000 where player_id = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
set role authenticated;
select public.open_branch('hautecombe');
select public.buy_goods('sel', 10);
select public.post_contract('sel', 10, 'hautecombe', 20);

select pg_temp.as_player('99999999-9999-9999-9999-999999999999');
select public.accept_contract((select (contract ->> 'id')::bigint from jsonb_array_elements(public.get_state() -> 'contracts') contract));
reset role;
select pg_temp.only_road_event('orage');
set role authenticated;
select public.depart('hautecombe');

reset role;
update public.caravans set departed_at = now() - interval '60 minutes', arrives_at = now() - interval '10 minutes'
where player_id = '99999999-9999-9999-9999-999999999999';
update public.pending_events set occurs_at = now() - interval '30 minutes' where player_id = '99999999-9999-9999-9999-999999999999';
update public.transport_contracts set carried_at = now() - interval '2 hours', deadline = now() - interval '5 minutes'
where carrier_id = '99999999-9999-9999-9999-999999999999';
set role authenticated;

select pg_temp.as_player('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb');
do $$
declare
  v_state jsonb := public.get_state();
begin
  assert v_state -> 'my_contracts' -> 0 ->> 'status' = 'failed', 'a storm waiting for an answer can make the carrier late';
end;
$$;

select pg_temp.as_player('99999999-9999-9999-9999-999999999999');
do $$
declare
  v_state jsonb := public.get_state();
begin
  assert v_state -> 'trip_event' ->> 'kind_id' = 'orage', 'the carrier still has to answer the storm';
  assert pg_temp.arrival(v_state) > now(), 'the storm stops the late caravan';
end;
$$;

reset role;
update private.settings set breakdown_hours = 0.0001;
set role authenticated;

select pg_temp.as_player('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa');
select public.found_player('Vera', 'forgeron', 'ferrenoire');
reset role;
update public.players set coins = 5000 where player_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
set role authenticated;
select public.buy_goods('fer', 6);
select public.buy_goods('charbon', 3);
select public.start_production('outils', 3);

reset role;
do $$
begin
  assert (select count(*) from public.pending_events where player_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa' and kind_id = 'panne') = 1,
    'a breakdown is rolled when production starts';
end;
$$;
update public.workshops set started_at = now() - interval '700 seconds' where player_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
update public.pending_events set occurs_at = now() - interval '50 seconds' where player_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
set role authenticated;

do $$
declare
  v_coins int := (select coins from public.players);
  v_state jsonb := public.get_state();
begin
  assert pg_temp.stored(v_state, 'outils') = 1, 'the batch finished before the breakdown is delivered';
  assert (v_state -> 'workshop' ->> 'queued')::int = 2, 'the remaining batches wait for the repair';
  assert (v_state -> 'player' ->> 'coins')::int = v_coins - 20, 'the repair costs 20 coins per workshop level';
  assert (v_state -> 'workshop' ->> 'started_at')::timestamptz between now() + interval '499 seconds' and now() + interval '501 seconds',
    'the repair pauses the workshop for 10 minutes';
  assert (v_state -> 'workshop' ->> 'paused_until')::timestamptz between now() + interval '549 seconds' and now() + interval '551 seconds',
    'the repair ends 10 minutes after the breakdown';
  assert v_state -> 'journal' -> 0 ->> 'title' = 'Panne', 'the breakdown is written in the journal';
end;
$$;

reset role;
update private.settings set special_order_hours = 0.0001;
update public.players set orders_checked_at = now() - interval '1 day' where player_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
set role authenticated;

do $$
declare
  v_state jsonb := public.get_state();
  v_order jsonb := v_state -> 'special_orders' -> 0;
begin
  assert v_order ->> 'status' = 'open', 'a client places a special order';
  assert v_order ->> 'good_id' in ('outils', 'ferrures'), 'the order asks for one of the smith products';
  assert (v_order ->> 'unit_price')::numeric > (select (quote ->> 'sell_price')::numeric from jsonb_array_elements(v_state -> 'market') quote
    where quote ->> 'good_id' = v_order ->> 'good_id'), 'the order pays more than the market';
  assert (v_order ->> 'unit_price')::numeric >= floor((select (quote ->> 'buy_price')::numeric from jsonb_array_elements(v_state -> 'market') quote
    where quote ->> 'good_id' = v_order ->> 'good_id') * 0.95), 'the order never pays less than 95 % of the market price';
  assert (v_order ->> 'crafted')::boolean, 'an artisan order must be made in the workshop';
  assert (v_order ->> 'produced')::int = 0, 'nothing is made for the order yet';
  assert v_state -> 'journal' -> 0 ->> 'title' = 'Commande spéciale', 'the order is written in the journal';
end;
$$;

reset role;
do $$
declare
  v_order public.special_orders;
  v_recipe public.recipes;
  v_minutes numeric;
begin
  select * into v_order from public.special_orders where player_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa' and status = 'open';
  select * into v_recipe from public.recipes where craft_id = 'forgeron' and output_good_id = v_order.good_id order by sort_order limit 1;
  v_minutes := v_recipe.seconds / 60.0 / v_recipe.output_quantity;
  assert v_order.unit_price >= ceil(private.recipe_unit_cost(v_recipe.id, 'ferrenoire') * 1.1 + v_minutes * 2),
    'the order pays the inputs, a 10 % margin and 2 coins per minute of work';
  assert v_order.quantity = least(greatest(round(60 / v_minutes), 1), 30), 'the order asks for about an hour of work';

  perform private.reward_production(v_order.player_id, v_recipe, 1, 'ferrenoire');
  assert (select produced from public.special_orders where id = v_order.id) = least(v_order.quantity, v_recipe.output_quantity),
    'each batch made in the workshop counts towards the order';
  update public.special_orders set produced = 0 where id = v_order.id;
end;
$$;
insert into public.warehouses (player_id, town_id, good_id, quantity)
select player_id, town_id, good_id, quantity from public.special_orders where player_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa' and status = 'open'
on conflict (player_id, town_id, good_id) do update set quantity = public.warehouses.quantity + excluded.quantity;
set role authenticated;

do $$
begin
  perform public.get_state();
  assert exists (select 1 from public.special_orders where status = 'open'),
    'goods bought elsewhere do not fill an order: only what the workshop makes counts';
end;
$$;

reset role;
update public.special_orders set produced = quantity where player_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa' and status = 'open';
create temp table purse as select coins from public.players where player_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
create temp table first_order as select * from public.special_orders where player_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
grant select on purse, first_order to authenticated;
set role authenticated;

do $$
declare
  v_state jsonb := public.get_state();
begin
  assert (select status from public.special_orders where id = (select id from first_order)) = 'delivered', 'the order is delivered once the workshop has made it';
  assert (v_state -> 'player' ->> 'coins')::bigint
    >= (select coins from purse) + (select quantity * unit_price from first_order), 'the order is paid';
end;
$$;

select public.set_standing_order('commande', 'demander');
reset role;
update public.players set orders_checked_at = now() - interval '1 day' where player_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
set role authenticated;

do $$
declare
  v_order jsonb := public.get_state() -> 'special_orders' -> 0;
begin
  assert v_order ->> 'status' = 'open', 'a second order is placed';
end;
$$;

select pg_temp.expect_refusal(
  format('select public.fulfill_special_order(%s)', (select id from public.special_orders where status = 'open')),
  'Ton atelier doit encore fabriquer');

reset role;
insert into public.warehouses (player_id, town_id, good_id, quantity)
select player_id, town_id, good_id, quantity from public.special_orders where player_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa' and status = 'open'
on conflict (player_id, town_id, good_id) do update set quantity = public.warehouses.quantity + excluded.quantity;
update public.special_orders set produced = quantity where player_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa' and status = 'open';
set role authenticated;

do $$
declare
  v_id bigint := (select id from public.special_orders where status = 'open');
  v_state jsonb := public.get_state();
begin
  assert (select status from public.special_orders where id = v_id) = 'open', 'with the ask order the delivery waits for the player';
  v_state := public.fulfill_special_order(v_id);
  assert (select status from public.special_orders where id = v_id) = 'delivered', 'the player delivers the order';
end;
$$;

reset role;
update public.players set orders_checked_at = now() - interval '1 day' where player_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
set role authenticated;
select public.get_state() is not null;
reset role;
update public.special_orders set deadline = now() - interval '1 minute' where status = 'open';
update private.settings set special_order_hours = 1000;
set role authenticated;

do $$
declare
  v_state jsonb := public.get_state();
begin
  assert not exists (select 1 from public.special_orders where status = 'open'), 'an order past its deadline expires';
  assert exists (select 1 from jsonb_array_elements(v_state -> 'journal') entry where entry ->> 'detail' like '%pas été livrée à temps%'),
    'the expiry is written in the journal';
end;
$$;

reset role;
update private.settings set special_order_hours = 0.0001;
update public.players set orders_checked_at = now() - interval '1 day' where player_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
set role authenticated;
select public.get_state() is not null;

do $$
declare
  v_id bigint := (select id from public.special_orders where status = 'open');
begin
  perform public.decline_special_order(v_id);
  assert (select status from public.special_orders where id = v_id) = 'declined', 'the player declines the order';
end;
$$;

select pg_temp.expect_refusal(format('select public.decline_special_order(%s)', (select id from first_order)), 'plus ouverte');

select public.set_standing_order('commande', 'livrer') is not null;
reset role;
update public.players set orders_checked_at = now() - interval '1 day' where player_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
set role authenticated;
select public.get_state() is not null;
reset role;
update private.settings set special_order_hours = 1000;
update public.special_orders set deadline = now() - interval '1 minute' where status = 'open';
insert into public.warehouses (player_id, town_id, good_id, quantity)
select player_id, town_id, good_id, quantity from public.special_orders where player_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa' and status = 'open'
on conflict (player_id, town_id, good_id) do update set quantity = public.warehouses.quantity + excluded.quantity;
update public.special_orders set produced = quantity where player_id = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa' and status = 'open';
create temp table last_order as select id from public.special_orders where status = 'open';
grant select on last_order to authenticated;
set role authenticated;

do $$
begin
  perform public.get_state();
  assert (select status from public.special_orders where id = (select id from last_order)) = 'delivered',
    'an order whose goods were in stock before its deadline is delivered, not expired';
end;
$$;

select pg_temp.as_player('99999999-9999-9999-9999-999999999999');
do $$
begin
  assert (select count(*) from public.special_orders) = 0, 'a player cannot read the orders of others';
  assert (select count(*) from public.event_log where player_id <> auth.uid()) = 0, 'a player only reads their own journal';
end;
$$;

reset role;
select pg_temp.arrive('99999999-9999-9999-9999-999999999999');
update private.settings set events_enabled = false;
set role authenticated;
select public.depart('port-sable');
reset role;

do $$
begin
  assert (select count(*) from public.pending_events where player_id = '99999999-9999-9999-9999-999999999999') = 0,
    'no event is rolled when events are disabled';
end;
$$;

update private.settings set road_event_minutes = 150, breakdown_hours = 6, special_order_hours = 8;
update public.road_event_odds set weight = 1;

select 'All events tests passed' as result;
