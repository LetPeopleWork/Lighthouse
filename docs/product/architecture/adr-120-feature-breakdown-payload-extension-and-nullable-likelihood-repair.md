# ADR-120: The per-epic breakdown payload is extended in place — and its nullable-likelihood mismatch is repaired at the same time

- **Status**: Accepted (2026-07-31, DESIGN wave for ADO #5585 / Story #5615). Interaction mode = **propose**.
- **Date**: 2026-07-31
- **Feature**: `epic-size-and-count-over-time` (Epic 5585, slice 02)
- **Extends**: ADR-048 (one snapshot store), ADR-049 (date-keyed idempotent recorder), ADR-050 (metrics-history endpoint), ADR-112 (unknown forecast when a contributor cannot be forecast)

## Context

Slice 02 needs two more facts per epic per day: the epic's **total child items** and whether that size is
the **portfolio default** rather than a real breakdown. Both already exist at record time —
`Delivery.ToFeatureMetric` (`Delivery.cs:152-159`) computes `totalItems` and discards it, and
`DeliveryMetricSnapshotRecordingHandler` already reads `feature.IsUsingDefaultFeatureSize`
(`:42`) for the aggregate `EstimatedItemCount`.

The per-epic breakdown is persisted as a JSON blob (`DeliveryMetricSnapshot.FeatureBreakdownJson`),
serialised from the domain record `DeliveryFeatureMetric` and deserialised into the API record
`DeliveryFeatureMetricDto`.

## Decision

**1. Extend both records in place; no new table, no EF migration.**

- `DeliveryFeatureMetric(string ReferenceId, string Name, double Completion, double? Likelihood)`
  gains `int TotalItems, bool IsUsingDefaultSize` — the domain always knows both, so neither is nullable.
- `DeliveryFeatureMetricDto(string ReferenceId, string Name, double Completion, double Likelihood)`
  gains `int? TotalItems, bool? IsUsingDefaultSize` — **nullable on the wire**, because a snapshot
  recorded before this slice legitimately has neither. Absence means "this day predates the field", and
  a null `IsUsingDefaultSize` renders solid, never hatched.

The blast radius is bounded: three production call sites (`Delivery.cs:158`, the recorder's serialise at
`DeliveryMetricSnapshotRecordingHandler.cs:61-63`, `DeliveryMetricsHistoryDto.ParseFeatureBreakdown`) and
three test sites. Per the shared-contract rule, the test factory is extended before the record.

**2. Repair the nullable-likelihood mismatch in the same change.**

The domain record's `Likelihood` is `double?` — ADR-112 made an un-forecastable feature report *unknown*
rather than a number, and `Feature.GetLikelhoodForDate` returns `null` for it (`Feature.cs:114-122`). The
recorder serialises the domain record verbatim, so `"Likelihood": null` reaches the stored JSON. But
`DeliveryFeatureMetricDto.Likelihood` is a **non-nullable** `double`, and
`DeliveryMetricsHistoryDto.ParseFeatureBreakdown` deserialises straight into it
(`DeliveryMetricsHistoryDto.cs:67-75`). System.Text.Json throws `JsonException` on `null` → `double`,
which surfaces as a 500 from `GET .../deliveries/{id}/metrics-history` — for the **whole** delivery, not
just the affected epic. The frontend mirrors the defect: `parseFeatureBreakdown` reads
`likelihood: asNumber(...)` and would raise `BoundaryError` (`DeliveryMetricsHistory.ts:93-110`).

This is **pre-existing** — not introduced by 5585 — and it is not covered by tests:
`DeliveryMetricsHistoryDtoTest` exercises a null *snapshot-level* `LikelihoodPercentage` (`:45`) but never
a null *per-epic* likelihood. It is repaired here because slice 02 rewrites exactly this serialisation
path, and shipping a widened payload through a path that can throw on existing data would be negligent.
`Likelihood` becomes `double?` on the DTO and `number | null` on the frontend model, with one round-trip
test proving a null survives record → store → endpoint → parse.

A Bug work item is filed for traceability even though the fix rides along with Story #5615.

## Alternatives considered

- **A dedicated `DeliveryEpicSizeSnapshot` table** — rejected: same `(delivery, day)` grain as the
  existing store, so it would duplicate ADR-048's single-source-of-truth for no gain and cost an EF
  migration across both providers.
- **A second JSON column** — rejected for the same reason; the payload already describes exactly this
  per-epic-per-day shape.
- **Backfilling `totalItems` onto existing rows from today's `Feature` state** — rejected: it would
  attribute today's size to a past day, which is precisely the dishonesty the forward-only store exists
  to avoid (parent journey D11).
- **Fixing the nullable likelihood as a separate detached bug** — rejected: it lives in the lines slice 02
  edits, and leaving a known 500 in the endpoint the same slice widens is not a defensible sequencing.

## Consequences

- Days recorded before slice 02 render the count line only, no bars — the honest forward-only state
  (parent journey D6/D11), visible in the chart's empty-ish early window.
- Both parsers must treat the new fields as optional; a strict parser on either side turns old rows into
  a hard failure. This is the slice's stated learning hypothesis.
- The likelihood repair changes an API field's nullability from non-null to nullable. That is additive
  for consumers that already tolerate `null` and a genuine contract widening for those that do not —
  no client reads this field today (verified: `lighthouse-clients` has no `metrics-history` surface at
  all until Story #5619), so the window to fix it cheaply is now.
- `DemoDataService.BuildFeatureBreakdownJson` must seed both new fields, including at least one epic that
  flips `isUsingDefaultSize` mid-window, or the hatch (ADR-119) has nothing to render in the demo
  instance or the docs screenshot.
