\set ON_ERROR_STOP on

insert into auth.users (id) values ('77777777-7777-7777-7777-777777777777');

create function pg_temp.check(p_condition boolean, p_message text) returns void
language plpgsql
as $$
begin
  if p_condition is not true then
    raise exception 'Échec : %', p_message;
  end if;
end;
$$;

create function pg_temp.save(
  p_revision integer, p_session uuid,
  p_stage integer default 12, p_max integer default null, p_best integer default null,
  p_souls bigint default 0, p_retreats integer default 0,
  p_plots text[] default '{forge,bakery,null,null,null,null,null,null,null,null,null,null}', p_levels integer[] default '{3,1,0,0,0,0,0,0,0,0,0,0}',
  p_gold double precision default 100, p_achievements text[] default '{monstres-100}', p_killed bigint default 1000)
returns text
language sql
as $$
  select public.save_frontier(p_revision, p_session, p_gold, 1000, p_souls, p_retreats,
    p_stage, coalesce(p_max, p_stage), coalesce(p_best, p_max, p_stage), 0, false,
    '{5,1,0}', p_plots, p_levels, '{0,30.5,0}', false, false, p_achievements,
    p_killed, 0, 0, 0, 0) ->> 'status';
$$;

grant execute on function pg_temp.check(boolean, text) to authenticated;
grant execute on function pg_temp.save(integer, uuid, integer, integer, integer, bigint, integer, text[], integer[], double precision, text[], bigint) to authenticated;

select set_config('request.jwt.claim.sub', '77777777-7777-7777-7777-777777777777', false);
set role authenticated;
select pg_temp.check(public.claim_frontier('00000000-0000-0000-0000-000000000001') -> 'frontier' = 'null'::jsonb, 'pas de partie avant le départ');
select pg_temp.check(public.start_frontier('00000000-0000-0000-0000-000000000001') #>> '{frontier,revision}' = '0', 'partie créée');
select pg_temp.check(public.start_frontier('00000000-0000-0000-0000-000000000001') #> '{frontier,fighters}' = '[1, 0, 0]', 'le héros part au niveau 1');
select pg_temp.check(public.claim_frontier('00000000-0000-0000-0000-000000000001') #> '{frontier,plots}' = '[null, null, null, null, null, null, null, null, null, null, null, null]', 'douze emplacements vides');
select pg_temp.check(public.claim_frontier('00000000-0000-0000-0000-000000000001') #> '{frontier,cooldowns}' = '[0, 0, 0]', 'compétences prêtes');
select pg_temp.check(public.claim_frontier('00000000-0000-0000-0000-000000000001') #> '{frontier,achievements}' = '[]', 'aucun succès');

select pg_temp.check(pg_temp.save(0, '00000000-0000-0000-0000-000000000001') = 'ok', 'sauvegarde acceptée');
select pg_temp.check(public.claim_frontier('00000000-0000-0000-0000-000000000001') #>> '{frontier,revision}' = '1', 'révision suivante');
select pg_temp.check(public.claim_frontier('00000000-0000-0000-0000-000000000001') #>> '{frontier,stage}' = '12', 'niveau relu');
select pg_temp.check(public.claim_frontier('00000000-0000-0000-0000-000000000001') #>> '{frontier,plots,0}' = 'forge', 'bâtiment relu');
select pg_temp.check(public.claim_frontier('00000000-0000-0000-0000-000000000001') #>> '{frontier,levels,0}' = '3', 'niveau du bâtiment relu');
select pg_temp.check(public.claim_frontier('00000000-0000-0000-0000-000000000001') #>> '{frontier,cooldowns,1}' = '30.5', 'recharge relue');
select pg_temp.check(public.claim_frontier('00000000-0000-0000-0000-000000000001') #>> '{frontier,achievements,0}' = 'monstres-100', 'succès relu');

select pg_temp.check(pg_temp.save(0, '00000000-0000-0000-0000-000000000001') = 'conflict', 'ancienne révision refusée');
select pg_temp.check(pg_temp.save(1, '00000000-0000-0000-0000-000000000002') = 'elsewhere', 'autre session refusée');
select pg_temp.check(pg_temp.save(1, '00000000-0000-0000-0000-000000000001', p_stage => 900) = 'refused', 'niveau impossible refusé');
select pg_temp.check(pg_temp.save(1, '00000000-0000-0000-0000-000000000001', p_souls => 500, p_retreats => 1) = 'refused', 'âmes impossibles refusées');
select pg_temp.check(pg_temp.save(1, '00000000-0000-0000-0000-000000000001', p_plots => '{forge,forge,null,null,null,null,null,null,null,null,null,null}', p_levels => '{1,1,0,0,0,0,0,0,0,0,0,0}') = 'refused', 'un seul exemplaire par bâtiment');
select pg_temp.check(pg_temp.save(1, '00000000-0000-0000-0000-000000000001', p_plots => '{null,null,null,null,null,null,portal,null,null,null,null,null}', p_levels => '{0,0,0,0,0,0,1,0,0,0,0,0}') = 'refused', 'emplacement encore inexploré');
select pg_temp.check(pg_temp.save(1, '00000000-0000-0000-0000-000000000001', p_levels => '{0,1,0,0,0,0,0,0,0,0,0,0}') = 'refused', 'bâtiment sans niveau refusé');
select pg_temp.check(pg_temp.save(1, '00000000-0000-0000-0000-000000000001', p_plots => '{null,null,null,null,null,null,null,null,null,null,null,null}', p_levels => '{2,0,0,0,0,0,0,0,0,0,0,0}') = 'refused', 'niveau sans bâtiment refusé');
select pg_temp.check(pg_temp.save(1, '00000000-0000-0000-0000-000000000001', p_plots => '{castle,null,null,null,null,null,null,null,null,null,null,null}', p_levels => '{1,0,0,0,0,0,0,0,0,0,0,0}') = 'refused', 'bâtiment inconnu refusé');
select pg_temp.check(pg_temp.save(1, '00000000-0000-0000-0000-000000000001', p_plots => '{forge,bakery}', p_levels => '{3,1}') = 'refused', 'emplacements manquants refusés');
select pg_temp.check(pg_temp.save(1, '00000000-0000-0000-0000-000000000001', p_gold => 'NaN') = 'refused', 'or invalide refusé');
select pg_temp.check(pg_temp.save(1, '00000000-0000-0000-0000-000000000001', p_killed => 10) = 'refused', 'compteur en baisse refusé');
select pg_temp.check(pg_temp.save(1, '00000000-0000-0000-0000-000000000001', p_achievements => '{"Pas Bon"}') = 'refused', 'succès invalide refusé');

select pg_temp.check(public.claim_frontier('00000000-0000-0000-0000-000000000002') ->> 'status' = 'ok', 'reprise sur un autre PC');
select pg_temp.check(pg_temp.save(1, '00000000-0000-0000-0000-000000000001', p_stage => 14) = 'elsewhere', 'ancien PC écarté');
select pg_temp.check(pg_temp.save(1, '00000000-0000-0000-0000-000000000002', p_stage => 14) = 'ok', 'nouveau PC accepté');
reset role;

update public.frontiers set saved_at = now() - interval '3 hours' where player_id = '77777777-7777-7777-7777-777777777777';

set role authenticated;
select pg_temp.check((public.claim_frontier('00000000-0000-0000-0000-000000000002') #>> '{frontier,away_seconds}')::numeric between 10790 and 10900, 'absence de 3 heures');
select pg_temp.check(pg_temp.save(2, '00000000-0000-0000-0000-000000000002', p_stage => 60) = 'ok', 'progression plausible après une absence');
select pg_temp.check(pg_temp.save(3, '00000000-0000-0000-0000-000000000002', p_stage => 1, p_max => 1, p_best => 60, p_souls => 36, p_retreats => 1, p_plots => '{bakery,forge,null,null,null,null,null,null,null,null,null,null}', p_levels => '{1,1,0,0,0,0,0,0,0,0,0,0}') = 'ok', 'repli puis village rebâti autrement');
select pg_temp.check(public.claim_frontier('00000000-0000-0000-0000-000000000002') #>> '{frontier,plots,0}' = 'bakery', 'bâtiments échangés');
select pg_temp.check(pg_temp.save(4, '00000000-0000-0000-0000-000000000002', p_stage => 1, p_max => 1, p_best => 60, p_souls => 10, p_retreats => 1) = 'refused', 'âmes perdues refusées');
select pg_temp.check(pg_temp.save(4, '00000000-0000-0000-0000-000000000002', p_stage => 1, p_max => 1, p_best => 60, p_souls => 2000000000, p_retreats => 60000000) = 'refused', 'replis gonflés refusés');
select pg_temp.check(pg_temp.save(4, '00000000-0000-0000-0000-000000000002', p_stage => 5000, p_souls => 36, p_retreats => 1) = 'refused', 'niveau courant impossible refusé');
select pg_temp.check(pg_temp.save(4, '00000000-0000-0000-0000-000000000002', p_stage => 1, p_max => 1, p_best => 59, p_souls => 36, p_retreats => 1) = 'refused', 'record en baisse refusé');
select pg_temp.check(public.rejoin_frontier('00000000-0000-0000-0000-000000000005') ->> 'status' = 'elsewhere', 'reconnexion sans voler une partie active');
select pg_temp.check(public.rejoin_frontier('00000000-0000-0000-0000-000000000002') ->> 'status' = 'ok', 'reconnexion de la session active');
reset role;

update public.frontiers set last_seen = now() - interval '10 minutes' where player_id = '77777777-7777-7777-7777-777777777777';

set role authenticated;
select pg_temp.check(public.rejoin_frontier('00000000-0000-0000-0000-000000000005') ->> 'status' = 'ok', 'reconnexion quand l''autre PC dort');
select pg_temp.check(pg_temp.save(4, '00000000-0000-0000-0000-000000000005', p_stage => 2, p_max => 2, p_best => 60, p_souls => 36, p_retreats => 1, p_plots => '{bakery,forge,null,null,null,null,null,null,null,null,null,null}', p_levels => '{2,1,0,0,0,0,0,0,0,0,0,0}') = 'ok', 'la session reprise sauvegarde');
select pg_temp.check(public.claim_frontier('00000000-0000-0000-0000-000000000005') #>> '{frontier,saved_by}' = '00000000-0000-0000-0000-000000000005', 'le serveur dit qui a sauvegardé');
select pg_temp.check(pg_temp.save(5, '00000000-0000-0000-0000-000000000005', p_stage => 2, p_max => 2, p_best => 60, p_souls => 36, p_retreats => 1, p_plots => '{bakery,forge,null,null,null,null,null}', p_levels => '{2,1,0,0,0,0,0}') = 'refused', 'un client à 7 emplacements est refusé');
select pg_temp.check(pg_temp.save(5, '00000000-0000-0000-0000-000000000005', p_stage => 2, p_max => 2, p_best => 60, p_souls => 36, p_retreats => 1, p_plots => '{bakery,forge,null,null,null,null,null,null,null,null,null,null}', p_levels => '{2,1,0,0,0,0,0,0,0,0,0,0}', p_achievements => '{monstres-100,triche-1,triche-2}') = 'ok', 'succès inconnus ignorés');
select pg_temp.check(public.claim_frontier('00000000-0000-0000-0000-000000000005') #> '{frontier,achievements}' = '["monstres-100"]', 'seuls les vrais succès sont gardés');
reset role;

delete from auth.users where id = '77777777-7777-7777-7777-777777777777';
select pg_temp.check(not exists (select 1 from public.frontier_plots where player_id = '77777777-7777-7777-7777-777777777777'), 'un compte supprimé emporte sa partie');
select pg_temp.check((select count(*) from public.plots) = 12 and (select max(unlock_stage) from public.plots) = 101, 'douze emplacements, le dernier au boss 100');
select pg_temp.check((select count(*) from public.building_kinds) = 12, 'douze bâtiments');
select pg_temp.check((select count(*) from public.achievement_kinds) = 30, 'trente succès connus');
select pg_temp.check((select min_client_version from private.settings) = '0.4.2', 'la 0.4.2 est exigée');
