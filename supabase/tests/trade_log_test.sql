\set ON_ERROR_STOP on

insert into auth.users (id) values
  ('b8b8b8b8-b8b8-b8b8-b8b8-b8b8b8b8b8b8'),
  ('c9c9c9c9-c9c9-c9c9-c9c9-c9c9c9c9c9c9');

update private.settings set travel_time_factor = 1, craft_time_factor = 1, events_enabled = false;

create function pg_temp.as_player(p_player_id text) returns void
language sql
as $$
  select set_config('request.jwt.claim.sub', p_player_id, false);
$$;

create function pg_temp.trades(p_state jsonb, p_good_id text, p_side text) returns jsonb
language sql
as $$
  select coalesce(jsonb_agg(trade), '[]'::jsonb) from jsonb_array_elements(p_state -> 'trades') trade
  where trade ->> 'good_id' = p_good_id and trade ->> 'side' = p_side;
$$;

create function pg_temp.coins(p_state jsonb) returns bigint
language sql
as $$
  select (p_state -> 'player' ->> 'coins')::bigint;
$$;

grant execute on function pg_temp.as_player(text), pg_temp.trades(jsonb, text, text), pg_temp.coins(jsonb) to authenticated;

set role authenticated;

select pg_temp.as_player('b8b8b8b8-b8b8-b8b8-b8b8-b8b8b8b8b8b8');

select public.found_player('Odilon', 'caravanier', 'port-sable') is not null;
reset role;
update public.players set coins = 5000 where player_id = 'b8b8b8b8-b8b8-b8b8-b8b8-b8b8b8b8b8b8';
set role authenticated;

do $$
declare
  v_start jsonb := public.get_state();
  v_first jsonb := public.buy_goods('sel', 5);
  v_second jsonb := public.buy_goods('sel', 3);
  v_bought jsonb := pg_temp.trades(v_second, 'sel', 'buy');
begin
  assert jsonb_array_length(v_bought) = 1, 'two quick purchases of the same good share one line';
  assert (v_bought -> 0 ->> 'quantity')::int = 8, 'the line adds up the quantities';
  assert (v_bought -> 0 ->> 'total')::bigint = pg_temp.coins(v_start) - pg_temp.coins(v_second), 'the line adds up what was paid';
  assert v_bought -> 0 ->> 'source' = 'marche' and v_bought -> 0 ->> 'town_id' = 'port-sable', 'the line names the market and the town';
end;
$$;

reset role;
update public.trade_log set updated_at = now() - interval '10 minutes' where player_id = 'b8b8b8b8-b8b8-b8b8-b8b8-b8b8b8b8b8b8';
set role authenticated;

do $$
declare
  v_before jsonb := public.get_state();
  v_after jsonb := public.sell_goods('sel', 4);
  v_sold jsonb := pg_temp.trades(v_after, 'sel', 'sell');
begin
  assert jsonb_array_length(v_sold) = 1 and (v_sold -> 0 ->> 'quantity')::int = 4, 'a sale gets its own line';
  assert (v_sold -> 0 ->> 'total')::bigint = pg_temp.coins(v_after) - pg_temp.coins(v_before), 'the sale line shows what was earned';
  v_after := public.buy_goods('sel', 1);
  assert jsonb_array_length(pg_temp.trades(v_after, 'sel', 'buy')) = 2, 'a purchase long after the last one starts a new line';
end;
$$;

select pg_temp.as_player('c9c9c9c9-c9c9-c9c9-c9c9-c9c9c9c9c9c9');
select public.found_player('Clémence', 'caravanier', 'port-sable') is not null;
select public.post_offer(null, 50, 'sel', 2) is not null;

select pg_temp.as_player('b8b8b8b8-b8b8-b8b8-b8b8-b8b8b8b8b8b8');

do $$
declare
  v_offer bigint := (select (offer ->> 'id')::bigint from jsonb_array_elements(public.get_state() -> 'offers') offer where offer ->> 'want_good_id' = 'sel');
  v_after jsonb := public.accept_offer(v_offer);
  v_sold jsonb := pg_temp.trades(v_after, 'sel', 'sell');
begin
  assert exists (select 1 from jsonb_array_elements(v_sold) trade where trade ->> 'source' = 'comptoir' and (trade ->> 'total')::int = 50),
    'selling to a counter offer is logged with its price';
end;
$$;

select pg_temp.as_player('c9c9c9c9-c9c9-c9c9-c9c9-c9c9c9c9c9c9');

do $$
declare
  v_bought jsonb := pg_temp.trades(public.get_state(), 'sel', 'buy');
begin
  assert jsonb_array_length(v_bought) = 1 and v_bought -> 0 ->> 'source' = 'comptoir' and (v_bought -> 0 ->> 'quantity')::int = 2,
    'the author of the offer sees the purchase too';
  assert (select count(*) from public.trade_log where player_id <> auth.uid()) = 0, 'a player only reads their own trades';
end;
$$;

reset role;
select 'All trade log tests passed' as result;