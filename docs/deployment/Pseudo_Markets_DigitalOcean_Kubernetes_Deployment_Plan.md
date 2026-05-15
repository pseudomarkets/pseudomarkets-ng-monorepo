# Pseudo Markets DigitalOcean Kubernetes Deployment Plan

## Overview
This document captures the implementation plan for deploying the full Pseudo Markets platform to a single-node DigitalOcean Kubernetes (DOKS) cluster using GitHub Actions for CI/CD and Cloudflare for DNS and public SSL.

This plan is intentionally staged so the platform can continue to grow before deployment work begins.

## Target Scope
The initial Kubernetes deployment target includes:

- Pseudo Markets Identity Server
- Pseudo Markets Market Data Service
- Pseudo Markets Transaction Processing Service
- Pseudo Markets Trading Instruments Service
- Shared PostgreSQL
- Shared Aerospike

Public service URLs should be:

- `https://idp.pseudomarkets.live`
- `https://marketdata.pseudomarkets.live`
- `https://transactions.pseudomarkets.live`
- `https://instruments.pseudomarkets.live`

## Target Architecture
- One DigitalOcean Kubernetes cluster with a single worker node pool
- One DigitalOcean Container Registry for all service images
- One Kubernetes namespace for the platform, for example `pseudomarkets-platform`
- One NGINX Ingress Controller exposed through one DigitalOcean load balancer
- One Cloudflare DNS entry per service hostname, all pointing to the same load balancer IP
- PostgreSQL and Aerospike deployed as single-replica stateful workloads with persistent storage
- All .NET services deployed as Kubernetes `Deployment` workloads behind internal `ClusterIP` services

## Phase 1: Pre-Deployment Hardening
1. Add `/health` support to the Identity Server so all public services have consistent readiness and liveness endpoints.
2. Review each service's runtime configuration and ensure Kubernetes-friendly environment-based configuration is complete.
3. Split configuration into Kubernetes `ConfigMap` and `Secret` resources.
4. Stop depending on application startup for shared Entity Framework Core schema setup.
5. Replace shared relational schema setup with a dedicated migration job.
6. Keep trading instrument seed execution as a separate one-shot job.

## Phase 2: Kubernetes Layout In Repo
Create a new root folder:

- `infrastructure/kubernetes`

Recommended structure:

- `infrastructure/kubernetes/base`
- `infrastructure/kubernetes/overlays/digitalocean-single-node`

Recommended manifest groupings under `base`:

- `namespace`
- `configmaps`
- `secrets`
- `postgres`
- `aerospike`
- `jobs/migrations`
- `jobs/seed-trading-instruments`
- `services/idp`
- `services/marketdata`
- `services/transactions`
- `services/instruments`
- `ingress`

Use Kustomize so a shared base can support future deployment targets in addition to the first DigitalOcean environment.

## Phase 3: Data Layer Workloads
### PostgreSQL
- Deploy as a single-replica `StatefulSet`
- Back with a `PersistentVolumeClaim` using DigitalOcean block storage
- Expose only with an internal `ClusterIP` service
- Add readiness and startup probes

### Aerospike
- Deploy as a single-replica stateful workload
- Back with its own `PersistentVolumeClaim`
- Expose only with an internal `ClusterIP` service
- Add readiness and startup probes

## Phase 4: Application Workloads
For each .NET service:

1. Create a Kubernetes `Deployment`
2. Create an internal `ClusterIP` `Service`
3. Configure:
   - `readinessProbe`
   - `livenessProbe`
   - resource requests and limits
   - environment variable mapping from `ConfigMap` and `Secret`
4. Keep each service independently deployable while still managed by the root platform overlay

## Phase 5: Migrations and Seeding
1. Create a Kubernetes `Job` to run shared Entity Framework Core migrations once against `pseudomarkets_db`.
2. Create a second `Job` to run the trading instrument seed SQL.
3. Enforce this deployment order:
   - PostgreSQL ready
   - Aerospike ready
   - migration job
   - seed job
   - application rollout
4. Use this pattern to avoid race conditions caused by multiple services attempting schema initialization at startup.

## Phase 6: Ingress and Cloudflare Routing
1. Install NGINX Ingress Controller in the DOKS cluster.
2. Expose the ingress controller through one DigitalOcean load balancer.
3. Create ingress host rules:
   - `idp.pseudomarkets.live` routes to the Identity Server
   - `marketdata.pseudomarkets.live` routes to the Market Data Service
   - `transactions.pseudomarkets.live` routes to the Transaction Processing Service
   - `instruments.pseudomarkets.live` routes to the Trading Instruments Service
4. In Cloudflare, create one `A` record per hostname and point all of them to the same DigitalOcean load balancer IP.
5. Since Cloudflare will handle public SSL, Cert-Manager is not required for phase 1.
6. Decide whether the origin connection from Cloudflare to Kubernetes will be:
   - HTTP to the ingress
   - HTTPS to the ingress using a Cloudflare origin certificate

Recommended long-term target:

- Cloudflare Full (strict) with HTTPS from Cloudflare to the ingress origin

## Phase 7: Container Registry and Image Strategy
1. Create one DigitalOcean Container Registry for the platform.
2. Build and publish one image per service.
3. Tag images with commit SHA values.
4. Optionally tag `latest` from `main` for convenience.
5. Reference immutable SHA-based image tags from deployment manifests during rollout.

## Phase 8: GitHub Actions CI
Create a CI workflow triggered by pull requests and pushes.

Recommended responsibilities:

1. Checkout the repository
2. Setup .NET
3. Restore and build the root solution
4. Run `dotnet test` across the platform
5. Optionally validate Docker builds for all deployable services
6. Optionally validate Kubernetes manifests with `kustomize build`

Planned workflow file:

- `.github/workflows/ci.yml`

## Phase 9: GitHub Actions CD
Create a production deployment workflow.

Recommended triggers:

- Push to `main`
- Manual `workflow_dispatch`

Recommended responsibilities:

1. Build service images
2. Log in to DigitalOcean Container Registry
3. Push images
4. Install `doctl`
5. Fetch kubeconfig for the DOKS cluster
6. Apply namespace, config, secrets, storage, and stateful workload manifests
7. Wait for PostgreSQL and Aerospike readiness
8. Run the migration job
9. Run the trading instrument seed job
10. Apply or update application deployments
11. Wait for rollout success
12. Optionally run smoke checks against the public service URLs

Planned workflow file:

- `.github/workflows/deploy.yml`

Use a GitHub `production` environment so deployment secrets and optional approval rules are centralized.

## Phase 10: GitHub Secrets and Variables
Recommended GitHub Actions environment secrets:

- `DIGITALOCEAN_ACCESS_TOKEN`
- `DOCR_REGISTRY_NAME`
- `DOKS_CLUSTER_NAME`
- `K8S_NAMESPACE`
- `JWTCONFIGURATION_KEY`
- `TWELVEDATA_APIKEY`
- `POSTGRES_PASSWORD`

Recommended GitHub Actions variables for non-sensitive values:

- Public hostnames
- Namespace name
- Image repository names

## Phase 11: Documentation
Update the root README with:

1. Kubernetes architecture overview
2. DigitalOcean prerequisites
3. Cloudflare DNS setup
4. GitHub Actions secret setup
5. Deployment flow
6. Rollback flow
7. Verification steps for each public service URL

Add a deployment runbook under `docs` covering:

- First-time cluster bootstrap
- Redeployments
- Failed migration troubleshooting
- Secret rotation

## Phase 12: Validation
After implementation, validate:

1. `kustomize build` succeeds for the target overlay
2. GitHub Actions workflows lint and execute cleanly
3. A fresh cluster bootstrap works end-to-end
4. Public hostnames resolve correctly through Cloudflare
5. Swagger is reachable on each service hostname
6. Migration and seed jobs are repeatable enough for redeployments

## Recommended Order Of Work
1. Add Identity Server health endpoint support
2. Create Kubernetes folder structure and base manifests
3. Add PostgreSQL and Aerospike manifests
4. Add migration and seed jobs
5. Add service deployments and services
6. Add ingress host routing
7. Add GitHub Actions CI
8. Add GitHub Actions CD
9. Update documentation
10. Validate on DigitalOcean Kubernetes with Cloudflare in front

## Open Decision
Before implementation, confirm the Cloudflare origin mode:

- Simpler initial setup: Cloudflare HTTPS to users and HTTP to the cluster ingress
- Stronger long-term setup: Cloudflare Full (strict) with HTTPS to the ingress origin

Recommended target:

- Cloudflare Full (strict)
