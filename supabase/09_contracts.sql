create or replace function public.post_contract(
  p_good_id text,
  p_quantity integer,
  p_destination_id text,
  p_reward integer,
  p_town_id text default null
) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player public.players;
  v_settings private.settings;
  v_town text;
  v_stored integer;
  v_minutes integer;
  v_fee integer;
  v_escrow integer;
  v_takeover_at timestamptz;
begin
  if p_quantity is null or p_quantity < 1 then
    raise exception 'Quantité invalide.';
  end if;

  if p_reward is null or p_reward < 1 then
    raise exception 'La récompense doit être d''au moins 1 écu.';
  end if;

  v_player := private.lock_player();
  select * into v_settings from private.settings;
  v_town := private.acting_town(v_player, p_town_id);
  if not exists (select 1 from public.towns where id = p_destination_id) then
    raise exception 'Cette ville n''existe pas.';
  end if;

  if p_destination_id = v_town then
    raise exception 'Choisis une destination différente de la ville de départ.';
  end if;

  if not private.can_collect_in(v_player, p_destination_id) then
    raise exception 'Personne ne pourrait recevoir ces marchandises là-bas : il te faut un comptoir dans cette ville.';
  end if;

  if (select count(*) from public.transport_contracts
      where shipper_id = v_player.player_id and status in ('open', 'carried', 'shipped')) >= v_settings.max_open_contracts then
    raise exception 'Tu as déjà % contrats en cours.', v_settings.max_open_contracts;
  end if;

  select quantity into v_stored from public.warehouses
  where player_id = v_player.player_id and town_id = v_town and good_id = p_good_id;
  if coalesce(v_stored, 0) < p_quantity then
    raise exception 'Il n''y en a pas assez dans ton entrepôt de cette ville.';
  end if;

  v_minutes := private.journey_minutes(v_town, p_destination_id);
  if v_minutes is null then
    raise exception 'Aucune route ne mène là.';
  end if;

  v_fee := greatest(ceil(private.freight_fee(p_good_id, p_quantity, v_minutes) * (1 - private.bonus(v_player.player_id, 'logistics'))), 1)::integer;
  v_escrow := greatest(p_reward, v_fee);
  if v_player.coins < v_escrow then
    raise exception 'Il faut % écus pour garantir le transport.', v_escrow;
  end if;

  v_takeover_at := now() + make_interval(secs => v_settings.takeover_minutes * 60 * v_settings.travel_time_factor);
  update public.players set coins = coins - v_escrow where player_id = v_player.player_id;
  perform private.change_warehouse(v_player.player_id, v_town, p_good_id, -p_quantity);
  insert into public.transport_contracts (
    shipper_id, origin_town_id, destination_town_id, good_id, quantity,
    reward, game_fee, deposit, takeover_at, game_arrives_at
  ) values (
    v_player.player_id, v_town, p_destination_id, p_good_id, p_quantity,
    p_reward, v_fee, p_quantity * (select base_price from public.goods where id = p_good_id), v_takeover_at,
    v_takeover_at + make_interval(secs => v_minutes * 60 * v_settings.travel_time_factor * v_settings.game_carrier_slowness)
  );
  return public.get_state();
end;
$$;

create or replace function public.cancel_contract(p_contract_id bigint) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player public.players;
  v_contract public.transport_contracts;
begin
  v_player := private.lock_player();
  select * into v_contract from public.transport_contracts
  where id = p_contract_id and shipper_id = v_player.player_id
  for update;
  if not found then
    raise exception 'Ce contrat n''existe pas.';
  end if;

  if v_contract.status <> 'open' then
    raise exception 'Ce contrat est déjà en route ou clos : il ne peut plus être annulé.';
  end if;

  if private.warehouse_free(v_player.player_id, v_contract.origin_town_id) < v_contract.quantity then
    raise exception 'Ton entrepôt de % n''a pas la place de reprendre cette marchandise.', (select name from public.towns where id = v_contract.origin_town_id);
  end if;

  update public.players
  set coins = coins + greatest(v_contract.reward, v_contract.game_fee)
  where player_id = v_player.player_id;
  perform private.change_warehouse(v_player.player_id, v_contract.origin_town_id, v_contract.good_id, v_contract.quantity);
  update public.transport_contracts set status = 'cancelled', closed_at = now() where id = v_contract.id;
  return public.get_state();
end;
$$;

create or replace function public.accept_contract(p_contract_id bigint) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player public.players;
  v_settings private.settings;
  v_contract public.transport_contracts;
  v_town text;
begin
  v_player := private.lock_player();
  if not private.is_itinerant(v_player.craft_id) then
    raise exception 'Seuls les caravaniers transportent les marchandises des autres.';
  end if;

  select * into v_contract from public.transport_contracts where id = p_contract_id for update;
  if not found or v_contract.status <> 'open' or v_contract.takeover_at <= now() then
    raise exception 'Ce contrat n''est plus disponible.';
  end if;

  if v_contract.shipper_id = v_player.player_id then
    raise exception 'C''est ton propre contrat.';
  end if;

  v_town := private.acting_town(v_player, null);
  if v_town <> v_contract.origin_town_id then
    raise exception 'Va d''abord à % pour charger la marchandise.', (select name from public.towns where id = v_contract.origin_town_id);
  end if;

  select * into v_settings from private.settings;
  if (select count(*) from public.transport_contracts
      where carrier_id = v_player.player_id and status = 'carried') >= v_settings.max_carried_contracts then
    raise exception 'Tu transportes déjà % contrats.', v_settings.max_carried_contracts;
  end if;

  if private.free_space(v_player, v_town) < v_contract.quantity then
    raise exception 'Ta cale n''a pas la place : il faut % places libres.', v_contract.quantity;
  end if;

  if v_player.coins < v_contract.deposit then
    raise exception 'Il faut % écus de caution pour prendre ce contrat.', v_contract.deposit;
  end if;

  update public.players set coins = coins - v_contract.deposit where player_id = v_player.player_id;
  update public.players
  set coins = coins + greatest(v_contract.reward, v_contract.game_fee) - v_contract.reward
  where player_id = v_contract.shipper_id;
  update public.transport_contracts
  set status = 'carried',
      carrier_id = v_player.player_id,
      carried_at = now(),
      deadline = now() + make_interval(secs =>
        (private.journey_minutes(origin_town_id, destination_town_id) + v_settings.contract_slack_minutes) * 60 * v_settings.travel_time_factor)
  where id = v_contract.id;
  return public.get_state();
end;
$$;

create or replace function public.mark_exchanges_seen() returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
begin
  if auth.uid() is null then
    raise exception 'Connexion requise.';
  end if;

  update public.offers set seller_seen = true where seller_id = auth.uid() and not seller_seen;
  update public.transport_contracts set shipper_seen = true where shipper_id = auth.uid() and not shipper_seen;
  update public.transport_contracts set carrier_seen = true where carrier_id = auth.uid() and not carrier_seen;
  return public.get_state();
end;
$$;

revoke all on function
  public.post_contract(text, integer, text, integer, text),
  public.cancel_contract(bigint),
  public.accept_contract(bigint),
  public.mark_exchanges_seen()
from public, anon;
grant execute on function
  public.post_contract(text, integer, text, integer, text),
  public.cancel_contract(bigint),
  public.accept_contract(bigint),
  public.mark_exchanges_seen()
to authenticated;

notify pgrst, 'reload schema';
