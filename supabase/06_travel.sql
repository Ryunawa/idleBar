create or replace function public.buy_wagon() returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player public.players;
  v_caravan public.caravans;
  v_price integer;
begin
  v_player := private.lock_player();
  v_caravan := private.lock_caravan_in_town();
  if v_caravan.wagons >= (select max_wagons from private.settings) then
    raise exception 'Ta caravane ne peut pas tirer plus de chariots.';
  end if;

  v_price := private.wagon_price(v_caravan.wagons + 1);
  if v_player.coins < v_price then
    raise exception 'Pas assez d''écus.';
  end if;

  update public.players set coins = coins - v_price where player_id = v_player.player_id;
  update public.caravans set wagons = wagons + 1 where player_id = v_caravan.player_id;

  return public.get_state();
end;
$$;

create or replace function public.depart(p_destination_id text) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_caravan public.caravans;
  v_seconds numeric;
begin
  perform private.lock_player();
  v_caravan := private.lock_caravan_in_town();
  select round(routes.minutes * 60 * settings.travel_time_factor) into v_seconds
  from public.routes
  cross join private.settings
  where routes.from_town_id = v_caravan.town_id and routes.to_town_id = p_destination_id;
  if not found then
    raise exception 'Aucune route ne mène là depuis ici.';
  end if;

  update public.caravans
  set from_town_id = town_id,
      town_id = p_destination_id,
      departed_at = now(),
      arrives_at = now() + make_interval(secs => v_seconds)
  where player_id = v_caravan.player_id;

  return public.get_state();
end;
$$;

revoke all on function public.buy_wagon(), public.depart(text) from public, anon;
grant execute on function public.buy_wagon(), public.depart(text) to authenticated;
