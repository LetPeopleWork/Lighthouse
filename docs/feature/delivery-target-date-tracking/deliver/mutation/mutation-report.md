# Mutation Report — delivery-target-date-tracking

Date: 2026-06-04 · Stryker.NET (BE) + Stryker/Vitest (FE) · feature-scoped configs:
`Lighthouse.Backend.Tests/stryker-config.delivery-target-date.json`,
`Lighthouse.Frontend/stryker.config.delivery-target-date.mjs` (+ `vitest.stryker.delivery-target-date.config.ts`).

## Verdict

| Surface | Score | Bar (≥80%) | Note |
|---|---|---|---|
| **Backend** (recorder + DTO) | **86.84%** | ✅ PASS | every feature-introduced mutation killed |
| **Frontend — feature core logic** (`deliveryTargetHistory.ts`) | **100.00%** | ✅ PASS | the new pure helper (steppedTargetData / targetChanges) |
| **Frontend — aggregate** (helper + 2 charts + parser) | 75.27% | ⚠️ below | presentational-bound; see analysis |

**The feature's real logic is fully mutation-covered.** The frontend aggregate sits below 80% solely
because Stryker mutates the *whole* chart + boundary-parser files, whose surviving mutants are
presentational (MUI labels / colours / config objects / axis formatters) or boundary error-message
strings — on code that is largely **pre-existing** (the charts and the parser shipped with
`delivery-metrics`; this feature added a stepped series, a dot overlay, and one parsed field).

## Backend — 86.84% (33 killed / 5 survived)

Only the two changed files were mutated.

| File | Killed | Survived | Survivor nature |
|---|---|---|---|
| `DeliveryMetricSnapshotRecordingHandler.cs` | 18 | 3 | L58/L65 best-effort `LogInformation`/stopwatch statements; L68 log-message string — observability, not behaviour. **My change (`TargetDateAtSnapshot = delivery.Date`) is killed** by the recorder tests. |
| `DeliveryMetricsHistoryDto.cs` | 15 | 2 | L27/L36 the **pre-existing** `firstSnapshotDate = points.Count == 0 ? null : points[0].Date` ternary — not introduced by this feature; the added `targetDateAtSnapshot` mapping is killed by the read-API test. |

All 5 survivors are pre-existing observability / a pre-existing branch — none are this feature's
new logic. Score clears the 80% bar.

## Frontend — per file

| File | Score | Killed | Survived | Assessment |
|---|---|---|---|---|
| `deliveryTargetHistory.ts` (NEW core) | **100.00%** | 31 | 0 | full coverage of steppedTargetData + targetChanges incl. the null-after-set and no-change edges |
| `DeliveryPredictabilityChart.tsx` (changed: step + dots) | 71.15% | 111 | 45 | survivors ≈ StringLiteral labels/ids/format-strings (22), ObjectLiteral MUI config (slotProps/sx/axis) (15), axis arrow-function formatters, colour choices — presentational. New-series *behaviour* (data positions, `curve:"stepAfter"`, `showMark`, the date-pair `valueFormatter`, the change-vs-no-change branch) is asserted and killed. |
| `DeliveryMetricsHistory.ts` (changed: +1 parsed field) | 77.50% | 93 | 27 | pre-existing boundary parser; survivors ≈ the `asX(... , "context")` error-message StringLiterals (the tests assert that parsing throws, not the exact message text). The new `targetDateAtSnapshot` parse is killed. |
| `DeliveryBurnupChart.tsx` (changed: removed dead line) | 69.23% | 45 | 20 | pre-existing presentational chart; this feature only *removed* the dead `ChartsReferenceLine` (asserted absent). No new logic. |

## Why the presentational survivors are not chased

Per `nw-test-design-mandates` / the project test conventions: tests assert **business behaviour**,
not framework wiring. Killing the remaining survivors would mean asserting MUI series labels,
colour constants, `slotProps.legend` positions, axis `valueFormatter` output, and exact boundary
error-message text — i.e. testing the chart library and string constants, not the feature. This is
the same presentational-bound profile documented for the sibling chart feature
(`state-time-cumulative-view`). The behavioural surface that matters — the moving-target step data,
the change-dot positions and visibility, the flat-line/empty fallbacks, the no-duplicate-marker
rule, and the recorder capture — is killed, and is additionally proven end-to-end by the live
Playwright walking skeleton (the `target` step series renders against demo data).

## Lifting tests added this pass (kill real new-logic mutants)

- `deliveryTargetHistory.test.ts` — "reports no change when a later snapshot has no recorded target"
  (kills the `targetChanges` null-after-set conditional → helper 96.77% → **100%**).
- `DeliveryPredictabilityChart.test.tsx` — assert the target step series `curve:"stepAfter"` +
  `showMark:false`, and the change-dot series `showMark:true` (kills the new-series boolean/curve
  mutants).
