# ADR-008: OAuth Credential Storage — Separate Entity, Configuration Reuses Options

**Status**: Accepted
**Date**: 2026-05-14
**Feature**: work-tracking-oauth-authentication
**Decider**: Morgan (Solution Architect) based on DISCUSS D6 (with DESIGN refinement DDD-3)

---

## Context

Each OAuth-authenticated work-tracking-system connection carries two distinct kinds of data:

- **Static configuration** — `clientId` and `clientSecret` (registered with the IdP once; rarely changes).
- **Runtime credential** — `accessToken`, `refreshToken`, `expiresAt`, `status` (issued by the IdP; rotated on every refresh; status changes when the IdP revokes).

DISCUSS D6 mandated that the two be persisted separately. The DESIGN wave then evaluated *how* to model the separation against the existing storage surfaces:

- `WorkTrackingSystemConnection` (`Models/WorkTrackingSystemConnection.cs:7-42`) holds `AuthenticationMethodKey` (string, already keys the provider) and a `List<WorkTrackingSystemConnectionOption>` of named secret options encrypted at rest via `ICryptoService` (registered in `Program.cs:679`).
- `LighthouseAppContext` (`Data/LighthouseAppContext.cs:17`) already takes `ICryptoService` and applies an encrypted-string value converter to flagged columns.

Three storage strategies were evaluated.

---

## Decision

**Reuse `WorkTrackingSystemConnectionOption` for `clientId` (`IsSecret = false`) and `clientSecret` (`IsSecret = true`). Introduce one new entity, `OAuthCredential`, for the runtime credential.**

```
WorkTrackingSystemConnection (existing)
├── AuthenticationMethodKey: "jira.oauth"           — provider keying (no new field)
├── Options: [                                       — existing pattern
│       { Key: "oauth.clientId",     Value: "...", IsSecret: false },
│       { Key: "oauth.clientSecret", Value: "...", IsSecret: true  }
│   ]
└── OAuthCredential (new, 1-to-1, cascade delete)
    ├── AccessToken    (encrypted via value converter)
    ├── RefreshToken   (encrypted via value converter)
    ├── ExpiresAt      (DateTimeOffset)
    ├── Status         (OAuthCredentialStatus enum: Valid / RefreshFailed / Disconnected)
    └── UpdatedAt      (DateTimeOffset)
```

The migration adds the `OAuthCredentials` table with `FK WorkTrackingSystemConnectionId → WorkTrackingSystemConnections.Id` and a `ON DELETE CASCADE` constraint. Generated via the existing `CreateMigration` PowerShell script for SQLite + PostgreSQL (per `CLAUDE.md`).

---

## Consequences

**Positive**

- `clientId` / `clientSecret` ride the existing encryption pipeline — zero new crypto code, zero new edit-form code (the existing options form already renders `IsSecret` fields correctly).
- `OAuthCredential` is a single-row update for token rotation under DDD-7's single-flight refresh — atomic without database-level transactions across multiple rows.
- `Status` is enum-typed, queryable directly (`SELECT ... WHERE Status = 1` for "needs reconnect"), and the connection-list payload's new `RequiresReconnect` flag (per `adr-006-connection-list-payload-shape.md`) populates from a typed join, not a string-typed Options-row scan.
- Cascade delete makes connection cleanup atomic: deleting a connection deletes its credential and all its option rows in one transaction.

**Negative**

- Two storage shapes for "encrypted-at-rest connection data" exist (Options rows + `OAuthCredential` columns). Mitigation: the boundary is principled — configuration is named/optional/many; credentials are typed/required/one. A future audit reading "where are this connection's secrets?" must look in both places, but the join is trivial.

---

## Alternatives considered

### Alternative A — Everything in `WorkTrackingSystemConnectionOption`

Add Options rows for `oauth.accessToken`, `oauth.refreshToken`, `oauth.expiresAt`, `oauth.status` alongside `oauth.clientId` / `oauth.clientSecret`.

- Rejected:
  - Token rotation under DDD-7 (single-flight refresh) requires updating three rows atomically (`accessToken`, `refreshToken`, `expiresAt`). EF Core can do this in one `SaveChangesAsync`, but the *intent* — "these three fields move together" — is not expressed in the schema. A bug that updates only the access token without the matching refresh token would not be caught by a constraint.
  - `Status` is fundamentally typed (enum, 3 values). Stringly-typed storage invites comparison bugs (`"refreshfailed"` vs `"RefreshFailed"`).
  - Querying "all connections needing reconnect" becomes a `WHERE Key = 'oauth.status' AND Value = 'RefreshFailed'` join — slow, brittle, and a violation of normalisation.

### Alternative B — A new `OAuthConfiguration` entity for clientId/clientSecret (DISCUSS D6 read literally)

Two new entities: `OAuthConfiguration` (clientId, clientSecret) + `OAuthCredential` (tokens).

- Rejected: duplicates the encryption pipeline already provided by `WorkTrackingSystemConnectionOption` + `ICryptoService`. The Reuse Analysis hard-gate (per `nw-design` skill F-1 rule) forbids this when extending is cheaper and creates no coupling.
- The *separation* intent of DISCUSS D6 is preserved by Alternative-Decision: `OAuthCredential` is still a distinct table from the Options table; the two are not co-mingled.
- The DESIGN wave's back-propagation entry (`feature-delta.md` § "Upstream changes (back-propagation)") records this refinement explicitly and confirms no DISCUSS AC needs amendment.

### Alternative C — Single new entity holding both config and credential

One `OAuthConnection` entity with all six fields (clientId, clientSecret, accessToken, refreshToken, expiresAt, status).

- Rejected: mixes lifecycles. Configuration is admin-edited; credentials are background-updated by the refresh service. Co-locating them invites accidental overwrites (e.g., a credential refresh that re-reads configuration into memory and writes back, clobbering an in-flight clientSecret rotation).
- Also: requires re-implementing the encrypted-secret-with-IsSecret-flag UX from `WorkTrackingSystemConnectionOption` for the clientSecret field.

---

## References

- DISCUSS D6 (locked, with DESIGN refinement DDD-3)
- Reuse Analysis: `feature-delta.md` § "Wave: DESIGN / [REF] Reuse Analysis", rows 2 and 11
- Back-propagation: `feature-delta.md` § "Wave: DESIGN / [REF] Upstream changes (back-propagation)"
- Existing pattern: `WorkTrackingSystemConnectionOption.cs`, `CryptoService.cs`, `LighthouseAppContext.cs:17`
