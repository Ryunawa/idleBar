\set ON_ERROR_STOP on

insert into auth.users (id) values
  ('9e000000-0000-0000-0000-000000000001'), ('9e000000-0000-0000-0000-000000000002'),
  ('9e000000-0000-0000-0000-000000000003'), ('9e000000-0000-0000-0000-000000000004');

create function pg_temp.check(p_condition boolean, p_message text) returns void
language plpgsql
as $$
begin
  if p_condition is not true then
    raise exception 'Échec : %', p_message;
  end if;
end;
$$;

create function pg_temp.player(p_digit integer) returns uuid
language sql
as $$
  select ('9e000000-0000-0000-0000-00000000000' || p_digit)::uuid;
$$;

create function pg_temp.as_player(p_digit integer) returns void
language sql
as $$
  select set_config('request.jwt.claim.sub', pg_temp.player(p_digit)::text, false);
$$;

create function pg_temp.enter(p_digit integer, p_name text) returns void
language sql
as $$
  select pg_temp.as_player(p_digit);
  select public.start_frontier(('00000000-0000-0000-0000-0000000009e' || p_digit)::uuid);
  select public.set_hero(('00000000-0000-0000-0000-0000000009e' || p_digit)::uuid, p_name, 'mage', 0, 0, 0, 0);
$$;

create function pg_temp.queue() returns text
language sql
as $$
  select string_agg((entry ->> 'node') || ' ' || (entry ->> 'level'), ', ' order by position)
  from jsonb_array_elements(public.get_social() #> '{research,queue}') with ordinality as queue(entry, position);
$$;

create function pg_temp.entry(p_position integer, p_field text) returns numeric
language sql
as $$
  select (public.get_social() #>> array['research', 'queue', p_position::text, p_field])::numeric;
$$;

create function pg_temp.move(p_node text, p_level integer, p_offset integer) returns text
language sql
as $$
  select public.move_research(p_node, p_level, p_offset) ->> 'status';
$$;

grant execute on function pg_temp.check(boolean, text), pg_temp.player(integer), pg_temp.as_player(integer), pg_temp.enter(integer, text),
  pg_temp.queue(), pg_temp.entry(integer, text), pg_temp.move(text, integer, integer) to authenticated;

set role authenticated;
select pg_temp.enter(1, 'Lise');
select pg_temp.enter(2, 'Marc');
select pg_temp.enter(3, 'Nora');
select pg_temp.enter(4, 'Owen');
select pg_temp.as_player(1);
select pg_temp.check(public.create_guild('La File') ->> 'status' = 'ok', 'guilde fondée');
reset role;
select set_config('test.code', (select code from public.guilds where name = 'La File'), false);
select set_config('test.guild', (select id::text from public.guilds where name = 'La File'), false);
select private.grant_seals(pg_temp.player(1), 500);
select private.grant_seals(pg_temp.player(2), 100);
set role authenticated;
select pg_temp.as_player(2);
select pg_temp.check(public.join_guild(current_setting('test.code')) ->> 'status' = 'ok', 'Marc rejoint');
select pg_temp.as_player(3);
select pg_temp.check(public.join_guild(current_setting('test.code')) ->> 'status' = 'ok', 'Nora rejoint');

select pg_temp.as_player(1);
select public.fund_research('common_forge', 15);
select public.fund_research('caravans', 15);
select public.fund_research('common_forge', 24);
select public.fund_research('war_cry', 15);
select pg_temp.check(pg_temp.queue() = 'common_forge 1, caravans 1, common_forge 2, war_cry 1', 'quatre niveaux dans l''ordre des financements');

select pg_temp.as_player(4);
select pg_temp.check(pg_temp.move('caravans', 1, 1) = 'not_member', 'il faut une guilde');
select pg_temp.as_player(2);
select pg_temp.check(pg_temp.move('caravans', 1, 1) = 'not_leader', 'seul le chef réordonne la file');
select pg_temp.as_player(1);
select pg_temp.check(pg_temp.move('common_forge', 1, 1) = 'running', 'la recherche en cours ne bouge pas');
select pg_temp.check(pg_temp.move('schools', 1, 1) = 'not_queued', 'un niveau absent de la file');
select pg_temp.check(pg_temp.move('caravans', 1, 2) = 'refused', 'une place à la fois');
select pg_temp.check(pg_temp.move('caravans', 1, null) = 'refused', 'un sens de déplacement');
select pg_temp.check(pg_temp.move('caravans', 1, -1) = 'queue_edge', 'rien ne passe devant la recherche en cours');
select pg_temp.check(pg_temp.move('war_cry', 1, 1) = 'queue_edge', 'le dernier ne descend pas');
select pg_temp.check(pg_temp.queue() = 'common_forge 1, caravans 1, common_forge 2, war_cry 1', 'les refus ne changent rien');

select pg_temp.check(pg_temp.move('caravans', 1, 1) = 'ok', 'Caravanes descend d''une place');
select pg_temp.check(pg_temp.queue() = 'common_forge 1, common_forge 2, caravans 1, war_cry 1', 'échangée avec la suivante');
select pg_temp.check(pg_temp.entry(1, 'starts_in') between 7190 and 7200, 'Forge commune 2 démarre après la recherche en cours');
select pg_temp.check(pg_temp.entry(1, 'ends_in') between 17990 and 18000, 'et garde ses 3 h');
select pg_temp.check(pg_temp.entry(2, 'starts_in') between 17990 and 18000, 'Caravanes démarre ensuite');
select pg_temp.check(pg_temp.entry(2, 'ends_in') between 25190 and 25200, 'et garde ses 2 h');
select pg_temp.check(pg_temp.entry(3, 'ends_in') between 32390 and 32400, 'le Cri de guerre ne bouge pas');

select public.fund_research('common_forge', 39);
select pg_temp.check(pg_temp.move('common_forge', 3, -1) = 'ok' and pg_temp.move('common_forge', 3, -1) = 'ok', 'Forge commune 3 remonte deux fois');
select pg_temp.check(pg_temp.queue() = 'common_forge 1, common_forge 2, common_forge 3, caravans 1, war_cry 1', 'derrière le niveau 2');
select pg_temp.check(pg_temp.move('common_forge', 3, -1) = 'refused', 'deux niveaux d''une même recherche gardent leur ordre');
select pg_temp.check(pg_temp.move('common_forge', 2, 1) = 'refused', 'dans les deux sens');

reset role;
create schema if not exists test_tools;
create extension if not exists dblink schema test_tools;
select test_tools.dblink_connect('mover', 'dbname=postgres');
select test_tools.dblink_connect('giver', 'dbname=postgres');
select test_tools.dblink_exec(link, command)
from (values ('mover', '9e000000-0000-0000-0000-000000000001'), ('giver', '9e000000-0000-0000-0000-000000000002')) as links(link, player),
  lateral (values ('set role authenticated'), (format('set request.jwt.claim.sub = %L', player))) as commands(command);
select test_tools.dblink_exec('mover', 'begin');
select pg_temp.check((select reply::jsonb ->> 'status' from test_tools.dblink('mover', 'select public.move_research(''caravans'', 1, 1)::text') as answer(reply text)) = 'ok',
  'le chef déplace Caravanes, sans valider encore');
select test_tools.dblink_send_query('giver', 'select public.fund_research(''caravans'', 24) ->> ''status''');
do $$
begin
  for v_try in 1 .. 200 loop
    exit when exists (
      select 1 from pg_stat_activity
      where pid <> pg_backend_pid() and wait_event_type = 'Lock' and query like '%fund_research%');
    perform pg_sleep(0.05);
  end loop;
end;
$$;
select pg_temp.check(exists (
  select 1 from pg_stat_activity
  where pid <> pg_backend_pid() and wait_event_type = 'Lock' and query like '%fund_research%'), 'le versement attend le déplacement');
select test_tools.dblink_exec('mover', 'commit');
select pg_temp.check((select status from test_tools.dblink_get_result('giver') as answer(status text)) = 'ok', 'le versement passe ensuite');
select test_tools.dblink_disconnect('mover');
select test_tools.dblink_disconnect('giver');
drop extension dblink;
drop schema test_tools;
select pg_temp.check((
  select bool_and(entry.previous is null or entry.starts_at = entry.previous) and count(*) = 6
  from (
    select queue.starts_at, lag(queue.ends_at) over (order by queue.starts_at) as previous
    from public.research_queue queue where queue.guild_id = current_setting('test.guild')::uuid) entry
  ), 'la file reste continue, sans chevauchement');
set role authenticated;
select pg_temp.as_player(1);
select pg_temp.check(pg_temp.queue() = 'common_forge 1, common_forge 2, common_forge 3, war_cry 1, caravans 1, caravans 2',
  'le niveau financé pendant le déplacement prend la dernière place');
reset role;

delete from auth.users where id in (pg_temp.player(1), pg_temp.player(2), pg_temp.player(3), pg_temp.player(4));
select pg_temp.check(not exists (select 1 from public.guilds where name = 'La File'), 'guilde dissoute');
