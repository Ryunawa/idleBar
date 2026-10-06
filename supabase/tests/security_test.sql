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
reset role;

set role authenticated;
select set_config('request.jwt.claim.sub', '33333333-3333-3333-3333-333333333333', false);
select public.found_player('Aldo', 'caravanier', 'port-sable');
select public.buy_goods('sel', 5);

do $$
begin
  assert (select count(*) from public.goods) = 18, 'players read the goods';
  assert (select count(*) from public.crafts) = 6, 'players read the crafts';
  assert (select count(*) from public.recipes) = 8, 'players read the recipes';
  assert (select count(*) from public.recipe_inputs) = 14, 'players read the recipe inputs';
  assert (select count(*) from public.players) = 1, 'a player reads their own profile';
  assert (select count(*) from public.towns) = 8, 'players read the towns';
  assert (select count(*) from public.routes) = 24, 'players read the routes';
  assert (select count(*) from public.markets) = 144, 'players read the markets';
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

select set_config('request.jwt.claim.sub', '44444444-4444-4444-4444-444444444444', false);
do $$
begin
  assert (select count(*) from public.caravans) = 0, 'another player cannot read the caravan';
  assert (select count(*) from public.cargo) = 0, 'another player cannot read the cargo';
  assert (select count(*) from public.players) = 0, 'another player cannot read the profile';
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
