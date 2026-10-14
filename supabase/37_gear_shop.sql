alter table public.items
  add column if not exists role text references public.role_kinds (id);


alter table private.settings
  add column if not exists shop_kills numeric not null default 30 check (shop_kills > 0),
  add column if not exists upgrade_share numeric not null default 0.5 check (upgrade_share > 0),
  add column if not exists upgrade_growth numeric not null default 1.25 check (upgrade_growth > 1),
  add column if not exists max_upgrades integer not null default 100 check (max_upgrades > 0);

alter table public.item_rarities
  add column if not exists price numeric not null default 1 check (price > 0),
  add column if not exists buyable boolean not null default false;

update public.item_rarities set price = 1, buyable = true where id = 'common';
update public.item_rarities set price = 4, buyable = true where id = 'rare';
update public.item_rarities set price = 15, buyable = true where id = 'epic';
update public.item_rarities set price = 40, buyable = false where id = 'legendary';

alter table public.items
  add column if not exists bound boolean not null default false,
  add column if not exists upgrades integer not null default 0 check (upgrades between 0 and 100);

alter table public.items drop constraint if exists items_source_check;
alter table public.items add constraint items_source_check check (source in ('boss', 'titan', 'shop'));

alter table public.hero_equipment
  add column if not exists fighter text not null default 'hero' references public.fighter_kinds (id);

alter table public.hero_equipment drop constraint if exists hero_equipment_pkey;
alter table public.hero_equipment add constraint hero_equipment_pkey primary key (player_id, fighter, slot);

create or replace function private.pick_stat(p_slot text, p_role text, p_except text) returns text
language sql volatile
set search_path = ''
as $$
  select id from public.item_stats
  where case when p_slot = 'weapon' then id = 'damage' or p_role = any (roles) else p_slot = any (slots) end
    and id is distinct from p_except
  order by random() * (case when p_role = any (roles) then 3 else 1 end) desc
  limit 1;
$$;

create or replace function private.fighter_class(p_player uuid, p_fighter text) returns text
language sql stable
set search_path = ''
as $$
  select case
    when kind.position = 0 then frontier.hero_role
    else coalesce(fighter.role, case kind.position when 1 then 'archer' else 'mage' end)
  end
  from public.frontiers frontier
  join public.fighter_kinds kind on kind.id = p_fighter
  left join public.frontier_fighters fighter on fighter.player_id = frontier.player_id and fighter.fighter = kind.id
  where frontier.player_id = p_player;
$$;

create or replace function private.fighter_ready(p_player uuid, p_fighter text) returns boolean
language sql stable
set search_path = ''
as $$
  select coalesce((
    select kind.position = 0 or fighter.level > 0
    from public.fighter_kinds kind
    left join public.frontier_fighters fighter on fighter.player_id = p_player and fighter.fighter = kind.id
    where kind.id = p_fighter), false);
$$;

create or replace function private.gear_base(p_player uuid, p_level integer, p_rarity text) returns numeric
language sql stable
set search_path = ''
as $$
  select settings.shop_kills * (private.frontier_hp(p_level) * 0.35 + 2) * rarity.price
    * (1 - least(0.5, 0.005 * coalesce((
      select owned.level from public.frontier_plots owned where owned.player_id = p_player and owned.building = 'workshop'), 0)))
  from private.settings settings, public.item_rarities rarity
  where rarity.id = p_rarity;
$$;

create or replace function private.upgrade_price(p_player uuid, p_item public.items, p_count integer) returns numeric
language sql stable
set search_path = ''
as $$
  select private.gear_base(p_player, frontier.best_stage, p_item.rarity) * settings.upgrade_share
    * power(settings.upgrade_growth, p_item.upgrades) * (power(settings.upgrade_growth, p_count) - 1) / (settings.upgrade_growth - 1)
  from public.frontiers frontier, private.settings settings
  where frontier.player_id = p_player;
$$;

drop function if exists private.roll_item(uuid, integer, text, text);

create or replace function private.roll_item(
  p_player uuid, p_level integer, p_rarity text, p_source text, p_slot text default null, p_role text default null)
returns bigint
language plpgsql volatile
set search_path = ''
as $$
declare
  v_rarity public.item_rarities;
  v_slot text;
  v_role text;
  v_weapon text;
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

  v_slot := coalesce(p_slot, (select id from public.item_slots order by random() limit 1));
  select coalesce(p_role, hero_role) into v_role from public.frontiers where player_id = p_player;
  if v_slot = 'weapon' then
    v_weapon := case when p_role is not null or random() < 0.75 then v_role else (select id from public.role_kinds order by random() limit 1) end;
    v_role := v_weapon;
  end if;

  v_stat_a := private.pick_stat(v_slot, v_role, null);
  if v_rarity.stats > 1 then
    v_stat_b := private.pick_stat(v_slot, v_role, v_stat_a);
  end if;

  insert into public.items (owner_id, slot, rarity, item_level, stat_a, value_a, stat_b, value_b, look, source, role, bound)
  select p_player, v_slot, v_rarity.id, v_level,
    v_stat_a, round(a.base * v_rarity.factor * v_scale, 1),
    v_stat_b, round(b.base * v_rarity.factor * v_scale * 0.6, 1),
    floor(random() * 4)::smallint, p_source, v_weapon, p_source = 'shop'
  from public.item_stats a
  left join public.item_stats b on b.id = v_stat_b
  where a.id = v_stat_a
  returning id into v_item;

  return v_item;
end;
$$;

drop function if exists private.item_json(public.items, boolean);

create or replace function private.item_json(p_item public.items, p_fighter text) returns jsonb
language sql stable
set search_path = ''
as $$
  select jsonb_build_object(
    'id', p_item.id,
    'slot', p_item.slot,
    'rarity', p_item.rarity,
    'level', p_item.item_level,
    'stat_a', p_item.stat_a,
    'value_a', p_item.value_a,
    'stat_b', p_item.stat_b,
    'value_b', p_item.value_b,
    'look', p_item.look,
    'source', p_item.source,
    'role', p_item.role,
    'bound', p_item.bound,
    'upgrades', p_item.upgrades,
    'fighter', p_fighter,
    'equipped', p_fighter is not null);
$$;

create or replace function private.social_items(p_player uuid) returns jsonb
language sql stable
set search_path = ''
as $$
  select jsonb_build_object(
    'max', (select bag_size from private.settings),
    'bag', coalesce((
      select jsonb_agg(private.item_json(item, equipment.fighter)
        order by rarity.rank desc, item.item_level desc, item.id desc)
      from public.items item
      join public.item_rarities rarity on rarity.id = item.rarity
      left join public.hero_equipment equipment on equipment.item_id = item.id
      where item.owner_id = p_player), '[]'::jsonb));
$$;

update public.items item
set role = frontier.hero_role
from public.frontiers frontier
where item.slot = 'weapon' and item.role is null and frontier.player_id = item.owner_id;

alter table public.items drop constraint if exists items_weapon_role;
alter table public.items add constraint items_weapon_role check ((slot = 'weapon') = (role is not null));

drop function if exists public.equip_item(bigint);

create or replace function public.equip_item(p_item bigint, p_fighter text) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player uuid := auth.uid();
  v_item public.items;
begin
  select * into v_item from public.items where id = p_item and owner_id = v_player;
  if not found then
    return private.social_reply(v_player, 'unknown_item');
  end if;

  if not private.fighter_ready(v_player, p_fighter) then
    return private.social_reply(v_player, 'unknown_fighter');
  end if;

  if v_item.role is not null and v_item.role is distinct from private.fighter_class(v_player, p_fighter) then
    return private.social_reply(v_player, 'wrong_class');
  end if;

  delete from public.hero_equipment where item_id = v_item.id;
  insert into public.hero_equipment (player_id, fighter, slot, item_id) values (v_player, p_fighter, v_item.slot, v_item.id)
  on conflict (player_id, fighter, slot) do update set item_id = excluded.item_id;
  return private.social_reply(v_player, 'ok');
end;
$$;

drop function if exists public.unequip_item(text);

create or replace function public.unequip_item(p_slot text, p_fighter text) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player uuid := auth.uid();
begin
  delete from public.hero_equipment where player_id = v_player and fighter = p_fighter and slot = p_slot;
  return private.social_reply(v_player, case when found then 'ok' else 'unknown_item' end);
end;
$$;

create or replace function private.spent_reply(p_player uuid, p_revision integer, p_spent numeric) returns jsonb
language sql
set search_path = ''
as $$
  select private.social_reply(p_player, 'ok') || jsonb_build_object('revision', p_revision, 'spent', p_spent);
$$;

create or replace function public.buy_item(p_session uuid, p_revision integer, p_fighter text, p_slot text, p_rarity text) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player uuid := auth.uid();
  v_row public.frontiers;
  v_price numeric;
  v_item bigint;
begin
  select * into v_row from public.frontiers where player_id = v_player for update;
  if not found then
    return private.social_reply(v_player, 'missing');
  elsif v_row.session_id is distinct from p_session then
    return private.social_reply(v_player, 'elsewhere');
  elsif v_row.revision <> p_revision then
    return private.social_reply(v_player, 'conflict');
  elsif not exists (select 1 from public.item_slots where id = p_slot)
    or not exists (select 1 from public.item_rarities where id = p_rarity and buyable) then
    return private.social_reply(v_player, 'refused');
  elsif not private.fighter_ready(v_player, p_fighter) then
    return private.social_reply(v_player, 'unknown_fighter');
  end if;

  v_price := private.gear_base(v_player, v_row.max_stage, p_rarity);
  if v_row.gold < v_price then
    return private.social_reply(v_player, 'too_poor');
  end if;

  v_item := private.roll_item(v_player, v_row.max_stage, p_rarity, 'shop', p_slot, private.fighter_class(v_player, p_fighter));
  if v_item is null then
    return private.social_reply(v_player, 'bag_full');
  end if;

  update public.frontiers set gold = gold - v_price, revision = revision + 1, saved_by = null where player_id = v_player;
  insert into public.hero_equipment (player_id, fighter, slot, item_id) values (v_player, p_fighter, p_slot, v_item)
  on conflict (player_id, fighter, slot) do update set item_id = excluded.item_id;
  return private.spent_reply(v_player, v_row.revision + 1, v_price);
end;
$$;

create or replace function public.upgrade_item(p_session uuid, p_revision integer, p_item bigint, p_count integer) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player uuid := auth.uid();
  v_row public.frontiers;
  v_item public.items;
  v_price numeric;
begin
  select * into v_row from public.frontiers where player_id = v_player for update;
  if not found then
    return private.social_reply(v_player, 'missing');
  elsif v_row.session_id is distinct from p_session then
    return private.social_reply(v_player, 'elsewhere');
  elsif v_row.revision <> p_revision then
    return private.social_reply(v_player, 'conflict');
  end if;

  select * into v_item from public.items where id = p_item and owner_id = v_player for update;
  if not found then
    return private.social_reply(v_player, 'unknown_item');
  elsif coalesce(p_count, 0) < 1 then
    return private.social_reply(v_player, 'refused');
  elsif v_item.upgrades + p_count > (select max_upgrades from private.settings) then
    return private.social_reply(v_player, 'maxed');
  end if;

  v_price := private.upgrade_price(v_player, v_item, p_count);
  if v_row.gold < v_price then
    return private.social_reply(v_player, 'too_poor');
  end if;

  update public.items set upgrades = upgrades + p_count, bound = true where id = v_item.id;
  update public.frontiers set gold = gold - v_price, revision = revision + 1, saved_by = null where player_id = v_player;
  return private.spent_reply(v_player, v_row.revision + 1, v_price);
end;
$$;

create or replace function private.hero_role_changed() returns trigger
language plpgsql
security definer
set search_path = ''
as $$
begin
  delete from public.hero_equipment equipment
  using public.items item
  where equipment.player_id = new.player_id
    and equipment.fighter = 'hero'
    and item.id = equipment.item_id
    and item.role is not null
    and item.role <> new.hero_role;
  return null;
end;
$$;

create or replace function private.companion_changed() returns trigger
language plpgsql
security definer
set search_path = ''
as $$
begin
  delete from public.hero_equipment equipment
  using public.items item
  where equipment.player_id = new.player_id
    and equipment.fighter = new.fighter
    and item.id = equipment.item_id
    and item.role is not null
    and item.role <> private.fighter_class(new.player_id, new.fighter);
  return null;
end;
$$;

create or replace trigger frontier_hero_role_changed
after update of hero_role on public.frontiers
for each row when (old.hero_role is distinct from new.hero_role)
execute function private.hero_role_changed();

create or replace trigger frontier_companion_changed
after update of role on public.frontier_fighters
for each row when (new.role is not null and old.role is distinct from new.role)
execute function private.companion_changed();

delete from public.hero_equipment equipment
using public.items item, public.frontiers frontier
where item.id = equipment.item_id
  and equipment.fighter = 'hero'
  and frontier.player_id = equipment.player_id
  and item.role is not null
  and item.role <> frontier.hero_role;

revoke all on function public.equip_item(bigint, text), public.unequip_item(text, text),
  public.buy_item(uuid, integer, text, text, text), public.upgrade_item(uuid, integer, bigint, integer) from public, anon;
grant execute on function public.equip_item(bigint, text), public.unequip_item(text, text),
  public.buy_item(uuid, integer, text, text, text), public.upgrade_item(uuid, integer, bigint, integer) to authenticated;
