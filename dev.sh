#!/usr/bin/env bash
# Local dev loop: Postgres via Docker, API + frontend running natively with hot reload.
# No image rebuilds needed when you edit code — Ctrl+C stops everything.
set -e
cd "$(dirname "$0")"

echo "Starting Postgres (Docker)..."
docker compose up -d postgres

echo "Waiting for Postgres to be healthy..."
until [ "$(docker inspect -f '{{.State.Health.Status}}' mtgdecklab-postgres-1 2>/dev/null)" = "healthy" ]; do
  sleep 1
done

trap 'kill 0' EXIT INT TERM

echo "Starting API (dotnet watch) on http://localhost:5052 ..."
dotnet watch run --project src/MtgDeckLab.API &

echo "Starting frontend (vite) on http://localhost:5173 ..."
(cd frontend && npm run dev) &

wait
