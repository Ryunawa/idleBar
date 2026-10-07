-- Commandes spéciales au coût réel : une commande d'artisan paie les matières au prix de sa ville,
-- une marge et le temps de fabrication, pour environ une heure de travail. Seules les pièces
-- fabriquées dans l'atelier après la commande comptent, pour qu'on ne la remplisse pas au marché.
-- À exécuter après 18_supply_requests.sql.

alter table private.settings add column if not exists special_order_minutes numeric not null default 60 check (special_order_minutes > 0);
alter table private.settings add column if not exists special_order_labour_rate numeric not null default 2 check (special_order_labour_rate >= 0);
alter table private.settings add column if not exists special_order_input_markup numeric not null default 0.1 check (special_order_input_markup >= 0);

alter table public.special_orders add column if not exists crafted boolean not null default false;
alter table public.special_orders add column if not exists produced integer not null default 0 check (produced >= 0);

update public.event_choices
set description = 'La commande part dès que ton atelier a fabriqué les pièces demandées.'
where kind_id = 'commande' and id = 'livrer';

create or replace function private.recipe_unit_cost(p_recipe_id text, p_town_id text) returns numeric
language sql stable
set search_path = ''
as $$
  select coalesce(sum(inputs.quantity * coalesce(
      private.buy_quote(markets.base_price, private.current_pressure(markets.pressure, markets.pressure_at)),
      private.buy_quote(goods.base_price, 0))), 0) / recipes.output_quantity
  from public.recipes
  join public.recipe_inputs inputs on inputs.recipe_id = recipes.id
  join public.goods on goods.id = inputs.good_id
  left join public.markets on markets.good_id = inputs.good_id and markets.town_id = p_town_id
  where recipes.id = p_recipe_id
  group by recipes.output_quantity;
$$;

create or replace function private.order_ready(p_order public.special_orders) returns boolean
language sql stable
set search_path = ''
as $$
  select (not p_order.crafted or p_order.produced >= p_order.quantity)
    and coalesce((select quantity from public.warehouses
      where player_id = p_order.player_id and town_id = p_order.town_id and good_id = p_order.good_id), 0) >= p_order.quantity;
$$;
create or replace function private.roll_special_order(p_player public.players) returns void
language plpgsql
set search_path = ''
as $$
declare
  v_settings private.settings;
  v_hours numeric;
  v_good public.goods;
  v_market_price numeric;
  v_buy_quote numeric;
  v_recipe public.recipes;
  v_minutes numeric;
  v_bonus numeric;
  v_quantity integer;
  v_price integer;
begin
  select * into v_settings from private.settings;
  v_hours := extract(epoch from (now() - p_player.orders_checked_at)) / 3600 / v_settings.craft_time_factor;
  update public.players set orders_checked_at = now() where player_id = p_player.player_id;
  if not v_settings.events_enabled
    or exists (select 1 from public.special_orders where player_id = p_player.player_id and status = 'open')
    or random() >= 1 - exp(-v_hours * (1 + private.bonus(p_player.player_id, 'renown')) / v_settings.special_order_hours) then
    return;
  end if;

  select goods.* into v_good from public.goods
  join public.markets on markets.good_id = goods.id and markets.town_id = p_player.home_town_id
  where goods.id in (select output_good_id from public.recipes where craft_id = p_player.craft_id)
    or (not exists (select 1 from public.recipes where craft_id = p_player.craft_id) and goods.market_sells and markets.base_price >= goods.base_price * 0.8)
  order by random()
  limit 1;
  if v_good.id is null then
    return;
  end if;

  select markets.base_price, private.buy_quote(markets.base_price, private.current_pressure(markets.pressure, markets.pressure_at))
  into v_market_price, v_buy_quote
  from public.markets where town_id = p_player.home_town_id and good_id = v_good.id;
  v_bonus := 1 + coalesce((private.reputation_tier(p_player.player_id, p_player.home_town_id)).order_bonus, 0);
  v_price := greatest(floor(v_buy_quote * v_settings.special_order_price_ratio), 1)::integer;
  select * into v_recipe from public.recipes
  where craft_id = p_player.craft_id and output_good_id = v_good.id
  order by sort_order
  limit 1;
  if v_recipe.id is null then
    v_quantity := least(greatest(round(v_settings.special_order_value * v_bonus / v_market_price), 1), 30)::integer;
  else
    v_minutes := v_recipe.seconds / 60.0 / v_recipe.output_quantity;
    v_quantity := least(greatest(round(v_settings.special_order_minutes * v_bonus / v_minutes), 1), 30)::integer;
    v_price := greatest(v_price, ceil(private.recipe_unit_cost(v_recipe.id, p_player.home_town_id) * (1 + v_settings.special_order_input_markup)
      + v_minutes * v_settings.special_order_labour_rate)::integer);
  end if;

  insert into public.special_orders (player_id, town_id, good_id, quantity, unit_price, deadline, crafted)
  values (p_player.player_id, p_player.home_town_id, v_good.id, v_quantity, v_price,
    now() + make_interval(secs => v_settings.special_order_window_hours * 3600 * v_settings.craft_time_factor), v_recipe.id is not null);
  perform private.log_event(p_player.player_id, 'commande', now(), 'neutral', format(
    case when v_recipe.id is null
      then 'Un client de %s veut %s %s à %s écus pièce, sous %s.'
      else 'Un client de %s commande %s %s à fabriquer dans ton atelier, à %s écus pièce, sous %s.'
    end,
    (select name from public.towns where id = p_player.home_town_id), v_quantity, lower(v_good.name), v_price,
    private.duration_text(v_settings.special_order_window_hours * v_settings.craft_time_factor * 60)));
end;
$$;

create or replace function private.reward_production(p_player_id uuid, p_recipe public.recipes, p_batches integer, p_town_id text) returns void
language plpgsql
set search_path = ''
as $$
declare
  v_settings private.settings;
  v_rank text;
  v_chance numeric;
  v_count integer := 0;
  v_name text;
begin
  update public.special_orders
  set produced = least(quantity, produced + p_batches * p_recipe.output_quantity)
  where player_id = p_player_id and status = 'open' and crafted
    and town_id = p_town_id and good_id = p_recipe.output_good_id;
  perform private.gain_xp(p_player_id, (p_batches * p_recipe.seconds / 60)::integer);

  select * into v_settings from private.settings;
  v_rank := private.rank_of(private.player_xp(p_player_id));
  v_chance := case v_rank
    when 'maitre' then v_settings.master_masterpiece_chance
    when 'compagnon' then v_settings.journeyman_masterpiece_chance
    else 0
  end;
  if v_chance <= 0 then
    return;
  end if;

  for v_batch in 1..p_batches loop
    if random() < v_chance then
      v_count := v_count + 1;
    end if;
  end loop;
  if v_count = 0 then
    return;
  end if;

  select name into v_name from public.players where player_id = p_player_id;
  insert into public.masterpieces (owner_id, maker_name, craft_id, good_id, town_id)
  select p_player_id, v_name, p_recipe.craft_id, p_recipe.output_good_id, p_town_id
  from generate_series(1, v_count);

  perform private.log_event(p_player_id, 'chef_oeuvre', now(), 'good', format(
    'Ton atelier signe %s : %s de %s, à vendre %s écus au marché.',
    case when v_count = 1 then 'un chef-d''œuvre' else v_count || ' chefs-d''œuvre' end,
    lower((select name from public.goods where id = p_recipe.output_good_id)), v_name,
    private.masterpiece_price(p_recipe.output_good_id, p_town_id)));
end;
$$;

create or replace function private.settle_events(p_player_id uuid) returns void
language plpgsql
set search_path = ''
as $$
declare
  v_player public.players;
  v_order public.special_orders;
  v_limit timestamptz;
begin
  delete from public.event_log where player_id = p_player_id and happened_at < now() - interval '7 days';
  select * into v_player from public.players where player_id = p_player_id for update;
  if not found or private.is_itinerant(v_player.craft_id) then
    return;
  end if;

  select * into v_order from public.special_orders where player_id = p_player_id and status = 'open' for update;
  if v_order.id is not null then
    v_limit := least(now(), v_order.deadline);
    if exists (select 1 from public.pending_events where player_id = p_player_id and kind_id = 'panne' and occurs_at <= v_limit) then
      perform private.settle_breakdown(p_player_id);
    end if;

    perform private.produce_until(p_player_id, v_limit);
    select * into v_order from public.special_orders where id = v_order.id;
    if private.order_choice(p_player_id, 'commande') = 'livrer' and private.order_ready(v_order) then
      perform private.deliver_special_order(v_order);
    elsif v_order.deadline <= now() then
      update public.special_orders set status = 'expired', closed_at = deadline where id = v_order.id;
      perform private.log_event(p_player_id, 'commande', v_order.deadline, 'bad', format('La commande de %s %s n''a pas été livrée à temps.',
        v_order.quantity, lower((select name from public.goods where id = v_order.good_id))));
    end if;
  end if;

  perform private.roll_special_order(v_player);
end;
$$;

create or replace function public.fulfill_special_order(p_order_id bigint) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player public.players;
  v_order public.special_orders;
begin
  v_player := private.lock_player();
  if not private.is_itinerant(v_player.craft_id) then
    perform private.lock_workshop();
  end if;

  select * into v_order from public.special_orders where id = p_order_id and player_id = v_player.player_id for update;
  if not found then
    raise exception 'Cette commande n''existe pas.';
  end if;

  if v_order.status <> 'open' then
    raise exception 'Cette commande est déjà close.';
  end if;

  if v_order.crafted and v_order.produced < v_order.quantity then
    raise exception 'Ton atelier doit encore fabriquer % % pour cette commande : seules les pièces faites après la commande comptent.',
      v_order.quantity - v_order.produced, lower((select name from public.goods where id = v_order.good_id));
  end if;

  if coalesce((select quantity from public.warehouses
      where player_id = v_player.player_id and town_id = v_order.town_id and good_id = v_order.good_id), 0) < v_order.quantity then
    raise exception 'Il te faut % % à l''entrepôt pour livrer cette commande.',
      v_order.quantity, lower((select name from public.goods where id = v_order.good_id));
  end if;

  perform private.deliver_special_order(v_order);
  return public.get_state();
end;
$$;

create or replace function private.events_state(p_player_id uuid) returns jsonb
language sql stable
set search_path = ''
as $$
  select jsonb_build_object(
    'orders', (
      select coalesce(jsonb_agg(jsonb_build_object('kind_id', kinds.id, 'choice_id', private.order_choice(p_player_id, kinds.id))
        order by kinds.sort_order), '[]'::jsonb)
      from public.event_kinds kinds
      where kinds.scope = 'workshop' and exists (select 1 from public.event_choices where kind_id = kinds.id)
    ),
    'journal', (
      select coalesce(jsonb_agg(listed.entry order by listed.happened_at desc, listed.id desc), '[]'::jsonb)
      from (
        select jsonb_build_object(
          'id', id, 'kind_id', kind_id, 'happened_at', happened_at, 'title', title, 'detail', detail, 'tone', tone, 'seen', seen
        ) as entry, happened_at, id
        from public.event_log
        where player_id = p_player_id
        order by happened_at desc, id desc
        limit 20
      ) listed
    ),
    'special_orders', (
      select coalesce(jsonb_agg(jsonb_build_object(
        'id', id, 'town_id', town_id, 'good_id', good_id, 'quantity', quantity, 'unit_price', unit_price,
        'created_at', created_at, 'deadline', deadline, 'status', status, 'closed_at', closed_at,
        'crafted', crafted, 'produced', produced
      ) order by created_at desc, id desc), '[]'::jsonb)
      from public.special_orders
      where player_id = p_player_id and (status = 'open' or closed_at > now() - interval '24 hours')
    ),
    'journal_unseen', (select count(*) from public.event_log where player_id = p_player_id and not seen)
  );
$$;

revoke all on all functions in schema private from public, anon, authenticated;
revoke all on function public.fulfill_special_order(bigint) from public, anon;
grant execute on function public.fulfill_special_order(bigint) to authenticated;

notify pgrst, 'reload schema';