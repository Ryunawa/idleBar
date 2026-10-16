\set ON_ERROR_STOP on

insert into auth.users (id) values
  ('8e000000-0000-0000-0000-000000000001'), ('8e000000-0000-0000-0000-000000000002'),
  ('8e000000-0000-0000-0000-000000000003'), ('8e000000-0000-0000-0000-000000000004');

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
  select ('8e000000-0000-0000-0000-00000000000' || p_digit)::uuid;
$$;

create function pg_temp.as_player(p_digit integer) returns void
language sql
as $$
  select set_config('request.jwt.claim.sub', pg_temp.player(p_digit)::text, false);
$$;

create function pg_temp.session(p_digit integer) returns uuid
language sql
as $$
  select ('00000000-0000-0000-0000-0000000008e' || p_digit)::uuid;
$$;

create function pg_temp.save(p_digit integer, p_bosses bigint, p_monsters bigint default 0) returns text
language sql
as $$
  select public.save_frontier((public.claim_frontier(pg_temp.session(p_digit)) #>> '{frontier,revision}')::integer,
    pg_temp.session(p_digit), 100, 1e10, 0, 0, 12, 12, 12, 0, false,
    '{5,1,0}', '{forge,bakery,null,null,null,null,null,null,null,null,null,null}', '{3,1,0,0,0,0,0,0,0,0,0,0}',
    '{0,0,0}', '{}', p_monsters, p_bosses, 0, 0, 0, 0) ->> 'status';
$$;

create function pg_temp.seals() returns integer
language sql
as $$
  select (public.get_social() #>> '{research,seals}')::integer;
$$;

create function pg_temp.node(p_node text) returns jsonb
language sql
as $$
  select node from jsonb_array_elements(public.get_social() #> '{research,nodes}') node where node ->> 'id' = p_node;
$$;

create function pg_temp.member(p_name text) returns bigint
language sql
as $$
  select (member ->> 'id')::bigint
  from jsonb_array_elements(public.get_social() #> '{guild,members}') member
  where member ->> 'name' = p_name;
$$;

grant execute on function pg_temp.check(boolean, text), pg_temp.player(integer), pg_temp.as_player(integer), pg_temp.session(integer),
  pg_temp.save(integer, bigint, bigint), pg_temp.seals(), pg_temp.node(text), pg_temp.member(text) to authenticated;

set role authenticated;
select pg_temp.as_player(1);
select pg_temp.check(pg_temp.seals() = 0, 'aucun sceau sans partie');
select public.start_frontier(pg_temp.session(1));
select public.set_hero(pg_temp.session(1), 'Aube', 'warrior', 0, 0, 0, 0);
select pg_temp.as_player(2);
select public.start_frontier(pg_temp.session(2));
select public.set_hero(pg_temp.session(2), 'Bastien', 'mage', 1, 1, 1, 1);
select pg_temp.as_player(3);
select public.start_frontier(pg_temp.session(3));
select public.set_hero(pg_temp.session(3), 'Cyrielle', 'tank', 2, 2, 2, 2);
select pg_temp.as_player(4);
select public.start_frontier(pg_temp.session(4));
select public.set_hero(pg_temp.session(4), 'Dorian', 'archer', 3, 0, 3, 3);

select pg_temp.as_player(1);
select pg_temp.check(public.get_social() #> '{research,nodes}' = '[]'::jsonb, 'pas de recherche sans guilde');
select pg_temp.check(public.get_social() #> '{research,queue}' = '[]'::jsonb, 'pas de file sans guilde');
select pg_temp.check(pg_temp.save(1, 3) = 'ok' and pg_temp.seals() = 3, 'un sceau par boss');
select pg_temp.check(pg_temp.save(1, 13) = 'ok' and pg_temp.seals() = 6, 'six sceaux par heure au plus');
select pg_temp.check(pg_temp.save(1, 20) = 'ok' and pg_temp.seals() = 6, 'sauvegarder souvent ne relance pas le plafond');
reset role;
update private.reward_credits set seals_at = seals_at - interval '30 minutes' where player_id = pg_temp.player(1);
set role authenticated;
select pg_temp.check(pg_temp.save(1, 30) = 'ok' and pg_temp.seals() = 9, 'trois sceaux de plus après une demi-heure');
reset role;
update private.reward_credits set seals_at = now() - interval '5 hours' where player_id = pg_temp.player(1);
set role authenticated;
select pg_temp.check(pg_temp.save(1, 100) = 'ok' and pg_temp.seals() = 15, 'une absence ne garde qu''une heure de sceaux');
select pg_temp.check(pg_temp.save(1, 100) = 'ok' and pg_temp.seals() = 15, 'sans boss, aucun sceau');

select pg_temp.as_player(3);
select pg_temp.check(pg_temp.save(3, 2) = 'ok' and pg_temp.seals() = 2, 'des sceaux même sans guilde');
select pg_temp.check(public.get_social() -> 'guild' = 'null'::jsonb, 'toujours sans guilde');

reset role;
delete from public.titans;
insert into public.titans (week, hp, damage, fallen_at) values (private.titan_week() - 7, 100000, 100000, now() - interval '1 day');
insert into public.titan_contributions (week, player_id, monsters) values
  (private.titan_week() - 7, pg_temp.player(1), 500), (private.titan_week() - 7, pg_temp.player(2), 3000),
  (private.titan_week() - 7, pg_temp.player(4), 10000);
set role authenticated;
select pg_temp.as_player(1);
select pg_temp.check((public.get_social() #>> '{titan,tiers,0,seals}')::integer = 10, 'le premier palier annonce 10 sceaux');
select pg_temp.check((public.get_social() #>> '{titan,claimable,seals}')::integer = 10, 'la récompense annonce ses sceaux');
select pg_temp.check(public.claim_titan() ->> 'status' = 'ok' and pg_temp.seals() = 25, 'palier de 500 : 10 sceaux');
select pg_temp.check(public.claim_titan() ->> 'status' = 'nothing' and pg_temp.seals() = 25, 'les sceaux du titan une seule fois');
select pg_temp.as_player(2);
select pg_temp.check(public.claim_titan() ->> 'status' = 'ok' and pg_temp.seals() = 25, 'palier de 3 000 : 25 sceaux');
select pg_temp.as_player(4);
select pg_temp.check(public.claim_titan() ->> 'status' = 'ok' and pg_temp.seals() = 50, 'palier de 10 000 : 50 sceaux');

select pg_temp.as_player(1);
select pg_temp.check(public.create_guild('Les Sceaux') ->> 'status' = 'ok', 'guilde fondée');
reset role;
select set_config('test.code', (select code from public.guilds where name = 'Les Sceaux'), false);
select set_config('test.guild', (select id::text from public.guilds where name = 'Les Sceaux'), false);
set role authenticated;
select pg_temp.as_player(2);
select pg_temp.check(public.join_guild(current_setting('test.code')) ->> 'status' = 'ok', 'Bastien rejoint');
select pg_temp.check(jsonb_array_length(public.get_social() #> '{research,nodes}') = 12, 'douze nœuds de recherche');
select pg_temp.check((pg_temp.node('common_forge') ->> 'cost')::integer = 15, 'premier niveau à 15 sceaux pour une petite guilde');
select pg_temp.check((pg_temp.node('common_forge') ->> 'hours')::numeric = 2, 'premier niveau en 2 h');
select pg_temp.check((pg_temp.node('hospitality') ->> 'hours')::numeric = 20, 'dernier nœud en 20 h');

select pg_temp.as_player(3);
select pg_temp.check(public.fund_research('common_forge', 1) ->> 'status' = 'not_member', 'il faut une guilde pour verser');
select pg_temp.as_player(1);
select pg_temp.check(public.fund_research('castle', 1) ->> 'status' = 'unknown_node', 'nœud inconnu');
select pg_temp.check(public.fund_research('common_forge', 0) ->> 'status' = 'refused', 'au moins un sceau');
select pg_temp.check(public.fund_research('common_forge', null) ->> 'status' = 'refused', 'un nombre de sceaux');
select pg_temp.check(public.fund_research('ramparts', 1) ->> 'status' = 'locked', 'Remparts demande la Forge commune');
select pg_temp.check(public.fund_research('common_forge', 26) ->> 'status' = 'too_poor' and pg_temp.seals() = 25, 'pas plus de sceaux que les siens');
select pg_temp.check((public.fund_research('common_forge', 5) ->> 'given')::integer = 5, 'versement partiel');
select pg_temp.check(pg_temp.seals() = 20 and (pg_temp.node('common_forge') ->> 'funded')::integer = 5, 'cinq sceaux versés');
select pg_temp.as_player(2);
select pg_temp.check((public.fund_research('common_forge', 20) ->> 'given')::integer = 10, 'le surplus reste au joueur');
select pg_temp.check(pg_temp.seals() = 15, 'Bastien garde ses sceaux en trop');
select pg_temp.check((pg_temp.node('common_forge') ->> 'queued')::integer = 1 and (pg_temp.node('common_forge') ->> 'funded')::integer = 0, 'niveau financé');
select pg_temp.check(public.get_social() #>> '{research,queue,0,node}' = 'common_forge', 'la recherche démarre');
select pg_temp.check((public.get_social() #>> '{research,queue,0,ends_in}')::numeric between 7190 and 7200, 'pendant 2 h');
select pg_temp.check((pg_temp.node('common_forge') ->> 'cost')::integer = 24, 'le niveau 2 coûte 60 % de plus');
select pg_temp.check((public.fund_research('common_forge', 15) ->> 'given')::integer = 15, 'Bastien finance le niveau 2');
select pg_temp.as_player(1);
select pg_temp.check((public.fund_research('common_forge', 9) ->> 'given')::integer = 9, 'Aube complète le niveau 2');
select pg_temp.check(public.get_social() #>> '{research,queue,1,level}' = '2', 'le niveau 2 attend son tour');
select pg_temp.check((public.get_social() #>> '{research,queue,1,starts_in}')::numeric between 7190 and 7200, 'il démarre après le niveau 1');
select pg_temp.check((public.get_social() #>> '{research,queue,1,ends_in}')::numeric between 17990 and 18000, 'et dure 3 h');
reset role;
select private.grant_seals(pg_temp.player(1), 500);
set role authenticated;
select pg_temp.check(pg_temp.seals() = 511, 'des sceaux donnés par le serveur');
select pg_temp.check((public.fund_research('caravans', 15) ->> 'given')::integer = 15, 'Caravanes financées');
select pg_temp.check(public.get_social() #>> '{research,queue,2,node}' = 'caravans', 'une recherche à la fois, dans l''ordre du financement');
select pg_temp.check((public.get_social() #>> '{research,queue,2,ends_in}')::numeric between 25190 and 25200, 'Caravanes après la Forge commune');
select pg_temp.check((public.fund_research('common_forge', 39) ->> 'given')::integer = 39, 'niveau 3 financé');
select pg_temp.check((public.fund_research('common_forge', 62) ->> 'given')::integer = 62, 'niveau 4 financé');
select pg_temp.check((public.fund_research('common_forge', 100) ->> 'given')::integer = 99, 'niveau 5 financé');
select pg_temp.check(public.fund_research('common_forge', 1) ->> 'status' = 'maxed', 'tous les niveaux financés');
select pg_temp.check(pg_temp.node('common_forge') -> 'cost' = 'null'::jsonb, 'plus rien à financer');
select pg_temp.check(public.fund_research('ramparts', 1) ->> 'status' = 'locked', 'Remparts attend la fin de la Forge commune');

reset role;
update public.research_queue set starts_at = starts_at - interval '2 hours 1 minute', ends_at = ends_at - interval '2 hours 1 minute'
where guild_id = current_setting('test.guild')::uuid;
select pg_temp.check(private.research_effect(pg_temp.player(1), 'common_forge') = 4, 'une recherche finie compte avant toute lecture');
select pg_temp.check((select level from public.guild_research where node = 'common_forge' and guild_id = current_setting('test.guild')::uuid) = 0, 'pas encore enregistrée');
set role authenticated;
select pg_temp.check((pg_temp.node('common_forge') ->> 'level')::integer = 1, 'la lecture termine la recherche');
select pg_temp.check((pg_temp.node('common_forge') ->> 'queued')::integer = 4, 'quatre niveaux en attente');
select pg_temp.check(public.get_social() #>> '{research,queue,0,level}' = '2' and (public.get_social() #>> '{research,queue,0,starts_in}')::numeric = 0, 'la suivante a démarré');
select pg_temp.check((public.get_social() #>> '{research,queue,0,ends_in}')::numeric between 10730 and 10740, 'à l''heure prévue');
reset role;
select pg_temp.check((select level from public.guild_research where node = 'common_forge' and guild_id = current_setting('test.guild')::uuid) = 1, 'niveau enregistré');
set role authenticated;
select pg_temp.check((public.fund_research('ramparts', 1) ->> 'given')::integer = 1, 'Remparts débloqué');
select pg_temp.check((pg_temp.node('ramparts') ->> 'frozen')::boolean and (pg_temp.node('ramparts') ->> 'cost')::integer = 22, 'prix fixé au premier sceau');

reset role;
select set_config('test.front_max', (select help_front_max::text from private.settings), false);
select set_config('test.titan_min_hp', (select titan_min_hp::text from private.settings), false);
select set_config('test.members_step', (select research_members_step::text from private.settings), false);
insert into public.guild_research (guild_id, node, level) values
  (current_setting('test.guild')::uuid, 'veterans', 2), (current_setting('test.guild')::uuid, 'heavy_convoys', 1),
  (current_setting('test.guild')::uuid, 'messengers', 1), (current_setting('test.guild')::uuid, 'war_cry', 2),
  (current_setting('test.guild')::uuid, 'guild_market', 2)
on conflict (guild_id, node) do update set level = excluded.level;
update private.settings set help_front_max = 1;
set role authenticated;
select pg_temp.as_player(4);
select pg_temp.check(public.join_guild(current_setting('test.code')) ->> 'status' = 'ok', 'Dorian rejoint');
select pg_temp.as_player(2);
select pg_temp.check(public.send_help(pg_temp.member('Aube'), 'hero') ->> 'status' = 'ok', 'renfort envoyé');
select pg_temp.check((public.get_social() #>> '{help,hero_ready_in}')::numeric between 12590 and 12600, 'Messagers : 30 min de recharge en moins');
select pg_temp.check(public.send_help(pg_temp.member('Aube'), 'convoy') ->> 'status' = 'ok', 'convoi envoyé');
select pg_temp.as_player(4);
select pg_temp.check(public.send_help(pg_temp.member('Aube'), 'hero') ->> 'status' = 'front_full', 'front plein sans Hospitalité');
reset role;
insert into public.guild_research (guild_id, node, level) values (current_setting('test.guild')::uuid, 'hospitality', 1);
set role authenticated;
select pg_temp.check(public.send_help(pg_temp.member('Aube'), 'hero') ->> 'status' = 'ok', 'Hospitalité : un renfort de plus');
select pg_temp.as_player(1);
select pg_temp.check((public.get_social() #>> '{help,allies,0,ends_in}')::numeric between 2990 and 3000, 'Renforts aguerris : 20 min de plus');
select pg_temp.check((
  select (boost ->> 'gold')::integer from jsonb_array_elements(public.get_social() #> '{help,boosts}') boost
  where boost ->> 'source' = 'convoy') = 65, 'Convois lourds : +15 % d''or');
select pg_temp.check((pg_temp.node('veterans') ->> 'level')::integer = 2, 'niveaux visibles');
reset role;
update private.settings set help_front_max = current_setting('test.front_max')::integer, titan_min_hp = 100000;
delete from public.titans;
select pg_temp.check(abs(private.gear_base(pg_temp.player(1), 50, 'common') - private.gear_base(pg_temp.player(3), 50, 'common') * 0.9) < 1e-6,
  'Marché de guilde : 10 % de moins');
set role authenticated;
select pg_temp.check(pg_temp.save(1, 100, 100) = 'ok' and (public.get_social() #>> '{titan,mine}')::integer = 120, 'Cri de guerre : 20 % de coups en plus');
select pg_temp.as_player(3);
select pg_temp.check(pg_temp.save(3, 2, 100) = 'ok' and (public.get_social() #>> '{titan,mine}')::integer = 100, 'sans guilde, un coup par monstre');

reset role;
update private.settings set research_members_step = 1;
insert into public.guild_research (guild_id, node, level) values (current_setting('test.guild')::uuid, 'sanctuary', 2);
select private.grant_seals(pg_temp.player(2), 60);
set role authenticated;
select pg_temp.as_player(1);
select pg_temp.check((pg_temp.node('caravans') ->> 'cost')::integer = 72, 'le coût suit le nombre de membres tant que personne n''a versé');
select pg_temp.check((pg_temp.node('ramparts') ->> 'cost')::integer = 22, 'un prix fixé ne monte pas quand la guilde grandit');
select pg_temp.check((pg_temp.node('sanctuary') ->> 'cost')::integer = 169 and not (pg_temp.node('sanctuary') ->> 'frozen')::boolean, 'trois membres : coût triplé');
select pg_temp.as_player(2);
select pg_temp.check((public.fund_research('sanctuary', 50) ->> 'given')::integer = 50, 'Bastien commence le dernier niveau du Sanctuaire');
select pg_temp.check(public.leave_guild() ->> 'status' = 'ok', 'Bastien part');
select pg_temp.check(pg_temp.seals() = 10 and public.get_social() #> '{research,nodes}' = '[]'::jsonb, 'ses sceaux restants le suivent, sans recherche');
select pg_temp.as_player(1);
select pg_temp.check((pg_temp.node('sanctuary') ->> 'funded')::integer = 50, 'les sceaux versés restent à la guilde');
select pg_temp.check((pg_temp.node('sanctuary') ->> 'cost')::integer = 169 and (pg_temp.node('sanctuary') ->> 'queued')::integer = 0, 'au prix fixé, sans démarrer');

reset role;
create schema if not exists test_tools;
create extension if not exists dblink schema test_tools;
select test_tools.dblink_connect('leaver', 'dbname=postgres');
select test_tools.dblink_connect('giver', 'dbname=postgres');
select test_tools.dblink_exec(link, command)
from (values ('leaver'), ('giver')) as links(link),
  (values ('set role authenticated'), ('set request.jwt.claim.sub = ''8e000000-0000-0000-0000-000000000004''')) as commands(command);
select test_tools.dblink_exec('leaver', 'begin');
select pg_temp.check((select reply::jsonb ->> 'status' from test_tools.dblink('leaver', 'select public.leave_guild()::text') as answer(reply text)) = 'ok',
  'Dorian part, sans valider encore');
select test_tools.dblink_send_query('giver', 'select public.fund_research(''sanctuary'', 1) ->> ''status''');
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
  where pid <> pg_backend_pid() and wait_event_type = 'Lock' and query like '%fund_research%'), 'le versement attend la guilde');
select test_tools.dblink_exec('leaver', 'commit');
select pg_temp.check((select status from test_tools.dblink_get_result('giver') as answer(status text)) = 'not_member',
  'pas de sceaux pour une guilde qu''on vient de quitter');
select test_tools.dblink_disconnect('leaver');
select test_tools.dblink_disconnect('giver');
drop extension dblink;
drop schema test_tools;
select pg_temp.check((select seals from public.player_seals where player_id = pg_temp.player(4)) = 50, 'Dorian garde ses sceaux');

set role authenticated;
select pg_temp.as_player(1);
select pg_temp.check((pg_temp.node('sanctuary') ->> 'cost')::integer = 169, 'le prix fixé ne baisse pas quand la guilde rétrécit');
select pg_temp.check((pg_temp.node('war_cry') ->> 'cost')::integer = 39, 'un niveau pas encore commencé suit la guilde');
select pg_temp.check((public.fund_research('sanctuary', 200) ->> 'given')::integer = 119, 'il faut le reste du prix fixé');
select pg_temp.check((pg_temp.node('sanctuary') ->> 'queued')::integer = 1 and (pg_temp.node('sanctuary') ->> 'funded')::integer = 0, 'dernier niveau en file');
select pg_temp.check(public.fund_research('sanctuary', 1) ->> 'status' = 'maxed', 'Sanctuaire entièrement financé');
reset role;
select pg_temp.check((select funded = 0 and cost is null from public.guild_research
  where guild_id = current_setting('test.guild')::uuid and node = 'sanctuary'), 'aucun sceau ne reste coincé');
update private.settings set research_members_step = current_setting('test.members_step')::integer;
set role authenticated;

select pg_temp.as_player(1);
select pg_temp.check(public.leave_guild() ->> 'status' = 'ok', 'Aube part en dernier');
reset role;
select pg_temp.check(not exists (select 1 from public.guild_research where guild_id = current_setting('test.guild')::uuid), 'une guilde dissoute perd sa recherche');
select pg_temp.check(not exists (select 1 from public.research_queue where guild_id = current_setting('test.guild')::uuid), 'et sa file');
select pg_temp.check((select seals from public.player_seals where player_id = pg_temp.player(1)) > 0, 'chacun garde ses sceaux');

delete from public.titans;
update private.settings set titan_min_hp = current_setting('test.titan_min_hp')::integer;
delete from auth.users where id in (pg_temp.player(1), pg_temp.player(2), pg_temp.player(3), pg_temp.player(4));
select pg_temp.check(not exists (select 1 from public.player_seals where player_id = pg_temp.player(1)), 'un compte supprimé emporte ses sceaux');
