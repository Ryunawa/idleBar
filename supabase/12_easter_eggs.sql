alter table public.stamps add column if not exists exclusive boolean not null default false;

insert into public.stamps (id, name, sort, exclusive) values
  ('dragon', 'Sceau du dragon', 9, true)
on conflict (id) do update set name = excluded.name, sort = excluded.sort, exclusive = excluded.exclusive;

create table if not exists public.stamp_owners (
  stamp_id text not null references public.stamps (id),
  player_id uuid not null references auth.users (id) on delete cascade,
  primary key (stamp_id, player_id)
);

alter table public.stamp_owners enable row level security;
revoke all on public.stamp_owners from anon, authenticated;

create or replace function private.may_stamp(p_player_id uuid, p_stamp text) returns boolean
language sql stable
set search_path = ''
as $$
  select exists (
    select 1 from public.stamps
    where stamps.id = p_stamp
      and (not stamps.exclusive or exists (
        select 1 from public.stamp_owners where stamp_owners.stamp_id = stamps.id and stamp_owners.player_id = p_player_id)));
$$;

create or replace function public.get_world() returns jsonb
language sql stable
security definer
set search_path = ''
as $$
  select jsonb_build_object(
    'tiers', (select jsonb_agg(jsonb_build_object('tier', tier, 'name', name, 'renown', renown) order by tier) from public.renown_tiers),
    'drinks', (select jsonb_agg(jsonb_build_object('id', id, 'name', name, 'price', price) order by sort) from public.drinks),
    'upgrades', (select jsonb_agg(jsonb_build_object(
      'id', id, 'kind', kind, 'name', name, 'description', description, 'price', price,
      'tier', tier, 'requires', requires, 'value', value) order by sort) from public.upgrades),
    'regulars', (select jsonb_agg(jsonb_build_object(
      'id', regulars.id, 'name', regulars.name, 'title', regulars.title, 'drink', regulars.drink,
      'condition', regulars.condition, 'hint', regulars.hint, 'souvenir', regulars.souvenir,
      'chapters', (select jsonb_agg(jsonb_build_object('chapter', chapter, 'friendship', friendship, 'line', line) order by chapter)
        from public.regular_chapters where regular_id = regulars.id)) order by regulars.sort)
      from public.regulars),
    'bases', (select jsonb_agg(word order by sort) from public.specialty_words where kind = 'base'),
    'complements', (select jsonb_agg(word order by sort) from public.specialty_words where kind = 'complement'),
    'stamps', (select jsonb_agg(jsonb_build_object('id', id, 'name', name) order by exclusive desc, sort)
      from public.stamps where private.may_stamp(auth.uid(), id)));
$$;

create or replace function public.start_visit(p_host uuid, p_stamp text) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_tavern public.taverns;
  v_settings private.settings;
begin
  v_tavern := private.lock_tavern();
  select * into v_settings from private.settings;
  perform private.settle_visits(v_tavern.player_id);
  if not private.are_friends(v_tavern.player_id, p_host) then
    raise exception 'Tu ne peux rendre visite qu''à tes amis.';
  end if;

  if not exists (select 1 from public.stamps where id = p_stamp) then
    raise exception 'Choisis un tampon pour le livre d''or.';
  end if;

  if not private.may_stamp(v_tavern.player_id, p_stamp) then
    raise exception 'Ce tampon appartient à quelqu''un d''autre.';
  end if;

  if exists (select 1 from public.visits where visitor_id = v_tavern.player_id and served_at is null) then
    raise exception 'Tu es déjà en visite.';
  end if;

  perform 1 from public.taverns where player_id = p_host for update;
  if (select count(*) from public.visits where host_id = p_host and served_at is null) >= v_settings.max_guests then
    raise exception 'La taverne de ton ami est pleine d''invités : réessaie dans un instant.';
  end if;

  insert into public.visits (visitor_id, host_id, stamp) values (v_tavern.player_id, p_host, p_stamp);
  perform private.ring(p_host, 'refresh', jsonb_build_object('guest', v_tavern.name));
  return private.state(v_tavern.player_id);
end;
$$;

revoke all on function public.get_world() from public, anon;
revoke all on function public.start_visit(uuid, text) from public, anon;
grant execute on function public.get_world() to authenticated;
grant execute on function public.start_visit(uuid, text) to authenticated;
