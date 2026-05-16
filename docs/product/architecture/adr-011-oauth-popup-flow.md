# ADR-011: OAuth Reconnect via Popup Window with Same-Origin postMessage Handshake

**Status**: Accepted (2026-05-16 — Option A locked in by product owner; Option B remains pre-approved fallback if a Safari Webkit gold-test in DELIVER reveals an issue with `window.opener` access on the same-origin landing page)
**Date**: 2026-05-16
**Feature**: work-tracking-oauth-authentication (Story #5018 — popup reconnect)
**Decider**: Morgan (Solution Architect), interaction mode PROPOSE — final selection by user

---

## Context

Story #5018 surfaced two UX defects in the OAuth reconnect flow as it shipped in slice 02:

1. Clicking *Reconnect* on the orange banner inside a connection's edit dialog navigates the WHOLE page to the IdP's consent screen. The user is then dropped back on the *create*-connection screen (`/connections/new?oauth=success&connectionId=...`) rather than the *edit* dialog they started in. They never see the live `Disconnected → Connected` flip.
2. Because the full-page redirect destroys in-memory wizard state, the backend persists half-baked rows (defensive upsert in `OAuthService.UpsertValidCredential` + `GroupBy/OrderByDescending` defensive read in `WorkTrackingSystemConnectionsController`) so the round trip can survive the navigation.

The story asks: replace the full-page redirect with a popup window; if the popup eliminates the need for half-baked state, simplify the data model.

Constraints that frame the decision:

- **Safari ITP / cross-site cookie restrictions** — Atlassian (`auth.atlassian.com`) and Microsoft (`login.microsoftonline.com`) consent screens live on third-party origins. Any popup→opener handshake that depends on `window.opener` being readable from the third-party origin can be severed by Safari ITP. The handshake must therefore happen *after* the IdP round trip is complete, on a Lighthouse-owned (same-origin) landing page.
- **Playwright testability** — the chosen mechanism must be testable without a real IdP. The existing `StubOAuthProvider` simulates the IdP; the popup mechanism must be deterministic against it.
- **No server-side session store** — DDD-8 + ADR-007 explicitly avoid one (HMAC state token replaces it). Any mechanism that requires the backend to track popup-completion state would re-introduce a session store via the back door.
- **No regressions to existing ADRs** — ADR-007 (provider registry), ADR-008 (credential separation), ADR-009 (BaseUrl as source of truth for `redirect_uri`), ADR-010 (single-flight refresh) must compose, not be replaced.

---

## Decision (proposed — three options for selection)

The popup is opened by the opener on the same origin as Lighthouse; the popup is navigated by the IdP to its consent screen; on consent the IdP 302s the popup back to the existing `/api/oauth/callback` route on the Lighthouse backend; the backend exchanges the code, persists the credential (unchanged from slice 02), and 302s the popup to a same-origin **OAuth landing page** (`/oauth/popup-complete`). The landing page is where the popup→opener handshake happens — entirely on the opener's own origin, *after* the IdP round trip.

Three mechanisms for the handshake are presented below. All three share the architecture above; they differ only in **how the landing page tells the opener "you're done."**

### Option A — `window.postMessage` from the landing page to `window.opener` *(recommended)*

The landing page calls `window.opener?.postMessage({ type: "oauth.complete", status, connectionId, reason }, BaseUrl)` and then `window.close()`. The opener subscribes via a `useOAuthPopup` hook that filters incoming messages by `event.origin === window.location.origin` AND `event.data?.type === "oauth.complete"`.

**Browser-compat notes:**
- `postMessage` works in every browser Lighthouse targets — IE11 included (though we don't target IE).
- Safari ITP does NOT sever `window.opener` for same-origin landings; ITP's concern is third-party tracking, not same-origin popup handshakes. The handshake happens on the Lighthouse origin, so ITP is a non-issue *for the message itself*. ITP can still sever `window.opener` during the third-party IdP round trip on Safari < 16 in rare cases — handled by the opener-side 90s timeout (DDD-15) and a Playwright Webkit gold-test (OQ-5018-3).
- Popup-blocker compatibility: standard. The popup is opened *synchronously inside the user's click handler*; no browser blocks user-initiated popups in that path.
- Third-party cookies: not relevant — the IdP's session cookies are first-party from its own perspective; Lighthouse never reads them.

**Testability (Playwright + unit):**
- Vitest: hook tested with mocked `window.open` returning a stub popup + `MessageEvent` dispatch. Wrong-origin filter, wrong-type filter, popup-blocked, cancelled-by-close all asserted.
- Playwright: existing `StubOAuthProvider` already drives the `/api/oauth/callback` round trip end-to-end. With the popup, Playwright's `context.waitForEvent("page")` captures the popup; the test asserts the opener's status badge flips after the popup closes. No new fixture infrastructure; existing `OAuthConnection.spec.ts` flow generalises.

**Composition with existing ADRs:**
- ADR-007 (provider registry): no change — providers are unaware of the transport.
- ADR-008 (credential separation): no change at the entity level. **DDD-13 adds a DB-level UNIQUE index** on `OAuthCredential.WorkTrackingSystemConnectionId` as a co-traveller (the relationship is already 1:1 in ADR-008; the index makes it enforced).
- ADR-009 (BaseUrl): **strengthened.** BaseUrl is now also the `targetOrigin` for the `postMessage` call. Mis-configured BaseUrl would cause messages to be silently dropped — same failure mode as today's redirect mismatch, with the same warning.
- ADR-010 (single-flight refresh): no change.

**Permits dropping persisted intermediate state?**
- Partial. The `OAuthCredential` upsert defence in `OAuthService` STAYS (retry-after-error semantics). The `GroupBy/OrderByDescending` defensive read in `WorkTrackingSystemConnectionsController` is simplified to `ToDictionary` after DDD-13 adds the UNIQUE index. The `?oauth=success&connectionId=` resume contract is deleted. **Net simplification, not "everything can go."**

### Option B — `BroadcastChannel`

The landing page calls `new BroadcastChannel("oauth-complete").postMessage({ status, connectionId, reason })` and then `window.close()`. The opener subscribes to the same channel.

**Browser-compat notes:**
- Supported in Chrome 54+, Firefox 38+, Safari 15.4+ (Mar 2022), Edge 79+. iOS Safari 15.4+.
- The 15.4+ Safari floor is the only meaningful gap; users on iOS Safari < 15.4 would silently never receive the message. Today's Lighthouse `browserslist` does not explicitly support iOS Safari < 15.4, but verifying this in DELIVER is required.
- Survives Safari ITP even more cleanly than `postMessage` (no `window.opener` dependency at all).
- Survives popups opened with `rel="noopener"` (which would break Option A). Lighthouse does not open popups with `noopener` — opener is required by Option A — but Option B is robust to a future change.

**Testability (Playwright + unit):**
- Vitest: `BroadcastChannel` has a polyfill for jsdom but Vitest's environment may not include it by default; needs verification. Otherwise the test shape is identical to Option A.
- Playwright: identical to Option A; the existing `StubOAuthProvider` drives the round trip.

**Composition with existing ADRs:**
- Same as Option A. `targetOrigin` enforcement disappears (BroadcastChannel doesn't have it) — replaced by the channel-name convention, which is per-origin-isolated by spec. This is **strictly equivalent security** to Option A's `targetOrigin`, not weaker, because BroadcastChannel is origin-scoped at the browser level.

**Permits dropping persisted intermediate state?** Identical to Option A.

**Why not the default choice:** `postMessage` is the established OAuth popup pattern (used by Auth0, Okta JS SDK, Google Identity Services, Microsoft MSAL.js). Engineers landing on Lighthouse will recognise the pattern immediately. BroadcastChannel works, is slightly more robust, but is a less-recognised idiom; the recognisability cost is paid every code review. Recommended as the **automatic fallback** if Option A's gold-test against Safari Webkit (OQ-5018-3) shows ITP severs `window.opener` for our specific landing-page sequence.

### Option C — `window.opener.location` direct write (rejected, listed for completeness)

The landing page directly sets `window.opener.location` to a "refresh and notify" URL, then `window.close()`. The opener observes the navigation and refetches.

**Rejected:**
- Triggers a full navigation on the opener — exactly the failure mode the story is fixing.
- Same-origin policy permits it, but it requires the opener to have URL-driven re-fetch logic that doesn't exist in `ModifyConnectionSettings` or `OverviewDashboard` today. We'd have to add it just to support this option.
- No advantage over A or B; strictly worse on observability.

### Option D — Server-side completion polling (rejected, listed for completeness)

The popup writes a "completion record" to the backend on success; the opener polls `GET /api/oauth/completion/{stateToken}` every 1s.

**Rejected:**
- Re-introduces server-side session-equivalent state. Violates DDD-8's "no session store" intent.
- Adds latency (polling interval) and a new endpoint with new auth questions (the opener is authenticated; the popup-side write is from the callback handler which is `[AllowAnonymous]` per ADR-007 — coupling the two requires care).
- Solves a problem the postMessage / BroadcastChannel options don't have.
- Defended-against in DDD-8 + ADR-007's rejection of the "session store" alternative.

---

## Recommended decision

**Option A — `window.postMessage` from the landing page to `window.opener`, with origin + message-type filtering on the opener side.**

Composes cleanly with all existing ADRs; uses the canonical OAuth-popup idiom; survives Safari ITP because the handshake is opener-origin, not IdP-origin; no new architectural concepts. **Option B is the pre-approved fallback** if a Webkit gold-test in DELIVER (OQ-5018-3) shows the landing-page `window.opener` is null on our oldest supported Safari — the swap is local to `useOAuthPopup` and `OAuthPopupComplete`.

---

## Consequences

**Positive**

- Edit dialog never unmounts; status badge flips live; user observes the recovery they triggered.
- Three call sites (`ReconnectBanner`, `OAuthAuthForm`, `CreateConnectionWizard`) collapse to one shared hook.
- `?oauth=success&connectionId=` resume contract is deleted — a brittle URL-driven state machine goes away.
- `OAuthCredential` 1:1 relationship is enforced at the DB level (DDD-13), not just by C# convention.
- Net frontend code reduction: roughly +110 LOC for the hook + landing page, −60 LOC for the deleted resume branch and three substituted handlers. Effective delta: small.

**Negative**

- Adds a popup-blocker failure mode the existing flow doesn't have. Mitigated by detecting `window.open` returning null and surfacing an inline error.
- Adds a new same-origin route (`/oauth/popup-complete`) that is part of the OAuth public-ish surface (the IdP redirects to `/api/oauth/callback`, which 302s to the landing page). Security exposure: zero — the landing page reads only query string, holds no secrets, and is subject to the same opener-side origin filter as any other postMessage handshake.
- Safari ITP behaviour on the landing page must be empirically verified in DELIVER (gold-test). If the test fails, Option B is the pre-approved swap; the change is local.

---

## Alternatives considered

See Options B, C, D above. Summary verdicts:

| Option | Verdict | Rationale |
|---|---|---|
| A — `window.postMessage` | **Recommended** | Canonical OAuth-popup idiom; composes cleanly; testable. |
| B — `BroadcastChannel` | Pre-approved fallback | Slightly more robust to popup `noopener`; less-recognised idiom; iOS Safari 15.4+ floor needs verification. |
| C — `window.opener.location` direct write | **Rejected** | Triggers the navigation we're trying to eliminate. |
| D — Server-side completion polling | **Rejected** | Reintroduces session-equivalent state; ADR-007's "no session store" intent. |

---

## Earned Trust (probes for the popup substrate)

Each new substrate dependency has an explicit probe:

| Dependency | Lie scenario | Probe (Vitest unless noted) |
|---|---|---|
| `window.open` returns a usable popup handle | Popup blocker returns null silently | `useOAuthPopup` checks for null → surfaces `popup_blocked`. Unit-tested. |
| Popup actually closes after consent | User closes manually mid-flow; IdP error page | `setInterval(() => popup.closed, 500)` with a 90s guard → surfaces `cancelled`. Unit-tested. |
| `window.opener` retained across same-origin landing | Safari ITP severs reference | `OAuthPopupComplete` falls back to "you may close this window" if `window.opener` is null; opener-side timeout surfaces `popup_blocked`. **Playwright Webkit gold-test required in DELIVER (OQ-5018-3).** |
| `postMessage` target-origin enforcement | Wrong `targetOrigin` leaks to malicious frame | Landing page uses BaseUrl as `targetOrigin`; opener filters `event.origin === window.location.origin`. Wrong-origin and wrong-type messages dropped. Unit-tested. |

If the Webkit gold-test fails, Option B (BroadcastChannel) is the pre-approved swap — change is local to the hook + landing page.

---

## References

- ADR-007 — OAuth Provider Registry (no change)
- ADR-008 — OAuth Credential Separation (DDD-13 adds UNIQUE index as co-traveller)
- ADR-009 — OAuth BaseUrl Callback (BaseUrl strengthened to `targetOrigin` for postMessage)
- ADR-010 — OAuth Single-Flight Refresh (no change)
- Story #5018 — *I can reconnect OAuth in a popup without leaving the connection edit dialog*
- Slice 02 commit `ac74fa3` — defensive upsert + `GroupBy` introduced
- `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/OAuth/OAuthService.cs:97-146` — `CompleteAsync` / `UpsertValidCredential`
- `Lighthouse.Backend/Lighthouse.Backend/API/WorkTrackingSystemConnectionsController.cs:54-72` — defensive `GroupBy` read
- `Lighthouse.Frontend/src/components/Common/Connection/CreateConnectionWizard.tsx:85,163,291,306,347,404` — `draftConnectionId` + `?oauth=success` resume
- `Lighthouse.Frontend/src/components/Common/Connections/ReconnectBanner.tsx:29-43` — current full-page redirect
- `Lighthouse.Frontend/src/components/Common/Connections/OAuthAuthForm.tsx:30-42` — current full-page redirect
