\set ON_ERROR_STOP on

insert into auth.users (id) values ('a7777777-7777-7777-7777-7777777777a7');

create function pg_temp.check(p_condition boolean, p_message text) returns void
language plpgsql
as $$
begin
  if p_condition is not true then
    raise exception 'Échec : %', p_message;
  end if;
end;
$$;

create function pg_temp.revision() returns integer
language sql
as $$
  select (public.claim_frontier('00000000-0000-0000-0000-0000000000a7') #>> '{frontier,revision}')::integer;
$$;

create function pg_temp.save(p_gold double precision, p_lifetime double precision, p_fighters integer[], p_companions text[]) returns text
language sql
as $$
  select public.save_frontier(pg_temp.revision(), '00000000-0000-0000-0000-0000000000a7', p_gold, p_lifetime, 0, 0, 31, 31, 31, 0, false,
    p_fighters, '{forge,bakery,null,null,null,null,null,null,null,null,null,null}', '{3,1,0,0,0,0,0,0,0,0,0,0}',
    '{0,0,0}', '{}', 1000, 3, 0, 0, 0, 0, p_companions, '{10,20,0}') ->> 'status';
$$;

create function pg_temp.buy(p_fighter text, p_slot text, p_rarity text, p_revision integer default null) returns jsonb
language sql
as $$
  select public.buy_item('00000000-0000-0000-0000-0000000000a7', coalesce(p_revision, pg_temp.revision()), p_fighter, p_slot, p_rarity);
$$;

create function pg_temp.upgrade(p_item bigint, p_count integer) returns jsonb
language sql
as $$
  select public.upgrade_item('00000000-0000-0000-0000-0000000000a7', pg_temp.revision(), p_item, p_count);
$$;

create function pg_temp.worn(p_fighter text, p_slot text) returns jsonb
language sql
as $$
  select item from jsonb_array_elements(public.get_social() #> '{items,bag}') item
  where item ->> 'fighter' = p_fighter and item ->> 'slot' = p_slot;
$$;

grant execute on function pg_temp.check(boolean, text), pg_temp.revision(), pg_temp.save(double precision, double precision, integer[], text[]),
  pg_temp.buy(text, text, text, integer), pg_temp.upgrade(bigint, integer), pg_temp.worn(text, text) to authenticated;

select set_config('request.jwt.claim.sub', 'a7777777-7777-7777-7777-7777777777a7', false);
set role authenticated;
select public.start_frontier('00000000-0000-0000-0000-0000000000a7');
select public.set_hero('00000000-0000-0000-0000-0000000000a7', 'Brune', 'tank', 0, 0, 0, 0);
select pg_temp.check(pg_temp.save(1e9, 1e9, '{20,5,0}', '{support,null}') = 'ok', 'partie au niveau 31 avec un soutien');
select pg_temp.check(public.claim_frontier('00000000-0000-0000-0000-0000000000a7') #> '{frontier,xp}' = '[10, 20, 0]'::jsonb, 'xp relue');

select set_config('test.reply', pg_temp.buy('hero', 'weapon', 'common')::text, false);
select pg_temp.check(current_setting('test.reply')::jsonb ->> 'status' = 'ok', 'arme achetée');
reset role;
select pg_temp.check(abs((current_setting('test.reply')::jsonb ->> 'spent')::numeric
  - 30 * (private.frontier_hp(31) * 0.35 + 2)) < 0.01, 'prix fixé par le serveur');
select pg_temp.check((select gold from public.frontiers where player_id = 'a7777777-7777-7777-7777-7777777777a7')
  = 1e9 - (current_setting('test.reply')::jsonb ->> 'spent')::double precision, 'or débité par le serveur');
select pg_temp.check((select saved_by is null from public.frontiers where player_id = 'a7777777-7777-7777-7777-7777777777a7'), 'un achat n''est pas une sauvegarde du jeu');
set role authenticated;
select pg_temp.check((current_setting('test.reply')::jsonb ->> 'revision')::integer = pg_temp.revision(), 'nouvelle révision rendue');
select pg_temp.check(pg_temp.worn('hero', 'weapon') ->> 'role' = 'tank', 'arme de la classe du héros');
select pg_temp.check((pg_temp.worn('hero', 'weapon') ->> 'bound')::boolean and pg_temp.worn('hero', 'weapon') ->> 'source' = 'shop', 'objet acheté lié');
select pg_temp.check(pg_temp.buy('hero', 'armor', 'common', 0) ->> 'status' = 'conflict', 'ancienne révision refusée');
select pg_temp.check(pg_temp.buy('hero', 'armor', 'legendary') ->> 'status' = 'refused', 'pas de légendaire en boutique');
select pg_temp.check(pg_temp.buy('mage', 'armor', 'common') ->> 'status' = 'unknown_fighter', 'second compagnon pas encore là');
select pg_temp.check(pg_temp.buy('archer', 'weapon', 'epic') ->> 'status' = 'ok', 'arme épique pour le soutien');
select pg_temp.check(pg_temp.worn('archer', 'weapon') ->> 'role' = 'support', 'arme de la classe du compagnon');

select set_config('test.item', pg_temp.worn('hero', 'weapon') ->> 'id', false);
select set_config('test.reply', pg_temp.upgrade(current_setting('test.item')::bigint, 3)::text, false);
select pg_temp.check(current_setting('test.reply')::jsonb ->> 'status' = 'ok', 'arme améliorée trois fois');
select pg_temp.check((pg_temp.worn('hero', 'weapon') ->> 'upgrades')::integer = 3, 'trois améliorations');
reset role;
select pg_temp.check(abs((current_setting('test.reply')::jsonb ->> 'spent')::numeric
  - 30 * (private.frontier_hp(31) * 0.35 + 2) * 0.5 * (1.25 ^ 3 - 1) / 0.25) < 0.01, 'améliorations au prix du record');
set role authenticated;
select pg_temp.check(pg_temp.upgrade(current_setting('test.item')::bigint, 98) ->> 'status' = 'maxed', 'cent améliorations au plus');
select pg_temp.check(pg_temp.upgrade(current_setting('test.item')::bigint, 0) ->> 'status' = 'refused', 'au moins une amélioration');
select pg_temp.check(pg_temp.save(1, 1e9, '{20,5,0}', '{support,null}') = 'ok', 'or presque vide');
select pg_temp.check(pg_temp.save(5e9, 1e9, '{20,5,0}', '{support,null}') = 'refused', 'or inventé sans gain refusé');
select pg_temp.check(pg_temp.buy('hero', 'helm', 'common') ->> 'status' = 'too_poor', 'pas assez d''or');
select pg_temp.check(pg_temp.upgrade(current_setting('test.item')::bigint, 1) ->> 'status' = 'too_poor', 'pas assez d''or pour améliorer');
select set_config('test.status', pg_temp.save(1e9, 2e9, '{20,5,0}', '{support,null}'), false);
select pg_temp.check(current_setting('test.status') = 'ok', 'or gagné depuis : ' || current_setting('test.status'));
reset role;
update private.settings set bag_size = 2;
set role authenticated;
select pg_temp.check(pg_temp.buy('hero', 'helm', 'common') ->> 'status' = 'bag_full', 'sac plein');
reset role;
update private.settings set bag_size = 60;
set role authenticated;
select pg_temp.check(pg_temp.save(1e9, 2e9, '{1,0,0}', '{}') = 'ok', 'repli : le soutien repart');
select pg_temp.check(pg_temp.worn('archer', 'weapon') ->> 'role' = 'support', 'son équipement attend son retour');
select pg_temp.check(public.equip_item(current_setting('test.item')::bigint, 'archer') ->> 'status' = 'unknown_fighter', 'pas d''équipement sans compagnon');
select pg_temp.check(pg_temp.save(1e9, 2e9, '{1,1,0}', '{tank,null}') = 'ok', 'il revient en tank');
select pg_temp.check(pg_temp.worn('archer', 'weapon') is null, 'l''arme du soutien quitte un compagnon qui change de classe');
select pg_temp.check(exists (select 1 from jsonb_array_elements(public.get_social() #> '{items,bag}') item where item ->> 'role' = 'support'),
  'elle reste dans le sac');
reset role;

delete from auth.users where id = 'a7777777-7777-7777-7777-7777777777a7';
select pg_temp.check(not exists (select 1 from public.items where owner_id = 'a7777777-7777-7777-7777-7777777777a7'), 'les achats partent avec le compte');
