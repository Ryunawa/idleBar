\set ON_ERROR_STOP on

update private.settings set events_enabled = false;

insert into auth.users (id) values ('55555555-5555-5555-5555-555555555555');

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

create function pg_temp.held(p_state jsonb, p_good_id text) returns integer
language sql
as $$
  select coalesce((select (line ->> 'quantity')::int from jsonb_array_elements(p_state -> 'warehouses') line where line ->> 'good_id' = p_good_id), 0);
$$;

grant execute on function pg_temp.expect_refusal(text, text), pg_temp.held(jsonb, text) to authenticated;

set role authenticated;
select set_config('request.jwt.claim.sub', '55555555-5555-5555-5555-555555555555', false);

do $$
declare
  v_state jsonb := public.found_player('Brunhilde', 'forgeron', 'ferrenoire');
begin
  assert v_state -> 'player' ->> 'craft_id' = 'forgeron', 'the craft is recorded';
  assert v_state -> 'player' ->> 'home_town_id' = 'ferrenoire', 'the artisan settles in the chosen town';
  assert v_state -> 'caravan' = 'null'::jsonb, 'an artisan has no caravan';
  assert (v_state -> 'workshop' ->> 'level')::int = 1, 'the workshop starts at level 1';
  assert (v_state -> 'workshop' ->> 'storage_capacity')::int = 60, 'the level 1 warehouse holds 60 goods';
  assert (v_state -> 'workshop' ->> 'max_queue')::int = 8, 'the level 1 workshop queues 8 batches';
  assert (v_state -> 'workshop' ->> 'queued')::int = 0, 'a new workshop is idle';
  assert jsonb_array_length(v_state -> 'market') = 19, 'the artisan trades on the home town market';
end;
$$;

select pg_temp.expect_refusal($$select public.depart('rocheclaire')$$, 'ne voyage pas');
select pg_temp.expect_refusal($$select public.buy_wagon()$$, 'ne voyage pas');
select pg_temp.expect_refusal($$select public.start_production('roues', 1)$$, 'pas de ton métier');
select pg_temp.expect_refusal($$select public.start_production('outils', 1)$$, '1 charbon et 2 fer');

do $$
declare
  v_before jsonb := public.get_state();
  v_state jsonb;
begin
  perform public.buy_goods('fer', 10);
  v_state := public.buy_goods('charbon', 5);
  assert pg_temp.held(v_state, 'fer') = 10 and pg_temp.held(v_state, 'charbon') = 5, 'raw materials land in the warehouse';
  assert (v_state -> 'player' ->> 'coins')::int < (v_before -> 'player' ->> 'coins')::int, 'the purchase is paid';
  assert jsonb_array_length(v_state -> 'cargo') = 0, 'an artisan has no cargo';
end;
$$;

do $$
declare
  v_state jsonb := public.start_production('outils', 10);
begin
  assert (v_state -> 'workshop' ->> 'queued')::int = 5, 'the queue is limited by the raw materials';
  assert v_state -> 'workshop' ->> 'recipe_id' = 'outils', 'the workshop forges tools';
  assert (v_state -> 'workshop' ->> 'batch_seconds')::int = 600, 'a level 1 batch lasts the recipe time';
  assert pg_temp.held(v_state, 'fer') = 0 and pg_temp.held(v_state, 'charbon') = 0, 'raw materials are consumed upfront';
end;
$$;

select pg_temp.expect_refusal($$select public.start_production('ferrures', 1)$$, 'déjà autre chose');

reset role;
update public.workshops set started_at = now() - interval '25 minutes' where player_id = '55555555-5555-5555-5555-555555555555';
set role authenticated;

do $$
declare
  v_state jsonb := public.get_state();
begin
  assert pg_temp.held(v_state, 'outils') = 2, 'two batches are done after 25 minutes';
  assert (v_state -> 'workshop' ->> 'queued')::int = 3, 'three batches remain';
  assert (v_state -> 'workshop' ->> 'started_at')::timestamptz between now() - interval '301 seconds' and now() - interval '299 seconds',
    'the current batch keeps its progress';
end;
$$;

reset role;
update public.workshops set started_at = now() - interval '2 hours' where player_id = '55555555-5555-5555-5555-555555555555';
set role authenticated;

do $$
declare
  v_state jsonb := public.get_state();
  v_after jsonb;
begin
  assert pg_temp.held(v_state, 'outils') = 5, 'every queued batch is delivered';
  assert (v_state -> 'workshop' ->> 'queued')::int = 0, 'the workshop is idle again';
  assert v_state -> 'workshop' -> 'recipe_id' = 'null'::jsonb, 'an idle workshop has no recipe';

  v_after := public.sell_goods('outils', 5);
  assert pg_temp.held(v_after, 'outils') = 0, 'the tools are sold';
  assert (v_after -> 'player' ->> 'coins')::int - (v_state -> 'player' ->> 'coins')::int > 250, 'tools sell well above their raw cost';
end;
$$;

reset role;
update public.players set coins = 100000 where player_id = '55555555-5555-5555-5555-555555555555';
set role authenticated;

do $$
declare
  v_state jsonb := public.buy_goods('sel', 1000);
begin
  assert pg_temp.held(v_state, 'sel') = 60, 'buying is capped by the warehouse capacity';
end;
$$;

select pg_temp.expect_refusal($$select public.buy_goods('fer', 1)$$, 'entrepôt est plein');

do $$
declare
  v_state jsonb;
begin
  perform public.sell_goods('sel', 60);
  v_state := public.upgrade_workshop();
  assert (v_state -> 'workshop' ->> 'level')::int = 2, 'the workshop is upgraded';
  assert (v_state -> 'workshop' ->> 'storage_capacity')::int = 80, 'the warehouse grows with the level';
  assert (v_state -> 'workshop' ->> 'max_queue')::int = 10, 'the queue grows with the level';
  assert (v_state -> 'workshop' ->> 'next_level_price')::int = 720, 'the next level costs more';

  perform public.buy_goods('fer', 2);
  perform public.buy_goods('charbon', 1);
  v_state := public.start_production('outils', 1);
  assert (v_state -> 'workshop' ->> 'batch_seconds')::int = 480, 'a level 2 workshop works 25 % faster';
end;
$$;

reset role;
update public.workshops set started_at = now() - interval '1 hour' where player_id = '55555555-5555-5555-5555-555555555555';
update private.settings set craft_time_factor = 0.01;
set role authenticated;

do $$
declare
  v_state jsonb;
begin
  perform public.buy_goods('fer', 2);
  perform public.buy_goods('charbon', 1);
  v_state := public.start_production('outils', 1);
  assert (v_state -> 'workshop' ->> 'batch_seconds')::int = 5, 'the craft time factor shortens batches for playtests';
end;
$$;

reset role;
update private.settings set craft_time_factor = 1;
update public.workshops set level = 5 where player_id = '55555555-5555-5555-5555-555555555555';
set role authenticated;

select pg_temp.expect_refusal($$select public.upgrade_workshop()$$, 'niveau maximal');

select set_config('request.jwt.claim.sub', '11111111-1111-1111-1111-111111111111', false);
do $$
begin
  assert (select count(*) from public.workshops) = 0, 'a player cannot read another workshop';
  assert (select count(*) from public.warehouses) = 0, 'a player cannot read another warehouse';
end;
$$;
reset role;

select 'All workshop tests passed' as result;
