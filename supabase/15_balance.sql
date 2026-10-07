alter table private.settings add column if not exists trade_xp_value integer not null default 10 check (trade_xp_value > 0);

update private.settings set takeover_minutes = 90 where takeover_minutes = 180;

update public.goods set base_price = 560 where id = 'chariot';

update public.markets
set base_price = round(goods.base_price * coalesce(
  case markets.town_id when 'port-sable' then 1.25 when 'ambrevault' then 1.35 end,
  1 + (((hashtext(markets.town_id || ':' || markets.good_id)::bigint % 21) + 21) % 21 - 10) / 100.0
), 2)
from public.goods
where goods.id = markets.good_id and markets.good_id = 'chariot';

create or replace function private.max_queue(p_level integer) returns integer
language sql immutable
set search_path = ''
as $$
  select 6 + 2 * p_level;
$$;

create or replace function private.branch_price(p_branch_number integer) returns integer
language sql immutable
set search_path = ''
as $$
  select (round(200 * power(1.8, p_branch_number - 1) / 10) * 10)::integer;
$$;

create or replace function private.gain_trade_xp(p_player_id uuid, p_value bigint) returns void
language plpgsql
set search_path = ''
as $$
declare
  v_craft text;
  v_total bigint;
  v_rate integer := (select trade_xp_value from private.settings);
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
  update public.masteries set trade_credit = (v_total % v_rate)::integer where player_id = p_player_id and craft_id = v_craft;
  perform private.gain_xp(p_player_id, (v_total / v_rate)::integer);
end;
$$;
