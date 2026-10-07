-- Petits services : chaque joueur, quel que soit son métier, rend de menus services
-- entre deux tâches. Les écus s'accumulent dans une bourse, plafonnée, qu'il vide d'un clic.
-- À exécuter après 16_reputation.sql.

alter table private.settings add column if not exists odd_jobs_hourly integer not null default 10 check (odd_jobs_hourly >= 0);
alter table private.settings add column if not exists odd_jobs_rank_step integer not null default 5 check (odd_jobs_rank_step >= 0);
alter table private.settings add column if not exists odd_jobs_cap_hours numeric not null default 8 check (odd_jobs_cap_hours > 0);

alter table public.players add column if not exists odd_jobs_since timestamptz not null default now();

create or replace function private.odd_jobs_rate(p_player_id uuid) returns integer
language sql stable
set search_path = ''
as $$
  select settings.odd_jobs_hourly + settings.odd_jobs_rank_step * private.rank_level(private.rank_of(private.player_xp(p_player_id)))
  from private.settings;
$$;

create or replace function private.odd_jobs_hours(p_player_id uuid) returns numeric
language sql stable
set search_path = ''
as $$
  select least(greatest(extract(epoch from now() - players.odd_jobs_since), 0) / 3600, settings.odd_jobs_cap_hours)
  from public.players, private.settings
  where players.player_id = p_player_id;
$$;

create or replace function private.odd_jobs_earned(p_player_id uuid) returns integer
language sql stable
set search_path = ''
as $$
  select floor(private.odd_jobs_hours(p_player_id) * private.odd_jobs_rate(p_player_id))::integer;
$$;

create or replace function private.odd_jobs_state(p_player_id uuid) returns jsonb
language sql stable
set search_path = ''
as $$
  select jsonb_build_object('odd_jobs', (
    select jsonb_build_object(
      'since', players.odd_jobs_since,
      'hourly', private.odd_jobs_rate(players.player_id),
      'cap_hours', settings.odd_jobs_cap_hours,
      'earned', private.odd_jobs_earned(players.player_id)
    )
    from public.players, private.settings
    where players.player_id = p_player_id
  ));
$$;

create or replace function public.collect_odd_jobs() returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player public.players;
  v_earned integer;
  v_capped boolean;
begin
  if auth.uid() is null then
    raise exception 'Connexion requise.';
  end if;

  select * into v_player from public.players where player_id = auth.uid() for update;
  if not found then
    raise exception 'Installe-toi d''abord dans une ville.';
  end if;

  v_earned := private.odd_jobs_earned(v_player.player_id);
  if v_earned < 1 then
    raise exception 'Ta bourse est encore vide : repasse un peu plus tard.';
  end if;

  v_capped := private.odd_jobs_hours(v_player.player_id) >= (select odd_jobs_cap_hours from private.settings);
  update public.players
  set coins = coins + v_earned,
      odd_jobs_since = case
        when v_capped then now()
        else odd_jobs_since + make_interval(secs => v_earned * 3600.0 / private.odd_jobs_rate(v_player.player_id))
      end
  where player_id = v_player.player_id;
  return public.get_state();
end;
$$;
create or replace function public.get_state() returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player public.players;
  v_caravan public.caravans;
  v_workshop public.workshops;
  v_settings private.settings;
  v_present text[];
  v_branches integer;
  v_haggle numeric;
begin
  if auth.uid() is null then
    raise exception 'Connexion requise.';
  end if;

  perform private.settle_player(auth.uid());
  select * into v_player from public.players where player_id = auth.uid();
  if not found then
    return jsonb_build_object(
      'server_time', now(), 'player', null, 'caravan', null, 'cargo', '[]'::jsonb,
      'workshop', null, 'warehouses', '[]'::jsonb, 'branches', '[]'::jsonb, 'market', '[]'::jsonb, 'trip_event', null
    ) || private.exchange_state(null, array[]::text[]) || private.events_state(null) || private.mastery_state(null)
      || private.reputation_state(null) || private.odd_jobs_state(null);
  end if;

  if exists (select 1 from public.workshops where player_id = v_player.player_id) then
    v_workshop := private.lock_workshop();
    select * into v_player from public.players where player_id = v_player.player_id;
  end if;

  select * into v_caravan from public.caravans where player_id = v_player.player_id;
  select * into v_settings from private.settings;
  select count(*) into v_branches from public.branches where player_id = v_player.player_id;
  v_present := private.present_towns(v_player.player_id);
  v_haggle := private.bonus(v_player.player_id, 'haggle');

  return jsonb_build_object(
    'server_time', now(),
    'player', jsonb_build_object(
      'name', v_player.name,
      'craft_id', v_player.craft_id,
      'home_town_id', v_player.home_town_id,
      'coins', v_player.coins,
      'next_branch_price', case
        when (select opens_branches from public.crafts where id = v_player.craft_id)
          and v_branches < v_settings.max_branches + private.bonus(v_player.player_id, 'network')
        then private.branch_price(v_branches + 1)
      end
    ),
    'caravan', case when v_caravan.player_id is null then null else jsonb_build_object(
      'wagons', v_caravan.wagons,
      'capacity', private.caravan_capacity(v_caravan.player_id),
      'load', private.cargo_load(v_caravan.player_id),
      'next_wagon_price', case when v_caravan.wagons < v_settings.max_wagons then private.wagon_price(v_caravan.wagons + 1) end,
      'town_id', v_caravan.town_id,
      'from_town_id', v_caravan.from_town_id,
      'departed_at', v_caravan.departed_at,
      'arrives_at', v_caravan.arrives_at,
      'directive_id', v_caravan.directive_id
    ) end,
    'cargo', (
      select coalesce(jsonb_agg(jsonb_build_object('good_id', cargo.good_id, 'quantity', cargo.quantity) order by goods.sort_order), '[]'::jsonb)
      from public.cargo
      join public.goods on goods.id = cargo.good_id
      where cargo.player_id = v_player.player_id
    ),
    'workshop', case when v_workshop.player_id is null then null else jsonb_build_object(
      'town_id', v_workshop.town_id,
      'level', v_workshop.level,
      'speed', private.workshop_speed(v_workshop.level) * (1 + private.bonus(v_player.player_id, 'speed')),
      'storage_capacity', private.warehouse_capacity(v_player.player_id),
      'max_queue', private.max_queue(v_workshop.level),
      'next_level_price', case when v_workshop.level < v_settings.max_workshop_level then private.workshop_price(v_workshop.level + 1) end,
      'recipe_id', v_workshop.recipe_id,
      'queued', v_workshop.queued,
      'batch_seconds', v_workshop.batch_seconds,
      'started_at', v_workshop.started_at,
      'paused_until', v_workshop.paused_until
    ) end,
    'warehouses', (
      select coalesce(jsonb_agg(jsonb_build_object(
        'town_id', warehouses.town_id, 'good_id', warehouses.good_id, 'quantity', warehouses.quantity
      ) order by warehouses.town_id, goods.sort_order), '[]'::jsonb)
      from public.warehouses
      join public.goods on goods.id = warehouses.good_id
      where warehouses.player_id = v_player.player_id
    ),
    'trip_event', (
      select jsonb_build_object(
        'kind_id', kind_id,
        'occurred_at', occurred_at,
        'decide_by', decide_by,
        'directive_choice_id', private.directive_choice(player_id, kind_id)
      )
      from public.trip_events where player_id = v_player.player_id
    ),
    'branches', (
      select coalesce(jsonb_agg(branches.town_id order by branches.opened_at, branches.town_id), '[]'::jsonb)
      from public.branches
      where branches.player_id = v_player.player_id
    ),
    'market', (
      select coalesce(jsonb_agg(jsonb_build_object(
        'town_id', markets.town_id,
        'good_id', markets.good_id,
        'buy_price', round(private.buy_quote(markets.base_price, private.current_pressure(markets.pressure, markets.pressure_at)) * (1 - v_haggle - private.town_rate(v_player.player_id, markets.town_id)), 1),
        'sell_price', round(private.sell_quote(markets.base_price, private.current_pressure(markets.pressure, markets.pressure_at)) * (1 + v_haggle + private.town_rate(v_player.player_id, markets.town_id)), 1)
      ) order by markets.town_id, goods.sort_order), '[]'::jsonb)
      from public.markets
      join public.goods on goods.id = markets.good_id
      where markets.town_id = any (v_present)
    )
  ) || private.exchange_state(v_player.player_id, v_present) || private.events_state(v_player.player_id)
    || private.mastery_state(v_player.player_id) || private.reputation_state(v_player.player_id)
    || private.odd_jobs_state(v_player.player_id);
end;
$$;

revoke all on function public.get_state(), public.collect_odd_jobs() from public, anon;
grant execute on function public.get_state(), public.collect_odd_jobs() to authenticated;

notify pgrst, 'reload schema';