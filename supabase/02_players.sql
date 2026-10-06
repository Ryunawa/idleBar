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

alter table public.players enable row level security;
alter table public.caravans enable row level security;
alter table public.cargo enable row level security;
alter table public.warehouses enable row level security;
alter table public.workshops enable row level security;

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

revoke all on table public.players, public.caravans, public.cargo, public.warehouses, public.workshops from anon, authenticated;
grant select on table public.players, public.caravans, public.cargo, public.warehouses, public.workshops to authenticated;
