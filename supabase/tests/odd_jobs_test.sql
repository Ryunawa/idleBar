\set ON_ERROR_STOP on

insert into auth.users (id) values
  ('d4d4d4d4-d4d4-d4d4-d4d4-d4d4d4d4d4d4'),
  ('e5e5e5e5-e5e5-e5e5-e5e5-e5e5e5e5e5e5');

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

create function pg_temp.expect_error(p_statement text, p_message text) returns void
language plpgsql
as $$
begin
  execute p_statement;
  raise exception 'Expected "%" to fail with "%"', p_statement, p_message using errcode = 'XX000';
exception
  when raise_exception then
    if sqlerrm not like p_message || '%' then
      raise exception 'Expected "%", got "%"', p_message, sqlerrm;
    end if;
end;
$$;

create function pg_temp.idle_for(p_player_id uuid, p_minutes numeric) returns void
language sql
as $$
  update public.players set odd_jobs_since = now() - make_interval(secs => p_minutes * 60) where player_id = p_player_id;
$$;

create function pg_temp.set_xp(p_player_id uuid, p_xp integer) returns void
language sql
as $$
  insert into public.masteries (player_id, craft_id, xp)
  select player_id, craft_id, p_xp from public.players where player_id = p_player_id
  on conflict (player_id, craft_id) do update set xp = excluded.xp;
$$;

grant execute on function pg_temp.as_player(text), pg_temp.coins(jsonb), pg_temp.expect_error(text, text) to authenticated;

set role authenticated;

select pg_temp.as_player('e5e5e5e5-e5e5-e5e5-e5e5-e5e5e5e5e5e5');
select pg_temp.expect_error('select public.collect_odd_jobs()', 'Installe-toi d''abord');

do $$
begin
  assert public.get_state() -> 'odd_jobs' = 'null'::jsonb, 'a player who has not settled has no purse';
end;
$$;

select pg_temp.as_player('d4d4d4d4-d4d4-d4d4-d4d4-d4d4d4d4d4d4');

do $$
declare
  v_state jsonb := public.found_player('Mahaut', 'herboriste', 'brumeval');
begin
  assert (v_state -> 'odd_jobs' ->> 'hourly')::int = 10, 'an apprentice earns 10 coins an hour';
  assert (v_state -> 'odd_jobs' ->> 'cap_hours')::numeric = 8, 'the purse fills for 8 hours at most';
  assert (v_state -> 'odd_jobs' ->> 'earned')::int = 0, 'a new player starts with an empty purse';
end;
$$;

select pg_temp.expect_error('select public.collect_odd_jobs()', 'Ta bourse est encore vide');

reset role;
select pg_temp.idle_for('d4d4d4d4-d4d4-d4d4-d4d4-d4d4d4d4d4d4', 180);
set role authenticated;

do $$
declare
  v_before jsonb := public.get_state();
  v_after jsonb;
begin
  assert (v_before -> 'odd_jobs' ->> 'earned')::int = 30, 'three hours fill the purse with 30 coins';
  v_after := public.collect_odd_jobs();
  assert pg_temp.coins(v_after) = pg_temp.coins(v_before) + 30, 'collecting adds the purse to the coins';
  assert (v_after -> 'odd_jobs' ->> 'earned')::int = 0, 'collecting empties the purse';
end;
$$;

reset role;
select pg_temp.idle_for('d4d4d4d4-d4d4-d4d4-d4d4-d4d4d4d4d4d4', 93);
set role authenticated;

do $$
declare
  v_before jsonb := public.get_state();
  v_after jsonb := public.collect_odd_jobs();
begin
  assert pg_temp.coins(v_after) = pg_temp.coins(v_before) + 15, '93 minutes pay 15 coins';
end;
$$;

reset role;
do $$
begin
  assert private.odd_jobs_hours('d4d4d4d4-d4d4-d4d4-d4d4-d4d4d4d4d4d4') between 0.04 and 0.06,
    'the 3 minutes not yet paid stay in the purse';
end;
$$;

reset role;
select pg_temp.idle_for('d4d4d4d4-d4d4-d4d4-d4d4-d4d4d4d4d4d4', 20 * 60);
set role authenticated;

do $$
declare
  v_before jsonb := public.get_state();
  v_after jsonb;
begin
  assert (v_before -> 'odd_jobs' ->> 'earned')::int = 80, 'the purse stops at 8 hours of odd jobs';
  v_after := public.collect_odd_jobs();
  assert pg_temp.coins(v_after) = pg_temp.coins(v_before) + 80, 'a full purse pays 80 coins';
end;
$$;

reset role;
do $$
begin
  assert private.odd_jobs_hours('d4d4d4d4-d4d4-d4d4-d4d4-d4d4d4d4d4d4') < 0.01, 'a full purse starts again from zero';
end;
$$;
select pg_temp.set_xp('d4d4d4d4-d4d4-d4d4-d4d4-d4d4d4d4d4d4', 600);
set role authenticated;

do $$
begin
  assert (public.get_state() -> 'odd_jobs' ->> 'hourly')::int = 15, 'a journeyman earns 15 coins an hour';
end;
$$;

reset role;
select pg_temp.set_xp('d4d4d4d4-d4d4-d4d4-d4d4-d4d4d4d4d4d4', 3000);
select pg_temp.idle_for('d4d4d4d4-d4d4-d4d4-d4d4-d4d4d4d4d4d4', 120);
set role authenticated;

do $$
declare
  v_state jsonb := public.get_state();
begin
  assert (v_state -> 'odd_jobs' ->> 'hourly')::int = 20, 'a master earns 20 coins an hour';
  assert (v_state -> 'odd_jobs' ->> 'earned')::int = 40, 'two hours as a master fill the purse with 40 coins';
end;
$$;

reset role;
do $$
begin
  assert has_function_privilege('authenticated', 'public.collect_odd_jobs()', 'execute'), 'players can collect';
  assert not has_function_privilege('anon', 'public.collect_odd_jobs()', 'execute'), 'visitors cannot collect';
end;
$$;
select 'All odd jobs tests passed' as result;