update private.settings set upgrade_share = 0.3;

create or replace function private.upgrade_price(p_player uuid, p_item public.items, p_count integer) returns numeric
language sql stable
set search_path = ''
as $$
  select private.gear_base(p_player, greatest(1, frontier.max_stage, coalesce(p_item.stage, p_item.item_level, frontier.max_stage)), p_item.rarity)
    * settings.upgrade_share
    * power(settings.upgrade_growth, p_item.upgrades) * (power(settings.upgrade_growth, p_count) - 1) / (settings.upgrade_growth - 1)
  from public.frontiers frontier, private.settings settings
  where frontier.player_id = p_player;
$$;
