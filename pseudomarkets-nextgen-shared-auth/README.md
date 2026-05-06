# Pseudo Markets Shared Authorization

`pseudomarkets-nextgen-shared-auth` contains the reusable authorization client, ASP.NET Core filter, and request metadata models used by Pseudo Markets services that delegate authorization to the Identity Server.

## Responsibilities

- Call `POST /api/identity/authorize` on the Identity Server
- Evaluate authorization success or failure for a requested platform action
- Surface authorized request metadata to downstream handlers
- Keep controller authorization attributes consistent across services

## Current Metadata Flow

When authorization succeeds, the shared authorization filter stores the following values in `HttpContext.Items`:

- authorized user ID
- authorized token type (`USER` or `SYSTEM`)
- required authorization action

This allows services such as Order Execution to enforce account-ownership rules without parsing JWT claims directly.

## IDP Response Shape

The shared auth client expects the Identity Server authorization endpoint to return:

- `success`
- `message`
- `userId`
- `tokenType`

If authorization succeeds but the Identity Server omits `userId` or `tokenType`, the shared auth client treats the response as an authorization dependency failure.

## Build And Test

From the repository root:

```bash
dotnet test pseudomarkets-nextgen-shared-auth/tests/PseudoMarkets.Shared.Authorization.Tests/PseudoMarkets.Shared.Authorization.Tests.csproj -m:1
```
