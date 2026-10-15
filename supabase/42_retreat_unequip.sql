create or replace function private.frontier_retreated() returns trigger
language plpgsql
security definer
set search_path = ''
as $$
begin
  delete from public.hero_equipment where player_id = new.player_id;
  return null;
end;
$$;

create or replace trigger frontier_retreated
after update of retreats on public.frontiers
for each row when (new.retreats > old.retreats)
execute function private.frontier_retreated();

delete from public.hero_equipment equipment
using public.frontier_fighters fighter
where fighter.player_id = equipment.player_id
  and fighter.fighter = equipment.fighter
  and fighter.level = 0;
