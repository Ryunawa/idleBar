\set ON_ERROR_STOP on

insert into auth.users (id) values ('7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaab1');

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

select set_config('request.jwt.claim.sub', '7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaab1', false);
set role authenticated;
select pg_temp.check(public.start_frontier('00000000-0000-0000-0000-0000000000b1') #>> '{frontier,revision}' = '0', 'partie créée');
reset role;

update private.settings set bag_size = 2;
select set_config('test.worn', private.roll_item('7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaab1', 10, 'common', 'boss', 'helm', null)::text, false);
insert into public.hero_equipment (player_id, fighter, slot, item_id)
values ('7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaab1', 'hero', 'helm', current_setting('test.worn')::bigint);
select pg_temp.check(private.bag_count('7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaab1') = 0, 'un objet équipé ne compte pas dans le sac');
select pg_temp.check(private.roll_item('7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaab1', 10, 'common', 'boss') is not null, 'premier objet au sac');
select pg_temp.check(private.roll_item('7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaab1', 10, 'common', 'boss') is not null, 'second objet au sac malgré l''objet porté');
select pg_temp.check(private.roll_item('7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaab1', 10, 'common', 'boss') is null, 'sac plein à deux objets non portés');
set role authenticated;
select pg_temp.check(public.unequip_item('helm', 'hero') ->> 'status' = 'bag_full', 'on ne retire rien dans un sac plein');
reset role;
update private.settings set bag_size = 3;
set role authenticated;
select pg_temp.check(public.unequip_item('helm', 'hero') ->> 'status' = 'ok', 'retiré quand il y a de la place');
reset role;
with ring as (
  insert into public.items (owner_id, slot, rarity, item_level, stat_a, value_a, source)
  values ('7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaab1', 'ring', 'common', 1, 'gold', 1, 'boss') returning id)
insert into public.hero_equipment (player_id, fighter, slot, item_id)
select '7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaab1', 'hero', 'ring', id from ring;
set role authenticated;
select pg_temp.check(public.unequip_item('ring', 'hero') ->> 'status' = 'bag_full', 'un anneau reste au doigt quand le sac est plein');
reset role;
update private.settings set bag_size = 4;
set role authenticated;
select pg_temp.check(public.unequip_item('ring', 'hero') ->> 'status' = 'ok', 'l''anneau rejoint le sac quand il y a de la place');
reset role;
update private.settings set bag_size = 60;

delete from auth.users where id = '7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaab1';
