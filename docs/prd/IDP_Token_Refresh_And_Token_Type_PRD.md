# Product Requirements Document

## Feature Name
Pseudo Markets IDP Token Refresh and Token Type Authorization Enhancements

## Description
Enhance the Identity Provider and shared authorization library so platform clients can refresh access tokens and downstream services can determine whether an authorized token represents a `USER` account or a `SYSTEM` account. This will support long-running service-to-service workflows and account-ownership checks in services such as Order Execution.

## Problem Statement
The current IDP authentication endpoint returns a JWT and expiration timestamp, but it does not provide a refresh-token workflow. All access tokens, including system account tokens, expire after one hour. Services that need to call dependent platform APIs must either authenticate repeatedly with stored credentials or fail when a token expires. This is especially problematic for service-to-service workflows, such as Order Execution calling Trading Instruments, Market Data, and Transaction Processing, where the service must be able to maintain a valid access token at any time without sending credentials for every downstream request.

Frontend applications also need a refresh path for user tokens. A user's access token may expire while their browser session is still active. Without refresh-token support, the frontend must either force the user to sign in again after every access-token expiration or rely on less controlled token-lifetime workarounds.

The current authorization response also does not expose token type. The IDP has account types (`USER` and `SYSTEM`), but downstream services currently have to infer system behavior from role combinations or parse token contents themselves. That makes authorization-sensitive business rules harder to implement consistently. For example, Order Execution needs to allow system tokens to submit orders for any user ID, while user tokens must only submit orders for the user ID in the token.

## Why
Token refresh support reduces credential reuse, simplifies long-running services, and provides a cleaner foundation for service-to-service authentication. System services should be able to refresh access tokens whenever their current one is near expiration, and frontend applications should be able to keep users authenticated while their browser session is still active. Adding token type to the authorization response gives downstream services a direct, consistent way to enforce account-ownership rules without duplicating IDP-specific token parsing logic.

These enhancements should make the shared auth library the common integration point for authorization metadata, instead of each service implementing its own token interpretation.

## Audience
This feature is for backend platform services, especially services that call other protected platform APIs. Direct consumers include the Order Execution Service, Market Data Service, Trading Instruments Service, Transaction Processing Service, and any future backend client that needs to refresh system tokens.

Frontend applications are also direct consumers because they need to refresh user tokens while a browser session remains active.

Developers working in the IDP and shared authorization library are also direct consumers of this PRD.

## What
The IDP should issue both an access token and a refresh token when credentials are successfully authenticated. The access token should continue to be a JWT and should keep the existing one-hour expiration behavior unless a separate PRD changes token lifetimes. The refresh token should be an opaque token that can be exchanged for a new access token without resubmitting the account password.

The IDP should add a token refresh endpoint. The refresh endpoint should validate the refresh token, ensure the related account is still active, and issue a new access token. Refresh tokens should expire and should be rotated on use so a consumed refresh token cannot be reused.

System account refresh tokens should support service-to-service clients that may need to refresh at any time while the service is running. User account refresh tokens should support frontend browser sessions that are still active. Refresh-token validation should therefore distinguish between expired or invalid refresh tokens and valid refresh tokens associated with still-active accounts or sessions.

The IDP authorization endpoint should include token type in its response. Token type should be based on the authenticated account's account type and should use the existing account type values: `USER` and `SYSTEM`. The authorization response should continue to include authorization success, message, and user ID.

The shared authorization library should update its contracts and client models to surface token type and user ID from the IDP authorization response. Services using the shared auth library should be able to access the current authorization metadata after a request is authorized. At minimum, the shared auth library should expose:

- whether the request was authorized
- authorized user ID
- token type (`USER` or `SYSTEM`)
- authorization failure details when applicable

The shared auth library should provide a standard way for services to inspect the authorized token type and user ID during request handling. This can be implemented using a scoped authorization context, `HttpContext.Items`, or another established ASP.NET Core pattern, but the behavior should be consistent across services.

## How
High level implementation should include the following:

1. Extend the IDP authentication response contract to include an opaque refresh token and refresh-token expiration timestamp.
2. Add a refresh-token persistence model. Refresh tokens must be stored securely, preferably hashed at rest, and associated with the account login ID, user ID, account type, issued timestamp, expiration timestamp, and revoked or consumed state.
3. Add a new IDP endpoint, `POST /api/identity/refresh`, that accepts a refresh token and returns a new access token, new access-token expiration, new refresh token, and new refresh-token expiration.
4. Rotate refresh tokens on use. A successful refresh should mark the submitted refresh token as consumed or revoked and issue a replacement refresh token.
5. Reject refresh requests when the refresh token is missing, malformed, expired, already consumed, revoked, or associated with an inactive or missing account.
6. Support refresh for system accounts so long-running services can maintain valid access tokens for service-to-service calls.
7. Support refresh for user accounts so frontend applications can maintain access tokens while the user's browser session remains active.
8. Include account type in generated JWTs as a `token_type` or `account_type` claim so authorization can return token type without relying on role inference.
9. Update IDP authorization logic to read token type from the validated token and return it in the authorization response as `tokenType`.
10. Preserve the existing `id` claim behavior and continue returning the authorized user ID in the authorization response.
11. Update shared auth contracts so `IdentityAuthorizationResponse` includes `userId` and `tokenType`.
12. Update `AuthorizationDecision` or add an equivalent shared auth model so downstream services can access the authorized user ID and token type after authorization succeeds.
13. Update the shared authorization filter to make successful authorization metadata available to the current request pipeline.
14. Update README files for the IDP and shared auth library with the new authentication, refresh, authorization response, and service-consumption behavior.

## Acceptance Criteria

- [ ] `POST /api/identity/authenticate` returns access token, access-token expiration, refresh token, and refresh-token expiration when credentials are valid.
- [ ] Access tokens continue to expire after one hour unless a separate token-lifetime requirement changes this behavior.
- [ ] Refresh tokens are opaque values and are not JWTs.
- [ ] Refresh tokens are stored securely, with token material hashed or otherwise protected at rest.
- [ ] Refresh tokens are associated with account login ID, user ID, account type, issued timestamp, expiration timestamp, and consumed or revoked state.
- [ ] `POST /api/identity/refresh` accepts a refresh token and returns a new access token, access-token expiration, refresh token, and refresh-token expiration.
- [ ] Refresh tokens are rotated on successful refresh.
- [ ] Reusing a consumed refresh token is rejected.
- [ ] Expired, revoked, malformed, unknown, or missing refresh tokens are rejected.
- [ ] Refresh is rejected when the related account is missing or inactive.
- [ ] System account refresh tokens support long-running service-to-service clients that need to refresh access tokens at any time while the service is running.
- [ ] User account refresh tokens support frontend applications refreshing access tokens while the user's browser session is still active.
- [ ] Frontend user refresh behavior distinguishes active browser sessions from expired or invalid refresh-token state.
- [ ] Access tokens include token type using the existing account type values `USER` and `SYSTEM`.
- [ ] `POST /api/identity/authorize` returns token type in addition to success, message, and user ID.
- [ ] Authorization responses use `USER` for user accounts and `SYSTEM` for system accounts.
- [ ] Shared auth `IdentityAuthorizationResponse` includes `userId` and `tokenType`.
- [ ] Shared auth authorization decisions expose authorized user ID and token type after successful authorization.
- [ ] Shared auth middleware/filter makes authorization metadata available to downstream request handling code.
- [ ] Existing services using shared authorization continue to authorize requests without breaking changes to their attributes.
- [ ] Unit tests cover authentication response shape, refresh-token issuance, refresh-token rotation, rejected refresh-token reuse, expired refresh tokens, revoked refresh tokens, inactive accounts, token type claim generation, authorization response token type, shared auth response parsing, and request-level authorization metadata exposure.
- [ ] IDP README and shared auth README are updated with the new endpoints, response fields, configuration notes, and service-consumption pattern.
- [ ] The full solution builds and `dotnet test PseudoMarkets.NextGen.Platform.sln -m:1` passes.

## Out Of Scope

- Implementing OAuth 2.0 client credentials flow.
- Implementing third-party identity provider federation.
- Adding frontend UI for token refresh.
- Adding fine-grained refresh-token device/session management UI.
- Changing existing role names or authorization action names.
- Implementing order execution logic.

## Open Questions

- What should the refresh-token lifetime be for user accounts?
- Should system accounts use a different refresh-token lifetime from user accounts?
- How should the IDP determine whether a frontend browser session is still active for user-token refresh?
- Should the token type claim be named `token_type` or `account_type`?
- Should refresh-token storage live in Aerospike with account records, or in PostgreSQL with other platform persistence?

## Notes

- Existing IDP account types are `USER` and `SYSTEM`.
- Current JWTs include an `id` claim for user ID and a `roles` claim for authorization actions.
- Current IDP authorization response includes success, message, and user ID.
- Current shared auth `IdentityAuthorizationResponse` includes success and message only.
- This PRD should be completed before implementing Order Execution authorization logic that depends on explicit token type.
