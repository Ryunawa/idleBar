\set ON_ERROR_STOP on

insert into auth.users (id) values ('5aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa44');

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

select set_config('request.jwt.claim.sub', '5aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa44', false);
set role authenticated;
select public.start_frontier('00000000-0000-0000-0000-000000000044');
select public.set_hero('00000000-0000-0000-0000-000000000044', 'Ysengrin', 'warrior', 0, 0, 0, 0);
reset role;

select pg_temp.check((select string_agg(id, ',' order by position) from public.item_slots) = 'weapon,armor,helm,arms,legs,feet,ring', 'sept emplacements');
select pg_temp.check((select string_agg(id, ',' order by id) from public.item_stats where 'legs' = any (slots)) = 'healing,health,xp', 'jambes : PV, soins ou xp');
select pg_temp.check((select string_agg(id, ',' order by id) from public.item_stats where 'feet' = any (slots)) = 'health,recharge,speed', 'pieds : vitesse, PV ou recharge');
select pg_temp.check((select string_agg(id, ',' order by id) from public.item_stats where 'arms' = any (slots)) = 'critical,damage,speed', 'bras : dégâts, critiques ou vitesse');
select pg_temp.check((select count(*) from public.item_stats where 'ring' = any (slots)) = 9, 'l''anneau peut porter les neuf caractéristiques');
select pg_temp.check((select item_scale = 0.22 and item_rules = 3 from private.settings), 'nouvelle échelle des valeurs');

update public.frontiers set best_stage = 60 where player_id = '5aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa44';
update public.frontier_fighters set level = 40 where player_id = '5aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa44' and fighter = 'hero';
update private.settings set bag_size = 1000;
select private.roll_item('5aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa44', null, null, 'boss') from generate_series(1, 700);
select pg_temp.check((select count(distinct slot) = 7 from public.items where owner_id = '5aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa44'), 'le butin tire les sept emplacements');
select pg_temp.check(not exists (
  select 1 from public.items item
  where item.owner_id = '5aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa44' and item.slot <> 'weapon'
    and exists (select 1 from public.item_stats stat where stat.id in (item.stat_a, item.stat_b) and not item.slot = any (stat.slots))),
  'chaque emplacement tire ses propres caractéristiques');
select pg_temp.check((select count(distinct stat) = 9 from (
  select stat_a as stat from public.items where owner_id = '5aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa44' and slot = 'ring'
  union all select stat_b from public.items where owner_id = '5aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa44' and slot = 'ring' and stat_b is not null) stats),
  'un anneau tombe avec chacune des neuf caractéristiques, or et dégâts aux boss compris');
select pg_temp.check(not exists (
  select 1 from public.items
  where owner_id = '5aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa44' and slot in ('arms', 'legs', 'feet', 'ring')
    and (role is not null or item_level not between 35 and 43 or look not between 0 and 7
      or value_a not between private.stat_value(stat_a, rarity, item_level, 0) and private.stat_value(stat_a, rarity, item_level, 1)
      or value_b not between private.stat_value(stat_b, rarity, item_level, 0, true) and private.stat_value(stat_b, rarity, item_level, 1, true))),
  'les nouvelles pièces suivent les règles du 43 : niveau requis, fourchette, apparence, sans classe');
delete from public.items where owner_id = '5aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa44';

create temporary table offered (slot text);
do $$
begin
  for v_round in 1 .. 60 loop
    delete from public.shop_offers where player_id = '5aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa44';
    perform private.stock_shop('5aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa44');
    insert into offered select slot from public.shop_offers where player_id = '5aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa44';
  end loop;
end;
$$;
select pg_temp.check((select count(distinct slot) = 7 from offered), 'la boutique propose les sept emplacements');
update private.settings set bag_size = 60;

with worn as (
  insert into public.items (owner_id, slot, rarity, item_level, stat_a, value_a, source, stage)
  values ('5aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa44', 'legs', 'common', 10, 'health', 2, 'boss', 10),
    ('5aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa44', 'ring', 'common', 10, 'gold', 2, 'boss', 10),
    ('5aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa44', 'ring', 'rare', 12, 'boss', 3, 'boss', 10) returning id, slot, rarity)
select set_config('test.legs', (select id from worn where slot = 'legs')::text, false),
  set_config('test.ring', (select id from worn where rarity = 'common' and slot = 'ring')::text, false),
  set_config('test.other', (select id from worn where rarity = 'rare')::text, false);
set role authenticated;
select pg_temp.check(public.equip_item(current_setting('test.legs')::bigint, 'hero') ->> 'status' = 'ok', 'des jambières se portent');
select pg_temp.check(public.equip_item(current_setting('test.ring')::bigint, 'hero') ->> 'status' = 'ok', 'un anneau se porte');
select pg_temp.check(public.equip_item(current_setting('test.other')::bigint, 'hero') ->> 'status' = 'ok', 'un second anneau remplace le premier');
select pg_temp.check((select string_agg(item ->> 'slot', ',' order by item ->> 'slot')
  from jsonb_array_elements(public.get_social() #> '{items,bag}') item where (item ->> 'equipped')::boolean) = 'legs,ring', 'un seul anneau porté, avec les jambières');
select pg_temp.check((select (item ->> 'equipped')::boolean = false from jsonb_array_elements(public.get_social() #> '{items,bag}') item
  where item ->> 'id' = current_setting('test.ring')), 'le premier anneau revient au sac');
select pg_temp.check(public.unequip_item('ring', 'hero') ->> 'status' = 'ok', 'l''anneau se retire');
select pg_temp.check(public.unequip_item('ring', 'hero') ->> 'status' = 'unknown_item', 'plus d''anneau porté');
reset role;

update private.settings set item_rules = 2, item_scale = 0.75;
with old as (
  insert into public.items (owner_id, slot, rarity, item_level, stat_a, value_a, stat_b, value_b, source, stage)
  values ('5aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa44', 'helm', 'rare', 40, 'damage', 7.5, 'xp', 0.03, 'boss', 50) returning id)
select set_config('test.old', (select id from old)::text, false);
select private.rescale_values(0.22);
select private.rescale_values(0.22);
select pg_temp.check((select value_a = 2.2 and value_b = 0.01 from public.items where id = current_setting('test.old')::bigint),
  'les valeurs d''avant passent à la nouvelle échelle, une seule fois, sans tomber à zéro');
select pg_temp.check((select item_scale = 0.22 and item_rules = 3 from private.settings), 'échelle enregistrée');

delete from auth.users where id = '5aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa44';
