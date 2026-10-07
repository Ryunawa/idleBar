\set ON_ERROR_STOP on

insert into auth.users (id) values
  ('cccccccc-cccc-cccc-cccc-cccccccccccc'),
  ('dddddddd-dddd-dddd-dddd-dddddddddddd'),
  ('eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee'),
  ('ffffffff-ffff-ffff-ffff-ffffffffffff');

update private.settings set travel_time_factor = 1, craft_time_factor = 1, events_enabled = false,
  journeyman_masterpiece_chance = 0, master_masterpiece_chance = 0;

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

create function pg_temp.stored(p_state jsonb, p_good_id text) returns integer
language sql
as $$
  select coalesce((select (line ->> 'quantity')::int from jsonb_array_elements(p_state -> 'warehouses') line where line ->> 'good_id' = p_good_id), 0);
$$;

create function pg_temp.loaded(p_state jsonb, p_good_id text) returns integer
language sql
as $$
  select coalesce((select (line ->> 'quantity')::int from jsonb_array_elements(p_state -> 'cargo') line where line ->> 'good_id' = p_good_id), 0);
$$;

create function pg_temp.quote(p_state jsonb, p_good_id text, p_side text) returns numeric
language sql
as $$
  select (quote ->> p_side)::numeric from jsonb_array_elements(p_state -> 'market') quote where quote ->> 'good_id' = p_good_id;
$$;

create function pg_temp.finish_production(p_player_id uuid) returns void
language sql
as $$
  update public.workshops set started_at = now() - interval '10 hours' where player_id = p_player_id and queued > 0;
$$;

create function pg_temp.set_xp(p_player_id uuid, p_xp integer) returns void
language sql
as $$
  insert into public.masteries (player_id, craft_id, xp)
  select player_id, craft_id, p_xp from public.players where player_id = p_player_id
  on conflict (player_id, craft_id) do update set xp = excluded.xp;
$$;

create function pg_temp.give_cargo(p_player_id uuid, p_good_id text, p_quantity integer) returns void
language sql
as $$
  insert into public.cargo (player_id, good_id, quantity) values (p_player_id, p_good_id, p_quantity)
  on conflict (player_id, good_id) do update set quantity = public.cargo.quantity + excluded.quantity;
$$;

create function pg_temp.arrive(p_player_id uuid) returns void
language sql
as $$
  delete from public.pending_events where player_id = p_player_id;
  update public.caravans set arrives_at = now() - interval '1 second', departed_at = now() - interval '1 hour' where player_id = p_player_id;
$$;

grant execute on function
  pg_temp.expect_refusal(text, text), pg_temp.as_player(text), pg_temp.stored(jsonb, text),
  pg_temp.loaded(jsonb, text), pg_temp.quote(jsonb, text, text)
to authenticated;

set role authenticated;
select pg_temp.as_player('cccccccc-cccc-cccc-cccc-cccccccccccc');

do $$
declare
  v_world jsonb := public.get_world();
  v_state jsonb := public.found_player('Xavier', 'forgeron', 'ferrenoire');
begin
  assert jsonb_array_length(v_world -> 'talents') = 12, 'the world lists every talent';
  assert jsonb_array_length(v_world -> 'fittings') = 4, 'the world lists every caravan fitting';
  assert (select craft ->> 'family' from jsonb_array_elements(v_world -> 'crafts') craft where craft ->> 'id' = 'negociant') = 'negociant',
    'the world tells the family of each craft';
  assert (select (good ->> 'market_sells')::boolean from jsonb_array_elements(v_world -> 'goods') good where good ->> 'id' = 'sel')
    and not (select (good ->> 'market_sells')::boolean from jsonb_array_elements(v_world -> 'goods') good where good ->> 'id' = 'chariot'),
    'the world tells which goods the markets sell';
  assert (v_world -> 'rules' ->> 'journeyman_xp')::int = 600, 'the journeyman rank needs 600 experience';
  assert v_state -> 'mastery' ->> 'rank' = 'apprenti', 'a new player is an apprentice';
  assert (v_state -> 'mastery' ->> 'next_rank_xp')::int = 600, 'the next rank is announced';
  assert v_state -> 'mastery' -> 'bonuses' = '{}'::jsonb, 'an apprentice has no bonus';
end;
$$;

select pg_temp.expect_refusal($$select public.choose_talent('cadence')$$, 'Il faut être compagnon');
select pg_temp.expect_refusal($$select public.choose_talent('bat')$$, 'pas de ton métier');
select pg_temp.expect_refusal($$select public.install_fitting('bachage')$$, 'ne voyage pas');

reset role;
update public.players set coins = 20000 where player_id = 'cccccccc-cccc-cccc-cccc-cccccccccccc';
set role authenticated;
select public.buy_goods('fer', 20) is not null;
select public.buy_goods('charbon', 10) is not null;
select public.start_production('outils', 3) is not null;
reset role;
select pg_temp.finish_production('cccccccc-cccc-cccc-cccc-cccccccccccc');
set role authenticated;

do $$
declare
  v_state jsonb := public.get_state();
begin
  assert pg_temp.stored(v_state, 'outils') = 3, 'the batches are delivered';
  assert (v_state -> 'mastery' ->> 'xp')::int = 30, 'each batch gives its recipe minutes as experience';
  v_state := public.get_state();
  assert (v_state -> 'mastery' ->> 'xp')::int = 30, 'experience is only given once';
end;
$$;

reset role;
select pg_temp.set_xp('cccccccc-cccc-cccc-cccc-cccccccccccc', 595);
set role authenticated;
select public.start_production('outils', 1) is not null;
reset role;
select pg_temp.finish_production('cccccccc-cccc-cccc-cccc-cccccccccccc');
set role authenticated;

do $$
declare
  v_state jsonb := public.get_state();
begin
  assert v_state -> 'mastery' ->> 'rank' = 'compagnon', 'enough experience makes a journeyman';
  assert (v_state -> 'mastery' ->> 'next_rank_xp')::int = 3000, 'the master rank comes next';
  assert exists (select 1 from jsonb_array_elements(v_state -> 'journal') entry
    where entry ->> 'kind_id' = 'rang' and entry ->> 'detail' like 'Tu deviens compagnon forgeron%'), 'the new rank is written in the journal';
  v_state := public.choose_talent('cadence');
  assert v_state -> 'mastery' -> 'talents' = '["cadence"]'::jsonb, 'the journeyman chooses a talent';
  assert (v_state -> 'mastery' -> 'bonuses' ->> 'speed')::numeric = 0.15, 'the talent gives its bonus';
  assert (v_state -> 'workshop' ->> 'speed')::numeric = 1.15, 'the workshop works faster';
end;
$$;

select pg_temp.expect_refusal($$select public.choose_talent('econome')$$, 'déjà choisi ton talent de compagnon');
select pg_temp.expect_refusal($$select public.choose_talent('main_sure')$$, 'Il faut être maître');

reset role;
update private.settings set journeyman_masterpiece_chance = 1;
set role authenticated;

do $$
declare
  v_state jsonb := public.start_production('outils', 2);
begin
  assert (v_state -> 'workshop' ->> 'batch_seconds')::int = 522, 'the cadence talent shortens batches';
end;
$$;

reset role;
select pg_temp.finish_production('cccccccc-cccc-cccc-cccc-cccccccccccc');
set role authenticated;

do $$
declare
  v_state jsonb := public.get_state();
  v_piece jsonb := v_state -> 'masterpieces' -> 0;
  v_coins bigint := (v_state -> 'player' ->> 'coins')::bigint;
begin
  assert jsonb_array_length(v_state -> 'masterpieces') = 2, 'a journeyman can sign masterpieces';
  assert v_piece ->> 'maker' = 'Xavier' and v_piece ->> 'good_id' = 'outils', 'the masterpiece is signed by its maker';
  assert (v_piece ->> 'price')::int = (select ceil(base_price * 10) from public.markets where town_id = 'ferrenoire' and good_id = 'outils'),
    'a masterpiece is worth ten times the local base price';
  assert exists (select 1 from jsonb_array_elements(v_state -> 'journal') entry where entry ->> 'kind_id' = 'chef_oeuvre'), 'the masterpiece is written in the journal';

  v_state := public.sell_masterpiece((v_piece ->> 'id')::bigint);
  assert (v_state -> 'player' ->> 'coins')::bigint = v_coins + (v_piece ->> 'price')::bigint, 'the masterpiece is sold';
  assert jsonb_array_length(v_state -> 'masterpieces') = 1, 'the sold masterpiece is gone';
end;
$$;

reset role;
update private.settings set journeyman_masterpiece_chance = 0;
set role authenticated;

select pg_temp.as_player('ffffffff-ffff-ffff-ffff-ffffffffffff');
select public.found_player('Willa', 'forgeron', 'ferrenoire') is not null;
reset role;
select pg_temp.set_xp('ffffffff-ffff-ffff-ffff-ffffffffffff', 600);
update public.players set coins = 20000 where player_id = 'ffffffff-ffff-ffff-ffff-ffffffffffff';
set role authenticated;
select public.choose_talent('econome') is not null;
select public.buy_goods('fer', 20) is not null;
select public.buy_goods('charbon', 10) is not null;

do $$
declare
  v_state jsonb := public.start_production('outils', 99);
begin
  assert (v_state -> 'workshop' ->> 'queued')::int = 6, 'the queue is still limited';
  assert pg_temp.stored(v_state, 'fer') between 9 and 10 and pg_temp.stored(v_state, 'charbon') between 4 and 5,
    'the thrifty smith saves 15 % of the raw materials on average';
end;
$$;

select pg_temp.as_player('dddddddd-dddd-dddd-dddd-dddddddddddd');
select public.found_player('Yann', 'caravanier', 'port-sable') is not null;
reset role;
update public.players set coins = 20000 where player_id = 'dddddddd-dddd-dddd-dddd-dddddddddddd';
set role authenticated;
select public.depart('hautecombe') is not null;
reset role;
select pg_temp.arrive('dddddddd-dddd-dddd-dddd-dddddddddddd');
set role authenticated;

do $$
declare
  v_state jsonb := public.get_state();
begin
  assert (v_state -> 'mastery' ->> 'xp')::int = 30, 'a finished trip gives its minutes as experience';
  v_state := public.get_state();
  assert (v_state -> 'mastery' ->> 'xp')::int = 30, 'a trip is only rewarded once';
end;
$$;

reset role;
select pg_temp.set_xp('dddddddd-dddd-dddd-dddd-dddddddddddd', 600);
set role authenticated;

do $$
declare
  v_state jsonb := public.choose_talent('bat');
begin
  assert (v_state -> 'caravan' ->> 'capacity')::int = 55, 'the packing talent adds 15 places to the hold';
end;
$$;

select pg_temp.expect_refusal($$select public.install_fitting('bachage')$$, 'Il te manque 4 bâches');
select pg_temp.expect_refusal($$select public.attach_wagon()$$, 'chariot du charron');
select pg_temp.expect_refusal($$select public.buy_goods('chariot', 1)$$, 'Le marché n''en vend pas');
reset role;
select pg_temp.give_cargo('dddddddd-dddd-dddd-dddd-dddddddddddd', 'baches', 4);
select pg_temp.give_cargo('dddddddd-dddd-dddd-dddd-dddddddddddd', 'roues', 4);
select pg_temp.give_cargo('dddddddd-dddd-dddd-dddd-dddddddddddd', 'ferrures', 8);
select pg_temp.give_cargo('dddddddd-dddd-dddd-dddd-dddddddddddd', 'chariot', 1);
set role authenticated;

do $$
declare
  v_state jsonb := public.install_fitting('bachage');
begin
  assert v_state -> 'fittings' = '["bachage"]'::jsonb, 'the tarp is installed';
  assert pg_temp.loaded(v_state, 'baches') = 0, 'the tarps are used up';
  v_state := public.install_fitting('roues_cerclees');
  assert (v_state -> 'mastery' -> 'bonuses' ->> 'swift')::numeric = 0.1, 'the iron wheels speed up the caravan';
  v_state := public.attach_wagon();
  assert (v_state -> 'caravan' ->> 'wagons')::int = 2, 'the wheelwright wagon is attached';
  assert (v_state -> 'caravan' ->> 'capacity')::int = 75, 'the new wagon adds 20 places';
  assert pg_temp.loaded(v_state, 'chariot') = 0, 'the wagon leaves the hold';
end;
$$;

reset role;
insert into public.warehouses (player_id, town_id, good_id, quantity)
select player_id, town_id, 'chariot', 1 from public.caravans where player_id = 'dddddddd-dddd-dddd-dddd-dddddddddddd';
select pg_temp.give_cargo('dddddddd-dddd-dddd-dddd-dddddddddddd', 'chariot', 1);
set role authenticated;

do $$
declare
  v_state jsonb := public.attach_wagon();
begin
  assert (v_state -> 'caravan' ->> 'wagons')::int = 3, 'a wagon waiting in the town warehouse is attached';
  assert pg_temp.stored(v_state, 'chariot') = 0, 'the warehouse wagon goes first';
  assert pg_temp.loaded(v_state, 'chariot') = 1, 'the wagon in the hold stays for later';
end;
$$;

select pg_temp.expect_refusal($$select public.install_fitting('bachage')$$, 'déjà cet équipement');

select public.buy_goods('sel', 20) is not null;
reset role;
update private.settings set events_enabled = true, road_event_minutes = 1;
update public.road_event_odds set weight = case when kind_id = 'orage' then 1 else 0 end;
set role authenticated;

do $$
declare
  v_state jsonb := public.depart('port-sable', 'rapidite');
  v_trip interval := (v_state -> 'caravan' ->> 'arrives_at')::timestamptz - (v_state -> 'caravan' ->> 'departed_at')::timestamptz;
begin
  assert v_trip = interval '27 minutes', 'iron wheels shorten the trip by 10 %';
end;
$$;

reset role;
update public.pending_events set occurs_at = now() - interval '1 second' where player_id = 'dddddddd-dddd-dddd-dddd-dddddddddddd';
set role authenticated;

do $$
declare
  v_state jsonb := public.answer_event('forcer');
begin
  assert pg_temp.loaded(v_state, 'sel') = 20, 'the tarp keeps the goods dry';
  assert v_state -> 'journal' -> 0 ->> 'detail' like '%sans dommage%', 'the journal tells the goods stayed dry';
end;
$$;

reset role;
update private.settings set events_enabled = false;
select pg_temp.arrive('dddddddd-dddd-dddd-dddd-dddddddddddd');
create temp table quotes (who text primary key, buy numeric, sell numeric);
grant all on quotes to authenticated;
set role authenticated;

insert into quotes
select 'yann', pg_temp.quote(state, 'epices', 'buy_price'), pg_temp.quote(state, 'epices', 'sell_price')
from (select public.get_state() as state) current;
select public.post_offer('sel', 5, null, 50) is not null;

select pg_temp.as_player('eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee');
select public.found_player('Zoé', 'negociant', 'port-sable') is not null;
reset role;
update public.players set coins = 20000 where player_id = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee';
select pg_temp.set_xp('eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee', 600);
set role authenticated;

do $$
declare
  v_state jsonb;
begin
  perform public.buy_goods('sel', 30);
  v_state := public.sell_goods('sel', 30);
  assert (v_state -> 'mastery' ->> 'xp')::int > 600, 'selling at the market trains the merchant';
end;
$$;

do $$
declare
  v_state jsonb := public.choose_talent('negoce');
begin
  insert into quotes values ('zoe', pg_temp.quote(v_state, 'epices', 'buy_price'), pg_temp.quote(v_state, 'epices', 'sell_price'));
  assert (select buy from quotes where who = 'zoe') < (select buy from quotes where who = 'yann'), 'the haggling merchant buys cheaper';
  assert (select sell from quotes where who = 'zoe') > (select sell from quotes where who = 'yann'), 'the haggling merchant sells dearer';
end;
$$;

do $$
declare
  v_before int := (public.get_state() -> 'mastery' ->> 'xp')::int;
  v_state jsonb := public.accept_offer((select (offer ->> 'id')::bigint from jsonb_array_elements(public.get_state() -> 'offers') offer));
begin
  assert (v_state -> 'mastery' ->> 'xp')::int between v_before + 2 and v_before + 3, 'a concluded offer gives 1 experience per 20 coins traded';
end;
$$;

select pg_temp.as_player('dddddddd-dddd-dddd-dddd-dddddddddddd');
do $$
begin
  assert (select count(*) from public.masterpieces) = 0, 'a player cannot read the masterpieces of others';
  assert (select count(*) from public.masteries) = 1, 'a player only reads their own mastery';
end;
$$;

reset role;
update public.road_event_odds set weight = 1;
update private.settings set road_event_minutes = 150, journeyman_masterpiece_chance = 0.02, master_masterpiece_chance = 0.06;

select 'All mastery tests passed' as result;
