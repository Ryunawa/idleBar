alter table private.settings add column if not exists invite_cooldown_minutes integer not null default 10 check (invite_cooldown_minutes >= 0);

update public.renown_tiers set renown = case tier
  when 1 then 600
  when 2 then 3000
  when 3 then 12000
  when 4 then 40000
  else renown
end;

update public.upgrades set price = prices.price
from (values
  ('tabouret-5', 240), ('tabouret-6', 600), ('tabouret-7', 1400), ('tabouret-8', 3000), ('tabouret-9', 6000), ('tabouret-10', 12000),
  ('marmite', 800), ('pressoir', 3000), ('four', 8000),
  ('apprenti', 400), ('commis', 4000), ('serveuse', 16000),
  ('deco-plantes', 160), ('deco-suspensions', 300), ('deco-affiches', 300), ('deco-tableaux', 500), ('deco-cible', 600),
  ('deco-bannieres', 800), ('deco-chat', 1000), ('deco-horloge', 1000), ('deco-trophees', 1200), ('deco-cheminee', 1800)
) prices (id, price)
where upgrades.id = prices.id;

create index if not exists friend_requests_by_target on public.friend_requests (to_id);
create index if not exists invitations_by_target on public.invitations (to_id);

create or replace function private.online(p_player_id uuid) returns boolean
language sql stable
set search_path = ''
as $$
  select exists (
    select 1 from public.taverns, private.settings
    where taverns.player_id = p_player_id and taverns.last_seen > now() - make_interval(secs => settings.online_seconds));
$$;

create or replace function private.has_room_for_friend(p_player_id uuid) returns boolean
language sql stable
set search_path = ''
as $$
  select (select count(*) from public.friendships where player_id = p_player_id) < (select max_friends from private.settings);
$$;

create or replace function private.tavern_name(p_name text) returns text
language plpgsql stable
set search_path = ''
as $$
declare
  v_name text;
begin
  v_name := regexp_replace(coalesce(p_name, ''), '[[:cntrl:]­​-‏‪-‮⁠-⁤﻿]', '', 'g');
  v_name := btrim(regexp_replace(v_name, '[[:space:]]+', ' ', 'g'));
  if char_length(v_name) not between 2 and 24 then
    raise exception 'Le nom de la taverne doit faire entre 2 et 24 caractères.';
  end if;

  if private.clean_chat(v_name) <> v_name then
    raise exception 'Choisis un nom de taverne plus aimable.';
  end if;

  return v_name;
end;
$$;

create or replace function public.found_tavern(p_name text) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_name text;
begin
  if auth.uid() is null then
    raise exception 'Connecte-toi pour jouer.';
  end if;

  v_name := private.tavern_name(p_name);
  if exists (select 1 from public.taverns where player_id = auth.uid()) then
    raise exception 'Ta taverne est déjà ouverte.';
  end if;

  insert into public.taverns (player_id, name, friend_code) values (auth.uid(), v_name, private.new_friend_code());
  perform private.ensure_goals(auth.uid());
  return private.state(auth.uid());
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

create or replace function public.greet_passerby(p_player uuid) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_tavern public.taverns;
begin
  v_tavern := private.lock_tavern();
  if p_player = v_tavern.player_id or not private.online(p_player) then
    raise exception 'Ce passant a disparu au coin de la rue.';
  end if;

  if exists (
    select 1 from public.greetings, private.settings
    where from_id = v_tavern.player_id and to_id = p_player and greeted_at > now() - make_interval(secs => settings.greet_seconds)) then
    return '{}';
  end if;

  insert into public.greetings (from_id, to_id) values (v_tavern.player_id, p_player)
  on conflict (from_id, to_id) do update set greeted_at = now();
  perform private.ring(p_player, 'wave', jsonb_build_object('from', v_tavern.name));
  return '{}';
end;
$$;

create or replace function public.invite_passerby(p_player uuid) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_tavern public.taverns;
begin
  v_tavern := private.lock_tavern();
  if p_player = v_tavern.player_id or not private.online(p_player) then
    raise exception 'Ce passant a disparu au coin de la rue.';
  end if;

  if private.are_friends(v_tavern.player_id, p_player) then
    raise exception 'Vous êtes déjà amis : invite-le depuis l''onglet Amis.';
  end if;

  if exists (
    select 1 from public.invitations, private.settings
    where from_id = v_tavern.player_id and to_id = p_player
      and created_at > now() - make_interval(mins => settings.invite_cooldown_minutes)) then
    return private.state(v_tavern.player_id);
  end if;

  insert into public.invitations (from_id, to_id) values (v_tavern.player_id, p_player)
  on conflict (from_id, to_id) do update set created_at = now();
  perform private.ring(p_player, 'refresh', '{}');
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
      return public.start_visit(p_from, 'mug');
    end if;
  end if;

  return private.state(v_tavern.player_id);
end;
$$;

revoke all on function public.found_tavern(text) from public, anon;
revoke all on function public.report_service(jsonb) from public, anon;
revoke all on function public.request_friend(text) from public, anon;
revoke all on function public.answer_friend(uuid, boolean) from public, anon;
revoke all on function public.greet_passerby(uuid) from public, anon;
revoke all on function public.invite_passerby(uuid) from public, anon;
revoke all on function public.answer_invitation(uuid, boolean) from public, anon;
grant execute on function public.found_tavern(text) to authenticated;
grant execute on function public.report_service(jsonb) to authenticated;
grant execute on function public.request_friend(text) to authenticated;
grant execute on function public.answer_friend(uuid, boolean) to authenticated;
grant execute on function public.greet_passerby(uuid) to authenticated;
grant execute on function public.invite_passerby(uuid) to authenticated;
grant execute on function public.answer_invitation(uuid, boolean) to authenticated;
