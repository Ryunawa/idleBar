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
  ('onguent', 'Onguent', 80, 18, true)
on conflict (id) do update set
  name = excluded.name,
  base_price = excluded.base_price,
  sort_order = excluded.sort_order,
  crafted = excluded.crafted;

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
  ('rocheclaire', 'onguent', 1.4), ('hautecombe', 'onguent', 1.35)
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

insert into public.crafts (id, name, kind, playable, description, sort_order) values
  ('caravanier', 'Caravanier', 'itinerant', true, 'Voyage de ville en ville, commerce et transporte les marchandises des autres.', 1),
  ('forgeron', 'Forgeron', 'sedentary', true, 'Forge outils et ferrures à partir de fer et de charbon.', 2),
  ('charron', 'Charron', 'sedentary', true, 'Assemble caisses et roues avec du bois et des outils.', 3),
  ('tisserand', 'Tisserand', 'sedentary', true, 'Tisse le drap et coud les bâches avec de la laine et de la teinture.', 4),
  ('herboriste', 'Herboriste', 'sedentary', true, 'Prépare remèdes et onguents à partir d''herbes.', 5),
  ('negociant', 'Négociant', 'sedentary', false, 'Tient un comptoir et commerce sans voyager. Arrive avec les échanges entre joueurs.', 6)
on conflict (id) do update set
  name = excluded.name,
  kind = excluded.kind,
  playable = excluded.playable,
  description = excluded.description,
  sort_order = excluded.sort_order;

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
  ('onguent', 'herboriste', 'onguent', 1, 600, 2)
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
  ('onguent', 'herbes', 2), ('onguent', 'vin', 1)
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
