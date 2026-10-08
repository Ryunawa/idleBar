create or replace function private.ensure_goals(p_player_id uuid) returns void
language sql
set search_path = ''
as $$
  insert into public.tavern_goals (player_id, day, goal_id)
  select p_player_id, current_date, kinds.id
  from public.goal_kinds kinds
  where not exists (select 1 from public.tavern_goals where player_id = p_player_id and day = current_date)
    and (kinds.drink is null or kinds.drink = any (private.menu(p_player_id)))
    and (kinds.measure not in ('friend', 'round') or exists (select 1 from public.friendships where player_id = p_player_id))
  order by md5(p_player_id::text || current_date::text || kinds.id)
  limit (select goals_per_day from private.settings)
  on conflict do nothing;
$$;

create or replace function private.street_state(p_player_id uuid) returns jsonb
language sql stable
set search_path = ''
as $$
  select jsonb_build_object(
    'passersby', coalesce((
      select jsonb_agg(jsonb_build_object('id', walker.player_id, 'name', walker.name, 'avatar', walker.avatar))
      from (
        select others.player_id, others.name, others.avatar
        from public.taverns others, private.settings
        where others.player_id <> p_player_id
          and others.last_seen > now() - make_interval(secs => settings.online_seconds)
          and not private.are_friends(p_player_id, others.player_id)
        order by md5(others.player_id::text || p_player_id::text || floor(extract(epoch from now()) / (60 * settings.passersby_minutes))::text)
        limit (select max_passersby from private.settings)) walker), '[]'),
    'invitations', coalesce((
      select jsonb_agg(jsonb_build_object('id', host.player_id, 'name', host.name) order by invitations.created_at)
      from public.invitations
      join public.taverns host on host.player_id = invitations.from_id
      where invitations.to_id = p_player_id), '[]'),
    'rounds', coalesce((
      select jsonb_agg(jsonb_build_object('from', giver.name, 'at', rounds.offered_at) order by rounds.offered_at)
      from public.rounds
      join public.taverns giver on giver.player_id = rounds.from_id
      where rounds.to_id = p_player_id and rounds.offered_at > now() - interval '1 day'), '[]'),
    'round_price', (select round_price from private.settings),
    'round_ready_at', (
      select max(rounds.offered_at) + make_interval(secs => settings.round_cooldown_hours * 3600)
      from public.rounds, private.settings
      where rounds.from_id = p_player_id
      group by settings.round_cooldown_hours));
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
          'drink', taverns.specialty_drink, 'color', taverns.specialty_color, 'name', private.specialty_name(taverns)))
        || private.friends_state(taverns.player_id)
        || private.visits_state(taverns.player_id)
        || private.street_state(taverns.player_id)
      from public.taverns, private.settings
      where taverns.player_id = p_player_id));
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
  if p_player = v_tavern.player_id or not exists (select 1 from public.taverns where player_id = p_player) then
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
  if p_player = v_tavern.player_id or not exists (select 1 from public.taverns where player_id = p_player) then
    raise exception 'Ce passant a disparu au coin de la rue.';
  end if;

  if private.are_friends(v_tavern.player_id, p_player) then
    raise exception 'Vous êtes déjà amis : invite-le depuis l''onglet Amis.';
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
    insert into public.friendships (player_id, friend_id) values (v_tavern.player_id, p_from), (p_from, v_tavern.player_id)
    on conflict do nothing;
    delete from public.friend_requests where (from_id = p_from and to_id = v_tavern.player_id) or (from_id = v_tavern.player_id and to_id = p_from);
    perform private.ring(p_from, 'refresh', '{}');
    if not exists (select 1 from public.visits where visitor_id = v_tavern.player_id and served_at is null) then
      return public.start_visit(p_from, 'mug');
    end if;
  end if;

  return private.state(v_tavern.player_id);
end;
$$;

create or replace function public.offer_round() returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_tavern public.taverns;
  v_settings private.settings;
  v_friend uuid;
  v_ready timestamptz;
begin
  v_tavern := private.lock_tavern();
  select * into v_settings from private.settings;
  if not exists (select 1 from public.friendships where player_id = v_tavern.player_id) then
    raise exception 'Il te faut des amis pour offrir une tournée.';
  end if;

  select max(offered_at) + make_interval(secs => v_settings.round_cooldown_hours * 3600) into v_ready
  from public.rounds where from_id = v_tavern.player_id;
  if v_ready > now() then
    raise exception 'Ta dernière tournée est encore dans toutes les têtes : la prochaine à %.', to_char(v_ready at time zone 'Europe/Paris', 'HH24:MI');
  end if;

  if v_tavern.coins < v_settings.round_price then
    raise exception 'Il te manque % écus.', v_settings.round_price - v_tavern.coins;
  end if;

  update public.taverns
  set coins = coins - v_settings.round_price, renown = renown + v_settings.round_renown
  where player_id = v_tavern.player_id;

  for v_friend in select friend_id from public.friendships where player_id = v_tavern.player_id loop
    insert into public.rounds (from_id, to_id) values (v_tavern.player_id, v_friend);
    update public.taverns set coins = coins + v_settings.round_gift where player_id = v_friend;
    perform private.ring(v_friend, 'refresh', jsonb_build_object('round', v_tavern.name));
  end loop;

  perform private.advance_measure(v_tavern.player_id, 'round', 1);
  return private.state(v_tavern.player_id);
end;
$$;

revoke all on function public.greet_passerby(uuid) from public, anon;
revoke all on function public.invite_passerby(uuid) from public, anon;
revoke all on function public.answer_invitation(uuid, boolean) from public, anon;
revoke all on function public.offer_round() from public, anon;
grant execute on function public.greet_passerby(uuid) to authenticated;
grant execute on function public.invite_passerby(uuid) to authenticated;
grant execute on function public.answer_invitation(uuid, boolean) to authenticated;
grant execute on function public.offer_round() to authenticated;
