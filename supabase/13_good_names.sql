alter table public.goods add column if not exists one_name text;
alter table public.goods add column if not exists many_name text;

update public.goods
set one_name = names.one_name, many_name = names.many_name
from (values
  ('sel', 'sel', 'sel'),
  ('poisson', 'poisson', 'poissons'),
  ('bois', 'bois', 'bois'),
  ('laine', 'laine', 'laine'),
  ('charbon', 'charbon', 'charbon'),
  ('fer', 'fer', 'fer'),
  ('herbes', 'herbe', 'herbes'),
  ('vin', 'vin', 'vin'),
  ('teinture', 'teinture', 'teinture'),
  ('epices', 'épice', 'épices'),
  ('outils', 'outil', 'outils'),
  ('ferrures', 'ferrure', 'ferrures'),
  ('caisses', 'caisse', 'caisses'),
  ('roues', 'roue', 'roues'),
  ('drap', 'drap', 'draps'),
  ('baches', 'bâche', 'bâches'),
  ('remedes', 'remède', 'remèdes'),
  ('onguent', 'onguent', 'onguents'),
  ('chariot', 'chariot', 'chariots')
) as names (id, one_name, many_name)
where goods.id = names.id;

create or replace function public.get_world() returns jsonb
language sql stable
security definer
set search_path = ''
as $$
  select jsonb_build_object(
    'goods', (
      select coalesce(jsonb_agg(jsonb_build_object(
        'id', id, 'name', name, 'one_name', one_name, 'many_name', many_name,
        'base_price', base_price, 'crafted', crafted, 'market_sells', market_sells
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

revoke all on function public.get_world() from public, anon;
grant execute on function public.get_world() to authenticated;
