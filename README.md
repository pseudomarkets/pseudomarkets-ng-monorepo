# Pseudo Markets NextGen Platform

Pseudo Markets is an open source stock trading simulation platform focused on realistic paper trading powered by real market data. The broader goal of the platform is to give users a practical way to learn, research, and simulate equities trading in an environment that feels closer to a real brokerage and exchange workflow, while remaining free to use and open source.

Pseudo Markets is designed around:

- open source paper trading powered by real market data
- a realistic simulated stock trading experience
- near real-time data aggregation and simulated order execution
- historical market data access for listed equities and ETFs
- a cloud-based experience for research and trading workflows
- a privacy-conscious approach with no ads, no tracking, and no data collection

More information about the project is available at [pseudomarkets.live](https://pseudomarkets.live/).

## Vision

Pseudo Markets NextGen is intended to grow into a multi-service platform made up of focused microservices. The identity provider is the first concrete service in this monorepo, and additional services such as market data, trading, portfolios, analytics, and supporting infrastructure can be added alongside it over time.

The long-term monorepo goals are:

- open the entire platform in a single Visual Studio or Rider solution
- develop each service in its own isolated folder with its own source, tests, and local Docker assets
- run multiple services together from a root Docker Compose entrypoint
- keep individual services independently evolvable while preserving a cohesive developer workflow

## Repository Layout

- `PseudoMarkets.NextGen.Platform.sln`
  Root solution for opening the platform in Visual Studio or Rider.
- `compose.yaml`
  Root Docker Compose entrypoint for running platform services together.
- `infrastructure/aerospike/`
  Shared Aerospike configuration for platform-local Docker stacks.
- `pseudomarkets-nextgen-idp/`
  Identity provider service for account creation, authentication, and centralized authorization decisions.
- `pseudomarkets-nextgen-marketdata/`
  Market data service for quotes, detailed quote snapshots, and market indices.
- `pseudomarkets-nextgen-shared-auth/`
  Shared authorization library used by downstream services to call the identity provider authorization endpoint.
- `pseudomarkets-nextgen-shared-entities/`
  Shared Entity Framework Core model, migrations, and `PseudoMarketsDbContext` for the platform PostgreSQL database.
- `pseudomarkets-nextgen-transaction-processing/`
  Transaction posting service for trades, cash movement, and compensating void transactions backed by the shared PostgreSQL database.
- `infrastructure/postgres/`
  Shared location for platform-level PostgreSQL infrastructure files and scripts.

## Current State

Today, the monorepo is structured to support platform growth:

- the root platform solution opens the currently implemented service projects together
- the identity service already has its own service-local solution, tests, and Docker workflow
- the market data service already has its own service-local solution, tests, and Docker workflow
- the shared authorization library has its own source project and standalone unit test project
- the shared entities library owns the common EF Core entity model, migrations, `PseudoMarketsDbContext`, and platform reference data such as market holidays
- the transaction processing service has its own service-local solution, tests, Docker workflow, and PostgreSQL-backed posting implementation
- the root Docker Compose file brings the identity service, market data service, transaction processing service, shared Aerospike container, and PostgreSQL up together
- market data authorization is delegated to the identity service, so a JWT issued by the IDP can be reused across services
- transaction write authorization is delegated to the identity service as well through the shared authorization library and the `UPDATE_TRANSACTIONS` action
- additional service folders can be added without restructuring the monorepo again

## IDE Workflow

Open the root solution to work across services at once:

```bash
dotnet build PseudoMarkets.NextGen.Platform.sln
```

The root solution currently groups projects by service folder and already includes:

- the Pseudo Markets identity server source and tests
- the Pseudo Markets market data source and tests
- the Pseudo Markets transaction processing source and tests
- the shared authorization library and tests
- the shared entities library

Open `PseudoMarkets.NextGen.Platform.sln` in Visual Studio, Rider, or another compatible .NET IDE from the repository root.

## Docker Workflow

Before starting the platform stack, create the shared root secrets file:

```bash
cp .env.example .env
```

Windows PowerShell:

```powershell
Copy-Item .env.example .env
```

Then update `.env` with the values you want to use locally, including:

- `JwtConfiguration__Key`
- `TwelveData__ApiKey`
- `Postgres__Password`

Run the platform stack from the monorepo root:

```bash
docker compose -f compose.yaml up --build
```

Current local browser endpoints:

- Identity server Swagger UI: [http://localhost:8080/swagger/index.html](http://localhost:8080/swagger/index.html)
- Market data Swagger UI: [http://localhost:8081/swagger/index.html](http://localhost:8081/swagger/index.html)
- Transaction processing Swagger UI: [http://localhost:8082/swagger/index.html](http://localhost:8082/swagger/index.html)

The root Compose stack uses the shared Aerospike config at `infrastructure/aerospike/aerospike.conf` and stores Aerospike data in `./.docker-data/aerospike`.
Application secrets are loaded from the shared root `.env` file, and PostgreSQL data for `pseudomarkets_db` is stored in `./.docker-data/postgres`.
Market data endpoints are protected by the identity service, so the normal local flow is:

1. Create or authenticate an account in the IDP Swagger UI.
2. Copy the returned JWT.
3. Open the Market data Swagger UI and use `Authorize` to paste the token.
4. Open the Transaction processing Swagger UI and use `Authorize` to paste the same token for write operations.

## Service Model

Each microservice should live in its own top-level folder, for example:

- `pseudomarkets-nextgen-idp`
- `pseudomarkets-nextgen-marketdata`
- `pseudomarkets-nextgen-transaction-processing`

Each service folder can own:

- its own source projects
- its own test projects
- its own service-level solution file when useful
- its own Dockerfile and service-local Compose configuration
- service-specific configuration assets, while shared infrastructure can live at the repository root

This keeps the platform modular while still allowing the full system to be opened and eventually orchestrated together from the monorepo root.

## Testing

Build the full platform:

```bash
dotnet build PseudoMarkets.NextGen.Platform.sln
```

Run the current platform test suites:

```bash
dotnet test PseudoMarkets.NextGen.Platform.sln -m:1
```
