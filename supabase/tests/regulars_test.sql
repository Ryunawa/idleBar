\set ON_ERROR_STOP on

insert into auth.users (id) values ('55555555-5555-5555-5555-555555555555');

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

select set_config('request.jwt.claim.sub', '55555555-5555-5555-5555-555555555555', false);
set role authenticated;
select public.found_tavern('Aux Habitués');
select pg_temp.check(jsonb_array_length(public.get_world() -> 'regulars') = 12, 'douze habitués');
select pg_temp.check(jsonb_array_length(public.get_world() #> '{regulars,0,chapters}') = 5, 'cinq chapitres');
reset role;

update public.taverns set reported_at = now() - interval '5 minutes' where player_id = '55555555-5555-5555-5555-555555555555';
delete from public.tavern_goals where player_id = '55555555-5555-5555-5555-555555555555';
insert into public.tavern_goals (player_id, day, goal_id) values ('55555555-5555-5555-5555-555555555555', current_date, 'habitue-1');

set role authenticated;
select public.report_service('{"drinks": {"beer": 3}, "perfect": 1, "coins": 15, "regulars": {"gaspard": {"served": 2, "perfect": 1}, "inconnu": {"served": 5}}}');
select pg_temp.check(
  public.get_state() #> '{tavern,regulars}' = '[{"id": "gaspard", "chapter": 1, "visits": 2, "friendship": 3}]'::jsonb,
  'Gaspard : amitié 3, premier chapitre');
select pg_temp.check((public.get_state() #>> '{tavern,goals,0,done}')::boolean, 'objectif « servir un habitué » rempli');
select pg_temp.check((public.get_state() #>> '{tavern,coins}')::integer = 15 + 80, 'récompense de l''objectif');
select public.report_service('{"drinks": {"beer": 1}, "regulars": {"gaspard": {"served": 9}}}');
select pg_temp.check((public.get_state() #>> '{tavern,regulars,0,visits}')::integer = 3, 'habitué plafonné aux services acceptés');
reset role;

update public.taverns set reported_at = now() - interval '10 minutes' where player_id = '55555555-5555-5555-5555-555555555555';
update public.tavern_regulars set friendship = 38 where player_id = '55555555-5555-5555-5555-555555555555';
set role authenticated;
select public.report_service('{"drinks": {"beer": 2}, "coins": 8, "regulars": {"gaspard": {"served": 2}}}');
select pg_temp.check((public.get_state() #>> '{tavern,regulars,0,chapter}')::integer = 5, 'dernier chapitre à 40');
select pg_temp.check((public.get_state() #>> '{tavern,coins}')::integer = 95 + 8 + 250, 'souvenir : 250 écus');
reset role;
