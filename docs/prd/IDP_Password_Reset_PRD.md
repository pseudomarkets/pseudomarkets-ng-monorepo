# Product Requirements Document

## Feature Name

Pseudo Markets IDP Password Reset

## Description

Add password reset support to the Pseudo Markets Identity Provider service for standard user accounts. Since users do not provide an email address or phone number during sign-up, the system will generate a password reset key as a GUID when a new user account is created, return it to the caller at sign-up time, and store only a hashed form of the key in the IDP backing store. A new reset endpoint will allow a user to provide their login ID, password reset key, and new password. Password reset keys are one-time-use only, so after a successful reset the previous key becomes invalid, a new key is generated, and the new key is returned to the caller.

## Problem Statement

The current IDP sign-up flow allows a user to create an account with only a username and password. If the user later forgets the password, there is no recovery mechanism because the platform does not currently collect contact information such as email address or phone number. This creates a risk of permanent account lockout for public users and leaves the platform without a recovery path that fits the current sign-up model.

## Why

This feature establishes a recovery mechanism that works within the current platform model without introducing email-based or phone-based identity recovery. It gives users a recovery artifact at sign-up time, allows them to reset credentials later, and protects the reset flow by hashing the stored key and rotating it after every successful reset.

## Audience

- Public users creating accounts in the Pseudo Markets platform
- Developers and testers validating IDP account lifecycle behavior
- The Pseudo Markets IDP service and any downstream consumers of its account creation response

## What

The system should:

- Generate a password reset key as a GUID when a new non-SYSTEM user account is created
- Hash the password reset key before storing it with the account record in the IDP backing database
- Return the plaintext password reset key in the response from the create user endpoint when user account creation succeeds
- Add a password reset endpoint where a user provides their login ID, password reset key, and new password
- Validate the password reset key against the stored hashed value
- Allow password reset only for standard user accounts
- Explicitly disallow password reset for SYSTEM accounts
- Treat password reset keys as one-time-use values
- Generate and return a new password reset key after a successful password reset
- Require the caller to retain the key because it is only shown at sign-up time or immediately after a successful reset

Key workflow expectations:

- A caller submits a request to create a user account
- The IDP creates the account as it does today
- The IDP also generates a password reset key, hashes it, and persists the hashed value with the user record
- The successful create-account response includes the generated password reset key
- The caller is responsible for presenting or storing the key for the end user
- Later, if the user forgets their password, they submit their login ID, password reset key, and new password to the password reset endpoint
- If the provided key is valid for that user account, the IDP updates the password, invalidates the previous reset key, generates a new reset key, stores the new hashed key, and returns the new plaintext key in the response
- SYSTEM accounts are never eligible for this reset flow

## How

High-level implementation direction:

- Extend the IDP account model to include password reset key storage suitable for hashed reset keys
- Persist only the hashed password reset key in Aerospike with the account record
- Update the create-account result contract so the plaintext reset key is returned on successful user account creation
- Add a password reset API contract that accepts login ID, password reset key, and new password
- Implement password reset key verification and password update logic in the IDP core layer
- Generate a new reset key after every successful password reset and return it in the reset response
- Ensure reset keys are generated server-side as GUIDs and are never caller-supplied for storage
- Ensure the reset flow rejects SYSTEM accounts
- Update the Web API response models, core account provisioning flow, authentication-related services, persistence logic, and dependent tests

Likely impacted areas:

- `PseudoMarkets.Security.IdentityServer.Core`
- `PseudoMarkets.Security.IdentityServer.Web`
- Aerospike account persistence in the IDP data layer
- IDP unit tests and any API contract tests

## Acceptance Criteria

- [ ] When a new non-SYSTEM user account is created successfully, the IDP generates a GUID password reset key
- [ ] The generated password reset key is hashed before it is stored with the account in Aerospike
- [ ] The create user endpoint returns the plaintext password reset key in the success response for user accounts
- [ ] SYSTEM account creation does not produce a password reset key and SYSTEM accounts are not eligible for password reset
- [ ] A password reset endpoint exists that accepts login ID, password reset key, and new password
- [ ] When a valid password reset request is submitted for a user account, the password is updated successfully
- [ ] After a successful password reset, the previous reset key can no longer be used
- [ ] After a successful password reset, a new GUID password reset key is generated, hashed for storage, and returned in plaintext in the response
- [ ] The password reset key is generated by the server and cannot be provided or overridden by the caller for persistence
- [ ] Existing user account creation behavior continues to work aside from the expanded success response payload

## Out Of Scope

- Adding email-based or phone-based password recovery
- Adding UI or frontend experiences for storing or displaying the key beyond returning it from the API
- Multi-factor verification or secondary recovery channels
- Reset key expiration policies, if any
- Administrative recovery flows for lost reset keys
- Changing SYSTEM account creation rules beyond the current behavior

## Notes

- Reset keys are expected to be shown only during initial sign-up or immediately after a successful reset.
- It is expected that the user will note down and retain the key externally.
- A later refinement can define whether reset keys should expire or whether additional protections should be added around repeated failed reset attempts.
