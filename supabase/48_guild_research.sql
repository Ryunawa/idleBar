alter table private.settings
  add column if not exists seal_bosses_per_hour integer not null default 6 check (seal_bosses_per_hour > 0),
  add column if not exists research_growth numeric not null default 1.6 check (research_growth >= 1),
  add column if not exists research_time_growth numeric not null default 1.5 check (research_time_growth >= 1),
  add column if not exists research_members_step integer not null default 5 check (research_members_step > 0);

alter table private.reward_credits add column if not exists seals_at timestamptz;

alter table public.titan_tiers add column if not exists seals integer not null default 0 check (seals >= 0);

update public.titan_tiers tier
set seals = reward.seals
from (values (500, 10), (3000, 25), (10000, 50)) as reward(points, seals)
where tier.points = reward.points;

create table if not exists public.research_nodes (
  id text primary key,
  branch text not null check (branch in ('war', 'prosperity', 'rally')),
  position smallint not null check (position between 1 and 4),
  max_level smallint not null check (max_level >= 1),
  cost integer not null check (cost > 0),
  hours numeric not null check (hours > 0),
  effect numeric not null check (effect > 0),
  unique (branch, position)
);

insert into public.research_nodes (id, branch, position, max_level, cost, hours, effect) values
  ('common_forge', 'war', 1, 5, 15, 2, 4),
  ('ramparts', 'war', 2, 5, 22, 5, 5),
  ('veterans', 'war', 3, 3, 30, 10, 10),
  ('final_assault', 'war', 4, 3, 40, 20, 6),
  ('caravans', 'prosperity', 1, 5, 15, 2, 4),
  ('schools', 'prosperity', 2, 5, 22, 5, 5),
  ('heavy_convoys', 'prosperity', 3, 3, 30, 10, 15),
  ('guild_market', 'prosperity', 4, 3, 40, 20, 5),
  ('war_cry', 'rally', 1, 5, 15, 2, 10),
  ('sanctuary', 'rally', 2, 3, 22, 5, 4),
  ('messengers', 'rally', 3, 3, 30, 10, 30),
  ('hospitality', 'rally', 4, 1, 40, 20, 1)
on conflict (id) do update
set branch = excluded.branch, position = excluded.position, max_level = excluded.max_level,
  cost = excluded.cost, hours = excluded.hours, effect = excluded.effect;

create table if not exists public.player_seals (
  player_id uuid primary key references public.frontiers (player_id) on delete cascade,
  seals integer not null default 0 check (seals >= 0),
  earned bigint not null default 0 check (earned >= 0),
  given bigint not null default 0 check (given >= 0)
);

create table if not exists public.guild_research (
  guild_id uuid not null references public.guilds (id) on delete cascade,
  node text not null references public.research_nodes (id),
  level smallint not null default 0 check (level >= 0),
  queued smallint not null default 0 check (queued >= 0),
  funded integer not null default 0 check (funded >= 0),
  cost integer check (cost is null or cost > 0),
  primary key (guild_id, node),
  check (cost is not null or funded = 0)
);

create table if not exists public.research_queue (
  id bigint generated always as identity primary key,
  guild_id uuid not null references public.guilds (id) on delete cascade,
  node text not null references public.research_nodes (id),
  level smallint not null check (level >= 1),
  starts_at timestamptz not null,
  ends_at timestamptz not null,
  unique (guild_id, node, level),
  check (ends_at > starts_at)
);

create index if not exists research_queue_by_guild on public.research_queue (guild_id, ends_at);

alter table public.research_nodes enable row level security;
alter table public.player_seals enable row level security;
alter table public.guild_research enable row level security;
alter table public.research_queue enable row level security;
revoke all on public.research_nodes, public.player_seals, public.guild_research, public.research_queue from anon, authenticated;

create or replace function private.grant_seals(p_player uuid, p_seals integer) returns integer
language plpgsql
set search_path = ''
as $$
begin
  if coalesce(p_seals, 0) <= 0 or not exists (select 1 from public.frontiers where player_id = p_player) then
    return 0;
  end if;

  insert into public.player_seals (player_id, seals, earned) values (p_player, p_seals, p_seals)
  on conflict (player_id) do update
  set seals = public.player_seals.seals + excluded.seals, earned = public.player_seals.earned + excluded.earned;
  return p_seals;
end;
$$;

create or replace function private.research_level(p_guild uuid, p_node text) returns integer
language sql stable
set search_path = ''
as $$
  select coalesce((
      select research.level::integer from public.guild_research research
      where research.guild_id = p_guild and research.node = p_node), 0)
    + (select count(*)::integer from public.research_queue queue
      where queue.guild_id = p_guild and queue.node = p_node and queue.ends_at <= now());
$$;

create or replace function private.research_effect(p_player uuid, p_node text) returns numeric
language sql stable
set search_path = ''
as $$
  select coalesce((
    select private.research_level(member.guild_id, node.id) * node.effect
    from public.guild_members member, public.research_nodes node
    where member.player_id = p_player and node.id = p_node), 0);
$$;

create or replace function private.research_cost(p_guild uuid, p_node text, p_level integer) returns integer
language sql stable
set search_path = ''
as $$
  select ceil(round(node.cost * power(settings.research_growth, p_level - 1)
    * greatest(1, (select count(*) from public.guild_members member where member.guild_id = p_guild)::numeric
      / settings.research_members_step), 6))::integer
  from public.research_nodes node, private.settings settings
  where node.id = p_node;
$$;

create or replace function private.research_hours(p_node text, p_level integer) returns numeric
language sql stable
set search_path = ''
as $$
  select node.hours * power(settings.research_time_growth, p_level - 1)
  from public.research_nodes node, private.settings settings
  where node.id = p_node;
$$;

create or replace function private.research_ready(p_guild uuid) returns table (node text, level integer, cost integer)
language sql stable
set search_path = ''
as $$
  select research.node, research.level + research.queued + 1, research.cost
  from public.guild_research research
  join public.research_nodes info on info.id = research.node
  where research.guild_id = p_guild
    and research.cost is not null
    and research.level + research.queued < info.max_level
    and research.funded >= research.cost
  order by array_position(array['war', 'prosperity', 'rally'], info.branch), info.position
  limit 1;
$$;

create or replace function private.advance_research(p_guild uuid) returns void
language plpgsql
set search_path = ''
as $$
declare
  v_next record;
  v_start timestamptz;
begin
  if p_guild is null or not (
    exists (select 1 from public.research_queue queue where queue.guild_id = p_guild and queue.ends_at <= now())
    or exists (select 1 from private.research_ready(p_guild))) then
    return;
  end if;

  perform 1 from public.guilds where id = p_guild for no key update;
  with done as (
    delete from public.research_queue queue where queue.guild_id = p_guild and queue.ends_at <= now() returning queue.node)
  update public.guild_research research
  set level = research.level + finished.levels, queued = research.queued - finished.levels
  from (select done.node, count(*)::smallint as levels from done group by done.node) finished
  where research.guild_id = p_guild and research.node = finished.node;

  loop
    select * into v_next from private.research_ready(p_guild);
    exit when not found;
    select greatest(now(), coalesce(max(queue.ends_at), now())) into v_start from public.research_queue queue where queue.guild_id = p_guild;
    insert into public.research_queue (guild_id, node, level, starts_at, ends_at)
    values (p_guild, v_next.node, v_next.level, v_start,
      v_start + make_interval(secs => private.research_hours(v_next.node, v_next.level)::double precision * 3600));
    update public.guild_research
    set funded = funded - v_next.cost, queued = queued + 1, cost = null
    where guild_id = p_guild and node = v_next.node;
  end loop;
end;
$$;

create or replace function private.social_research(p_player uuid) returns jsonb
language sql stable
set search_path = ''
as $$
  select jsonb_build_object(
    'seals', coalesce((select owned.seals from public.player_seals owned where owned.player_id = p_player), 0),
    'members', (select count(*) from public.guild_members mate where mate.guild_id = me.guild_id),
    'nodes', coalesce((
      select jsonb_agg(jsonb_build_object(
          'id', node.id,
          'level', coalesce(research.level, 0),
          'queued', coalesce(research.queued, 0),
          'funded', coalesce(research.funded, 0),
          'cost', case when coalesce(research.level + research.queued, 0) < node.max_level
            then coalesce(research.cost, private.research_cost(me.guild_id, node.id, coalesce(research.level + research.queued, 0) + 1)) end,
          'frozen', research.cost is not null,
          'hours', case when coalesce(research.level + research.queued, 0) < node.max_level
            then private.research_hours(node.id, coalesce(research.level + research.queued, 0) + 1) end)
        order by array_position(array['war', 'prosperity', 'rally'], node.branch), node.position)
      from public.research_nodes node
      left join public.guild_research research on research.guild_id = me.guild_id and research.node = node.id
      where me.guild_id is not null), '[]'::jsonb),
    'queue', coalesce((
      select jsonb_agg(jsonb_build_object(
          'node', queue.node,
          'level', queue.level,
          'starts_in', greatest(0, extract(epoch from queue.starts_at - now())),
          'ends_in', greatest(0, extract(epoch from queue.ends_at - now())),
          'hours', extract(epoch from queue.ends_at - queue.starts_at) / 3600)
        order by queue.ends_at, queue.id)
      from public.research_queue queue
      where queue.guild_id = me.guild_id and queue.ends_at > now()), '[]'::jsonb))
  from (select (select member.guild_id from public.guild_members member where member.player_id = p_player) as guild_id) me;
$$;

create or replace function private.frontier_earns_seals() returns trigger
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_rate integer := (select seal_bosses_per_hour from private.settings);
  v_floor timestamptz := now() - interval '1 hour';
  v_from timestamptz;
  v_seals integer;
begin
  if new.bosses_killed <= old.bosses_killed then
    return null;
  end if;

  insert into private.reward_credits (player_id) values (new.player_id) on conflict (player_id) do nothing;
  select greatest(coalesce(credit.seals_at, v_floor), v_floor) into v_from
  from private.reward_credits credit where credit.player_id = new.player_id for update;
  v_seals := least(new.bosses_killed - old.bosses_killed, floor(extract(epoch from now() - v_from) / 3600 * v_rate))::integer;
  if v_seals <= 0 then
    return null;
  end if;

  update private.reward_credits
  set seals_at = v_from + make_interval(secs => v_seals * 3600.0 / v_rate)
  where player_id = new.player_id;
  perform private.grant_seals(new.player_id, v_seals);
  return null;
end;
$$;

create or replace trigger frontier_earns_seals
after update of bosses_killed on public.frontiers
for each row execute function private.frontier_earns_seals();

create or replace function private.titan_reward(p_monsters bigint) returns integer[]
language sql stable
set search_path = ''
as $$
  select array[percent, hours, seals] from public.titan_tiers where points <= p_monsters order by points desc limit 1;
$$;

create or replace function private.frontier_feeds_titan() returns trigger
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_titan public.titans;
  v_settings private.settings;
  v_mine bigint;
  v_gift bigint;
  v_guild uuid;
  v_bosses bigint := greatest(0, new.bosses_killed - old.bosses_killed);
begin
  if new.monsters_killed <= old.monsters_killed then
    return null;
  end if;

  v_titan := private.current_titan();
  select * into v_titan from public.titans where week = v_titan.week for update;
  if v_titan.fallen_at is not null then
    return null;
  end if;

  select * into v_settings from private.settings;
  select coalesce(max(monsters), 0) into v_mine
  from public.titan_contributions where week = v_titan.week and player_id = new.player_id;
  v_gift := private.spend_credit(new.player_id, true, least(
    greatest(0, new.monsters_killed - old.monsters_killed - v_bosses) + v_bosses * v_settings.titan_boss_weight,
    greatest(0, v_titan.hp / 2 - v_mine),
    greatest(0, v_titan.hp - v_titan.damage)), v_settings.titan_kills_per_minute);
  v_gift := least(
    floor(v_gift * (1 + private.research_effect(new.player_id, 'war_cry') / 100))::bigint,
    greatest(0, v_titan.hp / 2 - v_mine),
    greatest(0, v_titan.hp - v_titan.damage));
  if v_gift <= 0 then
    return null;
  end if;

  insert into public.titan_contributions (week, player_id, monsters) values (v_titan.week, new.player_id, v_gift)
  on conflict (week, player_id) do update set monsters = public.titan_contributions.monsters + excluded.monsters;

  update public.titans
  set damage = damage + v_gift,
      fallen_at = case when damage + v_gift >= hp then coalesce(fallen_at, now()) else fallen_at end
  where week = v_titan.week;

  select guild_id into v_guild from public.guild_members where player_id = new.player_id;
  if v_guild is not null then
    insert into public.guild_titan (week, guild_id, monsters) values (v_titan.week, v_guild, v_gift)
    on conflict (week, guild_id) do update set monsters = public.guild_titan.monsters + excluded.monsters;
  end if;

  return null;
end;
$$;

create or replace function private.social_titan(p_player uuid, p_titan public.titans) returns jsonb
language sql stable
set search_path = ''
as $$
  select jsonb_build_object(
    'week', p_titan.week,
    'kind', extract(week from p_titan.week)::integer % 4,
    'hp', p_titan.hp,
    'damage', p_titan.damage,
    'fallen', p_titan.fallen_at is not null,
    'ends_in', extract(epoch from (p_titan.week + 7)::timestamp at time zone 'utc' - now()),
    'fighters', (select count(*) from public.titan_contributions where week = p_titan.week),
    'mine', coalesce((
      select monsters from public.titan_contributions where week = p_titan.week and player_id = p_player), 0),
    'tiers', coalesce((
      select jsonb_agg(jsonb_build_object('points', points, 'percent', percent, 'hours', hours, 'seals', seals) order by points)
      from public.titan_tiers), '[]'::jsonb),
    'last', (
      select jsonb_build_object(
        'kind', extract(week from last.week)::integer % 4,
        'fallen', last.fallen_at is not null,
        'mine', coalesce((
          select monsters from public.titan_contributions where week = last.week and player_id = p_player), 0))
      from public.titans last
      where last.week = p_titan.week - 7),
    'claimable', (
      select jsonb_build_object('percent', reward[1], 'hours', reward[2], 'seals', coalesce(reward[3], 0))
      from (select private.titan_reward((private.titan_claimable(p_player)).monsters) as reward) claim
      where claim.reward is not null),
    'ranking', coalesce((
      select jsonb_agg(jsonb_build_object('rank', ranked.rank, 'name', ranked.name, 'monsters', ranked.monsters, 'mine', ranked.mine)
        order by ranked.rank)
      from (
        select rank() over (order by score.monsters desc) as rank, guild.name, score.monsters,
          exists (select 1 from public.guild_members member where member.guild_id = guild.id and member.player_id = p_player) as mine
        from public.guild_titan score
        join public.guilds guild on guild.id = score.guild_id
        where score.week = p_titan.week) ranked
      where ranked.rank <= 10 or ranked.mine), '[]'::jsonb));
$$;

create or replace function public.claim_titan() returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player uuid := auth.uid();
  v_claim public.titan_contributions;
  v_reward integer[];
begin
  select * into v_claim from private.titan_claimable(v_player);
  if v_claim.week is null then
    return private.social_reply(v_player, 'nothing');
  end if;

  if private.bag_count(v_player) >= (select bag_size from private.settings) then
    return private.social_reply(v_player, 'bag_full');
  end if;

  v_reward := private.titan_reward(v_claim.monsters);
  update public.titan_contributions
  set claimed_at = now(),
      boost_percent = v_reward[1],
      boost_ends_at = now() + make_interval(hours => v_reward[2])
  where week = v_claim.week and player_id = v_player and claimed_at is null;
  if not found then
    return private.social_reply(v_player, 'nothing');
  end if;

  perform private.roll_item(v_player, null,
    case when v_reward[1] >= 100 then 'legendary' when v_reward[1] >= 50 then 'epic' else 'rare' end, 'titan');
  perform private.grant_seals(v_player, v_reward[3]);
  return private.social_reply(v_player, 'ok');
end;
$$;

create or replace function private.help_ready_in(p_player uuid, p_kind text) returns numeric
language sql stable
set search_path = ''
as $$
  select coalesce(greatest(0, extract(epoch from max(help.sent_at)
    + make_interval(mins => greatest(0, (select help_cooldown_minutes from private.settings)
      - private.research_effect(p_player, 'messengers')::integer)) - now())), 0)
  from public.guild_help help
  where help.sender_id = p_player and help.kind = p_kind;
$$;

create or replace function private.social_boosts(p_player uuid) returns jsonb
language sql stable
set search_path = ''
as $$
  select coalesce(jsonb_agg(boost), '[]'::jsonb)
  from (
    select jsonb_build_object(
      'source', 'convoy',
      'gold', settings.convoy_gold_percent + private.research_effect(p_player, 'heavy_convoys')::integer,
      'damage', 0,
      'ends_in', extract(epoch from private.convoy_end(p_player) - now())) as boost
    from private.settings settings
    where private.convoy_end(p_player) is not null
    union all
    select * from (
      select jsonb_build_object(
        'source', 'titan',
        'gold', contribution.boost_percent,
        'damage', contribution.boost_percent,
        'ends_in', extract(epoch from contribution.boost_ends_at - now()))
      from public.titan_contributions contribution
      where contribution.player_id = p_player and contribution.boost_ends_at > now()
      order by contribution.boost_percent desc, contribution.boost_ends_at desc
      limit 1) strongest) boosts;
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
        where receiver_id = v_target.player_id and kind = 'hero' and ends_at > now())
      >= v_settings.help_front_max + private.research_effect(v_target.player_id, 'hospitality')::integer then
      return private.social_reply(v_player, 'front_full');
    end if;

    v_ratio := least(
      coalesce(private.hero_power(v_player), 1) / greatest(coalesce(private.hero_power(v_target.player_id), 1), 1),
      v_settings.help_power_cap);
    insert into public.guild_help (
      sender_id, receiver_id, kind, hero_name, hero_role, hero_skin, hero_hair, hero_hair_color, hero_outfit, power_ratio, ends_at)
    values (
      v_player, v_target.player_id, 'hero', v_sender.hero_name, v_sender.hero_role, v_sender.hero_skin, v_sender.hero_hair,
      v_sender.hero_hair_color, v_sender.hero_outfit, greatest(v_ratio, 0.01),
      now() + make_interval(mins => v_settings.help_minutes + private.research_effect(v_player, 'veterans')::integer));
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

create or replace function private.gear_base(p_player uuid, p_level integer, p_rarity text) returns numeric
language sql stable
set search_path = ''
as $$
  select settings.shop_kills * (private.frontier_hp(p_level) * 0.35 + 2) * rarity.price
    * (1 - least(0.5, 0.005 * coalesce((
      select owned.level from public.frontier_plots owned where owned.player_id = p_player and owned.building = 'workshop'), 0)))
    * (1 - private.research_effect(p_player, 'guild_market') / 100)
  from private.settings settings, public.item_rarities rarity
  where rarity.id = p_rarity;
$$;

create or replace function private.social_reply(p_player uuid, p_status text) returns jsonb
language plpgsql
set search_path = ''
as $$
begin
  perform private.advance_research((select member.guild_id from public.guild_members member where member.player_id = p_player));
  return jsonb_build_object(
    'status', p_status,
    'server_time', now(),
    'guild', private.social_guild(p_player),
    'rejoin_in', coalesce((
      select greatest(0, extract(epoch from departure.left_at + make_interval(mins => settings.guild_rejoin_minutes) - now()))
      from public.guild_departures departure, private.settings settings
      where departure.player_id = p_player), 0),
    'help', private.social_help(p_player),
    'titan', private.social_titan(p_player, private.current_titan()),
    'items', private.social_items(p_player),
    'shop', private.social_shop(p_player),
    'research', private.social_research(p_player));
end;
$$;

create or replace function public.fund_research(p_node text, p_amount integer) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player uuid := auth.uid();
  v_guild uuid;
  v_node public.research_nodes;
  v_research public.guild_research;
  v_seals integer;
  v_cost integer;
  v_given integer;
begin
  select guild_id into v_guild from public.guild_members where player_id = v_player;
  if v_guild is null then
    return private.social_reply(v_player, 'not_member');
  end if;

  select * into v_node from public.research_nodes where id = p_node;
  if not found then
    return private.social_reply(v_player, 'unknown_node');
  end if;

  if coalesce(p_amount, 0) < 1 then
    return private.social_reply(v_player, 'refused');
  end if;

  select seals into v_seals from public.player_seals where player_id = v_player for update;
  perform 1 from public.guilds where id = v_guild for no key update;
  perform 1 from public.guild_members where player_id = v_player and guild_id = v_guild for share;
  if not found then
    return private.social_reply(v_player, 'not_member');
  end if;

  perform private.advance_research(v_guild);
  if v_node.position > 1 and private.research_level(v_guild, (
      select before.id from public.research_nodes before
      where before.branch = v_node.branch and before.position = v_node.position - 1)) < 1 then
    return private.social_reply(v_player, 'locked');
  end if;

  insert into public.guild_research (guild_id, node) values (v_guild, v_node.id) on conflict (guild_id, node) do nothing;
  select * into v_research from public.guild_research where guild_id = v_guild and node = v_node.id;
  if v_research.level + v_research.queued >= v_node.max_level then
    return private.social_reply(v_player, 'maxed');
  end if;

  if coalesce(v_seals, 0) < p_amount then
    return private.social_reply(v_player, 'too_poor');
  end if;

  v_cost := coalesce(v_research.cost, private.research_cost(v_guild, v_node.id, v_research.level + v_research.queued + 1));
  v_given := least(p_amount, greatest(0, v_cost - v_research.funded));
  update public.player_seals set seals = seals - v_given, given = given + v_given where player_id = v_player;
  update public.guild_research set funded = funded + v_given, cost = v_cost where guild_id = v_guild and node = v_node.id;
  perform private.advance_research(v_guild);
  return private.social_reply(v_player, 'ok') || jsonb_build_object('given', v_given);
end;
$$;

create or replace function public.move_research(p_node text, p_level integer, p_offset integer) returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_player uuid := auth.uid();
  v_guild uuid;
  v_leader boolean;
  v_row public.research_queue;
  v_entry public.research_queue;
  v_order bigint[];
  v_index integer;
  v_other integer;
  v_cursor timestamptz;
begin
  select guild_id into v_guild from public.guild_members where player_id = v_player;
  if v_guild is null then
    return private.social_reply(v_player, 'not_member');
  end if;

  if p_offset is null or p_offset not in (-1, 1) then
    return private.social_reply(v_player, 'refused');
  end if;

  perform 1 from public.guilds where id = v_guild for no key update;
  select leader into v_leader from public.guild_members where player_id = v_player and guild_id = v_guild for share;
  if not found then
    return private.social_reply(v_player, 'not_member');
  elsif not v_leader then
    return private.social_reply(v_player, 'not_leader');
  end if;

  perform private.advance_research(v_guild);
  select * into v_row from public.research_queue where guild_id = v_guild and node = p_node and level = p_level;
  if not found then
    return private.social_reply(v_player, 'not_queued');
  elsif v_row.starts_at <= now() then
    return private.social_reply(v_player, 'running');
  end if;

  v_order := array(
    select queue.id from public.research_queue queue
    where queue.guild_id = v_guild and queue.starts_at > now()
    order by queue.starts_at, queue.id);
  v_index := array_position(v_order, v_row.id);
  v_other := v_index + p_offset;
  if v_other < 1 or v_other > cardinality(v_order) then
    return private.social_reply(v_player, 'queue_edge');
  end if;

  if (select queue.node from public.research_queue queue where queue.id = v_order[v_other]) = v_row.node then
    return private.social_reply(v_player, 'refused');
  end if;

  v_order[v_index] := v_order[v_other];
  v_order[v_other] := v_row.id;
  select coalesce(max(queue.ends_at), now()) into v_cursor
  from public.research_queue queue
  where queue.guild_id = v_guild and queue.starts_at <= now();
  for v_position in 1 .. cardinality(v_order) loop
    select * into v_entry from public.research_queue where id = v_order[v_position];
    update public.research_queue
    set starts_at = v_cursor, ends_at = v_cursor + (v_entry.ends_at - v_entry.starts_at)
    where id = v_entry.id;
    v_cursor := v_cursor + (v_entry.ends_at - v_entry.starts_at);
  end loop;

  return private.social_reply(v_player, 'ok');
end;
$$;

revoke all on function public.fund_research(text, integer), public.move_research(text, integer, integer),
  public.send_help(bigint, text), public.claim_titan() from public, anon;
grant execute on function public.fund_research(text, integer), public.move_research(text, integer, integer),
  public.send_help(bigint, text), public.claim_titan() to authenticated;
