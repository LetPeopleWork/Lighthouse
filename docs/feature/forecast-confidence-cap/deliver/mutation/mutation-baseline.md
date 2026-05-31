# Mutation Baseline — forecast-confidence-cap (ADO #5126)

Frontend-only feature (ADR-038 Option A). Single mutation target: the pure cap helper.

## Result

| Target | Mutants | Killed | Survived | Score |
|---|---|---|---|---|
| `src/utils/forecast/formatLikelihood.ts` | 16 | 16 | 0 | **100.00%** |

Threshold: `per-feature >= 80%`. **Met** (100%).

## Configuration

- `stryker.config.forecast-confidence-cap.mjs` (mutate: `src/utils/forecast/formatLikelihood.ts`)
- `vitest.stryker.forecast-confidence-cap.config.ts` (oracle suite: `formatLikelihood.test.ts` boundary matrix + the three routed component tests: `ForecastLikelihood`, `DeliveriesChips`, `DeliverySection`)

## Why 100%

The boundary-matrix `it.each` over `94.9 / 95 / 95.01 / 100` × `hasRemainingWork ∈ {true,false}` × `precision ∈ {round, fixed2}` is a complete oracle for the helper: every arithmetic, comparison, conditional, and string-literal mutant (the strict `>` boundary, the `&&`, the `Math.round`/`toFixed(2)` precision branches, the `>95%` literal, the threshold constant `95`) is independently distinguished by at least one case. No survivors, no equivalent mutants to justify.
