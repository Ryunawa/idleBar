create table if not exists public.goods (
  id text primary key,
  name text not null,
  base_price integer not null check (base_price > 0),
  sort_order smallint not null
);

alter table public.goods add column if not exists crafted boolean not null default false;

create table if not exists public.towns (
  id text primary key,
  name text not null,
  biome text not null check (biome in ('coast', 'plain', 'forest', 'marsh', 'mountain', 'desert')),
  map_x smallint not null,
  map_y smallint not null
);

create table if not exists public.routes (
  from_town_id text not null references public.towns (id),
  to_town_id text not null references public.towns (id),
  minutes integer not null check (minutes > 0),
  biome text not null check (biome in ('coast', 'plain', 'forest', 'marsh', 'mountain', 'desert')),
  primary key (from_town_id, to_town_id),
  check (from_town_id <> to_town_id)
);

create table if not exists public.markets (
  town_id text not null references public.towns (id),
  good_id text not null references public.goods (id),
  base_price numeric(10, 2) not null check (base_price > 0),
  pressure numeric not null default 0,
  pressure_at timestamptz not null default now(),
  primary key (town_id, good_id)
);

insert into public.goods (id, name, base_price, sort_order, crafted) values
  ('sel', 'Sel', 10, 1, false),
  ('poisson', 'Poisson', 12, 2, false),
  ('bois', 'Bois', 8, 3, false),
  ('laine', 'Laine', 14, 4, false),
  ('charbon', 'Charbon', 9, 5, false),
  ('fer', 'Fer', 18, 6, false),
  ('herbes', 'Herbes', 15, 7, false),
  ('vin', 'Vin', 22, 8, false),
  ('teinture', 'Teinture', 26, 9, false),
  ('epices', 'Épices', 40, 10, false),
  ('outils', 'Outils', 70, 11, true),
  ('ferrures', 'Ferrures', 20, 12, true),
  ('caisses', 'Caisses', 12, 13, true),
  ('roues', 'Roues', 135, 14, true),
  ('drap', 'Drap', 65, 15, true),
  ('baches', 'Bâches', 85, 16, true),
  ('remedes', 'Remèdes', 85, 17, true),
  ('onguent', 'Onguent', 80, 18, true),
  ('chariot', 'Chariot', 480, 19, true)
on conflict (id) do update set
  name = excluded.name,
  base_price = excluded.base_price,
  sort_order = excluded.sort_order,
  crafted = excluded.crafted;

alter table public.goods add column if not exists market_sells boolean not null default true;
update public.goods set market_sells = (id <> 'chariot');

insert into public.towns (id, name, biome, map_x, map_y) values
  ('port-sable', 'Port-Sable', 'coast', 10, 70),
  ('hautecombe', 'Hautecombe', 'plain', 30, 55),
  ('vignelune', 'Vignelune', 'plain', 25, 85),
  ('bois-dormant', 'Bois-Dormant', 'forest', 20, 35),
  ('brumeval', 'Brumeval', 'marsh', 35, 15),
  ('rocheclaire', 'Rocheclaire', 'plain', 50, 45),
  ('ferrenoire', 'Ferrenoire', 'mountain', 65, 25),
  ('ambrevault', 'Ambrevault', 'desert', 92, 60)
on conflict (id) do update set
  name = excluded.name,
  biome = excluded.biome,
  map_x = excluded.map_x,
  map_y = excluded.map_y;

with roads (town_a, town_b, minutes, biome) as (values
  ('port-sable', 'hautecombe', 30, 'plain'),
  ('port-sable', 'bois-dormant', 45, 'forest'),
  ('port-sable', 'vignelune', 60, 'coast'),
  ('hautecombe', 'vignelune', 35, 'plain'),
  ('hautecombe', 'rocheclaire', 40, 'plain'),
  ('bois-dormant', 'brumeval', 50, 'marsh'),
  ('bois-dormant', 'rocheclaire', 60, 'forest'),
  ('vignelune', 'ferrenoire', 90, 'mountain'),
  ('rocheclaire', 'ferrenoire', 75, 'mountain'),
  ('brumeval', 'ferrenoire', 120, 'mountain'),
  ('rocheclaire', 'ambrevault', 240, 'desert'),
  ('ferrenoire', 'ambrevault', 300, 'desert')
)
insert into public.routes (from_town_id, to_town_id, minutes, biome)
select town_a, town_b, minutes, biome from roads
union all
select town_b, town_a, minutes, biome from roads
on conflict (from_town_id, to_town_id) do update set
  minutes = excluded.minutes,
  biome = excluded.biome;

with specialties (town_id, good_id, factor) as (values
  ('port-sable', 'sel', 0.5), ('port-sable', 'poisson', 0.55),
  ('port-sable', 'bois', 1.5), ('port-sable', 'vin', 1.4), ('port-sable', 'epices', 1.5),
  ('hautecombe', 'laine', 0.5),
  ('hautecombe', 'sel', 1.5), ('hautecombe', 'fer', 1.4), ('hautecombe', 'herbes', 1.35),
  ('vignelune', 'vin', 0.5),
  ('vignelune', 'laine', 1.4), ('vignelune', 'poisson', 1.4), ('vignelune', 'charbon', 1.4),
  ('bois-dormant', 'bois', 0.45), ('bois-dormant', 'herbes', 0.55),
  ('bois-dormant', 'sel', 1.4), ('bois-dormant', 'fer', 1.5), ('bois-dormant', 'epices', 1.4),
  ('brumeval', 'herbes', 0.5), ('brumeval', 'teinture', 0.6),
  ('brumeval', 'vin', 1.5), ('brumeval', 'sel', 1.5), ('brumeval', 'bois', 1.35),
  ('rocheclaire', 'charbon', 0.7), ('rocheclaire', 'bois', 0.75),
  ('rocheclaire', 'epices', 1.6), ('rocheclaire', 'teinture', 1.5), ('rocheclaire', 'herbes', 1.4),
  ('ferrenoire', 'fer', 0.5), ('ferrenoire', 'charbon', 0.5),
  ('ferrenoire', 'poisson', 1.6), ('ferrenoire', 'laine', 1.4), ('ferrenoire', 'vin', 1.5),
  ('ambrevault', 'epices', 0.45), ('ambrevault', 'teinture', 0.5),
  ('ambrevault', 'fer', 1.7), ('ambrevault', 'laine', 1.6), ('ambrevault', 'bois', 1.7), ('ambrevault', 'poisson', 1.5),
  ('bois-dormant', 'outils', 1.5), ('hautecombe', 'outils', 1.35), ('ambrevault', 'outils', 1.6),
  ('hautecombe', 'ferrures', 1.5), ('rocheclaire', 'ferrures', 1.4),
  ('port-sable', 'caisses', 1.5), ('vignelune', 'caisses', 1.4),
  ('port-sable', 'roues', 1.4), ('ambrevault', 'roues', 1.7), ('rocheclaire', 'roues', 1.35),
  ('ferrenoire', 'drap', 1.5), ('ambrevault', 'drap', 1.6), ('port-sable', 'drap', 1.35),
  ('port-sable', 'baches', 1.5), ('brumeval', 'baches', 1.4),
  ('ferrenoire', 'remedes', 1.6), ('ambrevault', 'remedes', 1.5), ('port-sable', 'remedes', 1.35),
  ('rocheclaire', 'onguent', 1.4), ('hautecombe', 'onguent', 1.35),
  ('port-sable', 'chariot', 1.25), ('ambrevault', 'chariot', 1.35)
)
insert into public.markets (town_id, good_id, base_price)
select
  towns.id,
  goods.id,
  round(goods.base_price * coalesce(
    specialties.factor,
    1 + (((hashtext(towns.id || ':' || goods.id)::bigint % 21) + 21) % 21 - 10) / 100.0
  ), 2)
from public.towns
cross join public.goods
left join specialties on specialties.town_id = towns.id and specialties.good_id = goods.id
on conflict (town_id, good_id) do update set base_price = excluded.base_price;

create table if not exists public.crafts (
  id text primary key,
  name text not null,
  kind text not null check (kind in ('itinerant', 'sedentary')),
  playable boolean not null,
  description text not null,
  sort_order smallint not null
);

alter table public.crafts add column if not exists opens_branches boolean not null default false;

insert into public.crafts (id, name, kind, playable, description, sort_order) values
  ('caravanier', 'Caravanier', 'itinerant', true, 'Voyage de ville en ville, commerce et transporte les marchandises des autres.', 1),
  ('forgeron', 'Forgeron', 'sedentary', true, 'Forge outils et ferrures à partir de fer et de charbon.', 2),
  ('charron', 'Charron', 'sedentary', true, 'Assemble caisses et roues avec du bois et des outils.', 3),
  ('tisserand', 'Tisserand', 'sedentary', true, 'Tisse le drap et coud les bâches avec de la laine et de la teinture.', 4),
  ('herboriste', 'Herboriste', 'sedentary', true, 'Prépare remèdes et onguents à partir d''herbes.', 5),
  ('negociant', 'Négociant', 'sedentary', true, 'Tient un comptoir, ouvre des succursales et fait voyager ses marchandises par contrats.', 6),
  ('cartographe', 'Cartographe', 'itinerant', false, 'Explore les routes et vend ses cartes aux caravaniers. Arrivera plus tard.', 7)
on conflict (id) do update set
  name = excluded.name,
  kind = excluded.kind,
  playable = excluded.playable,
  description = excluded.description,
  sort_order = excluded.sort_order;

update public.crafts set opens_branches = (id = 'negociant');

alter table public.crafts add column if not exists family text not null default 'artisan' check (family in ('artisan', 'caravanier', 'negociant'));
update public.crafts set family = case kind when 'itinerant' then 'caravanier' else case when opens_branches then 'negociant' else 'artisan' end end;

create table if not exists public.recipes (
  id text primary key,
  craft_id text not null references public.crafts (id),
  output_good_id text not null references public.goods (id),
  output_quantity integer not null check (output_quantity > 0),
  seconds integer not null check (seconds > 0),
  sort_order smallint not null
);

create table if not exists public.recipe_inputs (
  recipe_id text not null references public.recipes (id),
  good_id text not null references public.goods (id),
  quantity integer not null check (quantity > 0),
  primary key (recipe_id, good_id)
);

insert into public.recipes (id, craft_id, output_good_id, output_quantity, seconds, sort_order) values
  ('outils', 'forgeron', 'outils', 1, 600, 1),
  ('ferrures', 'forgeron', 'ferrures', 2, 480, 2),
  ('caisses', 'charron', 'caisses', 2, 360, 1),
  ('roues', 'charron', 'roues', 1, 900, 2),
  ('drap', 'tisserand', 'drap', 1, 720, 1),
  ('baches', 'tisserand', 'baches', 1, 900, 2),
  ('remedes', 'herboriste', 'remedes', 1, 720, 1),
  ('onguent', 'herboriste', 'onguent', 1, 600, 2),
  ('chariot', 'charron', 'chariot', 1, 1800, 3)
on conflict (id) do update set
  craft_id = excluded.craft_id,
  output_good_id = excluded.output_good_id,
  output_quantity = excluded.output_quantity,
  seconds = excluded.seconds,
  sort_order = excluded.sort_order;

insert into public.recipe_inputs (recipe_id, good_id, quantity) values
  ('outils', 'fer', 2), ('outils', 'charbon', 1),
  ('ferrures', 'fer', 1), ('ferrures', 'charbon', 1),
  ('caisses', 'bois', 2),
  ('roues', 'bois', 3), ('roues', 'outils', 1),
  ('drap', 'laine', 3),
  ('baches', 'laine', 2), ('baches', 'teinture', 1),
  ('remedes', 'herbes', 3), ('remedes', 'sel', 1),
  ('onguent', 'herbes', 2), ('onguent', 'vin', 1),
  ('chariot', 'roues', 2), ('chariot', 'caisses', 3), ('chariot', 'ferrures', 4)
on conflict (recipe_id, good_id) do update set quantity = excluded.quantity;

alter table public.goods enable row level security;
alter table public.towns enable row level security;
alter table public.routes enable row level security;
alter table public.markets enable row level security;
alter table public.crafts enable row level security;
alter table public.recipes enable row level security;
alter table public.recipe_inputs enable row level security;

drop policy if exists "Lire les marchandises" on public.goods;
create policy "Lire les marchandises" on public.goods for select to authenticated using (true);

drop policy if exists "Lire les villes" on public.towns;
create policy "Lire les villes" on public.towns for select to authenticated using (true);

drop policy if exists "Lire les routes" on public.routes;
create policy "Lire les routes" on public.routes for select to authenticated using (true);

drop policy if exists "Lire les marchés" on public.markets;
create policy "Lire les marchés" on public.markets for select to authenticated using (true);

drop policy if exists "Lire les métiers" on public.crafts;
create policy "Lire les métiers" on public.crafts for select to authenticated using (true);

drop policy if exists "Lire les recettes" on public.recipes;
create policy "Lire les recettes" on public.recipes for select to authenticated using (true);

drop policy if exists "Lire les ingrédients" on public.recipe_inputs;
create policy "Lire les ingrédients" on public.recipe_inputs for select to authenticated using (true);

revoke all on table
  public.goods, public.towns, public.routes, public.markets, public.crafts, public.recipes, public.recipe_inputs
from anon, authenticated;
grant select on table
  public.goods, public.towns, public.routes, public.markets, public.crafts, public.recipes, public.recipe_inputs
to authenticated;

create table if not exists public.event_kinds (
  id text primary key,
  scope text not null,
  name text not null,
  description text not null,
  sort_order smallint not null
);

alter table public.event_kinds drop constraint if exists event_kinds_scope_check;
alter table public.event_kinds add constraint event_kinds_scope_check check (scope in ('road', 'workshop', 'mastery'));

create table if not exists public.event_choices (
  kind_id text not null references public.event_kinds (id),
  id text not null,
  name text not null,
  description text not null,
  is_default boolean not null default false,
  sort_order smallint not null,
  primary key (kind_id, id)
);

create unique index if not exists event_choices_one_default on public.event_choices (kind_id) where is_default;

create table if not exists public.road_event_odds (
  biome text not null check (biome in ('coast', 'plain', 'forest', 'marsh', 'mountain', 'desert')),
  kind_id text not null references public.event_kinds (id),
  weight integer not null check (weight >= 0),
  primary key (biome, kind_id)
);

insert into public.event_kinds (id, scope, name, description, sort_order) values
  ('bandits', 'road', 'Bandits', 'Des brigands barrent la route de ta caravane.', 1),
  ('orage', 'road', 'Orage', 'Un orage éclate pendant le trajet.', 2),
  ('peage', 'road', 'Péage', 'Un seigneur réclame un droit de passage.', 3),
  ('trouvaille', 'road', 'Trouvaille', 'Une charrette abandonnée au bord du chemin.', 4),
  ('panne', 'workshop', 'Panne', 'Un outil casse en pleine fabrication.', 5),
  ('commande', 'workshop', 'Commande spéciale', 'Un client de la ville commande une marchandise à bon prix.', 6),
  ('rang', 'mastery', 'Nouveau rang', 'Tu progresses dans ton métier.', 7),
  ('chef_oeuvre', 'mastery', 'Chef-d''œuvre', 'Une pièce d''exception sort de ton atelier.', 8)
on conflict (id) do update set
  scope = excluded.scope,
  name = excluded.name,
  description = excluded.description,
  sort_order = excluded.sort_order;

update public.event_choices set is_default = false
where (kind_id, id) not in (values
  ('bandits', 'payer'), ('orage', 'abriter'), ('peage', 'payer'), ('panne', 'reparer'), ('commande', 'livrer'));

insert into public.event_choices (kind_id, id, name, description, is_default, sort_order) values
  ('bandits', 'payer', 'Payer le passage', 'Tu perds 8 % de ta bourse, 150 écus au plus.', true, 1),
  ('bandits', 'fuir', 'Fuir à travers champs', 'Tu gardes tes écus mais abandonnes un quart de ta marchandise la plus précieuse, et le trajet s''allonge de 10 %.', false, 2),
  ('orage', 'abriter', 'S''abriter', 'Tu attends l''éclaircie : le trajet s''allonge de 30 %.', true, 1),
  ('orage', 'forcer', 'Forcer le passage', 'Le trajet ne s''allonge que de 5 %, mais un dixième de ton plus gros chargement prend l''eau.', false, 2),
  ('peage', 'payer', 'Payer le péage', '3 écus par chariot et 1 écu par dizaine de marchandises transportées.', true, 1),
  ('peage', 'contourner', 'Contourner', 'Pas un écu, mais le trajet s''allonge de 35 %.', false, 2),
  ('panne', 'reparer', 'Faire réparer', '20 écus par niveau d''atelier, et l''atelier repart après 10 minutes.', true, 1),
  ('panne', 'attendre', 'Réparer soi-même', 'Gratuit, mais l''atelier s''arrête une heure.', false, 2),
  ('commande', 'livrer', 'Livrer dès que possible', 'La commande part dès que la marchandise est à l''entrepôt.', true, 1),
  ('commande', 'demander', 'Me laisser décider', 'La commande attend que tu la livres ou la refuses.', false, 2)
on conflict (kind_id, id) do update set
  name = excluded.name,
  description = excluded.description,
  is_default = excluded.is_default,
  sort_order = excluded.sort_order;

insert into public.road_event_odds (biome, kind_id, weight) values
  ('plain', 'bandits', 2), ('plain', 'orage', 2), ('plain', 'peage', 4), ('plain', 'trouvaille', 2),
  ('coast', 'bandits', 1), ('coast', 'orage', 4), ('coast', 'peage', 2), ('coast', 'trouvaille', 3),
  ('forest', 'bandits', 5), ('forest', 'orage', 2), ('forest', 'peage', 1), ('forest', 'trouvaille', 2),
  ('marsh', 'bandits', 2), ('marsh', 'orage', 4), ('marsh', 'peage', 1), ('marsh', 'trouvaille', 3),
  ('mountain', 'bandits', 3), ('mountain', 'orage', 4), ('mountain', 'peage', 2), ('mountain', 'trouvaille', 1),
  ('desert', 'bandits', 5), ('desert', 'orage', 2), ('desert', 'peage', 1), ('desert', 'trouvaille', 2)
on conflict (biome, kind_id) do update set weight = excluded.weight;

alter table public.event_kinds enable row level security;
alter table public.event_choices enable row level security;
alter table public.road_event_odds enable row level security;

drop policy if exists "Lire les événements" on public.event_kinds;
create policy "Lire les événements" on public.event_kinds for select to authenticated using (true);

drop policy if exists "Lire les réactions" on public.event_choices;
create policy "Lire les réactions" on public.event_choices for select to authenticated using (true);

drop policy if exists "Lire les chances d'événement" on public.road_event_odds;
create policy "Lire les chances d'événement" on public.road_event_odds for select to authenticated using (true);

revoke all on table public.event_kinds, public.event_choices, public.road_event_odds from anon, authenticated;
grant select on table public.event_kinds, public.event_choices, public.road_event_odds to authenticated;

create table if not exists public.talents (
  id text primary key,
  family text not null check (family in ('artisan', 'caravanier', 'negociant')),
  tier text not null check (tier in ('compagnon', 'maitre')),
  name text not null,
  description text not null,
  effect text not null,
  amount numeric not null,
  sort_order smallint not null
);

insert into public.talents (id, family, tier, name, description, effect, amount, sort_order) values
  ('cadence', 'artisan', 'compagnon', 'Cadence', 'Ton atelier travaille 15 % plus vite.', 'speed', 0.15, 1),
  ('econome', 'artisan', 'compagnon', 'Économe', 'Chaque fabrication consomme 15 % de matières premières en moins.', 'saving', 0.15, 2),
  ('main_sure', 'artisan', 'maitre', 'Main sûre', 'Les pannes deviennent quatre fois plus rares.', 'sturdy', 0.75, 3),
  ('renommee', 'artisan', 'maitre', 'Renommée', 'Les clients passent deux fois plus de commandes spéciales.', 'renown', 1, 4),
  ('bat', 'caravanier', 'compagnon', 'Bât bien arrimé', 'Ta cale gagne 15 places.', 'capacity', 15, 5),
  ('marchandage', 'caravanier', 'compagnon', 'Marchandage', 'Tu achètes 3 % moins cher et vends 3 % plus cher au marché.', 'haggle', 0.03, 6),
  ('raccourcis', 'caravanier', 'maitre', 'Raccourcis', 'Tes trajets durent 10 % de moins.', 'swift', 0.1, 7),
  ('eclaireur', 'caravanier', 'maitre', 'Éclaireur', 'Deux fois moins d''événements sur la route.', 'scout', 0.5, 8),
  ('reseau', 'negociant', 'compagnon', 'Réseau', 'Tu peux ouvrir une succursale de plus.', 'network', 1, 9),
  ('negoce', 'negociant', 'compagnon', 'Sens du négoce', 'Tu achètes 3 % moins cher et vends 3 % plus cher au marché.', 'haggle', 0.03, 10),
  ('logistique', 'negociant', 'maitre', 'Logistique', 'Le transporteur du jeu te coûte 30 % de moins.', 'logistics', 0.3, 11),
  ('grands_entrepots', 'negociant', 'maitre', 'Grands entrepôts', 'Chacun de tes entrepôts gagne 40 places.', 'storage', 40, 12)
on conflict (id) do update set
  family = excluded.family,
  tier = excluded.tier,
  name = excluded.name,
  description = excluded.description,
  effect = excluded.effect,
  amount = excluded.amount,
  sort_order = excluded.sort_order;

create table if not exists public.fittings (
  id text primary key,
  name text not null,
  description text not null,
  effect text not null,
  amount numeric not null,
  sort_order smallint not null
);

create table if not exists public.fitting_costs (
  fitting_id text not null references public.fittings (id),
  good_id text not null references public.goods (id),
  quantity integer not null check (quantity > 0),
  primary key (fitting_id, good_id)
);

insert into public.fittings (id, name, description, effect, amount, sort_order) values
  ('bachage', 'Bâchage', 'Une bâche huilée : l''orage ne mouille plus rien et l''abri dure deux fois moins.', 'weatherproof', 1, 1),
  ('roues_cerclees', 'Roues cerclées', 'Des roues cerclées de fer : tes trajets durent 10 % de moins.', 'swift', 0.1, 2),
  ('coffre', 'Coffre ferré', 'Les bandits ne trouvent que la moitié de ce qu''ils cherchent.', 'strongbox', 0.5, 3),
  ('pharmacie', 'Pharmacie de route', 'Remèdes et onguents pour l''équipage : les retards en route sont deux fois plus courts.', 'endurance', 0.5, 4)
on conflict (id) do update set
  name = excluded.name,
  description = excluded.description,
  effect = excluded.effect,
  amount = excluded.amount,
  sort_order = excluded.sort_order;

insert into public.fitting_costs (fitting_id, good_id, quantity) values
  ('bachage', 'baches', 4),
  ('roues_cerclees', 'roues', 4), ('roues_cerclees', 'ferrures', 8),
  ('coffre', 'outils', 3), ('coffre', 'caisses', 6),
  ('pharmacie', 'remedes', 4), ('pharmacie', 'onguent', 4)
on conflict (fitting_id, good_id) do update set quantity = excluded.quantity;

alter table public.talents enable row level security;
alter table public.fittings enable row level security;
alter table public.fitting_costs enable row level security;

drop policy if exists "Lire les talents" on public.talents;
create policy "Lire les talents" on public.talents for select to authenticated using (true);

drop policy if exists "Lire les équipements" on public.fittings;
create policy "Lire les équipements" on public.fittings for select to authenticated using (true);

drop policy if exists "Lire le prix des équipements" on public.fitting_costs;
create policy "Lire le prix des équipements" on public.fitting_costs for select to authenticated using (true);

revoke all on table public.talents, public.fittings, public.fitting_costs from anon, authenticated;
grant select on table public.talents, public.fittings, public.fitting_costs to authenticated;

create table if not exists public.directives (
  id text primary key,
  name text not null,
  description text not null,
  sort_order smallint not null
);

create table if not exists public.directive_choices (
  directive_id text not null references public.directives (id),
  kind_id text not null,
  choice_id text not null,
  primary key (directive_id, kind_id),
  foreign key (kind_id, choice_id) references public.event_choices (kind_id, id)
);

insert into public.directives (id, name, description, sort_order) values
  ('prudence', 'Prudence', 'Mettre l''équipage et la marchandise à l''abri, quitte à payer.', 1),
  ('rapidite', 'Rapidité', 'Arriver au plus vite, quitte à payer ou à mouiller un peu de marchandise.', 2),
  ('economie', 'Économie', 'Ne pas dépenser un écu, quitte à perdre du temps ou de la marchandise.', 3)
on conflict (id) do update set
  name = excluded.name,
  description = excluded.description,
  sort_order = excluded.sort_order;

insert into public.directive_choices (directive_id, kind_id, choice_id) values
  ('prudence', 'bandits', 'payer'), ('prudence', 'orage', 'abriter'), ('prudence', 'peage', 'payer'),
  ('rapidite', 'bandits', 'payer'), ('rapidite', 'orage', 'forcer'), ('rapidite', 'peage', 'payer'),
  ('economie', 'bandits', 'fuir'), ('economie', 'orage', 'abriter'), ('economie', 'peage', 'contourner')
on conflict (directive_id, kind_id) do update set choice_id = excluded.choice_id;

alter table public.directives enable row level security;
alter table public.directive_choices enable row level security;

drop policy if exists "Lire les directives" on public.directives;
create policy "Lire les directives" on public.directives for select to authenticated using (true);

drop policy if exists "Lire les choix des directives" on public.directive_choices;
create policy "Lire les choix des directives" on public.directive_choices for select to authenticated using (true);

revoke all on table public.directives, public.directive_choices from anon, authenticated;
grant select on table public.directives, public.directive_choices to authenticated;
