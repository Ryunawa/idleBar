create table if not exists public.taverns (
  player_id uuid primary key default auth.uid() references auth.users (id) on delete cascade,
  name text not null check (char_length(name) between 2 and 24),
  friend_code text not null unique check (friend_code ~ '^[A-Z2-9]{6}$'),
  coins bigint not null default 0 check (coins >= 0),
  renown bigint not null default 0 check (renown >= 0),
  served bigint not null default 0 check (served >= 0),
  perfect bigint not null default 0 check (perfect >= 0),
  tip_jar integer not null default 0 check (tip_jar >= 0),
  reported_at timestamptz not null default now(),
  last_seen timestamptz not null default now(),
  created_at timestamptz not null default now()
);

create table if not exists public.tavern_upgrades (
  player_id uuid not null references public.taverns (player_id) on delete cascade,
  upgrade_id text not null references public.upgrades (id),
  bought_at timestamptz not null default now(),
  primary key (player_id, upgrade_id)
);

create table if not exists public.tavern_goals (
  player_id uuid not null references public.taverns (player_id) on delete cascade,
  day date not null,
  goal_id text not null references public.goal_kinds (id),
  progress integer not null default 0 check (progress >= 0),
  done_at timestamptz,
  primary key (player_id, day, goal_id)
);

create index if not exists taverns_last_seen on public.taverns (last_seen);

alter table public.taverns enable row level security;
alter table public.tavern_upgrades enable row level security;
alter table public.tavern_goals enable row level security;
revoke all on public.taverns, public.tavern_upgrades, public.tavern_goals from anon, authenticated;
