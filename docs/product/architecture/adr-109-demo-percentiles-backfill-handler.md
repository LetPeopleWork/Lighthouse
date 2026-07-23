# ADR-109: Demo tenants get a percentile/PBC backfill handler; real tenants stay forward-only

- **Status**: Accepted
- **Date**: 2026-07-23
- **Feature**: epic-5427-percentiles-over-time (ADO Epic 5427)
- **Deciders**: Benjamin Huser-Berta (maintainer)

## Context

Recording is **forward-only** by design (DISCUSS D5, US-02 AC3) — the series starts the day recording begins. On a fresh team every over-time chart therefore reads the honest D6 empty-state until days accrue. That is correct for real tenants, but it means the docs/screenshot E2Es (`@screenshot`) and the demo experience would render *empty* over-time charts — no visible trend, and DoD item 7 (populated demo charts) unmet.

The precedent is exact and already shipped: `DemoBlockedHistoryBackfillHandler` backdates `BlockedCountSnapshot` rows for **demo connections only** (gated on `WorkTrackingSystemConnection.IsDemo`), is idempotent (a backdated snapshot means the owner was already backfilled — checked via `GetAllByPredicate`, not `Exists`), and never runs for real tenants.

## Decision

A new `DemoPercentilesBackfillHandler` mirroring `DemoBlockedHistoryBackfillHandler`:

- **Demo-gated**: runs only for owners on a demo `WorkTrackingSystemConnection`; a real (non-demo) connection is a no-op (verified by an AT, per the blocked-history precedent).
- **Backdates** `PercentilesOverTimeSnapshot` (CT 30/60/90 + WIA) and `ProcessBehaviorSnapshot` (per type) rows across a demo window so the over-time widgets show a populated, trending series in demos and screenshots.
- **Idempotent**: an existing backdated snapshot means the owner was already backfilled — skip (no duplicate/rewrite).
- **Not shipped to real tenants**: real tenants remain forward-only (D5); this handler only fabricates demo data.

## Alternatives considered

- **Empty-state only, no backfill**: ship just the D6 empty-state and let demo/screenshot charts render empty until days accrue. Simpler and one fewer handler, but screenshots then show no trend (the whole point of the feature is invisible in docs), and DoD item 7 is unmet. **Rejected** — the blocked-history precedent already solved this cheaply.
- **Backfill real tenants too**: would fabricate historical percentiles that were never actually measured — dishonest, and a direct violation of the forward-only D5 decision. **Rejected.**

## Consequences

- **Positive**: demo and `@screenshot` E2Es show populated over-time charts (DoD 7 met); real tenants keep an honest forward-only series and the D6 empty-state; reuses a proven, tested demo-backfill pattern.
- **Accepted cost**: a demo-only handler that must be kept in step with the two recording handlers' snapshot shapes — bounded, and covered by the same AT discipline as `DemoBlockedHistoryBackfillHandler`.
- **Reuse verdict**: `DemoBlockedHistoryBackfillHandler` → pattern-reuse → **CREATE NEW handler** (different snapshot tables, same demo-gate + idempotency + backdating idiom).
- Cross-refs [ADR-106](./adr-106-percentiles-over-time-snapshot-table-shape.md) (tables backfilled), [ADR-107](./adr-107-percentiles-recording-handler-on-refresh-events.md) (forward-only recording this complements), [ADR-069](./adr-069-blocked-count-snapshot-and-over-time-endpoint.md) (blocked-history demo-backfill precedent).
