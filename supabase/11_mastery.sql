create or replace function private.rank_of(p_xp integer) returns text
language sql stable
set search_path = ''
as $$
  select case
    when p_xp >= master_xp then 'maitre'
    when p_xp >= journeyman_xp then 'compagnon'
    else 'apprenti'
  end
  from private.settings;
$$;

create or replace function private.rank_level(p_rank text) returns integer
language sql immutable
set search_path = ''
as $$
  select case p_rank when 'maitre' then 2 when 'compagnon' then 1 else 0 end;
$$;

create or replace function private.rank_name(p_rank text) returns text
language sql immutable
set search_path = ''
as $$
  select case p_rank when 'maitre' then 'maître' else p_rank end;
$$;

create or replace function private.player_xp(p_player_id uuid) returns integer
language sql stable
set search_path = ''
as $$
  select coalesce((
    select masteries.xp from public.masteries
    join public.players on players.player_id = masteries.player_id and players.craft_id = masteries.craft_id
    where masteries.player_id = p_player_id
  ), 0);
$$;

create or replace function private.masterpiece_price(p_good_id text, p_town_id text) returns integer
language sql stable
set search_path = ''
as $$
  select ceil(markets.base_price * settings.masterpiece_value)::integer
  from public.markets
  cross join private.settings
  where markets.town_id = p_town_id and markets.good_id = p_good_id;
$$;

create or replace function private.gain_xp(p_player_id uuid, p_amount integer) returns void
language plpgsql
set search_path = ''
as $$
declare
  v_player public.players;
  v_after integer;
  v_rank text;
begin
  if p_player_id is null or coalesce(p_amount, 0) <= 0 then
    return;
  end if;

  select * into v_player from public.players where player_id = p_player_id;
  if not found then
    return;
  end if;

  insert into public.masteries (player_id, craft_id, xp) values (p_player_id, v_player.craft_id, p_amount)
  on conflict (player_id, craft_id) do update set xp = public.masteries.xp + excluded.xp
  returning xp into v_after;

  v_rank := private.rank_of(v_after);
  if private.rank_level(v_rank) > private.rank_level(private.rank_of(v_after - p_amount)) then
    perform private.log_event(p_player_id, 'rang', now(), 'good', format(
      'Tu deviens %s %s ! Choisis un talent dans l''onglet Maîtrise.',
      private.rank_name(v_rank), lower((select name from public.crafts where id = v_player.craft_id))));
  end if;
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

create or replace function private.settle_journey(p_player_id uuid) returns void
language plpgsql
set search_path = ''
as $$
declare
  v_caravan public.caravans;
begin
  select * into v_caravan from public.caravans where player_id = p_player_id for update;
  if not found
    or v_caravan.from_town_id is null
    or v_caravan.arrives_at is null
    or v_caravan.arrives_at > now()
    or v_caravan.rewarded_until >= v_caravan.arrives_at then
    return;
  end if;

  update public.caravans set rewarded_until = arrives_at where player_id = p_player_id;
  perform private.gain_xp(p_player_id, (
    select minutes from public.routes where from_town_id = v_caravan.from_town_id and to_town_id = v_caravan.town_id));
end;
$$;

create or replace function private.offer_value(p_offer public.offers) returns bigint
language sql stable
set search_path = ''
as $$
  select case
    when p_offer.give_good_id is null then p_offer.give_quantity
    when p_offer.want_good_id is null then p_offer.want_quantity
    else p_offer.give_quantity * (select base_price from public.goods where id = p_offer.give_good_id)
  end::bigint;
$$;

create or replace function private.gain_trade_xp(p_player_id uuid, p_value bigint) returns void
language plpgsql
set search_path = ''
as $$
declare
  v_craft text;
  v_total bigint;
begin
  if p_player_id is null or coalesce(p_value, 0) <= 0 then
    return;
  end if;

  select craft_id into v_craft from public.players where player_id = p_player_id;
  if v_craft is null then
    return;
  end if;

  insert into public.masteries (player_id, craft_id) values (p_player_id, v_craft)
  on conflict (player_id, craft_id) do nothing;
  select trade_credit + p_value into v_total from public.masteries
  where player_id = p_player_id and craft_id = v_craft
  for update;
  update public.masteries set trade_credit = (v_total % 20)::integer where player_id = p_player_id and craft_id = v_craft;
  perform private.gain_xp(p_player_id, (v_total / 20)::integer);
end;
$$;

create or replace function private.reward_delivery(p_contract public.transport_contracts) returns void
language plpgsql
set search_path = ''
as $$
declare
  v_minutes integer := private.journey_minutes(p_contract.origin_town_id, p_contract.destination_town_id);
  v_share numeric := least(1, p_contract.deposit::numeric / (select contract_xp_value from private.settings));
begin
  perform private.gain_xp(p_contract.carrier_id, round(v_minutes * v_share)::integer);
  if private.craft_family((select craft_id from public.players where player_id = p_contract.shipper_id)) = 'negociant' then
    perform private.gain_trade_xp(p_contract.shipper_id, p_contract.deposit);
  end if;
end;
$$;

create or replace function private.mastery_state(p_player_id uuid) returns jsonb
language sql stable
set search_path = ''
as $$
  select jsonb_build_object(
    'mastery', (
      select jsonb_build_object(
        'craft_id', players.craft_id,
        'xp', private.player_xp(players.player_id),
        'rank', private.rank_of(private.player_xp(players.player_id)),
        'next_rank_xp', case private.rank_of(private.player_xp(players.player_id))
          when 'apprenti' then settings.journeyman_xp
          when 'compagnon' then settings.master_xp
        end,
        'talents', (
          select coalesce(jsonb_agg(talent_id order by chosen_at), '[]'::jsonb)
          from public.player_talents where player_id = players.player_id
        ),
        'bonuses', (
          select coalesce(jsonb_object_agg(effect, total), '{}'::jsonb)
          from (
            select effect, sum(amount) as total
            from (
              select talents.effect, talents.amount from public.player_talents
              join public.talents on talents.id = player_talents.talent_id
              where player_talents.player_id = players.player_id
              union all
              select fittings.effect, fittings.amount from public.caravan_fittings
              join public.fittings on fittings.id = caravan_fittings.fitting_id
              where caravan_fittings.player_id = players.player_id
            ) effects
            group by effect
          ) totals
        )
      )
      from public.players
      cross join private.settings
      where players.player_id = p_player_id
    ),
    'masterpieces', (
      select coalesce(jsonb_agg(jsonb_build_object(
        'id', id, 'good_id', good_id, 'maker', maker_name, 'town_id', town_id, 'created_at', created_at,
        'price', private.masterpiece_price(good_id, town_id)
      ) order by created_at desc, id desc), '[]'::jsonb)
      from public.masterpieces
      where owner_id = p_player_id and sold_at is null
    ),
    'fittings', (
      select coalesce(jsonb_agg(fitting_id order by installed_at, fitting_id), '[]'::jsonb)
      from public.caravan_fittings where player_id = p_player_id
    )
  );
$$;

revoke all on all functions in schema private from public, anon, authenticated;

create or replace function public.choose_talent(p_talent_id text) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player public.players;
  v_talent public.talents;
begin
  v_player := private.lock_player();
  select * into v_talent from public.talents where id = p_talent_id;
  if not found or v_talent.family <> private.craft_family(v_player.craft_id) then
    raise exception 'Ce talent n''est pas de ton métier.';
  end if;

  if private.rank_level(private.rank_of(private.player_xp(v_player.player_id))) < private.rank_level(v_talent.tier) then
    raise exception 'Il faut être % pour choisir ce talent.', private.rank_name(v_talent.tier);
  end if;

  if exists (
    select 1 from public.player_talents
    join public.talents on talents.id = player_talents.talent_id
    where player_talents.player_id = v_player.player_id and talents.tier = v_talent.tier
  ) then
    raise exception 'Tu as déjà choisi ton talent de %.', private.rank_name(v_talent.tier);
  end if;

  insert into public.player_talents (player_id, talent_id) values (v_player.player_id, v_talent.id);
  return public.get_state();
end;
$$;

create or replace function public.sell_masterpiece(p_masterpiece_id bigint) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player public.players;
  v_piece public.masterpieces;
begin
  v_player := private.lock_player();
  select * into v_piece from public.masterpieces
  where id = p_masterpiece_id and owner_id = v_player.player_id and sold_at is null
  for update;
  if not found then
    raise exception 'Ce chef-d''œuvre n''est plus à toi.';
  end if;

  perform private.acting_town(v_player, v_piece.town_id);
  update public.players set coins = coins + private.masterpiece_price(v_piece.good_id, v_piece.town_id)
  where player_id = v_player.player_id;
  update public.masterpieces set sold_at = now() where id = v_piece.id;
  return public.get_state();
end;
$$;

create or replace function public.install_fitting(p_fitting_id text) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player public.players;
  v_cost record;
begin
  v_player := private.lock_player();
  perform private.lock_caravan_in_town();
  if not exists (select 1 from public.fittings where id = p_fitting_id) then
    raise exception 'Cet équipement n''existe pas.';
  end if;

  if exists (select 1 from public.caravan_fittings where player_id = v_player.player_id and fitting_id = p_fitting_id) then
    raise exception 'Ta caravane a déjà cet équipement.';
  end if;

  for v_cost in
    select fitting_costs.good_id, fitting_costs.quantity, goods.name, coalesce(cargo.quantity, 0) as held
    from public.fitting_costs
    join public.goods on goods.id = fitting_costs.good_id
    left join public.cargo on cargo.player_id = v_player.player_id and cargo.good_id = fitting_costs.good_id
    where fitting_costs.fitting_id = p_fitting_id
    order by goods.sort_order
  loop
    if v_cost.held < v_cost.quantity then
      raise exception 'Il te manque % % dans ta cale.', v_cost.quantity - v_cost.held, lower(v_cost.name);
    end if;

    perform private.change_cargo(v_player.player_id, v_cost.good_id, -v_cost.quantity);
  end loop;

  insert into public.caravan_fittings (player_id, fitting_id) values (v_player.player_id, p_fitting_id);
  return public.get_state();
end;
$$;

create or replace function public.attach_wagon() returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_caravan public.caravans;
begin
  perform private.lock_player();
  v_caravan := private.lock_caravan_in_town();
  if v_caravan.wagons >= (select max_wagons from private.settings) then
    raise exception 'Ta caravane ne peut pas tirer plus de chariots.';
  end if;

  if coalesce((select quantity from public.cargo where player_id = v_caravan.player_id and good_id = 'chariot'), 0) < 1 then
    raise exception 'Il te faut un chariot du charron dans ta cale.';
  end if;

  perform private.change_cargo(v_caravan.player_id, 'chariot', -1);
  update public.caravans set wagons = wagons + 1 where player_id = v_caravan.player_id;
  return public.get_state();
end;
$$;

revoke all on function
  public.choose_talent(text),
  public.sell_masterpiece(bigint),
  public.install_fitting(text),
  public.attach_wagon()
from public, anon;
grant execute on function
  public.choose_talent(text),
  public.sell_masterpiece(bigint),
  public.install_fitting(text),
  public.attach_wagon()
to authenticated;

notify pgrst, 'reload schema';
