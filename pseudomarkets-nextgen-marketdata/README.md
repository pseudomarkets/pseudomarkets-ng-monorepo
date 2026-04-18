# Pseudo Markets NextGen Market Data Service

`pseudomarkets-nextgen-marketdata` is the next-generation market data service for the Pseudo Markets platform. It exposes HTTP endpoints for real-time quote retrieval, detailed quote data, and U.S. market indices, with Aerospike-backed caching and Twelve Data as the upstream provider.

## Tech Stack

- .NET 10 ASP.NET Core Web API
- C# class libraries for contracts, core orchestration, provider integration, and cache access
- Aerospike Community Edition for low-latency market data caching
- Twelve Data via the published `TwelveDataSharp` NuGet package
- Swagger UI / OpenAPI for local API exploration
- Docker and Docker Compose for local containerized development
- NUnit, Moq, and Shouldly for unit testing

## Architecture

The project is split into five main projects:

- `src/PseudoMarkets.MarketData.Service`
  Hosts the HTTP API, Swagger UI, configuration binding, and dependency injection.
- `src/PseudoMarkets.MarketData.Core`
  Contains service orchestration, interfaces, configuration models, and typed exceptions.
- `src/PseudoMarkets.MarketData.Providers`
  Integrates with Twelve Data and follows the legacy quote and indices flow where appropriate.
- `src/PseudoMarkets.MarketData.Cache`
  Provides the Aerospike-backed cache implementation.
- `src/PseudoMarkets.MarketData.Contracts`
  Defines request and response models shared across layers.

At runtime, the flow looks like this:

1. Requests enter the ASP.NET Core web app.
2. Controllers call the quote service in the core layer.
3. The quote service checks Aerospike cache first.
4. On a cache miss, the provider layer calls Twelve Data.
5. Successful results are written back to Aerospike and returned to the client.

Aerospike uses the namespace `nsPseudoMarkets` and the market data service uses hardcoded internal sets for quotes, detailed quotes, and indices.

## Project Layout

```text
pseudomarkets-nextgen-marketdata/
├── compose.yaml
├── src/
│   ├── PseudoMarkets.MarketData.Cache/
│   ├── PseudoMarkets.MarketData.Contracts/
│   ├── PseudoMarkets.MarketData.Core/
│   ├── PseudoMarkets.MarketData.Providers/
│   └── PseudoMarkets.MarketData.Service/
├── tests/
│   └── PseudoMarkets.MarketData.Tests/
├── HANDOFF.md
└── PseudoMarkets.MarketData.Service.sln
```

Shared Aerospike infrastructure lives at the repository root:

- `../infrastructure/aerospike/aerospike.conf`

## Running Without Docker

### Prerequisites

- .NET 10 SDK
- Docker Desktop on Windows or macOS, or Docker Engine with Compose on Linux
- A trusted ASP.NET Core development certificate for HTTPS
- A shell such as PowerShell, Command Prompt, Bash, or Zsh

### 1. Start Aerospike

The simplest local option is to use only the Aerospike service from Compose:

```bash
docker compose -f compose.yaml up -d aerospike
```

This exposes Aerospike on `localhost:3000`, which matches local market data app settings.

### 2. Trust the ASP.NET Core HTTPS development certificate

Windows, macOS, and Linux can all use the same .NET command:

```bash
dotnet dev-certs https --trust
```

Depending on your OS, you may be prompted to approve certificate trust through the local certificate store or keychain UI.

### 3. Create the shared local secrets file

From the repository root:

```bash
cp .env.example .env
```

Then set at least:

- `TwelveData__ApiKey`

The market data service now loads the shared root `.env` file automatically for local non-Docker runs.

### 4. Run the web project

From the `pseudomarkets-nextgen-marketdata` folder:

```bash
dotnet run --project src/PseudoMarkets.MarketData.Service/PseudoMarkets.MarketData.Service.csproj
```

By default, the launch settings use:

- `https://localhost:7228`
- `http://localhost:5286`

Swagger UI is available at:

- [https://localhost:7228/swagger](https://localhost:7228/swagger)

## Running With Docker Compose

### What Compose Starts

The Compose stack brings up:

- `aerospike`
  Aerospike CE with disk-backed persistence
- `pseudomarkets.marketdata.service`
  The ASP.NET Core market data service configured to connect to the Aerospike container

### Start the full stack

```bash
docker compose -f compose.yaml up --build
```

### Run detached

```bash
docker compose -f compose.yaml up -d --build
```

### Stop the stack

```bash
docker compose -f compose.yaml down
```

### Service endpoints

- Market data service: [http://localhost:8081](http://localhost:8081)
- Swagger UI: [http://localhost:8081/swagger/index.html](http://localhost:8081/swagger/index.html)
- Aerospike: `localhost:3000`

### Notes about the Docker setup

- The Compose file waits for Aerospike to become healthy before starting the market data service.
- The web container uses `Aerospike__Host=aerospike`, so it talks to the database over the Compose network instead of `localhost`.
- Aerospike data is persisted in the shared repo-root directory `../.docker-data/aerospike`.
- The Compose stack runs the market data service in `Development` mode so Swagger UI is available locally.
- The service-local Compose file pins Aerospike to `linux/arm64`, which keeps it aligned with Apple Silicon / M-series development machines.
- The Twelve Data API key is read from the shared repo-root `.env` file through `../.env`.

## Configuration

### Local development configuration

`src/PseudoMarkets.MarketData.Service/appsettings.Development.json` contains the default local development values for:

- Aerospike host/port
- Twelve Data base URL
- cache TTL values

Secrets are centralized in the repository-root `.env` file instead of committed appsettings files.

### Container configuration

When running in Docker Compose, the web container overrides configuration with environment variables:

- `Aerospike__Host`
- `Aerospike__Port`
- `TwelveData__ApiKey`

The Compose files load `TwelveData__ApiKey` from the shared repo-root `.env` file.

## API Overview

Current primary endpoints include:

- `GET /api/marketdata/quote/{symbol}`
  Returns the latest quote for a symbol.
- `GET /api/marketdata/quote/{symbol}/detailed?interval=1min`
  Returns a detailed quote snapshot using the requested interval.
- `GET /api/marketdata/indices`
  Returns cached or provider-backed U.S. market index snapshots.

Use Swagger UI to inspect request and response schemas interactively.

## Build

From the monorepo root:

```bash
dotnet build pseudomarkets-nextgen-marketdata/PseudoMarkets.MarketData.Service.sln
```

## Test

From the monorepo root:

```bash
dotnet test pseudomarkets-nextgen-marketdata/PseudoMarkets.MarketData.Service.sln -m:1
```

## Troubleshooting

### Swagger is not available

- Non-Docker local runs expose Swagger at `https://localhost:7228/swagger`.
- Docker Compose exposes Swagger at `http://localhost:8081/swagger/index.html`.
- Swagger is enabled only in Development mode.

### The app cannot connect to Aerospike

- Verify Aerospike is running on `localhost:3000` for non-Docker runs.
- In Docker Compose, verify both containers are up:

```bash
docker compose -f compose.yaml ps
```

### Quote requests fail against Twelve Data

- Verify `TwelveData__ApiKey` is set in the repo-root `.env` file.
- If you are running without Docker, make sure the app was started from within the repository so it can locate the shared `.env` file.

### HTTPS certificate warnings locally

Trust the development certificate:

```bash
dotnet dev-certs https --trust
```

### Reset local Aerospike data

Stop the Compose stack and remove the local data directory:

macOS/Linux:

```bash
docker compose -f compose.yaml down
rm -rf ../.docker-data/aerospike
```

Windows PowerShell:

```powershell
docker compose -f compose.yaml down
Remove-Item -Recurse -Force ..\.docker-data\aerospike
```
