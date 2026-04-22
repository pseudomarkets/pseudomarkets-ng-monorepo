#!/bin/sh
set -eu

: "${POSTGRES_HOST:=postgres}"
: "${POSTGRES_PORT:=5432}"
: "${POSTGRES_DB:=pseudomarkets_db}"
: "${POSTGRES_USER:=postgres}"

echo "Waiting for trading_instruments table..."
until psql -h "$POSTGRES_HOST" -p "$POSTGRES_PORT" -U "$POSTGRES_USER" -d "$POSTGRES_DB" -tAc "SELECT to_regclass('public.trading_instruments')" | grep -q "trading_instruments"; do
  sleep 2
done

for seed_file in /seed/trading-instruments/*.sql; do
  echo "Running ${seed_file}"
  psql -h "$POSTGRES_HOST" -p "$POSTGRES_PORT" -U "$POSTGRES_USER" -d "$POSTGRES_DB" -v ON_ERROR_STOP=1 -f "$seed_file"
done
