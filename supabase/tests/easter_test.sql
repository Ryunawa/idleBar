\set ON_ERROR_STOP on

insert into auth.users (id) values
  ('cccccccc-0000-0000-0000-000000000001'),
  ('cccccccc-0000-0000-0000-000000000002');

create function pg_temp.check(p_condition boolean, p_message text) returns void
language plpgsql
as $$
begin
  if p_condition is not true then
    raise exception 'Échec : %', p_message;
  end if;
end;
$$;

create function pg_temp.expect_error(p_statement text, p_fragment text) returns void
language plpgsql
as $$
begin
  execute p_statement;
  raise exception 'Attendu une erreur « % » pour %', p_fragment, p_statement using errcode = 'XX000';
exception
  when raise_exception then
    if position(p_fragment in sqlerrm) = 0 then
      raise exception 'Erreur inattendue pour % : %', p_statement, sqlerrm;
    end if;
end;
$$;

create function pg_temp.act(p_player text) returns void
language sql
as $$
  select set_config('request.jwt.claim.sub', 'cccccccc-0000-0000-0000-00000000000' || p_player, false);
$$;

grant execute on function pg_temp.check(boolean, text) to authenticated;
grant execute on function pg_temp.expect_error(text, text) to authenticated;
grant execute on function pg_temp.act(text) to authenticated;

set role authenticated;
select pg_temp.act('1');
select public.found_tavern('Le Dragon Doré');
select pg_temp.act('2');
select public.found_tavern('La Chope Commune');
reset role;

insert into public.friendships (player_id, friend_id) values
  ('cccccccc-0000-0000-0000-000000000001', 'cccccccc-0000-0000-0000-000000000002'),
  ('cccccccc-0000-0000-0000-000000000002', 'cccccccc-0000-0000-0000-000000000001');
insert into public.stamp_owners (stamp_id, player_id) values ('dragon', 'cccccccc-0000-0000-0000-000000000001');

set role authenticated;
select pg_temp.act('2');
select pg_temp.check(
  not exists (select 1 from jsonb_array_elements(public.get_world() -> 'stamps') stamp where stamp ->> 'id' = 'dragon'),
  'le sceau du dragon est caché aux autres');
select pg_temp.expect_error('select public.start_visit(''cccccccc-0000-0000-0000-000000000001'', ''dragon'')', 'quelqu''un d''autre');

select pg_temp.act('1');
select pg_temp.check(public.get_world() #>> '{stamps,0,id}' = 'dragon', 'son propriétaire voit le sceau en premier');
select public.start_visit('cccccccc-0000-0000-0000-000000000002', 'dragon');
select pg_temp.act('2');
select pg_temp.check(public.get_state() #>> '{tavern,guests,0,stamp}' = 'dragon', 'le sceau du dragon arrive chez l''hôte');
reset role;
