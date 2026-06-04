# Mutation Report — Slice 4 (US-04 Predictability Trend)

Feature: delivery-metrics (Epic 3993) · Slice 04-predictability-trend · 2026-06-04
Strategy: `per-feature` (CLAUDE.md) · Gate: ≥80% kill on new code.

## Result — PASS (both stacks)

| Stack | Config | Score | Killed | Survived | Verdict |
|---|---|---|---|---|---|
| Backend | `stryker-config.delivery-metrics-slice4.json` | **83.33%** | 15 | 3 | PASS — survivors are equivalents |
| Frontend | `stryker.config.delivery-metrics-slice4.mjs` (logic-scoped) | **94.55%** | 52 | 3 | PASS — survivors are equivalents |

## Backend scope

Mutated the new Slice-4 logic:

- `Models/Delivery.cs:50-86` — `CalculateMetrics` / `GetLeastLikelyFeature` / `ToWhenPercentile` (the shared likelihood + when-distribution projection).
- `Services/Implementation/DomainEvents/DeliveryMetricSnapshotRecordingHandler.cs` — the likelihood/when forward-recording (`hasForecast` guard + serialization).

Tests: `DeliveryMetricSnapshotRecordingHandlerTest`, `DeliveryWithLikelihoodDtoTest`, `DeliveriesControllerTest`, `DeliveriesControllerUtcTest`, `DeliveryMetricsHistoryReadApiIntegrationTest`.

### Survivors (3) — all justified equivalents

All three are in the **pre-existing Slice-1 recorder diagnostics**, not Slice-4 business logic:

1. `DeliveryMetricSnapshotRecordingHandler.cs:54` — `stopwatch.Stop();` → `;`. Stopwatch is read only into a log message; removing the stop is behaviourally equivalent.
2. `DeliveryMetricSnapshotRecordingHandler.cs:61` — `stopwatch.Stop();` → `;` (catch path). Same.
3. `DeliveryMetricSnapshotRecordingHandler.cs:64` — the error log-message string → `""`. Log text, not behaviour.

The Slice-4 projection (least-likely-feature selection, percentile-set parameterization, when-date math) and the recorder's `hasForecast` guard + integer-percentile serialization were all killed.

## Frontend scope

Mutated the chart's **logic region** (the presentational MUI render is scope-excluded, matching the Slice-1/2 `DeliveryBurnupChart.tsx:15-95` precedent — a mocked `LineChart` cannot meaningfully assert sx/legend/axis-config mutants):

- `DeliveryPredictabilityChart.tsx:44-97` — formatters, `hasLikelihood`/`hasWhenDistribution`, `completionDateAt`, `buildWhenSeries`.
- `DeliveryPredictabilityChart.tsx:238-250` — view state, `hasDataForView` branch, `onViewChange` null-guard.

Tests: `DeliveryPredictabilityChart.test.tsx` (18 tests). The Phase-5 hardening pass added 6 tests killing the genuine logic survivors: percentage/date `valueFormatter` output (incl. null→""), empty-array-vs-null `whenDistribution` empty-state, per-percentile `completionDateAt` gapping, when-series labels + default-70 emphasis (`showMark`), and the toggle deselect (`onChange(null)`) guard.

### Survivors (3) — justified equivalents

Presentational/config with no observable behaviour through the mocked chart: the x-axis `formatDate` (line 55, x-axis tick formatter whose output is never asserted), the `WHEN_PALETTE` colours, and the `TARGET_LINE_DASH` pattern. Forcing tests on these would be assertion theater.

## Reproduce

```
# backend
cd Lighthouse.Backend/Lighthouse.Backend.Tests && dotnet stryker -f stryker-config.delivery-metrics-slice4.json
# frontend
cd Lighthouse.Frontend && pnpm exec stryker run stryker.config.delivery-metrics-slice4.mjs
```
