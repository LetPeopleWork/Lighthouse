# Slice 08 — Drill from a blocked-over-time bar into the items blocked that day

**Epic**: 5074 Blocked Items | **Batch**: enhancements (post slices 01-04) | **Job**: `job-flow-coach-drill-into-blocked-trend-point`

## Goal (one sentence)
Clicking a bar in the Blocked-Items-over-time chart opens a `WorkItemsDialog` of the items that were blocked at that date, with membership reconstructed from `WorkItemBlockedTransition` intervals.

### Elevator Pitch
Before: the over-time chart shows how many were blocked, never which ones — a dead end for investigation.
After: click a bar on the Blocked-Items-over-time chart → a `WorkItemsDialog` lists the items blocked at that date.
Decision enabled: escalate/investigate the named blockers behind a bad week, not just the trend line.

### Domain examples
1. Date T; BLK-1 (spell −14d→open) and BLK-2 (−12d→−5d) cover T, OPEN (−20d→−15d) does not → dialog lists BLK-1, BLK-2.
2. Latest bar (today); NOW-1 currently `IsBlocked` → dialog lists NOW-1 (live reconstruction).
3. Date with no covering interval → empty dialog "no items blocked on this date".
4. Reconstructed count for T = 2 and `BlockedCountSnapshot`(T) = 2 → reconcile; on mismatch, capture-gap note.
5. Date before transition capture began → partial set + "complete only from {captureStartDate}" note.

### Outcome KPI
Per feature-delta B1 KPI: reconstructed date-T count reconciles with `BlockedCountSnapshot.blockedCount` within ±1 for ≥95% of sampled dates (else current-only fallback). Job: `job-flow-coach-drill-into-blocked-trend-point` (jobs.yaml).

## IN scope
- Bar click on `BlockedItemsOverTimeChart` (`components/Common/Charts/BlockedItemsOverTimeChart.tsx`) → open the existing `WorkItemsDialog` (`components/Common/WorkItemsDialog/`).
- New **read-only** backend endpoint: items blocked at date T for a Team/Portfolio, `blockedMembershipAtDate`, reconstructed from `WorkItemBlockedTransition` enter/leave intervals (ADR-068) — an item is included when its blocked spell covers T. **No new persisted membership** on `BlockedCountSnapshot`.
- Latest bar (today) reconstructs from live `IsBlocked`.
- Pre-capture-start date: dialog shows the reconstructable set + a note that the record is complete only from the transition-capture-start date.
- Reconciliation guard: the returned count for date T should match `BlockedCountSnapshot.blockedCount` for that date where both exist; a mismatch surfaces a capture-gap note rather than silently diverging.
- Version-gate the new endpoint for CLI/MCP clients per the epic's contract-gate rule (ADR-072 pattern; new read endpoint).

## OUT of scope
- Persisting a per-snapshot item-id set (explicitly rejected — reconstruct instead; keeps `BlockedCountSnapshot` unchanged, no migration).
- Editing/actioning items from the dialog (read-only list, as the dialog is used elsewhere).
- Trend delta (slice 06) and RAG (slice 07).

## Learning hypothesis
- **Disproves if it fails**: that `WorkItemBlockedTransition` intervals can faithfully reconstruct blocked membership at an arbitrary past date — if enter/leave capture has gaps (re-block within one sync cadence, L1 limitation) the reconstructed set diverges from the snapshot count often enough to be untrustworthy.
- **Confirms if it succeeds**: the blocked trend becomes investigable with no new stored membership and no migration.

## Acceptance criteria
1. Given `WorkItemBlockedTransition` history, when the user clicks a bar at date T, then a `WorkItemsDialog` opens listing exactly the items whose blocked interval covers T.
2. Given the latest bar (today) is clicked, the dialog lists the items currently `IsBlocked` (live reconstruction).
3. Given the reconstructed count for T and a `BlockedCountSnapshot` for T both exist, the counts match; when they differ, the dialog shows a capture-gap note.
4. Given a date before transition capture started, the dialog shows the partial set plus a "complete only from {captureStartDate}" note.
5. Given no items were blocked at T, the dialog opens empty with a "no items blocked on this date" message (not an error).
6. E2E (demo data): clicking a bar on the blocked-over-time chart opens the dialog with ≥1 item; POM asserts the dialog and its item rows.

## Dependencies
- `WorkItemBlockedTransition` capture (slice 02) — the interval source reconstruction reads.
- `BlockedCountSnapshot` (slice 03) — used only for the reconciliation guard, not as the membership source.
- `WorkItemsDialog` (existing) — reused for rendering.

## Effort / reference class
~1–1.5 days. Reference class: existing chart-point → `WorkItemsDialog` drill-throughs elsewhere in the app, plus one new read endpoint + interval-overlap query. Backend (endpoint + reconstruct query + NUnit) and FE (click wiring + Vitest) + E2E. Largest of the three — sequenced last.

## Pre-slice SPIKE (recommended, ~2h)
Validate on real/demo transition data that interval-overlap reconstruction at a sampled past date reconciles with `BlockedCountSnapshot.blockedCount` within tolerance. If reconciliation is routinely off (capture gaps dominate), fall back to **current-only** click-through (latest bar only) and mark historical bars non-interactive — de-risks before committing to the reconstruct endpoint.
