\set ON_ERROR_STOP on

insert into auth.users (id) values
  ('11111111-1111-1111-1111-111111111111'),
  ('22222222-2222-2222-2222-222222222222');

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

grant execute on function pg_temp.expect_refusal(text, text) to authenticated;

set role anon;
do $$
begin
  perform public.get_state();
  raise exception 'anon must not call get_state' using errcode = 'XX000';
exception when insufficient_privilege then
  null;
end;
$$;
reset role;

set role authenticated;
select set_config('request.jwt.claim.sub', '11111111-1111-1111-1111-111111111111', false);

do $$
declare
  v_world jsonb := public.get_world();
  v_state jsonb := public.get_state();
begin
  assert jsonb_array_length(v_world -> 'goods') = 18, 'world lists every raw and crafted good';
  assert jsonb_array_length(v_world -> 'crafts') = 6, 'world lists every craft';
  assert jsonb_array_length(v_world -> 'recipes') = 8, 'world lists every recipe';
  assert jsonb_array_length(v_world -> 'towns') = 8, 'world lists every town';
  assert jsonb_array_length(v_world -> 'routes') = 24, 'every road is travelable both ways';
  assert (select town -> 'produces' ? 'sel' from jsonb_array_elements(v_world -> 'towns') town where town ->> 'id' = 'port-sable'),
    'Port-Sable produces salt';
  assert (select town -> 'demands' ? 'sel' from jsonb_array_elements(v_world -> 'towns') town where town ->> 'id' = 'hautecombe'),
    'Hautecombe demands salt';
  assert v_state -> 'player' = 'null'::jsonb, 'a new account has not settled yet';
end;
$$;

select pg_temp.expect_refusal($$select public.found_player('ab', 'caravanier', 'port-sable')$$, 'entre 3 et 20');
select pg_temp.expect_refusal($$select public.found_player('Robin', 'negociant', 'port-sable')$$, 'pas encore disponible');
select pg_temp.expect_refusal($$select public.found_player('Robin', 'caravanier', 'atlantide')$$, 'n''existe pas');
select pg_temp.expect_refusal($$select public.buy_goods('sel', 1)$$, 'Installe-toi d''abord');

do $$
declare
  v_state jsonb := public.found_player('  Robin  ', 'caravanier', 'port-sable');
begin
  assert v_state -> 'player' ->> 'name' = 'Robin', 'the name is trimmed';
  assert (v_state -> 'player' ->> 'coins')::int = 400, 'a player starts with 400 coins';
  assert v_state -> 'workshop' = 'null'::jsonb, 'a caravan driver has no workshop';
  assert (v_state -> 'caravan' ->> 'capacity')::int = 40, 'one wagon carries 40 goods';
  assert v_state -> 'caravan' ->> 'town_id' = 'port-sable', 'the caravan starts in the chosen town';
  assert jsonb_array_length(v_state -> 'market') = 18, 'the town market is visible on arrival';
end;
$$;

select pg_temp.expect_refusal($$select public.found_player('Autre', 'forgeron', 'ferrenoire')$$, 'déjà installé');
select pg_temp.expect_refusal($$select public.start_production('outils', 1)$$, 'pas d''atelier');
select pg_temp.expect_refusal($$select public.upgrade_workshop()$$, 'pas d''atelier');

select set_config('request.jwt.claim.sub', '22222222-2222-2222-2222-222222222222', false);
select pg_temp.expect_refusal($$select public.found_player('ROBIN', 'forgeron', 'ferrenoire')$$, 'déjà pris');
do $$
begin
  assert (select count(*) from public.caravans) = 0, 'a player cannot read another caravan';
  assert (select count(*) from public.players) = 0, 'a player cannot read another profile';
end;
$$;

select set_config('request.jwt.claim.sub', '11111111-1111-1111-1111-111111111111', false);

do $$
declare
  v_before jsonb := public.get_state();
  v_quote numeric := (select (quote ->> 'buy_price')::numeric from jsonb_array_elements(v_before -> 'market') quote where quote ->> 'good_id' = 'sel');
  v_after jsonb := public.buy_goods('sel', 10);
  v_spent int := (v_before -> 'player' ->> 'coins')::int - (v_after -> 'player' ->> 'coins')::int;
  v_next_quote numeric := (select (quote ->> 'buy_price')::numeric from jsonb_array_elements(v_after -> 'market') quote where quote ->> 'good_id' = 'sel');
begin
  assert (v_after -> 'cargo' -> 0 ->> 'quantity')::int = 10, 'ten salt are loaded';
  assert v_spent between floor(v_quote * 10) and ceil(v_quote * 10 * 1.08), 'ten units cost about ten times the quote';
  assert v_next_quote > v_quote, 'buying makes the good dearer';
end;
$$;

do $$
declare
  v_state jsonb := public.buy_goods('sel', 1000);
begin
  assert (v_state -> 'cargo' -> 0 ->> 'quantity')::int = 40, 'buying is capped by the free space in the hold';
end;
$$;

select pg_temp.expect_refusal($$select public.buy_goods('poisson', 1)$$, 'cale est pleine');
select pg_temp.expect_refusal($$select public.sell_goods('vin', 1)$$, 'pas cette marchandise');
select pg_temp.expect_refusal($$select public.depart('ferrenoire')$$, 'Aucune route');

do $$
declare
  v_state jsonb := public.depart('hautecombe');
begin
  assert v_state -> 'caravan' ->> 'town_id' = 'hautecombe', 'the destination becomes the caravan town';
  assert v_state -> 'caravan' ->> 'from_town_id' = 'port-sable', 'the departure town is remembered';
  assert (v_state -> 'caravan' ->> 'arrives_at')::timestamptz > now() + interval '29 minutes', 'the trip lasts thirty minutes';
  assert jsonb_array_length(v_state -> 'market') = 0, 'no market is visible on the road';
end;
$$;

select pg_temp.expect_refusal($$select public.sell_goods('sel', 1)$$, 'encore en route');
select pg_temp.expect_refusal($$select public.depart('port-sable')$$, 'encore en route');

reset role;
update public.caravans set arrives_at = now() - interval '1 second' where player_id = '11111111-1111-1111-1111-111111111111';
set role authenticated;

do $$
declare
  v_before jsonb := public.get_state();
  v_after jsonb := public.sell_goods('sel', 1000);
begin
  assert jsonb_array_length(v_after -> 'cargo') = 0, 'selling more than owned sells everything';
  assert (v_after -> 'player' ->> 'coins')::int - (v_before -> 'player' ->> 'coins')::int > 400,
    'salt from Port-Sable sells at a profit in Hautecombe';
end;
$$;

do $$
declare
  v_state jsonb := public.buy_wagon();
begin
  assert (v_state -> 'caravan' ->> 'wagons')::int = 2, 'a second wagon is attached';
  assert (v_state -> 'caravan' ->> 'capacity')::int = 60, 'each wagon adds 20 slots';
  assert (v_state -> 'caravan' ->> 'next_wagon_price')::int = 510, 'the next wagon costs more';
  assert (v_state -> 'player' ->> 'coins')::int > 0, 'the wagon is paid from the player purse';
end;
$$;

reset role;

do $$
declare
  v_market public.markets;
begin
  select * into v_market from public.markets where town_id = 'hautecombe' and good_id = 'sel';
  assert v_market.pressure > 0, 'selling floods the market';
  assert private.current_pressure(v_market.pressure, now() - interval '8 hours') < v_market.pressure / 5,
    'markets recover within a few hours';
end;
$$;

update private.settings set travel_time_factor = 0.01;
set role authenticated;
do $$
declare
  v_state jsonb := public.depart('rocheclaire');
begin
  assert (v_state -> 'caravan' ->> 'arrives_at')::timestamptz < now() + interval '30 seconds',
    'the travel time factor shortens trips for playtests';
end;
$$;
reset role;

select 'All caravan tests passed' as result;
