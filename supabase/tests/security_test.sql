\set ON_ERROR_STOP on

insert into auth.users (id) values ('33333333-3333-3333-3333-333333333333');

create function pg_temp.expect_denied(p_statement text) returns void
language plpgsql
as $$
begin
  execute p_statement;
  raise exception 'Expected "%" to be denied to %', p_statement, current_user using errcode = 'XX000';
exception
  when insufficient_privilege then
    null;
end;
$$;

grant execute on function pg_temp.expect_denied(text) to anon, authenticated;

set role anon;
select pg_temp.expect_denied('select public.get_requirements()');
select pg_temp.expect_denied('select public.start_frontier(gen_random_uuid())');
select pg_temp.expect_denied('select public.claim_frontier(gen_random_uuid())');
select pg_temp.expect_denied('select public.rejoin_frontier(gen_random_uuid())');
select pg_temp.expect_denied('select public.save_frontier(0, gen_random_uuid(), 0, 0, 0, 0, 1, 1, 1, 0, false, ''{1,0,0}'', ''{}'', ''{}'', ''{0,0,0}'', ''{}'', 0, 0, 0, 0, 0, 0)');
select pg_temp.expect_denied('select public.set_hero(gen_random_uuid(), ''Aldric'', ''tank'', 0, 0, 0, 0)');
select pg_temp.expect_denied('select public.get_social()');
select pg_temp.expect_denied('select public.create_guild(''Les Lames'')');
select pg_temp.expect_denied('select public.join_guild(''ABCDEF'')');
select pg_temp.expect_denied('select public.leave_guild()');
select pg_temp.expect_denied('select public.kick_member(1)');
select pg_temp.expect_denied('select public.rename_guild(''Les Lames'')');
select pg_temp.expect_denied('select public.new_guild_code()');
select pg_temp.expect_denied('select public.send_help(1, ''hero'')');
select pg_temp.expect_denied('select public.claim_titan()');
select pg_temp.expect_denied('select public.equip_item(1, ''hero'')');
select pg_temp.expect_denied('select public.unequip_item(''weapon'', ''hero'')');
select pg_temp.expect_denied('select public.buy_offer(gen_random_uuid(), 0, 1)');
select pg_temp.expect_denied('select public.upgrade_item(gen_random_uuid(), 0, 1, 1)');
select pg_temp.expect_denied('select public.salvage_item(1)');
reset role;

select set_config('request.jwt.claim.sub', '33333333-3333-3333-3333-333333333333', false);
set role authenticated;
select pg_temp.expect_denied('select * from private.settings');
select pg_temp.expect_denied('select * from public.frontiers');
select pg_temp.expect_denied('select * from public.shop_offers');
select pg_temp.expect_denied('update public.shop_offers set price = 1');
select pg_temp.expect_denied('update public.frontiers set souls = 1000000');
select pg_temp.expect_denied('select * from public.frontier_fighters');
select pg_temp.expect_denied('select * from public.frontier_plots');
select pg_temp.expect_denied('select * from public.frontier_cooldowns');
select pg_temp.expect_denied('select * from public.frontier_achievements');
select pg_temp.expect_denied('insert into public.frontier_achievements (player_id, achievement) values (auth.uid(), ''triche'')');
select pg_temp.expect_denied('select * from public.fighter_kinds');
select pg_temp.expect_denied('select * from public.skill_kinds');
select pg_temp.expect_denied('select * from public.building_kinds');
select pg_temp.expect_denied('select * from public.plots');
select pg_temp.expect_denied('select * from public.role_kinds');
select pg_temp.expect_denied('select * from private.banned_words');
select pg_temp.expect_denied('select * from public.guilds');
select pg_temp.expect_denied('select * from public.guild_members');
select pg_temp.expect_denied('insert into public.guild_members (player_id, guild_id) values (auth.uid(), gen_random_uuid())');
select pg_temp.expect_denied('select * from public.guild_departures');
select pg_temp.expect_denied('select * from public.guild_help');
select pg_temp.expect_denied('insert into public.guild_help (sender_id, receiver_id, kind, ends_at) values (gen_random_uuid(), auth.uid(), ''convoy'', now())');
select pg_temp.expect_denied('select * from public.help_kinds');
select pg_temp.expect_denied('select * from public.titans');
select pg_temp.expect_denied('update public.titans set damage = hp');
select pg_temp.expect_denied('select * from public.titan_contributions');
select pg_temp.expect_denied('select * from public.guild_titan');
select pg_temp.expect_denied('select * from public.titan_tiers');
select pg_temp.expect_denied('select * from public.items');
select pg_temp.expect_denied('insert into public.items (owner_id, slot, rarity, item_level, stat_a, value_a, source) values (auth.uid(), ''weapon'', ''legendary'', 999, ''damage'', 999, ''boss'')');
select pg_temp.expect_denied('update public.items set value_a = 999');
select pg_temp.expect_denied('select * from public.hero_equipment');
select pg_temp.expect_denied('select * from public.item_stats');
select pg_temp.expect_denied('select * from public.item_rarities');
select pg_temp.expect_denied('select * from public.item_slots');
select pg_temp.expect_denied('update public.titan_tiers set percent = 1000');
select pg_temp.expect_denied('update public.frontiers set hero_role = ''tank''');
select pg_temp.expect_denied('select * from public.achievement_kinds');
select pg_temp.expect_denied('select private.frontier_reply(auth.uid(), ''ok'')');
select public.get_requirements();
reset role;
