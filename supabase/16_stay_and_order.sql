alter table public.visits add column if not exists choice text;
alter table public.visits add column if not exists waiting boolean not null default false;
alter table public.visits add column if not exists ordered_at timestamptz;
alter table private.settings add column if not exists order_seconds integer not null default 5 check (order_seconds >= 0);

create or replace function private.is_present(p_visit public.visits) returns boolean
language sql stable
set search_path = ''
as $$
  select p_visit.left_at is null
    and (p_visit.served_at is null
      or p_visit.present_until is not null
      or p_visit.served_at > now() - make_interval(secs => (select visit_linger_seconds from private.settings)));
$$;

create or replace function private.wants_specialty(p_visit public.visits) returns boolean
language sql immutable
set search_path = ''
as $$
  select p_visit.choice is null or p_visit.choice = 'specialty';
$$;

create or replace function private.visit_drink(p_visit public.visits, p_host public.taverns) returns text
language sql stable
set search_path = ''
as $$
  select case when private.wants_specialty(p_visit) then p_host.specialty_drink else p_visit.choice end;
$$;

create or replace function private.present_visits(p_host uuid) returns setof public.visits
language sql stable
set search_path = ''
as $$
  select visits.* from public.visits
  where visits.host_id = p_host and private.is_present(visits)
  order by visits.id;
$$;

create or replace function private.current_visit(p_player_id uuid) returns public.visits
language sql stable
set search_path = ''
as $$
  select visits.* from public.visits
  where visits.visitor_id = p_player_id and private.is_present(visits)
  order by visits.id desc
  limit 1;
$$;

create or replace function private.visits_state(p_player_id uuid) returns jsonb
language sql stable
set search_path = ''
as $$
  select jsonb_build_object(
    'guests', coalesce((
      select jsonb_agg(jsonb_build_object(
        'visit', visits.id, 'name', visitor.name, 'avatar', visitor.avatar, 'stamp', visits.stamp,
        'drink', private.visit_drink(visits, host), 'started_at', visits.started_at) order by visits.id)
      from public.visits
      join public.taverns visitor on visitor.player_id = visits.visitor_id
      join public.taverns host on host.player_id = visits.host_id
      where visits.host_id = p_player_id and visits.left_at is null and (visits.served_at is null or visits.waiting)), '[]'),
    'outing', (
      select jsonb_build_object(
        'visit', visits.id, 'host', host.name, 'started_at', visits.started_at, 'served_at', visits.served_at,
        'perfect', visits.perfect, 'helped', visits.helped, 'specialty', private.specialty_name(host),
        'waiting', visits.waiting)
      from public.visits
      join public.taverns host on host.player_id = visits.host_id
      where visits.visitor_id = p_player_id and private.is_present(visits)
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
        'drink', private.visit_drink(present, v_host), 'specialty', private.wants_specialty(present),
        'served', present.served_at is not null and not present.waiting, 'started_at', present.started_at,
        'ordered_at', coalesce(present.ordered_at, present.started_at),
        'online', visitor.last_seen > now() - make_interval(secs => v_settings.online_seconds))
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

create or replace function private.serve_visit(p_visit public.visits, p_perfect boolean, p_helped boolean) returns void
language plpgsql
set search_path = ''
as $$
declare
  v_settings private.settings;
  v_host public.taverns;
  v_rewarded boolean;
begin
  if p_visit.served_at is not null then
    update public.visits set waiting = false where id = p_visit.id;
    perform private.ring(p_visit.visitor_id, 'refresh', jsonb_build_object('visit', p_visit.id));
    perform private.ring_room(p_visit.host_id, 'refresh', jsonb_build_object('visit', p_visit.id), p_visit.visitor_id);
    return;
  end if;

  select * into v_settings from private.settings;
  select * into v_host from public.taverns where player_id = p_visit.host_id;
  v_rewarded := not exists (
    select 1 from public.visits
    where visitor_id = p_visit.visitor_id and host_id = p_visit.host_id and rewarded
      and served_at >= date_trunc('day', now()));

  update public.visits
  set served_at = now(), waiting = false, perfect = p_perfect and not p_helped, helped = p_helped, rewarded = v_rewarded
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
      and (visits.served_at is null or visits.waiting)
      and visits.left_at is null
      and coalesce(visits.ordered_at, visits.started_at) < now() - make_interval(secs => settings.visit_patience_seconds)
    order by visits.id
    for update of visits
  loop
    perform private.serve_visit(v_visit, false, true);
  end loop;
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
  if not found or (v_visit.served_at is not null and not v_visit.waiting) then
    return private.state(v_tavern.player_id);
  end if;

  perform private.serve_visit(v_visit, coalesce(p_perfect, false), false);
  return private.state(v_tavern.player_id);
end;
$$;

create or replace function public.order_drink(p_drink text) returns jsonb
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
  if v_visit.id is null then
    raise exception 'Tu n''es chez personne : rends d''abord visite à un ami.';
  end if;

  if p_drink is null or (p_drink <> 'specialty' and not (p_drink = any (private.menu(v_visit.host_id)))) then
    raise exception 'Ce plat n''est pas à la carte de ton hôte.';
  end if;

  if v_visit.ordered_at > now() - make_interval(secs => (select order_seconds from private.settings)) then
    raise exception 'Laisse à ton hôte le temps de servir.';
  end if;

  update public.visits
  set choice = p_drink, waiting = served_at is not null, ordered_at = now()
  where id = v_visit.id;
  perform private.ring_room(v_visit.host_id, 'refresh', jsonb_build_object('order', v_visit.id), v_tavern.player_id);
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
  where id = p_visit and v_tavern.player_id in (visitor_id, host_id) and private.is_present(visits)
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

revoke all on function public.serve_visit(bigint, boolean) from public, anon;
revoke all on function public.order_drink(text) from public, anon;
revoke all on function public.send_emote(bigint, text) from public, anon;
grant execute on function public.serve_visit(bigint, boolean) to authenticated;
grant execute on function public.order_drink(text) to authenticated;
grant execute on function public.send_emote(bigint, text) to authenticated;
