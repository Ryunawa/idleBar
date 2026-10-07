\set ON_ERROR_STOP on

insert into auth.users (id) values
  ('66666666-6666-6666-6666-666666666666'),
  ('77777777-7777-7777-7777-777777777777'),
  ('88888888-8888-8888-8888-888888888888');

update private.settings set travel_time_factor = 1, craft_time_factor = 1, events_enabled = false;

create temp table ids (name text primary key, id bigint not null);
grant all on ids to authenticated;

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

create function pg_temp.coins(p_state jsonb) returns bigint
language sql
as $$
  select (p_state -> 'player' ->> 'coins')::bigint;
$$;

create function pg_temp.stored(p_state jsonb, p_town_id text, p_good_id text) returns integer
language sql
as $$
  select coalesce((
    select (line ->> 'quantity')::int from jsonb_array_elements(p_state -> 'warehouses') line
    where line ->> 'town_id' = p_town_id and line ->> 'good_id' = p_good_id
  ), 0);
$$;

create function pg_temp.loaded(p_state jsonb, p_good_id text) returns integer
language sql
as $$
  select coalesce((select (line ->> 'quantity')::int from jsonb_array_elements(p_state -> 'cargo') line where line ->> 'good_id' = p_good_id), 0);
$$;

create function pg_temp.mine(p_state jsonb, p_list text, p_id bigint) returns jsonb
language sql
as $$
  select entry from jsonb_array_elements(p_state -> p_list) entry where (entry ->> 'id')::bigint = p_id;
$$;

create function pg_temp.id_of(p_name text) returns bigint
language sql
as $$
  select id from ids where name = p_name;
$$;

grant execute on function
  pg_temp.expect_refusal(text, text), pg_temp.as_player(text), pg_temp.coins(jsonb),
  pg_temp.stored(jsonb, text, text), pg_temp.loaded(jsonb, text), pg_temp.mine(jsonb, text, bigint), pg_temp.id_of(text)
to authenticated;

set role authenticated;

select pg_temp.as_player('88888888-8888-8888-8888-888888888888');
do $$
declare
  v_world jsonb := public.get_world();
  v_state jsonb := public.found_player('Isaure', 'negociant', 'port-sable');
begin
  assert jsonb_array_length(v_world -> 'journeys') = 56, 'the world lists the shortest journey between every pair of towns';
  assert (select (journey ->> 'minutes')::int from jsonb_array_elements(v_world -> 'journeys') journey
    where journey ->> 'from_town_id' = 'port-sable' and journey ->> 'to_town_id' = 'rocheclaire') = 70,
    'Port-Sable to Rocheclaire goes through Hautecombe';
  assert (v_world -> 'rules' ->> 'offer_hours')::int = 48, 'offers last 48 hours';
  assert (select good ->> 'one_name' from jsonb_array_elements(v_world -> 'goods') good where good ->> 'id' = 'outils') = 'outil',
    'the world names one unit of a good';
  assert (select good ->> 'many_name' from jsonb_array_elements(v_world -> 'goods') good where good ->> 'id' = 'chariot') = 'chariots',
    'the world names several units of a good';
  assert v_state -> 'player' ->> 'craft_id' = 'negociant', 'the merchant is playable';
  assert (v_state -> 'workshop' ->> 'storage_capacity')::int = 60, 'the counter stores like a level 1 workshop';
  assert (v_state -> 'player' ->> 'next_branch_price')::int = 300, 'the first branch costs 300 coins';
  assert jsonb_array_length(v_state -> 'market') = 19, 'only the home market is visible before any branch';
end;
$$;

select pg_temp.expect_refusal($$select public.buy_goods('sel', 1, 'hautecombe')$$, 'pas de comptoir dans cette ville');
select pg_temp.expect_refusal($$select public.open_branch('port-sable')$$, 'déjà un comptoir');
select pg_temp.expect_refusal($$select public.open_branch('atlantide')$$, 'n''existe pas');

do $$
declare
  v_state jsonb := public.open_branch('hautecombe');
begin
  assert v_state -> 'branches' = '["hautecombe"]'::jsonb, 'the branch is opened';
  assert pg_temp.coins(v_state) = 100, 'the branch is paid';
  assert (v_state -> 'player' ->> 'next_branch_price')::int = 540, 'the next branch costs more';
  assert jsonb_array_length(v_state -> 'market') = 38, 'the branch market is visible';
end;
$$;

select pg_temp.expect_refusal($$select public.open_branch('rocheclaire')$$, 'Pas assez d''écus');

reset role;
update public.players set coins = 5000 where player_id = '88888888-8888-8888-8888-888888888888';
set role authenticated;

do $$
declare
  v_state jsonb;
begin
  perform public.buy_goods('sel', 30, 'port-sable');
  v_state := public.buy_goods('sel', 5, 'hautecombe');
  assert pg_temp.stored(v_state, 'port-sable', 'sel') = 30, 'the merchant buys at home';
  assert pg_temp.stored(v_state, 'hautecombe', 'sel') = 5, 'the merchant buys at a branch';
end;
$$;

select pg_temp.as_player('77777777-7777-7777-7777-777777777777');
select public.found_player('Hilda', 'forgeron', 'rocheclaire');
select pg_temp.expect_refusal($$select public.open_branch('ferrenoire')$$, 'Seul le négociant');
select pg_temp.expect_refusal($$select public.deposit_goods('fer', 1)$$, 'déjà rangées');

reset role;
update public.players set coins = 2000 where player_id = '77777777-7777-7777-7777-777777777777';
set role authenticated;

select pg_temp.expect_refusal($$select public.post_offer(null, 10, null, 5)$$, 'écus contre des écus');
select pg_temp.expect_refusal($$select public.post_offer('fer', 1, 'fer', 1)$$, 'différentes');
select pg_temp.expect_refusal($$select public.post_offer('fer', 5, null, 100)$$, 'pas assez pour cette offre');
select pg_temp.expect_refusal($$select public.post_offer(null, 10, 'fer', 1, 'port-sable')$$, 'pas de comptoir dans cette ville');
select pg_temp.expect_refusal($$select public.post_contract('outils', 1, 'port-sable', 10)$$, 'il te faut un comptoir');

do $$
declare
  v_state jsonb := public.post_offer(null, 300, 'fer', 10);
begin
  assert pg_temp.coins(v_state) = 1700, 'the offered coins are set aside';
  assert v_state -> 'my_offers' -> 0 ->> 'status' = 'open', 'the offer is open';
  assert v_state -> 'my_offers' -> 0 ->> 'town_id' = 'rocheclaire', 'the offer is posted in the home town';
  insert into ids values ('wants-iron', (v_state -> 'my_offers' -> 0 ->> 'id')::bigint);
end;
$$;

select pg_temp.expect_refusal(format('select public.accept_offer(%s)', pg_temp.id_of('wants-iron')), 'propre offre');

select pg_temp.as_player('66666666-6666-6666-6666-666666666666');
select public.found_player('Gaspard', 'caravanier', 'port-sable');

reset role;
update public.players set coins = 2000 where player_id = '66666666-6666-6666-6666-666666666666';
set role authenticated;

do $$
declare
  v_state jsonb;
begin
  perform public.buy_goods('fer', 10);
  v_state := public.buy_goods('sel', 10);
  assert jsonb_array_length(v_state -> 'offers') = 0, 'offers from other towns are not listed';
  assert (v_state -> 'caravan' ->> 'load')::int = 20, 'the caravan reports its load';
end;
$$;

select pg_temp.expect_refusal(format('select public.accept_offer(%s)', pg_temp.id_of('wants-iron')), 'pas dans cette ville');

reset role;
update public.caravans
set town_id = 'rocheclaire', from_town_id = 'hautecombe', departed_at = now() - interval '1 hour', arrives_at = now() - interval '1 minute'
where player_id = '66666666-6666-6666-6666-666666666666';
set role authenticated;

do $$
declare
  v_before jsonb := public.get_state();
  v_offer jsonb := v_before -> 'offers' -> 0;
  v_after jsonb;
begin
  assert jsonb_array_length(v_before -> 'offers') = 1, 'the town offers are listed';
  assert v_offer ->> 'seller' = 'Hilda', 'the seller name is shown';
  assert not v_offer ? 'seller_id', 'the seller id stays hidden';

  v_after := public.accept_offer((v_offer ->> 'id')::bigint);
  assert pg_temp.loaded(v_after, 'fer') = 0, 'the iron leaves the hold';
  assert pg_temp.coins(v_after) = pg_temp.coins(v_before) + 300, 'the offered coins are received';
  assert jsonb_array_length(v_after -> 'offers') = 0, 'a concluded offer leaves the counter';
end;
$$;

select pg_temp.expect_refusal(format('select public.accept_offer(%s)', pg_temp.id_of('wants-iron')), 'plus disponible');

do $$
declare
  v_state jsonb;
begin
  v_state := public.post_offer('sel', 5, null, 70);
  insert into ids values ('sells-salt', (v_state -> 'my_offers' -> 0 ->> 'id')::bigint);
  v_state := public.post_offer('sel', 5, 'outils', 1);
  assert pg_temp.loaded(v_state, 'sel') = 0, 'offered goods leave the hold';

  v_state := public.cancel_offer((v_state -> 'my_offers' -> 0 ->> 'id')::bigint);
  assert v_state -> 'my_offers' -> 0 ->> 'status' = 'cancelled', 'the offer is withdrawn';
  assert pg_temp.stored(v_state, 'rocheclaire', 'sel') = 5, 'withdrawn goods wait in the town warehouse';
end;
$$;

select pg_temp.as_player('77777777-7777-7777-7777-777777777777');
select pg_temp.expect_refusal(format('select public.cancel_offer(%s)', pg_temp.id_of('sells-salt')), 'n''existe pas');

do $$
declare
  v_before jsonb := public.get_state();
  v_after jsonb;
begin
  assert pg_temp.stored(v_before, 'rocheclaire', 'fer') = 10, 'the bought iron reached the forge warehouse';
  assert (v_before -> 'news' ->> 'concluded')::int = 1, 'the seller hears about the concluded offer';
  assert v_before -> 'my_offers' -> 0 ->> 'buyer' = 'Gaspard', 'the buyer name is shown to the seller';

  v_after := public.accept_offer(pg_temp.id_of('sells-salt'));
  assert pg_temp.coins(v_after) = pg_temp.coins(v_before) - 70, 'the salt is paid';
  assert pg_temp.stored(v_after, 'rocheclaire', 'sel') = 5, 'the salt lands in the warehouse';

  v_after := public.post_offer(null, 50, 'charbon', 1);
  insert into ids values ('expiring', (v_after -> 'my_offers' -> 0 ->> 'id')::bigint);
end;
$$;

reset role;
update public.offers set expires_at = now() - interval '1 minute' where id = (select id from ids where name = 'expiring');
set role authenticated;

select pg_temp.as_player('66666666-6666-6666-6666-666666666666');
do $$
declare
  v_state jsonb := public.get_state();
begin
  assert jsonb_array_length(v_state -> 'offers') = 0, 'an expired offer is no longer listed';
  assert (v_state -> 'news' ->> 'concluded')::int = 1, 'the caravan hears about its concluded offer';
  assert (select count(*) from public.offers where seller_id <> auth.uid()) = 0, 'a player only reads their own offer rows';
end;
$$;

select pg_temp.expect_refusal(format('select public.accept_offer(%s)', pg_temp.id_of('expiring')), 'plus disponible');

select pg_temp.as_player('77777777-7777-7777-7777-777777777777');
do $$
declare
  v_before bigint := (select coins from public.players);
  v_state jsonb := public.get_state();
begin
  assert pg_temp.coins(v_state) = v_before + 50, 'an expired offer gives its coins back';
  assert pg_temp.mine(v_state, 'my_offers', pg_temp.id_of('expiring')) ->> 'status' = 'expired', 'the offer is marked expired';
  assert (v_state -> 'news' ->> 'expired')::int = 1, 'the seller hears about the expired offer';

  v_state := public.mark_exchanges_seen();
  assert v_state -> 'news' = '{"concluded": 0, "expired": 0, "delivered": 0, "failed": 0}'::jsonb, 'news are cleared once seen';
end;
$$;

select pg_temp.as_player('66666666-6666-6666-6666-666666666666');
do $$
declare
  v_state jsonb;
begin
  v_state := public.withdraw_goods('sel', 99);
  assert pg_temp.loaded(v_state, 'sel') = 5 and pg_temp.stored(v_state, 'rocheclaire', 'sel') = 0, 'goods are withdrawn into the hold';
  v_state := public.deposit_goods('sel', 3);
  assert pg_temp.loaded(v_state, 'sel') = 2 and pg_temp.stored(v_state, 'rocheclaire', 'sel') = 3, 'goods are deposited in the warehouse';
end;
$$;

select pg_temp.expect_refusal($$select public.post_offer('sel', 4, null, 30, null, true)$$, 'pas assez dans ton entrepôt');

do $$
declare
  v_state jsonb := public.post_offer('sel', 3, null, 30, null, true);
begin
  assert pg_temp.stored(v_state, 'rocheclaire', 'sel') = 0, 'a caravan offers goods from its town warehouse';
  assert pg_temp.loaded(v_state, 'sel') = 2, 'offering from the warehouse leaves the hold untouched';
  v_state := public.cancel_offer((v_state -> 'my_offers' -> 0 ->> 'id')::bigint);
  assert pg_temp.stored(v_state, 'rocheclaire', 'sel') = 3, 'withdrawn goods go back to the warehouse';
end;
$$;

reset role;
update private.settings set depot_capacity = 4;
set role authenticated;

do $$
declare
  v_state jsonb := public.deposit_goods('sel', 2);
begin
  assert pg_temp.stored(v_state, 'rocheclaire', 'sel') = 4, 'deposits are capped by the warehouse capacity';
end;
$$;

select pg_temp.expect_refusal($$select public.deposit_goods('sel', 1)$$, 'entrepôt est plein');

do $$
declare
  v_state jsonb := public.post_offer('sel', 1, null, 10);
begin
  insert into ids values ('blocked', (v_state -> 'my_offers' -> 0 ->> 'id')::bigint);
end;
$$;

select pg_temp.expect_refusal(format('select public.cancel_offer(%s)', pg_temp.id_of('blocked')), 'pas la place');

reset role;
update private.settings set depot_capacity = 60;
set role authenticated;
select public.cancel_offer(pg_temp.id_of('blocked')) is not null;
select public.withdraw_goods('sel', 1) is not null;

select pg_temp.as_player('88888888-8888-8888-8888-888888888888');
select pg_temp.expect_refusal($$select public.post_contract('sel', 10, 'rocheclaire', 5)$$, 'il te faut un comptoir');
select pg_temp.expect_refusal($$select public.post_contract('sel', 100, 'hautecombe', 5)$$, 'pas assez dans ton entrepôt');
select pg_temp.expect_refusal($$select public.post_contract('sel', 10, 'port-sable', 5)$$, 'destination différente');
select pg_temp.expect_refusal($$select public.post_contract('sel', 10, 'hautecombe', 0)$$, 'au moins 1 écu');

do $$
declare
  v_before jsonb := public.get_state();
  v_state jsonb := public.post_contract('sel', 20, 'hautecombe', 5);
  v_contract jsonb := v_state -> 'my_contracts' -> 0;
begin
  assert v_contract ->> 'status' = 'open', 'the contract is published';
  assert (v_contract ->> 'game_fee')::int = 16, 'the game carrier charges by value and distance';
  assert (v_contract ->> 'deposit')::int = 200, 'the deposit equals the value of the goods';
  assert pg_temp.coins(v_state) = pg_temp.coins(v_before) - 16, 'the larger of the reward and the game fee is set aside';
  assert pg_temp.stored(v_state, 'port-sable', 'sel') = 10, 'the shipped goods leave the warehouse';
  insert into ids values ('delivered', (v_contract ->> 'id')::bigint);
end;
$$;

select pg_temp.as_player('77777777-7777-7777-7777-777777777777');
select pg_temp.expect_refusal(format('select public.accept_contract(%s)', pg_temp.id_of('delivered')), 'Seuls les caravaniers');

select pg_temp.as_player('66666666-6666-6666-6666-666666666666');
do $$
declare
  v_state jsonb := public.get_state();
begin
  assert pg_temp.mine(v_state, 'contracts', pg_temp.id_of('delivered')) ->> 'shipper' = 'Isaure', 'open contracts are listed for caravans';
end;
$$;

select pg_temp.expect_refusal(format('select public.accept_contract(%s)', pg_temp.id_of('delivered')), 'Va d''abord à Port-Sable');

reset role;
update public.caravans set town_id = 'port-sable', from_town_id = 'hautecombe' where player_id = '66666666-6666-6666-6666-666666666666';
set role authenticated;

do $$
declare
  v_before jsonb := public.get_state();
  v_state jsonb := public.accept_contract(pg_temp.id_of('delivered'));
begin
  assert pg_temp.coins(v_state) = pg_temp.coins(v_before) - 200, 'the carrier pays the deposit';
  assert (v_state -> 'caravan' ->> 'load')::int = 21, 'the contract goods take room in the hold';
  assert pg_temp.loaded(v_state, 'sel') = 1, 'the contract goods stay apart from the cargo';
  assert pg_temp.mine(v_state, 'my_contracts', pg_temp.id_of('delivered')) ->> 'role' = 'carrier', 'the carrier sees the contract';

  v_state := public.buy_goods('sel', 1000);
  assert (v_state -> 'caravan' ->> 'load')::int = 40, 'buying fills the room left by the contract';
  v_state := public.sell_goods('sel', 1000);
  assert (v_state -> 'caravan' ->> 'load')::int = 20, 'contract goods cannot be sold';
end;
$$;

select pg_temp.as_player('88888888-8888-8888-8888-888888888888');
do $$
declare
  v_state jsonb := public.get_state();
begin
  assert pg_temp.mine(v_state, 'my_contracts', pg_temp.id_of('delivered')) ->> 'carrier' = 'Gaspard', 'the shipper sees the carrier';
end;
$$;

select pg_temp.expect_refusal(format('select public.cancel_contract(%s)', pg_temp.id_of('delivered')), 'ne peut plus être annulé');

select pg_temp.as_player('66666666-6666-6666-6666-666666666666');
select public.depart('hautecombe');

reset role;
update public.transport_contracts set carried_at = now() - interval '2 hours' where id = (select id from ids where name = 'delivered');
update public.caravans
set departed_at = now() - interval '31 minutes', arrives_at = now() - interval '1 minute'
where player_id = '66666666-6666-6666-6666-666666666666';
set role authenticated;

select pg_temp.as_player('88888888-8888-8888-8888-888888888888');
do $$
declare
  v_state jsonb := public.get_state();
begin
  assert pg_temp.mine(v_state, 'my_contracts', pg_temp.id_of('delivered')) ->> 'status' = 'delivered', 'the contract is delivered on arrival';
  assert pg_temp.stored(v_state, 'hautecombe', 'sel') = 25, 'the goods reach the destination warehouse';
  assert (v_state -> 'news' ->> 'delivered')::int = 1, 'the shipper hears about the delivery';
end;
$$;

select pg_temp.as_player('66666666-6666-6666-6666-666666666666');
do $$
declare
  v_state jsonb := public.get_state();
begin
  assert (v_state -> 'caravan' ->> 'load')::int = 0, 'the delivered goods leave the hold';
  assert (v_state -> 'news' ->> 'delivered')::int = 1, 'the carrier hears about the delivery';
end;
$$;

select pg_temp.as_player('88888888-8888-8888-8888-888888888888');
do $$
declare
  v_state jsonb := public.post_contract('sel', 10, 'port-sable', 30, 'hautecombe');
begin
  insert into ids values ('late', (v_state -> 'my_contracts' -> 0 ->> 'id')::bigint);
  v_state := public.post_contract('sel', 10, 'hautecombe', 12, 'port-sable');
  insert into ids values ('by-game', (v_state -> 'my_contracts' -> 0 ->> 'id')::bigint);
  v_state := public.post_contract('sel', 5, 'port-sable', 2, 'hautecombe');
  insert into ids values ('cancelled', (v_state -> 'my_contracts' -> 0 ->> 'id')::bigint);
end;
$$;

do $$
declare
  v_before jsonb := public.get_state();
  v_state jsonb := public.cancel_contract(pg_temp.id_of('cancelled'));
begin
  assert pg_temp.mine(v_state, 'my_contracts', pg_temp.id_of('cancelled')) ->> 'status' = 'cancelled', 'the contract is cancelled';
  assert pg_temp.coins(v_state) = pg_temp.coins(v_before) + 4, 'the escrow comes back';
  assert pg_temp.stored(v_state, 'hautecombe', 'sel') = pg_temp.stored(v_before, 'hautecombe', 'sel') + 5, 'the goods come back';
end;
$$;

select pg_temp.as_player('66666666-6666-6666-6666-666666666666');
select pg_temp.expect_refusal(format('select public.cancel_contract(%s)', pg_temp.id_of('late')), 'n''existe pas');
select public.accept_contract(pg_temp.id_of('late'));

reset role;
update public.transport_contracts set deadline = now() - interval '1 minute' where id = (select id from ids where name = 'late');
update public.transport_contracts set takeover_at = now() - interval '1 minute', game_arrives_at = now() + interval '1 hour'
where id = (select id from ids where name = 'by-game');
create temp table purse as select coins from public.players where player_id = '88888888-8888-8888-8888-888888888888';
grant select on purse to authenticated;
set role authenticated;

do $$
declare
  v_state jsonb := public.get_state();
begin
  assert pg_temp.mine(v_state, 'my_contracts', pg_temp.id_of('late')) ->> 'status' = 'failed', 'a late contract fails';
  assert pg_temp.loaded(v_state, 'sel') = 10, 'the late carrier keeps the goods paid by the deposit';
  assert pg_temp.mine(v_state, 'contracts', pg_temp.id_of('by-game')) is null, 'a contract taken over by the game is no longer offered';
end;
$$;

select pg_temp.expect_refusal(format('select public.accept_contract(%s)', pg_temp.id_of('by-game')), 'plus disponible');

select pg_temp.as_player('88888888-8888-8888-8888-888888888888');
do $$
declare
  v_state jsonb := public.get_state();
begin
  assert pg_temp.coins(v_state) = (select coins from purse) + 130 + 4, 'the shipper gets the deposit, the reward and the game fee surplus';
  assert pg_temp.mine(v_state, 'my_contracts', pg_temp.id_of('by-game')) ->> 'status' = 'shipped', 'the game carrier takes the contract';
  assert (v_state -> 'news' ->> 'failed')::int = 1, 'the shipper hears about the late contract';
end;
$$;

reset role;
update public.transport_contracts set game_arrives_at = now() - interval '1 minute' where id = (select id from ids where name = 'by-game');
set role authenticated;

do $$
declare
  v_before integer := (select quantity from public.warehouses where town_id = 'hautecombe' and good_id = 'sel');
  v_state jsonb := public.get_state();
begin
  assert pg_temp.mine(v_state, 'my_contracts', pg_temp.id_of('by-game')) ->> 'status' = 'delivered', 'the game carrier delivers';
  assert pg_temp.stored(v_state, 'hautecombe', 'sel') = v_before + 10, 'the game carrier brings the goods';
  v_state := public.post_contract('sel', 5, 'port-sable', 2, 'hautecombe');
  insert into ids values ('refused', (v_state -> 'my_contracts' -> 0 ->> 'id')::bigint);
end;
$$;

reset role;
update public.players set coins = 10 where player_id = '66666666-6666-6666-6666-666666666666';
set role authenticated;

select pg_temp.as_player('66666666-6666-6666-6666-666666666666');
select pg_temp.expect_refusal(format('select public.accept_contract(%s)', pg_temp.id_of('refused')), 'écus de caution');

reset role;
update public.players set coins = 2000 where player_id = '66666666-6666-6666-6666-666666666666';
set role authenticated;

select public.buy_goods('sel', 1000);
select pg_temp.expect_refusal(format('select public.accept_contract(%s)', pg_temp.id_of('refused')), 'n''a pas la place');

do $$
declare
  v_before jsonb := public.deposit_goods('sel', 5);
  v_after jsonb := public.sell_goods('sel', 3);
begin
  assert pg_temp.stored(v_after, 'hautecombe', 'sel') = 2, 'a caravan sells from its town warehouse first';
  assert pg_temp.loaded(v_after, 'sel') = pg_temp.loaded(v_before, 'sel'), 'the hold is kept while the warehouse has stock';
  assert pg_temp.coins(v_after) > pg_temp.coins(v_before), 'the warehouse goods are paid';

  v_after := public.sell_goods('sel', 4);
  assert pg_temp.stored(v_after, 'hautecombe', 'sel') = 0, 'the warehouse is emptied';
  assert pg_temp.loaded(v_after, 'sel') = pg_temp.loaded(v_before, 'sel') - 2, 'the rest of the sale comes from the hold';
end;
$$;

do $$
begin
  assert (select count(*) from public.transport_contracts where auth.uid() not in (shipper_id, coalesce(carrier_id, shipper_id))) = 0,
    'a player only reads their own contract rows';
  assert (select count(*) from public.branches) = 0, 'a player cannot read the branches of others';
end;
$$;

reset role;

select 'All exchange tests passed' as result;
