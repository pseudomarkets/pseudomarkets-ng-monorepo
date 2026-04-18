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
  Identity provider service for authentication, authorization, and account provisioning.
- `pseudomarkets-nextgen-marketdata/`
  Market data service workspace.

## Current State

Today, the monorepo is structured to support platform growth:

- the root platform solution opens the currently implemented service projects together
- the identity service already has its own service-local solution, tests, and Docker workflow
- the root Docker Compose file brings multiple services up together against one shared Aerospike container
- additional service folders can be added without restructuring the monorepo again

## IDE Workflow

Open the root solution to work across services at once:

```bash
dotnet build PseudoMarkets.NextGen.Platform.sln
```

The root solution currently groups projects by service folder and already includes the Pseudo Markets identity server projects and tests.

Open `PseudoMarkets.NextGen.Platform.sln` in Visual Studio, Rider, or another compatible .NET IDE from the repository root.

## Docker Workflow

Before starting the platform stack, create the shared root secrets file:

```bash
cp .env.example .env
```

Then update `.env` with the values you want to use locally, including:

- `JwtConfiguration__Key`
- `TwelveData__ApiKey`

Run the platform stack from the monorepo root:

```bash
docker compose -f compose.yaml up --build
```

Current local browser endpoints:

- Identity server Swagger UI: [http://localhost:8080/swagger/index.html](http://localhost:8080/swagger/index.html)
- Market data Swagger UI: [http://localhost:8081/swagger/index.html](http://localhost:8081/swagger/index.html)

The root Compose stack uses the shared Aerospike config at `infrastructure/aerospike/aerospike.conf` and stores Aerospike data in `./.docker-data/aerospike`.
Application secrets are loaded from the shared root `.env` file.

## Service Model

Each microservice should live in its own top-level folder, for example:

- `pseudomarkets-nextgen-idp`
- `pseudomarkets-nextgen-marketdata`

Each service folder can own:

- its own source projects
- its own test projects
- its own service-level solution file when useful
- its own Dockerfile and service-local Compose configuration
- service-specific configuration assets, while shared infrastructure can live at the repository root

This keeps the platform modular while still allowing the full system to be opened and eventually orchestrated together from the monorepo root.
