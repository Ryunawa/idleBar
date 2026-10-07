-- Rançon des bandits : payer le passage coûte 8 % de ce que transporte la caravane (bourse et valeur
-- de la marchandise), 150 écus au plus. Les écus partent d'abord ; s'il en manque, les bandits se
-- servent dans la cargaison, en commençant par la marchandise la plus précieuse.
-- À exécuter après 20_trade_log.sql.

alter table private.settings add column if not exists bandit_ransom_rate numeric not null default 0.08 check (bandit_ransom_rate between 0 and 1);
alter table private.settings add column if not exists bandit_ransom_cap integer not null default 150 check (bandit_ransom_cap >= 0);

update public.event_choices
set description = 'Tu donnes 8 % de ce que tu transportes (bourse et marchandise), 150 écus au plus : en écus d''abord, puis en marchandise.'
where kind_id = 'bandits' and id = 'payer';

create or replace function private.cargo_value(p_player_id uuid) returns bigint
language sql stable
set search_path = ''
as $$
  select coalesce(sum(cargo.quantity * goods.base_price), 0)::bigint
  from public.cargo
  join public.goods on goods.id = cargo.good_id
  where cargo.player_id = p_player_id;
$$;

create or replace function private.seize_cargo(p_player_id uuid, p_value bigint) returns text
language plpgsql
set search_path = ''
as $$
declare
  v_line record;
  v_left bigint := p_value;
  v_taken integer;
  v_parts text[] := array[]::text[];
begin
  for v_line in
    select cargo.good_id, cargo.quantity, goods.base_price, goods.name
    from public.cargo
    join public.goods on goods.id = cargo.good_id
    where cargo.player_id = p_player_id
    order by goods.base_price desc, goods.sort_order
  loop
    exit when v_left <= 0;
    v_taken := least(v_line.quantity, ceil(v_left::numeric / greatest(v_line.base_price, 1))::integer);
    perform private.change_cargo(p_player_id, v_line.good_id, -v_taken);
    v_left := v_left - v_taken * v_line.base_price;
    v_parts := v_parts || format('%s %s', v_taken, lower(v_line.name));
  end loop;

  return case when cardinality(v_parts) = 0 then null else array_to_string(v_parts, ', ') end;
end;
$$;
create or replace function private.apply_road_event(
  p_player_id uuid,
  p_kind_id text,
  p_choice_id text,
  p_base_seconds numeric,
  p_at timestamptz
) returns void
language plpgsql
set search_path = ''
as $$
declare
  v_caravan public.caravans;
  v_coins bigint;
  v_town text;
  v_amount integer;
  v_lost text;
  v_detail text;
  v_tone text := 'bad';
  v_settings private.settings;
  v_paid bigint;
begin
  select * into v_settings from private.settings;
  select * into v_caravan from public.caravans where player_id = p_player_id for update;
  select coins into v_coins from public.players where player_id = p_player_id for update;
  v_town := (select name from public.towns where id = v_caravan.town_id);

  if p_kind_id = 'bandits' and p_choice_id = 'fuir' then
    v_lost := private.lose_cargo(p_player_id, true, 0.25 * greatest(1 - private.bonus(p_player_id, 'strongbox'), 0));
    v_detail := format('Tu échappes aux bandits sur la route de %s', v_town)
      || coalesce(' en abandonnant ' || v_lost, '') || '.'
      || private.delay_text(private.trip_delay(p_player_id, p_base_seconds, 0.1));
  elsif p_kind_id = 'bandits' then
    v_amount := floor(least(floor((v_coins + private.cargo_value(p_player_id)) * v_settings.bandit_ransom_rate), v_settings.bandit_ransom_cap)
      * greatest(1 - private.bonus(p_player_id, 'strongbox'), 0))::integer;
    v_paid := least(v_coins, v_amount);
    update public.players set coins = coins - v_paid where player_id = p_player_id;
    v_lost := case when v_amount > v_paid then private.seize_cargo(p_player_id, v_amount - v_paid) end;
    v_detail := case
      when v_amount = 0 then format('Des bandits barrent la route de %s, mais ta caravane est vide : ils te laissent filer.', v_town)
      when v_lost is null then format('Des bandits barrent la route de %s : tu paies %s écus pour passer.', v_town, v_paid)
      when v_paid = 0 then format('Des bandits barrent la route de %s : ta bourse est vide, ils se servent dans ta cargaison (%s).', v_town, v_lost)
      else format('Des bandits barrent la route de %s : tu paies %s écus et ils se servent dans ta cargaison (%s).', v_town, v_paid, v_lost)
    end;
  elsif p_kind_id = 'orage' and p_choice_id = 'forcer' then
    v_lost := case when private.bonus(p_player_id, 'weatherproof') > 0 then null else private.lose_cargo(p_player_id, false, 0.1) end;
    v_detail := format('Tu traverses l''orage vers %s', v_town)
      || coalesce(', mais ' || v_lost || ' prend l''eau', ' sans dommage') || '.'
      || private.delay_text(private.trip_delay(p_player_id, p_base_seconds, 0.05));
  elsif p_kind_id = 'orage' then
    v_detail := format('Un orage sur la route de %s : tu t''abrites en attendant l''éclaircie.', v_town)
      || private.delay_text(private.trip_delay(p_player_id, p_base_seconds,
        case when private.bonus(p_player_id, 'weatherproof') > 0 then 0.15 else 0.3 end));
  elsif p_kind_id = 'peage' then
    v_amount := 3 * v_caravan.wagons + private.cargo_load(p_player_id) / 10;
    if p_choice_id = 'payer' and v_coins >= v_amount then
      update public.players set coins = coins - v_amount where player_id = p_player_id;
      v_detail := format('Péage sur la route de %s : %s écus.', v_town, v_amount);
      v_tone := 'neutral';
    else
      v_detail := format('Tu contournes le péage sur la route de %s.', v_town)
        || private.delay_text(private.trip_delay(p_player_id, p_base_seconds, 0.35));
    end if;
  else
    v_detail := private.find_goods(p_player_id, v_town);
    v_tone := 'good';
  end if;

  perform private.log_event(p_player_id, p_kind_id, p_at, v_tone, v_detail);
end;
$$;

revoke all on all functions in schema private from public, anon, authenticated;

notify pgrst, 'reload schema';