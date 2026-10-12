create or replace function private.frontier_reply(p_player_id uuid, p_status text) returns jsonb
language sql stable
set search_path = ''
as $$
  select jsonb_build_object(
    'status', p_status,
    'server_time', now(),
    'frontier', (
      select jsonb_build_object(
        'save', frontier.save,
        'revision', frontier.revision,
        'saved_at', frontier.saved_at,
        'away_seconds', greatest(0, extract(epoch from now() - frontier.saved_at)),
        'veteran_souls', frontier.veteran_souls,
        'veteran_name', frontier.veteran_name)
      from public.frontiers frontier
      where frontier.player_id = p_player_id));
$$;

create or replace function private.frontier_number(p_save jsonb, p_key text) returns numeric
language sql immutable
set search_path = ''
as $$
  select case when jsonb_typeof(p_save -> p_key) = 'number' then (p_save ->> p_key)::numeric else 0 end;
$$;

create or replace function private.frontier_souls_for(p_stage numeric) returns numeric
language sql immutable
set search_path = ''
as $$
  select case when p_stage < 50 then 0 else floor(power(p_stage / 10, 2)) end;
$$;

create or replace function private.frontier_plausible(p_row public.frontiers, p_save jsonb) returns boolean
language plpgsql stable
set search_path = ''
as $$
declare
  v_settings private.settings;
  v_best numeric := greatest(private.frontier_number(p_save, 'best_stage'), private.frontier_number(p_save, 'max_stage'));
  v_souls numeric := private.frontier_number(p_save, 'souls');
  v_retreats numeric := private.frontier_number(p_save, 'retreats');
  v_minutes numeric := greatest(0, extract(epoch from now() - p_row.saved_at) / 60);
begin
  select * into v_settings from private.settings;
  return v_best <= p_row.max_stage + v_settings.frontier_stage_margin + v_minutes * v_settings.frontier_stage_rate
    and v_retreats >= p_row.retreats
    and v_souls >= p_row.souls
    and v_souls <= p_row.veteran_souls + v_retreats * private.frontier_souls_for(v_best);
end;
$$;

create or replace function public.start_frontier(p_session uuid) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player uuid := auth.uid();
  v_tier integer;
  v_name text;
  v_gift integer;
begin
  if v_player is null then
    raise exception 'Connecte-toi pour prendre la route.';
  end if;

  select tier, tavern_name into v_tier, v_name from private.veterans where player_id = v_player;
  if not found then
    select private.tier_of(renown), name into v_tier, v_name from public.taverns where player_id = v_player;
  end if;

  select coalesce(frontier_veteran_souls[v_tier + 1], 0) into v_gift from private.settings;
  insert into public.frontiers (player_id, session_id, souls, veteran_souls, veteran_name)
  values (v_player, p_session, coalesce(v_gift, 0), coalesce(v_gift, 0), v_name)
  on conflict (player_id) do update set session_id = excluded.session_id, last_seen = now();
  return private.frontier_reply(v_player, 'ok');
end;
$$;

create or replace function public.claim_frontier(p_session uuid) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
begin
  update public.frontiers set session_id = p_session, last_seen = now() where player_id = auth.uid();
  return private.frontier_reply(auth.uid(), 'ok');
end;
$$;

create or replace function public.save_frontier(p_save jsonb, p_revision integer, p_session uuid) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_row public.frontiers;
  v_limit integer;
begin
  select * into v_row from public.frontiers where player_id = auth.uid() for update;
  if not found then
    return private.frontier_reply(auth.uid(), 'missing');
  end if;

  if v_row.session_id is distinct from p_session then
    return private.frontier_reply(auth.uid(), 'elsewhere');
  end if;

  if v_row.revision <> p_revision then
    return private.frontier_reply(auth.uid(), 'conflict');
  end if;

  select frontier_save_max_bytes into v_limit from private.settings;
  if p_save is null or jsonb_typeof(p_save) <> 'object' or octet_length(p_save::text) > v_limit
    or not private.frontier_plausible(v_row, p_save) then
    return private.frontier_reply(auth.uid(), 'refused');
  end if;

  update public.frontiers
  set save = p_save,
      revision = revision + 1,
      max_stage = greatest(max_stage, greatest(private.frontier_number(p_save, 'best_stage'), private.frontier_number(p_save, 'max_stage'))::integer),
      souls = private.frontier_number(p_save, 'souls')::bigint,
      retreats = private.frontier_number(p_save, 'retreats')::integer,
      saved_at = now(),
      last_seen = now()
  where player_id = auth.uid();
  return private.frontier_reply(auth.uid(), 'ok');
end;
$$;

revoke all on function public.start_frontier(uuid) from public, anon;
revoke all on function public.claim_frontier(uuid) from public, anon;
revoke all on function public.save_frontier(jsonb, integer, uuid) from public, anon;
grant execute on function public.start_frontier(uuid) to authenticated;
grant execute on function public.claim_frontier(uuid) to authenticated;
grant execute on function public.save_frontier(jsonb, integer, uuid) to authenticated;
