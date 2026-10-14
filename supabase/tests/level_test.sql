\set ON_ERROR_STOP on

insert into auth.users (id) values ('79999999-9999-9999-9999-9999999999c1');

create function pg_temp.check(p_condition boolean, p_message text) returns void
language plpgsql
as $$
begin
  if p_condition is not true then
    raise exception 'Échec : %', p_message;
  end if;
end;
$$;

create function pg_temp.level(p_fighter text) returns integer
language sql
as $$
  select level from public.frontier_fighters where player_id = '79999999-9999-9999-9999-9999999999c1' and fighter = p_fighter;
$$;

grant execute on function pg_temp.check(boolean, text) to authenticated;

select set_config('request.jwt.claim.sub', '79999999-9999-9999-9999-9999999999c1', false);
set role authenticated;
select pg_temp.check(public.start_frontier('00000000-0000-0000-0000-0000000000c1') #>> '{frontier,revision}' = '0', 'partie créée');
reset role;

insert into public.frontier_fighters (player_id, fighter, level, xp)
values ('79999999-9999-9999-9999-9999999999c1', 'hero', 300, 12345), ('79999999-9999-9999-9999-9999999999c1', 'archer', 150, 50),
  ('79999999-9999-9999-9999-9999999999c1', 'mage', 0, 0)
on conflict (player_id, fighter) do update set level = excluded.level, xp = excluded.xp;
update public.frontiers set saved_by = '00000000-0000-0000-0000-0000000000c1' where player_id = '79999999-9999-9999-9999-9999999999c1';
update private.settings set level_curve = 1;

select private.steepen_levels();
select pg_temp.check(pg_temp.level('hero') = 104 and pg_temp.level('archer') = 52 and pg_temp.level('mage') = 0, 'niveaux ramenés à la nouvelle courbe');
select pg_temp.check(not exists (select 1 from public.frontier_fighters where player_id = '79999999-9999-9999-9999-9999999999c1' and xp <> 0), 'xp remise à zéro');
select pg_temp.check((select revision = 1 and saved_by is null from public.frontiers where player_id = '79999999-9999-9999-9999-9999999999c1'), 'nouvelle révision : une ancienne copie locale ne passe plus devant');
select pg_temp.check((select level_curve from private.settings) = 2, 'conversion notée');

select private.steepen_levels();
select pg_temp.check(pg_temp.level('hero') = 104, 'une seconde exécution ne convertit pas deux fois');
select pg_temp.check(abs(private.hero_power('79999999-9999-9999-9999-9999999999c1') - 11 * 104 * power(3.2::double precision, 10)) < 1, 'les renforts suivent la nouvelle courbe');

delete from auth.users where id = '79999999-9999-9999-9999-9999999999c1';
