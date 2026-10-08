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
select pg_temp.expect_denied('select public.get_world()');
select pg_temp.expect_denied('select public.get_state()');
select pg_temp.expect_denied('select public.found_tavern(''Chez moi'')');
select pg_temp.expect_denied('select public.report_service(''{}'')');
select pg_temp.expect_denied('select public.buy_upgrade(''tabouret-5'')');
select pg_temp.expect_denied('select public.collect_tip_jar()');
select pg_temp.expect_denied('select public.request_friend(''AAAAAA'')');
select pg_temp.expect_denied('select public.start_visit(gen_random_uuid(), ''heart'')');
select pg_temp.expect_denied('select public.send_emote(1, ''cheers'')');
select pg_temp.expect_denied('select public.offer_round()');
select pg_temp.expect_denied('select public.greet_passerby(gen_random_uuid())');
reset role;

select set_config('request.jwt.claim.sub', '33333333-3333-3333-3333-333333333333', false);
set role authenticated;
select pg_temp.expect_denied('select * from private.settings');
select pg_temp.expect_denied('select private.state(auth.uid())');
select pg_temp.expect_denied('select * from public.renown_tiers');
select pg_temp.expect_denied('select * from public.drinks');
select pg_temp.expect_denied('select * from public.upgrades');
select pg_temp.expect_denied('select * from public.goal_kinds');
select pg_temp.expect_denied('select * from public.taverns');
select pg_temp.expect_denied('select * from public.tavern_upgrades');
select pg_temp.expect_denied('select * from public.tavern_goals');
select pg_temp.expect_denied('select * from public.regulars');
select pg_temp.expect_denied('select * from public.regular_chapters');
select pg_temp.expect_denied('select * from public.tavern_regulars');
select pg_temp.expect_denied('select * from public.specialty_words');
select pg_temp.expect_denied('select * from public.stamps');
select pg_temp.expect_denied('select * from public.friend_requests');
select pg_temp.expect_denied('select * from public.friendships');
select pg_temp.expect_denied('select * from public.visits');
select pg_temp.expect_denied('select * from public.tasted');
select pg_temp.expect_denied('select * from public.invitations');
select pg_temp.expect_denied('select * from public.rounds');
select pg_temp.expect_denied('select * from public.greetings');
select pg_temp.expect_denied('select private.ring(auth.uid(), ''refresh'', ''{}'')');
select pg_temp.expect_denied('update public.taverns set coins = 1000000');
select public.get_world();
reset role;
