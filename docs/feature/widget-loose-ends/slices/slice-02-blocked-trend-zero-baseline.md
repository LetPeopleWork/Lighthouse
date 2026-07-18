# Slice 02 — Blocked Items trend: absent baseline counts as zero

**Story**: 5508 Cleanup Widget lose Ends | **Group**: B (trend fix) | **Job**: `job-delivery-lead-tell-blocked-trend-vs-last-period`

## Goal (one sentence)
Make the Blocked Items trend render a real direction on instances that have no snapshot history yet, by treating an absent previous-period baseline as a blocked count of zero instead of returning the neutral no-baseline marker.

### Elevator Pitch
Before: on a young instance the Blocked widget's trend shows "—" with a "waiting for baseline" hint, which reads as broken.
After: open **Team → Metrics → Flow Overview** → the Blocked Items widget shows an arrow and the delta against the blocked count at `startDate − 1 day`, counting a missing snapshot as 0.
Decision enabled: judge on day one whether blocking is growing or shrinking this period, without waiting a full period for the record to fill.

### Domain examples (range 01–14 July, boundary = 30 June)
1. Snapshot on 30 June = 3, current = 5 → `up`, `+66.7%`.
2. Snapshot on 28 June = 5 (latest at-or-before boundary), current = 2 → `down`.
3. No snapshot at or before 30 June, current = 4 → baseline 0, `up`, no percentage (division guard).
4. No snapshot at or before 30 June, current = 0 → baseline 0, `flat`, no arrow implying change.
5. Empty history entirely, current = 0 → `flat`.

### Outcome KPI
Blocked trend availability: a direction renders on 100% of instances with ≥1 blocked count, including day-one instances (Vitest on `computeBlockedTrend` empty-history and both-zero paths).

## IN scope
- `blockedTrend.ts` — replace the two `noBaselineTrend()` returns that fire on missing history / missing boundary snapshot with a zero baseline (`blockedCount: 0`), keeping the boundary at `startDate − ONE_DAY_MS` exactly as today.
- Preserve the existing `formatDelta` guard: baseline 0 → omit `percentageDelta`, still emit direction and the absolute current/previous values.
- Preserve the `current` lookup (`latestAtOrBefore(history, endDate)`); if there is no current snapshot either, current is 0 → `flat`.
- Choose a `previousLabel` for the synthetic baseline that does not fabricate a snapshot date — state the boundary date, not a recorded-at that never existed.
- Vitest coverage for all five domain examples.

## OUT of scope
- Removing the `noBaseline` field from `TrendPayload` — other widgets may still use it.
- Backfilling `BlockedCountSnapshot` history.
- Changing the boundary semantics (locked D2: boundary stays `startDate − 1 day`).
- The Blocked widget's RAG (already shipped, slice-07 of epic 5074) or its chart drill-through.

## Learning hypothesis
- **Disproves if it fails**: that the reported "trend doesn't seem to work" is the `noBaseline` path. If the trend still renders blank after this change, the real fault is upstream — `blockedCountHistory` not loading, or snapshots not being recorded at all — and the fix moves to the data path.
- **Confirms if it succeeds**: the trend chrome and wiring are sound and only the missing-baseline policy was hiding it.

## Acceptance criteria
1. Given a snapshot exists at or before `startDate − 1 day`, the trend compares against it — behaviour unchanged from today.
2. Given no snapshot exists at or before `startDate − 1 day`, the baseline is 0 and a current count of N renders direction `up`.
3. Given no snapshot exists and the current count is 0, the trend renders `flat`.
4. Given a baseline of 0, `percentageDelta` is omitted; direction and absolute values still render.
5. The tooltip does not present a fabricated snapshot date for the synthetic zero baseline.
6. Holds at Portfolio scope as well as Team scope.
7. E2E (demo data): a demo team with blocked items shows a directional arrow on the Blocked widget, not the neutral placeholder.

## Dependencies
None. `blockedCountHistory` is already loaded in `BaseMetricsView` and threaded into `computeBlockedTrend` (`BaseMetricsView.tsx:1611`).

## Effort / reference class
~0.5 day. Reference class: slice-06 of epic 5074, which introduced `computeBlockedTrend` — this is a policy change inside the same pure selector. Frontend + Vitest only.

## Pre-slice SPIKE
None. First action in the slice is a diagnostic: confirm on a real instance that `blockedCountHistory` is non-empty and that the `noBaseline` branch is what renders. If history is empty for a different reason, the learning hypothesis has already fired and the slice re-scopes to the data path before any code changes.
