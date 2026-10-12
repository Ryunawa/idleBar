create table if not exists public.fighter_kinds (
  id text primary key,
  position smallint not null unique check (position >= 0)
);

insert into public.fighter_kinds (id, position) values ('hero', 0), ('archer', 1), ('mage', 2)
on conflict (id) do nothing;

create table if not exists public.skill_kinds (
  id text primary key,
  position smallint not null unique check (position >= 0)
);

insert into public.skill_kinds (id, position) values ('smite', 0), ('fury', 1), ('gold_rain', 2)
on conflict (id) do nothing;

create table if not exists public.building_kinds (
  id text primary key
);

insert into public.building_kinds (id) values ('farm'), ('mill'), ('bakery'), ('forge'), ('market'), ('tower'), ('portal')
on conflict (id) do nothing;

create table if not exists public.plots (
  plot smallint primary key check (plot >= 0),
  unlock_stage integer not null check (unlock_stage >= 1)
);

insert into public.plots (plot, unlock_stage) values (0, 1), (1, 1), (2, 11), (3, 21), (4, 31), (5, 41), (6, 51)
on conflict (plot) do nothing;

create table if not exists public.frontiers (
  player_id uuid primary key references auth.users (id) on delete cascade,
  revision integer not null default 0 check (revision >= 0),
  session_id uuid,
  gold double precision not null default 0 check (gold >= 0 and gold < 'Infinity'),
  lifetime_gold double precision not null default 0 check (lifetime_gold >= 0 and lifetime_gold < 'Infinity'),
  souls bigint not null default 0 check (souls >= 0),
  retreats integer not null default 0 check (retreats >= 0),
  stage integer not null default 1 check (stage >= 1),
  max_stage integer not null default 1 check (max_stage >= stage),
  best_stage integer not null default 1 check (best_stage >= max_stage),
  kills integer not null default 0 check (kills >= 0),
  boss_held boolean not null default false,
  intendant_unlocked boolean not null default false,
  intendant_on boolean not null default false check (intendant_unlocked or not intendant_on),
  monsters_killed bigint not null default 0 check (monsters_killed >= 0),
  bosses_killed bigint not null default 0 check (bosses_killed >= 0),
  chickens bigint not null default 0 check (chickens >= 0),
  clicks bigint not null default 0 check (clicks >= 0),
  skills_used bigint not null default 0 check (skills_used >= 0),
  saved_at timestamptz not null default now(),
  last_seen timestamptz not null default now(),
  created_at timestamptz not null default now()
);

create table if not exists public.frontier_fighters (
  player_id uuid not null references public.frontiers (player_id) on delete cascade,
  fighter text not null references public.fighter_kinds (id),
  level integer not null default 0 check (level >= 0),
  primary key (player_id, fighter)
);

create table if not exists public.frontier_plots (
  player_id uuid not null references public.frontiers (player_id) on delete cascade,
  plot smallint not null references public.plots (plot),
  building text references public.building_kinds (id),
  level integer not null default 0 check (level >= 0),
  primary key (player_id, plot),
  constraint frontier_plots_one_each unique (player_id, building) deferrable initially deferred,
  check ((building is null) = (level = 0))
);

create table if not exists public.frontier_cooldowns (
  player_id uuid not null references public.frontiers (player_id) on delete cascade,
  skill text not null references public.skill_kinds (id),
  seconds double precision not null default 0 check (seconds >= 0 and seconds < 'Infinity'),
  primary key (player_id, skill)
);

create table if not exists public.frontier_achievements (
  player_id uuid not null references public.frontiers (player_id) on delete cascade,
  achievement text not null check (achievement ~ '^[a-z0-9-]{1,40}$'),
  unlocked_at timestamptz not null default now(),
  primary key (player_id, achievement)
);

alter table public.fighter_kinds enable row level security;
alter table public.skill_kinds enable row level security;
alter table public.building_kinds enable row level security;
alter table public.plots enable row level security;
alter table public.frontiers enable row level security;
alter table public.frontier_fighters enable row level security;
alter table public.frontier_plots enable row level security;
alter table public.frontier_cooldowns enable row level security;
alter table public.frontier_achievements enable row level security;

revoke all on public.fighter_kinds, public.skill_kinds, public.building_kinds, public.plots, public.frontiers,
  public.frontier_fighters, public.frontier_plots, public.frontier_cooldowns, public.frontier_achievements
from anon, authenticated;

create or replace function public.get_requirements() returns jsonb
language sql stable
security definer
set search_path = ''
as $$
  select jsonb_build_object('min_client_version', min_client_version) from private.settings;
$$;

revoke all on function public.get_requirements() from public, anon;
grant execute on function public.get_requirements() to authenticated;
