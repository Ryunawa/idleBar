alter table public.frontier_skills add column if not exists seconds double precision not null default 0 check (seconds >= 0 and seconds < 'Infinity');
alter table public.frontier_skills add column if not exists placed boolean not null default false;
alter table public.frontier_skills drop constraint if exists frontier_skills_level_check;
alter table public.frontier_skills add constraint frontier_skills_level_check check (level >= 0);
alter table public.frontier_slots drop column if exists seconds;
alter table public.frontier_slots drop column if exists cooldown;

notify pgrst, 'reload schema';
