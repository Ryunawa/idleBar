create or replace function public.get_state() returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
begin
  if auth.uid() is null then
    raise exception 'Connecte-toi pour jouer.';
  end if;

  if not exists (select 1 from public.taverns where player_id = auth.uid()) then
    return jsonb_build_object('server_time', now(), 'tavern', null);
  end if;

  perform private.lock_tavern();
  perform private.settle_absence(auth.uid());
  perform private.settle_visits(auth.uid());
  perform private.ensure_goals(auth.uid());
  update public.taverns set last_seen = now() where player_id = auth.uid();
  return private.state(auth.uid());
end;
$$;

create or replace function public.get_world() returns jsonb
language sql stable
security definer
set search_path = ''
as $$
  select jsonb_build_object(
    'tiers', (select jsonb_agg(jsonb_build_object('tier', tier, 'name', name, 'renown', renown) order by tier) from public.renown_tiers),
    'drinks', (select jsonb_agg(jsonb_build_object('id', id, 'name', name, 'price', price) order by sort) from public.drinks),
    'upgrades', (select jsonb_agg(jsonb_build_object(
      'id', id, 'kind', kind, 'name', name, 'description', description, 'price', price,
      'tier', tier, 'requires', requires, 'value', value) order by sort) from public.upgrades),
    'regulars', (select jsonb_agg(jsonb_build_object(
      'id', regulars.id, 'name', regulars.name, 'title', regulars.title, 'drink', regulars.drink,
      'condition', regulars.condition, 'hint', regulars.hint, 'souvenir', regulars.souvenir,
      'chapters', (select jsonb_agg(jsonb_build_object('chapter', chapter, 'friendship', friendship, 'line', line) order by chapter)
        from public.regular_chapters where regular_id = regulars.id)) order by regulars.sort)
      from public.regulars),
    'bases', (select jsonb_agg(word order by sort) from public.specialty_words where kind = 'base'),
    'complements', (select jsonb_agg(word order by sort) from public.specialty_words where kind = 'complement'),
    'stamps', (select jsonb_agg(jsonb_build_object('id', id, 'name', name) order by sort) from public.stamps));
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

  if private.are_friends(v_tavern.player_id, v_other.player_id) then
    raise exception 'Vous êtes déjà amis.';
  end if;

  if (select count(*) from public.friendships where player_id = v_tavern.player_id) >= (select max_friends from private.settings) then
    raise exception 'Tu as déjà le nombre maximum d''amis.';
  end if;

  if exists (select 1 from public.friend_requests where from_id = v_other.player_id and to_id = v_tavern.player_id) then
    delete from public.friend_requests where from_id = v_other.player_id and to_id = v_tavern.player_id;
    insert into public.friendships (player_id, friend_id) values (v_tavern.player_id, v_other.player_id), (v_other.player_id, v_tavern.player_id)
    on conflict do nothing;
  else
    insert into public.friend_requests (from_id, to_id) values (v_tavern.player_id, v_other.player_id) on conflict do nothing;
  end if;

  perform private.ring(v_other.player_id, 'refresh', '{}');
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
  delete from public.friend_requests where from_id = p_from and to_id = v_tavern.player_id;
  if not found then
    raise exception 'Cette demande n''existe plus.';
  end if;

  if p_accept then
    insert into public.friendships (player_id, friend_id) values (v_tavern.player_id, p_from), (p_from, v_tavern.player_id)
    on conflict do nothing;
    perform private.ring(p_from, 'refresh', '{}');
  end if;

  return private.state(v_tavern.player_id);
end;
$$;

create or replace function public.remove_friend(p_friend uuid) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_tavern public.taverns;
begin
  v_tavern := private.lock_tavern();
  delete from public.friendships
  where (player_id = v_tavern.player_id and friend_id = p_friend) or (player_id = p_friend and friend_id = v_tavern.player_id);
  return private.state(v_tavern.player_id);
end;
$$;

create or replace function public.set_avatar(p_avatar jsonb) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_tavern public.taverns;
  v_avatar jsonb;
begin
  v_tavern := private.lock_tavern();
  v_avatar := jsonb_build_object(
    'head', least(private.whole(p_avatar -> 'head'), 5),
    'skin', least(private.whole(p_avatar -> 'skin'), 4),
    'hair', least(private.whole(p_avatar -> 'hair'), 5),
    'clothes', least(private.whole(p_avatar -> 'clothes'), 6),
    'accent', least(private.whole(p_avatar -> 'accent'), 4));
  update public.taverns set avatar = v_avatar where player_id = v_tavern.player_id;
  return private.state(v_tavern.player_id);
end;
$$;

create or replace function public.set_specialty(p_base text, p_complement text, p_drink text, p_color integer) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_tavern public.taverns;
begin
  v_tavern := private.lock_tavern();
  if not exists (select 1 from public.specialty_words where kind = 'base' and word = p_base)
    or not exists (select 1 from public.specialty_words where kind = 'complement' and word = p_complement) then
    raise exception 'Choisis le nom de ta spécialité dans les listes.';
  end if;

  if not p_drink = any (private.menu(v_tavern.player_id)) then
    raise exception 'Ta spécialité doit être un plat de ta carte.';
  end if;

  update public.taverns
  set specialty_base = p_base, specialty_complement = p_complement, specialty_drink = p_drink,
      specialty_color = greatest(0, least(coalesce(p_color, 0), 7))
  where player_id = v_tavern.player_id;
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

  if exists (select 1 from public.visits where visitor_id = v_tavern.player_id and served_at is null) then
    raise exception 'Tu es déjà en visite.';
  end if;

  perform 1 from public.taverns where player_id = p_host for update;
  if (select count(*) from public.visits where host_id = p_host and served_at is null) >= v_settings.max_guests then
    raise exception 'La taverne de ton ami est pleine d''invités : réessaie dans un instant.';
  end if;

  insert into public.visits (visitor_id, host_id, stamp) values (v_tavern.player_id, p_host, p_stamp);
  perform private.ring(p_host, 'refresh', jsonb_build_object('guest', v_tavern.name));
  return private.state(v_tavern.player_id);
end;
$$;

create or replace function public.serve_visit(p_visit bigint, p_perfect boolean) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_tavern public.taverns;
  v_visit public.visits;
begin
  v_tavern := private.lock_tavern();
  select * into v_visit from public.visits where id = p_visit and host_id = v_tavern.player_id for update;
  if not found or v_visit.served_at is not null then
    return private.state(v_tavern.player_id);
  end if;

  perform private.serve_visit(v_visit, coalesce(p_perfect, false), false);
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
  where id = p_visit and v_tavern.player_id in (visitor_id, host_id)
    and (served_at is null or served_at > now() - make_interval(secs => v_settings.visit_linger_seconds))
  for update;
  if not found then
    raise exception 'La visite est terminée.';
  end if;

  if v_visit.last_emote_at > now() - make_interval(secs => v_settings.emote_seconds) then
    return '{}';
  end if;

  update public.visits set last_emote_at = now() where id = v_visit.id;
  v_other := case when v_visit.host_id = v_tavern.player_id then v_visit.visitor_id else v_visit.host_id end;
  perform private.ring(v_other, 'emote', jsonb_build_object('visit', v_visit.id, 'from', v_tavern.name, 'emote', p_emote));
  return '{}';
end;
$$;

revoke all on function public.get_state() from public, anon;
revoke all on function public.get_world() from public, anon;
revoke all on function public.request_friend(text) from public, anon;
revoke all on function public.answer_friend(uuid, boolean) from public, anon;
revoke all on function public.remove_friend(uuid) from public, anon;
revoke all on function public.set_avatar(jsonb) from public, anon;
revoke all on function public.set_specialty(text, text, text, integer) from public, anon;
revoke all on function public.start_visit(uuid, text) from public, anon;
revoke all on function public.serve_visit(bigint, boolean) from public, anon;
revoke all on function public.send_emote(bigint, text) from public, anon;
grant execute on function public.get_state() to authenticated;
grant execute on function public.get_world() to authenticated;
grant execute on function public.request_friend(text) to authenticated;
grant execute on function public.answer_friend(uuid, boolean) to authenticated;
grant execute on function public.remove_friend(uuid) to authenticated;
grant execute on function public.set_avatar(jsonb) to authenticated;
grant execute on function public.set_specialty(text, text, text, integer) to authenticated;
grant execute on function public.start_visit(uuid, text) to authenticated;
grant execute on function public.serve_visit(bigint, boolean) to authenticated;
grant execute on function public.send_emote(bigint, text) to authenticated;
