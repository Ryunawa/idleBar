alter table private.settings add column if not exists round_price integer not null default 200 check (round_price >= 0);
alter table private.settings add column if not exists round_gift integer not null default 30 check (round_gift >= 0);
alter table private.settings add column if not exists round_renown integer not null default 10 check (round_renown >= 0);
alter table private.settings add column if not exists round_cooldown_hours numeric not null default 4 check (round_cooldown_hours >= 0);
alter table private.settings add column if not exists greet_seconds integer not null default 30 check (greet_seconds >= 0);
alter table private.settings add column if not exists max_passersby integer not null default 4 check (max_passersby >= 0);
alter table private.settings add column if not exists passersby_minutes integer not null default 5 check (passersby_minutes > 0);

update private.settings set max_drink_coins = greatest(max_drink_coins, 40);

create table if not exists public.invitations (
  from_id uuid not null references public.taverns (player_id) on delete cascade,
  to_id uuid not null references public.taverns (player_id) on delete cascade,
  created_at timestamptz not null default now(),
  primary key (from_id, to_id),
  check (from_id <> to_id)
);

create table if not exists public.rounds (
  id bigint generated always as identity primary key,
  from_id uuid not null references public.taverns (player_id) on delete cascade,
  to_id uuid not null references public.taverns (player_id) on delete cascade,
  offered_at timestamptz not null default now()
);

create index if not exists rounds_to on public.rounds (to_id, offered_at desc);
create index if not exists rounds_from on public.rounds (from_id, offered_at desc);

create table if not exists public.greetings (
  from_id uuid not null references public.taverns (player_id) on delete cascade,
  to_id uuid not null references public.taverns (player_id) on delete cascade,
  greeted_at timestamptz not null default now(),
  primary key (from_id, to_id)
);

alter table public.invitations enable row level security;
alter table public.rounds enable row level security;
alter table public.greetings enable row level security;
revoke all on public.invitations, public.rounds, public.greetings from anon, authenticated;

insert into public.goal_kinds (id, label, measure, drink, target, reward) values
  ('tournee-1', 'Offrir une tournée générale', 'round', null, 1, 60)
on conflict (id) do update set label = excluded.label, measure = excluded.measure, target = excluded.target, reward = excluded.reward;
