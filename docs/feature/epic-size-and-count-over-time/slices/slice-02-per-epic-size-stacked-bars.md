# Slice 02 — Per-epic size recorded and drawn as stacked bars

**Feature**: epic-size-and-count-over-time · **ADO**: Epic #5585 · **Story**: US-02 · **Estimate**: ~6h
**Reference class**: the `TargetDateAtSnapshot` addition (Epic 3993 follow-up) — same recorder, same
forward-only shape, same "old rows keep working" constraint.

## Goal

Each day's bar shows one segment per epic, sized by that epic's total child items, so a backlog jump is
attributable to a named epic instead of to "scope".

## IN scope

- `Models/Delivery.cs:152-159` — `ToFeatureMetric` already computes `totalItems`; stop discarding it.
  Add `TotalItems` **and** `IsUsingDefaultSize` to `DeliveryFeatureMetric` / `DeliveryFeatureMetricDto`
  (both fields recorded here; only `TotalItems` is *rendered* here — the flag's renderer is slice 03).
- `DeliveryMetricSnapshotRecordingHandler` — no logic change needed beyond the widened record; it already
  serialises `metrics.FeatureBreakdown` wholesale (`:61-63`) and already reads
  `feature.IsUsingDefaultFeatureSize` for the aggregate (`:42`).
- `DeliveryMetricsHistoryDto.ParseFeatureBreakdown` — the two new fields must be **optional** on read so
  pre-slice JSON still deserialises (AC-2.3).
- `DeliveryMetricsHistory.ts` — `FeatureMetric` gains `totalItems: number | null` and
  `isUsingDefaultSize: boolean | null`, parsed with the nullable helpers already in that file (AC-2.4).
- `DeliveryEpicSizeChart.tsx` — stacked bar series per epic, keyed on `referenceId`, composed with the
  slice-01 line on a shared x-axis. Stable colour per epic across days.
- `DemoDataService.BuildFeatureBreakdownJson` (`:195`) — seed both new fields.
- Tests: recorder (NUnit) writes both fields; DTO round-trips old and new shapes; Vitest AC-2.5 … AC-2.7.

## OUT of scope

- Hatching / estimate tooltip (slice 03) — the flag is *recorded* here, never *rendered*.
- Legend filtering (slice 04). Burnup fix (slice 05).
- Any new table or EF migration (D5). If one appears necessary, stop and escalate to DESIGN.
- Backfilling `totalItems` onto existing rows.

## Learning hypothesis

**Disproves** "the breakdown JSON can be extended in place with no migration and no read break" **if**
either parser rejects a 4-field entry, or a pre-slice snapshot 500s the endpoint. Then D5 falls and the
size series needs its own column (and a migration), which changes slice 02's shape and cost.
**Confirms**, if it holds, that slices 03 and 04 are pure-frontend work over data already flowing.

## Acceptance criteria

AC-2.1 … AC-2.7 verbatim from `feature-delta.md`. Load-bearing pair:

- Recorder run over epics of 8 and 3 items → persisted JSON carries those totals (AC-2.1).
- A snapshot written **before** this slice still deserialises and returns with `totalItems` null, and the
  frontend parser does not raise `BoundaryError` (AC-2.3 / AC-2.4).

## Dependencies

Slice 01 (the chart component exists and is already on the tab).

## Dogfood moment

Same day: trigger a portfolio forecast update on the dogfood instance → one new snapshot day → the
chart shows a single day's stacked bar. The series only gets interesting as days accrue (forward-only,
D11 of the parent journey) — that is expected, not a defect.
