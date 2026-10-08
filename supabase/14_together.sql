alter table private.settings add column if not exists presence_seconds integer not null default 90 check (presence_seconds > 0);
alter table private.settings add column if not exists chat_length integer not null default 120 check (chat_length between 1 and 500);
alter table private.settings add column if not exists chat_seconds integer not null default 2 check (chat_seconds >= 0);
alter table private.settings add column if not exists chat_per_minute integer not null default 12 check (chat_per_minute > 0);
alter table private.settings add column if not exists chat_keep_hours integer not null default 24 check (chat_keep_hours > 0);
alter table private.settings add column if not exists chat_shown integer not null default 30 check (chat_shown > 0);
alter table private.settings add column if not exists visit_hop_seconds integer not null default 10 check (visit_hop_seconds >= 0);
update private.settings set max_guests = 6 where max_guests = 3;

alter table public.taverns add column if not exists stamp text not null default 'heart' references public.stamps (id);
alter table public.visits add column if not exists left_at timestamptz;
alter table public.visits add column if not exists present_until timestamptz;
create index if not exists visits_open_by_host on public.visits (host_id) where left_at is null;
create index if not exists visits_open_by_visitor on public.visits (visitor_id) where left_at is null;

create table if not exists public.chat_messages (
  id bigint generated always as identity primary key,
  room_id uuid not null references public.taverns (player_id) on delete cascade,
  author_id uuid not null references public.taverns (player_id) on delete cascade,
  body text not null,
  said_at timestamptz not null default now()
);

create index if not exists chat_messages_by_room on public.chat_messages (room_id, id desc);
create index if not exists chat_messages_by_author on public.chat_messages (author_id, said_at desc);
create index if not exists chat_messages_by_age on public.chat_messages (said_at);

create table if not exists public.chat_mutes (
  player_id uuid not null references public.taverns (player_id) on delete cascade,
  muted_id uuid not null references public.taverns (player_id) on delete cascade,
  created_at timestamptz not null default now(),
  primary key (player_id, muted_id),
  check (player_id <> muted_id)
);

create table if not exists private.chat_words (
  word text primary key,
  kind text not null check (kind in ('word', 'prefix', 'part'))
);

alter table public.chat_messages enable row level security;
alter table public.chat_mutes enable row level security;
alter table private.chat_words enable row level security;
revoke all on public.chat_messages, public.chat_mutes from anon, authenticated;
revoke all on private.chat_words from anon, authenticated;

insert into private.chat_words (word, kind) values
  ('con', 'word'), ('cons', 'word'), ('conne', 'word'), ('connes', 'word'),
  ('connard', 'prefix'), ('conard', 'prefix'), ('connas', 'prefix'), ('conasse', 'prefix'),
  ('pute', 'word'), ('putes', 'word'), ('putain', 'word'), ('salope', 'prefix'), ('salaud', 'prefix'),
  ('encul', 'prefix'), ('batard', 'prefix'), ('bite', 'word'), ('bites', 'word'), ('couille', 'prefix'),
  ('pd', 'word'), ('pede', 'word'), ('pedes', 'word'), ('tapette', 'prefix'), ('gouine', 'prefix'), ('negre', 'prefix'),
  ('negro', 'prefix'), ('bougnoul', 'prefix'), ('youpin', 'prefix'), ('bicot', 'prefix'), ('fdp', 'word'),
  ('ntm', 'word'), ('niqu', 'prefix'), ('nique', 'prefix'), ('enfoir', 'prefix'), ('tg', 'word'),
  ('tagueule', 'word'), ('poufias', 'prefix'), ('catin', 'prefix'), ('grognass', 'prefix'),
  ('branleur', 'prefix'), ('branlette', 'prefix'), ('suceur', 'prefix'), ('suceuse', 'prefix'), ('nazi', 'prefix'),
  ('hitler', 'prefix'), ('fuck', 'part'), ('shit', 'prefix'), ('bitch', 'prefix'), ('cunt', 'prefix'),
  ('asshole', 'prefix'), ('dick', 'word'), ('dicks', 'word'), ('dickhead', 'prefix'), ('nigger', 'prefix'),
  ('nigga', 'prefix'), ('faggot', 'prefix'), ('fag', 'word'), ('fags', 'word'), ('whore', 'prefix'),
  ('slut', 'prefix'), ('bastard', 'prefix'), ('wanker', 'prefix'), ('retarded', 'prefix')
on conflict (word) do update set kind = excluded.kind;

create or replace function private.chat_key(p_word text) returns text
language sql immutable
set search_path = ''
as $$
  select regexp_replace(
    regexp_replace(
      translate(lower(p_word), 'àâäáãåçéèêëíìîïñóòôöõúùûüýÿœæ0134@$5!7', 'aaaaaaceeeeiiiinooooouuuuyyoaoieaassit'),
      '[^a-z]', '', 'g'),
    '(.)\1{2,}', '\1\1', 'g');
$$;

create or replace function private.clean_chat(p_text text) returns text
language plpgsql stable
set search_path = ''
as $$
declare
  v_token text;
  v_key text;
  v_short text;
  v_words text[] := '{}';
begin
  foreach v_token in array string_to_array(p_text, ' ') loop
    v_key := private.chat_key(v_token);
    v_short := regexp_replace(v_key, '(.)\1+', '\1', 'g');
    if v_key <> '' and exists (
      select 1 from private.chat_words, unnest(array[v_key, v_short]) candidate
      where (kind = 'word' and candidate = word)
         or (kind = 'prefix' and starts_with(candidate, word))
         or (kind = 'part' and position(word in candidate) > 0)) then
      v_token := repeat('*', char_length(v_token));
    end if;

    v_words := v_words || v_token;
  end loop;

  return array_to_string(v_words, ' ');
end;
$$;

create or replace function private.present_visits(p_host uuid) returns setof public.visits
language sql stable
set search_path = ''
as $$
  select visits.* from public.visits, private.settings
  where visits.host_id = p_host
    and visits.left_at is null
    and (visits.served_at is null
      or visits.served_at > now() - make_interval(secs => settings.visit_linger_seconds)
      or visits.present_until > now())
  order by visits.id;
$$;

create or replace function private.current_visit(p_player_id uuid) returns public.visits
language sql stable
set search_path = ''
as $$
  select visits.* from public.visits, private.settings
  where visits.visitor_id = p_player_id
    and visits.left_at is null
    and (visits.served_at is null
      or visits.served_at > now() - make_interval(secs => settings.visit_linger_seconds)
      or visits.present_until > now())
  order by visits.id desc
  limit 1;
$$;

create or replace function private.room_members(p_host uuid) returns setof uuid
language sql stable
set search_path = ''
as $$
  select p_host
  union
  select present.visitor_id from private.present_visits(p_host) present;
$$;

create or replace function private.ring_room(p_host uuid, p_event text, p_payload jsonb, p_except uuid) returns void
language plpgsql
set search_path = ''
as $$
declare
  v_member uuid;
begin
  for v_member in select member from private.room_members(p_host) member where member is distinct from p_except loop
    perform private.ring(v_member, p_event, p_payload);
  end loop;
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
    where visitor_id = p_visit.visitor_id and host_id = p_visit.host_id and rewarded
      and served_at >= date_trunc('day', now()));

  update public.visits
  set served_at = now(), perfect = p_perfect and not p_helped, helped = p_helped, rewarded = v_rewarded
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

  insert into public.tasted (taster_id, host_id, name, drink, color)
  values (p_visit.visitor_id, p_visit.host_id, private.specialty_name(v_host), v_host.specialty_drink, v_host.specialty_color)
  on conflict (taster_id, host_id) do update
  set name = excluded.name, drink = excluded.drink, color = excluded.color, tasted_at = now();

  perform private.ring(p_visit.visitor_id, 'refresh', jsonb_build_object('visit', p_visit.id));
  perform private.ring_room(p_visit.host_id, 'refresh', jsonb_build_object('visit', p_visit.id), p_visit.visitor_id);
end;
$$;

create or replace function private.settle_visits(p_player_id uuid) returns void
language plpgsql
set search_path = ''
as $$
declare
  v_visit public.visits;
begin
  for v_visit in
    select visits.* from public.visits, private.settings
    where (visits.host_id = p_player_id or visits.visitor_id = p_player_id)
      and visits.served_at is null
      and visits.left_at is null
      and visits.started_at < now() - make_interval(secs => settings.visit_patience_seconds)
    order by visits.id
    for update of visits
  loop
    perform private.serve_visit(v_visit, false, true);
  end loop;
end;
$$;

create or replace function private.visits_state(p_player_id uuid) returns jsonb
language sql stable
set search_path = ''
as $$
  select jsonb_build_object(
    'guests', coalesce((
      select jsonb_agg(jsonb_build_object(
        'visit', visits.id, 'name', visitor.name, 'avatar', visitor.avatar, 'stamp', visits.stamp,
        'drink', host.specialty_drink, 'started_at', visits.started_at) order by visits.id)
      from public.visits
      join public.taverns visitor on visitor.player_id = visits.visitor_id
      join public.taverns host on host.player_id = visits.host_id
      where visits.host_id = p_player_id and visits.served_at is null and visits.left_at is null), '[]'),
    'outing', (
      select jsonb_build_object(
        'visit', visits.id, 'host', host.name, 'started_at', visits.started_at, 'served_at', visits.served_at,
        'perfect', visits.perfect, 'helped', visits.helped, 'specialty', private.specialty_name(host))
      from public.visits
      join public.taverns host on host.player_id = visits.host_id, private.settings
      where visits.visitor_id = p_player_id
        and visits.left_at is null
        and (visits.served_at is null
          or visits.served_at > now() - make_interval(secs => settings.visit_linger_seconds)
          or visits.present_until > now())
      order by visits.id desc
      limit 1),
    'guestbook', coalesce((
      select jsonb_agg(jsonb_build_object(
        'name', visitor.name, 'stamp', visitor.stamp, 'at', signed.last_at, 'visits', signed.total) order by signed.last_at desc)
      from (
        select visitor_id, max(started_at) last_at, count(*) total
        from public.visits
        where host_id = p_player_id
        group by visitor_id
        order by max(started_at) desc
        limit 12) signed
      join public.taverns visitor on visitor.player_id = signed.visitor_id), '[]'));
$$;

create or replace function private.room_state(p_player_id uuid) returns jsonb
language plpgsql stable
set search_path = ''
as $$
declare
  v_settings private.settings;
  v_visit public.visits;
  v_host public.taverns;
begin
  select * into v_settings from private.settings;
  v_visit := private.current_visit(p_player_id);
  select * into v_host from public.taverns where player_id = coalesce(v_visit.host_id, p_player_id);
  return jsonb_build_object(
    'host_id', v_host.player_id,
    'host', v_host.name,
    'mine', v_visit.id is null,
    'visit', v_visit.id,
    'host_home', v_host.last_seen > now() - make_interval(secs => v_settings.online_seconds)
      and private.current_visit(v_host.player_id) is null,
    'host_avatar', v_host.avatar,
    'stools', private.stools(v_host.player_id),
    'menu', to_jsonb(private.menu(v_host.player_id)),
    'helper', private.helper_level(v_host.player_id),
    'upgrades', coalesce((
      select jsonb_agg(upgrade_id order by upgrade_id) from public.tavern_upgrades where player_id = v_host.player_id), '[]'),
    'souvenirs', coalesce((
      select jsonb_agg(regular_id order by regular_id) from public.tavern_regulars where player_id = v_host.player_id and chapter >= 5), '[]'),
    'specialty', jsonb_build_object(
      'name', private.specialty_name(v_host), 'drink', v_host.specialty_drink, 'color', v_host.specialty_color),
    'guests', coalesce((
      select jsonb_agg(jsonb_build_object(
        'visit', present.id, 'id', visitor.player_id, 'name', visitor.name, 'avatar', visitor.avatar,
        'drink', v_host.specialty_drink, 'served', present.served_at is not null, 'started_at', present.started_at)
        order by present.id)
      from private.present_visits(v_host.player_id) present
      join public.taverns visitor on visitor.player_id = present.visitor_id), '[]'),
    'chat', coalesce((
      select jsonb_agg(jsonb_build_object(
        'id', recent.id, 'author', recent.author_id, 'name', recent.name, 'text', recent.body, 'at', recent.said_at)
        order by recent.id)
      from (
        select messages.id, messages.author_id, author.name, messages.body, messages.said_at
        from public.chat_messages messages
        join public.taverns author on author.player_id = messages.author_id
        where messages.room_id = v_host.player_id
          and messages.said_at > now() - make_interval(hours => v_settings.chat_keep_hours)
          and messages.said_at >= coalesce(v_visit.started_at, '-infinity'::timestamptz)
          and not exists (
            select 1 from public.chat_mutes where player_id = p_player_id and muted_id = messages.author_id)
        order by messages.id desc
        limit v_settings.chat_shown) recent), '[]'));
end;
$$;

create or replace function private.state(p_player_id uuid) returns jsonb
language sql stable
set search_path = ''
as $$
  select jsonb_build_object(
    'server_time', now(),
    'tavern', (
      select jsonb_build_object(
        'name', taverns.name,
        'friend_code', taverns.friend_code,
        'coins', taverns.coins,
        'renown', taverns.renown,
        'tier', private.tier_of(taverns.renown),
        'served', taverns.served,
        'perfect', taverns.perfect,
        'stools', private.stools(taverns.player_id),
        'menu', to_jsonb(private.menu(taverns.player_id)),
        'helper', private.helper_level(taverns.player_id),
        'upgrades', coalesce((select jsonb_agg(upgrade_id order by upgrade_id) from public.tavern_upgrades where player_id = taverns.player_id), '[]'),
        'tip_jar', jsonb_build_object(
          'amount', taverns.tip_jar,
          'hourly', private.helper_hourly(taverns.player_id),
          'cap', floor(private.helper_hourly(taverns.player_id) * settings.tip_jar_cap_hours)),
        'goals', coalesce((
          select jsonb_agg(jsonb_build_object(
            'id', kinds.id, 'label', kinds.label, 'progress', goals.progress, 'target', kinds.target,
            'reward', kinds.reward, 'done', goals.done_at is not null) order by kinds.reward, kinds.id)
          from public.tavern_goals goals
          join public.goal_kinds kinds on kinds.id = goals.goal_id
          where goals.player_id = taverns.player_id and goals.day = current_date), '[]'),
        'regulars', private.regulars_state(taverns.player_id),
        'avatar', taverns.avatar,
        'specialty', jsonb_build_object(
          'base', taverns.specialty_base, 'complement', taverns.specialty_complement,
          'drink', taverns.specialty_drink, 'color', taverns.specialty_color, 'name', private.specialty_name(taverns)),
        'stamp', taverns.stamp,
        'room', private.room_state(taverns.player_id),
        'muted', coalesce((
          select jsonb_agg(jsonb_build_object('id', muted.player_id, 'name', muted.name) order by muted.name)
          from public.chat_mutes
          join public.taverns muted on muted.player_id = chat_mutes.muted_id
          where chat_mutes.player_id = taverns.player_id), '[]'))
        || private.friends_state(taverns.player_id)
        || private.visits_state(taverns.player_id)
        || private.street_state(taverns.player_id)
      from public.taverns, private.settings
      where taverns.player_id = p_player_id));
$$;

create or replace function public.sync_state(p_present boolean) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_visit public.visits;
begin
  if auth.uid() is not null and coalesce(p_present, false) then
    v_visit := private.current_visit(auth.uid());
    if v_visit.id is not null then
      update public.visits
      set present_until = now() + make_interval(secs => (select presence_seconds from private.settings))
      where id = v_visit.id;
    end if;
  end if;

  return public.get_state();
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

  update public.taverns set stamp = p_stamp where player_id = v_tavern.player_id;

  perform 1 from public.taverns where player_id = p_host for update;
  if (select count(*) from private.present_visits(p_host)) >= v_settings.max_guests then
    raise exception 'La taverne de ton ami est pleine : réessaie dans un instant.';
  end if;

  update public.visits set left_at = now() where visitor_id = v_tavern.player_id and left_at is null;
  if v_previous.id is not null then
    perform private.ring_room(v_previous.host_id, 'refresh', jsonb_build_object('left', v_tavern.name), v_tavern.player_id);
  end if;

  insert into public.visits (visitor_id, host_id, stamp) values (v_tavern.player_id, p_host, p_stamp);
  perform private.ring_room(p_host, 'refresh', jsonb_build_object('guest', v_tavern.name), v_tavern.player_id);
  return private.state(v_tavern.player_id);
end;
$$;

create or replace function public.set_stamp(p_stamp text) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_tavern public.taverns;
begin
  v_tavern := private.lock_tavern();
  if not exists (select 1 from public.stamps where id = p_stamp) then
    raise exception 'Ce tampon n''existe pas.';
  end if;

  if not private.may_stamp(v_tavern.player_id, p_stamp) then
    raise exception 'Ce tampon appartient à quelqu''un d''autre.';
  end if;

  update public.taverns set stamp = p_stamp where player_id = v_tavern.player_id;
  return private.state(v_tavern.player_id);
end;
$$;

create or replace function public.leave_visit() returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_tavern public.taverns;
  v_visit public.visits;
begin
  v_tavern := private.lock_tavern();
  v_visit := private.current_visit(v_tavern.player_id);
  update public.visits set left_at = now() where visitor_id = v_tavern.player_id and left_at is null;
  if v_visit.id is not null then
    perform private.ring_room(v_visit.host_id, 'refresh', jsonb_build_object('left', v_tavern.name), v_tavern.player_id);
  end if;

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
  update public.visits set left_at = now()
  where id = p_visit and host_id = v_tavern.player_id and left_at is null
  returning * into v_visit;
  if v_visit.id is not null then
    perform private.ring(v_visit.visitor_id, 'refresh', jsonb_build_object('door', v_tavern.name));
    perform private.ring_room(v_tavern.player_id, 'refresh', jsonb_build_object('left', v_visit.id), v_tavern.player_id);
  end if;

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
  select * into v_visit from public.visits
  where id = p_visit and host_id = v_tavern.player_id and left_at is null
  for update;
  if not found or v_visit.served_at is not null then
    return private.state(v_tavern.player_id);
  end if;

  perform private.serve_visit(v_visit, coalesce(p_perfect, false), false);
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
  v_visit public.visits;
begin
  v_tavern := private.lock_tavern();
  delete from public.friendships
  where (player_id = v_tavern.player_id and friend_id = p_friend) or (player_id = p_friend and friend_id = v_tavern.player_id);
  for v_visit in
    update public.visits set left_at = now()
    where left_at is null
      and ((visitor_id = v_tavern.player_id and host_id = p_friend) or (visitor_id = p_friend and host_id = v_tavern.player_id))
    returning *
  loop
    perform private.ring(v_visit.visitor_id, 'refresh', '{}');
    perform private.ring_room(v_visit.host_id, 'refresh', '{}', v_visit.visitor_id);
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
  where id = p_visit and v_tavern.player_id in (visitor_id, host_id)
    and left_at is null
    and (served_at is null
      or served_at > now() - make_interval(secs => v_settings.visit_linger_seconds)
      or present_until > now())
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
  v_text := btrim(regexp_replace(regexp_replace(coalesce(p_text, ''), '[[:cntrl:]]', ' ', 'g'), '[[:space:]]+', ' ', 'g'));
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

create or replace function public.mute_player(p_player uuid, p_muted boolean) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_tavern public.taverns;
begin
  v_tavern := private.lock_tavern();
  if p_player = v_tavern.player_id then
    raise exception 'Tu ne peux pas te masquer toi-même.';
  end if;

  if not exists (select 1 from public.taverns where player_id = p_player) then
    raise exception 'Ce joueur a fermé sa taverne.';
  end if;

  if coalesce(p_muted, true) then
    insert into public.chat_mutes (player_id, muted_id) values (v_tavern.player_id, p_player) on conflict do nothing;
  else
    delete from public.chat_mutes where player_id = v_tavern.player_id and muted_id = p_player;
  end if;

  return private.state(v_tavern.player_id);
end;
$$;

create or replace function public.get_friend_profile(p_friend uuid) returns jsonb
language plpgsql stable
security definer
set search_path = ''
as $$
declare
  v_friend public.taverns;
  v_settings private.settings;
begin
  if auth.uid() is null then
    raise exception 'Connecte-toi pour jouer.';
  end if;

  if not private.are_friends(auth.uid(), p_friend) then
    raise exception 'Seuls tes amis ont une fiche.';
  end if;

  select * into v_settings from private.settings;
  select * into v_friend from public.taverns where player_id = p_friend;
  return jsonb_build_object(
    'id', v_friend.player_id,
    'name', v_friend.name,
    'avatar', v_friend.avatar,
    'tier', private.tier_of(v_friend.renown),
    'served', v_friend.served,
    'online', v_friend.last_seen > now() - make_interval(secs => v_settings.online_seconds),
    'friends_since', (select created_at from public.friendships where player_id = auth.uid() and friend_id = p_friend),
    'muted', exists (select 1 from public.chat_mutes where player_id = auth.uid() and muted_id = p_friend),
    'specialty', jsonb_build_object(
      'name', private.specialty_name(v_friend), 'drink', v_friend.specialty_drink, 'color', v_friend.specialty_color),
    'souvenirs', (select count(*) from public.tavern_regulars where player_id = p_friend and chapter >= 5),
    'stamp', v_friend.stamp,
    'guestbook', coalesce((
      select jsonb_agg(jsonb_build_object(
        'name', visitor.name, 'stamp', visitor.stamp, 'at', signed.last_at, 'visits', signed.total) order by signed.last_at desc)
      from (
        select visitor_id, max(started_at) last_at, count(*) total
        from public.visits
        where host_id = p_friend
        group by visitor_id
        order by max(started_at) desc
        limit 12) signed
      join public.taverns visitor on visitor.player_id = signed.visitor_id), '[]'),
    'tasted', coalesce((
      select jsonb_agg(jsonb_build_object('name', tasted.name, 'drink', tasted.drink, 'color', tasted.color) order by tasted.tasted_at desc)
      from public.tasted where taster_id = p_friend), '[]'));
end;
$$;

revoke all on function public.sync_state(boolean) from public, anon;
revoke all on function public.start_visit(uuid, text) from public, anon;
revoke all on function public.leave_visit() from public, anon;
revoke all on function public.set_stamp(text) from public, anon;
revoke all on function public.send_emote(bigint, text) from public, anon;
revoke all on function public.say(text) from public, anon;
revoke all on function public.mute_player(uuid, boolean) from public, anon;
revoke all on function public.get_friend_profile(uuid) from public, anon;
revoke all on function public.show_door(bigint) from public, anon;
revoke all on function public.serve_visit(bigint, boolean) from public, anon;
revoke all on function public.remove_friend(uuid) from public, anon;
grant execute on function public.show_door(bigint) to authenticated;
grant execute on function public.serve_visit(bigint, boolean) to authenticated;
grant execute on function public.remove_friend(uuid) to authenticated;
grant execute on function public.sync_state(boolean) to authenticated;
grant execute on function public.start_visit(uuid, text) to authenticated;
grant execute on function public.leave_visit() to authenticated;
grant execute on function public.set_stamp(text) to authenticated;
grant execute on function public.send_emote(bigint, text) to authenticated;
grant execute on function public.say(text) to authenticated;
grant execute on function public.mute_player(uuid, boolean) to authenticated;
grant execute on function public.get_friend_profile(uuid) to authenticated;
