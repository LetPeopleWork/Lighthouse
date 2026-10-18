# Mutation testing — Epic #6311 slices 01–04 (Bug #6308 + Story #6309)

Gate is 80 % kill rate on each stack, over the lines the feature changed (`git diff 27392a583^..HEAD`,
production files only).

## Frontend

StrykerJS 10.0.0, run 2026-10-18 from `Lighthouse.Frontend/` against the frontend code at `f1a89679a`:

```
pnpm exec stryker run <abs path>/stryker.6308.frontend.json --incremental
```

Configs: `stryker.6308.frontend.json` and `vitest.stryker.6308.ts` (copy it into `Lighthouse.Frontend/`
before a run; it names the 56 spec files that import the mutated files). `coverageAnalysis: perTest`,
incremental file `reports/mutation/stryker-incremental-6308.json` (gitignored). `mutate` is scoped by
line range to the changed lines of the 12 files listed in the config.

| run | score | tested | killed | survived | timeout | no cov | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| First (full) | **91.58 %** | 95 | 87 | 8 | 0 | 0 | 18 m 12 s |
| After kill round (incremental, 56 of 95 reused) | **97.89 %** as reported, **98.95 %** verified | 95 | 93 (94) | 2 (1) | 0 | 0 | 6 m 20 s |

`Feature.ts`, `forecastSchemas.ts` and `ForecastService.ts` produced no mutants on their changed lines
(argument passing and schema declarations Stryker does not mutate).

Per-file, first run → after kill round:

| file | mutants | survived (first) | survived (after) |
| --- | --- | --- | --- |
| DeliveryPredictabilityChart.tsx | 15 | 2 | 1 |
| ForecastInfo.tsx | 18 | 2 | 0 |
| forecastPercentileText.ts | 22 | 0 | 0 |
| ArchivedDelivery.ts | 3 | 2 | 1 reported, 0 verified |
| DeliveryMetricsHistory.ts | 4 | 2 | 0 |
| WhenForecast.ts | 1 | 0 | 0 |
| deliveryTimelineModel.ts | 30 | 0 | 0 |
| deliveryExportTable.ts | 1 | 0 | 0 |
| DeliverySection.tsx | 1 | 0 | 0 |

The gaps a review had called thin were already killed on the first run: `yearsBeyondHorizon`'s
`=== true`, `drawableDate`, and `barEndOf`'s `!forecast?.expectedDate` (`forecastPercentileText.ts` and
`deliveryTimelineModel.ts` both 100 %). `whenValueFormatter` was the real gap.

### Killed in the kill round

- `DeliveryPredictabilityChart.tsx:124` `years === undefined` → `false` (a dated day's tooltip would read
  "More than undefined years") — *shows the date in the tooltip for a day whose percentile had one*.
  Also added *words each day with the horizon that day was recorded under* (7 then 3 years), so the years
  are no longer only ever tested with 5.
- `ForecastInfo.tsx:80` reason ternary → always a reason, and `ForecastInfo.tsx:29` `reason &&` →
  `reason ||` (an empty reason line in the tooltip) — *gives a percentile within the horizon a tooltip
  with only its level, and no reason line*.
- `DeliveryMetricsHistory.ts:121` and `:124`, the field names in the boundary error — two new rows in
  *names the offending field %s in the boundary error* (`whenDistribution.beyondHorizon`,
  `whenDistribution.horizonYears`).
- `ArchivedDelivery.ts:67` `value ?? undefined` → `value && undefined` and → `() => undefined` (an
  archived Delivery would lose the horizon's years) — *keeps a closing-day percentile beyond the horizon,
  with the horizon's years*, plus *reads a closing-day percentile with no horizon years as having none*.

The incremental re-run still reports `() => undefined` as Survived: its first-run result recorded no
covering test, so the incremental differ saw nothing changed and reused it. Applied by hand and run
against `ArchivedDelivery.test.ts`, it fails the new test, so it is killed; hence the verified 94 / 95.

### Accepted survivor

- `DeliveryPredictabilityChart.tsx:98` `point?.whenDistribution` → `point.whenDistribution`. `point` is
  undefined only for a data index outside the history, and the chart only ever asks for the indices of
  the points it was handed. Unreachable guard; equivalent in every call the chart makes.
