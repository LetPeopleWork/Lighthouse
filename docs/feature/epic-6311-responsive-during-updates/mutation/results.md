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

## Backend

Stryker.NET 5.0.0, run 2026-10-18 from `Lighthouse.Backend/Lighthouse.Backend.Tests/` against the backend
code at `109ff62c5`:

```
dotnet stryker --config-file <abs path>/stryker.6308.backend.json            # first run
dotnet stryker --config-file <abs path>/stryker.6308.backend.killround.json  # kill-round re-run
```

`coverage-analysis: perTestInIsolation`, concurrency 4. `mutate` names whole files (Stryker.NET ignores
line ranges), so each score below covers the whole file, not only the lines this feature changed; the
"in changed lines" column says how many survivors sit on lines `git diff 27392a583^..HEAD` touched. The
test filter takes the unit tests of the forecast models and services, the update services, the delivery
metrics and forecast publishing, write-back, the startup banner, the DTOs and the forecast/deliveries
controllers. It leaves out `API.Integration`, `Dogfood`, `Integration.Containers` and the live-connector
categories, so a mutant only an acceptance test reaches shows as a survivor and is triaged below.

The kill-round re-run narrows `mutate` to the ten files that had survivors worth a test, with the same
test filter plus the new `WriteBackRoundTest`.

| run | score | tested | killed | survived | timeout | no cov | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| First (25 files) | **80.21 %** | 480 | 368 | 81 | 17 | 14 | 29 m 48 s |
| Kill-round re-run (10 files) | **84.42 %** | 276 | 231 | 43 | 2 | 0 | 22 m 53 s |
| Whole scope after kill round (untouched files carried from the first run) | **87.29 %** | 480 | 419 killed or timed out | 61 | | | |

Survivors on the lines this feature changed: **16 on the first run, 1 after the kill round** (a log
message's wording).

`ForecastHorizon.cs`, `HowTheRunEnded.cs`, `OneWorkersShareOfTheRuns.cs` and `DeliveryForecastBlock.cs`
produced no testable mutants (constants, an enum, a one-line delegation and a record).

Per-file, first run → after kill round (survived includes no-coverage):

| file | tested | survived (first) | of them in changed lines | survived (after) | score after |
| --- | --- | --- | --- | --- | --- |
| API/DTO/DeliveryMetricsHistoryDto.cs | 17 | 1 | 0 | 1 (not re-run) | 94.1 % |
| API/DTO/WhenForecastDto.cs | 1 | 0 | 0 | 0 | 100 % |
| API/FeatureForecastWindow.cs | 9 | 5 | 4 | 0 | 100 % |
| Models/Forecast/AggregatedWhenForecast.cs | 21 | 0 | 0 | 0 | 100 % |
| Models/Forecast/DeliveryCompletionForecast.cs | 23 | 2 | 0 | 2 (not re-run) | 91.3 % |
| Models/Forecast/ForecastBase.cs | 34 | 6 | 2 | 4 | 88.2 % |
| Models/Forecast/ForecastPercentile.cs | 7 | 1 | 1 | 0 | 100 % |
| Models/Forecast/ForecastSimulationLimits.cs | 2 | 0 | 0 | 0 | 100 % |
| Models/Forecast/StartForecast.cs | 2 | 0 | 0 | 0 | 100 % |
| Models/Forecast/WhenForecast.cs | 8 | 1 | 1 | 0 | 100 % |
| Models/SimulationResult.cs | 1 | 0 | 0 | 0 | 100 % |
| Services/.../Update/ForecastUpdater.cs | 38 | 6 | 0 | 6 (not re-run) | 84.2 % |
| Services/.../DeliveryMetricValuesProjector.cs | 29 | 13 | 1 | 6 | 79.3 % |
| Services/.../DeliverySources/DeliveryForecastBlockRenderer.cs | 59 | 2 | 0 | 2 (not re-run) | 96.6 % |
| Services/.../Forecast/ForecastService.cs | 82 | 27 | 1 | 26 | 68.3 % |
| Services/.../Forecast/SimulatedRun.cs | 40 | 7 | 0 | 7 (not re-run) | 82.5 % |
| Services/.../Forecast/WhatTheRunsCouldNotFinish.cs | 19 | 4 | 0 | 1 | 94.7 % |
| Services/.../RefreshRoundSummary.cs | 1 | 1 | 0 | 0 | 100 % |
| Services/.../WriteBackRound.cs | 15 | 12 | 4 | 1 | 93.3 % |
| Startup/StartupBanner.cs | 72 | 7 | 2 | 5 | 93.1 % |

The thin spots a review named, as the first run found them: `AggregatedWhenForecast` (total before the
never-finished bucket is stripped), `StartForecast`'s `Math.Max`, `ForecastPercentile.ExpectedDateFrom`
and the `trialCounter >= numberOfTrials` boundary were already killed. `ForecastBase.GetPercentile`'s
`TotalTrials > 0 ? BeyondTheHorizon : NoAnswer`, `WhenForecast.EveryRunOf`'s `unfinished > 0`,
`FeatureForecastWindow.WorstCaseWorkingDaysOf`'s ternary, the `ForecastCancelled` fold in `WriteBackRound`
(no unit test reached `TakeSummary` at all) and the `== 0` early return in
`ReportTheRunsThatReachedTheHorizon` were real gaps, all closed below.

### Killed in the kill round

- `ForecastBase.cs:76` `TotalTrials > 0` → `>= 0` and → `true`, and `ForecastPercentile.cs:20`
  `NoAnswer` built as beyond the horizon — *GetPercentile_WhenNothingWasSimulated_HasNoAnswerRatherThanOneBeyondTheHorizon*
  (`WhenForecastTest`). A forecast of nothing would otherwise read as five years out.
- `WhenForecast.cs:55` `unfinished > 0` → `>= 0` (an empty never-finished bucket in every combined
  forecast) — *EveryRunOf_WhenEveryRunFinished_AddsNoDayForRunsThatNeverFinished*.
- `FeatureForecastWindow.cs` — all five: `Max()` → `Min()`, the single-forecast overload emptied, the
  null-forecast guard (no coverage before), the beyond-the-horizon ternary forced true, and `+ headroom` →
  `- headroom`. New `FeatureForecastWindowTest`: *A_forecast_whose_95th_percentile_falls_on_day_40_reads_twice_that_plus_the_headroom*,
  *…beyond_the_horizon_reads_twice_the_horizon_plus_the_headroom*, *Without_a_forecast_only_the_headroom_is_read*,
  *Several_Features_read_as_far_as_the_one_that_finishes_last*, *No_Features_read_only_the_headroom*.
- `WriteBackRound.cs` — 11 of 12, including the `ForecastCancelled ?? false` / `ForecastSucceeded ?? true`
  fold, the refresh-or-forecast choice, `HasFinished`, `Join` and the staging drain; also
  `RefreshRoundSummary.cs:25`'s `ForecastSucceeded = true` default. New `WriteBackRoundTest`:
  *A_round_that_only_refreshed_reports_the_refresh_with_no_forecast_in_it*,
  *A_round_that_only_forecast_reports_the_forecast_as_its_whole_story*,
  *A_forecast_that_was_stopped_folds_into_the_refresh_as_stopped_not_as_broken*,
  *A_round_that_is_asked_twice_speaks_only_once*,
  *A_round_finishes_only_once_every_execution_that_joined_it_has_left*, *What_was_staged_is_handed_over_once*.
- `DeliveryMetricValuesProjector.cs:90` (the one changed line: `WhenDistributionJson` when nothing could
  be forecast), plus `:71`/`:72` `Sum` → `Max` and the four `:88` estimated-portion mutants —
  *Project_DeliveryNobodyCouldForecast_RecordsNoLikelihoodAndNoDistribution*,
  *Project_DeliveryWhoseFeaturesAreAllSized_RecordsNoEstimatedPortion*,
  *Project_DeliveryWithAFeatureOnTheDefaultSize_RecordsHowMuchOfTheWorkIsEstimated*.
- `ForecastService.cs:287`, the `== 0` early return before the horizon Debug line —
  *AForecastWhoseEveryRunFinishes_SaysNothingAboutTheHorizon* (`ForecastRunThatCannotEndTest`).
- `WhatTheRunsCouldNotFinish.cs:21`, `:29`, `:31` (the Teams' order, `+=` → `-=` and the union across
  workers) — *WhatEveryWorkerCollected_AddsUpToOneAccountOfTheWholeForecast*.
- `StartupBanner.cs:228`, the cores line's emoji and label —
  *The_cores_line_sits_in_the_banner_under_its_own_label_like_every_other_line*.

### Accepted survivors

On a line this feature changed:

- `ForecastService.cs:274` — the wording of the "Abandoned … simulated runs" error. The tests pin that the
  line is logged at Error with the run count and the Teams, not its prose.

Outside the lines this feature changed (pre-existing code in a file the feature touched):

- `ForecastService.cs` — 16 are log statements or their message text (`:25`, `:26`, `:35`, `:50`, `:57`,
  `:62`, `:79`, `:83`, `:276`). `:92` is equivalent (both sides yield the same list). `:141` emptying
  `return []` for a plan with no rows is equivalent: the code after it builds nothing from no rows.
  `:232` and `:253` `OrderBy` → `OrderByDescending` are equivalent: the histogram is read back through a
  `SortedDictionary`, so insertion order never shows. Untested pre-existing behaviour: `:39`
  `trial < Trials` → `<=` in How Many (one extra run, invisible to percentile assertions), `:81` `Any` →
  `All` over a Feature's Teams (needs a multi-Team Feature with one Team outside the forecast), `:87`
  `Save()` removed (the unit tests read the in-memory Feature) and `:328` the throughput-status copy onto a
  Team's forecast.
- `ForecastBase.cs` — `:13` the comparer-only constructor emptied (the default comparer it falls back to
  is the same ascending one every caller passes), `:38` `Count < 1` → `<= 1` (rebuilds a one-day cache from
  the same rows: equivalent), `:105` and `:112` the clears on a second `SetSimulationResult` (no caller sets
  a histogram twice on one instance).
- `DeliveryMetricValuesProjector.cs` — `:89` is equivalent in every reachable state (a Delivery without a
  distribution has no likelihood either), `:95` the feature breakdown's empty case, `:110` and `:114` the
  blackout window's end when there are no Deliveries or only past ones.
- `WriteBackRound.cs:105` `connectionsById.Clear()` removed: equivalent, since connections are only ever
  read through the keys of the staged updates, which are cleared.
- `WhatTheRunsCouldNotFinish.cs:41` — the early `return` for a run that finished. Removing it would count
  every healthy run as given up and log an Error, and no unit test asserts the absence of that Error on a
  healthy forecast. A real gap, not an equivalent mutant: adding `WhatWasLoggedAt(LogLevel.Error)` is empty
  to *AForecastWhoseEveryRunFinishes_SaysNothingAboutTheHorizon* would close it. Left for the maintainer
  rather than run a second kill round.
- `StartupBanner.cs` — `:62`, `:79`, `:80`, `:218` encryption-notice wording and `:117` `ThrowIfNull`.
- Not re-run (no survivor on a changed line): `ForecastUpdater.cs` (`:41`, `:145`, `:150` the unsupported
  periodic-refresh path; `:101`, `:118`, `:180` hold bookkeeping and the stopwatch), `SimulatedRun.cs`
  (`>` → `>=` boundaries at `:27`, `:29`, `:41`, `:66`, `:114`; `:29` is the horizon ceiling, where `>=`
  ends a run one simulated day earlier out of five years; `:76` the draw-coordinate offset),
  `DeliveryCompletionForecast.cs:77` (joining excluded summaries), `DeliveryMetricsHistoryDto.cs:88` (the
  `?? []` fallback after a JSON `null`) and `DeliveryForecastBlockRenderer.cs:59`, `:82` (an exception
  message and the `%` suffix text).

### Not mutated

Large files where the feature changed a few lines. Mutating them whole would bury those lines under
hundreds of mutants of untouched code, so they are excluded and the changed lines are covered as follows:

| file | size | changed | what changed | covered by |
| --- | --- | --- | --- | --- |
| Models/Delivery.cs | 684 lines | 9 | `ToWhenPercentile` reads `GetPercentile` and sets `BeyondHorizon`/`HorizonYears` | `WhenDistributionBeyondTheHorizonDtoTest`, `Slice03BeyondTheHorizonScenarios`, and `ForecastPercentile.ExpectedDateFrom` mutated and killed above |
| API/DTO/DeliveryWithLikelihoodDto.cs | 180 lines | 4 | copies `BeyondHorizon`/`HorizonYears` onto the DTO (plus a comment) | `WhenDistributionBeyondTheHorizonDtoTest`, `Slice03BeyondTheHorizonScenarios` |
| API/ForecastController.cs | 239 lines | 13 | the private window estimate is removed and replaced by `FeatureForecastWindow.EndFor` | `FeatureForecastWindowTest` (100 % above), `ForecastControllerTest` |
| Services/.../Update/UpdateServiceBase.cs | 361 lines | 12 | `ReportForecastSummary` carries `cancelled`; the summary line says `cancelled after` | `ForecastUpdaterTest` (asserts `" \| forecast=cancelled after "`), `WriteBackRoundTest` |
| Services/.../WriteBackTriggerService.cs | 363 lines | 10 | a percentile with no day writes nothing (plus a comment) | `WriteBackTriggerServiceTest.BeyondTheHorizon` |
| Services/.../DeliverySources/DeliveryForecastPublishingService.cs | 232 lines | 3 | passes `BeyondHorizon` into the block percentile | `DeliveryForecastBlockRendererBeyondTheHorizonTest` (renderer 96.6 % above), `DeliveryForecastPublishingServiceTest` |
| Services/Interfaces/Forecast/IForecastService.cs | interface | 1 | adds an optional `CancellationToken` | nothing to mutate |
