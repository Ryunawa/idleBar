\set ON_ERROR_STOP on

insert into auth.users (id) values ('e5555555-5555-5555-5555-555555555555'), ('f6666666-6666-6666-6666-666666666666');

create function pg_temp.check(p_condition boolean, p_message text) returns void
language plpgsql
as $$
begin
  if p_condition is not true then
    raise exception 'Échec : %', p_message;
  end if;
end;
$$;

create function pg_temp.bag() returns jsonb
language sql
as $$
  select public.get_social() #> '{items,bag}';
$$;

create function pg_temp.save(p_revision integer, p_killed bigint, p_bosses bigint) returns text
language sql
as $$
  select public.save_frontier(p_revision, '00000000-0000-0000-0000-00000000000e', 100, 1e10, 0, 0, 12, 12, 12, 0, false,
    '{5,1,0}', '{forge,bakery,null,null,null,null,null,null,null,null,null,null}', '{3,1,0,0,0,0,0,0,0,0,0,0}',
    '{0,0,0}', false, false, '{}', p_killed, p_bosses, 0, 0, 0, 0) ->> 'status';
$$;

grant execute on function pg_temp.check(boolean, text), pg_temp.bag(), pg_temp.save(integer, bigint, bigint) to authenticated;

update private.settings set loot_chance = 1;

select set_config('request.jwt.claim.sub', 'f6666666-6666-6666-6666-666666666666', false);
set role authenticated;
select public.start_frontier('00000000-0000-0000-0000-00000000000f');
select set_config('request.jwt.claim.sub', 'e5555555-5555-5555-5555-555555555555', false);
select public.start_frontier('00000000-0000-0000-0000-00000000000e');
select public.set_hero('00000000-0000-0000-0000-00000000000e', 'Elouan', 'archer', 0, 0, 0, 0);
select pg_temp.check(jsonb_array_length(pg_temp.bag()) = 0, 'sac vide au départ');
select pg_temp.check((public.get_social() #>> '{items,max}')::integer = 60, 'sac de 60 objets');
select pg_temp.check(pg_temp.save(0, 30, 3) = 'ok', 'trois boss vaincus');
select pg_temp.check(jsonb_array_length(pg_temp.bag()) = 2, 'au plus deux boss par minute pour le butin');
select pg_temp.check(not exists (
  select 1 from jsonb_array_elements(pg_temp.bag()) item
  where item ->> 'source' <> 'boss' or (item ->> 'level')::integer <> 12 or (item ->> 'value_a')::numeric <= 0
    or (item ->> 'equipped')::boolean), 'butin des boss au niveau atteint');

select set_config('test.item', pg_temp.bag() #>> '{0,id}', false);
select pg_temp.check((public.equip_item(current_setting('test.item')::bigint) #>> '{items,bag,0,equipped}')::boolean, 'objet équipé');
select pg_temp.check(public.unequip_item('weapon') ->> 'status' in ('ok', 'unknown_item'), 'retrait d''un emplacement');
select pg_temp.check(public.unequip_item('crown') ->> 'status' = 'unknown_item', 'emplacement inconnu');
select pg_temp.check(public.equip_item(current_setting('test.item')::bigint) ->> 'status' = 'ok', 'objet rééquipé');
reset role;
select set_config('test.foreign', private.roll_item('f6666666-6666-6666-6666-666666666666', 5, null, 'boss')::text, false);
set role authenticated;
select pg_temp.check(public.equip_item(current_setting('test.foreign')::bigint) ->> 'status' = 'unknown_item', 'l''objet d''un autre reste à lui');
select pg_temp.check(public.salvage_item(current_setting('test.foreign')::bigint) ->> 'status' = 'unknown_item', 'pas de recyclage chez les autres');
select pg_temp.check(jsonb_array_length(public.salvage_item(current_setting('test.item')::bigint) #> '{items,bag}') = 1, 'objet recyclé, même équipé');
reset role;

select pg_temp.check(not exists (select 1 from public.hero_equipment where item_id = current_setting('test.item')::bigint), 'un objet recyclé quitte l''équipement');
update private.settings set bag_size = 200;
select private.roll_item('e5555555-5555-5555-5555-555555555555', 100, null, 'boss') from generate_series(1, 120);
select pg_temp.check(not exists (
  select 1 from public.items item join public.item_stats stat on stat.id = item.stat_a
  where not (item.slot = any (stat.slots))), 'chaque caractéristique sur un emplacement permis');
select pg_temp.check(not exists (
  select 1 from public.items item join public.item_rarities rarity on rarity.id = item.rarity
  where (rarity.stats = 2) <> (item.stat_b is not null)), 'deux caractéristiques dès la rareté rare');
select pg_temp.check((select count(distinct rarity) from public.items) >= 3, 'plusieurs raretés tirées');
select set_config('test.legend', private.roll_item('e5555555-5555-5555-5555-555555555555', 100, 'legendary', 'titan')::text, false);
select pg_temp.check((select value_a from public.items where id = current_setting('test.legend')::bigint) >= 8, 'un légendaire du niveau 100 est fort');
update private.settings set bag_size = 1;
select pg_temp.check(private.roll_item('e5555555-5555-5555-5555-555555555555', 10, null, 'boss') is null, 'sac plein, butin perdu');
update private.settings set bag_size = 60, loot_chance = 0.3;

delete from auth.users where id in ('e5555555-5555-5555-5555-555555555555', 'f6666666-6666-6666-6666-666666666666');
select pg_temp.check(not exists (select 1 from public.items), 'les objets partent avec le compte');
