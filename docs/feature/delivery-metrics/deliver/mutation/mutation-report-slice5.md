# Mutation Report — Slice 5 (US-05 Fever Chart, STRETCH)

Feature: delivery-metrics (Epic 3993) · Slice 05-fever-chart-stretch · 2026-06-04
Strategy: `per-feature` (CLAUDE.md) · Gate: ≥80% kill on new code.

The fever chart was redesigned twice under review to match the canonical
LetPeopleWork "Feature Progress" chart: (1) axes realigned to **completion rate**
(x) vs **chance of being late** = `100 − likelihood` (y) with diagonal green/amber/red
bands; (2) grain changed to **one bubble per feature** with a Run animation that moves
each feature first→latest. This added a backend slice (per-feature breakdown
recording), so mutation now covers BOTH stacks.

## Result — PASS (both stacks)

| Stack | Config | Score | Killed | Survived | Verdict |
|---|---|---|---|---|---|
| Backend | `Lighthouse.Backend.Tests/stryker-config.delivery-metrics-slice5.json` | **86.84%** | 33 | 5 | PASS — survivors pre-existing/equivalent |
| Frontend | `stryker.config.delivery-metrics-slice5.mjs` | **89.15%** | 121 | 14 | PASS — survivors presentational/equivalent |

## Backend scope

Mutated the per-feature additions:
- `Models/Delivery.cs{50..105}` — `CalculateMetrics` + `CalculateFeatureBreakdown` (per-feature completion + likelihood projection).
- `Services/Implementation/DomainEvents/DeliveryMetricSnapshotRecordingHandler.cs` — `FeatureBreakdownJson` serialization (+ the empty→null branch).
- `API/DTO/DeliveryMetricsHistoryDto.cs` — `ParseFeatureBreakdown` (case-insensitive deserialize, null/empty → `[]`).

Tests: `DeliveryMetricSnapshotRecordingHandlerTest`, `DeliveryMetricsHistoryReadApiIntegrationTest`, `DeliveryWithLikelihoodDtoTest`, `DeliveriesControllerTest`/`UtcTest`.

A hardening pass added two tests that killed the genuine per-feature survivors:
- `HandleAsync_DeliveryWithNoPlottableFeatures_RecordsNullFeatureBreakdown` — kills the recorder's `Count > 0 ? … : null` conditional + equality mutants.
- `GetMetricsHistory_FeatureBreakdownJsonIsLiteralNull_ReturnsEmptyBreakdown` — kills the DTO's `?? []` null-coalescing mutant (literal `"null"` JSON).

### Backend survivors (5) — all pre-existing/equivalent, none in per-feature code
- `DeliveryMetricsHistoryDto.cs:35` — the Slice-1 `firstSnapshotDate` ternary (pre-existing read-API code).
- `DeliveryMetricsHistoryDto.cs:26` — `PropertyNameCaseInsensitive = true → false` (equivalent: PascalCase round-trips regardless).
- `DeliveryMetricSnapshotRecordingHandler.cs:57,64` — `stopwatch.Stop(); → ;` (Slice-4 diagnostics equivalents; only feed an unasserted log).
- `DeliveryMetricSnapshotRecordingHandler.cs:67` — the error log-message string (Slice-4 equivalent).

## Frontend scope

The chart's PURE logic was extracted into `feverChartView.ts` and mutated there;
the presentational `DeliveryFeverChart.tsx` shell (JSX, `FeverZoneBands`/`FeatureLegend`
render, `sx`) is excluded — the established Slice-4 chart precedent (a mocked chart
primitive cannot assert presentation).

Mutated:
- `models/Delivery/FeverTrail.ts` — `deriveFeatureFeverChart` (per-feature grouping) + `feverZonePolygons` + band constants.
- `components/Common/Charts/useFeatureFeverReveal.ts` — the Run/animation reveal hook.
- `components/Common/Charts/feverChartView.ts` — `currentPoint`/`visiblePoints` (frame→point incl. the hold-at-last boundary), `likelihoodTooltip`, `runButtonLabel`, `featureColor`, `zoneColors`, `zoneBandPath`.

Tests: `feverChartView.test.ts` (15), `FeverTrail.test.ts`, `DeliveryFeverChart.test.tsx` (component behaviour + hook).

| File | Score | Survivors |
|---|---|---|
| `feverChartView.ts` | **100.00%** | 0 |
| `FeverTrail.ts` | 85.19% | 8 |
| `useFeatureFeverReveal.ts` | 80.65% | 6 |
| **Overall** | **89.15%** | 14 |

### Frontend survivors (14) — all equivalent/presentational
- `FeverTrail.ts` (8): band-geometry equivalents — `BAND_SLOPE`/`AMBER_RED_INTERCEPT` arithmetic-operator swaps and the polygon-vertex `ArrayDeclaration` mutants. These move triangle/quadrilateral vertices consumed only as presentational SVG zone fills; the coordinates are equivalent geometry, not asserted business outcomes.
- `useFeatureFeverReveal.ts` (6): interval/cleanup plumbing — `clearInterval` guard + `useCallback`/`useEffect` dependency-array mutants. React-plumbing equivalents; the observable frame/running sequence is pinned by the hook tests.

## Configs

- `Lighthouse.Backend/Lighthouse.Backend.Tests/stryker-config.delivery-metrics-slice5.json`
- `Lighthouse.Frontend/stryker.config.delivery-metrics-slice5.mjs` + `vitest.stryker.delivery-metrics-slice5.config.ts`
