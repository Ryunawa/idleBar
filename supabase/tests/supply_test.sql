\set ON_ERROR_STOP on

insert into auth.users (id) values
  ('f6f6f6f6-f6f6-f6f6-f6f6-f6f6f6f6f6f6'),
  ('a7a7a7a7-a7a7-a7a7-a7a7-a7a7a7a7a7a7');

update private.settings set travel_time_factor = 1, craft_time_factor = 1, events_enabled = false;

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

create function pg_temp.supply(p_state jsonb, p_town_id text, p_good_id text) returns jsonb
language sql
as $$
  select request from jsonb_array_elements(p_state -> 'supply_requests') request
  where request ->> 'town_id' = p_town_id and request ->> 'want_good_id' = p_good_id;
$$;

create function pg_temp.expect_failure(p_statement text, p_message text) returns void
language plpgsql
as $$
begin
  execute p_statement;
  raise exception 'Expected "%" to fail', p_statement using errcode = 'XX000';
exception
  when raise_exception then
    if sqlerrm not like p_message || '%' then
      raise exception 'Expected "%", got "%"', p_message, sqlerrm;
    end if;
end;
$$;

grant execute on function pg_temp.as_player(text), pg_temp.coins(jsonb), pg_temp.supply(jsonb, text, text), pg_temp.expect_failure(text, text)
to authenticated;

set role authenticated;

select pg_temp.as_player('f6f6f6f6-f6f6-f6f6-f6f6-f6f6f6f6f6f6');

do $$
declare
  v_state jsonb := public.found_player('Ysolde', 'herboriste', 'brumeval');
  v_after jsonb := public.post_offer(null, 200, 'herbes', 10);
begin
  assert pg_temp.coins(v_after) = pg_temp.coins(v_state) - 200, 'an order blocks the coins it offers';
  assert pg_temp.supply(v_after, 'brumeval', 'herbes') is null, 'an artisan does not see their own order';
end;
$$;

select pg_temp.as_player('a7a7a7a7-a7a7-a7a7-a7a7-a7a7a7a7a7a7');

do $$
declare
  v_state jsonb := public.found_player('Aymon', 'caravanier', 'port-sable');
  v_order jsonb := pg_temp.supply(v_state, 'brumeval', 'herbes');
begin
  assert v_order is not null, 'a caravan sees an order posted in another town';
  assert (v_order ->> 'want_quantity')::int = 10 and (v_order ->> 'give_quantity')::int = 200, 'the order states quantity and payment';
  assert v_order ->> 'seller' = 'Ysolde', 'the order names who posted it';
  assert not exists (select 1 from jsonb_array_elements(v_state -> 'offers') offer where offer ->> 'town_id' = 'brumeval'),
    'the counter still only lists the offers of the current town';
end;
$$;

do $$
declare
  v_order bigint := (pg_temp.supply(public.get_state(), 'brumeval', 'herbes') ->> 'id')::bigint;
begin
  perform pg_temp.expect_failure(format('select public.accept_offer(%s)', v_order), 'Ta caravane n''est pas dans cette ville');
end;
$$;

reset role;
update public.caravans set town_id = 'brumeval', from_town_id = null, departed_at = null, arrives_at = null
where player_id = 'a7a7a7a7-a7a7-a7a7-a7a7-a7a7a7a7a7a7';
insert into public.cargo (player_id, good_id, quantity) values ('a7a7a7a7-a7a7-a7a7-a7a7-a7a7a7a7a7a7', 'herbes', 6);
insert into public.warehouses (player_id, town_id, good_id, quantity) values ('a7a7a7a7-a7a7-a7a7-a7a7-a7a7a7a7a7a7', 'brumeval', 'herbes', 3);
set role authenticated;

do $$
declare
  v_order bigint := (pg_temp.supply(public.get_state(), 'brumeval', 'herbes') ->> 'id')::bigint;
begin
  perform pg_temp.expect_failure(format('select public.accept_offer(%s)', v_order), 'Il te manque de la marchandise');
end;
$$;

reset role;
update public.warehouses set quantity = 4
where player_id = 'a7a7a7a7-a7a7-a7a7-a7a7-a7a7a7a7a7a7' and town_id = 'brumeval' and good_id = 'herbes';
set role authenticated;

do $$
declare
  v_before jsonb := public.get_state();
  v_after jsonb := public.accept_offer((pg_temp.supply(v_before, 'brumeval', 'herbes') ->> 'id')::bigint);
begin
  assert pg_temp.coins(v_after) = pg_temp.coins(v_before) + 200, 'delivering an order pays its price';
  assert not exists (select 1 from jsonb_array_elements(v_after -> 'cargo') line where line ->> 'good_id' = 'herbes'),
    'the hold gives what the warehouse lacked';
  assert not exists (select 1 from jsonb_array_elements(v_after -> 'warehouses') line
      where line ->> 'town_id' = 'brumeval' and line ->> 'good_id' = 'herbes'),
    'the town warehouse is drawn first';
  assert pg_temp.supply(v_after, 'brumeval', 'herbes') is null, 'a delivered order leaves the list';
end;
$$;

select pg_temp.as_player('f6f6f6f6-f6f6-f6f6-f6f6-f6f6f6f6f6f6');

do $$
declare
  v_state jsonb := public.get_state();
begin
  assert exists (select 1 from jsonb_array_elements(v_state -> 'warehouses') line
      where line ->> 'town_id' = 'brumeval' and line ->> 'good_id' = 'herbes' and (line ->> 'quantity')::int = 10),
    'the goods arrive in the artisan''s warehouse';
  assert exists (select 1 from jsonb_array_elements(v_state -> 'my_offers') offer
      where offer ->> 'want_good_id' = 'herbes' and offer ->> 'status' = 'concluded' and offer ->> 'buyer' = 'Aymon'),
    'the artisan sees who delivered';
end;
$$;

reset role;
select 'All supply tests passed' as result;