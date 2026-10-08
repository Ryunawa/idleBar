\set ON_ERROR_STOP on

insert into auth.users (id) values
  ('bbbbbbbb-0000-0000-0000-000000000001'),
  ('bbbbbbbb-0000-0000-0000-000000000002'),
  ('bbbbbbbb-0000-0000-0000-000000000003');

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
  select set_config('request.jwt.claim.sub', 'bbbbbbbb-0000-0000-0000-00000000000' || p_player, false);
$$;

grant execute on function pg_temp.check(boolean, text) to authenticated;
grant execute on function pg_temp.expect_error(text, text) to authenticated;
grant execute on function pg_temp.act(text) to authenticated;

set role authenticated;
select pg_temp.act('1');
select public.found_tavern('Le Xénon');
select pg_temp.act('2');
select public.found_tavern('Le Yéti');
select pg_temp.act('3');
select public.found_tavern('Le Zéphyr');
reset role;

insert into public.friendships (player_id, friend_id) values
  ('bbbbbbbb-0000-0000-0000-000000000001', 'bbbbbbbb-0000-0000-0000-000000000002'),
  ('bbbbbbbb-0000-0000-0000-000000000002', 'bbbbbbbb-0000-0000-0000-000000000001');
update public.taverns set last_seen = now() - interval '1 hour' where player_id::text not like 'bbbbbbbb-%';
delete from realtime.messages;

set role authenticated;
select pg_temp.act('1');
select pg_temp.check(
  (select array_agg(passer ->> 'name') from jsonb_array_elements(public.get_state() #> '{tavern,passersby}') passer) @> array['Le Zéphyr']
  and not (select array_agg(passer ->> 'name') from jsonb_array_elements(public.get_state() #> '{tavern,passersby}') passer) @> array['Le Yéti'],
  'le Zéphyr passe dans la rue, pas l''ami Yéti');
select public.greet_passerby('bbbbbbbb-0000-0000-0000-000000000003');
select public.greet_passerby('bbbbbbbb-0000-0000-0000-000000000003');
select public.invite_passerby('bbbbbbbb-0000-0000-0000-000000000003');
select pg_temp.expect_error('select public.invite_passerby(''bbbbbbbb-0000-0000-0000-000000000002'')', 'déjà amis');
reset role;

select pg_temp.check(
  (select count(*) from realtime.messages where topic = 'taverne:bbbbbbbb-0000-0000-0000-000000000003' and event = 'wave') = 1,
  'un seul salut toutes les 30 secondes');

set role authenticated;
select pg_temp.act('3');
select pg_temp.check(public.get_state() #>> '{tavern,invitations,0,name}' = 'Le Xénon', 'le Zéphyr est invité par le Xénon');
select public.answer_invitation('bbbbbbbb-0000-0000-0000-000000000001', true);
select pg_temp.check(public.get_state() #>> '{tavern,friends,0,name}' = 'Le Xénon', 'invitation acceptée : amis');
select pg_temp.check(public.get_state() #>> '{tavern,outing,host}' = 'Le Xénon', 'invitation acceptée : en visite');
reset role;

update public.taverns set coins = 500, renown = 0 where player_id = 'bbbbbbbb-0000-0000-0000-000000000001';
update public.taverns set coins = 0 where player_id in ('bbbbbbbb-0000-0000-0000-000000000002', 'bbbbbbbb-0000-0000-0000-000000000003');

set role authenticated;
select pg_temp.act('1');
select public.offer_round();
select pg_temp.check((public.get_state() #>> '{tavern,coins}')::integer = 300, 'la tournée coûte 200 écus');
select pg_temp.check((public.get_state() #>> '{tavern,renown}')::integer = 10, 'la tournée rapporte de la renommée');
select pg_temp.check(public.get_state() #>> '{tavern,round_ready_at}' is not null, 'prochaine tournée annoncée');
select pg_temp.expect_error('select public.offer_round()', 'encore dans toutes les têtes');
select pg_temp.act('2');
select pg_temp.check((public.get_state() #>> '{tavern,coins}')::integer = 30, 'le Yéti reçoit la tournée');
select pg_temp.check(public.get_state() #>> '{tavern,rounds,0,from}' = 'Le Xénon', 'le Yéti sait qui a offert');
select pg_temp.act('3');
select pg_temp.check((public.get_state() #>> '{tavern,coins}')::integer = 30, 'le Zéphyr reçoit la tournée');
reset role;
