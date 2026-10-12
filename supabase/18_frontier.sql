alter table private.settings add column if not exists frontier_save_max_bytes integer not null default 65536
  check (frontier_save_max_bytes > 0);
alter table private.settings add column if not exists frontier_stage_rate numeric not null default 6
  check (frontier_stage_rate > 0);
alter table private.settings add column if not exists frontier_stage_margin integer not null default 30
  check (frontier_stage_margin >= 0);
alter table private.settings add column if not exists frontier_veteran_souls integer[] not null default '{0,2,5,10,20}';

create table if not exists public.frontiers (
  player_id uuid primary key default auth.uid() references auth.users (id) on delete cascade,
  save jsonb not null default '{}'::jsonb check (jsonb_typeof(save) = 'object'),
  revision integer not null default 0 check (revision >= 0),
  session_id uuid,
  max_stage integer not null default 1 check (max_stage >= 1),
  souls bigint not null default 0 check (souls >= 0),
  retreats integer not null default 0 check (retreats >= 0),
  veteran_souls integer not null default 0 check (veteran_souls >= 0),
  veteran_name text,
  saved_at timestamptz not null default now(),
  last_seen timestamptz not null default now(),
  created_at timestamptz not null default now()
);

create index if not exists frontiers_last_seen on public.frontiers (last_seen);

alter table public.frontiers enable row level security;
revoke all on public.frontiers from anon, authenticated;

create table if not exists private.veterans (
  player_id uuid primary key references auth.users (id) on delete cascade,
  tier integer not null check (tier >= 0),
  tavern_name text not null
);

alter table private.veterans enable row level security;
revoke all on private.veterans from anon, authenticated;

insert into private.veterans (player_id, tier, tavern_name)
select player_id, private.tier_of(renown), name from public.taverns
on conflict (player_id) do update
  set tier = greatest(private.veterans.tier, excluded.tier), tavern_name = excluded.tavern_name;
