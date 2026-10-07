-- Marchés vivants : les achats et les ventes marquent les prix pendant environ un jour, et chaque jour
-- les besoins des villes changent : quelques marchandises y deviennent recherchées, d'autres
-- surabondantes. Les prix glissent progressivement vers cette nouvelle situation. Tout se règle à la
-- lecture (au plus toutes les quinze minutes), sans tâche planifiée.
-- À exécuter après 21_bandit_ransom.sql.

alter table private.settings add column if not exists market_recovery_hours numeric not null default 24 check (market_recovery_hours > 0);
alter table private.settings add column if not exists market_cycle_hours numeric not null default 24 check (market_cycle_hours > 0);
alter table private.settings add column if not exists market_drift_hours numeric not null default 6 check (market_drift_hours > 0);
alter table private.settings add column if not exists market_settle_minutes numeric not null default 15 check (market_settle_minutes >= 0);
alter table private.settings add column if not exists market_trending_goods integer not null default 2 check (market_trending_goods >= 0);
alter table private.settings add column if not exists market_demand_min numeric not null default 1.35 check (market_demand_min > 0);
alter table private.settings add column if not exists market_demand_max numeric not null default 1.6 check (market_demand_max > 0);
alter table private.settings add column if not exists market_surplus_min numeric not null default 0.55 check (market_surplus_min > 0);
alter table private.settings add column if not exists market_surplus_max numeric not null default 0.75 check (market_surplus_max > 0);

alter table public.markets add column if not exists home_price numeric(10, 2);
alter table public.markets add column if not exists target_price numeric(10, 2);
update public.markets set home_price = base_price where home_price is null;
update public.markets set target_price = base_price where target_price is null;
alter table public.markets alter column home_price drop not null;
alter table public.markets alter column target_price drop not null;

create table if not exists private.market_cycle (
  id smallint primary key check (id = 1),
  rolled_at timestamptz not null,
  drifted_at timestamptz not null
);

insert into private.market_cycle (id, rolled_at, drifted_at) values (1, '2000-01-01', now())
on conflict (id) do nothing;

alter table private.market_cycle enable row level security;
revoke all on table private.market_cycle from anon, authenticated;

create or replace function private.current_pressure(p_pressure numeric, p_since timestamptz) returns numeric
language sql stable
set search_path = ''
as $$
  select p_pressure * exp(-greatest(extract(epoch from (now() - p_since)), 0) / (settings.market_recovery_hours * 3600))
  from private.settings;
$$;

create or replace function private.roll_market_trends() returns void
language plpgsql
set search_path = ''
as $$
declare
  v_settings private.settings;
begin
  select * into v_settings from private.settings;
  update public.markets set home_price = base_price where home_price is null;
  update public.markets set target_price = home_price where target_price is distinct from home_price;
  with ranked as (
    select town_id, good_id, row_number() over (partition by town_id order by random()) as rank
    from public.markets
  )
  update public.markets
  set target_price = round(markets.home_price * case
    when ranked.rank <= v_settings.market_trending_goods
      then v_settings.market_demand_min + random()::numeric * (v_settings.market_demand_max - v_settings.market_demand_min)
    else v_settings.market_surplus_min + random()::numeric * (v_settings.market_surplus_max - v_settings.market_surplus_min)
  end, 2)
  from ranked
  where ranked.town_id = markets.town_id and ranked.good_id = markets.good_id
    and ranked.rank <= v_settings.market_trending_goods * 2;
end;
$$;

create or replace function private.settle_markets() returns void
language plpgsql
set search_path = ''
as $$
declare
  v_settings private.settings;
  v_cycle private.market_cycle;
  v_hours numeric;
begin
  select * into v_settings from private.settings;
  select * into v_cycle from private.market_cycle where id = 1 for update skip locked;
  if not found then
    return;
  end if;

  if now() >= v_cycle.rolled_at + make_interval(secs => v_settings.market_cycle_hours * 3600) then
    perform private.roll_market_trends();
    update private.market_cycle set rolled_at = now() where id = 1;
  elsif now() < v_cycle.drifted_at + make_interval(secs => v_settings.market_settle_minutes * 60) then
    return;
  end if;

  v_hours := greatest(extract(epoch from (now() - v_cycle.drifted_at)), 0) / 3600;
  update public.markets
  set base_price = greatest(round(target_price + (base_price - target_price) * exp(-v_hours / v_settings.market_drift_hours), 2), 0.01)
  where target_price is not null and base_price <> target_price;
  update private.market_cycle set drifted_at = now() where id = 1;
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

  perform private.settle_markets();

  perform private.settle_player(auth.uid());
  select * into v_player from public.players where player_id = auth.uid();
  if not found then
    return jsonb_build_object(
      'server_time', now(), 'player', null, 'caravan', null, 'cargo', '[]'::jsonb,
      'workshop', null, 'warehouses', '[]'::jsonb, 'branches', '[]'::jsonb, 'market', '[]'::jsonb, 'trip_event', null
    ) || private.exchange_state(null, array[]::text[]) || private.events_state(null) || private.mastery_state(null)
      || private.reputation_state(null) || private.odd_jobs_state(null) || private.supply_requests(null) || private.trade_log_state(null);
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
        'sell_price', round(private.sell_quote(markets.base_price, private.current_pressure(markets.pressure, markets.pressure_at)) * (1 + v_haggle + private.town_rate(v_player.player_id, markets.town_id)), 1),
        'trend', case
          when markets.base_price < goods.base_price * 0.8 then 'cheap'
          when markets.base_price > goods.base_price * 1.3 then 'dear'
        end
      ) order by markets.town_id, goods.sort_order), '[]'::jsonb)
      from public.markets
      join public.goods on goods.id = markets.good_id
      where markets.town_id = any (v_present)
    )
  ) || private.exchange_state(v_player.player_id, v_present) || private.events_state(v_player.player_id)
    || private.mastery_state(v_player.player_id) || private.reputation_state(v_player.player_id)
    || private.odd_jobs_state(v_player.player_id) || private.supply_requests(v_player.player_id)
    || private.trade_log_state(v_player.player_id);
end;
$$;

revoke all on all functions in schema private from public, anon, authenticated;
revoke all on function public.get_state() from public, anon;
grant execute on function public.get_state() to authenticated;

notify pgrst, 'reload schema';