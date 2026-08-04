# ADR-131: Embed token lifecycle — database-backed single use and revocation

**Status**: Accepted
**Date**: 2026-08-04
**Feature**: `epic-5146-jira-forge-app` (ADO Epic 5146, Story 5641)
**Decider**: Morgan (Solution Architect), DESIGN re-run after slice 01

---

## Context

[ADR-129](./adr-129-embed-session-token-exchange-and-identity.md) introduces a token that converts an
API key into a browser session inside a third-party frame. It is a bearer credential that grants a
session: whoever holds it becomes the key's principal. Short expiry, single use and revocation are
therefore part of the slice, not follow-ups (recorded in the feature record on 2026-08-03).

Two deployment facts constrain where that state can live:

- **Multiple replicas are a supported topology.** Epic 5305 designed for it (ADR-075 SignalR Redis
  backplane, ADR-076 cluster-aware update queue). In-process memory would make a token redeemable once
  *per replica*, which is not single use.
- **Redis is optional.** The Helm chart treats it as a toggle; a single-VM self-hosted install has
  none. The application database is the only store the product can rely on — the same reasoning
  ADR-005 used when it declined a distributed rate limiter.

## Decision

**A new `EmbedSessionToken` entity in the application database, with single use enforced by a
conditional update and revocation carried by the API key that minted the token.**

### Schema

`EmbedSessionToken(Id, TokenId, SecretHash, ApiKeyId, CreatedAt, ExpiresAt, RedeemedAt?, RevokedAt?)`

- `TokenId` — unique index. The lookup key, so redemption is one indexed read rather than the
  full-table scan `ApiKeyService.FindMatchingKey` performs.
- `SecretHash` — digest of the high-entropy random secret, compared in constant time. Not a password
  KDF; see ADR-129 for why.
- `ApiKeyId` — foreign key to `ApiKey`, **cascade delete**, mirroring what ADR-004 established for
  `ApiKeyPermission`.
- Migration is additive: one new table, no column changed, no data backfill. Generated with the
  existing `Lighthouse.Backend/Create-Migration.ps1` across both provider assemblies, never
  `dotnet ef migrations add` directly. The expand-only discipline is already guarded by
  `Lighthouse.Backend.Tests/Architecture/ExpandOnlyMigrationGuardTest.cs`, which this migration must
  pass unmodified.

### Single use

Redemption is a **conditional update**, not a read-then-write:

> set `RedeemedAt` where `TokenId` matches **and** `RedeemedAt is null` **and** `RevokedAt is null`
> **and** `ExpiresAt` is in the future — then require exactly one affected row.

The database performs the atomicity. Two replicas racing the same token produce one winner and one
refusal, with no lock, no lease and no coordination service. A read-then-write would pass every test
and lose the race in production exactly when it mattered.

### Expiry

Short and configurable, defaulting to **60 seconds**. The token exists only to survive one navigation
from the Forge resolver's response to the browser's request; anything longer is lifetime bought for no
purpose. The *session* lifetime is a separate, longer setting on the embed cookie
([ADR-130](./adr-130-embed-only-cookie-policy.md)).

### Revocation

Two levers, and one honest limit.

1. **Deleting the API key revokes every token it minted** — cascade delete. This is the lever the
   administrator already understands and already has a UI for; it needs no new concept.
2. **A revoke-all operation scoped to the calling key**, so a Forge app that suspects a leak can
   invalidate outstanding tokens without destroying the key.
3. **The limit, stated rather than hidden**: revoking a token does *not* end an embed session already
   established from it. The cookie is the session. That gap is bounded by ADR-130's non-sliding embed
   cookie lifetime — **30 minutes, settled 2026-08-04** — and by nothing else. An operator who needs
   the session gone now deletes the key and waits out at most half an hour. The number is small
   because it is doing this job, not because short sessions are tidy: a sliding embed cookie would
   make this gap unbounded, which is why ADR-130 turns sliding expiration off.

### Pruning

Expired and redeemed rows are deleted opportunistically on each exchange — bounded work on a path that
is already writing. No new `BackgroundService`. This matches how the codebase already handles
housekeeping: `OrphanedFeatureCleanupService` rides along on `PortfolioUpdater.Update`
(`PortfolioUpdater.cs:110`) rather than owning a timer.

## Alternatives Considered

### A. In-memory store (`IMemoryCache` or a static dictionary) — rejected

Simplest possible, zero schema, and the token's 60-second life makes durability look irrelevant.

**Rejected because it is silently wrong on two supported topologies.** On multiple replicas the token
is redeemable once per replica, which is not single use — and the failure is invisible: the second
redemption *succeeds*, so nothing ever reports an error. On a single replica a restart mid-demo
invalidates outstanding tokens, which is merely annoying, but the multi-replica case is a real
security property quietly not holding. Correctness that depends on a deployment topology nobody
promised is not correctness.

### B. Redis — rejected

The natural store for short-lived single-use tokens, with native TTL and atomic `GETDEL`.

**Rejected because Redis is optional in this product.** ADR-005 already declined a Redis-backed rate
limiter for exactly this reason. Making Redis a hard dependency of a feature would either exclude every
self-hosted single-VM install from the Jira app or fork the implementation by topology. The database
is present in every topology by definition, and the conditional update gives the same atomicity.

### C. Stateless signed token with a redemption denylist — rejected

Sign the token, validate it statelessly, and keep only a small denylist of redeemed identifiers.

Rejected as a distinction without a difference: the denylist *is* the server-side state, with the same
multi-replica and durability requirements, plus signing-key management on top. It also biases toward
longer expiry — the denylist has to be kept for at least the token's lifetime, so a short lifetime is
its own reward and the stateless framing gives none.

### D. Reuse `ApiKey` rows with a short-lived marker — rejected

Mint an ephemeral API key instead of a new entity type.

Rejected because it pollutes a user-facing list. API keys appear in the management UI and are the
administrator's mental model of "credentials I issued"; filling that list with machine-minted
60-second entries makes the one surface where revocation happens unreadable. It would also inherit
PBKDF2-per-row validation on a per-page-load path.

## Consequences

**Positive**

- Single use holds on every supported topology, enforced by the database rather than by a convention.
- Revocation reuses a lever the administrator already has, so the feature adds no new operational
  concept.
- Additive migration, expand-only, guarded by an existing architecture test.
- No new infrastructure dependency — the feature works identically on a single VM and on a
  multi-replica cluster.

**Negative**

- One new table on the auth surface, and one more thing an operator's database contains.
- Two database round-trips on the framed page's critical path (exchange, then redeem). Negligible
  against a page that then loads an entire SPA, but it is a real cost and it is on the user's first
  impression.
- Revocation granularity is the token, not the session, with a worst case of 30 minutes between
  revoking and the session actually ending. Stated above rather than left for someone to discover.
- Opportunistic pruning means a database that stops receiving exchanges keeps its last rows. Harmless
  — they are expired and unredeemable — but it is not zero.

**Quality attribute impact**

- Security: the credential's blast radius is bounded in time (60 seconds), in count (once) and in
  authority (the key's own scope, per ADR-129 and ADR-004).
- Reliability: no dependency on an optional component; a restart loses at most in-flight tokens, which
  the caller re-mints.
- Performance: one indexed lookup and one conditional update per embed load. The `tokenId`/`secret`
  split exists so this stays O(1) rather than inheriting the existing key path's O(n) scan.
