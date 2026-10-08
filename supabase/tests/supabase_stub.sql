do $$
begin
  if not exists (select 1 from pg_roles where rolname = 'anon') then
    create role anon nologin;
  end if;
  if not exists (select 1 from pg_roles where rolname = 'authenticated') then
    create role authenticated nologin;
  end if;
end;
$$;

create schema if not exists auth;
create table if not exists auth.users (id uuid primary key);

create or replace function auth.uid() returns uuid
language sql stable
as $$
  select nullif(current_setting('request.jwt.claim.sub', true), '')::uuid;
$$;

grant usage on schema auth, public to anon, authenticated;
grant execute on function auth.uid() to anon, authenticated;

alter default privileges in schema public grant all on tables to anon, authenticated;
alter default privileges in schema public grant all on functions to anon, authenticated;
alter default privileges in schema public grant all on sequences to anon, authenticated;

create schema if not exists realtime;
create table if not exists realtime.messages (
  topic text not null,
  extension text not null,
  payload jsonb,
  event text,
  private boolean,
  inserted_at timestamptz not null default now()
);

create or replace function realtime.topic() returns text
language sql stable
as $$
  select nullif(current_setting('realtime.topic', true), '');
$$;

create or replace function realtime.send(payload jsonb, event text, topic text, private boolean default true) returns void
language sql
as $$
  insert into realtime.messages (topic, extension, payload, event, private) values (topic, 'broadcast', payload, event, private);
$$;
