alter table public.visits add column if not exists last_reward_at timestamptz;
alter table public.visits add column if not exists shown_out boolean not null default false;
alter table private.settings add column if not exists door_minutes integer not null default 30 check (door_minutes >= 0);

update public.visits set last_reward_at = served_at where rewarded and last_reward_at is null;

update public.visits
set left_at = present_until + interval '90 seconds'
where left_at is null and present_until < '2026-10-08 12:30:00+00';

create or replace function private.present_visits(p_host uuid) returns setof public.visits
language sql stable
set search_path = ''
as $$
  select visits.* from public.visits
  where visits.host_id = p_host and visits.left_at is null and private.is_present(visits)
  order by visits.id;
$$;

create or replace function private.current_visit(p_player_id uuid) returns public.visits
language sql stable
set search_path = ''
as $$
  select visits.* from public.visits
  where visits.visitor_id = p_player_id and visits.left_at is null and private.is_present(visits)
  order by visits.id desc
  limit 1;
$$;

create or replace function private.visible(p_text text) returns text
language sql immutable
set search_path = ''
as $$
  select btrim(regexp_replace(
    regexp_replace(
      regexp_replace(normalize(coalesce(p_text, ''), NFKC), '[[:cntrl:]]', ' ', 'g'),
      '[­؜ᅟᅠ​-‏‪-‮⁠-⁩⠀ㅤ﻿ﾠ]', '', 'g'),
    '[[:space:]]+', ' ', 'g'));
$$;

create or replace function private.chat_key(p_word text) returns text
language sql immutable
set search_path = ''
as $$
  select regexp_replace(
    regexp_replace(
      translate(
        lower(normalize(p_word, NFKC)),
        'àâäáãåçéèêëíìîïñóòôöõúùûüýÿœæ0134@$5!7аеорсухкмтнві',
        'aaaaaaceeeeiiiinooooouuuuyyoaoieaassitaeopcyxkmthbi'),
      '[^a-z]', '', 'g'),
    '(.)\1{2,}', '\1\1', 'g');
$$;

create or replace function private.banned(p_word text) returns boolean
language plpgsql stable
set search_path = ''
as $$
declare
  v_key text;
  v_short text;
begin
  v_key := private.chat_key(regexp_replace(p_word, '^[^[:alnum:]@$]+|[^[:alnum:]@$]+$', '', 'g'));
  if v_key = '' then
    return false;
  end if;

  v_short := regexp_replace(v_key, '(.)\1+', '\1', 'g');
  return exists (
    select 1 from private.chat_words, unnest(array[v_key, v_short]) candidate
    where (kind = 'word' and candidate = word)
       or (kind = 'prefix' and starts_with(candidate, word))
       or (kind = 'part' and position(word in candidate) > 0));
end;
$$;

create or replace function private.clean_chat(p_text text) returns text
language plpgsql stable
set search_path = ''
as $$
declare
  v_token text;
  v_part text;
  v_hit boolean;
  v_words text[] := '{}';
begin
  foreach v_token in array string_to_array(p_text, ' ') loop
    v_hit := private.banned(v_token);
    if not v_hit then
      foreach v_part in array regexp_split_to_array(v_token, '[''’\-]+') loop
        if private.banned(v_part) then
          v_hit := true;
          exit;
        end if;
      end loop;
    end if;

    if v_hit then
      v_token := repeat('*', char_length(v_token));
    end if;

    v_words := v_words || v_token;
  end loop;

  return array_to_string(v_words, ' ');
end;
$$;

create or replace function private.tavern_name(p_name text) returns text
language plpgsql stable
set search_path = ''
as $$
declare
  v_name text := private.visible(p_name);
begin
  if char_length(v_name) not between 2 and 24 then
    raise exception 'Le nom de la taverne doit faire entre 2 et 24 caractères.';
  end if;

  if char_length(regexp_replace(v_name, '[^[:alpha:]]', '', 'g')) < 2 then
    raise exception 'Le nom de la taverne doit contenir au moins deux lettres.';
  end if;

  if private.clean_chat(v_name) <> v_name then
    raise exception 'Choisis un nom de taverne plus aimable.';
  end if;

  return v_name;
end;
$$;

create or replace function private.serve_visit(p_visit public.visits, p_perfect boolean, p_helped boolean) returns void
language plpgsql
set search_path = ''
as $$
declare
  v_settings private.settings;
  v_host public.taverns;
  v_rewarded boolean;
begin
  select * into v_settings from private.settings;
  select * into v_host from public.taverns where player_id = p_visit.host_id;
  v_rewarded := not exists (
    select 1 from public.visits
    where visitor_id = p_visit.visitor_id and host_id = p_visit.host_id
      and last_reward_at >= date_trunc('day', now()));

  update public.visits
  set served_at = coalesce(served_at, now()),
      waiting = false,
      perfect = p_perfect and not p_helped,
      helped = p_helped,
      rewarded = rewarded or v_rewarded,
      last_reward_at = case when v_rewarded then now() else last_reward_at end
  where id = p_visit.id;

  if v_rewarded then
    update public.taverns
    set coins = coins + case when p_helped then v_settings.friend_tip / 2 else v_settings.friend_tip end
          + case when p_perfect and not p_helped then v_settings.friend_perfect_tip else 0 end,
        renown = renown + v_settings.friend_renown
    where player_id = p_visit.host_id;
    update public.taverns set coins = coins + v_settings.visitor_gift where player_id = p_visit.visitor_id;
    perform private.advance_measure(p_visit.host_id, 'friend', 1);
  end if;

  if private.wants_specialty(p_visit) then
    insert into public.tasted (taster_id, host_id, name, drink, color)
    values (p_visit.visitor_id, p_visit.host_id, private.specialty_name(v_host), v_host.specialty_drink, v_host.specialty_color)
    on conflict (taster_id, host_id) do update
    set name = excluded.name, drink = excluded.drink, color = excluded.color, tasted_at = now();
  end if;

  perform private.ring(p_visit.visitor_id, 'refresh', jsonb_build_object('visit', p_visit.id));
  perform private.ring_room(p_visit.host_id, 'refresh', jsonb_build_object('visit', p_visit.id), p_visit.visitor_id);
end;
$$;

create or replace function public.report_service(p_report jsonb) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_tavern public.taverns;
  v_settings private.settings;
  v_menu text[];
  v_elapsed numeric;
  v_cycles integer;
  v_room integer;
  v_drink text;
  v_count integer;
  v_served integer := 0;
  v_accepted jsonb := '{}';
  v_perfect integer;
  v_parting integer;
  v_coins bigint;
  v_regulars integer;
begin
  v_tavern := private.lock_tavern();
  select * into v_settings from private.settings;
  v_menu := private.menu(v_tavern.player_id);
  v_elapsed := extract(epoch from now() - v_tavern.reported_at);
  v_cycles := greatest(0, floor(least(v_elapsed, v_settings.report_window_seconds) / v_settings.serve_cycle_seconds + 0.5))::integer;
  if v_cycles = 0 and v_elapsed > -v_settings.serve_cycle_seconds then
    v_cycles := 1;
  end if;

  v_room := private.stools(v_tavern.player_id) * v_cycles;

  for v_drink, v_count in
    select key, private.whole(value) from jsonb_each(coalesce(p_report -> 'drinks', '{}')) where key = any (v_menu)
  loop
    v_count := least(v_count, v_room - v_served);
    v_served := v_served + v_count;
    v_accepted := v_accepted || jsonb_build_object(v_drink, v_count);
  end loop;

  v_perfect := least(private.whole(p_report -> 'perfect'), v_served);
  v_parting := least(private.whole(p_report -> 'parting'), v_room);
  v_coins := least(private.whole(p_report -> 'coins'), v_served * v_settings.max_drink_coins + v_parting);

  perform private.settle_absence(v_tavern.player_id);
  update public.taverns
  set coins = coins + v_coins,
      served = served + v_served,
      perfect = perfect + v_perfect,
      renown = renown + v_served + v_perfect,
      reported_at = case
        when v_elapsed > v_settings.report_window_seconds then now()
        else v_tavern.reported_at + make_interval(secs => v_cycles * v_settings.serve_cycle_seconds)
      end,
      last_seen = now()
  where player_id = v_tavern.player_id;

  v_regulars := private.advance_regulars(
    v_tavern.player_id,
    case when jsonb_typeof(p_report -> 'regulars') = 'object' then p_report -> 'regulars' end,
    v_cycles,
    v_served);
  perform private.ensure_goals(v_tavern.player_id);
  perform private.advance_goals(v_tavern.player_id, v_served, v_perfect, v_accepted, v_regulars);
  return private.state(v_tavern.player_id);
end;
$$;

create or replace function public.start_visit(p_host uuid, p_stamp text) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_tavern public.taverns;
  v_settings private.settings;
  v_previous public.visits;
begin
  v_tavern := private.lock_tavern();
  select * into v_settings from private.settings;
  perform private.settle_visits(v_tavern.player_id);
  if not private.are_friends(v_tavern.player_id, p_host) then
    raise exception 'Tu ne peux rendre visite qu''à tes amis.';
  end if;

  if not exists (select 1 from public.stamps where id = p_stamp) then
    raise exception 'Choisis un tampon pour le livre d''or.';
  end if;

  if not private.may_stamp(v_tavern.player_id, p_stamp) then
    raise exception 'Ce tampon appartient à quelqu''un d''autre.';
  end if;

  v_previous := private.current_visit(v_tavern.player_id);
  if v_previous.host_id = p_host then
    return private.state(v_tavern.player_id);
  end if;

  if exists (
    select 1 from public.visits
    where visitor_id = v_tavern.player_id and started_at > now() - make_interval(secs => v_settings.visit_hop_seconds)) then
    raise exception 'Prends le temps de finir ton verre avant de repartir.';
  end if;

  if exists (
    select 1 from public.visits
    where visitor_id = v_tavern.player_id and host_id = p_host and shown_out
      and left_at > now() - make_interval(mins => v_settings.door_minutes)) then
    raise exception 'Ton ami t''a raccompagné à la porte : laisse-lui un peu de temps avant de revenir.';
  end if;

  perform pg_advisory_xact_lock(hashtextextended(p_host::text, 0));
  if (select count(*)
      from private.present_visits(p_host) present
      join public.taverns visitor on visitor.player_id = present.visitor_id
      where visitor.last_seen > now() - make_interval(secs => v_settings.online_seconds)) >= v_settings.max_guests then
    raise exception 'La taverne de ton ami est pleine : réessaie dans un instant.';
  end if;

  update public.taverns set stamp = p_stamp where player_id = v_tavern.player_id;
  update public.visits set left_at = now() where visitor_id = v_tavern.player_id and left_at is null;
  if v_previous.id is not null then
    perform private.ring_room(v_previous.host_id, 'refresh', jsonb_build_object('left', v_tavern.name), v_tavern.player_id);
  end if;

  insert into public.visits (visitor_id, host_id, stamp) values (v_tavern.player_id, p_host, p_stamp);
  perform private.ring_room(p_host, 'refresh', jsonb_build_object('guest', v_tavern.name), v_tavern.player_id);
  return private.state(v_tavern.player_id);
end;
$$;

create or replace function public.show_door(p_visit bigint) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_tavern public.taverns;
  v_visit public.visits;
begin
  v_tavern := private.lock_tavern();
  update public.visits set left_at = now(), shown_out = true
  where id = p_visit and host_id = v_tavern.player_id and left_at is null
  returning * into v_visit;
  if v_visit.id is not null then
    perform private.ring(v_visit.visitor_id, 'refresh', jsonb_build_object('door', v_tavern.name));
    perform private.ring_room(v_tavern.player_id, 'refresh', jsonb_build_object('left', v_visit.id), v_tavern.player_id);
  end if;

  return private.state(v_tavern.player_id);
end;
$$;

create or replace function public.say(p_text text) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_tavern public.taverns;
  v_settings private.settings;
  v_visit public.visits;
  v_room uuid;
  v_text text;
  v_member uuid;
  v_id bigint;
  v_at timestamptz;
begin
  v_tavern := private.lock_tavern();
  select * into v_settings from private.settings;
  v_text := private.visible(p_text);
  if v_text = '' then
    raise exception 'Écris quelque chose avant d''envoyer.';
  end if;

  if char_length(v_text) > v_settings.chat_length then
    raise exception 'Ton message est trop long : % caractères au plus.', v_settings.chat_length;
  end if;

  v_visit := private.current_visit(v_tavern.player_id);
  v_room := coalesce(v_visit.host_id, v_tavern.player_id);
  if v_visit.id is null and not exists (select 1 from private.present_visits(v_room)) then
    raise exception 'Personne ne t''entend : invite des amis à boire un verre.';
  end if;

  if exists (
    select 1 from public.chat_messages
    where author_id = v_tavern.player_id and said_at > now() - make_interval(secs => v_settings.chat_seconds)) then
    raise exception 'Doucement : laisse aux autres le temps de lire.';
  end if;

  if (select count(*) from public.chat_messages
      where author_id = v_tavern.player_id and said_at > now() - interval '1 minute') >= v_settings.chat_per_minute then
    raise exception 'Tu parles beaucoup : attends un peu avant le prochain message.';
  end if;

  v_text := private.clean_chat(v_text);
  insert into public.chat_messages (room_id, author_id, body)
  values (v_room, v_tavern.player_id, v_text)
  returning id, said_at into v_id, v_at;
  delete from public.chat_messages where said_at < now() - make_interval(hours => v_settings.chat_keep_hours);

  for v_member in select member from private.room_members(v_room) member loop
    if v_member <> v_tavern.player_id
      and not exists (select 1 from public.chat_mutes where player_id = v_member and muted_id = v_tavern.player_id) then
      perform private.ring(v_member, 'chat', jsonb_build_object(
        'id', v_id, 'room', v_room, 'author', v_tavern.player_id, 'name', v_tavern.name, 'text', v_text, 'at', v_at));
    end if;
  end loop;

  return private.state(v_tavern.player_id);
end;
$$;

create or replace function public.send_emote(p_visit bigint, p_emote text) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_tavern public.taverns;
  v_visit public.visits;
  v_settings private.settings;
  v_other uuid;
begin
  v_tavern := private.lock_tavern();
  select * into v_settings from private.settings;
  if p_emote not in ('cheers', 'thanks', 'laugh') then
    raise exception 'Cette émote n''existe pas.';
  end if;

  select * into v_visit from public.visits
  where id = p_visit and v_tavern.player_id in (visitor_id, host_id) and left_at is null and private.is_present(visits)
  for update;
  if not found then
    raise exception 'La visite est terminée.';
  end if;

  if v_visit.last_emote_at > now() - make_interval(secs => v_settings.emote_seconds) then
    return '{}';
  end if;

  update public.visits set last_emote_at = now() where id = v_visit.id;
  v_other := case when v_visit.host_id = v_tavern.player_id then v_visit.visitor_id else v_visit.host_id end;
  if not exists (select 1 from public.chat_mutes where player_id = v_other and muted_id = v_tavern.player_id) then
    perform private.ring(v_other, 'emote', jsonb_build_object('visit', v_visit.id, 'from', v_tavern.name, 'emote', p_emote));
  end if;

  return '{}';
end;
$$;

create or replace function public.request_friend(p_code text) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_tavern public.taverns;
  v_other public.taverns;
begin
  v_tavern := private.lock_tavern();
  select * into v_other from public.taverns where friend_code = upper(btrim(coalesce(p_code, '')));
  if not found then
    raise exception 'Aucune taverne n''a ce code ami.';
  end if;

  if v_other.player_id = v_tavern.player_id then
    raise exception 'C''est ton propre code ami.';
  end if;

  perform 1 from public.taverns where player_id = v_other.player_id for update;
  if private.are_friends(v_tavern.player_id, v_other.player_id) then
    raise exception 'Vous êtes déjà amis.';
  end if;

  if not private.has_room_for_friend(v_tavern.player_id) then
    raise exception 'Tu as déjà le nombre maximum d''amis.';
  end if;

  if exists (select 1 from public.friend_requests where from_id = v_other.player_id and to_id = v_tavern.player_id) then
    if not private.has_room_for_friend(v_other.player_id) then
      raise exception 'Cette taverne a déjà le nombre maximum d''amis.';
    end if;

    delete from public.friend_requests where from_id = v_other.player_id and to_id = v_tavern.player_id;
    insert into public.friendships (player_id, friend_id) values (v_tavern.player_id, v_other.player_id), (v_other.player_id, v_tavern.player_id)
    on conflict do nothing;
    perform private.ring(v_other.player_id, 'refresh', '{}');
  else
    insert into public.friend_requests (from_id, to_id) values (v_tavern.player_id, v_other.player_id) on conflict do nothing;
    if found then
      perform private.ring(v_other.player_id, 'refresh', '{}');
    end if;
  end if;

  return private.state(v_tavern.player_id);
end;
$$;

create or replace function public.answer_friend(p_from uuid, p_accept boolean) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_tavern public.taverns;
begin
  v_tavern := private.lock_tavern();
  perform 1 from public.taverns where player_id = p_from for update;
  delete from public.friend_requests where from_id = p_from and to_id = v_tavern.player_id;
  if not found then
    raise exception 'Cette demande n''existe plus.';
  end if;

  if p_accept then
    if not private.has_room_for_friend(v_tavern.player_id) then
      raise exception 'Tu as déjà le nombre maximum d''amis.';
    end if;

    if not private.has_room_for_friend(p_from) then
      raise exception 'Cette taverne a déjà le nombre maximum d''amis.';
    end if;

    insert into public.friendships (player_id, friend_id) values (v_tavern.player_id, p_from), (p_from, v_tavern.player_id)
    on conflict do nothing;
    perform private.ring(p_from, 'refresh', '{}');
  end if;

  return private.state(v_tavern.player_id);
end;
$$;

create or replace function public.answer_invitation(p_from uuid, p_accept boolean) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_tavern public.taverns;
begin
  v_tavern := private.lock_tavern();
  perform 1 from public.taverns where player_id = p_from for update;
  delete from public.invitations where from_id = p_from and to_id = v_tavern.player_id;
  if not found then
    raise exception 'Cette invitation n''existe plus.';
  end if;

  if p_accept then
    if not private.are_friends(v_tavern.player_id, p_from) then
      if not private.has_room_for_friend(v_tavern.player_id) then
        raise exception 'Tu as déjà le nombre maximum d''amis.';
      end if;

      if not private.has_room_for_friend(p_from) then
        raise exception 'Cette taverne a déjà le nombre maximum d''amis.';
      end if;
    end if;

    insert into public.friendships (player_id, friend_id) values (v_tavern.player_id, p_from), (p_from, v_tavern.player_id)
    on conflict do nothing;
    delete from public.friend_requests where (from_id = p_from and to_id = v_tavern.player_id) or (from_id = v_tavern.player_id and to_id = p_from);
    perform private.ring(p_from, 'refresh', '{}');
    if (private.current_visit(v_tavern.player_id)).id is null then
      begin
        return public.start_visit(p_from, v_tavern.stamp);
      exception
        when raise_exception then
          return private.state(v_tavern.player_id);
      end;
    end if;
  end if;

  return private.state(v_tavern.player_id);
end;
$$;

revoke all on function public.report_service(jsonb) from public, anon;
revoke all on function public.start_visit(uuid, text) from public, anon;
revoke all on function public.show_door(bigint) from public, anon;
revoke all on function public.say(text) from public, anon;
revoke all on function public.send_emote(bigint, text) from public, anon;
revoke all on function public.request_friend(text) from public, anon;
revoke all on function public.answer_friend(uuid, boolean) from public, anon;
revoke all on function public.answer_invitation(uuid, boolean) from public, anon;
grant execute on function public.report_service(jsonb) to authenticated;
grant execute on function public.start_visit(uuid, text) to authenticated;
grant execute on function public.show_door(bigint) to authenticated;
grant execute on function public.say(text) to authenticated;
grant execute on function public.send_emote(bigint, text) to authenticated;
grant execute on function public.request_friend(text) to authenticated;
grant execute on function public.answer_friend(uuid, boolean) to authenticated;
grant execute on function public.answer_invitation(uuid, boolean) to authenticated;
