\set ON_ERROR_STOP on

insert into auth.users (id) values
  ('aaaaaaaa-0000-0000-0000-000000000001'),
  ('aaaaaaaa-0000-0000-0000-000000000002'),
  ('aaaaaaaa-0000-0000-0000-000000000003');

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
  select set_config('request.jwt.claim.sub', 'aaaaaaaa-0000-0000-0000-00000000000' || p_player, false);
$$;

grant execute on function pg_temp.check(boolean, text) to authenticated;
grant execute on function pg_temp.expect_error(text, text) to authenticated;
grant execute on function pg_temp.act(text) to authenticated;

set role authenticated;
select pg_temp.act('1');
select public.found_tavern('Chez Aline');
select pg_temp.act('2');
select public.found_tavern('Chez Bruno');
select pg_temp.act('3');
select public.found_tavern('Chez Carla');
reset role;

update public.taverns set friend_code = 'AAAAA2' where player_id = 'aaaaaaaa-0000-0000-0000-000000000001';
update public.taverns set friend_code = 'BBBBB2' where player_id = 'aaaaaaaa-0000-0000-0000-000000000002';
update public.taverns set friend_code = 'CCCCC2' where player_id = 'aaaaaaaa-0000-0000-0000-000000000003';
delete from realtime.messages;

set role authenticated;
select pg_temp.act('1');
select pg_temp.expect_error('select public.request_friend(''ZZZZZ9'')', 'Aucune taverne');
select pg_temp.expect_error('select public.request_friend(''AAAAA2'')', 'propre code');
select public.request_friend(' bbbbb2 ');
select pg_temp.act('2');
select pg_temp.check(public.get_state() #>> '{tavern,requests,0,name}' = 'Chez Aline', 'Bruno voit la demande d''Aline');
select public.answer_friend('aaaaaaaa-0000-0000-0000-000000000001', true);
select pg_temp.check(public.get_state() #>> '{tavern,friends,0,name}' = 'Chez Aline', 'Bruno et Aline sont amis');
select pg_temp.act('3');
select public.request_friend('BBBBB2');
select pg_temp.act('2');
select public.request_friend('CCCCC2');
select pg_temp.check(jsonb_array_length(public.get_state() #> '{tavern,friends}') = 2, 'demandes croisées : amis tout de suite');
select pg_temp.check((public.get_state() #>> '{tavern,friends,0,online}')::boolean, 'Aline est en ligne');
reset role;

select pg_temp.check(
  (select count(*) from realtime.messages where topic = 'taverne:aaaaaaaa-0000-0000-0000-000000000002') >= 1,
  'Bruno a été prévenu de la demande');

update public.taverns set coins = 0, renown = 0 where player_id in ('aaaaaaaa-0000-0000-0000-000000000001', 'aaaaaaaa-0000-0000-0000-000000000002');

set role authenticated;
select pg_temp.act('2');
select public.set_specialty('Hydromel', 'de la Lune', 'tea', 3);
select pg_temp.expect_error('select public.set_specialty(''Bière'', ''de la Lune'', ''tea'', 3)', 'dans les listes');
select pg_temp.expect_error('select public.set_specialty(''Hydromel'', ''de la Lune'', ''pie'', 3)', 'de ta carte');
select public.set_avatar('{"head": 9, "skin": 2, "hair": "roux", "clothes": 3, "accent": -1}');
select pg_temp.check(public.get_state() #> '{tavern,avatar}' = '{"head": 5, "skin": 2, "hair": 0, "clothes": 3, "accent": 0}'::jsonb, 'avatar borné');

select pg_temp.act('1');
select pg_temp.expect_error('select public.start_visit(''aaaaaaaa-0000-0000-0000-000000000003'', ''heart'')', 'qu''à tes amis');
select pg_temp.expect_error('select public.start_visit(''aaaaaaaa-0000-0000-0000-000000000002'', ''fusee'')', 'tampon');
select public.start_visit('aaaaaaaa-0000-0000-0000-000000000002', 'heart');
select pg_temp.check(public.get_state() #>> '{tavern,outing,host}' = 'Chez Bruno', 'Aline est en visite chez Bruno');
select pg_temp.expect_error('select public.start_visit(''aaaaaaaa-0000-0000-0000-000000000002'', ''star'')', 'déjà en visite');
select public.send_emote((public.get_state() #>> '{tavern,outing,visit}')::bigint, 'cheers');
select public.send_emote((public.get_state() #>> '{tavern,outing,visit}')::bigint, 'laugh');

select pg_temp.act('2');
select pg_temp.check(public.get_state() #>> '{tavern,guests,0,name}' = 'Chez Aline', 'Bruno voit Aline arriver');
select pg_temp.check(public.get_state() #>> '{tavern,guests,0,drink}' = 'tea', 'Aline commandera la spécialité de Bruno');
select public.serve_visit((public.get_state() #>> '{tavern,guests,0,visit}')::bigint, true);
select pg_temp.check((public.get_state() #>> '{tavern,coins}')::integer = 50, 'pourboire d''ami parfait : 40 + 10');
select pg_temp.check((public.get_state() #>> '{tavern,renown}')::integer = 5, 'renommée d''ami');
select pg_temp.check(public.get_state() #>> '{tavern,guestbook,0,stamp}' = 'heart', 'tampon dans le livre d''or');

select pg_temp.act('1');
select pg_temp.check((public.get_state() #>> '{tavern,coins}')::integer = 20, 'cadeau du visiteur');
select pg_temp.check((public.get_state() #>> '{tavern,outing,perfect}')::boolean, 'Aline a été servie parfaitement');
select pg_temp.check(public.get_state() #>> '{tavern,tasted,0,name}' = 'Hydromel de la Lune', 'spécialité goûtée');
reset role;

select pg_temp.check(
  (select count(*) from realtime.messages where topic = 'taverne:aaaaaaaa-0000-0000-0000-000000000002' and event = 'emote') = 1,
  'une seule émote toutes les deux secondes');

update public.visits set served_at = now() - interval '2 minutes' where visitor_id = 'aaaaaaaa-0000-0000-0000-000000000001';
set role authenticated;
select pg_temp.act('1');
select public.start_visit('aaaaaaaa-0000-0000-0000-000000000002', 'star');
reset role;

update public.visits set started_at = now() - interval '5 minutes' where visitor_id = 'aaaaaaaa-0000-0000-0000-000000000001' and served_at is null;
set role authenticated;
select pg_temp.act('2');
select pg_temp.check(jsonb_array_length(public.get_state() #> '{tavern,guests}') = 0, 'l''aide a servi la deuxième visite');
select pg_temp.check((public.get_state() #>> '{tavern,coins}')::integer = 50, 'une seule récompense par ami et par jour');
reset role;

select pg_temp.check(
  (select helped and not rewarded from public.visits where visitor_id = 'aaaaaaaa-0000-0000-0000-000000000001' order by id desc limit 1),
  'deuxième visite servie par l''aide, sans récompense');
