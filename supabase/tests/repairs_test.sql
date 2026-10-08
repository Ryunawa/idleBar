\set ON_ERROR_STOP on

insert into auth.users (id) values
  ('ffffffff-0000-0000-0000-000000000001'),
  ('ffffffff-0000-0000-0000-000000000002'),
  ('ffffffff-0000-0000-0000-000000000003'),
  ('ffffffff-0000-0000-0000-000000000004');

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
  select set_config('request.jwt.claim.sub', 'ffffffff-0000-0000-0000-00000000000' || p_player, false);
$$;

create function pg_temp.player(p_player text) returns uuid
language sql
as $$
  select ('ffffffff-0000-0000-0000-00000000000' || p_player)::uuid;
$$;

grant execute on function pg_temp.check(boolean, text) to authenticated;
grant execute on function pg_temp.expect_error(text, text) to authenticated;
grant execute on function pg_temp.act(text) to authenticated;
grant execute on function pg_temp.player(text) to authenticated;

select pg_temp.check(private.clean_chat('Espèce de con!') = 'Espèce de ****', 'la ponctuation ne protège plus un mot');
select pg_temp.check(private.clean_chat('fils-de-pute') = '************', 'les traits d''union sont découpés');
select pg_temp.check(private.clean_chat('bande d''enculés') = 'bande *********', 'les apostrophes sont découpées');
select pg_temp.check(private.clean_chat('ｃｏｎｎａｒｄ') = '*******', 'les lettres pleine chasse sont ramenées');
select pg_temp.check(private.clean_chat('сonnard') = '*******', 'les lettres cyrilliques sont ramenées');
select pg_temp.check(private.clean_chat('Ce fût est content, aujourd''hui !') = 'Ce fût est content, aujourd''hui !', 'les mots voisins passent');

set role authenticated;
select pg_temp.act('1');
select pg_temp.expect_error('select public.found_tavern(''Chez l''''Enculé'')', 'plus aimable');
select pg_temp.expect_error('select public.found_tavern(''Au Fils-de-Pute'')', 'plus aimable');
select pg_temp.expect_error('select public.found_tavern(repeat(chr(12644), 3))', 'entre 2 et 24');
select pg_temp.expect_error('select public.found_tavern(''12 !'')', 'deux lettres');
select public.found_tavern('Chez Joël');
select pg_temp.act('2');
select public.found_tavern('Chez Karim');
select pg_temp.act('3');
select public.found_tavern('Chez Lina');
select pg_temp.act('4');
select public.found_tavern('Chez Max');
reset role;

insert into public.friendships (player_id, friend_id) values
  (pg_temp.player('1'), pg_temp.player('2')), (pg_temp.player('2'), pg_temp.player('1')),
  (pg_temp.player('1'), pg_temp.player('4')), (pg_temp.player('4'), pg_temp.player('1'));
update private.settings set visit_hop_seconds = 0, order_seconds = 0, chat_seconds = 0, max_guests = 1;
update public.taverns set reported_at = now() + interval '4 seconds' where player_id = pg_temp.player('1');

set role authenticated;
select pg_temp.act('1');
select public.report_service('{"drinks": {"beer": 2}, "coins": 8}');
select pg_temp.check((public.get_state() #>> '{tavern,served}')::integer = 2, 'un relevé envoyé juste après le précédent est compté');

select pg_temp.act('2');
select public.start_visit(pg_temp.player('1'), 'heart');
select pg_temp.expect_error('select public.say(chr(8203))', 'Écris quelque chose');

select pg_temp.act('3');
select public.set_stamp('crown');
reset role;

insert into public.invitations (from_id, to_id) values (pg_temp.player('1'), pg_temp.player('3'));
set role authenticated;
select pg_temp.act('3');
select public.answer_invitation(pg_temp.player('1'), true);
select pg_temp.check(jsonb_array_length(public.get_state() #> '{tavern,friends}') = 1, 'l''amitié tient même si la taverne est pleine');
select pg_temp.check((public.get_state() -> 'tavern' -> 'room' ->> 'mine')::boolean, 'la visite attendra une place libre');
select pg_temp.check(public.get_state() #>> '{tavern,stamp}' = 'crown', 'accepter une invitation garde son tampon');
reset role;

update public.taverns set last_seen = now() - interval '2 hours' where player_id = pg_temp.player('2');
set role authenticated;
select pg_temp.act('3');
select public.start_visit(pg_temp.player('1'), 'crown');
select pg_temp.check(not (public.get_state() -> 'tavern' -> 'room' ->> 'mine')::boolean, 'un invité assoupi ne prend pas de place');

select pg_temp.act('1');
select public.show_door((select (guest ->> 'visit')::bigint from jsonb_array_elements(public.get_state() -> 'tavern' -> 'room' -> 'guests') guest where guest ->> 'name' = 'Chez Lina'));
select pg_temp.act('3');
select pg_temp.expect_error('select public.start_visit(pg_temp.player(''1''), ''crown'')', 'raccompagné');
reset role;

update public.visits set left_at = now() where visitor_id = pg_temp.player('2');
update private.settings set max_guests = 6;
set role authenticated;
select pg_temp.act('4');
select public.start_visit(pg_temp.player('1'), 'sun');
select public.order_drink('tea');
select pg_temp.act('1');
select public.serve_visit((public.get_state() #>> '{tavern,room,guests,0,visit}')::bigint, false);
select pg_temp.act('4');
select public.order_drink('specialty');
select pg_temp.act('1');
select public.serve_visit((public.get_state() #>> '{tavern,room,guests,0,visit}')::bigint, false);
select pg_temp.act('4');
select pg_temp.check(jsonb_array_length(public.get_state() #> '{tavern,tasted}') = 1, 'une spécialité recommandée rejoint la carte');
reset role;

update public.visits set last_reward_at = now() - interval '1 day' where visitor_id = pg_temp.player('4');
create temp table coins_before as select coins from public.taverns where player_id = pg_temp.player('1');
grant select on coins_before to authenticated;
set role authenticated;
select pg_temp.act('4');
select public.order_drink('beer');
select pg_temp.act('1');
select public.serve_visit((public.get_state() #>> '{tavern,room,guests,0,visit}')::bigint, false);
select pg_temp.check(
  (public.get_state() #>> '{tavern,coins}')::bigint = (select coins from coins_before) + 40,
  'un invité resté jusqu''au lendemain rapporte de nouveau');
select public.mute_player(pg_temp.player('4'), true);
reset role;

delete from realtime.messages;
set role authenticated;
select pg_temp.act('4');
select public.send_emote((public.get_state() #>> '{tavern,outing,visit}')::bigint, 'cheers');
reset role;
select pg_temp.check(
  (select count(*) from realtime.messages where topic = 'taverne:' || pg_temp.player('1') and event = 'emote') = 0,
  'un hôte qui a masqué son invité ne reçoit plus ses émotes');

update private.settings set visit_hop_seconds = 10, order_seconds = 5, chat_seconds = 2, max_guests = 6;
