# Slice 02 — OAuth tokens stay alive past expiry

**Feature**: work-tracking-oauth-authentication
**ADO stories rolled in**: #4968
**OAuth Health tile**: folded in per OQ-DV1 resolution (2026-05-14)
**Effort estimate**: 1–1.5 days of crafter dispatch (was 0.5–1 day pre-fold; OAuth Health tile adds ~0.5 day for `GET /api/oauth/health` endpoint + the React tile component)
**Reference class**: hosted background service + single-flight mutex + new banner UI + new admin-visible KPI tile

## Goal
A `connector-admin` with a Jira OAuth connection from Slice 01 does **not** have to re-authenticate after the access token expires (typically 1 hour on Jira Cloud). Lighthouse refreshes silently; a failed refresh surfaces a reconnect banner. **The same slice also surfaces the OAuth health KPIs (setup-success rate, refresh-success rate, stale RefreshFailed counts) in an admin-visible tile on the Connections settings page** — the data this slice already produces makes the tile a near-free addition compared to splitting it into its own slice.

## IN scope
- `OAuthCredential.status` field (`Valid` / `RefreshFailed` / `Disconnected`).
- Pre-request expiry check on outbound Jira HTTP calls (refresh if `expiresAt - now < 5 min`).
- Single-flight refresh mutex per `OAuthCredential.Id` (no thundering-herd refresh).
- `OAuthTokenRefreshService` (background, but invoked synchronously from the request path; not a polling timer).
- Connection-list payload gains additive `requiresReconnect: boolean` (per `adr-006-connection-list-payload-shape.md`).
- Frontend: `ReconnectBanner` on connection settings + connections list, with **Reconnect** button that re-initiates `POST /api/oauth/jira/connect`.
- **OAuth Health tile (folded per OQ-DV1)**:
  - New endpoint `GET /api/oauth/health` gated by `[RbacGuard(SystemAdmin)] + [LicenseGuard(RequirePremium = true)]`. Returns `{ setup_success_rate_30d, refresh_success_rate_7d_per_connection, stale_refresh_failed_count_24h, stale_refresh_failed_count_7d }`.
  - New React component `OAuthHealthTile.tsx` on the Connections settings page rendering the three KPIs.
  - **`time_to_first_sync_p95_30d` deferred to a follow-up step** — the `connection.sync.first_after_oauth` event wires later (it requires the OAuth connection sync history, which only exists post-Slice-01 + a few hours of runtime).
- Tests: unit (single-flight, expiry math, KPI aggregation), integration (refresh roundtrip, health endpoint), E2E (forced expiry via test fixture asserting silent refresh; forced refresh failure asserting banner appears; **tile renders KPIs gated by SystemAdmin + Premium** — see Playwright scenario 14).

## OUT scope
- ADO provider — Slice 03 (this slice is Jira-only; ADO inherits refresh "for free" when slice 03 lands).
- Proactive token introspection / revocation.
- Audit events specific to refresh outcomes.
- Per-user reconnect notifications (banner only).

## Learning hypothesis
**Disproves if it fails**: "refresh is operationally silent in real conditions, including when two team-sync jobs race for the same expired credential." The truth test is concurrency under load — the AC #4 single-flight test must pass deterministically.

**Confirms if it succeeds**: "OAuth is operationally cheaper than PAT for the connector-admin." (This is the slice that proves the *business* claim of the epic.)

## Acceptance criteria
See US-02 AC #1–4 in `feature-delta.md`.

## Production-data requirement
The forced-expiry E2E uses an Atlassian sandbox with the access-token lifetime reduced; this is real Atlassian token issuance, not a mock.

## Dogfood moment
Same-day: leave the dev-environment Jira OAuth connection from Slice 01 running for 90 minutes. After the expiry, trigger a manual `Update All`. If the sync succeeds without any UI prompt, the slice is done. If a banner appears, the slice has a bug.

## Dependencies
- Slice 01 landed (`IOAuthProvider`, `JiraOAuthProvider`, persistence).
- The test fixture from Slice 01's stub provider must support "simulate expired token" (small extension).

## Pre-slice SPIKE
None — refresh is a well-trodden path; the only risk (single-flight under concurrency) is addressed by a single test that runs in CI.

## Carpaccio taste tests
- *4+ new components?* One: `OAuthTokenRefreshService`. PASS.
- *Every slice depends on a new abstraction?* No new abstractions; uses the slice-01 `IOAuthProvider.RefreshToken` method.
- *Disproves a pre-commitment?* Yes — disproves the operational-cost claim if refresh is not silent.
- *Synthetic data only?* No — real Atlassian sandbox token expiry.
- *Identical-at-scale to another slice?* No.

**Verdict**: PASS.
