# Slice 01 — `TargetDateAtSnapshot` spine + moving target on the When? view

**Story:** US-01 (ADO #5174) | **job_id:** `job-honest-delivery-trend-when-target-moves` |
**Estimate:** ~0.75 day | **Reference class:** the `EstimatedItemCount` column + dashed estimated
series already shipped in `delivery-metrics` Slice 2 (same column-to-chart path).

## Goal
Forward-record the delivery's target date per snapshot and make the predictability **When?** view
contrast each day's forecast percentiles against the target as it stood that day (a stepped line),
not a single flat line at today's target.

## IN scope
- `DeliveryMetricSnapshot.TargetDateAtSnapshot` (`DateTime?`).
- One EF migration per provider via `Create-Migration.ps1` (Sqlite + Postgres); past rows `null`.
- Recorder: `snapshot.TargetDateAtSnapshot = delivery.Date;` (idempotent same-day overwrite preserved).
- `DeliveryMetricsHistoryPointDto.TargetDateAtSnapshot` populated from the snapshot.
- FE: `DeliveryMetricsHistoryPoint.targetDateAtSnapshot: Date | null` + `asNullableDate` parse.
- When?-view target rendered as a step-after line over `targetDateAtSnapshot`, with a flat-line
  fallback to `history.deliveryDate` when all points are null.
- Boy-Scout cleanup: remove the dead off-axis `ChartsReferenceLine x={deliveryDate}` from
  `DeliveryBurnupChart.tsx` (renders past the data, effectively invisible; user-confirmed).

## OUT of scope
- How Likely? change dots (S2). Burnup (dropped from scope). Fever annotation (dropped).

## Learning hypothesis
Disproves "the daily recorder reliably captures `delivery.Date` and it renders as a clean step" if
the captured series is wrong (e.g. off-by-a-day vs `RecordedAt`) or the stepped line is unreadable in
MUI-X — caught against a real replanned demo delivery before S2/S3 build on the column.

## Acceptance criteria
US-01 AC1–AC4 (feature-delta). Key: stepped reference holds at the old target across the days it
applied and steps on the change day; null-only history falls back to one flat line.

## Dependencies
Shipped `delivery-metrics` store + recorder + metrics-history endpoint + When? view.

## Dogfood moment
Open a seeded delivery whose demo target was moved; confirm the When? target line steps where the
move was recorded.
