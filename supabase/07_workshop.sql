create or replace function public.start_production(p_recipe_id text, p_batches integer) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player public.players;
  v_workshop public.workshops;
  v_recipe public.recipes;
  v_input record;
  v_batches integer;
  v_needs text;
begin
  if p_batches is null or p_batches < 1 then
    raise exception 'Quantité invalide.';
  end if;

  v_player := private.lock_player();
  v_workshop := private.lock_workshop();
  select * into v_recipe from public.recipes where id = p_recipe_id;
  if not found or v_recipe.craft_id <> v_player.craft_id then
    raise exception 'Cette recette n''est pas de ton métier.';
  end if;

  if v_workshop.queued > 0 and v_workshop.recipe_id <> p_recipe_id then
    raise exception 'Ton atelier fabrique déjà autre chose : attends la fin de la commande en cours.';
  end if;

  v_batches := least(p_batches, private.max_queue(v_workshop.level) - v_workshop.queued);
  if v_batches < 1 then
    raise exception 'La file de l''atelier est pleine.';
  end if;

  for v_input in
    select recipe_inputs.quantity, coalesce(warehouses.quantity, 0) as held
    from public.recipe_inputs
    left join public.warehouses
      on warehouses.player_id = v_player.player_id
      and warehouses.town_id = v_workshop.town_id
      and warehouses.good_id = recipe_inputs.good_id
    where recipe_inputs.recipe_id = p_recipe_id
  loop
    v_batches := least(v_batches, v_input.held / v_input.quantity);
  end loop;

  if v_batches < 1 then
    select string_agg(recipe_inputs.quantity || ' ' || lower(goods.name), ' et ' order by goods.sort_order) into v_needs
    from public.recipe_inputs
    join public.goods on goods.id = recipe_inputs.good_id
    where recipe_inputs.recipe_id = p_recipe_id;
    raise exception 'Il te manque des matières premières : il faut % par fabrication.', v_needs;
  end if;

  for v_input in select good_id, quantity from public.recipe_inputs where recipe_id = p_recipe_id loop
    perform private.change_warehouse(v_player.player_id, v_workshop.town_id, v_input.good_id, -v_input.quantity * v_batches);
  end loop;

  if v_workshop.queued = 0 then
    update public.workshops
    set recipe_id = p_recipe_id,
        queued = v_batches,
        started_at = now(),
        batch_seconds = greatest(ceil(v_recipe.seconds * (select craft_time_factor from private.settings) / private.workshop_speed(level)), 1)
    where player_id = v_player.player_id;
  else
    update public.workshops set queued = queued + v_batches where player_id = v_player.player_id;
  end if;

  return public.get_state();
end;
$$;

create or replace function public.upgrade_workshop() returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player public.players;
  v_workshop public.workshops;
  v_price integer;
begin
  v_player := private.lock_player();
  v_workshop := private.lock_workshop();
  if v_workshop.level >= (select max_workshop_level from private.settings) then
    raise exception 'Ton atelier est déjà au niveau maximal.';
  end if;

  v_price := private.workshop_price(v_workshop.level + 1);
  if v_player.coins < v_price then
    raise exception 'Pas assez d''écus.';
  end if;

  update public.players set coins = coins - v_price where player_id = v_player.player_id;
  update public.workshops set level = level + 1 where player_id = v_player.player_id;

  return public.get_state();
end;
$$;

revoke all on function public.start_production(text, integer), public.upgrade_workshop() from public, anon;
grant execute on function public.start_production(text, integer), public.upgrade_workshop() to authenticated;

notify pgrst, 'reload schema';
