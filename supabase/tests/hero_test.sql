\set ON_ERROR_STOP on

insert into auth.users (id) values ('88888888-8888-8888-8888-888888888888');

create function pg_temp.check(p_condition boolean, p_message text) returns void
language plpgsql
as $$
begin
  if p_condition is not true then
    raise exception 'Échec : %', p_message;
  end if;
end;
$$;

create function pg_temp.hero(p_name text, p_role text, p_skin integer default 1, p_outfit integer default 4,
  p_session uuid default '00000000-0000-0000-0000-000000000008')
returns jsonb
language sql
as $$
  select public.set_hero(p_session, p_name, p_role, p_skin, 2, 3, p_outfit);
$$;

create function pg_temp.save(p_revision integer, p_armor integer) returns text
language sql
as $$
  select public.save_frontier(p_revision, '00000000-0000-0000-0000-000000000008', 100, 1e10, 0, 0, 12, 12, 12, 0, false,
    '{5,1,0}', '{forge,bakery,null,null,null,null,null,null,null,null,null,null}', '{3,1,0,0,0,0,0,0,0,0,0,0}',
    '{0,0,0}', '{}', 1000, 0, 0, 0, 0, p_armor) ->> 'status';
$$;

grant execute on function pg_temp.check(boolean, text) to authenticated;
grant execute on function pg_temp.hero(text, text, integer, integer, uuid) to authenticated;
create function pg_temp.squad(p_companions text[]) returns jsonb
language sql
as $$
  select public.save_frontier((public.claim_frontier('00000000-0000-0000-0000-000000000008') #>> '{frontier,revision}')::integer,
    '00000000-0000-0000-0000-000000000008', 100, 1e10, 0, 0, 12, 12, 12, 0, false,
    '{5,1,0}', '{forge,bakery,null,null,null,null,null,null,null,null,null,null}', '{3,1,0,0,0,0,0,0,0,0,0,0}',
    '{0,0,0}', '{}', 1000, 0, 0, 0, 0, 12, p_companions);
$$;

grant execute on function pg_temp.save(integer, integer) to authenticated;
grant execute on function pg_temp.squad(text[]) to authenticated;

select set_config('request.jwt.claim.sub', '88888888-8888-8888-8888-888888888888', false);
set role authenticated;
select pg_temp.check(pg_temp.hero('Aldric', 'tank') ->> 'status' = 'missing', 'pas de héros sans partie');
select pg_temp.check(public.start_frontier('00000000-0000-0000-0000-000000000008') #> '{frontier,hero_name}' = 'null'::jsonb, 'héros à créer');
select pg_temp.check(public.claim_frontier('00000000-0000-0000-0000-000000000008') #>> '{frontier,hero_role}' = 'warrior', 'guerrier par défaut');

select pg_temp.check(pg_temp.hero('Aldric', 'tank', p_session => '00000000-0000-0000-0000-000000000009') ->> 'status' = 'elsewhere', 'autre PC refusé');
select pg_temp.check(pg_temp.hero('Al', 'tank') ->> 'status' = 'invalid_name', 'nom trop court');
select pg_temp.check(pg_temp.hero('Aldric le Magnifique', 'tank') ->> 'status' = 'invalid_name', 'nom trop long');
select pg_temp.check(pg_temp.hero('Aldric<b>', 'tank') ->> 'status' = 'invalid_name', 'caractères interdits');
select pg_temp.check(pg_temp.hero('Gros Connard', 'tank') ->> 'status' = 'banned_name', 'insulte refusée');
select pg_temp.check(pg_temp.hero('MegaFuckeur', 'tank') ->> 'status' = 'banned_name', 'insulte cachée refusée');
select pg_temp.check(pg_temp.hero('P D', 'tank') ->> 'status' = 'banned_name', 'insulte espacée refusée');
select pg_temp.check(pg_temp.hero('Constance', 'tank') ->> 'status' = 'ok', 'un prénom qui commence comme une insulte passe');
select pg_temp.check(pg_temp.hero('Aldric', 'ninja') ->> 'status' = 'refused', 'rôle inconnu refusé');
select pg_temp.check(pg_temp.hero('Aldric', 'tank', p_skin => 4) ->> 'status' = 'refused', 'teinte inconnue refusée');

select pg_temp.check(pg_temp.hero('  Jean-Éric   d''Arc ', 'tank') #>> '{frontier,hero_name}' = 'Jean-Éric d''Arc', 'nom nettoyé');
select pg_temp.check(pg_temp.hero('Aldric', 'mage') ->> 'status' = 'role_locked', 'rôle figé jusqu''au repli');
select pg_temp.check(pg_temp.hero('Aldric', 'tank', p_outfit => 7) #>> '{frontier,hero_outfit}' = '7', 'apparence changée à tout moment');
select pg_temp.check(public.claim_frontier('00000000-0000-0000-0000-000000000008') #>> '{frontier,hero_name}' = 'Aldric', 'nom relu');

select pg_temp.check(pg_temp.save(0, -1) = 'refused', 'armure négative refusée');
select pg_temp.check(pg_temp.save(0, 12) = 'ok', 'armure sauvegardée');
select pg_temp.check(public.claim_frontier('00000000-0000-0000-0000-000000000008') #>> '{frontier,armor}' = '12', 'armure relue');
select pg_temp.check(public.claim_frontier('00000000-0000-0000-0000-000000000008') #>> '{frontier,hero_role}' = 'tank', 'la sauvegarde garde le rôle');
select pg_temp.check(pg_temp.squad('{support,warrior}') #> '{frontier,companions}' = '["support", null]'::jsonb, 'classe du compagnon recruté gardée');
select pg_temp.check(pg_temp.squad('{dragon,null}') ->> 'status' = 'refused', 'classe inconnue refusée');
select pg_temp.check(pg_temp.squad(null) #> '{frontier,companions}' = '[null, null]'::jsonb, 'sans classe, le compagnon garde la sienne par défaut');
select pg_temp.check(not (pg_temp.squad('{tank}') -> 'frontier' ? 'intendant_on'), 'plus d''intendant');
reset role;

update public.frontiers set retreats = 1, stage = 1, max_stage = 1 where player_id = '88888888-8888-8888-8888-888888888888';

set role authenticated;
select pg_temp.check(pg_temp.hero('Aldric', 'mage') #>> '{frontier,role_retreats}' = '1', 'rôle changé après un repli');
select pg_temp.check(pg_temp.hero('Aldric', 'archer') ->> 'status' = 'role_locked', 'un seul changement par repli');
select pg_temp.check(public.claim_frontier('00000000-0000-0000-0000-000000000008') #>> '{frontier,hero_role}' = 'mage', 'nouveau rôle relu');
reset role;

update public.frontiers set retreats = 2, stage = 1, max_stage = 1 where player_id = '88888888-8888-8888-8888-888888888888';

set role authenticated;
select pg_temp.check(pg_temp.hero('Aldric', 'mage') #>> '{frontier,role_retreats}' = '2', 'garder la même classe après un repli compte comme le choix');
select pg_temp.check(pg_temp.hero('Aldric', 'archer') ->> 'status' = 'role_locked', 'plus de changement après avoir gardé sa classe');
reset role;

update public.frontiers set retreats = 3, best_stage = greatest(best_stage, 11), max_stage = 11, stage = 11
where player_id = '88888888-8888-8888-8888-888888888888';

set role authenticated;
select pg_temp.check(pg_temp.hero('Aldric', 'archer') ->> 'status' = 'role_locked', 'pas de changement de classe au-delà du niveau 10 de la partie');
reset role;

update public.frontiers set stage = 10, max_stage = 10 where player_id = '88888888-8888-8888-8888-888888888888';

set role authenticated;
select pg_temp.check(pg_temp.hero('Aldric', 'archer') #>> '{frontier,hero_role}' = 'archer', 'changement de classe accepté au début de la partie');
reset role;

delete from auth.users where id = '88888888-8888-8888-8888-888888888888';
select pg_temp.check((select count(*) from public.role_kinds) = 5, 'cinq rôles');
