alter table private.settings add column if not exists min_client_version text not null default '0.2.0'
  check (min_client_version ~ '^[0-9]+\.[0-9]+\.[0-9]+$');

create or replace function public.get_requirements() returns jsonb
language sql stable
security definer
set search_path = ''
as $$
  select jsonb_build_object('min_client_version', min_client_version) from private.settings;
$$;

revoke all on function public.get_requirements() from public, anon;
grant execute on function public.get_requirements() to authenticated;
