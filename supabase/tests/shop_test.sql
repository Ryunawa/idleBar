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

create function pg_temp.offer(p_position integer) returns bigint
language sql
as $$
  select (public.get_social() #>> array['shop', 'offers', p_position::text, 'id'])::bigint;
$$;

create function pg_temp.buy(p_offer bigint, p_revision integer default null) returns jsonb
language sql
as $$
  select public.buy_offer('00000000-0000-0000-0000-0000000000a7', coalesce(p_revision, pg_temp.revision()), p_offer);
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

create function pg_temp.bagged(p_role text) returns jsonb
language sql
as $$
  select item from jsonb_array_elements(public.get_social() #> '{items,bag}') item where item ->> 'role' = p_role limit 1;
$$;

grant execute on function pg_temp.check(boolean, text), pg_temp.revision(), pg_temp.save(double precision, double precision, integer[], text[]),
  pg_temp.offer(integer), pg_temp.buy(bigint, integer), pg_temp.upgrade(bigint, integer), pg_temp.worn(text, text), pg_temp.bagged(text)
  to authenticated;

select set_config('request.jwt.claim.sub', 'a7777777-7777-7777-7777-7777777777a7', false);
set role authenticated;
select public.start_frontier('00000000-0000-0000-0000-0000000000a7');
select public.set_hero('00000000-0000-0000-0000-0000000000a7', 'Brune', 'tank', 0, 0, 0, 0);
select pg_temp.check(pg_temp.save(1e9, 1e9, '{20,5,0}', '{support,null}') = 'ok', 'partie au niveau 31 avec un soutien');
select pg_temp.check(public.claim_frontier('00000000-0000-0000-0000-0000000000a7') #> '{frontier,xp}' = '[10, 20, 0]'::jsonb, 'xp relue');
select pg_temp.check(jsonb_array_length(public.get_social() #> '{shop,offers}') = 4, 'quatre offres en boutique');
select pg_temp.check((public.get_social() #>> '{shop,refresh_in}')::numeric between 0 and 10800, 'nouvel arrivage dans moins de trois heures');
select pg_temp.check(not exists (
  select 1 from jsonb_array_elements(public.get_social() #> '{shop,offers}') offer
  where (offer ->> 'sold')::boolean or (offer ->> 'level')::integer not between 15 and 23 or (offer ->> 'price')::numeric <= 0
    or (offer ->> 'slot' = 'weapon') <> (offer ->> 'role' is not null)), 'offres au niveau des combattants, armes avec une classe');
select pg_temp.check(public.get_social() #> '{shop,offers}' = public.get_social() #> '{shop,offers}', 'les offres restent les mêmes');
reset role;

update public.shop_offers set item_level = 5, slot = 'weapon', role = 'tank', rarity = 'common', stat_a = 'damage', value_a = 6, stat_b = null, value_b = null,
  price = private.gear_base(player_id, 31, 'common')
where player_id = 'a7777777-7777-7777-7777-7777777777a7' and position = 0;
update public.shop_offers set item_level = 5, slot = 'weapon', role = 'support', rarity = 'epic', stat_a = 'damage', value_a = 15, stat_b = 'healing', value_b = 7,
  price = private.gear_base(player_id, 31, 'epic')
where player_id = 'a7777777-7777-7777-7777-7777777777a7' and position = 1;
update public.shop_offers set item_level = 5, slot = 'helm', role = null, rarity = 'common', stat_a = 'health', value_a = 9, stat_b = null, value_b = null,
  price = private.gear_base(player_id, 31, 'common')
where player_id = 'a7777777-7777-7777-7777-7777777777a7' and position in (2, 3);
delete from public.items where owner_id = 'a7777777-7777-7777-7777-7777777777a7';

set role authenticated;
select set_config('test.reply', pg_temp.buy(pg_temp.offer(0))::text, false);
select pg_temp.check(current_setting('test.reply')::jsonb ->> 'status' = 'ok', 'arme achetée');
reset role;
select pg_temp.check(abs((current_setting('test.reply')::jsonb ->> 'spent')::numeric
  - 30 * (private.frontier_hp(31) * 0.35 + 2)) < 0.01, 'prix fixé par le serveur');
select pg_temp.check((select gold from public.frontiers where player_id = 'a7777777-7777-7777-7777-7777777777a7')
  = 1e9 - (current_setting('test.reply')::jsonb ->> 'spent')::double precision, 'or débité par le serveur');
select pg_temp.check((select saved_by is null from public.frontiers where player_id = 'a7777777-7777-7777-7777-7777777777a7'), 'un achat n''est pas une sauvegarde du jeu');
set role authenticated;
select pg_temp.check((current_setting('test.reply')::jsonb ->> 'revision')::integer = pg_temp.revision(), 'nouvelle révision rendue');
select pg_temp.check((public.get_social() #>> '{shop,offers,0,sold}')::boolean, 'offre vendue');
select pg_temp.check(pg_temp.buy(pg_temp.offer(0)) ->> 'status' = 'sold', 'une offre ne se vend qu''une fois');
select pg_temp.check(pg_temp.bagged('tank') ->> 'source' = 'shop' and (pg_temp.bagged('tank') ->> 'bound')::boolean
  and not (pg_temp.bagged('tank') ->> 'equipped')::boolean, 'objet acheté lié, dans le sac');
select pg_temp.check(pg_temp.buy(pg_temp.offer(1), 0) ->> 'status' = 'conflict', 'ancienne révision refusée');
select pg_temp.check(pg_temp.buy(pg_temp.offer(1)) ->> 'status' = 'ok', 'arme épique pour le soutien');
select pg_temp.check(public.equip_item((pg_temp.bagged('support') ->> 'id')::bigint, 'mage') ->> 'status' = 'unknown_fighter', 'second compagnon pas encore là');
select pg_temp.check(public.equip_item((pg_temp.bagged('support') ->> 'id')::bigint, 'archer') ->> 'status' = 'ok', 'arme équipée sur le soutien');
select pg_temp.check(public.equip_item((pg_temp.bagged('tank') ->> 'id')::bigint, 'hero') ->> 'status' = 'ok', 'arme équipée sur le héros');
select pg_temp.check(pg_temp.worn('hero', 'weapon') ->> 'role' = 'tank', 'arme de la classe du héros');
select pg_temp.check(pg_temp.worn('archer', 'weapon') ->> 'role' = 'support', 'arme de la classe du compagnon');

select set_config('test.item', pg_temp.worn('hero', 'weapon') ->> 'id', false);
select set_config('test.reply', pg_temp.upgrade(current_setting('test.item')::bigint, 3)::text, false);
select pg_temp.check(current_setting('test.reply')::jsonb ->> 'status' = 'ok', 'arme améliorée trois fois');
select pg_temp.check((pg_temp.worn('hero', 'weapon') ->> 'upgrades')::integer = 3, 'trois améliorations');
reset role;
select pg_temp.check(abs((current_setting('test.reply')::jsonb ->> 'spent')::numeric
  - 30 * (private.frontier_hp(31) * 0.35 + 2) * 0.3 * (1.25 ^ 3 - 1) / 0.25) < 0.01, 'améliorations à 30 % du prix d''achat au niveau du front');
set role authenticated;
select pg_temp.check(pg_temp.upgrade(current_setting('test.item')::bigint, 98) ->> 'status' = 'maxed', 'cent améliorations au plus');
select pg_temp.check(pg_temp.upgrade(current_setting('test.item')::bigint, 0) ->> 'status' = 'refused', 'au moins une amélioration');
select pg_temp.check(pg_temp.save(1, 1e9, '{20,5,0}', '{support,null}') = 'ok', 'or presque vide');
select pg_temp.check(pg_temp.save(5e9, 1e9, '{20,5,0}', '{support,null}') = 'refused', 'or inventé sans gain refusé');
select pg_temp.check(pg_temp.buy(pg_temp.offer(2)) ->> 'status' = 'too_poor', 'pas assez d''or');
select pg_temp.check(pg_temp.upgrade(current_setting('test.item')::bigint, 1) ->> 'status' = 'too_poor', 'pas assez d''or pour améliorer');
reset role;
update private.settings set bag_size = 2;
set role authenticated;
select pg_temp.check(pg_temp.buy(pg_temp.offer(2)) ->> 'status' = 'too_poor', 'les deux objets portés ne remplissent pas le sac');
reset role;
update private.settings set bag_size = 60;
set role authenticated;
select set_config('test.status', pg_temp.save(1e9, 2e9, '{20,5,0}', '{support,null}'), false);
select pg_temp.check(current_setting('test.status') = 'ok', 'or gagné depuis : ' || current_setting('test.status'));
reset role;
update private.settings set bag_size = 1;
select set_config('test.extra', private.roll_item('a7777777-7777-7777-7777-7777777777a7', 31, 'common', 'boss')::text, false);
set role authenticated;
select pg_temp.check(pg_temp.buy(pg_temp.offer(2)) ->> 'status' = 'bag_full', 'sac plein, objets portés non comptés');
reset role;
delete from public.items where id = current_setting('test.extra')::bigint;
update private.settings set bag_size = 60;
update public.shop_offers set opened_at = opened_at - interval '3 hours' where player_id = 'a7777777-7777-7777-7777-7777777777a7';
select set_config('test.old', (select id from public.shop_offers where player_id = 'a7777777-7777-7777-7777-7777777777a7' and position = 3)::text, false);
set role authenticated;
select pg_temp.check(not exists (select 1 from jsonb_array_elements(public.get_social() #> '{shop,offers}') offer where (offer ->> 'sold')::boolean)
  and jsonb_array_length(public.get_social() #> '{shop,offers}') = 4, 'nouvel arrivage toutes les trois heures');
select pg_temp.check(pg_temp.buy(current_setting('test.old')::bigint) ->> 'status' = 'unknown_offer', 'une ancienne offre a disparu');
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
