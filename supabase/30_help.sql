alter table private.settings
  add column if not exists help_cooldown_minutes integer not null default 240 check (help_cooldown_minutes >= 0),
  add column if not exists help_minutes integer not null default 30 check (help_minutes > 0),
  add column if not exists convoy_cap_minutes integer not null default 120 check (convoy_cap_minutes > 0),
  add column if not exists convoy_gold_percent integer not null default 50 check (convoy_gold_percent > 0),
  add column if not exists help_front_max integer not null default 3 check (help_front_max > 0),
  add column if not exists help_power_cap numeric not null default 1.2 check (help_power_cap > 0);

create table if not exists public.help_kinds (
  id text primary key
);

insert into public.help_kinds (id) values ('hero'), ('convoy')
on conflict (id) do nothing;

create table if not exists public.guild_help (
  id bigint generated always as identity primary key,
  sender_id uuid not null references public.frontiers (player_id) on delete cascade,
  receiver_id uuid not null references public.frontiers (player_id) on delete cascade,
  kind text not null references public.help_kinds (id),
  hero_name text,
  hero_role text references public.role_kinds (id),
  hero_skin smallint,
  hero_hair smallint,
  hero_hair_color smallint,
  hero_outfit smallint,
  power_ratio double precision check (power_ratio is null or (power_ratio > 0 and power_ratio <= 10)),
  sent_at timestamptz not null default now(),
  ends_at timestamptz not null,
  check (sender_id <> receiver_id),
  check (kind <> 'hero' or (hero_name is not null and hero_role is not null and power_ratio is not null))
);

create index if not exists guild_help_by_receiver on public.guild_help (receiver_id, kind, ends_at);
create index if not exists guild_help_by_sender on public.guild_help (sender_id, kind, sent_at);

alter table public.help_kinds enable row level security;
alter table public.guild_help enable row level security;
revoke all on public.help_kinds, public.guild_help from anon, authenticated;

create or replace function private.hero_power(p_player uuid) returns double precision
language sql stable
set search_path = ''
as $$
  select 11 * greatest(hero.level, 1) * power(1.5::double precision, least(greatest(hero.level, 1), 1500) / 10)
    * (1 + 0.025 * frontier.souls)
  from public.frontiers frontier
  join public.frontier_fighters hero on hero.player_id = frontier.player_id and hero.fighter = 'hero'
  where frontier.player_id = p_player;
$$;

create or replace function private.convoy_end(p_player uuid) returns timestamptz
language sql stable
set search_path = ''
as $$
  select max(ends_at) from public.guild_help where receiver_id = p_player and kind = 'convoy' and ends_at > now();
$$;

create or replace function private.help_ready_in(p_player uuid, p_kind text) returns numeric
language sql stable
set search_path = ''
as $$
  select coalesce(greatest(0, extract(epoch from max(help.sent_at)
    + make_interval(mins => (select help_cooldown_minutes from private.settings)) - now())), 0)
  from public.guild_help help
  where help.sender_id = p_player and help.kind = p_kind;
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
        'away_seconds', greatest(0, extract(epoch from now() - frontier.last_seen)),
        'heroes', (
          select count(*) from public.guild_help help
          where help.receiver_id = member.player_id and help.kind = 'hero' and help.ends_at > now()),
        'convoy_in', coalesce(greatest(0, extract(epoch from private.convoy_end(member.player_id) - now())), 0))
        order by member.leader desc, member.joined_at, member.id)
      from public.guild_members member
      join public.frontiers frontier on frontier.player_id = member.player_id
      left join public.frontier_fighters hero on hero.player_id = member.player_id and hero.fighter = 'hero'
      where member.guild_id = guild.id))
  from public.guild_members me
  join public.guilds guild on guild.id = me.guild_id
  where me.player_id = p_player;
$$;

create or replace function private.social_boosts(p_player uuid) returns jsonb
language sql stable
set search_path = ''
as $$
  select coalesce(jsonb_agg(jsonb_build_object(
      'source', 'convoy',
      'gold', settings.convoy_gold_percent,
      'damage', 0,
      'ends_in', extract(epoch from private.convoy_end(p_player) - now()))), '[]'::jsonb)
  from private.settings settings
  where private.convoy_end(p_player) is not null;
$$;

create or replace function private.social_help(p_player uuid) returns jsonb
language sql stable
set search_path = ''
as $$
  select jsonb_build_object(
    'hero_ready_in', private.help_ready_in(p_player, 'hero'),
    'convoy_ready_in', private.help_ready_in(p_player, 'convoy'),
    'allies', coalesce((
      select jsonb_agg(jsonb_build_object(
        'id', help.id,
        'name', help.hero_name,
        'role', help.hero_role,
        'skin', help.hero_skin,
        'hair', help.hero_hair,
        'hair_color', help.hero_hair_color,
        'outfit', help.hero_outfit,
        'ratio', help.power_ratio,
        'ends_in', extract(epoch from help.ends_at - now()))
        order by help.sent_at, help.id)
      from public.guild_help help
      where help.receiver_id = p_player and help.kind = 'hero' and help.ends_at > now()), '[]'::jsonb),
    'boosts', private.social_boosts(p_player));
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
      where departure.player_id = p_player), 0),
    'help', private.social_help(p_player));
$$;

create or replace function public.send_help(p_member bigint, p_kind text) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player uuid := auth.uid();
  v_settings private.settings;
  v_me public.guild_members;
  v_target public.guild_members;
  v_sender public.frontiers;
  v_end timestamptz;
  v_ratio double precision;
begin
  select * into v_settings from private.settings;
  select * into v_me from public.guild_members where player_id = v_player;
  if not found then
    return private.social_reply(v_player, 'not_member');
  end if;

  select * into v_target from public.guild_members where id = p_member and guild_id = v_me.guild_id;
  if not found then
    return private.social_reply(v_player, 'unknown_member');
  end if;

  if v_target.player_id = v_player then
    return private.social_reply(v_player, 'self');
  end if;

  if not exists (select 1 from public.help_kinds where id = p_kind) then
    return private.social_reply(v_player, 'refused');
  end if;

  perform 1 from public.frontiers where player_id in (v_player, v_target.player_id) order by player_id for update;
  if private.help_ready_in(v_player, p_kind) > 0 then
    return private.social_reply(v_player, 'cooldown');
  end if;

  select * into v_sender from public.frontiers where player_id = v_player;
  if p_kind = 'hero' then
    if v_sender.hero_name is null then
      return private.social_reply(v_player, 'no_hero');
    end if;

    if (select count(*) from public.guild_help
        where receiver_id = v_target.player_id and kind = 'hero' and ends_at > now()) >= v_settings.help_front_max then
      return private.social_reply(v_player, 'front_full');
    end if;

    v_ratio := least(
      coalesce(private.hero_power(v_player), 1) / greatest(coalesce(private.hero_power(v_target.player_id), 1), 1),
      v_settings.help_power_cap);
    insert into public.guild_help (
      sender_id, receiver_id, kind, hero_name, hero_role, hero_skin, hero_hair, hero_hair_color, hero_outfit, power_ratio, ends_at)
    values (
      v_player, v_target.player_id, 'hero', v_sender.hero_name, v_sender.hero_role, v_sender.hero_skin, v_sender.hero_hair,
      v_sender.hero_hair_color, v_sender.hero_outfit, greatest(v_ratio, 0.01), now() + make_interval(mins => v_settings.help_minutes));
  else
    v_end := least(
      greatest(now(), coalesce(private.convoy_end(v_target.player_id), now())) + make_interval(mins => v_settings.help_minutes),
      now() + make_interval(mins => v_settings.convoy_cap_minutes));
    if v_end <= coalesce(private.convoy_end(v_target.player_id), now()) + interval '1 minute' then
      return private.social_reply(v_player, 'convoy_full');
    end if;

    insert into public.guild_help (sender_id, receiver_id, kind, ends_at) values (v_player, v_target.player_id, 'convoy', v_end);
  end if;

  delete from public.guild_help where sent_at < now() - interval '2 days';
  return private.social_reply(v_player, 'ok');
end;
$$;

revoke all on function public.send_help(bigint, text) from public, anon;
grant execute on function public.send_help(bigint, text) to authenticated;
