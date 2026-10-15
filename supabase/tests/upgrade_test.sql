\set ON_ERROR_STOP on

insert into auth.users (id) values ('7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaad1');

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

select set_config('request.jwt.claim.sub', '7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaad1', false);
set role authenticated;
select pg_temp.check(public.start_frontier('00000000-0000-0000-0000-0000000000d1') #>> '{frontier,revision}' = '0', 'partie créée');
reset role;

update public.frontiers set best_stage = 150 where player_id = '7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaad1';
select set_config('test.item', private.roll_item('7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaad1', 10, 'common', 'boss', 'helm', null)::text, false);
update public.items set stage = 31, upgrades = 0 where id = current_setting('test.item')::bigint;

select pg_temp.check(abs(private.upgrade_price('7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaad1',
  (select item from public.items item where id = current_setting('test.item')::bigint), 1)
  - private.gear_base('7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaad1', 31, 'common') * 0.3) < 0.001,
  'une amélioration coûte 30 % du prix d''achat au niveau où l''objet est apparu, pas au record');
update public.items set upgrades = 2 where id = current_setting('test.item')::bigint;
select pg_temp.check(abs(private.upgrade_price('7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaad1',
  (select item from public.items item where id = current_setting('test.item')::bigint), 1)
  - private.gear_base('7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaad1', 31, 'common') * 0.3 * 1.25 * 1.25) < 0.001,
  'chaque amélioration coûte 25 % de plus que la précédente');

delete from auth.users where id = '7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaad1';
