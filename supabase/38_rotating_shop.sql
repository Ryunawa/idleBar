alter table private.settings
  add column if not exists shop_window_hours integer not null default 3 check (shop_window_hours between 1 and 24),
  add column if not exists shop_offers integer not null default 4 check (shop_offers between 1 and 12);

create table if not exists public.shop_offers (
  id bigint generated always as identity primary key,
  player_id uuid not null references public.frontiers (player_id) on delete cascade,
  opened_at timestamptz not null,
  position smallint not null check (position >= 0),
  slot text not null references public.item_slots (id),
  rarity text not null references public.item_rarities (id),
  item_level integer not null check (item_level >= 1),
  stat_a text not null references public.item_stats (id),
  value_a numeric not null check (value_a > 0),
  stat_b text references public.item_stats (id),
  value_b numeric check (value_b is null or value_b > 0),
  look smallint not null check (look between 0 and 3),
  role text references public.role_kinds (id),
  price numeric not null check (price > 0),
  sold_at timestamptz,
  unique (player_id, opened_at, position),
  check ((slot = 'weapon') = (role is not null)),
  check ((stat_b is null) = (value_b is null))
);

alter table public.shop_offers enable row level security;
revoke all on public.shop_offers from anon, authenticated;

create or replace function private.shop_opened() returns timestamptz
language sql stable
set search_path = ''
as $$
  select date_trunc('day', now() at time zone 'utc') at time zone 'utc'
    + make_interval(hours => extract(hour from now() at time zone 'utc')::integer / settings.shop_window_hours * settings.shop_window_hours)
  from private.settings settings;
$$;

create or replace function private.stock_shop(p_player uuid) returns void
language plpgsql volatile
set search_path = ''
as $$
declare
  v_opened timestamptz := private.shop_opened();
  v_frontier public.frontiers;
  v_rarity public.item_rarities;
  v_slot text;
  v_role text;
  v_stat_a text;
  v_stat_b text;
  v_scale numeric;
begin
  select * into v_frontier from public.frontiers where player_id = p_player;
  if not found or exists (select 1 from public.shop_offers where player_id = p_player and opened_at = v_opened) then
    return;
  end if;

  delete from public.shop_offers where player_id = p_player and opened_at < v_opened;
  v_scale := 1 + least(v_frontier.max_stage, 1000) / 100.0;
  for v_position in 0 .. (select shop_offers from private.settings) - 1 loop
    select rarity.* into v_rarity
    from public.item_rarities rarity, (select random() as roll) dice
    where dice.roll < (select sum(other.odds) from public.item_rarities other where other.rank >= rarity.rank)
    order by rarity.rank desc
    limit 1;

    select id into v_slot from public.item_slots order by random() limit 1;
    v_role := v_frontier.hero_role;
    if v_slot = 'weapon' then
      v_role := case
        when random() < 0.75 then (
          select private.fighter_class(p_player, kind.id) from public.fighter_kinds kind
          where private.fighter_ready(p_player, kind.id) order by random() limit 1)
        else (select id from public.role_kinds order by random() limit 1)
      end;
    end if;

    v_stat_a := private.pick_stat(v_slot, v_role, null);
    v_stat_b := case when v_rarity.stats > 1 then private.pick_stat(v_slot, v_role, v_stat_a) end;
    insert into public.shop_offers (player_id, opened_at, position, slot, rarity, item_level, stat_a, value_a, stat_b, value_b, look, role, price)
    select p_player, v_opened, v_position, v_slot, v_rarity.id, v_frontier.max_stage,
      v_stat_a, round(a.base * v_rarity.factor * v_scale, 1),
      v_stat_b, round(b.base * v_rarity.factor * v_scale * 0.6, 1),
      floor(random() * 4)::smallint, case when v_slot = 'weapon' then v_role end,
      private.gear_base(p_player, v_frontier.max_stage, v_rarity.id)
    from public.item_stats a
    left join public.item_stats b on b.id = v_stat_b
    where a.id = v_stat_a
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
    'items', private.social_items(p_player),
    'shop', private.social_shop(p_player));
end;
$$;

drop function if exists public.buy_item(uuid, integer, text, text, text);

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
  elsif (select count(*) from public.items where owner_id = v_player) >= (select bag_size from private.settings) then
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

revoke all on function public.buy_offer(uuid, integer, bigint) from public, anon;
grant execute on function public.buy_offer(uuid, integer, bigint) to authenticated;

update private.settings set min_client_version = '0.8.1';
