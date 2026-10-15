\set ON_ERROR_STOP on

insert into auth.users (id) values ('c9999999-9999-9999-9999-9999999999c9');

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

select set_config('request.jwt.claim.sub', 'c9999999-9999-9999-9999-9999999999c9', false);
set role authenticated;
select public.start_frontier('00000000-0000-0000-0000-0000000000c9');
select public.set_hero('00000000-0000-0000-0000-0000000000c9', 'Ysolde', 'mage', 0, 0, 0, 0);
reset role;

select pg_temp.check((select count(*) = 2 and bool_and(total = 100) from (
  select source, sum(percent) as total from public.rarity_odds group by source) sums), 'chaque source totalise 100 %');
select pg_temp.check((select string_agg(rarity || '=' || percent, ',' order by source, rarity) from public.rarity_odds)
  = 'common=62,epic=8,legendary=2,rare=28,common=55,epic=10,legendary=3,rare=32', 'chances du butin et de la boutique');

create temporary table rolls as
select sources.source, private.roll_rarity(sources.source) as rarity
from (values ('boss'), ('shop')) sources (source), generate_series(1, 5000);
select pg_temp.check(not exists (
  select 1 from public.rarity_odds odds
  where abs(100.0 * (select count(*) from rolls where rolls.source = odds.source and rolls.rarity = odds.rarity) / 5000 - odds.percent)
    > 4 * 100 * sqrt(odds.percent / 100.0 * (1 - odds.percent / 100.0) / 5000)), 'les tirages suivent les chances de chaque source');

select pg_temp.check(not exists (
  select 1 from public.item_stats stat, generate_series(1, 300, 7) level
  where private.stat_value(stat.id, 'common', level, 1) >= private.stat_value(stat.id, 'epic', level, 0)
    or private.stat_value(stat.id, 'rare', level, 1) >= private.stat_value(stat.id, 'legendary', level, 0)
    or private.stat_value(stat.id, 'epic', level, 0.5) <= private.stat_value(stat.id, 'rare', level, 0.5)
    or private.stat_value(stat.id, 'common', level + 1, 0.5) <= private.stat_value(stat.id, 'common', level, 0.5)),
  'à niveau égal, une commune n''égale jamais une épique ni une rare une légendaire, et un niveau de plus vaut toujours mieux');
select pg_temp.check(not exists (
  select 1 from public.item_stats stat
  where private.stat_value(stat.id, 'rare', 60, 0) <= private.stat_value(stat.id, 'legendary', 30, 1)),
  'une rare du niveau 60 bat toujours une légendaire du niveau 30');
select pg_temp.check(not exists (
  select 1 from public.item_stats stat, generate_series(1, 300, 11) level
  where private.stat_value(stat.id, 'legendary', level, 0) <= private.stat_value(stat.id, 'rare', level, 1)),
  'une légendaire bat toujours une rare du même niveau');
select pg_temp.check(private.stat_value('damage', 'common', 100, 0.5) = round(4 * 0.22 * power(1.019, 99), 2), 'formule des valeurs');

update public.frontiers set best_stage = 60 where player_id = 'c9999999-9999-9999-9999-9999999999c9';
update public.frontier_fighters set level = 1000 where player_id = 'c9999999-9999-9999-9999-9999999999c9' and fighter = 'hero';
select pg_temp.check(private.top_level('c9999999-9999-9999-9999-9999999999c9') = 70, 'le niveau des combattants est plafonné par le record du front');
update public.frontier_fighters set level = case fighter when 'hero' then 40 when 'archer' then 30 else 0 end
where player_id = 'c9999999-9999-9999-9999-9999999999c9';
update private.settings set bag_size = 1000;
select private.roll_item('c9999999-9999-9999-9999-9999999999c9', null, null, 'boss') from generate_series(1, 700);
select pg_temp.check((select min(item_level) >= 35 and max(item_level) <= 43 and count(distinct item_level) >= 6
  and bool_or(item_level > 40) from public.items where owner_id = 'c9999999-9999-9999-9999-9999999999c9'),
  'le butin demande le niveau du meilleur combattant, à 5 de moins ou 3 de plus');
select pg_temp.check((select count(distinct role) = 5 and 100.0 * count(*) filter (where role = 'mage') / count(*) < 35
  from public.items where owner_id = 'c9999999-9999-9999-9999-9999999999c9' and slot = 'weapon'),
  'des armes de toutes les classes, sans favoriser celle du héros');
select pg_temp.check((select count(distinct stat) = 9 from (
  select stat_a as stat from public.items where owner_id = 'c9999999-9999-9999-9999-9999999999c9'
  union all select stat_b from public.items where owner_id = 'c9999999-9999-9999-9999-9999999999c9' and stat_b is not null) stats),
  'les neuf caractéristiques tombent');
select pg_temp.check((select count(distinct look) = 8 from public.items where owner_id = 'c9999999-9999-9999-9999-9999999999c9'), 'huit apparences');
select pg_temp.check(not exists (
  select 1 from public.items
  where owner_id = 'c9999999-9999-9999-9999-9999999999c9'
    and (value_a not between private.stat_value(stat_a, rarity, item_level, 0) and private.stat_value(stat_a, rarity, item_level, 1)
      or value_b not between private.stat_value(stat_b, rarity, item_level, 0, true) and private.stat_value(stat_b, rarity, item_level, 1, true)
      or stage <> 1)), 'chaque valeur tombe dans la fourchette de sa rareté');
delete from public.items where owner_id = 'c9999999-9999-9999-9999-9999999999c9';
update private.settings set bag_size = 60;

with staff as (
  insert into public.items (owner_id, slot, rarity, item_level, stat_a, value_a, source, role, stage)
  values ('c9999999-9999-9999-9999-9999999999c9', 'weapon', 'common', 41, 'damage', 4, 'boss', 'mage', 50),
    ('c9999999-9999-9999-9999-9999999999c9', 'weapon', 'common', 40, 'damage', 4, 'boss', 'mage', 50) returning id, item_level)
select set_config('test.high', (select id from staff where item_level = 41)::text, false),
  set_config('test.fit', (select id from staff where item_level = 40)::text, false);
set role authenticated;
select pg_temp.check(public.equip_item(current_setting('test.high')::bigint, 'hero') ->> 'status' = 'level_too_low', 'un objet du niveau 41 attend le niveau 41');
select pg_temp.check(public.equip_item(current_setting('test.fit')::bigint, 'hero') ->> 'status' = 'ok', 'un objet du niveau 40 va au héros du niveau 40');
select pg_temp.check(public.equip_item(current_setting('test.fit')::bigint, 'archer') ->> 'status' = 'wrong_class', 'la classe passe avant le niveau');
select pg_temp.check((select (item ->> 'stage')::integer = 50 from jsonb_array_elements(public.get_social() #> '{items,bag}') item
  where item ->> 'id' = current_setting('test.fit')), 'le niveau du front de l''objet est rendu');
select pg_temp.check((select string_agg(odds ->> 'rarity' || ' ' || (odds ->> 'percent'), ', ')
  from jsonb_array_elements(public.get_social() #> '{shop,odds}') odds) = 'common 55, rare 32, epic 10, legendary 3', 'chances de la boutique envoyées au jeu');
select pg_temp.check(not exists (
  select 1 from jsonb_array_elements(public.get_social() #> '{shop,offers}') offer
  where (offer ->> 'level')::integer not between 35 and 43 or (offer ->> 'look')::integer not between 0 and 7), 'offres au niveau des combattants');
reset role;

update private.settings set item_rules = 1;
with old as (
  insert into public.items (owner_id, slot, rarity, item_level, stat_a, value_a, stat_b, value_b, look, source, upgrades)
  values ('c9999999-9999-9999-9999-9999999999c9', 'helm', 'epic', 150, 'damage', 50, 'speed', 22.5, 3, 'boss', 4) returning id)
select set_config('test.old', (select id from old)::text, false);
select private.rescale_items();
select private.rescale_items();
select pg_temp.check((select item_level = 108 and stage = 150 and upgrades = 4 and rarity = 'epic' and look = 3
  and value_a = private.stat_value('damage', 'epic', 108, 0.5) and value_b = private.stat_value('speed', 'epic', 108, 0.5, true)
  from public.items where id = current_setting('test.old')::bigint), 'un ancien objet passe au niveau requis et aux nouvelles valeurs');
select pg_temp.check((select item_level from public.items where id = current_setting('test.fit')::bigint) = 40, 'un objet récent ne change pas');
select pg_temp.check((select item_rules from private.settings) = 2, 'conversion faite une fois');
update private.settings set item_rules = 3;

delete from auth.users where id = 'c9999999-9999-9999-9999-9999999999c9';
