# Mutation testing — 6251 (Feature Size on the Team)

Run 2026-10-13 on `worktree-cozy-growing-shore` @ `5e85e70b7`, after the refactor and the adversarial
review's fixes had landed. The gate is an 80 % kill rate on each stack.

| stack | score | tested | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.0.0), lines this story changed | **77.61 %** → **88.06 %** after the new tests | 67 | 52 → 59 | 15 → 8 | 0 | 31 m 33 s |
| Backend, the eight files whole | 47.20 % | 1004 | 633 (1 by timeout) | 371 | 0 | (same run) |
| Frontend (StrykerJS, vitest runner), scoped to changed lines | **86.82 %** → **93.80 %** after the new tests | 129 | 112 → 121 | 11 → 8 | 6 → 0 | 18 m 58 s |

Configs: `stryker.6251.backend.json` (run from `Lighthouse.Backend/Lighthouse.Backend.Tests/`),
`stryker.6251.frontend.json` with `vitest.stryker.6251.config.ts` (both copied into
`Lighthouse.Frontend/` to run).

## Backend

Stryker.NET mutates whole files, and five of the eight files are large metrics services and controllers
whose old code the story barely touched. The whole-file 47 % scores that old code against a test filter
narrowed to the story's tests. The gate is read on the mutants that sit on the lines this story added or
changed (taken from `git diff -U0 fc633d9dd..HEAD`).

| file | whole file | changed lines |
| --- | --- | --- |
| `TeamFeatureSizeMetricsController.cs` (new) | 12/18 | 12/18 → 16/18 |
| `TeamMetricsController.cs` | 116/205 | 1/2 → 2/2 |
| `Feature.cs` | 32/87 | 4/4 |
| `BaseMetricsService.cs` | 283/417 | 14/14 |
| `MetricsCacheKey.cs` | 2/2 | 2/2 |
| `TeamMetricsService.cs` | 107/337 | 19/27 → 21/27 |
| `PortfolioMetricsService.cs` | 69/255 | no mutant on a changed line |
| `FeatureDto.cs` | 12/20 | no mutant on a changed line |

### Closed by this pass (`b36e0c661`)

Each test was proven by putting the surviving mutation back by hand and watching it go red.

- `startDate.Date > endDate.Date` → `>=` on all three Team routes: a one-day window is answered, on
  `allFeaturesForSizeChart`, `sizePercentiles` and `featureSizePercentilesInfo`.
- `f.Portfolios.Any(…IsBlocked…)` → `All`, on the Team's size chart and on `featuresInProgress`: a
  Feature blocked in only one of its two Portfolios reads blocked on the Team.
- `periodDays = … + 1` → `- 1` and `startDate.AddDays(-1)` → `AddDays(1)`: the period before a window
  is exactly as long as the window and ends the day before it starts.

### Accepted survivors

- Six mutants on the three `LogDebug` calls in `TeamMetricsService` (statement removed, message emptied):
  log wording.
- `.ConfigureAwait(false)` → `true` in `GetReadablePortfolioIds`: equivalent in ASP.NET Core, which has
  no synchronisation context.
- `readablePortfolioIds is { } ? … : …` forced true: the RBAC service never returns null in production,
  so the fallback only matters to a mock. Same pattern as `FeaturesController`.

## Frontend

Scoped with line spans to the lines the story changed; the per-file table was checked for every file
expected (no span silently dropped).

| file | score |
| --- | --- |
| `FeatureSizeScatterPlotChart.tsx` | 80.00 % → 90.00 % |
| `useMetricsData.ts` | 90.00 % |
| `BaseMetricsView.tsx` | 100 % |
| `categoryMetadata.ts` | 88.89 % |
| `widgetInfoMetadata.ts` | 100 % |
| `MetricsService.ts` | 54.55 % → 100 % |

### Closed by this pass (`993ad1807`)

- `getAllFeaturesForSizeChart` on the shared metrics service had no unit test (5 uncovered mutants):
  it now asks each owner's own route for the window and turns the dates into real dates.
- The Portfolio bubble's title says `1 item` and `2 items` (three mutants on the plural suffix).
- Outside closed-date mode the y-axis names the cycle time in days, or the estimation unit.

### Accepted survivors

- Two MUI `sx` mutants on the scatter's `Card`: styling.
- The two query-key names in `useMetricsData.ts` emptied: an empty name is still unique among the keys,
  so nothing can collide.
- `"featureSizePbc"` emptied in the Portfolio-only fetch keys: the same fetch is also kept from a Team by
  the widget's applicability and by the service check, so this list is a third guard.
- `?? []` in `categoryMetadata.ts` (no coverage): every widget has an entry, so the fallback is never
  taken.
- The size axis's `fixedXAxisMax ?? …` and its value getter: the axis maximum is a visual scale that no
  test pins.
