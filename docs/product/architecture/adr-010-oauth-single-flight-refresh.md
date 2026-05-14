# ADR-010: OAuth Token Refresh — Pre-Request, Single-Flight, In-Process

**Status**: Accepted
**Date**: 2026-05-14
**Feature**: work-tracking-oauth-authentication
**Decider**: Morgan (Solution Architect) based on DISCUSS D5 + Story 4968

---

## Context

OAuth access tokens expire (commonly 1 hour for Jira Cloud, configurable for Entra ID). To keep work-item syncs working, Lighthouse must refresh the access token before each outbound call when expiry is close. The refresh is a network call to the IdP's token endpoint exchanging the refresh token for a new pair.

Lighthouse runs scheduled syncs (`BackgroundServices`) that can fire multiple concurrent outbound requests against the same connection (different teams sharing one Jira connection). Two concurrent requests observing an expired token would each call the IdP refresh endpoint — a "thundering herd" that wastes calls and, worse, may cause the IdP to invalidate the previously-issued refresh token (some implementations rotate refresh tokens on every refresh and reject the older one).

Three refresh strategies were evaluated.

---

## Decision

**Refresh synchronously, pre-request, single-flight, using an in-process `SemaphoreSlim` keyed on `OAuthCredential.Id`. Refresh fires when `expiresAt - now < 5 minutes`. A failed refresh marks `OAuthCredential.Status = RefreshFailed` and aborts the outbound call.**

Surface:
- `IOAuthService.EnsureFreshTokenAsync(int connectionId, CancellationToken ct)` is the single entry point. Called by `OAuthBearerAuthStrategy` before every outbound request that uses OAuth auth.
- `OAuthService` owns a `ConcurrentDictionary<int, SemaphoreSlim>` keyed on `OAuthCredential.Id`. Acquire-release pattern:
  1. Read the credential (cheap DB read).
  2. If `Status != Valid` → throw `OAuthCredentialNotValidException` (caller surfaces "reconnect required").
  3. If `expiresAt - now > 5 min` → return `accessToken` immediately.
  4. Else → acquire the semaphore (timeout 30s, per OQ-D3).
     - Re-read the credential under the lock (double-check: another thread may have refreshed while we waited).
     - If now-fresh → return.
     - Else → call `IOAuthProvider.RefreshTokenAsync(refreshToken, ct)`, persist new tokens + `expiresAt`, return.
     - On refresh failure (non-2xx, expired refresh token, revoked grant) → set `Status = RefreshFailed`, persist, throw `OAuthRefreshFailedException`.
- The semaphore dictionary entries are never removed; the entry count is bounded by the number of OAuth-authenticated connections in the system (low cardinality, no memory pressure concern).
- Per OQ-D3, semaphore wait exceeding 30s throws `OAuthRefreshTimeoutException` rather than blocking indefinitely.

---

## Consequences

**Positive**

- Slice 02 AC #4 (single-flight under concurrent syncs) passes deterministically: the semaphore guarantees at most one refresh call per credential at a time.
- Refresh is invisible in the happy path: no UI change, no banner, no failed sync (Slice 02 AC #1, dogfood moment).
- `RefreshFailed` status drives the visible "reconnect required" UX via the additive `RequiresReconnect` flag in the connection-list payload (`adr-006-connection-list-payload-shape.md`).
- No background polling timer — refresh is event-driven (request-time). Idle connections do not consume any refresh budget.

**Negative**

- The first request after a long idle period pays the refresh latency (one extra HTTP round-trip to the IdP). Acceptable: refresh is typically <300 ms; the user-visible latency on the immediately-following sync absorbs it.
- In-process locking does not extend to multi-instance deployments. Lighthouse currently runs single-instance per deployment, so this is fine today. If multi-instance is ever needed, the lock moves to the DB row via `SELECT ... FOR UPDATE` on `OAuthCredentials` (or to a `RefreshLeaseExpiresAt` column read-modify-write under an EF Core concurrency token). The change is local to `OAuthService` and does not propagate.

---

## Alternatives considered

### Alternative A — Background polling timer refreshes proactively

A hosted service iterates all OAuth credentials every N minutes and refreshes ones with `expiresAt - now < 10 minutes`.

- Rejected:
  - Idle connections waste refresh budget; high-cardinality deployments would do many unnecessary IdP calls.
  - The race between the proactive refresh and a concurrent sync still exists — would need the same single-flight semaphore anyway.
  - Operational signal lost: a sync that fails due to refresh failure is a user-facing event; a background-timer refresh failure is invisible until the next sync surfaces it. Pre-request refresh ties the failure mode to the visible workflow.

### Alternative B — Optimistic refresh on 401 from the IdP

Send the outbound request with the stored access token; on `401 Unauthorized`, refresh and retry once.

- Rejected:
  - Doubles latency on every expired-token call (one wasted Jira/ADO request).
  - The IdPs do not always return 401 deterministically — some return 403, some return a redirect to a login page. The detection logic per provider would be brittle.
  - Pre-request expiry checking is metadata Lighthouse already has; using it is cheaper and clearer.

### Alternative C — Distributed lock via DB row (`SELECT ... FOR UPDATE`)

Lock the credential row in the database for the duration of the refresh.

- Rejected for v1 because Lighthouse is single-instance per deployment. The in-process semaphore achieves the same invariant without holding a DB row lock for ~300 ms (during which other queries against `OAuthCredentials` would queue).
- Retained as the migration path if multi-instance deployment is ever needed (see "Negative" above).

---

## References

- DISCUSS D5: "Refresh tokens are stored and rotated automatically on a pre-request expiry check"
- Story 4968 ACs (single-flight under concurrency is AC #4)
- OQ-D3 in `feature-delta.md` § Open Questions (30 s timeout)
- Pattern reference: `ConcurrentDictionary<TKey, SemaphoreSlim>` for keyed mutex; standard .NET idiom.
