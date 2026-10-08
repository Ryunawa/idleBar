create or replace function private.ring(p_player_id uuid, p_event text, p_payload jsonb) returns void
language plpgsql
set search_path = ''
as $$
begin
  if to_regprocedure('realtime.send(jsonb, text, text, boolean)') is not null then
    execute 'select realtime.send($1, $2, $3, true)' using p_payload, p_event, 'taverne:' || p_player_id::text;
  end if;
exception
  when others then
    raise warning 'Sonnette impossible pour % : %', p_player_id, sqlerrm;
end;
$$;

create or replace function private.are_friends(p_player_id uuid, p_other_id uuid) returns boolean
language sql stable
set search_path = ''
as $$
  select exists (select 1 from public.friendships where player_id = p_player_id and friend_id = p_other_id);
$$;

create or replace function private.ensure_goals(p_player_id uuid) returns void
language sql
set search_path = ''
as $$
  insert into public.tavern_goals (player_id, day, goal_id)
  select p_player_id, current_date, kinds.id
  from public.goal_kinds kinds
  where not exists (select 1 from public.tavern_goals where player_id = p_player_id and day = current_date)
    and (kinds.drink is null or kinds.drink = any (private.menu(p_player_id)))
    and (kinds.measure <> 'friend' or exists (select 1 from public.friendships where player_id = p_player_id))
  order by md5(p_player_id::text || current_date::text || kinds.id)
  limit (select goals_per_day from private.settings)
  on conflict do nothing;
$$;

create or replace function private.advance_measure(p_player_id uuid, p_measure text, p_amount integer) returns integer
language plpgsql
set search_path = ''
as $$
declare
  v_reward integer;
begin
  update public.tavern_goals goals
  set progress = least(kinds.target, goals.progress + p_amount)
  from public.goal_kinds kinds
  where goals.player_id = p_player_id and goals.day = current_date and goals.goal_id = kinds.id
    and goals.done_at is null and kinds.measure = p_measure;

  with finished as (
    update public.tavern_goals goals
    set done_at = now()
    from public.goal_kinds kinds
    where goals.player_id = p_player_id and goals.day = current_date and goals.goal_id = kinds.id
      and goals.done_at is null and goals.progress >= kinds.target
    returning kinds.reward
  )
  select coalesce(sum(reward), 0) into v_reward from finished;

  update public.taverns set coins = coins + v_reward where player_id = p_player_id;
  return v_reward;
end;
$$;

create or replace function private.specialty_name(p_tavern public.taverns) returns text
language sql immutable
set search_path = ''
as $$
  select p_tavern.specialty_base || ' ' || p_tavern.specialty_complement;
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
  perform private.ring(p_visit.host_id, 'refresh', jsonb_build_object('visit', p_visit.id));
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
      and visits.started_at < now() - make_interval(secs => settings.visit_patience_seconds)
    order by visits.id
    for update of visits
  loop
    perform private.serve_visit(v_visit, false, true);
  end loop;
end;
$$;

create or replace function private.friends_state(p_player_id uuid) returns jsonb
language sql stable
set search_path = ''
as $$
  select jsonb_build_object(
    'friends', coalesce((
      select jsonb_agg(jsonb_build_object(
        'id', friend.player_id, 'name', friend.name, 'avatar', friend.avatar,
        'online', friend.last_seen > now() - make_interval(secs => settings.online_seconds),
        'specialty', private.specialty_name(friend)) order by friend.name)
      from public.friendships
      join public.taverns friend on friend.player_id = friendships.friend_id, private.settings
      where friendships.player_id = p_player_id), '[]'),
    'requests', coalesce((
      select jsonb_agg(jsonb_build_object('id', asker.player_id, 'name', asker.name) order by requests.created_at)
      from public.friend_requests requests
      join public.taverns asker on asker.player_id = requests.from_id
      where requests.to_id = p_player_id), '[]'),
    'tasted', coalesce((
      select jsonb_agg(jsonb_build_object('host', host.name, 'name', tasted.name, 'drink', tasted.drink, 'color', tasted.color) order by tasted.tasted_at desc)
      from public.tasted
      join public.taverns host on host.player_id = tasted.host_id
      where tasted.taster_id = p_player_id), '[]'));
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
      where visits.host_id = p_player_id and visits.served_at is null), '[]'),
    'outing', (
      select jsonb_build_object(
        'visit', visits.id, 'host', host.name, 'started_at', visits.started_at, 'served_at', visits.served_at,
        'perfect', visits.perfect, 'helped', visits.helped, 'specialty', private.specialty_name(host))
      from public.visits
      join public.taverns host on host.player_id = visits.host_id, private.settings
      where visits.visitor_id = p_player_id
        and (visits.served_at is null or visits.served_at > now() - make_interval(secs => settings.visit_linger_seconds))
      order by visits.id desc
      limit 1),
    'guestbook', coalesce((
      select jsonb_agg(entry order by entry ->> 'at' desc)
      from (
        select jsonb_build_object('name', visitor.name, 'stamp', visits.stamp, 'at', visits.started_at) entry
        from public.visits
        join public.taverns visitor on visitor.player_id = visits.visitor_id
        where visits.host_id = p_player_id
        order by visits.id desc
        limit 12) recent), '[]'));
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
      from public.taverns, private.settings
      where taverns.player_id = p_player_id));
$$;
