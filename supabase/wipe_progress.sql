do $$
declare
  v_frontiers bigint := (select count(*) from public.frontiers);
  v_guilds bigint := (select count(*) from public.guilds);
  v_items bigint := (select count(*) from public.items);
begin
  truncate public.frontiers, public.guilds, public.titans restart identity cascade;
  raise notice 'Progression effacée : % parties, % guildes et % objets. Les comptes et les réglages sont gardés.',
    v_frontiers, v_guilds, v_items;
end;
$$;
