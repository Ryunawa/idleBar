create or replace function public.salvage_item(p_item bigint) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player uuid := auth.uid();
begin
  delete from public.items where id = p_item and owner_id = v_player;
  return private.social_reply(v_player, case when found then 'ok' else 'unknown_item' end);
end;
$$;

revoke all on function public.salvage_item(bigint) from public, anon;
grant execute on function public.salvage_item(bigint) to authenticated;
