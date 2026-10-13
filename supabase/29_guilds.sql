alter table private.settings
  add column if not exists guild_max_members integer not null default 20 check (guild_max_members > 0),
  add column if not exists guild_rejoin_minutes integer not null default 60 check (guild_rejoin_minutes >= 0);

create table if not exists public.guilds (
  id uuid primary key default gen_random_uuid(),
  name text not null check (char_length(name) between 3 and 24),
  code text not null unique check (code ~ '^[A-HJ-NP-Z2-9]{6}$'),
  created_at timestamptz not null default now()
);

create unique index if not exists guilds_name_key on public.guilds (lower(name));

create table if not exists public.guild_members (
  id bigint generated always as identity primary key,
  player_id uuid not null unique references public.frontiers (player_id) on delete cascade,
  guild_id uuid not null references public.guilds (id) on delete cascade,
  leader boolean not null default false,
  joined_at timestamptz not null default now()
);

create unique index if not exists guild_members_one_leader on public.guild_members (guild_id) where leader;
create index if not exists guild_members_by_guild on public.guild_members (guild_id, joined_at);

create table if not exists public.guild_departures (
  player_id uuid primary key references public.frontiers (player_id) on delete cascade,
  left_at timestamptz not null default now()
);

alter table public.guilds enable row level security;
alter table public.guild_members enable row level security;
alter table public.guild_departures enable row level security;
revoke all on public.guilds, public.guild_members, public.guild_departures from anon, authenticated;

create or replace function private.guild_code() returns text
language sql volatile
set search_path = ''
as $$
  select string_agg(substr('ABCDEFGHJKLMNPQRSTUVWXYZ23456789', get_byte(source.bytes, position) % 32 + 1, 1), '' order by position)
  from (select uuid_send(gen_random_uuid()) as bytes) source, generate_series(0, 5) position;
$$;

create or replace function private.unused_guild_code() returns text
language plpgsql volatile
set search_path = ''
as $$
declare
  v_code text;
begin
  loop
    v_code := private.guild_code();
    exit when not exists (select 1 from public.guilds where code = v_code);
  end loop;

  return v_code;
end;
$$;

create or replace function private.tidy_name(p_name text) returns text
language sql immutable
set search_path = ''
as $$
  select btrim(regexp_replace(coalesce(p_name, ''), '\s+', ' ', 'g'));
$$;

create or replace function private.guild_name_valid(p_name text) returns boolean
language sql immutable
set search_path = ''
as $$
  select coalesce(char_length(p_name) between 3 and 24 and p_name ~ '^[A-Za-z0-9À-ÖØ-öø-ÿŒœ ''_-]+$', false);
$$;

create or replace function private.guild_entry_status(p_player uuid) returns text
language sql stable
set search_path = ''
as $$
  select case
    when not exists (select 1 from public.frontiers where player_id = p_player) then 'missing'
    when exists (select 1 from public.frontiers where player_id = p_player and hero_name is null) then 'no_hero'
    when exists (select 1 from public.guild_members where player_id = p_player) then 'already_member'
    when exists (
      select 1 from public.guild_departures departure, private.settings settings
      where departure.player_id = p_player
        and departure.left_at > now() - make_interval(mins => settings.guild_rejoin_minutes)) then 'cooldown'
  end;
$$;

create or replace function private.social_guild(p_player uuid) returns jsonb
language sql stable
set search_path = ''
as $$
  select jsonb_build_object(
    'name', guild.name,
    'code', guild.code,
    'leader', me.leader,
    'members', (
      select jsonb_agg(jsonb_build_object(
        'id', member.id,
        'name', coalesce(frontier.hero_name, '?'),
        'role', frontier.hero_role,
        'skin', frontier.hero_skin,
        'hair', frontier.hero_hair,
        'hair_color', frontier.hero_hair_color,
        'outfit', frontier.hero_outfit,
        'level', coalesce(hero.level, 1),
        'best_stage', frontier.best_stage,
        'leader', member.leader,
        'me', member.player_id = p_player,
        'away_seconds', greatest(0, extract(epoch from now() - frontier.last_seen)))
        order by member.leader desc, member.joined_at, member.id)
      from public.guild_members member
      join public.frontiers frontier on frontier.player_id = member.player_id
      left join public.frontier_fighters hero on hero.player_id = member.player_id and hero.fighter = 'hero'
      where member.guild_id = guild.id))
  from public.guild_members me
  join public.guilds guild on guild.id = me.guild_id
  where me.player_id = p_player;
$$;

create or replace function private.social_reply(p_player uuid, p_status text) returns jsonb
language sql stable
set search_path = ''
as $$
  select jsonb_build_object(
    'status', p_status,
    'server_time', now(),
    'guild', private.social_guild(p_player),
    'rejoin_in', coalesce((
      select greatest(0, extract(epoch from departure.left_at + make_interval(mins => settings.guild_rejoin_minutes) - now()))
      from public.guild_departures departure, private.settings settings
      where departure.player_id = p_player), 0));
$$;

create or replace function private.guild_member_left() returns trigger
language plpgsql
security definer
set search_path = ''
as $$
begin
  if not exists (select 1 from public.guild_members where guild_id = old.guild_id) then
    delete from public.guilds where id = old.guild_id;
  elsif old.leader and not exists (select 1 from public.guild_members where guild_id = old.guild_id and leader) then
    update public.guild_members
    set leader = true
    where id = (select id from public.guild_members where guild_id = old.guild_id order by joined_at, id limit 1);
  end if;

  return null;
end;
$$;

create or replace trigger guild_member_left
after delete on public.guild_members
for each row execute function private.guild_member_left();

create or replace function public.get_social() returns jsonb
language sql stable
security definer
set search_path = ''
as $$
  select private.social_reply(auth.uid(),
    case when exists (select 1 from public.frontiers where player_id = auth.uid()) then 'ok' else 'missing' end);
$$;

create or replace function public.create_guild(p_name text) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player uuid := auth.uid();
  v_name text := private.tidy_name(p_name);
  v_status text := private.guild_entry_status(v_player);
  v_guild uuid;
begin
  if v_status is not null then
    return private.social_reply(v_player, v_status);
  end if;

  if not private.guild_name_valid(v_name) then
    return private.social_reply(v_player, 'invalid_name');
  end if;

  if not private.hero_name_clean(v_name) then
    return private.social_reply(v_player, 'banned_name');
  end if;

  begin
    insert into public.guilds (name, code) values (v_name, private.unused_guild_code()) returning id into v_guild;
  exception
    when unique_violation then
      return private.social_reply(v_player, 'name_taken');
  end;

  insert into public.guild_members (player_id, guild_id, leader) values (v_player, v_guild, true);
  return private.social_reply(v_player, 'ok');
end;
$$;

create or replace function public.join_guild(p_code text) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player uuid := auth.uid();
  v_status text := private.guild_entry_status(v_player);
  v_guild public.guilds;
begin
  if v_status is not null then
    return private.social_reply(v_player, v_status);
  end if;

  select * into v_guild from public.guilds where code = upper(btrim(coalesce(p_code, ''))) for update;
  if not found then
    return private.social_reply(v_player, 'unknown_code');
  end if;

  if (select count(*) from public.guild_members where guild_id = v_guild.id) >= (select guild_max_members from private.settings) then
    return private.social_reply(v_player, 'full');
  end if;

  insert into public.guild_members (player_id, guild_id) values (v_player, v_guild.id);
  return private.social_reply(v_player, 'ok');
end;
$$;

create or replace function public.leave_guild() returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player uuid := auth.uid();
begin
  perform 1 from public.guilds guild
  join public.guild_members member on member.guild_id = guild.id
  where member.player_id = v_player
  for update of guild;

  delete from public.guild_members where player_id = v_player;
  if not found then
    return private.social_reply(v_player, 'not_member');
  end if;

  insert into public.guild_departures (player_id, left_at) values (v_player, now())
  on conflict (player_id) do update set left_at = excluded.left_at;
  return private.social_reply(v_player, 'ok');
end;
$$;

create or replace function public.kick_member(p_member bigint) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player uuid := auth.uid();
  v_me public.guild_members;
begin
  select * into v_me from public.guild_members where player_id = v_player;
  if not found then
    return private.social_reply(v_player, 'not_member');
  end if;

  if not v_me.leader then
    return private.social_reply(v_player, 'not_leader');
  end if;

  delete from public.guild_members where id = p_member and guild_id = v_me.guild_id and player_id <> v_player;
  if not found then
    return private.social_reply(v_player, 'unknown_member');
  end if;

  return private.social_reply(v_player, 'ok');
end;
$$;

create or replace function public.rename_guild(p_name text) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player uuid := auth.uid();
  v_name text := private.tidy_name(p_name);
  v_me public.guild_members;
begin
  select * into v_me from public.guild_members where player_id = v_player;
  if not found then
    return private.social_reply(v_player, 'not_member');
  end if;

  if not v_me.leader then
    return private.social_reply(v_player, 'not_leader');
  end if;

  if not private.guild_name_valid(v_name) then
    return private.social_reply(v_player, 'invalid_name');
  end if;

  if not private.hero_name_clean(v_name) then
    return private.social_reply(v_player, 'banned_name');
  end if;

  begin
    update public.guilds set name = v_name where id = v_me.guild_id;
  exception
    when unique_violation then
      return private.social_reply(v_player, 'name_taken');
  end;

  return private.social_reply(v_player, 'ok');
end;
$$;

create or replace function public.new_guild_code() returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player uuid := auth.uid();
  v_me public.guild_members;
begin
  select * into v_me from public.guild_members where player_id = v_player;
  if not found then
    return private.social_reply(v_player, 'not_member');
  end if;

  if not v_me.leader then
    return private.social_reply(v_player, 'not_leader');
  end if;

  update public.guilds set code = private.unused_guild_code() where id = v_me.guild_id;
  return private.social_reply(v_player, 'ok');
end;
$$;

revoke all on function public.get_social(), public.create_guild(text), public.join_guild(text), public.leave_guild(),
  public.kick_member(bigint), public.rename_guild(text), public.new_guild_code() from public, anon;
grant execute on function public.get_social(), public.create_guild(text), public.join_guild(text), public.leave_guild(),
  public.kick_member(bigint), public.rename_guild(text), public.new_guild_code() to authenticated;
