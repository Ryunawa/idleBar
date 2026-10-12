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
select pg_temp.expect_denied('select public.save_frontier(0, gen_random_uuid(), 0, 0, 0, 0, 1, 1, 1, 0, false, ''{1,0,0}'', ''{}'', ''{}'', ''{0,0,0}'', false, false, ''{}'', 0, 0, 0, 0, 0)');
reset role;

select set_config('request.jwt.claim.sub', '33333333-3333-3333-3333-333333333333', false);
set role authenticated;
select pg_temp.expect_denied('select * from private.settings');
select pg_temp.expect_denied('select * from public.frontiers');
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
select pg_temp.expect_denied('select * from public.achievement_kinds');
select pg_temp.expect_denied('select private.frontier_reply(auth.uid(), ''ok'')');
select public.get_requirements();
reset role;

do $$
begin
  if to_regclass('public.taverns') is not null or to_regclass('private.veterans') is not null
    or to_regprocedure('public.get_world()') is not null then
    raise exception 'La purge a laissé des restes de la taverne';
  end if;
end;
$$;
