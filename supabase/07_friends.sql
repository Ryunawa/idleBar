alter table private.settings add column if not exists visit_patience_seconds integer not null default 180 check (visit_patience_seconds > 0);
alter table private.settings add column if not exists visit_linger_seconds integer not null default 60 check (visit_linger_seconds > 0);
alter table private.settings add column if not exists friend_tip integer not null default 40 check (friend_tip >= 0);
alter table private.settings add column if not exists friend_perfect_tip integer not null default 10 check (friend_perfect_tip >= 0);
alter table private.settings add column if not exists visitor_gift integer not null default 20 check (visitor_gift >= 0);
alter table private.settings add column if not exists friend_renown integer not null default 5 check (friend_renown >= 0);
alter table private.settings add column if not exists max_friends integer not null default 30 check (max_friends > 0);
alter table private.settings add column if not exists max_guests integer not null default 3 check (max_guests > 0);
alter table private.settings add column if not exists emote_seconds integer not null default 2 check (emote_seconds >= 0);

create table if not exists public.specialty_words (
  kind text not null check (kind in ('base', 'complement')),
  word text not null,
  sort integer not null,
  primary key (kind, word)
);

insert into public.specialty_words (kind, word, sort) values
  ('base', 'Breuvage', 1), ('base', 'Hydromel', 2), ('base', 'Cervoise', 3), ('base', 'Infusion', 4),
  ('base', 'Bouillon', 5), ('base', 'Poiré', 6), ('base', 'Grog', 7), ('base', 'Élixir', 8),
  ('base', 'Potion', 9), ('base', 'Tisane', 10), ('base', 'Tourte', 11), ('base', 'Liqueur', 12),
  ('complement', 'de la Maison', 1), ('complement', 'du Dragon', 2), ('complement', 'de la Lune', 3),
  ('complement', 'des Brumes', 4), ('complement', 'du Vieux Chêne', 5), ('complement', 'de la Forge', 6),
  ('complement', 'des Marais', 7), ('complement', 'du Phare', 8), ('complement', 'de Minuit', 9),
  ('complement', 'des Moissons', 10), ('complement', 'du Voyageur', 11), ('complement', 'de la Sorcière', 12),
  ('complement', 'du Roi', 13), ('complement', 'des Étoiles', 14), ('complement', 'de l''Aube', 15), ('complement', 'des Fées', 16)
on conflict (kind, word) do update set sort = excluded.sort;

create table if not exists public.stamps (
  id text primary key,
  name text not null,
  sort integer not null
);

insert into public.stamps (id, name, sort) values
  ('heart', 'Cœur', 1), ('star', 'Étoile', 2), ('mug', 'Chope', 3), ('sun', 'Soleil', 4),
  ('moon', 'Lune', 5), ('flower', 'Fleur', 6), ('note', 'Note', 7), ('crown', 'Couronne', 8)
on conflict (id) do update set name = excluded.name, sort = excluded.sort;

alter table public.taverns add column if not exists avatar jsonb not null default '{"head": 0, "skin": 0, "hair": 0, "clothes": 0, "accent": 0}';
alter table public.taverns add column if not exists specialty_base text not null default 'Breuvage';
alter table public.taverns add column if not exists specialty_complement text not null default 'de la Maison';
alter table public.taverns add column if not exists specialty_drink text not null default 'beer' references public.drinks (id);
alter table public.taverns add column if not exists specialty_color integer not null default 0 check (specialty_color between 0 and 7);

create table if not exists public.friend_requests (
  from_id uuid not null references public.taverns (player_id) on delete cascade,
  to_id uuid not null references public.taverns (player_id) on delete cascade,
  created_at timestamptz not null default now(),
  primary key (from_id, to_id),
  check (from_id <> to_id)
);

create table if not exists public.friendships (
  player_id uuid not null references public.taverns (player_id) on delete cascade,
  friend_id uuid not null references public.taverns (player_id) on delete cascade,
  created_at timestamptz not null default now(),
  primary key (player_id, friend_id),
  check (player_id <> friend_id)
);

create table if not exists public.visits (
  id bigint generated always as identity primary key,
  visitor_id uuid not null references public.taverns (player_id) on delete cascade,
  host_id uuid not null references public.taverns (player_id) on delete cascade,
  stamp text not null references public.stamps (id),
  started_at timestamptz not null default now(),
  served_at timestamptz,
  perfect boolean not null default false,
  helped boolean not null default false,
  rewarded boolean not null default false,
  last_emote_at timestamptz,
  check (visitor_id <> host_id)
);

create index if not exists visits_host on public.visits (host_id, started_at desc);
create index if not exists visits_visitor on public.visits (visitor_id, started_at desc);

create table if not exists public.tasted (
  taster_id uuid not null references public.taverns (player_id) on delete cascade,
  host_id uuid not null references public.taverns (player_id) on delete cascade,
  name text not null,
  drink text not null references public.drinks (id),
  color integer not null,
  tasted_at timestamptz not null default now(),
  primary key (taster_id, host_id)
);

alter table public.specialty_words enable row level security;
alter table public.stamps enable row level security;
alter table public.friend_requests enable row level security;
alter table public.friendships enable row level security;
alter table public.visits enable row level security;
alter table public.tasted enable row level security;
revoke all on public.specialty_words, public.stamps, public.friend_requests, public.friendships, public.visits, public.tasted from anon, authenticated;

insert into public.goal_kinds (id, label, measure, drink, target, reward) values
  ('ami-1', 'Servir un ami de passage', 'friend', null, 1, 100)
on conflict (id) do update set label = excluded.label, measure = excluded.measure, target = excluded.target, reward = excluded.reward;

do $$
begin
  if to_regclass('realtime.messages') is not null then
    execute 'drop policy if exists "Recevoir sa sonnette" on realtime.messages';
    execute $policy$
      create policy "Recevoir sa sonnette" on realtime.messages
      for select to authenticated
      using (realtime.topic() = 'taverne:' || auth.uid()::text and extension = 'broadcast')
    $policy$;
  end if;
end;
$$;
