# ADR-004: API-Key Scope Storage Strategy

**Status**: Accepted
**Date**: 2026-05-12
**Feature**: security-review-2026-05
**Decider**: Morgan (Solution Architect) based on Story S-5 (DISCUSS wave)

---

## Context

API keys today (`Lighthouse.Backend/Lighthouse.Backend/Models/Auth/ApiKey.cs`) carry only an `OwnerUserProfileId` / `OwnerSubject`. At authentication time (`ApiKeyAuthenticationHandler.cs:54-57`) the handler emits the owner's `sub` claim and downstream authorisation flows through `RbacAdministrationService.CanSatisfyRequirementAsync(principal, requirement, scopeId)` (`RbacAdministrationService.cs:299`) which resolves the owner's effective permissions from the `UserPermissions` table.

Net effect: an API key inherits the **full** RBAC scope of its owner. Story S-5 requires per-key narrowing (e.g., issue a key restricted to `PortfolioRead/42`).

Three storage strategies were evaluated.

---

## Decision

**Add a new `ApiKeyPermission` table modelled exactly on `UserPermission`, but keyed on `ApiKeyId` instead of `UserProfileId`.** The schema mirror is deliberate: it lets the existing permission-resolution machinery in `RbacAdministrationService` be reused with a single new lookup branch, minimising divergence.

Concretely:

- New entity `ApiKeyPermission(Id, ApiKeyId, Role, ScopeType, ScopeId?, GrantedAt)` — shape parallels `UserPermission` but loses `GrantedByUserProfileId` (the owner record on `ApiKey` is the audit anchor).
- New `DbSet<ApiKeyPermission>` on `LighthouseAppContext`, with FK `ApiKey.Id → ApiKeyPermission.ApiKeyId` and cascade-delete (deleting the key removes its scopes; no orphans).
- `ApiKeyAuthenticationHandler` emits an additional claim `api_key_id` (the integer ID of the authenticating key) alongside the existing `sub` and `auth_method=api-key` claims.
- `RbacAdministrationService` is extended at the **scope-resolution** layer (not the requirement layer). When the principal carries an `api_key_id` claim, the effective permissions become `intersection(owner_permissions, apikey_permissions)`. If `ApiKeyPermissions` has zero rows for the key, the key inherits the full owner scope (backwards-compatible default; existing keys remain functional after migration).
- `POST /api/apikeys` accepts an optional `scope: { role: PortfolioRead|PortfolioWrite|TeamRead|TeamWrite|SystemAdmin, scopeId?: int }` array. The endpoint validates that the **caller's own** permissions are a superset of the requested scope (a non-admin cannot mint a SystemAdmin key for themselves).

---

## Alternatives Considered

### Option A: `Scope` JSON column on `ApiKey` with a discriminated union (rejected)

Store `Scope` as `string?` (serialised JSON) on the `ApiKey` row. Deserialise at authentication time and translate into an in-memory permission set.

**Rejected because**:
- Introduces a second representation of permissions. `UserPermission` uses normalised relational rows; a JSON column duplicates the model and forks the resolution logic.
- Querying ("which keys can read Portfolio 42?") requires JSON predicates — provider-specific between SQLite and PostgreSQL, both of which Lighthouse supports.
- Multi-scope keys (e.g., read on Team 7 AND read on Portfolio 11) require either a list inside JSON or a single-scope cap. Both are inferior to relational rows.
- Schema migration brittleness: changing the union shape requires data migration on every existing JSON blob.

### Option B: Reuse `UserPermission` directly via a new join table (rejected)

Create a `ApiKeyUserPermission(ApiKeyId, UserPermissionId)` join. Each key references one or more existing `UserPermission` rows.

**Rejected because**:
- Couples API-key lifecycle to user-permission lifecycle: revoking a permission from the owner silently shrinks every linked key, which is surprising and hard to audit.
- Requires synthetic `UserPermission` rows that don't belong to any user, polluting the user permission table semantically.
- Forces a join on every authentication; the parallel-table approach (Option C) is one direct lookup by `ApiKeyId`.

### Option C: Parallel `ApiKeyPermission` table mirroring `UserPermission` (selected)

**Accepted because**:
- Schema parallel: existing reviewers and the data layer recognise the shape immediately.
- Resolution logic in `RbacAdministrationService` extends by a single private method (`GetApiKeyPermissionsAsync(int apiKeyId, …)`) that mirrors the existing `GetEffectivePermissionsAsync` shape.
- Decoupled lifecycle: revoking owner permissions does not silently widen or narrow key scope; cascade-delete on key removal is the only implicit link.
- Auditable: each key's scope is a finite set of rows that can be listed in the API key management UI.
- Migration is additive: a new EF migration adds one table and one nullable claim, no data backfill required (zero rows = inherit owner = existing behaviour).

---

## Consequences

**Positive**:
- Per-key least privilege: a leaked key is bounded by its scope, not by the owner's full role.
- Resolution logic stays in one place (`RbacAdministrationService`); no parallel permission engine.
- The new `api_key_id` claim is a single, well-known seam for future extensions (rate-limit per key, audit per key).

**Negative**:
- One new table, one new claim, two new fields on the API key creation request DTO. The `CreateApiKeyRequest` and `ApiKeyCreationResult` contracts evolve. Existing clients that don't send `scope` continue to receive owner-scoped keys (backwards-compatible).
- Migration must be generated via the project's `CreateMigration` PowerShell script per `backend-csharp.instructions.md`.

**Quality attribute impact**:
- Security: improved — least privilege is now expressible at the key level.
- Maintainability: improved — single permission model, single resolver.
- Testability: improved — the intersection rule has a small, finite test surface (owner-only, key-only, intersection, empty-key-fallback).
