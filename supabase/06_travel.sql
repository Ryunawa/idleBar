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

drop function if exists public.depart(text);

create or replace function public.depart(p_destination_id text, p_directive_id text default null) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_caravan public.caravans;
  v_route public.routes;
  v_seconds numeric;
begin
  perform private.lock_player();
  v_caravan := private.lock_caravan_in_town();
  select * into v_route from public.routes where from_town_id = v_caravan.town_id and to_town_id = p_destination_id;
  if not found then
    raise exception 'Aucune route ne mène là depuis ici.';
  end if;

  if p_directive_id is not null and not exists (select 1 from public.directives where id = p_directive_id) then
    raise exception 'Cette directive n''existe pas.';
  end if;

  v_seconds := round(v_route.minutes * 60 * (select travel_time_factor from private.settings) * (1 - private.bonus(v_caravan.player_id, 'swift')));
  update public.caravans
  set from_town_id = town_id,
      town_id = p_destination_id,
      departed_at = now(),
      arrives_at = now() + make_interval(secs => v_seconds),
      directive_id = coalesce(p_directive_id, directive_id)
  where player_id = v_caravan.player_id;

  perform private.roll_trip_event(v_caravan.player_id, now(), now() + make_interval(secs => v_seconds), v_route.biome, v_route.minutes);
  return public.get_state();
end;
$$;

revoke all on function public.buy_wagon(), public.depart(text, text) from public, anon;
grant execute on function public.buy_wagon(), public.depart(text, text) to authenticated;
