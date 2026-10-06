create table if not exists public.saves (
  user_id uuid primary key default auth.uid() references auth.users (id) on delete cascade,
  data jsonb not null,
  revision bigint not null default 1,
  active_device text not null,
  updated_at timestamptz not null default now()
);

alter table public.saves enable row level security;

drop policy if exists "Lire sa sauvegarde" on public.saves;
create policy "Lire sa sauvegarde" on public.saves
  for select to authenticated
  using ((select auth.uid()) = user_id);

drop policy if exists "Créer sa sauvegarde" on public.saves;
create policy "Créer sa sauvegarde" on public.saves
  for insert to authenticated
  with check ((select auth.uid()) = user_id);

drop policy if exists "Modifier sa sauvegarde" on public.saves;
create policy "Modifier sa sauvegarde" on public.saves
  for update to authenticated
  using ((select auth.uid()) = user_id)
  with check ((select auth.uid()) = user_id);

revoke all on table public.saves from anon, authenticated;
grant select, insert, update on table public.saves to authenticated;

create or replace function public.bump_save_revision() returns trigger
language plpgsql
set search_path = ''
as $$
begin
  new.revision := old.revision + 1;
  new.updated_at := now();
  return new;
end;
$$;

drop trigger if exists saves_bump_revision on public.saves;
create trigger saves_bump_revision
  before update on public.saves
  for each row execute function public.bump_save_revision();
