insert into public.item_stats (id, base, slots, roles) values
  ('damage', 4, '{weapon,helm,arms,ring}', '{warrior,mage}'),
  ('health', 6, '{armor,helm,legs,feet,ring}', '{tank,support}'),
  ('critical', 2, '{weapon,helm,arms,ring}', '{archer,mage}'),
  ('healing', 5, '{armor,helm,legs,ring}', '{support}'),
  ('speed', 3, '{weapon,helm,arms,feet,ring}', '{warrior,archer}'),
  ('gold', 6, '{armor,helm,ring}', '{warrior}'),
  ('xp', 10, '{armor,helm,legs,ring}', '{tank,support}'),
  ('recharge', 4, '{weapon,helm,feet,ring}', '{tank,archer,mage}'),
  ('boss', 6, '{weapon,helm,ring}', '{warrior,mage}')
on conflict (id) do update set base = excluded.base, slots = excluded.slots, roles = excluded.roles;

insert into public.item_slots (id, position) values ('arms', 3), ('legs', 4), ('feet', 5), ('ring', 6)
on conflict (id) do nothing;

create or replace function private.rescale_values(p_scale numeric) returns void
language plpgsql
set search_path = ''
as $$
declare
  v_ratio numeric;
begin
  if (select item_rules from private.settings) >= 3 then
    return;
  end if;

  select p_scale / settings.item_scale into v_ratio from private.settings settings;
  update public.items
  set value_a = greatest(0.01, round(value_a * v_ratio, 2)),
      value_b = case when value_b is not null then greatest(0.01, round(value_b * v_ratio, 2)) end;
  update public.shop_offers
  set value_a = greatest(0.01, round(value_a * v_ratio, 2)),
      value_b = case when value_b is not null then greatest(0.01, round(value_b * v_ratio, 2)) end;
  update private.settings set item_scale = p_scale, item_rules = 3;
end;
$$;

select private.rescale_values(0.22);

update private.settings set min_client_version = '0.9.0';
