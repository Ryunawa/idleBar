\set ON_ERROR_STOP on

insert into auth.users (id) values
  ('dddddddd-0000-0000-0000-000000000001'),
  ('dddddddd-0000-0000-0000-000000000002'),
  ('dddddddd-0000-0000-0000-000000000003'),
  ('dddddddd-0000-0000-0000-000000000004');

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

create function pg_temp.expect_denied(p_statement text) returns void
language plpgsql
as $$
begin
  execute p_statement;
  raise exception 'Attendu un refus pour %', p_statement using errcode = 'XX000';
exception
  when insufficient_privilege then
    null;
end;
$$;

create function pg_temp.act(p_player text) returns void
language sql
as $$
  select set_config('request.jwt.claim.sub', 'dddddddd-0000-0000-0000-00000000000' || p_player, false);
$$;

create function pg_temp.player(p_player text) returns uuid
language sql
as $$
  select ('dddddddd-0000-0000-0000-00000000000' || p_player)::uuid;
$$;

grant execute on function pg_temp.check(boolean, text) to anon, authenticated;
grant execute on function pg_temp.expect_error(text, text) to anon, authenticated;
grant execute on function pg_temp.expect_denied(text) to anon, authenticated;
grant execute on function pg_temp.act(text) to anon, authenticated;
grant execute on function pg_temp.player(text) to anon, authenticated;

set role anon;
select pg_temp.expect_denied('select public.sync_state(true)');
select pg_temp.expect_denied('select public.leave_visit()');
select pg_temp.expect_denied('select public.say(''bonjour'')');
select pg_temp.expect_denied('select public.mute_player(gen_random_uuid(), true)');
select pg_temp.expect_denied('select public.get_friend_profile(gen_random_uuid())');
reset role;

set role authenticated;
select pg_temp.act('1');
select public.found_tavern('Chez Diane');
select pg_temp.act('2');
select public.found_tavern('Chez Émile');
select pg_temp.act('3');
select public.found_tavern('Chez Fanny');
select pg_temp.act('4');
select public.found_tavern('Chez Gaston');
reset role;

insert into public.friendships (player_id, friend_id) values
  (pg_temp.player('1'), pg_temp.player('2')), (pg_temp.player('2'), pg_temp.player('1')),
  (pg_temp.player('1'), pg_temp.player('3')), (pg_temp.player('3'), pg_temp.player('1')),
  (pg_temp.player('1'), pg_temp.player('4')), (pg_temp.player('4'), pg_temp.player('1')),
  (pg_temp.player('4'), pg_temp.player('2')), (pg_temp.player('2'), pg_temp.player('4'));
update private.settings set chat_seconds = 0, visit_hop_seconds = 0;
delete from realtime.messages;

set role authenticated;
select pg_temp.act('1');
select pg_temp.expect_error('select public.say(''Il y a quelqu''''un ?'')', 'Personne ne t''entend');
select pg_temp.check((public.get_state() -> 'tavern' -> 'room' ->> 'mine')::boolean, 'chez soi, la salle est la sienne');

select pg_temp.act('2');
select public.start_visit(pg_temp.player('1'), 'heart');
select pg_temp.act('3');
select public.start_visit(pg_temp.player('1'), 'star');
reset role;

select pg_temp.check(
  (select count(*) from realtime.messages where topic = 'taverne:' || pg_temp.player('2') and event = 'refresh') = 1,
  'le premier invité est prévenu de l''arrivée du second');

set role authenticated;
select pg_temp.act('2');
select pg_temp.check(not (public.get_state() -> 'tavern' -> 'room' ->> 'mine')::boolean, 'en visite, la salle est celle de l''hôte');
select pg_temp.check(public.get_state() -> 'tavern' -> 'room' ->> 'host' = 'Chez Diane', 'la salle porte le nom de l''hôte');
select pg_temp.check(jsonb_array_length(public.get_state() -> 'tavern' -> 'room' -> 'guests') = 2, 'deux invités sont assis chez l''hôte');
select pg_temp.check((public.get_state() -> 'tavern' -> 'room' ->> 'stools')::integer = 4, 'la salle a les tabourets de l''hôte');
select pg_temp.expect_error('select public.say(''   '')', 'Écris quelque chose');
select pg_temp.expect_error('select public.say(repeat(''a'', 121))', 'trop long');
select public.say(E'Salut   la\ncompagnie !');
reset role;
select pg_temp.check(
  (select body from public.chat_messages where author_id = pg_temp.player('2') order by id desc limit 1) = 'Salut la compagnie !',
  'les espaces et retours à la ligne sont resserrés');
set role authenticated;
select pg_temp.act('2');
select public.say('Espèce de CONNARD, va');
select public.say('Ce fût est content et la constitution tient');
select public.say('fuuuck ce t4p3tte');
reset role;

select pg_temp.check(
  (select array_agg(body order by id) from public.chat_messages where author_id = pg_temp.player('2')) =
    array['Salut la compagnie !', 'Espèce de ******** va', 'Ce fût est content et la constitution tient', '****** ce *******'],
  'les grossièretés sont masquées, pas les mots voisins');
select pg_temp.check(
  (select count(*) from realtime.messages where topic = 'taverne:' || pg_temp.player('1') and event = 'chat') = 4,
  'l''hôte reçoit chaque message');
select pg_temp.check(
  (select count(*) from realtime.messages where topic = 'taverne:' || pg_temp.player('2') and event = 'chat') = 0,
  'l''auteur ne se sonne pas lui-même');

set role authenticated;
select pg_temp.act('3');
select pg_temp.check(jsonb_array_length(public.get_state() -> 'tavern' -> 'room' -> 'chat') = 4, 'le second invité voit le fil');
select public.mute_player(pg_temp.player('2'), true);
select pg_temp.check(jsonb_array_length(public.get_state() -> 'tavern' -> 'room' -> 'chat') = 0, 'un joueur masqué disparaît du fil');
select pg_temp.check(public.get_state() -> 'tavern' -> 'muted' -> 0 ->> 'name' = 'Chez Émile', 'la liste des masqués le montre');
select pg_temp.expect_error('select public.mute_player(auth.uid(), true)', 'toi-même');
reset role;

delete from realtime.messages;
set role authenticated;
select pg_temp.act('2');
select public.say('Tu m''entends ?');
reset role;

select pg_temp.check(
  (select count(*) from realtime.messages where topic = 'taverne:' || pg_temp.player('3') and event = 'chat') = 0,
  'un joueur qui a masqué l''auteur n''est pas sonné');
select pg_temp.check(
  (select count(*) from realtime.messages where topic = 'taverne:' || pg_temp.player('1') and event = 'chat') = 1,
  'les autres le sont');

set role authenticated;
select pg_temp.act('3');
select public.mute_player(pg_temp.player('2'), false);
select pg_temp.check(jsonb_array_length(public.get_state() -> 'tavern' -> 'muted') = 0, 'on peut ne plus masquer');

select pg_temp.act('1');
select public.serve_visit((select (public.get_state() -> 'tavern' -> 'room' -> 'guests' -> 1 ->> 'visit')::bigint), true);
select pg_temp.check(
  ((public.get_state() -> 'tavern' -> 'room' -> 'guests' -> 1 ->> 'served')::boolean),
  'un invité servi reste assis');
select public.say('Bienvenue à tous !');

select pg_temp.act('3');
select public.sync_state(true);
reset role;

update public.visits set served_at = now() - interval '10 minutes' where visitor_id = pg_temp.player('3');
set role authenticated;
select pg_temp.act('1');
select pg_temp.check(jsonb_array_length(public.get_state() -> 'tavern' -> 'room' -> 'guests') = 2, 'un invité qui reste présent garde sa place');
reset role;

update public.visits set present_until = now() - interval '1 second' where visitor_id = pg_temp.player('3');
set role authenticated;
select pg_temp.act('1');
select pg_temp.check(jsonb_array_length(public.get_state() -> 'tavern' -> 'room' -> 'guests') = 2, 'un invité qui ne se manifeste plus reste assis jusqu''à son départ');

select pg_temp.act('2');
select public.leave_visit();
select pg_temp.check((public.get_state() -> 'tavern' -> 'room' ->> 'mine')::boolean, 'après le départ, on est chez soi');
select pg_temp.act('1');
select pg_temp.check(jsonb_array_length(public.get_state() -> 'tavern' -> 'room' -> 'guests') = 1, 'seul l''invité parti quitte la salle');

select pg_temp.act('2');
select public.start_visit(pg_temp.player('1'), 'mug');
select public.start_visit(pg_temp.player('4'), 'sun');
reset role;

select pg_temp.check(
  (select count(*) from public.visits where visitor_id = pg_temp.player('2') and left_at is null) = 1,
  'aller chez un autre ami ferme la visite précédente');

update private.settings set max_guests = 1;
set role authenticated;
select pg_temp.act('3');
select public.start_visit(pg_temp.player('1'), 'moon');
select pg_temp.act('4');
select pg_temp.expect_error('select public.start_visit(pg_temp.player(''1''), ''note'')', 'pleine');
reset role;
update private.settings set max_guests = 6;

set role authenticated;
select pg_temp.act('2');
select pg_temp.check(public.get_friend_profile(pg_temp.player('1')) ->> 'name' = 'Chez Diane', 'la fiche d''un ami donne son nom');
select pg_temp.check(
  public.get_friend_profile(pg_temp.player('1')) -> 'guestbook' @> '[{"name": "Chez Émile", "stamp": "sun", "visits": 2}]',
  'la fiche montre le livre d''or avec le nom et le tampon de chaque visiteur');
select pg_temp.expect_error('select public.get_friend_profile(pg_temp.player(''3''))', 'Seuls tes amis');
select pg_temp.act('3');
select pg_temp.check(
  jsonb_array_length(public.get_friend_profile(pg_temp.player('1')) -> 'tasted') = 0,
  'l''hôte n''a encore rien goûté');
select pg_temp.check(
  public.get_friend_profile(pg_temp.player('1')) ? 'avatar',
  'la fiche montre l''avatar');
select pg_temp.act('2');
select pg_temp.expect_error('select public.set_stamp(''dragon'')', 'quelqu''un d''autre');
select public.set_stamp('crown');
select pg_temp.act('1');
select pg_temp.check(
  (select entry from jsonb_array_elements(public.get_state() -> 'tavern' -> 'guestbook') entry where entry ->> 'name' = 'Chez Émile')
    @> '{"stamp": "crown", "visits": 2}',
  'le livre d''or montre le tampon actuel du visiteur et ses visites');
select pg_temp.act('1');
select public.show_door((public.get_state() -> 'tavern' -> 'room' -> 'guests' -> 0 ->> 'visit')::bigint);
select pg_temp.check(jsonb_array_length(public.get_state() -> 'tavern' -> 'room' -> 'guests') = 0, 'l''hôte peut raccompagner un invité');
select pg_temp.check(jsonb_array_length(public.get_state() -> 'tavern' -> 'guests') = 0, 'un invité raccompagné n''attend plus d''être servi');
select pg_temp.act('3');
select pg_temp.check(public.get_state() -> 'tavern' -> 'outing' = 'null'::jsonb, 'l''invité raccompagné n''est plus en visite');

select pg_temp.act('4');
select public.start_visit(pg_temp.player('1'), 'note');
select pg_temp.act('1');
select public.remove_friend(pg_temp.player('4'));
select pg_temp.act('4');
select pg_temp.check((public.get_state() -> 'tavern' -> 'room' ->> 'mine')::boolean, 'un ami retiré quitte la salle');
reset role;

update private.settings set chat_seconds = 2, visit_hop_seconds = 10;
