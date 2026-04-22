# Pseudo Markets Trading Instrument Database

The Trading Instrument Database service is the platform reference-data API for tradable securities. It stores instruments in the shared PostgreSQL database and exposes secured endpoints for lookup, instrument creation, and closing-price updates.

## Tech Stack

- .NET 10 ASP.NET Core Web API
- PostgreSQL with EF Core and Npgsql
- Shared `PseudoMarketsDbContext` from `pseudomarkets-nextgen-shared-entities`
- Shared IDP-backed authorization from `pseudomarkets-nextgen-shared-auth`
- Swagger / OpenAPI for browser testing
- NUnit, Moq, and Shouldly for unit tests

## API

- `GET /api/trading-instruments/{symbol}`
  Requires `VIEW_MARKET_DATA`.
- `POST /api/trading-instruments`
  Requires `UPDATE_INSTRUMENTS`. New instruments default `trading_status` to `true`.
- `PATCH /api/trading-instruments/{symbol}/closing-price`
  Requires `UPDATE_INSTRUMENTS`.

Swagger is available at [http://localhost:8083/swagger/index.html](http://localhost:8083/swagger/index.html) when running locally or through Docker.

## Database

The service uses the shared PostgreSQL database `pseudomarkets_db`. The `trading_instruments` table is owned by the shared EF Core model and has these columns:

- `symbol`
- `description`
- `trading_status`
- `primary_instrument_type`
- `secondary_instrument_type`
- `closing_price`
- `closing_price_date`
- `source`

Committed seed SQL scripts live in:

```text
../infrastructure/postgres/trading-instruments
```

The scripts are idempotent and run in this order:

1. `001_seed_nasdaq_listed.sql`
2. `002_seed_nyse_arca_etfs.sql`
3. `003_seed_nyse_listed.sql`

## Run With Docker

From the repository root:

```bash
docker compose -f compose.yaml up -d --build pseudomarkets.tradinginstruments.service trading-instruments-seed
```

Or run the full platform:

```bash
docker compose -f compose.yaml up -d --build
```

The service is exposed on `localhost:8083`. The seed container waits until the `trading_instruments` table exists, then applies the committed SQL scripts.

## Run Without Docker

Start PostgreSQL on `localhost:5432` with database `pseudomarkets_db`, then run:

```bash
dotnet run --project src/PseudoMarkets.ReferenceData.TradingInstruments.Service/PseudoMarkets.ReferenceData.TradingInstruments.Service.csproj
```

After the service applies migrations, seed the table:

```bash
psql "host=localhost port=5432 dbname=pseudomarkets_db user=postgres password=postgres" -f ../infrastructure/postgres/trading-instruments/001_seed_nasdaq_listed.sql
psql "host=localhost port=5432 dbname=pseudomarkets_db user=postgres password=postgres" -f ../infrastructure/postgres/trading-instruments/002_seed_nyse_arca_etfs.sql
psql "host=localhost port=5432 dbname=pseudomarkets_db user=postgres password=postgres" -f ../infrastructure/postgres/trading-instruments/003_seed_nyse_listed.sql
```

## Build And Test

```bash
dotnet build PseudoMarkets.ReferenceData.TradingInstruments.sln
dotnet test PseudoMarkets.ReferenceData.TradingInstruments.sln -m:1
```
