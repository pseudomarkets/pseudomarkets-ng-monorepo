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
- `pseudomarkets-nextgen-idp/`
  Identity provider service for authentication, authorization, and account provisioning.
- `pseudomarkets-nextgen-marketdata/`
  Market data service workspace.

## Current State

Today, the monorepo is structured to support platform growth:

- the root platform solution opens the currently implemented service projects together
- the identity service already has its own service-local solution, tests, and Docker workflow
- the root Docker Compose file acts as the future aggregation point for bringing multiple services up together
- additional service folders can be added without restructuring the monorepo again

## IDE Workflow

Open the root solution to work across services at once:

```bash
dotnet build PseudoMarkets.NextGen.Platform.sln
```

The root solution currently groups projects by service folder and already includes the Pseudo Markets identity server projects and tests.

Open `PseudoMarkets.NextGen.Platform.sln` in Visual Studio, Rider, or another compatible .NET IDE from the repository root.

## Docker Workflow

Run the platform stack from the monorepo root:

```bash
docker compose -f compose.yaml up --build
```

As new microservices are added, their service-local Compose files can be included from the root `compose.yaml`.

## Service Model

Each microservice should live in its own top-level folder, for example:

- `pseudomarkets-nextgen-idp`
- `pseudomarkets-nextgen-marketdata`

Each service folder can own:

- its own source projects
- its own test projects
- its own service-level solution file when useful
- its own Dockerfile and service-local Compose configuration
- its own infrastructure or configuration assets

This keeps the platform modular while still allowing the full system to be opened and eventually orchestrated together from the monorepo root.
