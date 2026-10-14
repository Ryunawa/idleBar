alter table private.settings add column if not exists level_curve integer not null default 1;

create or replace function private.steepen_levels() returns void
language plpgsql
set search_path = ''
as $$
begin
  if (select level_curve from private.settings) < 2 then
    update public.frontiers set revision = revision + 1, saved_by = null;
    update public.frontier_fighters
    set level = case when level > 0 then greatest(1, round(level / 2.876)::integer) else 0 end,
        xp = 0;
    update private.settings set level_curve = 2;
  end if;
end;
$$;

select private.steepen_levels();

create or replace function private.hero_power(p_player uuid) returns double precision
language sql stable
set search_path = ''
as $$
  select 11 * greatest(hero.level, 1) * power(3.2::double precision, least(greatest(hero.level, 1), 1500) / 10)
    * (1 + 0.025 * frontier.souls)
  from public.frontiers frontier
  join public.frontier_fighters hero on hero.player_id = frontier.player_id and hero.fighter = 'hero'
  where frontier.player_id = p_player;
$$;

update private.settings set min_client_version = '0.8.2';
