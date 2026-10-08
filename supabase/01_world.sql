create schema if not exists private;
revoke all on schema private from anon, authenticated;

create table if not exists private.settings (
  id boolean primary key default true check (id),
  serve_cycle_seconds numeric not null default 10 check (serve_cycle_seconds > 0),
  report_window_seconds integer not null default 600 check (report_window_seconds > 0),
  max_drink_coins integer not null default 25 check (max_drink_coins > 0),
  absence_seconds integer not null default 300 check (absence_seconds > 0),
  tip_jar_cap_hours numeric not null default 8 check (tip_jar_cap_hours > 0),
  online_seconds integer not null default 120 check (online_seconds > 0),
  goals_per_day integer not null default 3 check (goals_per_day > 0),
  base_stools integer not null default 4 check (base_stools > 0)
);

insert into private.settings default values on conflict (id) do nothing;
alter table private.settings enable row level security;
revoke all on private.settings from anon, authenticated;

create table if not exists public.renown_tiers (
  tier integer primary key,
  name text not null,
  renown bigint not null unique check (renown >= 0)
);

insert into public.renown_tiers (tier, name, renown) values
  (0, 'Bicoque', 0),
  (1, 'Estaminet', 300),
  (2, 'Taverne', 1500),
  (3, 'Auberge', 6000),
  (4, 'Grande auberge', 20000)
on conflict (tier) do update set name = excluded.name, renown = excluded.renown;

create table if not exists public.drinks (
  id text primary key,
  name text not null,
  price integer not null check (price > 0),
  sort integer not null
);

insert into public.drinks (id, name, price, sort) values
  ('beer', 'Bière', 4, 1),
  ('tea', 'Thé', 6, 2),
  ('soup', 'Soupe', 8, 3),
  ('cider', 'Cidre', 10, 4),
  ('pie', 'Tourte', 14, 5)
on conflict (id) do update set name = excluded.name, price = excluded.price, sort = excluded.sort;

create table if not exists public.upgrades (
  id text primary key,
  kind text not null check (kind in ('stool', 'station', 'helper', 'decor')),
  name text not null,
  description text not null,
  price integer not null check (price >= 0),
  tier integer not null references public.renown_tiers (tier),
  requires text references public.upgrades (id),
  value text not null,
  sort integer not null
);

insert into public.upgrades (id, kind, name, description, price, tier, requires, value, sort) values
  ('tabouret-5', 'stool', 'Cinquième tabouret', 'Un client de plus au comptoir.', 120, 0, null, '5', 10),
  ('tabouret-6', 'stool', 'Sixième tabouret', 'Un client de plus au comptoir.', 300, 1, 'tabouret-5', '6', 11),
  ('tabouret-7', 'stool', 'Septième tabouret', 'Un client de plus au comptoir.', 700, 2, 'tabouret-6', '7', 12),
  ('tabouret-8', 'stool', 'Huitième tabouret', 'Un client de plus au comptoir.', 1500, 2, 'tabouret-7', '8', 13),
  ('tabouret-9', 'stool', 'Neuvième tabouret', 'Un client de plus au comptoir.', 3000, 3, 'tabouret-8', '9', 14),
  ('tabouret-10', 'stool', 'Dixième tabouret', 'Un client de plus au comptoir.', 6000, 4, 'tabouret-9', '10', 15),
  ('marmite', 'station', 'Marmite', 'Ajoute la soupe à la carte : touille trois fois en rythme.', 400, 1, null, 'soup', 20),
  ('pressoir', 'station', 'Pressoir', 'Ajoute le cidre à la carte : presse en cadence.', 1500, 2, null, 'cider', 21),
  ('four', 'station', 'Four', 'Ajoute la tourte à la carte : sors-la avant qu''elle fume.', 4000, 3, null, 'pie', 22),
  ('apprenti', 'helper', 'Un apprenti', 'Sert les clients qui attendent depuis 45 s et remplit le pot à pourboires pendant ton absence.', 200, 0, null, '1', 30),
  ('commis', 'helper', 'Un commis', 'Sert au bout de 30 s et remplit mieux le pot à pourboires.', 2000, 2, 'apprenti', '2', 31),
  ('serveuse', 'helper', 'Une serveuse', 'Sert au bout de 20 s et remplit très bien le pot à pourboires.', 8000, 3, 'commis', '3', 32),
  ('deco-plantes', 'decor', 'Plantes en pot', 'Un peu de verdure au pied du mur.', 80, 0, null, 'plants', 40),
  ('deco-suspensions', 'decor', 'Ail et herbes', 'Des bottes séchées pendues aux poutres.', 150, 0, null, 'hangings', 41),
  ('deco-affiches', 'decor', 'Panneau d''affichage', 'Les petites annonces du quartier.', 150, 0, null, 'boards', 42),
  ('deco-tableaux', 'decor', 'Tableaux', 'Un paysage et un portrait au mur.', 250, 1, null, 'paintings', 43),
  ('deco-cible', 'decor', 'Cible de fléchettes', 'Pour les soirs de concours.', 300, 1, null, 'darts', 44),
  ('deco-bannieres', 'decor', 'Bannières', 'Aux couleurs de la maison.', 400, 1, null, 'banners', 45),
  ('deco-chat', 'decor', 'Le chat de la taverne', 'Il dort sur les tonneaux et ne sert personne.', 500, 1, null, 'cat', 46),
  ('deco-horloge', 'decor', 'Horloge à balancier', 'Tic, tac.', 500, 2, null, 'clocks', 47),
  ('deco-trophees', 'decor', 'Trophées', 'Une tête de cerf et un bouclier.', 600, 2, null, 'trophies', 48),
  ('deco-cheminee', 'decor', 'Cheminée', 'Un bon feu qui crépite.', 900, 2, null, 'fireplaces', 49)
on conflict (id) do update set
  kind = excluded.kind, name = excluded.name, description = excluded.description, price = excluded.price,
  tier = excluded.tier, requires = excluded.requires, value = excluded.value, sort = excluded.sort;

create table if not exists public.goal_kinds (
  id text primary key,
  label text not null,
  measure text not null check (measure in ('served', 'perfect', 'drink')),
  drink text references public.drinks (id),
  target integer not null check (target > 0),
  reward integer not null check (reward > 0),
  check ((measure = 'drink') = (drink is not null))
);

insert into public.goal_kinds (id, label, measure, drink, target, reward) values
  ('servir-20', 'Servir 20 clients', 'served', null, 20, 60),
  ('servir-50', 'Servir 50 clients', 'served', null, 50, 150),
  ('parfait-5', 'Réussir 5 services parfaits', 'perfect', null, 5, 50),
  ('parfait-15', 'Réussir 15 services parfaits', 'perfect', null, 15, 130),
  ('biere-15', 'Tirer 15 bières', 'drink', 'beer', 15, 60),
  ('the-10', 'Servir 10 thés', 'drink', 'tea', 10, 60),
  ('soupe-8', 'Servir 8 soupes', 'drink', 'soup', 8, 80),
  ('cidre-8', 'Servir 8 cidres', 'drink', 'cider', 8, 100),
  ('tourte-5', 'Servir 5 tourtes', 'drink', 'pie', 5, 120)
on conflict (id) do update set
  label = excluded.label, measure = excluded.measure, drink = excluded.drink, target = excluded.target, reward = excluded.reward;

alter table public.renown_tiers enable row level security;
alter table public.drinks enable row level security;
alter table public.upgrades enable row level security;
alter table public.goal_kinds enable row level security;
revoke all on public.renown_tiers, public.drinks, public.upgrades, public.goal_kinds from anon, authenticated;
