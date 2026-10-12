\set ON_ERROR_STOP on

insert into auth.users (id) values
  ('77777777-7777-7777-7777-777777777777'),
  ('88888888-8888-8888-8888-888888888888'),
  ('99999999-9999-9999-9999-999999999999');

insert into public.taverns (player_id, name, friend_code, renown)
values ('99999999-9999-9999-9999-999999999999', 'Le Pot d''Étain', 'ZZZ999', 15000);

insert into private.veterans (player_id, tier, tavern_name)
values ('88888888-8888-8888-8888-888888888888', 4, 'La Grande Chope');

create function pg_temp.check(p_condition boolean, p_message text) returns void
language plpgsql
as $$
begin
  if p_condition is not true then
    raise exception 'Échec : %', p_message;
  end if;
end;
$$;

grant execute on function pg_temp.check(boolean, text) to authenticated;

select set_config('request.jwt.claim.sub', '77777777-7777-7777-7777-777777777777', false);
set role authenticated;
select pg_temp.check(public.claim_frontier('00000000-0000-0000-0000-000000000001') -> 'frontier' = 'null'::jsonb, 'pas de partie avant le départ');
select pg_temp.check(public.start_frontier('00000000-0000-0000-0000-000000000001') #>> '{frontier,revision}' = '0', 'partie créée');
select pg_temp.check(public.start_frontier('00000000-0000-0000-0000-000000000001') #>> '{frontier,veteran_souls}' = '0', 'pas de cadeau sans taverne');
select pg_temp.check(public.save_frontier('{"stage": 12, "max_stage": 12, "best_stage": 12, "souls": 0, "retreats": 0}', 0, '00000000-0000-0000-0000-000000000001') ->> 'status' = 'ok', 'sauvegarde acceptée');
select pg_temp.check(public.claim_frontier('00000000-0000-0000-0000-000000000001') #>> '{frontier,revision}' = '1', 'révision suivante');
select pg_temp.check(public.claim_frontier('00000000-0000-0000-0000-000000000001') #>> '{frontier,save,stage}' = '12', 'sauvegarde relue');
select pg_temp.check(public.save_frontier('{"stage": 13}', 0, '00000000-0000-0000-0000-000000000001') ->> 'status' = 'conflict', 'ancienne révision refusée');
select pg_temp.check(public.save_frontier('{"stage": 13}', 1, '00000000-0000-0000-0000-000000000002') ->> 'status' = 'elsewhere', 'autre session refusée');
select pg_temp.check(public.save_frontier('{"stage": 900, "best_stage": 900}', 1, '00000000-0000-0000-0000-000000000001') ->> 'status' = 'refused', 'niveau impossible refusé');
select pg_temp.check(public.save_frontier('{"best_stage": 20, "souls": 500, "retreats": 1}', 1, '00000000-0000-0000-0000-000000000001') ->> 'status' = 'refused', 'âmes impossibles refusées');
select pg_temp.check(public.save_frontier('"pas un objet"', 1, '00000000-0000-0000-0000-000000000001') ->> 'status' = 'refused', 'sauvegarde invalide refusée');
select pg_temp.check(public.claim_frontier('00000000-0000-0000-0000-000000000002') ->> 'status' = 'ok', 'reprise sur un autre PC');
select pg_temp.check(public.save_frontier('{"stage": 14, "best_stage": 14}', 1, '00000000-0000-0000-0000-000000000001') ->> 'status' = 'elsewhere', 'ancien PC écarté');
select pg_temp.check(public.save_frontier('{"stage": 14, "best_stage": 14}', 1, '00000000-0000-0000-0000-000000000002') ->> 'status' = 'ok', 'nouveau PC accepté');
reset role;

update public.frontiers set saved_at = now() - interval '3 hours' where player_id = '77777777-7777-7777-7777-777777777777';

set role authenticated;
select pg_temp.check((public.claim_frontier('00000000-0000-0000-0000-000000000002') #>> '{frontier,away_seconds}')::numeric between 10790 and 10900, 'absence de 3 heures');
select pg_temp.check(public.save_frontier('{"stage": 60, "max_stage": 60, "best_stage": 60}', 2, '00000000-0000-0000-0000-000000000002') ->> 'status' = 'ok', 'progression plausible après une absence');
select pg_temp.check(public.save_frontier('{"stage": 1, "max_stage": 1, "best_stage": 60, "souls": 36, "retreats": 1}', 3, '00000000-0000-0000-0000-000000000002') ->> 'status' = 'ok', 'repli plausible');
select pg_temp.check(public.save_frontier('{"stage": 1, "best_stage": 60, "souls": 10, "retreats": 1}', 4, '00000000-0000-0000-0000-000000000002') ->> 'status' = 'refused', 'âmes perdues refusées');

select set_config('request.jwt.claim.sub', '99999999-9999-9999-9999-999999999999', false);
select pg_temp.check(public.start_frontier('00000000-0000-0000-0000-000000000003') #>> '{frontier,veteran_souls}' = '10', 'cadeau de l''Auberge');
select pg_temp.check(public.claim_frontier('00000000-0000-0000-0000-000000000003') #>> '{frontier,veteran_name}' = 'Le Pot d''Étain', 'enseigne de la taverne');

select set_config('request.jwt.claim.sub', '88888888-8888-8888-8888-888888888888', false);
select pg_temp.check(public.start_frontier('00000000-0000-0000-0000-000000000004') #>> '{frontier,veteran_souls}' = '20', 'cadeau gardé après l''effacement des tavernes');
select pg_temp.check(public.save_frontier('{"best_stage": 1, "souls": 20, "retreats": 0}', 0, '00000000-0000-0000-0000-000000000004') ->> 'status' = 'ok', 'les âmes du cadeau sont plausibles');
reset role;
