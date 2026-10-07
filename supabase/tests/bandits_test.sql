\set ON_ERROR_STOP on

insert into auth.users (id) values ('d1d1d1d1-d1d1-d1d1-d1d1-d1d1d1d1d1d1');

update private.settings set travel_time_factor = 1, craft_time_factor = 1, events_enabled = false;

select set_config('request.jwt.claim.sub', 'd1d1d1d1-d1d1-d1d1-d1d1-d1d1d1d1d1d1', false);
set role authenticated;
select public.found_player('Gontran', 'caravanier', 'brumeval') is not null;
reset role;

create function pg_temp.stock(p_good_id text) returns integer
language sql
as $$
  select coalesce((select quantity from public.cargo where player_id = 'd1d1d1d1-d1d1-d1d1-d1d1-d1d1d1d1d1d1' and good_id = p_good_id), 0);
$$;

create function pg_temp.coins() returns bigint
language sql
as $$
  select coins from public.players where player_id = 'd1d1d1d1-d1d1-d1d1-d1d1-d1d1d1d1d1d1';
$$;

update public.players set coins = 0 where player_id = 'd1d1d1d1-d1d1-d1d1-d1d1-d1d1d1d1d1d1';
delete from public.cargo where player_id = 'd1d1d1d1-d1d1-d1d1-d1d1-d1d1d1d1d1d1';

do $$
begin
  perform private.apply_road_event('d1d1d1d1-d1d1-d1d1-d1d1-d1d1d1d1d1d1', 'bandits', 'payer', 3600, now());
  assert pg_temp.coins() = 0, 'an empty caravan pays nothing';
  assert (select detail from public.event_log where player_id = 'd1d1d1d1-d1d1-d1d1-d1d1-d1d1d1d1d1d1' order by id desc limit 1) like '%caravane est vide%',
    'the journal says the caravan was empty';
end;
$$;

insert into public.cargo (player_id, good_id, quantity) values
  ('d1d1d1d1-d1d1-d1d1-d1d1-d1d1d1d1d1d1', 'vin', 20),
  ('d1d1d1d1-d1d1-d1d1-d1d1-d1d1d1d1d1d1', 'sel', 30);

do $$
declare
  v_value bigint := private.cargo_value('d1d1d1d1-d1d1-d1d1-d1d1-d1d1d1d1d1d1');
  v_ransom integer := least(floor(v_value * 0.08), 150);
  v_wine_before int := pg_temp.stock('vin');
begin
  perform private.apply_road_event('d1d1d1d1-d1d1-d1d1-d1d1-d1d1d1d1d1d1', 'bandits', 'payer', 3600, now());
  assert pg_temp.coins() = 0, 'an empty purse stays empty';
  assert (v_wine_before - pg_temp.stock('vin')) * 22 >= v_ransom, 'with an empty purse the bandits take goods worth the ransom';
  assert pg_temp.stock('sel') = 30, 'they start with the most valuable goods';
  assert (select detail from public.event_log where player_id = 'd1d1d1d1-d1d1-d1d1-d1d1-d1d1d1d1d1d1' order by id desc limit 1) like '%se servent dans ta cargaison%',
    'the journal says what they took';
end;
$$;

update public.players set coins = 1000 where player_id = 'd1d1d1d1-d1d1-d1d1-d1d1-d1d1d1d1d1d1';

do $$
declare
  v_ransom integer := least(floor((1000 + private.cargo_value('d1d1d1d1-d1d1-d1d1-d1d1-d1d1d1d1d1d1')) * 0.08), 150);
  v_wine int := pg_temp.stock('vin');
begin
  perform private.apply_road_event('d1d1d1d1-d1d1-d1d1-d1d1-d1d1d1d1d1d1', 'bandits', 'payer', 3600, now());
  assert pg_temp.coins() = 1000 - v_ransom, 'a full purse pays the ransom in coins';
  assert pg_temp.stock('vin') = v_wine, 'goods stay untouched when the coins suffice';
end;
$$;

select 'All bandit tests passed' as result;