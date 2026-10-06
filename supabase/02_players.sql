create schema if not exists private;
revoke all on schema private from public, anon, authenticated;

create table if not exists private.settings (
  id boolean primary key default true check (id),
  travel_time_factor numeric not null default 1 check (travel_time_factor > 0),
  starting_coins integer not null default 400 check (starting_coins >= 0),
  starting_town_id text not null default 'port-sable' references public.towns (id),
  max_wagons integer not null default 6 check (max_wagons > 0)
);

alter table private.settings add column if not exists craft_time_factor numeric not null default 1 check (craft_time_factor > 0);
alter table private.settings add column if not exists max_workshop_level integer not null default 5 check (max_workshop_level > 0);
alter table private.settings add column if not exists depot_capacity integer not null default 60 check (depot_capacity > 0);
alter table private.settings add column if not exists offer_hours integer not null default 48 check (offer_hours > 0);
alter table private.settings add column if not exists max_open_offers integer not null default 8 check (max_open_offers > 0);
alter table private.settings add column if not exists max_open_contracts integer not null default 5 check (max_open_contracts > 0);
alter table private.settings add column if not exists max_carried_contracts integer not null default 3 check (max_carried_contracts > 0);
alter table private.settings add column if not exists takeover_minutes integer not null default 180 check (takeover_minutes > 0);
alter table private.settings add column if not exists contract_slack_minutes integer not null default 360 check (contract_slack_minutes >= 0);
alter table private.settings add column if not exists game_carrier_slowness numeric not null default 2 check (game_carrier_slowness >= 1);
alter table private.settings add column if not exists freight_base_rate numeric not null default 0.05 check (freight_base_rate >= 0);
alter table private.settings add column if not exists freight_hourly_rate numeric not null default 0.06 check (freight_hourly_rate >= 0);
alter table private.settings add column if not exists max_branches integer not null default 3 check (max_branches >= 0);

insert into private.settings (id) values (true) on conflict (id) do nothing;

alter table private.settings enable row level security;
revoke all on table private.settings from public, anon, authenticated;

create table if not exists public.players (
  player_id uuid primary key default auth.uid() references auth.users (id) on delete cascade,
  name text not null check (char_length(name) between 3 and 20),
  craft_id text not null references public.crafts (id),
  home_town_id text not null references public.towns (id),
  coins bigint not null check (coins >= 0),
  created_at timestamptz not null default now()
);

create unique index if not exists players_name_key on public.players (lower(name));

create table if not exists public.caravans (
  player_id uuid primary key references public.players (player_id) on delete cascade,
  wagons integer not null default 1 check (wagons > 0),
  town_id text not null references public.towns (id),
  from_town_id text references public.towns (id),
  departed_at timestamptz,
  arrives_at timestamptz
);

do $$
begin
  if exists (
    select 1 from information_schema.columns
    where table_schema = 'public' and table_name = 'caravans' and column_name = 'coins'
  ) then
    insert into public.players (player_id, name, craft_id, home_town_id, coins, created_at)
    select player_id, name, 'caravanier', town_id, coins, created_at from public.caravans
    on conflict (player_id) do nothing;

    alter table public.caravans drop constraint if exists caravans_player_id_fkey;
    alter table public.caravans drop column name, drop column coins, drop column created_at;
    alter table public.caravans
      add constraint caravans_player_id_fkey foreign key (player_id) references public.players (player_id) on delete cascade;
  end if;
end;
$$;

create table if not exists public.cargo (
  player_id uuid not null references public.caravans (player_id) on delete cascade,
  good_id text not null references public.goods (id),
  quantity integer not null check (quantity > 0),
  primary key (player_id, good_id)
);

create table if not exists public.warehouses (
  player_id uuid not null references public.players (player_id) on delete cascade,
  town_id text not null references public.towns (id),
  good_id text not null references public.goods (id),
  quantity integer not null check (quantity > 0),
  primary key (player_id, town_id, good_id)
);

create table if not exists public.workshops (
  player_id uuid primary key references public.players (player_id) on delete cascade,
  town_id text not null references public.towns (id),
  level integer not null default 1 check (level > 0),
  recipe_id text references public.recipes (id),
  queued integer not null default 0 check (queued >= 0),
  batch_seconds integer check (batch_seconds > 0),
  started_at timestamptz,
  check ((queued = 0) = (recipe_id is null and batch_seconds is null and started_at is null))
);

alter table public.workshops add column if not exists paused_until timestamptz;

create table if not exists public.branches (
  player_id uuid not null references public.players (player_id) on delete cascade,
  town_id text not null references public.towns (id),
  opened_at timestamptz not null default now(),
  primary key (player_id, town_id)
);

create table if not exists public.offers (
  id bigint generated always as identity primary key,
  town_id text not null references public.towns (id),
  seller_id uuid not null references public.players (player_id) on delete cascade,
  give_good_id text references public.goods (id),
  give_quantity integer not null check (give_quantity > 0),
  want_good_id text references public.goods (id),
  want_quantity integer not null check (want_quantity > 0),
  status text not null default 'open' check (status in ('open', 'concluded', 'cancelled', 'expired')),
  buyer_id uuid references public.players (player_id) on delete set null,
  created_at timestamptz not null default now(),
  expires_at timestamptz not null,
  closed_at timestamptz,
  seller_seen boolean not null default true,
  check (give_good_id is not null or want_good_id is not null),
  check (give_good_id is distinct from want_good_id),
  check ((status = 'open') = (closed_at is null))
);

create index if not exists offers_open_by_town on public.offers (town_id) where status = 'open';
create index if not exists offers_by_seller on public.offers (seller_id);

create table if not exists public.transport_contracts (
  id bigint generated always as identity primary key,
  shipper_id uuid not null references public.players (player_id) on delete cascade,
  origin_town_id text not null references public.towns (id),
  destination_town_id text not null references public.towns (id),
  good_id text not null references public.goods (id),
  quantity integer not null check (quantity > 0),
  reward integer not null check (reward > 0),
  game_fee integer not null check (game_fee > 0),
  deposit integer not null check (deposit > 0),
  status text not null default 'open' check (status in ('open', 'carried', 'shipped', 'delivered', 'failed', 'cancelled')),
  carrier_id uuid references public.players (player_id) on delete set null,
  created_at timestamptz not null default now(),
  takeover_at timestamptz not null,
  game_arrives_at timestamptz not null,
  carried_at timestamptz,
  deadline timestamptz,
  closed_at timestamptz,
  shipper_seen boolean not null default true,
  carrier_seen boolean not null default true,
  check (origin_town_id <> destination_town_id),
  check ((status = 'carried') <= (carried_at is not null and deadline is not null))
);

create index if not exists transport_contracts_open on public.transport_contracts (origin_town_id) where status = 'open';
create index if not exists transport_contracts_by_shipper on public.transport_contracts (shipper_id);
create index if not exists transport_contracts_by_carrier on public.transport_contracts (carrier_id);

alter table public.players enable row level security;
alter table public.caravans enable row level security;
alter table public.cargo enable row level security;
alter table public.warehouses enable row level security;
alter table public.workshops enable row level security;
alter table public.branches enable row level security;
alter table public.offers enable row level security;
alter table public.transport_contracts enable row level security;

drop policy if exists "Voir son profil" on public.players;
create policy "Voir son profil" on public.players
  for select to authenticated
  using ((select auth.uid()) = player_id);

drop policy if exists "Voir sa caravane" on public.caravans;
create policy "Voir sa caravane" on public.caravans
  for select to authenticated
  using ((select auth.uid()) = player_id);

drop policy if exists "Voir sa cargaison" on public.cargo;
create policy "Voir sa cargaison" on public.cargo
  for select to authenticated
  using ((select auth.uid()) = player_id);

drop policy if exists "Voir ses entrepôts" on public.warehouses;
create policy "Voir ses entrepôts" on public.warehouses
  for select to authenticated
  using ((select auth.uid()) = player_id);

drop policy if exists "Voir son atelier" on public.workshops;
create policy "Voir son atelier" on public.workshops
  for select to authenticated
  using ((select auth.uid()) = player_id);

drop policy if exists "Voir ses succursales" on public.branches;
create policy "Voir ses succursales" on public.branches
  for select to authenticated
  using ((select auth.uid()) = player_id);

drop policy if exists "Voir ses offres" on public.offers;
create policy "Voir ses offres" on public.offers
  for select to authenticated
  using ((select auth.uid()) = seller_id);

drop policy if exists "Voir ses contrats" on public.transport_contracts;
create policy "Voir ses contrats" on public.transport_contracts
  for select to authenticated
  using ((select auth.uid()) in (shipper_id, carrier_id));

revoke all on table
  public.players, public.caravans, public.cargo, public.warehouses, public.workshops,
  public.branches, public.offers, public.transport_contracts
from anon, authenticated;
grant select on table
  public.players, public.caravans, public.cargo, public.warehouses, public.workshops,
  public.branches, public.offers, public.transport_contracts
to authenticated;

alter table private.settings add column if not exists events_enabled boolean not null default true;
alter table private.settings add column if not exists road_event_minutes integer not null default 150 check (road_event_minutes > 0);
alter table private.settings add column if not exists breakdown_hours numeric not null default 6 check (breakdown_hours > 0);
alter table private.settings add column if not exists special_order_hours numeric not null default 8 check (special_order_hours > 0);
alter table private.settings add column if not exists special_order_window_hours numeric not null default 6 check (special_order_window_hours > 0);
alter table private.settings drop column if exists special_order_premium;
alter table private.settings add column if not exists special_order_price_ratio numeric not null default 0.95 check (special_order_price_ratio > 0);
alter table private.settings add column if not exists special_order_value integer not null default 400 check (special_order_value > 0);

alter table public.players add column if not exists orders_checked_at timestamptz not null default now();

create table if not exists public.standing_orders (
  player_id uuid not null references public.players (player_id) on delete cascade,
  kind_id text not null,
  choice_id text not null,
  primary key (player_id, kind_id),
  foreign key (kind_id, choice_id) references public.event_choices (kind_id, id)
);

create table if not exists public.pending_events (
  player_id uuid not null references public.players (player_id) on delete cascade,
  kind_id text not null references public.event_kinds (id),
  occurs_at timestamptz not null,
  primary key (player_id, kind_id)
);

create table if not exists public.special_orders (
  id bigint generated always as identity primary key,
  player_id uuid not null references public.players (player_id) on delete cascade,
  town_id text not null references public.towns (id),
  good_id text not null references public.goods (id),
  quantity integer not null check (quantity > 0),
  unit_price integer not null check (unit_price > 0),
  created_at timestamptz not null default now(),
  deadline timestamptz not null,
  status text not null default 'open' check (status in ('open', 'delivered', 'expired', 'declined')),
  closed_at timestamptz,
  check ((status = 'open') = (closed_at is null))
);

create unique index if not exists special_orders_one_open on public.special_orders (player_id) where status = 'open';

create table if not exists public.event_log (
  id bigint generated always as identity primary key,
  player_id uuid not null references public.players (player_id) on delete cascade,
  kind_id text not null references public.event_kinds (id),
  happened_at timestamptz not null,
  title text not null,
  detail text not null,
  tone text not null check (tone in ('good', 'bad', 'neutral')),
  seen boolean not null default false
);

create index if not exists event_log_by_player on public.event_log (player_id, happened_at desc);

alter table public.standing_orders enable row level security;
alter table public.pending_events enable row level security;
alter table public.special_orders enable row level security;
alter table public.event_log enable row level security;

drop policy if exists "Voir ses consignes" on public.standing_orders;
create policy "Voir ses consignes" on public.standing_orders
  for select to authenticated
  using ((select auth.uid()) = player_id);

drop policy if exists "Voir ses commandes" on public.special_orders;
create policy "Voir ses commandes" on public.special_orders
  for select to authenticated
  using ((select auth.uid()) = player_id);

drop policy if exists "Voir son journal" on public.event_log;
create policy "Voir son journal" on public.event_log
  for select to authenticated
  using ((select auth.uid()) = player_id);

revoke all on table public.standing_orders, public.pending_events, public.special_orders, public.event_log from anon, authenticated;
grant select on table public.standing_orders, public.special_orders, public.event_log to authenticated;

alter table private.settings add column if not exists journeyman_xp integer not null default 600 check (journeyman_xp > 0);
alter table private.settings add column if not exists master_xp integer not null default 3000 check (master_xp > 0);
alter table private.settings add column if not exists journeyman_masterpiece_chance numeric not null default 0.02 check (journeyman_masterpiece_chance between 0 and 1);
alter table private.settings add column if not exists master_masterpiece_chance numeric not null default 0.06 check (master_masterpiece_chance between 0 and 1);
alter table private.settings add column if not exists masterpiece_value numeric not null default 10 check (masterpiece_value > 0);
alter table private.settings add column if not exists contract_xp_value integer not null default 300 check (contract_xp_value > 0);

alter table public.caravans add column if not exists rewarded_until timestamptz;

create table if not exists public.masteries (
  player_id uuid not null references public.players (player_id) on delete cascade,
  craft_id text not null references public.crafts (id),
  xp integer not null default 0 check (xp >= 0),
  primary key (player_id, craft_id)
);

alter table public.masteries add column if not exists trade_credit integer not null default 0 check (trade_credit >= 0);

create table if not exists public.player_talents (
  player_id uuid not null references public.players (player_id) on delete cascade,
  talent_id text not null references public.talents (id),
  chosen_at timestamptz not null default now(),
  primary key (player_id, talent_id)
);

create table if not exists public.masterpieces (
  id bigint generated always as identity primary key,
  owner_id uuid not null references public.players (player_id) on delete cascade,
  maker_name text not null,
  craft_id text not null references public.crafts (id),
  good_id text not null references public.goods (id),
  town_id text not null references public.towns (id),
  created_at timestamptz not null default now(),
  sold_at timestamptz
);

create index if not exists masterpieces_by_owner on public.masterpieces (owner_id) where sold_at is null;

create table if not exists public.caravan_fittings (
  player_id uuid not null references public.caravans (player_id) on delete cascade,
  fitting_id text not null references public.fittings (id),
  installed_at timestamptz not null default now(),
  primary key (player_id, fitting_id)
);

alter table public.masteries enable row level security;
alter table public.player_talents enable row level security;
alter table public.masterpieces enable row level security;
alter table public.caravan_fittings enable row level security;

drop policy if exists "Voir sa maîtrise" on public.masteries;
create policy "Voir sa maîtrise" on public.masteries
  for select to authenticated
  using ((select auth.uid()) = player_id);

drop policy if exists "Voir ses talents" on public.player_talents;
create policy "Voir ses talents" on public.player_talents
  for select to authenticated
  using ((select auth.uid()) = player_id);

drop policy if exists "Voir ses chefs-d'œuvre" on public.masterpieces;
create policy "Voir ses chefs-d'œuvre" on public.masterpieces
  for select to authenticated
  using ((select auth.uid()) = owner_id);

drop policy if exists "Voir ses équipements" on public.caravan_fittings;
create policy "Voir ses équipements" on public.caravan_fittings
  for select to authenticated
  using ((select auth.uid()) = player_id);

revoke all on table public.masteries, public.player_talents, public.masterpieces, public.caravan_fittings from anon, authenticated;
grant select on table public.masteries, public.player_talents, public.masterpieces, public.caravan_fittings to authenticated;

alter table private.settings add column if not exists decision_minutes integer not null default 60 check (decision_minutes > 0);

alter table public.caravans add column if not exists directive_id text not null default 'prudence' references public.directives (id);

delete from public.standing_orders where kind_id in (select id from public.event_kinds where scope = 'road');

create table if not exists public.trip_events (
  player_id uuid primary key references public.caravans (player_id) on delete cascade,
  kind_id text not null references public.event_kinds (id),
  occurred_at timestamptz not null,
  decide_by timestamptz not null,
  planned_seconds numeric not null check (planned_seconds >= 0)
);

alter table public.trip_events enable row level security;

drop policy if exists "Voir l'événement de son voyage" on public.trip_events;
create policy "Voir l'événement de son voyage" on public.trip_events
  for select to authenticated
  using ((select auth.uid()) = player_id);

revoke all on table public.trip_events from anon, authenticated;
grant select on table public.trip_events to authenticated;
