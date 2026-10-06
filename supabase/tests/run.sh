#!/usr/bin/env bash
set -euo pipefail

container="idlebar-sql-tests"
here="$(cd "$(dirname "$0")" && pwd)"
schema="$(dirname "$here")"

docker rm -f "$container" >/dev/null 2>&1 || true
docker run -d --rm --name "$container" -e POSTGRES_PASSWORD=postgres postgres:16-alpine >/dev/null
trap 'docker rm -f "$container" >/dev/null 2>&1 || true' EXIT

until docker logs "$container" 2>&1 | grep -q "PostgreSQL init process complete" && docker exec "$container" pg_isready -U postgres >/dev/null 2>&1; do
  sleep 1
done

for file in "$here/supabase_stub.sql" "$schema"/0*.sql "$here"/*_test.sql; do
  docker exec -i "$container" psql -q -U postgres -v ON_ERROR_STOP=1 < "$file"
done
