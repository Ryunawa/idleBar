create or replace function private.bag_count(p_player uuid) returns integer
language sql stable
set search_path = ''
as $$
  select count(*)::integer from public.items item
  where item.owner_id = p_player
    and not exists (select 1 from public.hero_equipment equipment where equipment.item_id = item.id);
$$;

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
  if private.bag_count(p_player) >= (select bag_size from private.settings) then
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

create or replace function public.claim_titan() returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player uuid := auth.uid();
  v_claim public.titan_contributions;
  v_reward integer[];
  v_level integer;
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

  select best_stage into v_level from public.frontiers where player_id = v_player;
  perform private.roll_item(v_player, v_level,
    case when v_reward[1] >= 100 then 'legendary' when v_reward[1] >= 50 then 'epic' else 'rare' end, 'titan');
  return private.social_reply(v_player, 'ok');
end;
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

  insert into public.items (owner_id, slot, rarity, item_level, stat_a, value_a, stat_b, value_b, look, source, role, bound)
  values (v_player, v_offer.slot, v_offer.rarity, v_offer.item_level, v_offer.stat_a, v_offer.value_a, v_offer.stat_b,
    v_offer.value_b, v_offer.look, 'shop', v_offer.role, true);
  update public.shop_offers set sold_at = now() where id = v_offer.id;
  update public.frontiers set gold = gold - v_offer.price, revision = revision + 1, saved_by = null where player_id = v_player;
  return private.spent_reply(v_player, v_row.revision + 1, v_offer.price);
end;
$$;

create or replace function public.unequip_item(p_slot text, p_fighter text) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player uuid := auth.uid();
begin
  if not exists (select 1 from public.hero_equipment where player_id = v_player and fighter = p_fighter and slot = p_slot) then
    return private.social_reply(v_player, 'unknown_item');
  end if;

  if private.bag_count(v_player) >= (select bag_size from private.settings) then
    return private.social_reply(v_player, 'bag_full');
  end if;

  delete from public.hero_equipment where player_id = v_player and fighter = p_fighter and slot = p_slot;
  return private.social_reply(v_player, 'ok');
end;
$$;
