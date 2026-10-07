\set ON_ERROR_STOP on

update private.market_cycle set rolled_at = '2000-01-01', drifted_at = now();
select private.settle_markets();

do $$
begin
  assert not exists (
    select town_id from public.markets
    group by town_id
    having count(*) filter (where target_price >= home_price * 1.34) <> 2
      or count(*) filter (where target_price <= home_price * 0.76) <> 2
  ), 'every town gets two goods in demand and two in surplus';
  assert (select count(*) from public.markets where target_price <> home_price) = 4 * (select count(*) from public.towns),
    'the other goods aim at their usual price';
  assert (select rolled_at from private.market_cycle) > now() - interval '1 minute', 'the cycle remembers when it rolled';
end;
$$;

create temp table targets as select town_id, good_id, target_price, base_price from public.markets;

select private.settle_markets();

do $$
begin
  assert not exists (
    select 1 from public.markets join targets using (town_id, good_id)
    where markets.target_price <> targets.target_price
  ), 'needs do not change again before the next day';
  assert not exists (
    select 1 from public.markets join targets using (town_id, good_id)
    where markets.base_price <> targets.base_price
  ), 'prices do not move more than once every fifteen minutes';
end;
$$;

update private.market_cycle set drifted_at = now() - interval '6 hours';
select private.settle_markets();

do $$
begin
  assert not exists (
    select 1 from public.markets join targets using (town_id, good_id)
    where markets.target_price <> targets.base_price
      and abs(markets.base_price - markets.target_price) > abs(targets.base_price - targets.target_price) * 0.4 + 0.01
  ), 'after six hours prices have covered most of the way to the new needs';
  assert exists (
    select 1 from public.markets join targets using (town_id, good_id)
    where markets.base_price <> targets.base_price
  ), 'prices move towards the new needs';
end;
$$;

update private.market_cycle set rolled_at = now() - interval '25 hours';
select private.settle_markets();

do $$
begin
  assert exists (
    select 1 from public.markets join targets using (town_id, good_id)
    where markets.target_price <> targets.target_price
  ), 'a new day brings new needs';
end;
$$;

select 'All market tests passed' as result;