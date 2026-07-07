# Slice 07 — Max-blocked-age RAG status on the Blocked overview widget

**Epic**: 5074 Blocked Items | **Batch**: enhancements (post slices 01-04) | **Job**: `job-flow-coach-read-blocked-health-at-a-glance`

## Goal (one sentence)
Give the Blocked overview widget a red/amber/green status driven by the maximum blocked age across the currently-blocked items, calibrated on the existing `blockedStalenessThresholdDays`.

### Elevator Pitch
Before: the coach can't tell whether "5 blocked" is all fresh or one stuck three weeks without opening each item.
After: open a team metrics page → the Blocked widget is RED/AMBER/GREEN with a tooltip "oldest blocker: N days".
Decision enabled: dig into blockers this session when red; move on when green.

### Domain examples (threshold = 10 days)
1. Oldest blocker 12d → RED (past threshold), tooltip "oldest blocker: 12 days".
2. Oldest blocker 8d (≥75% aging band) → AMBER.
3. Oldest blocker 2d → GREEN.
4. `blockedStalenessThresholdDays` = 0 → neutral (RAG disabled).
5. Blocked item still establishing `blockedSince` baseline → excluded from max-age (doesn't force a colour).

### Outcome KPI
Per feature-delta B2 KPI: widget RED fires within one sync of an item crossing the threshold (parity with blocked→stale). Job: `job-flow-coach-read-blocked-health-at-a-glance` (jobs.yaml).

## IN scope
- RAG indicator on `BlockedOverviewWidget`: RED = an item is blocked past `blockedStalenessThresholdDays`, AMBER = an item is aging toward it, GREEN = nothing aging.
- Drive from `ctx.blockedItems` (already passed to the widget site in `BaseMetricsView`) using each item's `blockedSince` (shipped slice 02) to compute max blocked age; threshold from `ctx.blockedStalenessThresholdDays` (shipped slice 04).
- AMBER band definition = a bounded fraction of the threshold (e.g. ≥ X% of `blockedStalenessThresholdDays`); exact fraction is a DESIGN decision, defaulted and documented, not a new user setting.
- Threshold `0` ⇒ RAG disabled (neutral, no colour) — matches the blocked→stale opt-out.
- Items still establishing a `blockedSince` baseline ("—") are excluded from the max-age computation.
- Accessible: colour is not the only signal (label/tooltip states "oldest blocker: N days"), consistent with the app's stale treatment.

## OUT of scope
- Any new backend endpoint or setting (reuses `blockedStalenessThresholdDays` + `blockedSince`).
- A separate RAG threshold distinct from `blockedStalenessThresholdDays` (deliberately reuse the one line the admin already tuned).
- Trend delta (slice 06) and chart drill-through (slice 08).

## Learning hypothesis
- **Disproves if it fails**: that max-blocked-age against the existing blocked-staleness line is the right urgency signal — if RAG goes red at the same moment blocked→stale already fires, coaches may find it redundant rather than an earlier glance.
- **Confirms if it succeeds**: a colour on the widget replaces opening each blocked item to judge urgency.

## Acceptance criteria
1. Given a blocked item whose blocked age exceeds `blockedStalenessThresholdDays`, when the metrics view loads, then the Blocked widget renders RED with a tooltip naming the oldest blocker's age.
2. Given the oldest blocked item is within the AMBER band (aging toward but not past the threshold), the widget renders AMBER; given all blocked items are well within, GREEN.
3. Given `blockedStalenessThresholdDays = 0`, the widget renders neutral (no RAG colour).
4. Given a blocked item with no established `blockedSince` baseline, it is excluded from the max-age computation (does not force a colour).
5. E2E (demo data): a team whose demo blocked items cross the threshold shows a RED Blocked widget; POM asserts the status/colour attribute (not a raw pixel).

## Dependencies
- `blockedSince` per-item capture (slice 02) and `blockedStalenessThresholdDays` setting (slice 04) — both shipped and already threaded into `BaseMetricsView`.

## Effort / reference class
~0.5–1 day. Reference class: the time-in-state stale red treatment (same threshold-driven colour language). Primarily FE + Vitest; verify `ctx.blockedItems` carries `blockedSince` to the widget site (thread the prop if not).

## Pre-slice SPIKE
None required. Watch-item at DESIGN: confirm `blockedSince` is present on the `blockedItems` handed to the `blockedOverview` widget site; if only `blockedItems.length` is currently used, a thin prop-threading is part of the slice.
