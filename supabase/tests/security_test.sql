\set ON_ERROR_STOP on

insert into auth.users (id) values
  ('33333333-3333-3333-3333-333333333333'),
  ('44444444-4444-4444-4444-444444444444');

create function pg_temp.expect_denied(p_statement text) returns void
language plpgsql
as $$
begin
  execute p_statement;
  raise exception 'Expected "%" to be denied to %', p_statement, current_user using errcode = 'XX000';
exception
  when insufficient_privilege then
    null;
end;
$$;

grant execute on function pg_temp.expect_denied(text) to anon, authenticated;

set role anon;
select pg_temp.expect_denied('select * from public.goods');
select pg_temp.expect_denied('select * from public.towns');
select pg_temp.expect_denied('select * from public.routes');
select pg_temp.expect_denied('select * from public.markets');
select pg_temp.expect_denied('select * from public.caravans');
select pg_temp.expect_denied('select * from public.cargo');
select pg_temp.expect_denied('select * from public.players');
select pg_temp.expect_denied('select * from public.warehouses');
select pg_temp.expect_denied('select * from public.workshops');
select pg_temp.expect_denied('select * from public.crafts');
select pg_temp.expect_denied('select * from public.recipes');
select pg_temp.expect_denied('select * from public.recipe_inputs');
select pg_temp.expect_denied('select * from public.branches');
select pg_temp.expect_denied('select * from public.offers');
select pg_temp.expect_denied('select * from public.transport_contracts');
select pg_temp.expect_denied('select * from public.event_kinds');
select pg_temp.expect_denied('select * from public.event_choices');
select pg_temp.expect_denied('select * from public.road_event_odds');
select pg_temp.expect_denied('select * from public.standing_orders');
select pg_temp.expect_denied('select * from public.pending_events');
select pg_temp.expect_denied('select * from public.special_orders');
select pg_temp.expect_denied('select * from public.event_log');
select pg_temp.expect_denied('select * from public.talents');
select pg_temp.expect_denied('select * from public.fittings');
select pg_temp.expect_denied('select * from public.fitting_costs');
select pg_temp.expect_denied('select * from public.masteries');
select pg_temp.expect_denied('select * from public.player_talents');
select pg_temp.expect_denied('select * from public.masterpieces');
select pg_temp.expect_denied('select * from public.caravan_fittings');
select pg_temp.expect_denied('select * from public.directives');
select pg_temp.expect_denied('select * from public.directive_choices');
select pg_temp.expect_denied('select * from public.trip_events');
select pg_temp.expect_denied('select * from private.settings');
select pg_temp.expect_denied('select public.get_world()');
select pg_temp.expect_denied('select public.get_state()');
select pg_temp.expect_denied($$select public.found_player('Intrus', 'forgeron', 'ferrenoire')$$);
select pg_temp.expect_denied($$select public.start_production('outils', 1)$$);
select pg_temp.expect_denied('select public.upgrade_workshop()');
select pg_temp.expect_denied($$select public.buy_goods('sel', 1)$$);
select pg_temp.expect_denied($$select public.sell_goods('sel', 1)$$);
select pg_temp.expect_denied('select public.buy_wagon()');
select pg_temp.expect_denied($$select public.depart('hautecombe')$$);
select pg_temp.expect_denied($$select public.deposit_goods('sel', 1)$$);
select pg_temp.expect_denied($$select public.withdraw_goods('sel', 1)$$);
select pg_temp.expect_denied($$select public.open_branch('hautecombe')$$);
select pg_temp.expect_denied($$select public.post_offer('sel', 1, null, 10)$$);
select pg_temp.expect_denied('select public.cancel_offer(1)');
select pg_temp.expect_denied('select public.accept_offer(1)');
select pg_temp.expect_denied($$select public.post_contract('sel', 1, 'hautecombe', 10)$$);
select pg_temp.expect_denied('select public.cancel_contract(1)');
select pg_temp.expect_denied('select public.accept_contract(1)');
select pg_temp.expect_denied('select public.mark_exchanges_seen()');
select pg_temp.expect_denied($$select public.set_standing_order('bandits', 'fuir')$$);
select pg_temp.expect_denied('select public.fulfill_special_order(1)');
select pg_temp.expect_denied('select public.decline_special_order(1)');
select pg_temp.expect_denied('select public.mark_journal_seen()');
select pg_temp.expect_denied($$select public.choose_talent('cadence')$$);
select pg_temp.expect_denied('select public.sell_masterpiece(1)');
select pg_temp.expect_denied($$select public.install_fitting('bachage')$$);
select pg_temp.expect_denied('select public.attach_wagon()');
select pg_temp.expect_denied($$select public.answer_event('payer')$$);
reset role;

set role authenticated;
select set_config('request.jwt.claim.sub', '33333333-3333-3333-3333-333333333333', false);
select public.found_player('Aldo', 'caravanier', 'port-sable');
select public.buy_goods('sel', 5);
select public.post_offer('sel', 2, null, 30);

do $$
begin
  assert (select count(*) from public.goods) = 19, 'players read the goods';
  assert (select count(*) from public.crafts) = 7, 'players read the crafts';
  assert (select count(*) from public.recipes) = 9, 'players read the recipes';
  assert (select count(*) from public.recipe_inputs) = 17, 'players read the recipe inputs';
  assert (select count(*) from public.players) = 1, 'a player reads their own profile';
  assert (select count(*) from public.towns) = 8, 'players read the towns';
  assert (select count(*) from public.routes) = 24, 'players read the routes';
  assert (select count(*) from public.markets) = 152, 'players read the markets';
  assert (select count(*) from public.caravans) = 1, 'a player reads their own caravan';
  assert (select count(*) from public.cargo) = 1, 'a player reads their own cargo';
end;
$$;

select pg_temp.expect_denied($$insert into public.goods (id, name, base_price, sort_order) values ('or', 'Or', 1, 99)$$);
select pg_temp.expect_denied($$update public.towns set name = 'Pirate' where id = 'port-sable'$$);
select pg_temp.expect_denied($$delete from public.routes$$);
select pg_temp.expect_denied($$update public.markets set pressure = -300$$);
select pg_temp.expect_denied($$update public.players set coins = 999999$$);
select pg_temp.expect_denied($$update public.players set craft_id = 'forgeron'$$);
select pg_temp.expect_denied($$insert into public.workshops (player_id, town_id) values (auth.uid(), 'ferrenoire')$$);
select pg_temp.expect_denied($$insert into public.warehouses (player_id, town_id, good_id, quantity) values (auth.uid(), 'ferrenoire', 'outils', 99)$$);
select pg_temp.expect_denied($$update public.recipes set seconds = 1$$);
select pg_temp.expect_denied($$update public.crafts set playable = true$$);
select pg_temp.expect_denied($$update public.caravans set arrives_at = null$$);
select pg_temp.expect_denied($$delete from public.caravans$$);
select pg_temp.expect_denied($$insert into public.cargo (player_id, good_id, quantity) values (auth.uid(), 'epices', 500)$$);
select pg_temp.expect_denied($$update public.cargo set quantity = 500$$);
select pg_temp.expect_denied($$delete from public.cargo$$);
select pg_temp.expect_denied('select * from private.settings');
select pg_temp.expect_denied('update private.settings set starting_coins = 1000000');
select pg_temp.expect_denied('select private.capacity(100)');
select pg_temp.expect_denied('select private.lock_caravan_in_town()');
select pg_temp.expect_denied($$select private.unit_price(1, 0)$$);
select pg_temp.expect_denied('select private.lock_workshop()');
select pg_temp.expect_denied($$select private.change_warehouse(auth.uid(), 'ferrenoire', 'outils', 99)$$);
select pg_temp.expect_denied($$insert into public.branches (player_id, town_id) values (auth.uid(), 'ferrenoire')$$);
select pg_temp.expect_denied($$insert into public.offers (town_id, seller_id, give_good_id, give_quantity, want_quantity, expires_at)
  values ('port-sable', auth.uid(), 'epices', 500, 1, now() + interval '1 day')$$);
select pg_temp.expect_denied($$update public.offers set status = 'open', closed_at = null$$);
select pg_temp.expect_denied($$delete from public.offers$$);
select pg_temp.expect_denied($$insert into public.transport_contracts
  (shipper_id, origin_town_id, destination_town_id, good_id, quantity, reward, game_fee, deposit, takeover_at, game_arrives_at)
  values (auth.uid(), 'port-sable', 'hautecombe', 'epices', 500, 1, 1, 1, now(), now())$$);
select pg_temp.expect_denied($$update public.transport_contracts set status = 'delivered'$$);
select pg_temp.expect_denied($$delete from public.transport_contracts$$);
select pg_temp.expect_denied($$select private.settle_player(auth.uid())$$);
select pg_temp.expect_denied($$select private.credit(auth.uid(), 'port-sable', null, 99999)$$);
select pg_temp.expect_denied($$select private.exchange_state(auth.uid(), array['port-sable'])$$);
select pg_temp.expect_denied($$select private.journey_minutes('port-sable', 'ambrevault')$$);
select pg_temp.expect_denied('select * from public.pending_events');
select pg_temp.expect_denied($$insert into public.pending_events (player_id, kind_id, occurs_at) values (auth.uid(), 'trouvaille', now())$$);
select pg_temp.expect_denied($$insert into public.event_log (player_id, kind_id, happened_at, title, detail, tone) values (auth.uid(), 'trouvaille', now(), 'x', 'x', 'good')$$);
select pg_temp.expect_denied($$update public.event_log set seen = true$$);
select pg_temp.expect_denied($$insert into public.special_orders (player_id, town_id, good_id, quantity, unit_price, deadline) values (auth.uid(), 'port-sable', 'epices', 1, 99999, now())$$);
select pg_temp.expect_denied($$insert into public.standing_orders (player_id, kind_id, choice_id) values (auth.uid(), 'bandits', 'fuir')$$);
select pg_temp.expect_denied($$update public.event_choices set description = 'x'$$);
select pg_temp.expect_denied($$update public.road_event_odds set weight = 0$$);
select pg_temp.expect_denied($$select private.settle_trip(auth.uid())$$);
select pg_temp.expect_denied($$select private.roll_trip_event(auth.uid(), now(), now(), 'plain', 1)$$);
select pg_temp.expect_denied($$select private.log_event(auth.uid(), 'trouvaille', now(), 'good', 'x')$$);
select pg_temp.expect_denied($$insert into public.masteries (player_id, craft_id, xp) values (auth.uid(), 'caravanier', 99999)$$);
select pg_temp.expect_denied($$update public.masteries set xp = 99999$$);
select pg_temp.expect_denied($$insert into public.player_talents (player_id, talent_id) values (auth.uid(), 'bat')$$);
select pg_temp.expect_denied($$insert into public.masterpieces (owner_id, maker_name, craft_id, good_id, town_id) values (auth.uid(), 'x', 'forgeron', 'outils', 'port-sable')$$);
select pg_temp.expect_denied($$update public.masterpieces set sold_at = null$$);
select pg_temp.expect_denied($$insert into public.caravan_fittings (player_id, fitting_id) values (auth.uid(), 'bachage')$$);
select pg_temp.expect_denied($$update public.talents set amount = 100$$);
select pg_temp.expect_denied($$update public.fittings set amount = 100$$);
select pg_temp.expect_denied($$select private.gain_xp(auth.uid(), 99999)$$);
select pg_temp.expect_denied($$select private.bonus(auth.uid(), 'haggle')$$);
select pg_temp.expect_denied($$insert into public.trip_events (player_id, kind_id, occurred_at, decide_by, planned_seconds) values (auth.uid(), 'orage', now(), now(), 0)$$);
select pg_temp.expect_denied($$delete from public.trip_events$$);
select pg_temp.expect_denied($$update public.caravans set directive_id = 'economie'$$);
select pg_temp.expect_denied($$update public.directive_choices set choice_id = 'fuir'$$);
select pg_temp.expect_denied($$select private.apply_road_event(auth.uid(), 'trouvaille', null, 0, now())$$);
select pg_temp.expect_denied($$select private.directive_choice(auth.uid(), 'orage')$$);

select set_config('request.jwt.claim.sub', '44444444-4444-4444-4444-444444444444', false);
do $$
begin
  assert (select count(*) from public.caravans) = 0, 'another player cannot read the caravan';
  assert (select count(*) from public.cargo) = 0, 'another player cannot read the cargo';
  assert (select count(*) from public.players) = 0, 'another player cannot read the profile';
  assert (select count(*) from public.offers) = 0, 'another player cannot read the offer rows';
  assert public.get_state() -> 'player' = 'null'::jsonb, 'get_state only returns the caller player';
end;
$$;

select set_config('request.jwt.claim.sub', '', false);
do $$
begin
  perform public.get_state();
  raise exception 'get_state must refuse a request without user' using errcode = 'XX000';
exception when raise_exception then
  assert sqlerrm like '%Connexion requise%', 'an anonymous token is refused by get_state';
end;
$$;
reset role;

do $$
declare
  v_table record;
begin
  for v_table in
    select namespace.nspname, class.relname
    from pg_class class
    join pg_namespace namespace on namespace.oid = class.relnamespace
    where class.relkind = 'r' and namespace.nspname in ('public', 'private') and not class.relrowsecurity
  loop
    raise exception 'Row level security is disabled on %.%', v_table.nspname, v_table.relname using errcode = 'XX000';
  end loop;
end;
$$;

select 'All security tests passed' as result;
