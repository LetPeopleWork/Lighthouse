# ADR-044: In-app nudge eligibility — FE-derived from existing signals

> **Scope: LIGHTHOUSE repo (`/storage/repos/Lighthouse`) — frontend (React 18) + a thin backend settings read.** Authored for ADO Epic #5124, US-06/US-07.

## Status

**Accepted** — DESIGN wave, 2026-05-31. Seam 5 (the key open decision) was **confirmed by the user on 2026-05-31**: FE-derived eligibility, no new server endpoint (CLI/MCP clients N/A). The rejected Option (b) server endpoint is retained in Alternatives Considered for the record.

## Context

US-06/US-07: a calm, dismissible in-app nudge appears for a **non-premium** instance only when **install age ≥ ~14 days** and **(never shown OR `lastShownAt` older than ~6 months)**, links out to `/survey`, and persists show/dismissal server-side. Hard constraints:

- **Premium exclusion fails CLOSED** — `bothered-premium count` MUST be 0 (guardrail KPI 5); on any uncertainty about license tier, show nothing.
- **Install-age must be UTC-stable / monotonic-safe** — a backward clock jump or timezone skew must never make a not-yet-eligible instance fire early (journey `step-install-and-use` failure mode).
- The authoritative `installTimestamp`/`lastShownAt` live **server-side** (ADR-045).
- The premium signal is the **existing** `canUsePremiumFeatures` license-tier capability (NOT RBAC / `IRbacAdministrationService` — DISCUSS cross-cutting checklist). FE gating uses the existing license/premium hook adjacent to `useRbac()`.

The decision: where is eligibility *evaluated* — frontend (reading existing signals) or a new backend endpoint?

## Decision

**FE-derived eligibility (Option a), RECOMMENDED.** The frontend computes "show the nudge?" from three existing/near-existing inputs, with NO new eligibility endpoint:

1. **`isPremium`** — read from the existing license/premium signal (`canUsePremiumFeatures`) the FE already consumes for premium gating.
2. **`installTimestamp`** — read from the server via the per-instance settings read path (ADR-045). Authoritative, UTC, written once on first run.
3. **`lastShownAt`** — read from the same settings path; written on show/dismiss.

Eligibility (pure function, exhaustively testable):

```
showNudge =
  isPremium === false                                   // premium fails CLOSED
  && installTimestampKnown                              // unknown ⇒ NOT eligible
  && (nowUtc - installTimestampUtc) >= 14 days          // UTC-stable comparison
  && (lastShownAt === null || (nowUtc - lastShownAtUtc) >= ~6 months)
```

- **Premium is evaluated FIRST and is absolute** — no install-age/cadence branch can override it (journey `step-eligibility-evaluated` integration checkpoint).
- **Fail CLOSED everywhere**: if the premium read is uncertain/errors, or the settings read fails, or `installTimestamp` is unknown/unparseable → `showNudge = false`. Guessing YES risks both nagging and surveying a premium user; the default is always "do not show."
- **UTC-stable / monotonic-safe**: all comparisons are on server-supplied UTC instants; the FE never compares against local wall-clock that can move. A negative or backward-jumping delta is treated as "not yet eligible," never as "fire now" (clamp to not-eligible on any anomaly).
- **No new server endpoint** ⇒ per the CLAUDE.md CLI/MCP checklist, **the Lighthouse CLI and MCP clients are UNAFFECTED (N/A)** — no `FEATURE_REQUIRES_SERVER_NEWER_THAN` version-gate is triggered. The only server touch is the settings read/write (ADR-045), which extends the existing per-instance settings mechanism rather than adding a feature endpoint.

The guardrail (premium → never render) is enforced by a **deterministic test**, not telemetry alone (DoD item 2): a premium instance never renders the nudge at any install age. The install-age boundary (just-under-14d → no; at/over → yes) and the UTC-skew case are unit-tested on the pure eligibility function.

## Alternatives Considered

- **(b) New backend endpoint `GET /api/.../survey-nudge/eligibility` evaluating server-side**: rejected as the recommendation, viable as a fallback. Pros: eligibility is non-spoofable and computed where the authoritative dates live; one place to change the cadence. Cons: it adds a NEW server endpoint, which **triggers the clients-repo version-gate rule** — an older Lighthouse server returns an opaque 404, so the CLI/MCP wrapping methods would need a `FEATURE_REQUIRES_SERVER_NEWER_THAN` pre-check and an "upgrade Lighthouse" failure. That is real cross-repo cost for a *frontend-only* nudge that no CLI/MCP client consumes. The spoofing concern is immaterial here: the nudge is a low-stakes UI invitation, not an authorization decision — a user spoofing themselves *into* a feedback nudge harms no one, and the premium-exclusion guardrail (the one thing that matters) is enforced by reading the same authoritative premium signal the FE already trusts plus a deterministic test. DISCUSS's default expectation was explicitly "NO new endpoint (FE-derived)."
- **(c) Pure client-side dates (localStorage install timestamp)**: rejected — dates would be spoofable AND non-authoritative AND reset on browser-storage clear, so the ~2-week and ~6-month guarantees would not hold across sessions/machines. The authoritative dates MUST live server-side (journey `step-eligibility-evaluated` failure mode "eligibility computed purely client-side and spoofable").

## Consequences

- **Positive**: NO new server endpoint ⇒ CLI/MCP unaffected (N/A); eligibility is a pure, exhaustively-testable function; premium-fails-closed and UTC-stability are explicit, test-anchored properties; the authoritative dates still live server-side (ADR-045) so the guarantees hold across sessions.
- **Negative**: eligibility logic runs in the browser, so it is technically inspectable/spoofable by the user — accepted, because (a) the premium-exclusion guardrail does not depend on the FE being honest (it reads the trusted premium signal and is test-enforced), and (b) the only thing a user could spoof is showing *themselves* a feedback invitation. If a future requirement makes server-authoritative eligibility necessary, promoting to Option (b) is a contained change behind the same FE eligibility seam (plus the clients version-gate work).
- **DECISION FLAG**: this ADR records Option (a). **Pending user confirmation** before DISTILL (per task process). If the user prefers server-authoritative evaluation, switch to Option (b) and add the clients-repo version-gate work to the DEVOPS handoff.
