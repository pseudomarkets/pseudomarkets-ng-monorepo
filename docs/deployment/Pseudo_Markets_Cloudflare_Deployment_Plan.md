# Pseudo Markets Cloudflare Deployment Plan

## Overview
This document captures the implementation plan for replatforming the Pseudo Markets platform onto Cloudflare-native services. It replaces the prior DigitalOcean Kubernetes target with a Cloudflare-first architecture built around Workers, Containers Beta, D1, Workers KV, Durable Objects, Cron Triggers, and Workflows.

This is not a lift-and-shift of the current Docker and Kubernetes deployment shape. The existing platform relies on long-running .NET services, shared PostgreSQL, shared Aerospike, and Hangfire. A Cloudflare target requires changing the runtime, networking, persistence, and batch architecture accordingly.

## Target Scope
The initial Cloudflare deployment target includes:

- Pseudo Markets Identity Server
- Pseudo Markets Market Data Service
- Pseudo Markets Transaction Processing Service
- Pseudo Markets Trading Instruments Service
- Internal Order Execution components where still required by platform flows
- Cloudflare-native replacements for PostgreSQL-backed and Aerospike-backed workloads

Public service URLs should remain:

- `https://idp.pseudomarkets.live`
- `https://marketdata.pseudomarkets.live`
- `https://transactions.pseudomarkets.live`
- `https://instruments.pseudomarkets.live`

For the first Cloudflare phase, Order Execution and batch orchestration should remain internal-only unless there is a later requirement to expose them publicly.

## Target Architecture
- One Cloudflare account hosting all runtime, storage, edge, and DNS resources
- One public Worker facade per externally exposed service hostname
- Optional Cloudflare Containers Beta workloads for .NET services that are not rewritten immediately
- One D1 database per bounded context instead of one shared relational database
- Workers KV for cache-like and read-heavy key-value workloads
- Durable Objects for strongly consistent coordination and serialized mutation flows
- Cron Triggers plus Workflows for scheduled and long-running jobs
- One DNS record per public hostname in Cloudflare DNS
- SSL terminated by Cloudflare custom domains on the public Worker facades
- Internal service-to-service calls handled through Worker service bindings instead of public URLs

## Phase 1: Replatforming Readiness
1. Treat this effort as a replatforming initiative, not an infrastructure-only migration.
2. Inventory each current service boundary and classify it as:
   - public API facade
   - internal business logic service
   - batch orchestration workload
   - persistence owner
3. Review all current PostgreSQL tables and Aerospike sets and assign each one a Cloudflare-native destination.
4. Identify which service behaviors can move into Workers first and which still require .NET runtime preservation.
5. Keep current public API contracts stable unless there is an explicit product decision to change them.

## Phase 2: Runtime Layout In Repo
Create a new root folder:

- `infrastructure/cloudflare`

Recommended structure:

- `infrastructure/cloudflare/workers`
- `infrastructure/cloudflare/containers`
- `infrastructure/cloudflare/d1`
- `infrastructure/cloudflare/kv`
- `infrastructure/cloudflare/durable-objects`
- `infrastructure/cloudflare/workflows`
- `infrastructure/cloudflare/terraform`

Recommended Worker groupings:

- `workers/idp-facade`
- `workers/marketdata-facade`
- `workers/transactions-facade`
- `workers/instruments-facade`
- `workers/platform-batch`
- `workers/shared`

Recommended container groupings:

- one containerized workload per .NET service that remains in ASP.NET Core during the transition

## Phase 3: Public Runtime Model
Each public API should move behind a Worker facade.

Responsibilities of each public Worker facade:

1. Bind to the public hostname
2. Terminate TLS
3. Apply Cloudflare edge controls such as WAF and rate limiting
4. Handle request routing and edge-level request shaping
5. Use service bindings for internal platform calls
6. Proxy to a Cloudflare Container only when .NET runtime behavior still needs to be retained

This means the .NET container should no longer be treated as the direct public HTTP entrypoint. The Worker facade becomes the control plane for each service.

## Phase 4: Container Strategy
Use Cloudflare Containers Beta only where keeping the .NET runtime is still necessary.

Recommended approach:

1. Keep business logic that cannot be rewritten immediately inside containerized .NET services.
2. Remove assumptions that the .NET service will directly own public ingress.
3. Avoid making containers the primary integration point to Cloudflare-native storage.
4. Keep container responsibilities focused on reusable business logic and transitional application behavior.

Cloudflare Containers Beta should be treated as a migration bridge, not the final architecture for the whole platform.

## Phase 5: Relational Data Replatforming To D1
Do not preserve the current single shared PostgreSQL shape. D1 is SQLite-based, single-threaded per database instance, and capped per database, so the platform should split relational data by bounded context.

Recommended D1 databases:

- `pm_identity`
- `pm_reference_data`
- `pm_transactions`
- `pm_order_execution`

Recommended ownership:

### `pm_identity`
- account state that must be relational
- password reset metadata if retained relationally
- refresh token metadata if moved away from key-value storage

### `pm_reference_data`
- `market_holidays`
- `trading_instruments`

### `pm_transactions`
- `posting_batches`
- `ledger_transactions`
- `trade_executions`
- `cash_movements`
- `account_balances`
- `positions`
- `position_lots`
- `position_lot_closures`

### `pm_order_execution`
- `order_executions`
- `queued_orders`

Migration requirements:

1. Replace Entity Framework Core migration assumptions that target one shared PostgreSQL schema.
2. Create Cloudflare-compatible schema creation and migration workflows per D1 database.
3. Seed `market_holidays` and trading instruments through D1-compatible deployment steps.
4. Update data access layers so each service targets the correct bounded-context database.

## Phase 6: Aerospike Replacement Strategy
Do not replace Aerospike with a single Cloudflare product. Replace by behavior.

### Workers KV
Use Workers KV for:

- market data quote cache
- detailed quote cache
- indices cache
- other read-heavy, cache-friendly, eventually consistent data

### Durable Objects
Use Durable Objects for:

- user ID reservation and uniqueness coordination
- refresh token single-use consumption and revocation coordination
- other identity mutations that require stronger consistency or serialized access
- workflow and coordination patterns where one logical entity should have one mutation owner

Rules:

1. Do not move atomic coordination flows into KV.
2. Do not use Durable Objects as a drop-in replacement for all cache workloads.
3. Keep the mapping explicit in documentation for each former Aerospike set.

## Phase 7: Service And Data Access Pattern
Because D1, KV, and Durable Objects are native Worker bindings, the Cloudflare-native access pattern should be:

1. Public request reaches Worker facade
2. Worker facade performs auth, routing, and binding-based data access where appropriate
3. Worker facade calls internal Worker logic or proxies to a .NET container for business logic that still depends on the .NET runtime
4. Internal platform calls use Worker service bindings rather than public hostnames

This means some responsibilities that currently live inside ASP.NET services may need to move into the Worker layer.

## Phase 8: Batch Replatforming
Replace Hangfire with:

- Cron Triggers for schedules
- Workflows for long-running or multi-step jobs

Recommended batch model:

1. Create one internal batch Worker or small set of internal Workers
2. Trigger workflows from Cron schedules
3. Use workflows for:
   - queued order execution
   - trade settlement
   - future scheduled jobs
4. Keep workflow calls internal and avoid routing them through public hostnames

Current timing assumptions should remain:

- settlement should be the first morning batch job
- settlement should run at `12:00 AM America/New_York`

## Phase 9: Identity And Auth Direction
JWT-based authentication can remain conceptually intact, but storage and coordination need to be reworked for Cloudflare-native persistence.

Recommended direction:

1. Preserve public token flows and service expectations where possible.
2. Move refresh token coordination into Durable Objects or a clearly defined hybrid relational pattern.
3. Keep service-to-service auth flows internal to the Cloudflare runtime wherever possible.
4. Re-evaluate any assumptions that currently depend on direct shared database access from multiple services.

## Phase 10: DNS, Edge Security, And Routing
Cloudflare becomes both the DNS provider and the runtime edge provider.

Recommended steps:

1. Create one Cloudflare-managed DNS record per public hostname.
2. Bind each hostname to the corresponding Worker facade.
3. Use Cloudflare SSL on the public custom domains.
4. Apply WAF and rate-limiting policies at the Worker-hostname boundary.
5. Keep internal service-to-service calls off the public network by using bindings instead of internet-facing URLs.

## Phase 11: CI/CD
Replace the Kubernetes-oriented CI/CD plan with Cloudflare-oriented workflows.

### CI workflow
Recommended responsibilities:

1. Checkout the repository
2. Setup .NET
3. Restore and build the root solution
4. Run `dotnet test` across the platform
5. Build any container images still needed for Cloudflare Containers Beta workloads
6. Validate Worker configuration and infrastructure-as-code configuration

Planned workflow file:

- `.github/workflows/ci.yml`

### CD workflow
Recommended responsibilities:

1. Build public Worker bundles
2. Build and publish any containerized .NET workloads
3. Apply Cloudflare infrastructure resources through Terraform or Pulumi
4. Deploy Workers and Containers with Wrangler
5. Run D1 migration and seed steps in the correct order
6. Validate public hostnames and internal service health

Planned workflow file:

- `.github/workflows/deploy-cloudflare.yml`

Use a GitHub `production` environment so deployment secrets and approval rules remain centralized.

## Phase 12: Secrets And Configuration
Recommended GitHub environment secrets:

- Cloudflare API credentials
- JWT signing key
- any remaining provider API keys
- any service secrets still required by containerized workloads

Recommended non-sensitive configuration:

- public hostnames
- environment names
- Worker names
- D1 database names
- KV namespace names
- Durable Object namespace names

Configuration should be split by:

- Worker environment variables and secrets
- container environment configuration
- D1 migration configuration

## Phase 13: Documentation
Update the root README with:

1. Cloudflare architecture overview
2. Cloudflare account prerequisites
3. DNS and custom domain setup
4. GitHub Actions secret setup
5. Deployment flow
6. Rollback flow
7. Verification steps for each public service URL

Add or update deployment runbooks under `docs` covering:

- first-time Cloudflare environment bootstrap
- D1 migration and seeding
- Worker rollout and rollback
- Cloudflare Containers Beta rollout
- Durable Object and KV troubleshooting

## Phase 14: Validation
After implementation, validate:

1. Public hostnames resolve correctly through Cloudflare DNS
2. SSL is valid for each public hostname
3. Public Worker facades route correctly
4. Internal service bindings work correctly
5. D1 schema creation and seeding work in a fresh environment
6. KV-backed market data caching behaves correctly
7. Durable Object coordination behaves correctly for identity flows
8. Cron plus Workflow execution works for scheduled jobs
9. Scalar is reachable on each public service hostname
10. CI/CD can bootstrap and deploy the Cloudflare environment end-to-end

## Recommended Order Of Work
1. Define final service runtime ownership between Workers and Containers Beta
2. Create `infrastructure/cloudflare` folder structure
3. Create public Worker facades for the four public APIs
4. Split the shared relational model into D1 bounded contexts
5. Replace Aerospike-backed behaviors with KV and Durable Objects
6. Rework internal service-to-service calls to use bindings
7. Replace Hangfire planning with Cron plus Workflows
8. Add Terraform or Pulumi for Cloudflare infrastructure
9. Add GitHub Actions CI
10. Add GitHub Actions CD
11. Update documentation
12. Validate a full Cloudflare deployment

## Open Decisions
Before implementation, confirm or re-confirm these architectural decisions:

- whether each public API should remain backed by a .NET container or be rewritten fully into Workers
- whether refresh tokens should remain key-value and coordination based, or move partially into D1
- whether Order Execution should remain internal-only for the first Cloudflare phase
- whether Cloudflare Containers Beta is acceptable as a transitional runtime for production workloads

## Notes
- This document supersedes the DigitalOcean Kubernetes target for a Cloudflare-native future, but the older DigitalOcean plan should remain in the repo as historical reference unless explicitly removed.
- D1 is a bounded-context fit, not a one-for-one substitute for the current shared PostgreSQL database.
- Workers KV is appropriate for market data caching because it is read-heavy and eventually consistent.
- Durable Objects should be used where the old Aerospike usage depended on stronger mutation coordination semantics.
