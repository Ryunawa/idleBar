\set ON_ERROR_STOP on

insert into auth.users (id) values
  ('a1111111-1111-1111-1111-111111111111'), ('b2222222-2222-2222-2222-222222222222'),
  ('c3333333-3333-3333-3333-333333333333'), ('d4444444-4444-4444-4444-444444444444');

create function pg_temp.check(p_condition boolean, p_message text) returns void
language plpgsql
as $$
begin
  if p_condition is not true then
    raise exception 'Échec : %', p_message;
  end if;
end;
$$;

create function pg_temp.as_player(p_player text) returns void
language sql
as $$
  select set_config('request.jwt.claim.sub', p_player, false);
$$;

create function pg_temp.session(p_player text) returns uuid
language sql
as $$
  select ('00000000-0000-0000-0000-' || substr(p_player, 1, 1) || '00000000000')::uuid;
$$;

create function pg_temp.member(p_name text) returns bigint
language sql
as $$
  select (member ->> 'id')::bigint
  from jsonb_array_elements(public.get_social() #> '{guild,members}') member
  where member ->> 'name' = p_name;
$$;

create function pg_temp.save(p_player text, p_revision integer, p_killed bigint) returns text
language sql
as $$
  select public.save_frontier(p_revision, pg_temp.session(p_player), 100, 1e10, 0, 0, 12, 12, 12, 0, false,
    '{5,1,0}', '{forge,bakery,null,null,null,null,null,null,null,null,null,null}', '{3,1,0,0,0,0,0,0,0,0,0,0}',
    '{0,0,0}', false, false, '{}', p_killed, 0, 0, 0, 0, 0) ->> 'status';
$$;

grant execute on function pg_temp.check(boolean, text), pg_temp.as_player(text), pg_temp.session(text), pg_temp.member(text),
  pg_temp.save(text, integer, bigint) to authenticated;

set role authenticated;
select pg_temp.as_player('a1111111-1111-1111-1111-111111111111');
select pg_temp.check(public.get_social() ->> 'status' = 'missing', 'pas de guilde sans partie');
select public.start_frontier(pg_temp.session('a1111111-1111-1111-1111-111111111111'));
select pg_temp.check(public.create_guild('Les Lames') ->> 'status' = 'no_hero', 'un héros avant la guilde');
select public.set_hero(pg_temp.session('a1111111-1111-1111-1111-111111111111'), 'Aldric', 'tank', 0, 0, 0, 0);
select pg_temp.as_player('b2222222-2222-2222-2222-222222222222');
select public.start_frontier(pg_temp.session('b2222222-2222-2222-2222-222222222222'));
select public.set_hero(pg_temp.session('b2222222-2222-2222-2222-222222222222'), 'Brune', 'mage', 1, 1, 1, 1);
select pg_temp.as_player('c3333333-3333-3333-3333-333333333333');
select public.start_frontier(pg_temp.session('c3333333-3333-3333-3333-333333333333'));
select public.set_hero(pg_temp.session('c3333333-3333-3333-3333-333333333333'), 'Corentin', 'support', 2, 2, 2, 2);
select pg_temp.as_player('d4444444-4444-4444-4444-444444444444');
select public.start_frontier(pg_temp.session('d4444444-4444-4444-4444-444444444444'));
select public.set_hero(pg_temp.session('d4444444-4444-4444-4444-444444444444'), 'Dalia', 'archer', 3, 0, 3, 3);

select pg_temp.as_player('a1111111-1111-1111-1111-111111111111');
select pg_temp.check(public.get_social() -> 'guild' = 'null'::jsonb, 'sans guilde au départ');
select pg_temp.check(public.create_guild('Al') ->> 'status' = 'invalid_name', 'nom de guilde trop court');
select pg_temp.check(public.create_guild('Les Connards') ->> 'status' = 'banned_name', 'nom de guilde insultant');
select pg_temp.check(public.create_guild('  Les   Lames ') #>> '{guild,name}' = 'Les Lames', 'guilde fondée');
select pg_temp.check(public.get_social() #>> '{guild,code}' ~ '^[A-HJ-NP-Z2-9]{6}$', 'code de 6 caractères');
select pg_temp.check((public.get_social() #>> '{guild,leader}')::boolean, 'le fondateur est chef');
select pg_temp.check(public.create_guild('Autre Guilde') ->> 'status' = 'already_member', 'une seule guilde');

select pg_temp.as_player('b2222222-2222-2222-2222-222222222222');
select pg_temp.check(public.create_guild('les lames') ->> 'status' = 'name_taken', 'nom déjà pris');
select pg_temp.check(public.join_guild('ZZZZZZ') ->> 'status' = 'unknown_code', 'code inconnu');
reset role;
select set_config('test.code', (select code from public.guilds where name = 'Les Lames'), false);
update private.settings set guild_max_members = 2;
set role authenticated;
select pg_temp.check(jsonb_array_length(public.join_guild(lower(current_setting('test.code'))) #> '{guild,members}') = 2, 'rejoint par le code');

select pg_temp.as_player('c3333333-3333-3333-3333-333333333333');
select pg_temp.check(public.join_guild(current_setting('test.code')) ->> 'status' = 'full', 'guilde pleine');
reset role;
update private.settings set guild_max_members = 20;
set role authenticated;
select pg_temp.check(public.join_guild(current_setting('test.code')) ->> 'status' = 'ok', 'place libérée');

select pg_temp.as_player('b2222222-2222-2222-2222-222222222222');
select pg_temp.check(public.kick_member(pg_temp.member('Corentin')) ->> 'status' = 'not_leader', 'seul le chef exclut');
select pg_temp.as_player('a1111111-1111-1111-1111-111111111111');
select pg_temp.check(jsonb_array_length(public.kick_member(pg_temp.member('Corentin')) #> '{guild,members}') = 2, 'membre exclu');
select pg_temp.as_player('c3333333-3333-3333-3333-333333333333');
select pg_temp.check(public.join_guild(current_setting('test.code')) ->> 'status' = 'ok', 'un exclu peut revenir');

select pg_temp.as_player('a1111111-1111-1111-1111-111111111111');
select pg_temp.check(public.rename_guild('Les Épées') #>> '{guild,name}' = 'Les Épées', 'guilde renommée');
select pg_temp.check(public.new_guild_code() #>> '{guild,code}' <> current_setting('test.code'), 'nouveau code');
select pg_temp.check(public.join_guild(current_setting('test.code')) ->> 'status' = 'already_member', 'déjà membre');
reset role;
select set_config('test.code', (select code from public.guilds where name = 'Les Épées'), false);
set role authenticated;
select pg_temp.check(public.leave_guild() -> 'guild' = 'null'::jsonb, 'départ du chef');
select pg_temp.check(public.leave_guild() ->> 'status' = 'not_member', 'déjà parti');
select pg_temp.check(public.join_guild(current_setting('test.code')) ->> 'status' = 'cooldown', 'une heure avant de revenir');
select pg_temp.check((public.get_social() ->> 'rejoin_in')::numeric between 3500 and 3600, 'attente annoncée');
select pg_temp.as_player('b2222222-2222-2222-2222-222222222222');
select pg_temp.check((public.get_social() #>> '{guild,leader}')::boolean, 'le plus ancien devient chef');
reset role;
update public.guild_departures set left_at = now() - interval '2 hours';
set role authenticated;
select pg_temp.as_player('a1111111-1111-1111-1111-111111111111');
select pg_temp.check(public.join_guild(current_setting('test.code')) ->> 'status' = 'ok', 'retour après une heure');

reset role;
update public.frontier_fighters set level = 200 where player_id = 'a1111111-1111-1111-1111-111111111111' and fighter = 'hero';
set role authenticated;
select pg_temp.check(public.send_help(pg_temp.member('Aldric'), 'hero') ->> 'status' = 'self', 'pas d''aide à soi-même');
select pg_temp.check(public.send_help(pg_temp.member('Brune'), 'hero') ->> 'status' = 'ok', 'héros envoyé');
select pg_temp.check(public.send_help(pg_temp.member('Corentin'), 'hero') ->> 'status' = 'cooldown', 'quatre heures entre deux renforts');
select pg_temp.check(public.send_help(pg_temp.member('Brune'), 'convoy') ->> 'status' = 'ok', 'convoi envoyé');
select pg_temp.check((public.get_social() #>> '{help,hero_ready_in}')::numeric > 14000, 'recharge annoncée');
select pg_temp.check((select (member ->> 'heroes')::integer from jsonb_array_elements(public.get_social() #> '{guild,members}') member where member ->> 'name' = 'Brune') = 1, 'renfort visible des membres');
select pg_temp.as_player('b2222222-2222-2222-2222-222222222222');
select pg_temp.check(public.get_social() #>> '{help,allies,0,name}' = 'Aldric', 'le renfort arrive');
select pg_temp.check((public.get_social() #>> '{help,allies,0,ratio}')::numeric = 1.2, 'puissance plafonnée à +20 %');
select pg_temp.check((public.get_social() #>> '{help,allies,0,ends_in}')::numeric between 1790 and 1800, 'renfort pour 30 min');
select pg_temp.check((public.get_social() #>> '{help,boosts,0,gold}')::integer = 50, 'convoi : +50 % d''or');
select pg_temp.as_player('c3333333-3333-3333-3333-333333333333');
select pg_temp.check(public.send_help(pg_temp.member('Brune'), 'convoy') ->> 'status' = 'ok', 'second convoi');
select pg_temp.as_player('b2222222-2222-2222-2222-222222222222');
select pg_temp.check((public.get_social() #>> '{help,boosts,0,ends_in}')::numeric between 3590 and 3600, 'les convois se suivent');
reset role;
update public.guild_help set sent_at = now() - interval '5 hours';
update public.guild_help set ends_at = now() + interval '119 minutes 50 seconds' where kind = 'convoy';
update private.settings set help_front_max = 1;
set role authenticated;
select pg_temp.as_player('c3333333-3333-3333-3333-333333333333');
select pg_temp.check(public.send_help(pg_temp.member('Brune'), 'convoy') ->> 'status' = 'convoy_full', 'deux heures de convoi au plus');
select pg_temp.check(public.send_help(pg_temp.member('Brune'), 'hero') ->> 'status' = 'front_full', 'front plein');
select pg_temp.check(public.send_help(pg_temp.member('Aldric'), 'hero') ->> 'status' = 'ok', 'renfort ailleurs');
select pg_temp.as_player('a1111111-1111-1111-1111-111111111111');
select pg_temp.check((public.get_social() #>> '{help,allies,0,ratio}')::numeric <= 0.01, 'un renfort faible reste faible');
reset role;
update private.settings set help_front_max = 3, titan_min_hp = 1000;
delete from public.titans;
set role authenticated;

select pg_temp.as_player('b2222222-2222-2222-2222-222222222222');
select pg_temp.check((public.get_social() #>> '{titan,hp}')::bigint = 20000, 'titan à 5 000 PV par joueur actif');
select pg_temp.check(pg_temp.save('b2222222-2222-2222-2222-222222222222', 0, 500) = 'ok', 'sauvegarde avec des monstres');
select pg_temp.check((public.get_social() #>> '{titan,mine}')::bigint = 40, 'au plus 40 monstres par minute');
reset role;
update public.frontiers set saved_at = now() - interval '30 minutes' where player_id = 'b2222222-2222-2222-2222-222222222222';
set role authenticated;
select pg_temp.check(pg_temp.save('b2222222-2222-2222-2222-222222222222', 1, 5500) = 'ok', 'sauvegarde après 30 min');
select pg_temp.check((public.get_social() #>> '{titan,mine}')::bigint = 1280, 'contribution plafonnée au temps écoulé');
select pg_temp.check(public.get_social() #>> '{titan,ranking,0,name}' = 'Les Épées', 'classement des guildes');
select pg_temp.check((public.get_social() #>> '{titan,ranking,0,mine}')::boolean, 'sa guilde est signalée');
select pg_temp.check(public.get_social() #> '{titan,claimable}' = 'null'::jsonb, 'rien à réclamer avant la chute');
select pg_temp.check(public.claim_titan() ->> 'status' = 'nothing', 'pas de récompense avant la chute');
reset role;
update public.titans set hp = 4000, damage = 3990;
update public.frontiers set saved_at = now() - interval '10 minutes' where player_id = 'b2222222-2222-2222-2222-222222222222';
set role authenticated;
select pg_temp.check(pg_temp.save('b2222222-2222-2222-2222-222222222222', 2, 6000) = 'ok', 'le coup final');
select pg_temp.check((public.get_social() #>> '{titan,fallen}')::boolean, 'le titan tombe');
select pg_temp.check((public.get_social() #>> '{titan,mine}')::bigint = 1290, 'pas plus que les PV restants');
select pg_temp.check(public.get_social() #> '{titan,claimable}' = '{"hours": 4, "percent": 50}'::jsonb, 'palier de 1 000 monstres');
select pg_temp.check(public.claim_titan() ->> 'status' = 'ok', 'récompense réclamée');
select pg_temp.check(public.claim_titan() ->> 'status' = 'nothing', 'une seule fois');
select pg_temp.check(exists (
  select 1 from jsonb_array_elements(public.get_social() #> '{help,boosts}') boost
  where boost ->> 'source' = 'titan' and (boost ->> 'damage')::integer = 50 and (boost ->> 'ends_in')::numeric > 14000), 'bonus de 4 h');
select pg_temp.as_player('c3333333-3333-3333-3333-333333333333');
select pg_temp.check(public.claim_titan() ->> 'status' = 'nothing', 'rien sans participation');
reset role;

delete from auth.users where id = 'a1111111-1111-1111-1111-111111111111';
select pg_temp.check(not exists (select 1 from public.guild_help where sender_id = 'a1111111-1111-1111-1111-111111111111'), 'un compte supprimé emporte ses aides');
delete from auth.users where id in ('b2222222-2222-2222-2222-222222222222', 'c3333333-3333-3333-3333-333333333333');
select pg_temp.check(not exists (select 1 from public.guilds where name = 'Les Épées'), 'une guilde vide disparaît');
delete from auth.users where id = 'd4444444-4444-4444-4444-444444444444';
