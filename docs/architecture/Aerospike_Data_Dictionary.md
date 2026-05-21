# Pseudo Markets Aerospike Data Dictionary

## Overview

Pseudo Markets uses Aerospike for high-speed key-value storage. The shared namespace is `nsPseudoMarkets`.

Current Aerospike responsibilities are:

- IDP account, user ID reservation, and refresh token storage.
- Market Data Service quote and index response caching.

The Docker-backed namespace is configured to persist to disk.

## Namespace

### `nsPseudoMarkets`

| Property | Value |
| --- | --- |
| Purpose | Shared Aerospike namespace for identity data and market data cache records. |
| Persistence | Disk-backed namespace in the local Docker configuration. |
| Host port | `3000` when running through Docker Compose. |

## Identity Server Sets

### `sAccounts`

Stores IDP account records keyed by login ID.

| Attribute | Value |
| --- | --- |
| Namespace | `nsPseudoMarkets` |
| Set | `sAccounts` |
| Primary key | Login ID / username |
| Write behavior | Create uses create-only writes; updates require an existing record. |
| Owner | Identity Server |

| Bin | Type | Description |
| --- | --- | --- |
| `bUserId` | Integer | 10-digit user ID issued by IDP. |
| `bPass` | String | Hashed account password. |
| `bResetKey` | String | Hashed password reset key. Used for user password reset flows. |
| `bType` | String | Account type, such as `USER` or `SYSTEM`. |
| `bRoles` | List of strings | Roles assigned to the account. |
| `bActive` | Boolean | Whether the account is active. |
| `bFailCnt` | Integer | Failed login attempt count. |
| `bLockoutTs` | Long | UTC ticks for lockout expiration, or `0` when not locked out. |

### `sUserIds`

Stores reserved user IDs so generated 10-digit IDs are not duplicated.

| Attribute | Value |
| --- | --- |
| Namespace | `nsPseudoMarkets` |
| Set | `sUserIds` |
| Primary key | 10-digit user ID |
| Write behavior | Create-only reservation. Existing keys are treated as collisions. |
| Owner | Identity Server |

| Bin | Type | Description |
| --- | --- | --- |
| `bUserId` | Integer | Reserved 10-digit user ID. |
| `bLoginId` | String | Login ID associated with the reserved user ID. |

### `sTokens`

Stores refresh token records keyed by token ID.

| Attribute | Value |
| --- | --- |
| Namespace | `nsPseudoMarkets` |
| Set | `sTokens` |
| Primary key | Refresh token ID |
| Write behavior | Create uses create-only writes; updates require an existing record. Token consumption uses Aerospike generation checks. |
| Owner | Identity Server |

| Bin | Type | Description |
| --- | --- | --- |
| `bTokHash` | String | Hashed refresh token secret. |
| `bLoginId` | String | Login ID associated with the token. |
| `bUserId` | Integer | User ID associated with the token. |
| `bType` | String | Account type associated with the token. |
| `bIssuedTs` | Long | Issued-at UTC ticks. |
| `bExpireTs` | Long | Expiration UTC ticks. |
| `bConsumed` | Boolean | Indicates whether the token has been consumed. |
| `bRevoked` | Boolean | Indicates whether the token has been revoked. |

## Market Data Cache Sets

Market data cache records are best-effort cache entries. If Aerospike is unavailable or a cache read/write fails, the Market Data Service continues serving provider-backed responses where possible.

Cache TTL values are configured through Market Data Service configuration. The default intended TTL for quote, detailed quote, and indices cache records is 15 minutes.

### `sMarketQuotes`

Caches latest quote responses by symbol.

| Attribute | Value |
| --- | --- |
| Namespace | `nsPseudoMarkets` |
| Set | `sMarketQuotes` |
| Primary key | Uppercase symbol |
| Owner | Market Data Service |
| TTL | `QuoteTtlSeconds` |

| Bin | Type | Description |
| --- | --- | --- |
| `symbol` | String | Symbol associated with the quote. |
| `price` | String | Latest price serialized using invariant culture. |
| `source` | String | Data source label, including cached-source labels when served from cache. |
| `timestampUtc` | String | ISO 8601 timestamp for the quote. |

### `sDetailedMarketQuotes`

Caches detailed quote responses by symbol.

| Attribute | Value |
| --- | --- |
| Namespace | `nsPseudoMarkets` |
| Set | `sDetailedMarketQuotes` |
| Primary key | Uppercase symbol |
| Owner | Market Data Service |
| TTL | `DetailedQuoteTtlSeconds` |

| Bin | Type | Description |
| --- | --- | --- |
| `symbol` | String | Symbol associated with the quote. |
| `name` | String | Instrument name or description returned by the market data provider. |
| `open` | String | Open price serialized using invariant culture. |
| `high` | String | High price serialized using invariant culture. |
| `low` | String | Low price serialized using invariant culture. |
| `close` | String | Current or close price serialized using invariant culture. |
| `previousClose` | String | Previous close price serialized using invariant culture. |
| `change` | String | Price change serialized using invariant culture. |
| `changePct` | String | Percentage change serialized using invariant culture. |
| `source` | String | Data source label, including cached-source labels when served from cache. |
| `timestampUtc` | String | ISO 8601 timestamp for the detailed quote. |

### `sMarketIndices`

Caches the U.S. market indices response.

| Attribute | Value |
| --- | --- |
| Namespace | `nsPseudoMarkets` |
| Set | `sMarketIndices` |
| Primary key | `us-indices` |
| Owner | Market Data Service |
| TTL | `IndicesTtlSeconds` |

| Bin | Type | Description |
| --- | --- | --- |
| `indicesPayload` | String | JSON serialized `IndicesResponse` payload containing S&P 500, Dow Jones Industrial Average, and NASDAQ snapshots. |

