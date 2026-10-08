\set ON_ERROR_STOP on

insert into auth.users (id) values
  ('eeeeeeee-0000-0000-0000-000000000001'),
  ('eeeeeeee-0000-0000-0000-000000000002');

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
  select set_config('request.jwt.claim.sub', 'eeeeeeee-0000-0000-0000-00000000000' || p_player, false);
$$;

create function pg_temp.player(p_player text) returns uuid
language sql
as $$
  select ('eeeeeeee-0000-0000-0000-00000000000' || p_player)::uuid;
$$;

create function pg_temp.guest() returns jsonb
language sql
as $$
  select public.get_state() -> 'tavern' -> 'room' -> 'guests' -> 0;
$$;

grant execute on function pg_temp.check(boolean, text) to authenticated;
grant execute on function pg_temp.expect_error(text, text) to authenticated;
grant execute on function pg_temp.act(text) to authenticated;
grant execute on function pg_temp.player(text) to authenticated;
grant execute on function pg_temp.guest() to authenticated;

set role authenticated;
select pg_temp.act('1');
select public.found_tavern('Chez Hector');
select pg_temp.act('2');
select public.found_tavern('Chez Irène');
select pg_temp.expect_error('select public.order_drink(''beer'')', 'Tu n''es chez personne');
reset role;

insert into public.friendships (player_id, friend_id) values
  (pg_temp.player('1'), pg_temp.player('2')), (pg_temp.player('2'), pg_temp.player('1'));
update private.settings set visit_hop_seconds = 0, order_seconds = 0;

set role authenticated;
select pg_temp.act('2');
select public.start_visit(pg_temp.player('1'), 'heart');
select pg_temp.check(pg_temp.guest() ->> 'drink' = 'beer' and (pg_temp.guest() ->> 'specialty')::boolean, 'sans choix, le visiteur commande la spécialité');
select pg_temp.expect_error('select public.order_drink(''pie'')', 'pas à la carte');
select public.order_drink('tea');
select pg_temp.check(pg_temp.guest() ->> 'drink' = 'tea' and not (pg_temp.guest() ->> 'specialty')::boolean, 'le visiteur choisit son plat');

select pg_temp.act('1');
select pg_temp.check(public.get_state() #>> '{tavern,guests,0,drink}' = 'tea', 'l''hôte voit la commande choisie');
select public.serve_visit((pg_temp.guest() ->> 'visit')::bigint, false);
select pg_temp.check((pg_temp.guest() ->> 'served')::boolean, 'le visiteur est servi');
select pg_temp.check((public.get_state() #>> '{tavern,coins}')::integer = 40, 'le premier service est récompensé');

select pg_temp.act('2');
select pg_temp.check(jsonb_array_length(public.get_state() #> '{tavern,tasted}') = 0, 'un thé ordinaire ne compte pas comme la spécialité');
select public.sync_state(true);
select public.order_drink('specialty');
select pg_temp.check(not (pg_temp.guest() ->> 'served')::boolean, 'une nouvelle commande attend d''être servie');
select pg_temp.check((public.get_state() #>> '{tavern,outing,waiting}')::boolean, 'le visiteur sait qu''on va le resservir');

select pg_temp.act('1');
select public.serve_visit((pg_temp.guest() ->> 'visit')::bigint, true);
select pg_temp.check((pg_temp.guest() ->> 'served')::boolean, 'la nouvelle commande est servie');
select pg_temp.check((public.get_state() #>> '{tavern,coins}')::integer = 40, 'une seule récompense par visite et par jour');
reset role;

update public.visits
set served_at = now() - interval '3 days', present_until = now() - interval '2 days'
where visitor_id = pg_temp.player('2');
update public.taverns set last_seen = now() - interval '2 days' where player_id = pg_temp.player('2');

set role authenticated;
select pg_temp.act('1');
select pg_temp.check(jsonb_array_length(public.get_state() -> 'tavern' -> 'room' -> 'guests') = 1, 'un visiteur reste tant qu''il ne part pas');
select pg_temp.check(not (pg_temp.guest() ->> 'online')::boolean, 'un visiteur absent est montré assoupi');

select pg_temp.act('2');
select pg_temp.check(not (public.get_state() -> 'tavern' -> 'room' ->> 'mine')::boolean, 'en revenant, on est toujours chez son ami');
select public.order_drink('beer');
reset role;

update public.visits set ordered_at = now() - interval '10 minutes' where visitor_id = pg_temp.player('2');
set role authenticated;
select pg_temp.act('1');
select pg_temp.check((pg_temp.guest() ->> 'served')::boolean, 'l''aide sert une commande oubliée');
select public.show_door((pg_temp.guest() ->> 'visit')::bigint);
select pg_temp.check(jsonb_array_length(public.get_state() -> 'tavern' -> 'room' -> 'guests') = 0, 'l''hôte peut toujours raccompagner');
reset role;

update private.settings set visit_hop_seconds = 10, order_seconds = 5;
