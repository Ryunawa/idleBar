\set ON_ERROR_STOP on

insert into auth.users (id) values ('11111111-1111-1111-1111-111111111111');

create function pg_temp.check(p_condition boolean, p_message text) returns void
language plpgsql
as $$
begin
  if p_condition is not true then
    raise exception 'Échec : %', p_message;
  end if;
end;
$$;

create function pg_temp.expect_error(p_statement text, p_fragment text) returns void
language plpgsql
as $$
begin
  execute p_statement;
  raise exception 'Attendu une erreur « % » pour %', p_fragment, p_statement using errcode = 'XX000';
exception
  when raise_exception then
    if position(p_fragment in sqlerrm) = 0 then
      raise exception 'Erreur inattendue pour % : %', p_statement, sqlerrm;
    end if;
end;
$$;

grant execute on function pg_temp.check(boolean, text) to authenticated;
grant execute on function pg_temp.expect_error(text, text) to authenticated;

select set_config('request.jwt.claim.sub', '11111111-1111-1111-1111-111111111111', false);

set role authenticated;
select pg_temp.check(public.get_state() -> 'tavern' = 'null'::jsonb, 'pas de taverne avant la fondation');
select pg_temp.expect_error('select public.found_tavern(''X'')', 'entre 2 et 24');
select pg_temp.check(public.found_tavern('  Le Pot d''Étain ') #>> '{tavern,name}' = 'Le Pot d''Étain', 'nom de la taverne');
select pg_temp.expect_error('select public.found_tavern(''Une autre'')', 'déjà ouverte');

select pg_temp.check((public.get_state() #>> '{tavern,stools}')::integer = 4, 'quatre tabourets au départ');
select pg_temp.check(public.get_state() #> '{tavern,menu}' = '["beer", "tea"]'::jsonb, 'bière et thé au départ');
select pg_temp.check(jsonb_array_length(public.get_state() #> '{tavern,goals}') = 3, 'trois objectifs du jour');
select pg_temp.check(public.get_state() #>> '{tavern,friend_code}' ~ '^[A-Z2-9]{6}$', 'code ami');
reset role;

update public.taverns set reported_at = now() where player_id = '11111111-1111-1111-1111-111111111111';
delete from public.tavern_goals where player_id = '11111111-1111-1111-1111-111111111111';
insert into public.tavern_goals (player_id, day, goal_id) values
  ('11111111-1111-1111-1111-111111111111', current_date, 'servir-20'),
  ('11111111-1111-1111-1111-111111111111', current_date, 'the-10');

set role authenticated;
select public.report_service('{"drinks": {"beer": 100, "soup": 5, "water": 3}, "perfect": 50, "coins": 9999, "parting": 2}');
select pg_temp.check((public.get_state() #>> '{tavern,served}')::integer = 4, 'services plafonnés aux tabourets');
select pg_temp.check((public.get_state() #>> '{tavern,perfect}')::integer = 4, 'parfaits plafonnés aux services');
select pg_temp.check((public.get_state() #>> '{tavern,coins}')::integer = 162, 'écus plafonnés : 4 × 40 + 2');
select pg_temp.check((public.get_state() #>> '{tavern,renown}')::integer = 8, 'renommée : services et parfaits');
select public.report_service('{"drinks": {"beer": "beaucoup", "tea": -4}, "coins": "tout"}');
select pg_temp.check((public.get_state() #>> '{tavern,coins}')::integer = 162, 'valeurs invalides ignorées');
reset role;

update public.taverns set reported_at = now() - interval '60 seconds' where player_id = '11111111-1111-1111-1111-111111111111';
set role authenticated;
select public.report_service('{"drinks": {"beer": 10, "tea": 6}, "perfect": 3, "coins": 70}');
select pg_temp.check((public.get_state() #>> '{tavern,served}')::integer = 20, 'seize services en une minute');
select pg_temp.check((public.get_state() #>> '{tavern,coins}')::integer = 162 + 70 + 60, 'objectif « servir 20 » payé');
select pg_temp.check(
  (select bool_and((goal ->> 'done')::boolean = (goal ->> 'id' = 'servir-20')) from jsonb_array_elements(public.get_state() #> '{tavern,goals}') goal),
  'seul l''objectif atteint est rempli');

select pg_temp.expect_error('select public.buy_upgrade(''tabouret-6'')', 'Il faut d''abord');
select pg_temp.expect_error('select public.buy_upgrade(''marmite'')', 'Estaminet');
select pg_temp.expect_error('select public.buy_upgrade(''inconnue'')', 'n''existe pas');
select public.buy_upgrade('tabouret-5');
select pg_temp.check((public.get_state() #>> '{tavern,stools}')::integer = 5, 'cinquième tabouret');
select pg_temp.check((public.get_state() #>> '{tavern,coins}')::integer = 292 - 120, 'tabouret payé');
select pg_temp.expect_error('select public.buy_upgrade(''tabouret-5'')', 'déjà');
select pg_temp.expect_error('select public.buy_upgrade(''apprenti'')', 'Il te manque 28 écus');
select pg_temp.expect_error('select public.collect_tip_jar()', 'vide');
reset role;

update public.taverns set coins = 1000, renown = 300 where player_id = '11111111-1111-1111-1111-111111111111';
set role authenticated;
select public.buy_upgrade('apprenti');
select public.buy_upgrade('marmite');
select pg_temp.check((public.get_state() #>> '{tavern,helper}')::integer = 1, 'apprenti embauché');
select pg_temp.check(public.get_state() #> '{tavern,menu}' = '["beer", "tea", "soup"]'::jsonb, 'soupe à la carte');
select pg_temp.check((public.get_state() #>> '{tavern,tier}')::integer = 1, 'Estaminet à 300 de renommée');
select pg_temp.check((public.get_state() #>> '{tavern,tip_jar,hourly}')::integer = 50, 'apprenti : 40 par heure pour 4 tabourets, 50 pour 5');
reset role;

update public.taverns set reported_at = now() - interval '3 hours' where player_id = '11111111-1111-1111-1111-111111111111';
set role authenticated;
select pg_temp.check((public.get_state() #>> '{tavern,tip_jar,amount}')::integer = 150, 'trois heures d''absence : 150 écus dans le pot');
select pg_temp.check((public.get_state() #>> '{tavern,tip_jar,amount}')::integer = 150, 'le pot ne se remplit pas en ligne');
select public.collect_tip_jar();
select pg_temp.check((public.get_state() #>> '{tavern,coins}')::integer = 1000 - 200 - 400 + 150, 'pot vidé dans la bourse');
select pg_temp.check((public.get_state() #>> '{tavern,tip_jar,amount}')::integer = 0, 'pot vide');
reset role;

update public.taverns set reported_at = now() - interval '3 days' where player_id = '11111111-1111-1111-1111-111111111111';
set role authenticated;
select pg_temp.check((public.get_state() #>> '{tavern,tip_jar,amount}')::integer = 400, 'pot plafonné à 8 heures');
reset role;

set role authenticated;
select pg_temp.check(public.get_requirements() ->> 'min_client_version' = '0.2.0', 'version minimale du jeu');
reset role;
