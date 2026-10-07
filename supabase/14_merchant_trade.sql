alter table private.settings add column if not exists merchant_market_rate numeric not null default 0.05
  check (merchant_market_rate between 0 and 0.5);
alter table private.settings add column if not exists merchant_open_offers integer not null default 16
  check (merchant_open_offers > 0);

update public.crafts
set description = 'Tient un comptoir et des succursales, achète moins cher et vend plus cher au marché, et fait voyager ses marchandises par contrats.'
where id = 'negociant';

create or replace function private.bonus(p_player_id uuid, p_effect text) returns numeric
language sql stable
set search_path = ''
as $$
  select
    coalesce((select sum(talents.amount) from public.player_talents
      join public.talents on talents.id = player_talents.talent_id
      where player_talents.player_id = p_player_id and talents.effect = p_effect), 0)
    + coalesce((select sum(fittings.amount) from public.caravan_fittings
      join public.fittings on fittings.id = caravan_fittings.fitting_id
      where caravan_fittings.player_id = p_player_id and fittings.effect = p_effect), 0)
    + case when p_effect = 'haggle' and exists (
        select 1 from public.players
        join public.crafts on crafts.id = players.craft_id
        where players.player_id = p_player_id and crafts.opens_branches)
      then (select merchant_market_rate from private.settings) else 0 end;
$$;

create or replace function public.post_offer(
  p_give_good_id text,
  p_give_quantity integer,
  p_want_good_id text,
  p_want_quantity integer,
  p_town_id text default null,
  p_from_warehouse boolean default false
) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player public.players;
  v_town text;
  v_settings private.settings;
  v_limit integer;
begin
  if p_give_quantity is null or p_give_quantity < 1 or p_want_quantity is null or p_want_quantity < 1 then
    raise exception 'Quantité invalide.';
  end if;

  if p_give_good_id is null and p_want_good_id is null then
    raise exception 'Échanger des écus contre des écus n''a pas de sens.';
  end if;

  if p_give_good_id = p_want_good_id then
    raise exception 'Choisis deux marchandises différentes.';
  end if;

  if (p_give_good_id is not null and not exists (select 1 from public.goods where id = p_give_good_id))
    or (p_want_good_id is not null and not exists (select 1 from public.goods where id = p_want_good_id)) then
    raise exception 'Cette marchandise n''existe pas.';
  end if;

  v_player := private.lock_player();
  select * into v_settings from private.settings;
  v_limit := case when (select opens_branches from public.crafts where id = v_player.craft_id)
    then v_settings.merchant_open_offers else v_settings.max_open_offers end;
  if (select count(*) from public.offers where seller_id = v_player.player_id and status = 'open') >= v_limit then
    raise exception 'Tu as déjà % offres au comptoir : retires-en une d''abord.', v_limit;
  end if;

  v_town := private.acting_town(v_player, p_town_id);
  if p_give_good_id is null then
    if v_player.coins < p_give_quantity then
      raise exception 'Pas assez d''écus.';
    end if;

    update public.players set coins = coins - p_give_quantity where player_id = v_player.player_id;
  elsif p_from_warehouse then
    if coalesce((select quantity from public.warehouses
        where player_id = v_player.player_id and town_id = v_town and good_id = p_give_good_id), 0) < p_give_quantity then
      raise exception 'Tu n''en as pas assez dans ton entrepôt pour cette offre.';
    end if;

    perform private.change_warehouse(v_player.player_id, v_town, p_give_good_id, -p_give_quantity);
  else
    if private.held(v_player, v_town, p_give_good_id) < p_give_quantity then
      raise exception 'Tu n''en as pas assez pour cette offre.';
    end if;

    perform private.change_holdings(v_player, v_town, p_give_good_id, -p_give_quantity);
  end if;

  insert into public.offers (town_id, seller_id, give_good_id, give_quantity, want_good_id, want_quantity, expires_at)
  values (v_town, v_player.player_id, p_give_good_id, p_give_quantity, p_want_good_id, p_want_quantity,
    now() + make_interval(hours => v_settings.offer_hours));
  return public.get_state();
end;
$$;

revoke all on function public.post_offer(text, integer, text, integer, text, boolean) from public, anon;
grant execute on function public.post_offer(text, integer, text, integer, text, boolean) to authenticated;
