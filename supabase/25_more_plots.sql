insert into public.plots (plot, unlock_stage) values (7, 61), (8, 71), (9, 81), (10, 91), (11, 101)
on conflict (plot) do nothing;

insert into public.building_kinds (id) values ('barracks'), ('chapel'), ('tavern'), ('workshop'), ('belfry')
on conflict (id) do nothing;

insert into public.frontier_plots (player_id, plot)
select frontier.player_id, plot.plot
from public.frontiers frontier
cross join public.plots plot
on conflict (player_id, plot) do nothing;
