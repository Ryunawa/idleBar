create schema if not exists private;
revoke all on schema private from anon, authenticated;

create table if not exists private.settings (
  id boolean primary key default true check (id)
);

alter table private.settings
  add column if not exists min_client_version text not null default '0.4.1' check (min_client_version ~ '^[0-9]+\.[0-9]+\.[0-9]+$'),
  add column if not exists frontier_stage_rate numeric not null default 6 check (frontier_stage_rate > 0),
  add column if not exists frontier_stage_margin integer not null default 30 check (frontier_stage_margin >= 0),
  add column if not exists frontier_retreat_minutes numeric not null default 3 check (frontier_retreat_minutes > 0),
  add column if not exists frontier_idle_seconds integer not null default 120 check (frontier_idle_seconds > 0);

insert into private.settings default values on conflict (id) do nothing;
alter table private.settings enable row level security;
revoke all on private.settings from anon, authenticated;
