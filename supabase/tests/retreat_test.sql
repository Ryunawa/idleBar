\set ON_ERROR_STOP on

insert into auth.users (id) values ('7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaac1');

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

select set_config('request.jwt.claim.sub', '7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaac1', false);
set role authenticated;
select pg_temp.check(public.start_frontier('00000000-0000-0000-0000-0000000000c1') #>> '{frontier,revision}' = '0', 'partie créée');
reset role;

select set_config('test.helm', private.roll_item('7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaac1', 10, 'common', 'boss', 'helm', null)::text, false);
select set_config('test.armor', private.roll_item('7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaac1', 10, 'common', 'boss', 'armor', null)::text, false);
select set_config('test.feet', private.roll_item('7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaac1', 10, 'common', 'boss', 'feet', null)::text, false);
insert into public.hero_equipment (player_id, fighter, slot, item_id)
values ('7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaac1', 'hero', 'helm', current_setting('test.helm')::bigint),
       ('7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaac1', 'hero', 'feet', current_setting('test.feet')::bigint),
       ('7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaac1', 'archer', 'armor', current_setting('test.armor')::bigint);
select pg_temp.check(private.bag_count('7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaac1') = 0, 'trois objets portés, sac vide');

update private.settings set bag_size = 1;
update public.frontiers set retreats = retreats where player_id = '7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaac1';
select pg_temp.check((select count(*) from public.hero_equipment where player_id = '7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaac1') = 3, 'rien ne bouge sans repli');

update public.frontiers set retreats = retreats + 1 where player_id = '7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaac1';
select pg_temp.check(not exists (select 1 from public.hero_equipment where player_id = '7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaac1'), 'le repli retire tout l''équipement');
select pg_temp.check(private.bag_count('7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaac1') = 3, 'tout revient au sac, au-delà de sa limite, pieds compris');
select pg_temp.check((select count(*) from public.items where owner_id = '7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaac1') = 3, 'aucun objet perdu');
select pg_temp.check(private.roll_item('7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaac1', 10, 'common', 'boss') is null, 'un sac qui déborde refuse le butin suivant');
update private.settings set bag_size = 60;

delete from auth.users where id = '7aaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaac1';
