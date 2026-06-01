# Mutation Testing — forecast-minimum-data-guard (ADO #5125)

Feature-scoped mutation testing per the project's `per-feature` strategy (min kill rate 80%).

## Result

| Stack | Scope | Killed | Survived | Score |
|-------|-------|--------|----------|-------|
| Backend (Stryker.NET) | `ForecastDataSufficiencyPolicy`, `RunChartData`, `AggregatedWhenForecast` | 18 (+1 timeout) | 0 | **100%** |
| Frontend (Stryker) | `isForecastDataInsufficient` (pure rule) | 7 | 0 | **100%** |

Both stacks exceed the 80% threshold.

## Configs

- Backend: `Lighthouse.Backend.Tests/stryker-config.forecast-minimum-data-guard.json`
- Frontend: `stryker.config.forecast-minimum-data-guard.mjs` + `vitest.stryker.forecast-minimum-data-guard.config.ts`

## Notes

- The first backend run scored 57.89%: every survivor sat on `AggregatedWhenForecast`. The
  feature-relevant one was the `All()`→`Any()` mutant on the `HasSufficientData` AND-aggregation —
  it survived because every prior test gave a feature a single contributing team, where `All` ≡ `Any`
  (the DISTILL AI-1 multi-team gap). A dedicated `AggregatedWhenForecastTest` (multi-forecast, mixed
  flags + summaries) killed that mutant and Boy-Scouted the pre-existing `FilterApplied`/`ExcludedSummary`
  aggregation mutants in the same file → 100%.
- The first frontend run scored 41.67%: the survivors were all on a dead `compact`/`full` variant of
  `InsufficientForecastDataIndicator` that nothing rendered (the chips use the `INSUFFICIENT_FORECAST_DATA_SHORT`
  constant directly). Removing the unused variant left a purely presentational component (excluded from the
  mutate scope, mirroring the sibling forecast-confidence-cap which mutated only its pure util), and the pure
  decision rule `isForecastDataInsufficient` scores 100%.
