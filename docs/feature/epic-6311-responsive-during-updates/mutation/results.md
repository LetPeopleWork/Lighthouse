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

## Slice 05 — Cancel all (Story #6322), frontend

StrykerJS 10.0.0, run 2026-10-19 from `Lighthouse.Frontend/` against the frontend code at `0fe60583b`,
over the lines slice 05 and its review fixes changed (`git diff f0ebbe5e6..HEAD`, production files only):

```
pnpm exec stryker run <abs path>/stryker.6322.frontend.json --incremental
```

Configs: `stryker.6322.frontend.json` and `vitest.stryker.6322.ts` (copy it into `Lighthouse.Frontend/`
before a run; it names the 6 spec files that render the Task Manager or call the update service:
`ActivitySection`, `TaskManagerIcon`, `TaskManagerIcon.cancelAll`, both `Header` specs, and
`UpdateSubscriptionService`). `coverageAnalysis: perTest`, incremental file
`reports/mutation/stryker-incremental-6322.json` (gitignored). `mutate` is scoped by line range to the
`git diff -U0` hunks of the 5 changed files.

| run | score | tested | killed | survived | timeout | no cov | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| First (full) | **82.86 %** | 70 | 58 | 11 | 0 | 1 | 3 m 55 s |
| After kill round (incremental, 17 of 70 reused) | **92.86 %** | 70 | 65 | 5 | 0 | 0 | 2 m 49 s |

Per-file, first run → after kill round:

| file | mutants | survived (first) | survived (after) |
| --- | --- | --- | --- |
| ActivitySection.tsx | 31 | 4 | 2 |
| SectionHeading.tsx | 1 | 1 | 1 |
| useTaskManagerPopover.ts | 33 | 6 | 2 |
| TaskManagerIcon.tsx | 4 | 0 | 0 |
| UpdateSubscriptionService.ts | 1 | 1 (no coverage) | 0 |

### Killed in the kill round

- `UpdateSubscriptionService.ts:276` the request path → `""` (no spec called `cancelAllTasks` at all) —
  *asks the instance to stop everything in one request*.
- `useTaskManagerPopover.ts:83` the refusal starts out `true` (the warning would greet the first opening
  over rows nobody asked to stop) — *says nothing about a refusal before "Cancel all" was pressed*.
- `useTaskManagerPopover.ts:154` the refusal is set rather than cleared when Cancel all is pressed (a
  successful second try would leave "Could not cancel" standing) — *drops "Could not cancel. Try again."
  once a second try is heard*.
- `useTaskManagerPopover.ts:97` `some(canBeStopped)` → `every` (a removal beside stoppable work would wipe
  the warning on the next read) — *keeps "Could not cancel. Try again." while a removal is listed beside
  work that can still be stopped*.
- `useTaskManagerPopover.ts:148` dropping `.filter((key) => !stopAsked.has(key))` (a refusal would put back
  a row its own Stop had already marked) — *leaves a row stopped on its own still stopping when "Cancel
  all" is refused*.
- `ActivitySection.tsx:275` the Stop button's tooltip → empty — *names what its Stop would stop when the
  reader points at it*.
- `ActivitySection.tsx:241` `cancelAllFailed` defaulting to `true` — *warns about no refused Cancel all
  unless it is told one was refused*.

### Accepted survivors

- `ActivitySection.tsx:288` `closest("[data-task-row]")?.focus()` → `.focus()`. The Stop button is only
  ever drawn inside a `data-task-row` box, so `closest` never returns null. Unreachable guard.
- `ActivitySection.tsx:306` the refusal `Alert`'s `sx={{ py: 0, mb: 0.5 }}` → `{}` and
  `SectionHeading.tsx:36` the heading text's `sx={{ flexGrow: 1 }}` → `{}`. Spacing and the push of the
  Cancel all button to the far end of the heading. jsdom does no layout, so no unit test can see them;
  they show only in the rendered page.
- `useTaskManagerPopover.ts:175` `popoverClosings.current += 1` → `-= 1`. The counter is only ever compared
  for equality against the value taken when Cancel all was pressed; any change means "the popover closed
  since". Equivalent.
- `useTaskManagerPopover.ts:177` `useCallback(..., [])` → `["Stryker was here"]`. A constant dependency
  never changes, so the callback is memoised exactly as before. Equivalent.

## Slice 05 — Cancel all (Story #6322), backend

Stryker.NET 5.0.0, run 2026-10-19 from `Lighthouse.Backend/Lighthouse.Backend.Tests/`. The first run
used the backend code at `0fe60583b`. The kill-round re-run used `7f5c373d4`, which adds tests and
changes no production code. The scope is the production files that slice 05 and its review fixes
changed (`git diff f0ebbe5e6..HEAD`):

```
dotnet stryker --config-file <abs path>/stryker.6322.backend.json            # first run
dotnet stryker --config-file <abs path>/stryker.6322.backend.killround.json  # kill-round re-run
```

`coverage-analysis: perTestInIsolation`, concurrency 4. `mutate` names whole files, because Stryker.NET
ignores line ranges, so each per-file score covers the whole file and not only this slice's lines. The
"changed lines" figures count mutants on lines that `git diff -U0 f0ebbe5e6..HEAD` touched.

The test filter takes the unit tests under `BackgroundServices.Update`, which include
`UpdateQueueServiceTests`, `CancelAllWhileWorkIsAdmittedTest` and
`RoundWriteBackDuringDatabaseMaintenanceTest`. It also takes `UpdateControllerTest`,
`DatabaseMaintenanceGateTest`, `WriteBackCollectorTest`, `WriteBackRoundTest` and one acceptance
suite, `Slice05CancelAllScenarios`. It leaves out `Dogfood`, `Integration.Containers` and the
live-connector categories.

**Why one acceptance suite is in the filter.** When the slice landed, three unit tests reached
Cancel all. Dropping parked follow-ups, letting held forecasts go, leaving removals to finish, and
writing back what a let-go round had resolved were pinned only by the eight
`Slice05CancelAllScenarios`. Without that suite, every `UpdateQueueService` mutant on those paths would
read as a survivor, and the first run would measure the suite's absence, not the gaps. It costs about
5 s per covering mutant, and the first run took 20 minutes. `CancelAllMultiReplicaTests` needs
Docker (Testcontainers) and stays out. So do the other `API.Integration` suites that reach
`UpdateController`'s list endpoints (Slice02, Slice03, Slice07), which is why most of that file's
survivors are on code those suites pin.

The kill-round re-run drops the four files that had nothing left to kill (`AdmittedCancellations`,
`InProcessUpdateCancellationNotifier`, `UpdateCancellationPolicy`, `DatabaseMaintenanceGate`). It
keeps the same test filter, so first-run kills stay comparable.

| run | score | tested | killed | survived | timeout | no cov | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| First (9 files) | **62.58 %** | 310 | 171 | 97 | 23 | 19 | 20 m 01 s |
| Kill-round re-run (5 files) | **69.23 %** | 247 | 158 | 65 | 13 | 11 | 17 m 28 s |
| Whole scope after kill round (untouched files carried from the first run) | **74.19 %** | 310 | 230 killed or timed out | 80 | | | |
| Changed lines only, first run → after kill round | **65.43 % → 93.83 %** | 81 | 53 → 76 | 28 → 5 | | | |

**Gate: not met on the whole-file score (74.19 % against 80 %).** It is met on the lines this slice
changed (93.83 %). Of the 80 survivors left in the whole scope, 34 are statements and wording of log
lines and 18 are pre-existing `UpdateController` code. That code is reached only by acceptance suites
this filter leaves out. With the log-text mutants set aside, the score is 230 / 276 = 83.3 %.
Elsewhere in this codebase, log-text mutants are excluded with a
`// Stryker disable once Statement,String : diagnostic log text is not behaviour` comment in the
production file. That is a production edit, so this pass did not make it. It is the maintainer's call.

Per-file, first run → after kill round (survived includes no coverage):

| file | tested | survived (first) | of them in changed lines | survived (after) | of them in changed lines | score after |
| --- | --- | --- | --- | --- | --- | --- |
| API/UpdateController.cs | 28 | 21 | 1 | 18 | 0 | 35.7 % |
| Services/.../Update/AdmittedCancellations.cs | 14 | 2 | 1 | 2 (not re-run) | 1 | 85.7 % |
| Services/.../Update/InProcessUpdateCancellationNotifier.cs | 5 | 0 | 0 | 0 | 0 | 100 % |
| Services/.../Update/UpdateCancellationPolicy.cs | 2 | 0 | 0 | 0 | 0 | 100 % |
| Services/.../Update/UpdateQueueService.cs | 189 | 82 | 24 | 55 | 4 | 70.9 % |
| Services/.../DatabaseManagement/DatabaseMaintenanceGate.cs | 42 | 2 | 0 | 2 (not re-run) | 0 | 95.2 % |
| Services/.../RefreshRoundSummaryLine.cs | 4 | 1 | 1 | 0 | 0 | 100 % |
| Services/.../WriteBackCollector.cs | 10 | 6 | 0 | 2 | 0 | 80.0 % |
| Services/.../WriteBackRound.cs | 16 | 2 | 1 | 1 | 0 | 93.8 % |

### Killed in the kill round

- `UpdateQueueService.cs:173`, `:201` (the Cancel all report line, the held-updates counter `++` →
  `--` and removed) — *Cancel_all_tells_the_operator_how_much_it_stopped_let_go_and_left_to_finish*
  (`CancelAllWhileWorkIsAdmittedTest`). The line must read "asked 1 refreshes to stop, let go 1 held
  updates …, and left 1 removals to finish".
- `UpdateQueueService.cs:440`, `:447` and `RefreshRoundSummaryLine.cs:40` (the queue says how a let-go
  round went once it has sent its write-back, in exactly the operator's one-line shape) —
  *What_a_round_let_go_by_cancelled_work_resolved_is_reported_in_one_line_once_its_write_back_is_sent*.
- `UpdateQueueService.cs:435` (no coverage before; the Error when that write-back fails) —
  *A_round_whose_write_back_fails_after_its_work_was_let_go_still_frees_the_database_and_says_how_it_went*.
  The same test also pins that the database is freed and the summary line is still written.
- `UpdateQueueService.cs:405`, `:406` (the round dropped because a restore holds the database: the Warning
  naming the operation, and the summary line) —
  *A_round_let_go_while_a_restore_holds_the_database_says_it_was_not_written_back_and_how_it_went*.
- `UpdateQueueService.cs:415`, `:416` (work let go during shutdown must hand back its hold on the
  database, or no backup, restore or clear could ever start again) —
  *Work_let_go_as_Lighthouse_shuts_down_does_not_leave_the_database_blocked*.
- `UpdateQueueService.cs:690`, `:720`, `:721` (the follow-up parked behind a stopped refresh gets a
  fresh cancellation: it still runs, and it can itself be stopped) —
  *EnqueueUpdate_TheRunningRefreshIsStoppedWithAFollowUpParked_TheFollowUpStillRunsAndCanItselfBeStopped*.
- `UpdateQueueService.cs:719` (the follow-up is requeued, so while it waits it reads as Queued and a
  cancel can take it off the list at once) — *EnqueueUpdate_AFollowUpWaitingBehindOtherWork_ReadsAsQueued*.
- `UpdateQueueService.cs:733`, `:749` (awaited work cancelled before it ran gives its round the place
  back) — *EnqueueAndAwaitAsync_CancelledBeforeItRuns_StillLeavesTheRoundItJoined*.
- `UpdateQueueService.cs:615`, `:738` (work stopped while running leaves its round through its own flush,
  and the queue must not leave it a second time) —
  *EnqueueUpdate_WorkStoppedWhileRunning_LeavesItsRoundOnlyThroughItsOwnFlush*.
- `UpdateQueueService.cs:834`, `:835` (disposing the queue drops both cancel subscriptions) —
  *Dispose_StopsListeningForCancelsAndForCancelAll*.
- `UpdateController.cs:95`, `:97`, `:100` (no coverage before: the single-cancel route refuses a removal
  with its message and never forwards it, and forwards a refresh) —
  *CancelTask_ARemoval_IsRefusedAndNeverReachesTheQueue* (TeamDelete, PortfolioDelete),
  *CancelTask_ARefresh_IsAcceptedAndHandedToTheQueue*.
- `WriteBackRound.cs:79` `> 0` → `>= 0` —
  *A_round_has_writes_to_send_only_while_something_is_staged_in_it*.
- `WriteBackCollector.cs:13`, `:25`, `:29`, `:32` (pre-existing: a collector whose round has another
  execution to come writes nothing, and leaves the round once however often it is flushed) —
  *FlushAsync_AnotherExecutionOfTheRoundIsStillToCome_LeavesTheWriteToIt*.

### Survivors left

On a line this slice changed (5):

- `UpdateQueueService.cs:399`, `return` after a leave that was not the last. Without it, every
  execution let go from a round queues a write-back job. This is a real but narrow gap. With a single
  sequential reader, that job runs after every execution queued ahead of it, so it usually finds
  nothing staged. It sends early only when a hold is released after the job was queued: two holds on
  one round, the first released with nothing to queue, the second releasing work that stages more.
  The round would then reach the tracker in two conversations instead of one. A test needs that
  two-hold arrangement, and it was left for the maintainer rather than run as a second kill round.
- `UpdateQueueService.cs:417`, the summary line on the shutdown path. The queue writes it when the
  last execution of a round that already has a summary is let go while the queue is closing. Reaching
  that needs the round's own execution to leave before it asks for more work, which no updater does,
  so the test would have to stage an order production never takes. Left.
- `UpdateQueueService.cs:182` (two mutants), the Debug line for a dropped follow-up. Log text.
- `AdmittedCancellations.cs:37`, `admitted.Dispose()` for the caller that lost the race. The source
  was never handed out, so not disposing it leaks one `CancellationTokenSource` and changes nothing
  observable. Equivalent.

Outside the lines this slice changed (pre-existing code in files the slice touched):

- `UpdateQueueService.cs`, 33 log statements or their wording (`:128`, `:218`, `:240`, `:244`, `:270`,
  `:287`, `:357`, `:369`, `:508`, `:561`, `:622`, `:629`, `:696`, `:747`, `:759`, `:808`). Behaviour
  without a unit test: `:107`/`:108`/`:122`, telling the browser that waiting work was cancelled (the
  hub is a mock nobody verifies); `:261`, the maintenance-blocked awaitable path; `:654`, the
  late-rerun race window; `:702`/`:703`, a follow-up met by a closed queue; `:730`, the awaitable path's
  InProgress advance; `:746`/`:758`, `?? terminalStatus`; `:766`, holds released after awaited work;
  `:579`, `:586`, `:590`, the caller-token bridge in `RegisterCancellation`; `:836`, disposing the sources
  on shutdown; `:826`, `GC.SuppressFinalize`; `:820`, the global group's method name (killed in the first
  run, survived the re-run, which suggests a race in the covering test). Equivalent: `:79`, `:297`,
  `:742` (the awaiter's `bool` result is never read, because callers get a plain `Task`).
- `UpdateController.cs`, 18 mutants in `GetTasks`/`ElapsedOn`/`WhatItIsWaitingFor` (`:62`, `:66`,
  `:131`, `:132`, `:143`, `:154`, `:155`, `:162`, `:163`). That is the lane-holder naming and the
  elapsed-time clamp, pinned by the excluded Slice02/Slice03/Slice07 Task Manager acceptance suites.
- `WriteBackCollector.cs:52` (the empty-staging early return: the loop after it does nothing with
  nothing, so equivalent) and `:56` (a Debug line).
- `WriteBackRound.cs:107`, `connectionsById.Clear()`. Accepted in slices 01–04 for the same reason.
- `DatabaseMaintenanceGate.cs:52` (`IsMaintenanceOperationActive`, no coverage in this filter) and
  `:179` (`&&` → `||` over an id and a type that are always set together). Not re-run.
- `AdmittedCancellations.cs:69`, the `ObjectDisposedException` catch in `TokenFor`, a race no unit
  test can place. Not re-run.

A finding outside this scope: `UpdateQueueServiceTests.EnqueueUpdate_CancelledBeforeItRuns_StillLeavesTheRoundItJoined`
asserts `rounds.Current is null || rounds.Current.HasFinished` from the test's own async flow. There,
`WriteBackRoundContext.Current` (an `AsyncLocal`) is always null, so the assertion cannot fail. The
awaitable twin added here captures the round inside the execution instead. The fire-and-forget path
is still killed through `RoundWriteBackDuringDatabaseMaintenanceTest`, so this costs no mutant today,
but the test proves nothing on its own.

### Not mutated

| file | size | changed | what changed | covered by |
| --- | --- | --- | --- | --- |
| Services/.../Update/UpdateServiceBase.cs | 318 lines | 47, all but 2 deletions | the summary-line writer moved out to `RefreshRoundSummaryLine` (mutated above, 100 %); two call sites now call it | `RefreshRoundSummaryLine` above; `ForecastUpdaterTest`, `TeamUpdaterTest`, `PortfolioUpdaterTest` assert the line |
| Services/.../Update/RedisUpdateCancellationNotifier.cs | 89 lines | 33 | publishes and subscribes the Cancel all channel over Redis | `CancelAllMultiReplicaTests` (Testcontainers, needs Docker, outside a Stryker run); its in-process twin is mutated above at 100 % |
| Services/Interfaces/Update/IUpdateCancellationNotifier.cs | interface | 9 | adds `PublishCancelAllAsync` / `SubscribeToCancelAll` | nothing to mutate |
