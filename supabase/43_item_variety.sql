alter table private.settings
  add column if not exists item_growth numeric not null default 1.019 check (item_growth > 1),
  add column if not exists item_scale numeric not null default 0.75 check (item_scale > 0),
  add column if not exists item_second_share numeric not null default 0.6 check (item_second_share > 0),
  add column if not exists item_levels_below integer not null default 5 check (item_levels_below >= 0),
  add column if not exists item_levels_above integer not null default 3 check (item_levels_above >= 0),
  add column if not exists item_rules integer not null default 1;

alter table public.item_rarities
  add column if not exists roll_min numeric not null default 1 check (roll_min > 0),
  add column if not exists roll_max numeric not null default 1 check (roll_max > 0);

alter table public.item_rarities drop constraint if exists item_rarities_roll_band;
alter table public.item_rarities add constraint item_rarities_roll_band check (roll_max >= roll_min);

update public.item_rarities set roll_min = 0.90, roll_max = 1.10, factor = 1 where id = 'common';
update public.item_rarities set roll_min = 1.01, roll_max = 1.23, factor = 1.12 where id = 'rare';
update public.item_rarities set roll_min = 1.13, roll_max = 1.38, factor = 1.255 where id = 'epic';
update public.item_rarities set roll_min = 1.26, roll_max = 1.55, factor = 1.405 where id = 'legendary';

create table if not exists public.rarity_odds (
  source text not null check (source in ('boss', 'shop')),
  rarity text not null references public.item_rarities (id),
  percent numeric not null check (percent between 0 and 100),
  primary key (source, rarity)
);

alter table public.rarity_odds enable row level security;
revoke all on public.rarity_odds from anon, authenticated;

insert into public.rarity_odds (source, rarity, percent) values
  ('boss', 'common', 62), ('boss', 'rare', 28), ('boss', 'epic', 8), ('boss', 'legendary', 2),
  ('shop', 'common', 55), ('shop', 'rare', 32), ('shop', 'epic', 10), ('shop', 'legendary', 3)
on conflict (source, rarity) do update set percent = excluded.percent;

insert into public.item_stats (id, base, slots, roles) values
  ('damage', 4, '{weapon,helm}', '{warrior,mage}'),
  ('health', 6, '{armor,helm}', '{tank,support}'),
  ('critical', 2, '{weapon,helm}', '{archer,mage}'),
  ('healing', 5, '{armor,helm}', '{support}'),
  ('speed', 3, '{weapon,helm}', '{warrior,archer}'),
  ('gold', 6, '{armor,helm}', '{warrior}'),
  ('xp', 10, '{armor,helm}', '{tank,support}'),
  ('recharge', 4, '{weapon,helm}', '{tank,archer,mage}'),
  ('boss', 6, '{weapon,helm}', '{warrior,mage}')
on conflict (id) do update set base = excluded.base, slots = excluded.slots, roles = excluded.roles;

alter table public.items add column if not exists stage integer check (stage is null or stage >= 1);
alter table public.shop_offers add column if not exists stage integer check (stage is null or stage >= 1);
alter table public.items drop constraint if exists items_look_check;
alter table public.items add constraint items_look_check check (look between 0 and 7);
alter table public.shop_offers drop constraint if exists shop_offers_look_check;
alter table public.shop_offers add constraint shop_offers_look_check check (look between 0 and 7);

create or replace function private.stat_value(p_stat text, p_rarity text, p_level integer, p_roll double precision, p_second boolean default false)
returns numeric
language sql stable
set search_path = ''
as $$
  select round(stat.base * settings.item_scale * power(settings.item_growth, least(greatest(p_level, 1), 1000) - 1)
    * (rarity.roll_min + (rarity.roll_max - rarity.roll_min) * least(greatest(p_roll, 0), 1)::numeric)
    * case when p_second then settings.item_second_share else 1 end, 2)
  from public.item_stats stat, public.item_rarities rarity, private.settings settings
  where stat.id = p_stat and rarity.id = p_rarity;
$$;

create or replace function private.top_level(p_player uuid) returns integer
language sql stable
set search_path = ''
as $$
  select least(greatest(1, coalesce(max(fighter.level), 1)), max(frontier.best_stage) + 10)
  from public.frontiers frontier
  left join public.frontier_fighters fighter on fighter.player_id = frontier.player_id
  where frontier.player_id = p_player;
$$;

create or replace function private.fighter_level(p_player uuid, p_fighter text) returns integer
language sql stable
set search_path = ''
as $$
  select coalesce((select fighter.level from public.frontier_fighters fighter where fighter.player_id = p_player and fighter.fighter = p_fighter), 0);
$$;

create or replace function private.item_level(p_player uuid) returns integer
language sql volatile
set search_path = ''
as $$
  select greatest(1, private.top_level(p_player) - settings.item_levels_below
    + floor(random() * (settings.item_levels_below + settings.item_levels_above + 1))::integer)
  from private.settings settings;
$$;

create or replace function private.roll_rarity(p_source text) returns text
language sql volatile
set search_path = ''
as $$
  select coalesce((
    select odds.rarity
    from public.rarity_odds odds
    join public.item_rarities rarity on rarity.id = odds.rarity
    cross join (select random() * 100 as roll) dice
    where odds.source = p_source
      and dice.roll < (
        select sum(other.percent) from public.rarity_odds other
        join public.item_rarities lower_rarity on lower_rarity.id = other.rarity
        where other.source = p_source and lower_rarity.rank <= rarity.rank)
    order by rarity.rank
    limit 1), 'common');
$$;

create or replace function private.odds_json(p_source text) returns jsonb
language sql stable
set search_path = ''
as $$
  select coalesce(jsonb_agg(jsonb_build_object('rarity', odds.rarity, 'percent', odds.percent) order by rarity.rank), '[]'::jsonb)
  from public.rarity_odds odds
  join public.item_rarities rarity on rarity.id = odds.rarity
  where odds.source = p_source;
$$;

create or replace function private.pick_stat(p_slot text, p_role text, p_except text) returns text
language sql volatile
set search_path = ''
as $$
  select id from public.item_stats
  where case when p_slot = 'weapon' then id = 'damage' or p_role = any (roles) else p_slot = any (slots) end
    and id is distinct from p_except
  order by random()
  limit 1;
$$;

create or replace function private.roll_item(
  p_player uuid, p_level integer, p_rarity text, p_source text, p_slot text default null, p_role text default null)
returns bigint
language plpgsql volatile
set search_path = ''
as $$
declare
  v_rarity text := coalesce(p_rarity, private.roll_rarity(case when p_source = 'shop' then 'shop' else 'boss' end));
  v_level integer := case when p_level is null then private.item_level(p_player) else greatest(1, least(p_level, 100000)) end;
  v_slot text := coalesce(p_slot, (select id from public.item_slots order by random() limit 1));
  v_role text;
  v_stat_a text;
  v_stat_b text;
  v_item bigint;
begin
  if private.bag_count(p_player) >= (select bag_size from private.settings) then
    return null;
  end if;

  if v_slot = 'weapon' then
    v_role := coalesce(p_role, (select id from public.role_kinds order by random() limit 1));
  end if;

  v_stat_a := private.pick_stat(v_slot, v_role, null);
  if (select stats from public.item_rarities where id = v_rarity) > 1 then
    v_stat_b := private.pick_stat(v_slot, v_role, v_stat_a);
  end if;

  insert into public.items (owner_id, slot, rarity, item_level, stat_a, value_a, stat_b, value_b, look, source, role, bound, stage)
  values (p_player, v_slot, v_rarity, v_level,
    v_stat_a, private.stat_value(v_stat_a, v_rarity, v_level, random()),
    v_stat_b, case when v_stat_b is not null then private.stat_value(v_stat_b, v_rarity, v_level, random(), true) end,
    floor(random() * 8)::smallint, p_source, v_role, p_source = 'shop',
    (select greatest(1, frontier.max_stage) from public.frontiers frontier where frontier.player_id = p_player))
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
  v_bosses integer;
begin
  if new.bosses_killed <= old.bosses_killed then
    return null;
  end if;

  select * into v_settings from private.settings;
  v_bosses := private.spend_credit(new.player_id, false, new.bosses_killed - old.bosses_killed, v_settings.loot_bosses_per_minute);
  for v_roll in 1 .. v_bosses loop
    if random() < v_settings.loot_chance and private.roll_item(new.player_id, null, null, 'boss') is null then
      exit;
    end if;
  end loop;

  return null;
end;
$$;

create or replace function public.claim_titan() returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player uuid := auth.uid();
  v_claim public.titan_contributions;
  v_reward integer[];
begin
  select * into v_claim from private.titan_claimable(v_player);
  if v_claim.week is null then
    return private.social_reply(v_player, 'nothing');
  end if;

  if private.bag_count(v_player) >= (select bag_size from private.settings) then
    return private.social_reply(v_player, 'bag_full');
  end if;

  v_reward := private.titan_reward(v_claim.monsters);
  update public.titan_contributions
  set claimed_at = now(),
      boost_percent = v_reward[1],
      boost_ends_at = now() + make_interval(hours => v_reward[2])
  where week = v_claim.week and player_id = v_player and claimed_at is null;
  if not found then
    return private.social_reply(v_player, 'nothing');
  end if;

  perform private.roll_item(v_player, null,
    case when v_reward[1] >= 100 then 'legendary' when v_reward[1] >= 50 then 'epic' else 'rare' end, 'titan');
  return private.social_reply(v_player, 'ok');
end;
$$;

create or replace function private.stock_shop(p_player uuid) returns void
language plpgsql volatile
set search_path = ''
as $$
declare
  v_opened timestamptz := private.shop_opened();
  v_frontier public.frontiers;
  v_rarity text;
  v_slot text;
  v_role text;
  v_level integer;
  v_stat_a text;
  v_stat_b text;
begin
  select * into v_frontier from public.frontiers where player_id = p_player;
  if not found or exists (select 1 from public.shop_offers where player_id = p_player and opened_at = v_opened) then
    return;
  end if;

  delete from public.shop_offers where player_id = p_player and opened_at < v_opened;
  for v_position in 0 .. (select shop_offers from private.settings) - 1 loop
    v_rarity := private.roll_rarity('shop');
    v_level := private.item_level(p_player);
    select id into v_slot from public.item_slots order by random() limit 1;
    v_role := case when v_slot = 'weapon' then (select id from public.role_kinds order by random() limit 1) end;
    v_stat_a := private.pick_stat(v_slot, v_role, null);
    v_stat_b := case when (select stats from public.item_rarities where id = v_rarity) > 1 then private.pick_stat(v_slot, v_role, v_stat_a) end;
    insert into public.shop_offers (player_id, opened_at, position, slot, rarity, item_level, stat_a, value_a, stat_b, value_b, look, role, price, stage)
    values (p_player, v_opened, v_position, v_slot, v_rarity, v_level,
      v_stat_a, private.stat_value(v_stat_a, v_rarity, v_level, random()),
      v_stat_b, case when v_stat_b is not null then private.stat_value(v_stat_b, v_rarity, v_level, random(), true) end,
      floor(random() * 8)::smallint, v_role,
      private.gear_base(p_player, v_frontier.max_stage, v_rarity), greatest(1, v_frontier.max_stage))
    on conflict (player_id, opened_at, position) do nothing;
  end loop;
end;
$$;

create or replace function private.social_shop(p_player uuid) returns jsonb
language plpgsql
set search_path = ''
as $$
begin
  perform private.stock_shop(p_player);
  return jsonb_build_object(
    'refresh_in', greatest(0, extract(epoch from private.shop_opened()
      + make_interval(hours => (select shop_window_hours from private.settings)) - now())),
    'odds', private.odds_json('shop'),
    'offers', coalesce((
      select jsonb_agg(jsonb_build_object(
          'id', offer.id,
          'slot', offer.slot,
          'rarity', offer.rarity,
          'level', offer.item_level,
          'stat_a', offer.stat_a,
          'value_a', offer.value_a,
          'stat_b', offer.stat_b,
          'value_b', offer.value_b,
          'look', offer.look,
          'role', offer.role,
          'price', offer.price,
          'sold', offer.sold_at is not null)
        order by offer.position)
      from public.shop_offers offer
      where offer.player_id = p_player and offer.opened_at = private.shop_opened()), '[]'::jsonb));
end;
$$;

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
    'stage', coalesce(p_item.stage, p_item.item_level),
    'fighter', p_fighter,
    'equipped', p_fighter is not null);
$$;

create or replace function public.buy_offer(p_session uuid, p_revision integer, p_offer bigint) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player uuid := auth.uid();
  v_row public.frontiers;
  v_offer public.shop_offers;
begin
  select * into v_row from public.frontiers where player_id = v_player for update;
  if not found then
    return private.social_reply(v_player, 'missing');
  elsif v_row.session_id is distinct from p_session then
    return private.social_reply(v_player, 'elsewhere');
  elsif v_row.revision <> p_revision then
    return private.social_reply(v_player, 'conflict');
  end if;

  select * into v_offer from public.shop_offers where id = p_offer and player_id = v_player for update;
  if not found or v_offer.opened_at <> private.shop_opened() then
    return private.social_reply(v_player, 'unknown_offer');
  elsif v_offer.sold_at is not null then
    return private.social_reply(v_player, 'sold');
  elsif private.bag_count(v_player) >= (select bag_size from private.settings) then
    return private.social_reply(v_player, 'bag_full');
  elsif v_row.gold < v_offer.price then
    return private.social_reply(v_player, 'too_poor');
  end if;

  insert into public.items (owner_id, slot, rarity, item_level, stat_a, value_a, stat_b, value_b, look, source, role, bound, stage)
  values (v_player, v_offer.slot, v_offer.rarity, v_offer.item_level, v_offer.stat_a, v_offer.value_a, v_offer.stat_b,
    v_offer.value_b, v_offer.look, 'shop', v_offer.role, true, coalesce(v_offer.stage, v_row.max_stage));
  update public.shop_offers set sold_at = now() where id = v_offer.id;
  update public.frontiers set gold = gold - v_offer.price, revision = revision + 1, saved_by = null where player_id = v_player;
  return private.spent_reply(v_player, v_row.revision + 1, v_offer.price);
end;
$$;

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

  if v_item.item_level > private.fighter_level(v_player, p_fighter) then
    return private.social_reply(v_player, 'level_too_low');
  end if;

  delete from public.hero_equipment where item_id = v_item.id;
  insert into public.hero_equipment (player_id, fighter, slot, item_id) values (v_player, p_fighter, v_item.slot, v_item.id)
  on conflict (player_id, fighter, slot) do update set item_id = excluded.item_id;
  return private.social_reply(v_player, 'ok');
end;
$$;

revoke all on function public.equip_item(bigint, text), public.buy_offer(uuid, integer, bigint), public.claim_titan() from public, anon;
grant execute on function public.equip_item(bigint, text), public.buy_offer(uuid, integer, bigint), public.claim_titan() to authenticated;

create or replace function private.stage_level(p_stage integer) returns integer
language sql immutable
set search_path = ''
as $$
  select greatest(1, round(0.7 * greatest(p_stage, 1) + 3)::integer);
$$;

create or replace function private.rescale_items() returns void
language plpgsql
set search_path = ''
as $$
begin
  if (select item_rules from private.settings) >= 2 then
    return;
  end if;

  update public.items item
  set stage = item.item_level,
      item_level = private.stage_level(item.item_level),
      value_a = private.stat_value(item.stat_a, item.rarity, private.stage_level(item.item_level), 0.5),
      value_b = case when item.stat_b is not null
        then private.stat_value(item.stat_b, item.rarity, private.stage_level(item.item_level), 0.5, true) end
  where item.stage is null;
  update public.shop_offers offer
  set stage = offer.item_level,
      item_level = private.stage_level(offer.item_level),
      value_a = private.stat_value(offer.stat_a, offer.rarity, private.stage_level(offer.item_level), 0.5),
      value_b = case when offer.stat_b is not null
        then private.stat_value(offer.stat_b, offer.rarity, private.stage_level(offer.item_level), 0.5, true) end
  where offer.stage is null;
  update private.settings set item_rules = 2;
end;
$$;

select private.rescale_items();

update private.settings set min_client_version = '0.9.0';
