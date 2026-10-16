create or replace function public.set_hero(
  p_session uuid, p_name text, p_role text, p_skin integer, p_hair integer, p_hair_color integer, p_outfit integer)
returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player uuid := auth.uid();
  v_row public.frontiers;
  v_name text := btrim(regexp_replace(coalesce(p_name, ''), '\s+', ' ', 'g'));
begin
  select * into v_row from public.frontiers where player_id = v_player for update;
  if not found then
    return private.frontier_reply(v_player, 'missing');
  end if;

  if v_row.session_id is distinct from p_session then
    return private.frontier_reply(v_player, 'elsewhere');
  end if;

  if not private.hero_name_valid(v_name) then
    return private.frontier_reply(v_player, 'invalid_name');
  end if;

  if not private.hero_name_clean(v_name) then
    return private.frontier_reply(v_player, 'banned_name');
  end if;

  if not coalesce(p_skin between 0 and 3 and p_hair between 0 and 2 and p_hair_color between 0 and 5
    and p_outfit between 0 and 7 and exists (select 1 from public.role_kinds kind where kind.id = p_role), false) then
    return private.frontier_reply(v_player, 'refused');
  end if;

  if v_row.hero_name is not null and p_role <> v_row.hero_role
    and (v_row.retreats <= v_row.role_retreats or v_row.max_stage > (select skill_reset_stage from private.settings)) then
    return private.frontier_reply(v_player, 'role_locked');
  end if;

  update public.frontiers
  set role_retreats = retreats,
      hero_name = v_name,
      hero_role = p_role,
      hero_skin = p_skin,
      hero_hair = p_hair,
      hero_hair_color = p_hair_color,
      hero_outfit = p_outfit,
      last_seen = now()
  where player_id = v_player;

  return private.frontier_reply(v_player, 'ok');
end;
$$;
