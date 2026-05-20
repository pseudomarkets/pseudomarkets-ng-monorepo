# Pseudo Markets NextGen Batch Processing

`pseudomarkets-nextgen-batch-processing` is the shared batch-processing host for the Pseudo Markets platform. It provides a configurable Hangfire-based framework for scheduling and running recurring platform jobs, persists Hangfire state in the shared PostgreSQL server, and exposes the Hangfire dashboard UI for operational visibility.

## Tech Stack

- .NET 10 ASP.NET Core
- Hangfire
- Hangfire.PostgreSql
- PostgreSQL with Npgsql
- Shared `PseudoMarketsDbContext` health integration from `pseudomarkets-nextgen-shared-entities`
- Shared `/info` and `/health` support from `pseudomarkets-nextgen-shared-servicehelpers`
- NUnit, Moq, and Shouldly

## Architecture

The component is split into three projects:

- `src/PseudoMarkets.Platform.Batch.Core`
  Contains the reusable batch framework abstractions, configuration models, DI extensions, job registration logic, and distributed-lock support.
- `src/PseudoMarkets.Platform.Batch.Host`
  Hosts the Hangfire server, Hangfire dashboard, PostgreSQL storage wiring, and standardized operational endpoints.
- `tests/PseudoMarkets.Platform.Batch.Tests`
  Covers framework behavior such as job registration, queued-order execution orchestration, and downstream client behavior.

The first concrete batch job is the queued-order execution job, which runs at market open, reads pending queued orders from PostgreSQL, authenticates with the IDP using a configured `SYSTEM` account, and resubmits those orders through the Order Execution API using the original queued-order `userId`.

## Runtime Behavior

The host reads batch configuration from the `BatchProcessing` section and registers recurring Hangfire jobs from DI at startup. Each future batch job can be controlled independently through configuration for:

- enablement
- cron schedule
- queue name
- time zone
- concurrency protection

By default, the host uses the shared PostgreSQL database `pseudomarkets_db` and stores Hangfire data in the `hangfire` schema.

The queued-order execution job is registered by default as `queued-order-execution` with a `9:30 AM America/New_York` schedule. It processes pending queued orders in submission order, uses a configurable max batch size, and marks each queue row as `Succeeded` or `Failed` after attempting execution.

## Operational Endpoints

- `GET /info`
  Returns the application name, version, and build timestamp.
- `GET /health`
  Returns the standardized JSON health payload with a lightweight PostgreSQL connectivity check.
- `GET /hangfire`
  Returns the Hangfire dashboard UI.

For this implementation pass, the Hangfire dashboard is intentionally left unauthenticated for local development and validation. Authentication can be added in a later pass.

## Configuration

The host uses the shared root `.env` file or deployment secrets for:

- `ConnectionStrings__PseudoMarketsDb`
- `QueuedOrderExecution__SystemAccountLoginId`
- `QueuedOrderExecution__SystemAccountPassword`

Primary appsettings-driven batch configuration lives under:

- `BatchProcessing:Enabled`
- `BatchProcessing:Dashboard:Enabled`
- `BatchProcessing:Dashboard:Path`
- `BatchProcessing:Dashboard:ReadOnly`
- `BatchProcessing:Server:WorkerCount`
- `BatchProcessing:Server:Queues`
- `BatchProcessing:Server:ServerName`
- `BatchProcessing:Storage:SchemaName`
- `BatchProcessing:Storage:QueuePollIntervalSeconds`
- `BatchProcessing:Storage:InvisibilityTimeoutMinutes`
- `BatchProcessing:Jobs:{JobName}:Enabled`
- `BatchProcessing:Jobs:{JobName}:CronExpression`
- `BatchProcessing:Jobs:{JobName}:Queue`
- `BatchProcessing:Jobs:{JobName}:DisableConcurrentExecution`
- `BatchProcessing:Jobs:{JobName}:TimeZoneId`

Queued-order execution settings live under:

- `QueuedOrderExecution:IdentityServerBaseUrl`
- `QueuedOrderExecution:OrderExecutionBaseUrl`
- `QueuedOrderExecution:SystemAccountLoginId`
- `QueuedOrderExecution:SystemAccountPassword`
- `QueuedOrderExecution:TimeoutSeconds`
- `QueuedOrderExecution:TokenRefreshBufferSeconds`
- `QueuedOrderExecution:MaxBatchSize`

## Run With Docker

From the repository root:

```bash
docker compose -f compose.yaml up -d --build pseudomarkets.platform.batch.host
```

Or run the full platform:

```bash
docker compose -f compose.yaml up -d --build
```

By default, the batch host is available at:

- Hangfire dashboard: [http://localhost:8085/hangfire](http://localhost:8085/hangfire)
- Info endpoint: [http://localhost:8085/info](http://localhost:8085/info)
- Health endpoint: [http://localhost:8085/health](http://localhost:8085/health)

## Run Without Docker

Start PostgreSQL first, then run from the repository root:

```bash
dotnet run --project pseudomarkets-nextgen-batch-processing/src/PseudoMarkets.Platform.Batch.Host/PseudoMarkets.Platform.Batch.Host.csproj
```

The host loads the shared root `.env` file for local development secrets.

By default, the launch settings use:

- `https://localhost:7285`
- `http://localhost:8085`

## Build And Test

From the repository root:

```bash
dotnet build pseudomarkets-nextgen-batch-processing/PseudoMarkets.Platform.Batch.sln
dotnet test pseudomarkets-nextgen-batch-processing/PseudoMarkets.Platform.Batch.sln -m:1
```
