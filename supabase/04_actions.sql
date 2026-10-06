drop function if exists public.found_caravan(text);

create or replace function public.get_world() returns jsonb
language sql stable
security definer
set search_path = ''
as $$
  select jsonb_build_object(
    'goods', (
      select coalesce(jsonb_agg(jsonb_build_object(
        'id', id, 'name', name, 'base_price', base_price, 'crafted', crafted, 'market_sells', market_sells
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
        'id', id, 'name', name, 'kind', kind, 'playable', playable, 'description', description, 'opens_branches', opens_branches,
        'family', family
      ) order by sort_order), '[]'::jsonb)
      from public.crafts
    ),
    'journeys', (
      select coalesce(jsonb_agg(jsonb_build_object(
        'from_town_id', journeys.from_town_id,
        'to_town_id', journeys.to_town_id,
        'minutes', journeys.minutes,
        'seconds', round(journeys.minutes * 60 * (select travel_time_factor from private.settings))
      ) order by journeys.from_town_id, journeys.minutes), '[]'::jsonb)
      from (
        select origins.id as from_town_id, destinations.id as to_town_id, private.journey_minutes(origins.id, destinations.id) as minutes
        from public.towns origins
        cross join public.towns destinations
        where origins.id <> destinations.id
      ) journeys
    ),
    'event_kinds', (
      select coalesce(jsonb_agg(jsonb_build_object(
        'id', kinds.id,
        'scope', kinds.scope,
        'name', kinds.name,
        'description', kinds.description,
        'choices', (
          select coalesce(jsonb_agg(jsonb_build_object(
            'id', choices.id, 'name', choices.name, 'description', choices.description, 'is_default', choices.is_default
          ) order by choices.sort_order), '[]'::jsonb)
          from public.event_choices choices
          where choices.kind_id = kinds.id
        )
      ) order by kinds.sort_order), '[]'::jsonb)
      from public.event_kinds kinds
    ),
    'directives', (
      select coalesce(jsonb_agg(jsonb_build_object(
        'id', directives.id,
        'name', directives.name,
        'description', directives.description,
        'choices', (
          select coalesce(jsonb_agg(jsonb_build_object('kind_id', kind_id, 'choice_id', choice_id) order by kind_id), '[]'::jsonb)
          from public.directive_choices where directive_id = directives.id
        )
      ) order by directives.sort_order), '[]'::jsonb)
      from public.directives
    ),
    'talents', (
      select coalesce(jsonb_agg(jsonb_build_object(
        'id', id, 'family', family, 'tier', tier, 'name', name, 'description', description, 'effect', effect, 'amount', amount
      ) order by sort_order), '[]'::jsonb)
      from public.talents
    ),
    'fittings', (
      select coalesce(jsonb_agg(jsonb_build_object(
        'id', fittings.id,
        'name', fittings.name,
        'description', fittings.description,
        'costs', (
          select coalesce(jsonb_agg(jsonb_build_object('good_id', good_id, 'quantity', quantity) order by good_id), '[]'::jsonb)
          from public.fitting_costs where fitting_id = fittings.id
        )
      ) order by fittings.sort_order), '[]'::jsonb)
      from public.fittings
    ),
    'rules', (
      select jsonb_build_object(
        'offer_hours', offer_hours,
        'takeover_seconds', round(takeover_minutes * 60 * travel_time_factor),
        'contract_slack_seconds', round(contract_slack_minutes * 60 * travel_time_factor),
        'game_carrier_slowness', game_carrier_slowness,
        'freight_base_rate', freight_base_rate,
        'freight_hourly_rate', freight_hourly_rate,
        'depot_capacity', depot_capacity,
        'journeyman_xp', journeyman_xp,
        'master_xp', master_xp
      )
      from private.settings
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
  v_present text[];
  v_branches integer;
  v_haggle numeric;
begin
  if auth.uid() is null then
    raise exception 'Connexion requise.';
  end if;

  perform private.settle_player(auth.uid());
  select * into v_player from public.players where player_id = auth.uid();
  if not found then
    return jsonb_build_object(
      'server_time', now(), 'player', null, 'caravan', null, 'cargo', '[]'::jsonb,
      'workshop', null, 'warehouses', '[]'::jsonb, 'branches', '[]'::jsonb, 'market', '[]'::jsonb, 'trip_event', null
    ) || private.exchange_state(null, array[]::text[]) || private.events_state(null) || private.mastery_state(null);
  end if;

  if exists (select 1 from public.workshops where player_id = v_player.player_id) then
    v_workshop := private.lock_workshop();
    select * into v_player from public.players where player_id = v_player.player_id;
  end if;

  select * into v_caravan from public.caravans where player_id = v_player.player_id;
  select * into v_settings from private.settings;
  select count(*) into v_branches from public.branches where player_id = v_player.player_id;
  v_present := private.present_towns(v_player.player_id);
  v_haggle := private.bonus(v_player.player_id, 'haggle');

  return jsonb_build_object(
    'server_time', now(),
    'player', jsonb_build_object(
      'name', v_player.name,
      'craft_id', v_player.craft_id,
      'home_town_id', v_player.home_town_id,
      'coins', v_player.coins,
      'next_branch_price', case
        when (select opens_branches from public.crafts where id = v_player.craft_id)
          and v_branches < v_settings.max_branches + private.bonus(v_player.player_id, 'network')
        then private.branch_price(v_branches + 1)
      end
    ),
    'caravan', case when v_caravan.player_id is null then null else jsonb_build_object(
      'wagons', v_caravan.wagons,
      'capacity', private.caravan_capacity(v_caravan.player_id),
      'load', private.cargo_load(v_caravan.player_id),
      'next_wagon_price', case when v_caravan.wagons < v_settings.max_wagons then private.wagon_price(v_caravan.wagons + 1) end,
      'town_id', v_caravan.town_id,
      'from_town_id', v_caravan.from_town_id,
      'departed_at', v_caravan.departed_at,
      'arrives_at', v_caravan.arrives_at,
      'directive_id', v_caravan.directive_id
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
      'speed', private.workshop_speed(v_workshop.level) * (1 + private.bonus(v_player.player_id, 'speed')),
      'storage_capacity', private.warehouse_capacity(v_player.player_id),
      'max_queue', private.max_queue(v_workshop.level),
      'next_level_price', case when v_workshop.level < v_settings.max_workshop_level then private.workshop_price(v_workshop.level + 1) end,
      'recipe_id', v_workshop.recipe_id,
      'queued', v_workshop.queued,
      'batch_seconds', v_workshop.batch_seconds,
      'started_at', v_workshop.started_at,
      'paused_until', v_workshop.paused_until
    ) end,
    'warehouses', (
      select coalesce(jsonb_agg(jsonb_build_object(
        'town_id', warehouses.town_id, 'good_id', warehouses.good_id, 'quantity', warehouses.quantity
      ) order by warehouses.town_id, goods.sort_order), '[]'::jsonb)
      from public.warehouses
      join public.goods on goods.id = warehouses.good_id
      where warehouses.player_id = v_player.player_id
    ),
    'trip_event', (
      select jsonb_build_object(
        'kind_id', kind_id,
        'occurred_at', occurred_at,
        'decide_by', decide_by,
        'directive_choice_id', private.directive_choice(player_id, kind_id)
      )
      from public.trip_events where player_id = v_player.player_id
    ),
    'branches', (
      select coalesce(jsonb_agg(branches.town_id order by branches.opened_at, branches.town_id), '[]'::jsonb)
      from public.branches
      where branches.player_id = v_player.player_id
    ),
    'market', (
      select coalesce(jsonb_agg(jsonb_build_object(
        'town_id', markets.town_id,
        'good_id', markets.good_id,
        'buy_price', round(private.buy_quote(markets.base_price, private.current_pressure(markets.pressure, markets.pressure_at)) * (1 - v_haggle), 1),
        'sell_price', round(private.sell_quote(markets.base_price, private.current_pressure(markets.pressure, markets.pressure_at)) * (1 + v_haggle), 1)
      ) order by markets.town_id, goods.sort_order), '[]'::jsonb)
      from public.markets
      join public.goods on goods.id = markets.good_id
      where markets.town_id = any (v_present)
    )
  ) || private.exchange_state(v_player.player_id, v_present) || private.events_state(v_player.player_id)
    || private.mastery_state(v_player.player_id);
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
