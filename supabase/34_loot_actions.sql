create or replace function private.item_json(p_item public.items, p_equipped boolean) returns jsonb
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
    'equipped', p_equipped);
$$;

create or replace function private.social_items(p_player uuid) returns jsonb
language sql stable
set search_path = ''
as $$
  select jsonb_build_object(
    'max', (select bag_size from private.settings),
    'bag', coalesce((
      select jsonb_agg(private.item_json(item, equipment.item_id is not null)
        order by rarity.rank desc, item.item_level desc, item.id desc)
      from public.items item
      join public.item_rarities rarity on rarity.id = item.rarity
      left join public.hero_equipment equipment on equipment.item_id = item.id
      where item.owner_id = p_player), '[]'::jsonb));
$$;

create or replace function private.social_reply(p_player uuid, p_status text) returns jsonb
language plpgsql
set search_path = ''
as $$
begin
  return jsonb_build_object(
    'status', p_status,
    'server_time', now(),
    'guild', private.social_guild(p_player),
    'rejoin_in', coalesce((
      select greatest(0, extract(epoch from departure.left_at + make_interval(mins => settings.guild_rejoin_minutes) - now()))
      from public.guild_departures departure, private.settings settings
      where departure.player_id = p_player), 0),
    'help', private.social_help(p_player),
    'titan', private.social_titan(p_player, private.current_titan()),
    'items', private.social_items(p_player));
end;
$$;

create or replace function public.equip_item(p_item bigint) returns jsonb
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

  insert into public.hero_equipment (player_id, slot, item_id) values (v_player, v_item.slot, v_item.id)
  on conflict (player_id, slot) do update set item_id = excluded.item_id;
  return private.social_reply(v_player, 'ok');
end;
$$;

create or replace function public.unequip_item(p_slot text) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player uuid := auth.uid();
begin
  delete from public.hero_equipment where player_id = v_player and slot = p_slot;
  return private.social_reply(v_player, case when found then 'ok' else 'unknown_item' end);
end;
$$;

create or replace function public.salvage_item(p_item bigint) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player uuid := auth.uid();
begin
  delete from public.items where id = p_item and owner_id = v_player;
  return private.social_reply(v_player, case when found then 'ok' else 'unknown_item' end);
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

revoke all on function public.equip_item(bigint), public.unequip_item(text), public.salvage_item(bigint) from public, anon;
grant execute on function public.equip_item(bigint), public.unequip_item(text), public.salvage_item(bigint) to authenticated;
