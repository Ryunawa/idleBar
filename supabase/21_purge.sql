do $$
begin
  if to_regclass('realtime.messages') is not null then
    execute 'drop policy if exists "Recevoir sa sonnette" on realtime.messages';
  end if;
end;
$$;

do $$
declare
  v_function regprocedure;
begin
  for v_function in
    select procedure.oid::regprocedure
    from pg_proc procedure
    join pg_namespace namespace on namespace.oid = procedure.pronamespace
    where namespace.nspname in ('public', 'private')
      and procedure.prokind = 'f'
      and procedure.proname = any (array[
        'advance_goals', 'advance_measure', 'advance_regulars', 'are_friends', 'banned', 'chat_key', 'clean_chat',
        'current_visit', 'ensure_goals', 'friends_state', 'frontier_number', 'frontier_plausible', 'frontier_reply',
        'frontier_souls_for', 'has_room_for_friend', 'helper_hourly', 'helper_level', 'is_present', 'lock_tavern',
        'may_stamp', 'menu', 'new_friend_code', 'online', 'owned_values', 'present_visits', 'regulars_state', 'ring',
        'ring_room', 'room_members', 'room_state', 'serve_visit', 'settle_absence', 'settle_visits', 'specialty_name',
        'state', 'stools', 'street_state', 'tavern_name', 'tier_of', 'visible', 'visit_drink', 'visits_state',
        'wants_specialty', 'whole', 'answer_friend', 'answer_invitation', 'buy_upgrade', 'claim_frontier',
        'collect_tip_jar', 'found_tavern', 'get_friend_profile', 'get_requirements', 'get_state', 'get_world',
        'greet_passerby', 'invite_passerby', 'leave_visit', 'mute_player', 'offer_round', 'order_drink',
        'rejoin_frontier', 'remove_friend', 'report_service', 'request_friend', 'save_frontier', 'say', 'send_emote',
        'set_avatar', 'set_specialty', 'set_stamp', 'show_door', 'start_frontier', 'start_visit', 'sync_state'])
  loop
    execute format('drop function if exists %s cascade', v_function);
  end loop;
end;
$$;

drop table if exists
  public.chat_messages, public.chat_mutes, public.friend_requests, public.friendships, public.visits, public.tasted,
  public.invitations, public.rounds, public.greetings, public.stamp_owners, public.tavern_goals, public.tavern_regulars,
  public.tavern_upgrades, public.regular_chapters, public.regulars, public.taverns, public.goal_kinds, public.upgrades,
  public.drinks, public.renown_tiers, public.specialty_words, public.stamps, public.frontiers,
  private.chat_words, private.veterans
cascade;

do $$
declare
  v_column text;
begin
  for v_column in
    select column_name
    from information_schema.columns
    where table_schema = 'private'
      and table_name = 'settings'
      and column_name not in ('id', 'min_client_version', 'frontier_stage_rate', 'frontier_stage_margin',
        'frontier_retreat_minutes', 'frontier_idle_seconds')
  loop
    execute format('alter table private.settings drop column %I', v_column);
  end loop;
end;
$$;

update private.settings set min_client_version = '0.4.1';
