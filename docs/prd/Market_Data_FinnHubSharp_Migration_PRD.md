# Product Requirements Document

## Feature Name

Migrate Pseudo Markets Market Data Service From TwelveDataSharp To FinnHubSharp

## Description

Replace the Market Data Service's upstream data-provider dependency from `TwelveDataSharp` to `FinnHubSharp` while preserving the current external API surface of the Pseudo Markets Market Data Service as much as possible.

The one approved contract adjustment is the detailed quote response: fields that are not supported by the `GetQuoteAsync` method in `FinnHubSharp` may be removed from the detailed quote response model as part of this migration.

For U.S. market indices specifically, the service should use Yahoo Finance instead of Finnhub because the Finnhub free tier does not provide the required index market data.

## Problem Statement

The current Market Data Service uses `TwelveDataSharp` as its provider integration library. That provider is no longer the best fit for this project because its license terms and free-tier API limits are less favorable for an open-source stock trading simulation platform that is currently operating within free-tier constraints.

If the project continues to depend on the current provider, the platform risks tighter usage ceilings and less suitable licensing for long-term development and testing.

## Why

This migration matters because it improves the operational and legal fit of the upstream market-data dependency without forcing breaking changes on downstream services or future clients.

The expected value includes:

- better alignment with the project's open-source usage model
- higher free-tier request capacity for local development and testing
- reduced need to revisit the Market Data Service public API just to change providers
- clearer abstraction between the Market Data Service contract and the underlying third-party source

## Audience

- platform developers working on the Pseudo Markets monorepo
- internal platform services that depend on the Market Data Service
- future frontend or external consumers that call the Market Data Service endpoints
- maintainers responsible for local Docker and environment configuration

## What

The Market Data Service should continue to expose the same endpoints, request models, authorization behavior, and cache behavior that it supports today.

The implementation should be updated so that:

- provider calls are made through `FinnHubSharp` instead of `TwelveDataSharp`
- service responses remain compatible with the current Market Data Service contract
- the current quote and detailed quote functionality continues to work through the existing API surface using Finnhub as the upstream provider
- the current indices functionality continues to work through the existing API surface using Yahoo Finance as the upstream provider
- secrets and configuration are updated to use a Finnhub API key instead of a Twelve Data API key
- quote, detailed quote, and indices caching continue to use the existing Market Data Service caching mechanism, with configurable TTLs that default to 15 minutes
- local non-Docker runs, service-local Docker Compose, and the root Docker Compose workflow all remain supported

The migration should preserve the existing boundary that downstream services call the Pseudo Markets Market Data Service rather than calling the provider library directly.

For the detailed quote response specifically, the service may drop fields that are not available from `FinnHubSharp` `GetQuoteAsync`, rather than attempting to synthesize or backfill unsupported values from another provider.

## How

Implementation should follow the existing Market Data Service architecture and replace the provider integration behind that abstraction rather than redesigning the service contract.

The provider design should become split by responsibility:

- Finnhub for latest quote and detailed quote data
- Yahoo Finance for U.S. market index values

High-level implementation expectations:

- use the `FinnHubSharp` NuGet package as the new provider dependency:
  [https://www.nuget.org/packages/FinnHubSharp/](https://www.nuget.org/packages/FinnHubSharp/)
- use the package source repository as the implementation reference:
  [https://github.com/pseudomarkets/FinnHubSharp](https://github.com/pseudomarkets/FinnHubSharp)
- add the `FinnHubSharp` NuGet package to the Market Data Service provider layer
- remove the direct dependency on `TwelveDataSharp`
- refactor provider-facing services or adapters under `pseudomarkets-nextgen-marketdata` so they map Finnhub responses into the existing internal quote contracts and Yahoo Finance responses into the existing internal indices contracts
- update the detailed quote response contract and mapping only as needed to remove fields that are not supported by `FinnHubSharp` `GetQuoteAsync`
- update configuration objects, appsettings, `.env.example`, Docker Compose environment variables, and README documentation to use the Finnhub API key name
- update the quote, detailed quote, and indices cache TTL configuration so each cache duration is configurable and defaults to 15 minutes to reduce upstream API usage
- preserve the existing shared authorization flow and Aerospike caching behavior
- keep the current controller routes and HTTP verbs unchanged
- add or update unit tests so provider mapping and service behavior remain covered after the migration

If Finnhub or Yahoo Finance response shapes do not perfectly match the current provider, the Market Data Service should adapt the provider response internally so the external API remains stable.

Based on the linked source repository, `FinnHubSharp` is described as a .NET Standard 2.1 client for Finnhub APIs and includes example usage through `HttpClient`. The implementation should validate that the selected package version is compatible with the current Market Data Service target framework and supports the quote and related data flows needed by the existing endpoints.

For index data, the service should call Yahoo Finance's chart API directly for the required symbols. Example reference for the S&P 500:
[https://query2.finance.yahoo.com/v8/finance/chart/%5EGSPC](https://query2.finance.yahoo.com/v8/finance/chart/%5EGSPC)

The indices endpoint must continue to return current values for:

- S&P 500
- Dow Jones Industrial Average
- NASDAQ Composite

The `points` field in each index snapshot should be populated from the Yahoo Finance `regularMarketPrice` field.

## Acceptance Criteria

- [ ] The Market Data Service no longer depends on `TwelveDataSharp` and instead uses `FinnHubSharp`.
- [ ] The current Market Data Service endpoint routes and request contracts remain unchanged for existing consumers.
- [ ] Quote and detailed quote endpoints continue to return valid data through the existing routes using Finnhub as the upstream source.
- [ ] The indices endpoint continues to return valid data through the existing route using Yahoo Finance as the upstream source.
- [ ] The indices endpoint returns current values for the S&P 500, Dow Jones Industrial Average, and NASDAQ Composite.
- [ ] The indices endpoint populates `IndexSnapshotResponse.Points` from Yahoo Finance `regularMarketPrice`.
- [ ] The detailed quote response model is reduced only where necessary to remove fields unsupported by `FinnHubSharp` `GetQuoteAsync`.
- [ ] Existing authorization behavior for Market Data endpoints remains unchanged.
- [ ] Existing Aerospike caching behavior remains in place unless an implementation detail must change internally without changing the service contract.
- [ ] Quote, detailed quote, and indices responses use the existing caching mechanism with configurable TTLs that default to 15 minutes.
- [ ] Local development configuration, Docker Compose configuration, and service documentation are updated to reference the Finnhub API key instead of the Twelve Data API key.
- [ ] Unit tests are added or updated to cover the new provider integration and response mapping behavior.
- [ ] The full platform solution builds successfully after the migration.

## Out Of Scope

- changing Market Data Service endpoint routes or verbs
- redesigning Market Data authorization
- changing downstream consumers such as Order Execution to call Finnhub directly
- changing downstream consumers to call Yahoo Finance directly
- introducing a second provider or provider failover strategy
- replacing the existing Market Data Service caching mechanism with a new caching design

## Notes

- The reason for the provider switch is that Finnhub offers more suitable license terms for this project and higher free-tier API usage limits.
- This should be treated as an internal provider migration, not an external Market Data Service contract redesign.
- Any provider-specific field gaps should be handled through adapter logic inside the Market Data Service.
- The approved exception to strict response compatibility is the detailed quote contract. Fields may be removed if they are not supported by `FinnHubSharp` `GetQuoteAsync`.
- Finnhub is only intended to cover quote-oriented endpoints in this migration. U.S. market indices should come from Yahoo Finance because the Finnhub free tier does not cover the required index data.
- The linked `FinnHubSharp` source repository describes the package as a .NET Standard 2.1 client and also notes support for streaming data via WebSocket. This migration does not require adding WebSocket behavior to the Market Data Service unless it later becomes necessary for parity or performance.
- During implementation, validate the package license, supported frameworks, and the specific provider operations needed for quotes and detailed quote data before fully removing the current provider dependency.
- The Yahoo Finance chart API should be treated as a lightweight HTTP integration for index retrieval in this feature, not as a full replacement SDK for all market data operations.
