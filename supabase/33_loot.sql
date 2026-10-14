alter table private.settings
  add column if not exists loot_chance numeric not null default 0.3 check (loot_chance between 0 and 1),
  add column if not exists loot_bosses_per_minute integer not null default 2 check (loot_bosses_per_minute > 0),
  add column if not exists bag_size integer not null default 60 check (bag_size > 0);

create table if not exists public.item_slots (
  id text primary key,
  position smallint not null unique
);

insert into public.item_slots (id, position) values ('weapon', 0), ('armor', 1), ('helm', 2)
on conflict (id) do nothing;

create table if not exists public.item_rarities (
  id text primary key,
  rank smallint not null unique,
  odds numeric not null check (odds >= 0),
  factor numeric not null check (factor > 0),
  stats smallint not null check (stats between 1 and 2)
);

insert into public.item_rarities (id, rank, odds, factor, stats) values
  ('common', 0, 0.62, 1, 1), ('rare', 1, 0.28, 1.6, 2), ('epic', 2, 0.08, 2.5, 2), ('legendary', 3, 0.02, 4, 2)
on conflict (id) do update set odds = excluded.odds, factor = excluded.factor, stats = excluded.stats;

create table if not exists public.item_stats (
  id text primary key,
  base numeric not null check (base > 0),
  slots text[] not null,
  roles text[] not null
);

insert into public.item_stats (id, base, slots, roles) values
  ('damage', 4, '{weapon,helm}', '{warrior,mage}'),
  ('health', 6, '{armor,helm}', '{tank,support}'),
  ('critical', 2, '{weapon,helm}', '{archer,mage}'),
  ('healing', 5, '{armor,helm}', '{support}'),
  ('speed', 3, '{weapon,helm}', '{warrior,archer}')
on conflict (id) do update set base = excluded.base, slots = excluded.slots, roles = excluded.roles;

create table if not exists public.items (
  id bigint generated always as identity primary key,
  owner_id uuid not null references public.frontiers (player_id) on delete cascade,
  slot text not null references public.item_slots (id),
  rarity text not null references public.item_rarities (id),
  item_level integer not null check (item_level >= 1),
  stat_a text not null references public.item_stats (id),
  value_a numeric not null check (value_a > 0),
  stat_b text references public.item_stats (id),
  value_b numeric check (value_b is null or value_b > 0),
  look smallint not null default 0 check (look between 0 and 3),
  source text not null check (source in ('boss', 'titan')),
  created_at timestamptz not null default now(),
  check ((stat_b is null) = (value_b is null)),
  check (stat_b is null or stat_b <> stat_a)
);

create index if not exists items_by_owner on public.items (owner_id, created_at);

create table if not exists public.hero_equipment (
  player_id uuid not null references public.frontiers (player_id) on delete cascade,
  slot text not null references public.item_slots (id),
  item_id bigint not null unique references public.items (id) on delete cascade,
  primary key (player_id, slot)
);

alter table public.item_slots enable row level security;
alter table public.item_rarities enable row level security;
alter table public.item_stats enable row level security;
alter table public.items enable row level security;
alter table public.hero_equipment enable row level security;
revoke all on public.item_slots, public.item_rarities, public.item_stats, public.items, public.hero_equipment from anon, authenticated;

create or replace function private.pick_stat(p_slot text, p_role text, p_except text) returns text
language sql volatile
set search_path = ''
as $$
  select id from public.item_stats
  where p_slot = any (slots) and id is distinct from p_except
  order by random() * (case when p_role = any (roles) then 3 else 1 end) desc
  limit 1;
$$;

create or replace function private.roll_item(p_player uuid, p_level integer, p_rarity text, p_source text) returns bigint
language plpgsql volatile
set search_path = ''
as $$
declare
  v_rarity public.item_rarities;
  v_slot text;
  v_role text;
  v_level integer := greatest(1, least(coalesce(p_level, 1), 100000));
  v_scale numeric := 1 + least(v_level, 1000) / 100.0;
  v_stat_a text;
  v_stat_b text;
  v_item bigint;
begin
  if (select count(*) from public.items where owner_id = p_player) >= (select bag_size from private.settings) then
    return null;
  end if;

  if p_rarity is null then
    select rarity.* into v_rarity
    from public.item_rarities rarity, (select random() as roll) dice
    where dice.roll < (select sum(other.odds) from public.item_rarities other where other.rank >= rarity.rank)
    order by rarity.rank desc
    limit 1;
  else
    select * into v_rarity from public.item_rarities where id = p_rarity;
  end if;

  select id into v_slot from public.item_slots order by random() limit 1;
  select hero_role into v_role from public.frontiers where player_id = p_player;
  v_stat_a := private.pick_stat(v_slot, v_role, null);
  if v_rarity.stats > 1 then
    v_stat_b := private.pick_stat(v_slot, v_role, v_stat_a);
  end if;

  insert into public.items (owner_id, slot, rarity, item_level, stat_a, value_a, stat_b, value_b, look, source)
  select p_player, v_slot, v_rarity.id, v_level,
    v_stat_a, round(a.base * v_rarity.factor * v_scale, 1),
    v_stat_b, round(b.base * v_rarity.factor * v_scale * 0.6, 1),
    floor(random() * 4)::smallint, p_source
  from public.item_stats a
  left join public.item_stats b on b.id = v_stat_b
  where a.id = v_stat_a
  returning id into v_item;

  return v_item;
end;
$$;

create or replace function private.frontier_drops_loot() returns trigger
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_settings private.settings;
  v_minutes numeric := greatest(0, extract(epoch from new.saved_at - old.saved_at) / 60);
  v_bosses integer;
begin
  if new.bosses_killed <= old.bosses_killed then
    return null;
  end if;

  select * into v_settings from private.settings;
  v_bosses := least(new.bosses_killed - old.bosses_killed, floor((v_minutes + 1) * v_settings.loot_bosses_per_minute))::integer;
  for v_roll in 1 .. v_bosses loop
    if random() < v_settings.loot_chance then
      perform private.roll_item(new.player_id, new.max_stage, null, 'boss');
    end if;
  end loop;

  return null;
end;
$$;

create or replace trigger frontier_drops_loot
after update of bosses_killed on public.frontiers
for each row execute function private.frontier_drops_loot();
