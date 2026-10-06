drop function if exists public.found_caravan(text);

create or replace function public.get_world() returns jsonb
language sql stable
security definer
set search_path = ''
as $$
  select jsonb_build_object(
    'goods', (
      select coalesce(jsonb_agg(jsonb_build_object(
        'id', id, 'name', name, 'base_price', base_price, 'crafted', crafted
      ) order by sort_order), '[]'::jsonb)
      from public.goods
    ),
    'towns', (
      select coalesce(jsonb_agg(jsonb_build_object(
        'id', towns.id,
        'name', towns.name,
        'biome', towns.biome,
        'map_x', towns.map_x,
        'map_y', towns.map_y,
        'produces', (
          select coalesce(jsonb_agg(markets.good_id order by goods.sort_order), '[]'::jsonb)
          from public.markets
          join public.goods on goods.id = markets.good_id
          where markets.town_id = towns.id and markets.base_price < goods.base_price * 0.8
        ),
        'demands', (
          select coalesce(jsonb_agg(markets.good_id order by goods.sort_order), '[]'::jsonb)
          from public.markets
          join public.goods on goods.id = markets.good_id
          where markets.town_id = towns.id and markets.base_price > goods.base_price * 1.3
        )
      ) order by towns.name), '[]'::jsonb)
      from public.towns
    ),
    'routes', (
      select coalesce(jsonb_agg(jsonb_build_object(
        'from_town_id', from_town_id,
        'to_town_id', to_town_id,
        'seconds', round(minutes * 60 * (select travel_time_factor from private.settings)),
        'biome', biome
      ) order by from_town_id, minutes), '[]'::jsonb)
      from public.routes
    ),
    'crafts', (
      select coalesce(jsonb_agg(jsonb_build_object(
        'id', id, 'name', name, 'kind', kind, 'playable', playable, 'description', description
      ) order by sort_order), '[]'::jsonb)
      from public.crafts
    ),
    'recipes', (
      select coalesce(jsonb_agg(jsonb_build_object(
        'id', recipes.id,
        'craft_id', recipes.craft_id,
        'output_good_id', recipes.output_good_id,
        'output_quantity', recipes.output_quantity,
        'seconds', round(recipes.seconds * (select craft_time_factor from private.settings)),
        'inputs', (
          select coalesce(jsonb_agg(jsonb_build_object('good_id', good_id, 'quantity', quantity) order by good_id), '[]'::jsonb)
          from public.recipe_inputs
          where recipe_inputs.recipe_id = recipes.id
        )
      ) order by recipes.craft_id, recipes.sort_order), '[]'::jsonb)
      from public.recipes
    )
  );
$$;

create or replace function public.get_state() returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player public.players;
  v_caravan public.caravans;
  v_workshop public.workshops;
  v_settings private.settings;
  v_market_town text;
begin
  if auth.uid() is null then
    raise exception 'Connexion requise.';
  end if;

  select * into v_player from public.players where player_id = auth.uid();
  if not found then
    return jsonb_build_object(
      'server_time', now(), 'player', null, 'caravan', null, 'cargo', '[]'::jsonb,
      'workshop', null, 'storage', '[]'::jsonb, 'market', '[]'::jsonb);
  end if;

  if exists (select 1 from public.workshops where player_id = v_player.player_id) then
    v_workshop := private.lock_workshop();
  end if;

  select * into v_caravan from public.caravans where player_id = v_player.player_id;
  select * into v_settings from private.settings;
  v_market_town := case
    when v_caravan.player_id is not null and v_caravan.arrives_at > now() then null
    when v_caravan.player_id is not null then v_caravan.town_id
    else v_workshop.town_id
  end;

  return jsonb_build_object(
    'server_time', now(),
    'player', jsonb_build_object(
      'name', v_player.name,
      'craft_id', v_player.craft_id,
      'home_town_id', v_player.home_town_id,
      'coins', v_player.coins
    ),
    'caravan', case when v_caravan.player_id is null then null else jsonb_build_object(
      'wagons', v_caravan.wagons,
      'capacity', private.capacity(v_caravan.wagons),
      'next_wagon_price', case when v_caravan.wagons < v_settings.max_wagons then private.wagon_price(v_caravan.wagons + 1) end,
      'town_id', v_caravan.town_id,
      'from_town_id', v_caravan.from_town_id,
      'departed_at', v_caravan.departed_at,
      'arrives_at', v_caravan.arrives_at
    ) end,
    'cargo', (
      select coalesce(jsonb_agg(jsonb_build_object('good_id', cargo.good_id, 'quantity', cargo.quantity) order by goods.sort_order), '[]'::jsonb)
      from public.cargo
      join public.goods on goods.id = cargo.good_id
      where cargo.player_id = v_player.player_id
    ),
    'workshop', case when v_workshop.player_id is null then null else jsonb_build_object(
      'town_id', v_workshop.town_id,
      'level', v_workshop.level,
      'speed', private.workshop_speed(v_workshop.level),
      'storage_capacity', private.storage_capacity(v_workshop.level),
      'max_queue', private.max_queue(v_workshop.level),
      'next_level_price', case when v_workshop.level < v_settings.max_workshop_level then private.workshop_price(v_workshop.level + 1) end,
      'recipe_id', v_workshop.recipe_id,
      'queued', v_workshop.queued,
      'batch_seconds', v_workshop.batch_seconds,
      'started_at', v_workshop.started_at
    ) end,
    'storage', (
      select coalesce(jsonb_agg(jsonb_build_object('good_id', warehouses.good_id, 'quantity', warehouses.quantity) order by goods.sort_order), '[]'::jsonb)
      from public.warehouses
      join public.goods on goods.id = warehouses.good_id
      where warehouses.player_id = v_player.player_id and warehouses.town_id = v_player.home_town_id
    ),
    'market', case when v_market_town is null then '[]'::jsonb else (
      select coalesce(jsonb_agg(jsonb_build_object(
        'good_id', markets.good_id,
        'buy_price', private.buy_quote(markets.base_price, private.current_pressure(markets.pressure, markets.pressure_at)),
        'sell_price', private.sell_quote(markets.base_price, private.current_pressure(markets.pressure, markets.pressure_at))
      ) order by goods.sort_order), '[]'::jsonb)
      from public.markets
      join public.goods on goods.id = markets.good_id
      where markets.town_id = v_market_town
    ) end
  );
end;
$$;

create or replace function public.found_player(p_name text, p_craft_id text, p_town_id text) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_name text := btrim(coalesce(p_name, ''));
  v_craft public.crafts;
begin
  if auth.uid() is null then
    raise exception 'Connexion requise.';
  end if;

  if char_length(v_name) not between 3 and 20 or v_name ~ '[[:cntrl:]]' then
    raise exception 'Le nom doit faire entre 3 et 20 caractères.';
  end if;

  select * into v_craft from public.crafts where id = p_craft_id;
  if not found or not v_craft.playable then
    raise exception 'Ce métier n''est pas encore disponible.';
  end if;

  if not exists (select 1 from public.towns where id = p_town_id) then
    raise exception 'Cette ville n''existe pas.';
  end if;

  if exists (select 1 from public.players where player_id = auth.uid()) then
    raise exception 'Tu es déjà installé.';
  end if;

  begin
    insert into public.players (player_id, name, craft_id, home_town_id, coins)
    values (auth.uid(), v_name, v_craft.id, p_town_id, (select starting_coins from private.settings));
  exception when unique_violation then
    raise exception 'Ce nom est déjà pris.';
  end;

  if v_craft.kind = 'itinerant' then
    insert into public.caravans (player_id, town_id) values (auth.uid(), p_town_id);
  else
    insert into public.workshops (player_id, town_id) values (auth.uid(), p_town_id);
  end if;

  return public.get_state();
end;
$$;

revoke all on function public.get_world(), public.get_state(), public.found_player(text, text, text) from public, anon;
grant execute on function public.get_world(), public.get_state(), public.found_player(text, text, text) to authenticated;
