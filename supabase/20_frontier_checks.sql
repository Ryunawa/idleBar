alter table private.settings add column if not exists frontier_retreat_minutes numeric not null default 3
  check (frontier_retreat_minutes > 0);
alter table private.settings add column if not exists frontier_idle_seconds integer not null default 120
  check (frontier_idle_seconds > 0);

create or replace function private.frontier_plausible(p_row public.frontiers, p_save jsonb) returns boolean
language plpgsql stable
set search_path = ''
as $$
declare
  v_settings private.settings;
  v_best numeric := greatest(
    private.frontier_number(p_save, 'best_stage'),
    private.frontier_number(p_save, 'max_stage'),
    private.frontier_number(p_save, 'stage'));
  v_souls numeric := private.frontier_number(p_save, 'souls');
  v_retreats numeric := private.frontier_number(p_save, 'retreats');
  v_minutes numeric := greatest(0, extract(epoch from now() - p_row.saved_at) / 60);
begin
  select * into v_settings from private.settings;
  return v_best <= p_row.max_stage + v_settings.frontier_stage_margin + v_minutes * v_settings.frontier_stage_rate
    and v_retreats >= p_row.retreats
    and v_retreats - p_row.retreats <= 1 + v_minutes / v_settings.frontier_retreat_minutes
    and v_souls >= p_row.souls
    and v_souls - p_row.souls <= (v_retreats - p_row.retreats) * private.frontier_souls_for(v_best);
end;
$$;

create or replace function public.start_frontier(p_session uuid) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player uuid := auth.uid();
  v_snapshot integer;
  v_snapshot_name text;
  v_live integer;
  v_live_name text;
  v_gift integer;
begin
  if v_player is null then
    raise exception 'Connecte-toi pour prendre la route.';
  end if;

  select tier, tavern_name into v_snapshot, v_snapshot_name from private.veterans where player_id = v_player;
  select private.tier_of(renown), name into v_live, v_live_name from public.taverns where player_id = v_player;
  select coalesce(frontier_veteran_souls[greatest(v_snapshot, v_live) + 1], 0) into v_gift from private.settings;
  insert into public.frontiers (player_id, session_id, souls, veteran_souls, veteran_name)
  values (v_player, p_session, coalesce(v_gift, 0), coalesce(v_gift, 0), coalesce(v_live_name, v_snapshot_name))
  on conflict (player_id) do update set session_id = excluded.session_id, last_seen = now();
  return private.frontier_reply(v_player, 'ok');
end;
$$;

create or replace function public.rejoin_frontier(p_session uuid) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_idle integer;
begin
  select frontier_idle_seconds into v_idle from private.settings;
  update public.frontiers
  set session_id = p_session, last_seen = now()
  where player_id = auth.uid()
    and (session_id is null or session_id = p_session or last_seen < now() - make_interval(secs => v_idle));
  if not found and exists (select 1 from public.frontiers where player_id = auth.uid()) then
    return private.frontier_reply(auth.uid(), 'elsewhere');
  end if;

  return private.frontier_reply(auth.uid(), 'ok');
end;
$$;

revoke all on function public.rejoin_frontier(uuid) from public, anon;
grant execute on function public.rejoin_frontier(uuid) to authenticated;
