\set ON_ERROR_STOP on

insert into auth.users (id) values
  ('5c000000-0000-0000-0000-000000000001'), ('5c000000-0000-0000-0000-000000000002'),
  ('5c000000-0000-0000-0000-000000000003'), ('5c000000-0000-0000-0000-000000000004');

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
  p_skills text[], p_levels integer[], p_slots text[],
  p_stage integer default 12, p_max integer default null, p_best integer default 12,
  p_retreats integer default 0, p_seconds double precision[] default null, p_placed boolean[] default null,
  p_legacy double precision[] default '{0,0,0}')
returns text
language sql
as $$
  select public.save_frontier((public.claim_frontier('00000000-0000-0000-0000-0000000005c1') #>> '{frontier,revision}')::integer,
    '00000000-0000-0000-0000-0000000005c1', 100, 1e10, 0, p_retreats,
    p_stage, coalesce(p_max, p_stage), p_best, 0, false,
    '{5,1,0}', '{forge,null,null,null,null,null,null,null,null,null,null,null}', '{3,0,0,0,0,0,0,0,0,0,0,0}', p_legacy, '{}',
    1000, 0, 0, 0, 0, 0, null, null, p_skills, p_levels, p_slots,
    case when p_skills is not null then coalesce(p_placed, array(
      select coalesce(entry.node = any (p_slots), false) from unnest(p_skills) with ordinality as entry(node, position) order by entry.position)) end,
    case when p_skills is not null then coalesce(p_seconds, array_fill(0::double precision, array[cardinality(p_skills)])) end) ->> 'status';
$$;

create function pg_temp.tree() returns jsonb
language sql
as $$
  select public.claim_frontier('00000000-0000-0000-0000-0000000005c1') -> 'frontier';
$$;

grant execute on function pg_temp.check(boolean, text) to authenticated;
grant execute on function pg_temp.save(text[], integer[], text[], integer, integer, integer, integer, double precision[], boolean[], double precision[]) to authenticated;
grant execute on function pg_temp.tree() to authenticated;

select set_config('request.jwt.claim.sub', '5c000000-0000-0000-0000-000000000001', false);
set role authenticated;
select pg_temp.check(public.start_frontier('00000000-0000-0000-0000-0000000005c1') #> '{frontier,slots}' = '[null, null, null]', 'trois emplacements vides au départ');
select pg_temp.check(pg_temp.tree() -> 'skills' = '[]' and pg_temp.tree() -> 'skill_levels' = '[]', 'aucune compétence apprise au départ');
select pg_temp.check(pg_temp.tree() -> 'skill_cooldowns' = '[]' and pg_temp.tree() -> 'placed' = '[]', 'aucune recharge ni marque au départ');
select pg_temp.check(pg_temp.tree() -> 'cooldowns' = '[0, 0, 0]', 'trois recharges pour les anciens jeux');

select pg_temp.check(pg_temp.save('{smite}', '{1}', '{smite,null,null}') = 'ok', 'un point au record 12');
select pg_temp.check(pg_temp.tree() -> 'skills' = '["smite"]' and pg_temp.tree() -> 'skill_levels' = '[1]', 'compétence relue');
select pg_temp.check(pg_temp.tree() -> 'slots' = '["smite", null, null]', 'emplacement relu');
select pg_temp.check(pg_temp.tree() -> 'skill_cooldowns' = '[0]', 'une compétence apprise est prête');
select pg_temp.check(pg_temp.tree() -> 'placed' = '[true]', 'compétence marquée placée dans la partie');

select pg_temp.check(pg_temp.save('{smite,fury}', '{1,1}', '{smite,fury,null}') = 'refused', 'deux points au record 12 refusés');
select pg_temp.check(pg_temp.save('{smite,castle}', '{1,1}', '{smite,null,null}', p_best => 25) = 'refused', 'compétence inconnue refusée');
select pg_temp.check(pg_temp.save('{smite}', '{2}', '{smite,null,null}', p_best => 25) = 'refused', 'niveau au-delà du maximum refusé');
select pg_temp.check(pg_temp.save('{smite,thunder}', '{1,0}', '{smite,null,null}', p_best => 25) = 'refused', 'amélioration au niveau nul refusée');
select pg_temp.check(pg_temp.save('{smite,storm}', '{1,1}', '{smite,null,null}', p_best => 25) = 'refused', 'nœud sans le précédent de sa branche refusé');
select pg_temp.check(pg_temp.save('{smite,smite}', '{1,1}', '{smite,null,null}', p_best => 25) = 'refused', 'compétence en double refusée');
select pg_temp.check(pg_temp.save('{smite}', '{1}', '{fury,null,null}') = 'refused', 'emplacement d''une compétence non apprise refusé');
select pg_temp.check(pg_temp.save('{smite,fury}', '{1,0}', '{smite,fury,null}', p_placed => '{true,false}') = 'refused', 'compétence désapprise dans un emplacement refusée');
select pg_temp.check(pg_temp.save('{smite,fury}', '{1,0}', '{smite,null,null}', p_placed => '{true,true}') = 'refused', 'compétence désapprise marquée placée refusée');
select pg_temp.check(pg_temp.save('{smite,thunder}', '{1,1}', '{thunder,null,null}', p_best => 25) = 'refused', 'amélioration dans un emplacement refusée');
select pg_temp.check(pg_temp.save('{smite}', '{1}', '{smite,smite,null}') = 'refused', 'même compétence dans deux emplacements refusée');
select pg_temp.check(pg_temp.save('{smite}', '{1}', '{smite,null}') = 'refused', 'deux emplacements au lieu de trois refusés');
select pg_temp.check(pg_temp.save('{smite}', '{}', '{smite,null,null}') = 'refused', 'niveaux manquants refusés');
select pg_temp.check(pg_temp.save('{smite}', '{1}', null) = 'refused', 'arbre incomplet refusé');
select pg_temp.check(pg_temp.save('{smite}', '{1}', '{smite,null,null}', p_seconds => '{0,0,0}') = 'refused', 'une recharge par compétence');
select pg_temp.check(pg_temp.save('{smite}', '{1}', '{smite,null,null}', p_seconds => '{-5}') = 'refused', 'recharge négative refusée');
select pg_temp.check(pg_temp.save('{smite}', '{1}', '{smite,null,null}', p_legacy => '{0,0}') = 'refused', 'trois recharges anciennes exigées');
select pg_temp.check(pg_temp.save('{smite,thunder}', '{1,1}', '{smite,null,null}', p_best => 25, p_seconds => '{0,30}') = 'refused', 'recharge d''une amélioration refusée');
select pg_temp.check(pg_temp.save('{smite}', '{1}', '{smite,null,null}', p_placed => '{false}') = 'refused', 'compétence placée sans sa marque refusée');
select pg_temp.check(pg_temp.save('{smite,thunder}', '{1,1}', '{smite,null,null}', p_best => 25, p_placed => '{true,true}') = 'refused', 'amélioration marquée placée refusée');
select pg_temp.check(pg_temp.save('{smite}', '{1}', '{smite,null,null}', p_placed => '{true,false}') = 'refused', 'une marque par compétence');
select pg_temp.check(pg_temp.save('{}', '{}', '{null,null,null}') = 'refused', 'redistribution refusée au-delà du niveau 10');

select pg_temp.check(pg_temp.save('{smite,storm}', '{1,0}', '{smite,null,null}', p_seconds => '{0,300}') = 'ok', 'une active non apprise garde sa recharge, sans coût ni prérequis');
select pg_temp.check(pg_temp.tree() -> 'skills' = '["smite", "storm"]' and pg_temp.tree() -> 'skill_levels' = '[1, 0]' and pg_temp.tree() -> 'skill_cooldowns' = '[0, 300]', 'recharge d''une active non apprise relue');
select pg_temp.check(pg_temp.save('{smite}', '{1}', '{smite,null,null}', p_seconds => '{120.5}') = 'ok', 'recharge de la Frappe céleste');
select pg_temp.check(pg_temp.tree() -> 'skill_cooldowns' = '[120.5]' and pg_temp.tree() -> 'cooldowns' = '[120.5, 0, 0]', 'recharge relue, aussi pour les anciens jeux');
select pg_temp.check(pg_temp.save(null, null, null, p_legacy => '{45.5,0,0}') = 'ok', 'sauvegarde d''un ancien jeu sans arbre');
select pg_temp.check(pg_temp.tree() -> 'skills' = '["smite"]' and pg_temp.tree() -> 'skill_cooldowns' = '[45.5]' and pg_temp.tree() -> 'placed' = '[true]', 'arbre et marques gardés, recharge de l''ancien jeu rangée sur la Frappe céleste');

select pg_temp.check(pg_temp.save('{smite,thunder}', '{1,1}', '{null,null,smite}', p_best => 25, p_seconds => '{600,0}') = 'ok', 'Orage au record 25, Frappe céleste déplacée');
select pg_temp.check(pg_temp.tree() -> 'slots' = '[null, null, "smite"]' and pg_temp.tree() -> 'skill_cooldowns' = '[600, 0]', 'emplacements et recharges relus');
select pg_temp.check(pg_temp.save('{smite,thunder}', '{1,1}', '{null,null,null}', p_best => 25, p_seconds => '{600,0}', p_placed => '{true,false}') = 'ok', 'Frappe céleste retirée de son emplacement');
select pg_temp.check(pg_temp.tree() -> 'slots' = '[null, null, null]' and pg_temp.tree() -> 'skill_cooldowns' = '[600, 0]', 'une compétence retirée garde sa recharge');
select pg_temp.check(pg_temp.tree() -> 'placed' = '[true, false]', 'une compétence retirée reste marquée placée dans la partie');
select pg_temp.check(pg_temp.save('{smite,thunder,storm}', '{1,1,1}', '{storm,null,smite}', p_best => 35, p_stage => 35, p_seconds => '{600,0,0}') = 'ok', 'Tempête au record 35');
select pg_temp.check(pg_temp.tree() -> 'skills' = '["smite", "storm", "thunder"]' and pg_temp.tree() -> 'skill_cooldowns' = '[600, 0, 0]', 'recharges rangées comme les compétences');
select pg_temp.check(pg_temp.save('{smite,storm}', '{1,1}', '{storm,null,smite}', p_best => 35, p_stage => 35) = 'refused', 'retirer Orage casse la branche et baisse un niveau');
select pg_temp.check(pg_temp.save('{smite,thunder,storm,lightning_rod,judgment}', '{1,1,1,1,1}', '{storm,judgment,smite}', p_best => 50, p_stage => 50) = 'refused', 'Jugement coûte deux points : six points au record 50 refusés');
select pg_temp.check(pg_temp.save('{smite,thunder,storm,lightning_rod,judgment}', '{1,1,1,1,1}', '{storm,judgment,smite}', p_best => 61, p_stage => 61) = 'ok', 'Jugement au record 61');

select pg_temp.check(pg_temp.save('{fury,lasting_rage}', '{1,2}', '{null,fury,null}', p_stage => 1, p_max => 1, p_best => 61, p_retreats => 1) = 'ok', 'redistribution après un repli');
select pg_temp.check(pg_temp.tree() -> 'skills' = '["fury", "lasting_rage"]' and pg_temp.tree() -> 'skill_levels' = '[1, 2]', 'nouvel arbre relu');
select pg_temp.check(pg_temp.tree() -> 'slots' = '[null, "fury", null]' and pg_temp.tree() -> 'placed' = '[true, false]', 'nouveaux emplacements et marques relus');
select pg_temp.check(pg_temp.save('{fury,lasting_rage,gold_rain}', '{1,2,1}', '{null,fury,gold_rain}', p_stage => 3, p_max => 3, p_best => 61, p_retreats => 1, p_seconds => '{0,0,0}') = 'ok', 'Pluie d''or apprise en début de partie');
select pg_temp.check(pg_temp.save('{fury,lasting_rage,gold_rain}', '{1,2,0}', '{null,fury,null}', p_stage => 4, p_max => 4, p_best => 61, p_retreats => 1, p_seconds => '{0,0,1800}', p_placed => '{true,false,false}') = 'ok', 'Pluie d''or lancée puis désapprise : sa recharge est envoyée');
select pg_temp.check(pg_temp.tree() -> 'skills' = '["fury", "gold_rain", "lasting_rage"]' and pg_temp.tree() -> 'skill_levels' = '[1, 0, 2]' and pg_temp.tree() -> 'skill_cooldowns' = '[0, 1800, 0]', 'la recharge d''une compétence désapprise survit à la sauvegarde');
select pg_temp.check(pg_temp.save('{fury,lasting_rage,gold_rain}', '{1,2,1}', '{gold_rain,fury,null}', p_stage => 5, p_max => 5, p_best => 61, p_retreats => 1, p_seconds => '{0,0,1790}') = 'ok', 'Pluie d''or réapprise et replacée');
select pg_temp.check(pg_temp.tree() -> 'skill_cooldowns' = '[0, 1790, 0]', 'Pluie d''or réapprise toujours en recharge');
select pg_temp.check(pg_temp.save('{fury}', '{1}', '{null,fury,null}', p_stage => 40, p_max => 40, p_best => 61, p_retreats => 1) = 'ok', 'repli sauvegardé, puis redistribution sauvegardée plus tard au niveau 40');
select pg_temp.check(pg_temp.tree() -> 'skills' = '["fury"]' and pg_temp.tree() -> 'skill_levels' = '[1]', 'redistribution tardive relue');
select pg_temp.check(pg_temp.save('{}', '{}', '{null,null,null}', p_stage => 41, p_max => 41, p_best => 61, p_retreats => 1) = 'refused', 'plus de redistribution une fois la partie sauvegardée au-delà du niveau 10');
select pg_temp.check(pg_temp.save('{}', '{}', '{null,null,null}', p_stage => 12, p_max => 12, p_best => 61, p_retreats => 2) = 'ok', 'une sauvegarde qui contient un repli peut redistribuer');
select pg_temp.check(pg_temp.tree() -> 'skills' = '[]' and pg_temp.tree() -> 'slots' = '[null, null, null]', 'arbre vidé après le repli');
reset role;

insert into public.frontiers (player_id, stage, max_stage, best_stage) values
  ('5c000000-0000-0000-0000-000000000002', 27, 27, 27),
  ('5c000000-0000-0000-0000-000000000003', 20, 30, 35),
  ('5c000000-0000-0000-0000-000000000004', 5, 5, 5);
select private.grant_skills('5c000000-0000-0000-0000-000000000002', '{12.5,120.5,0}');
select private.grant_skills('5c000000-0000-0000-0000-000000000003', '{0,0,900}');
select private.grant_skills('5c000000-0000-0000-0000-000000000004', '{0,0,0}');
select pg_temp.check(private.frontier_reply('5c000000-0000-0000-0000-000000000002', 'ok') #> '{frontier,skills}' = '["fury", "smite"]', 'record 27 : Frappe céleste et Furie');
select pg_temp.check(private.frontier_reply('5c000000-0000-0000-0000-000000000002', 'ok') #> '{frontier,slots}' = '["smite", "fury", null]', 'record 27 : emplacements 1 et 2');
select pg_temp.check(private.frontier_reply('5c000000-0000-0000-0000-000000000002', 'ok') #> '{frontier,skill_cooldowns}' = '[120.5, 12.5]', 'chaque compétence garde son ancienne recharge');
select pg_temp.check(private.frontier_reply('5c000000-0000-0000-0000-000000000002', 'ok') #> '{frontier,cooldowns}' = '[12.5, 120.5, 0]', 'les anciens jeux relisent les recharges dans leur ordre');
select pg_temp.check(private.frontier_reply('5c000000-0000-0000-0000-000000000002', 'ok') #> '{frontier,placed}' = '[true, true]', 'les compétences données sont déjà placées');
select pg_temp.check(private.frontier_reply('5c000000-0000-0000-0000-000000000003', 'ok') #> '{frontier,slots}' = '["smite", "fury", "gold_rain"]', 'record 35 : les trois compétences');
select pg_temp.check(private.frontier_reply('5c000000-0000-0000-0000-000000000003', 'ok') #> '{frontier,skill_cooldowns}' = '[0, 900, 0]', 'recharge de la Pluie d''or gardée');
select pg_temp.check(private.frontier_reply('5c000000-0000-0000-0000-000000000003', 'ok') #> '{frontier,cooldowns}' = '[0, 0, 900]', 'recharge de la Pluie d''or à sa place pour les anciens jeux');
select pg_temp.check(private.frontier_reply('5c000000-0000-0000-0000-000000000004', 'ok') #> '{frontier,slots}' = '[null, null, null]', 'record 5 : rien à donner');
select pg_temp.check(private.frontier_reply('5c000000-0000-0000-0000-000000000004', 'ok') #> '{frontier,skills}' = '[]', 'record 5 : aucune compétence');
select pg_temp.check(private.frontier_reply('5c000000-0000-0000-0000-000000000004', 'ok') #> '{frontier,skill_cooldowns}' = '[]', 'record 5 : aucune recharge');
select private.grant_skills('5c000000-0000-0000-0000-000000000002', '{1,1,1}');
select pg_temp.check(private.frontier_reply('5c000000-0000-0000-0000-000000000002', 'ok') #> '{frontier,skill_cooldowns}' = '[120.5, 12.5]', 'le don ne se refait pas');
select pg_temp.check((select count(*) from public.frontier_skills where player_id = '5c000000-0000-0000-0000-000000000002') = 2, 'aucune compétence en double');

delete from auth.users where id::text like '5c000000-%';
