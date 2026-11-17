# Mutation testing — 6655 (outlier switch and outliers above the upper natural process limit)

The gate is an 80 % kill rate per stack.

## Backend

Run 2026-11-17, Stryker.NET 5.0.0, against the worktree at `5af9db680` (frozen code). The gate passes.

| run | score | tested | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| first, full scope (`stryker.6655.backend.json`) | **91.28 %** | 195 | 178 | 14 | 3 | 6 m 33 s |
| kill round, narrowed (`stryker.6655.backend.killround.json`) | **92.31 %** | 195 | 180 | 15 | 0 | 4 m 49 s |

The kill round mutates the same two files that hold every mutant of the first run (the DTO has none), so its 195
mutants are the full scope's and the two scores compare directly. It runs only the unit classes, not the port
scenarios.

Run from `Lighthouse.Backend/Lighthouse.Backend.Tests`:
`dotnet stryker --config-file ../../docs/feature/story-6655-outlier-toggle-and-unpl/mutation/stryker.6655.backend.json`

**Test selection.** `XmRCalculatorTest`, the new `CycleTimeOutlierDefinitionTest` and `CycleTimeOutliersDtoTest`, and
`Tests.Acceptance.CycleTimeOutliers`. The port scenarios run through `ServicePortHarness` with no web host, so they cost
seconds, and they are the only tests that ask the metrics services for a verdict. `API.Integration`, `Dogfood`,
`Integration.Containers`, `Tests.Contracts` (the wire contracts start a host) and the live-connector categories are left
out. The first run's test project was built before the kill-round tests existed, so the first score is the Story's own.

| File | Killed | Survived | No coverage | Score (first → kill round) |
| --- | --- | --- | --- | --- |
| `Modules/FlowMetrics/Application/CycleTimeOutlierDefinition.cs` | 3 | 0 | 0 | 100 % → 100 % |
| `API/DTO/CycleTimeOutliersDto.cs` | – | – | – | Stryker.NET created no mutant in it |
| `Modules/FlowMetrics/Domain/XmRCalculator.cs` | 175 → 177 | 14 → 15 | 3 → 0 | 91.15 % → 92.19 % |

`CycleTimeOutlierDefinition.cs` also had 2 compile-error mutants and 1 ignored mutant, and `XmRCalculator.cs` had 2 and
53. They do not count towards the score.

### Closed by the kill round

- **`XmRCalculator.cs:35-37`, the refusal of `UpperNaturalProcessLimitOf` below 2 values (no coverage).** New rows in
  `XmRCalculatorTest`: `UpperNaturalProcessLimitOf_FewerThanTwoValues_RefusesAndSaysHowManyAreNeeded` (0 and 1 values
  give an `ArgumentException` that names `baselineValues` and both counts) and
  `UpperNaturalProcessLimitOf_TwoValues_IsTheirAveragePlus2Point66TimesTheirMovingRange` (10 and 12 give 16.32). The
  throw and the first half of the message are killed. The second half of the message survives as text (see below).

### Tests the whole-candidate review asked for

Each test failed against a deliberate break of the production line it covers. The line was then restored, and
`git status` shows no production file changed.

| Test | What it pins | Deliberate break, and the failure it gave |
| --- | --- | --- |
| `Acceptance/CycleTimeOutliers/CycleTimeOutliersAtTheEdgeOfTheirLimitsTest`, `A_named_Cycle_Time_that_starts_in_a_state_outside_the_workflow_has_no_limits_while_the_default_has_them` | A named Cycle Time that exists but names a state outside the Team's workflow gives `NoLimits`. The default Cycle Time over the same data gives `Limited`. | The invalid-definition branch of `BaseMetricsService.CycleTimeMeasure` falls back to the default Cycle Time: fails, `Limited` |
| same class, `A_range_without_a_configured_baseline_has_limits_from_14_days_on(13, False / 14, True)` | The minimum baseline length, `MinimumBaselineDays = 14` in `BaselineValidationService`: a 13-day range has no limits, a 14-day range has them | `MinimumBaselineDays` set to 13: the 13-day row fails, `Limited` |
| same class, `A_configured_baseline_has_limits_from_14_days_on(13, False / 14, True)` | The same threshold for a configured baseline | The same break: the 13-day row fails |
| `Contracts/CycleTimeOutliersNoLimitsWireContractTest` (new class on `WireContractTest`) | Team route without limits: 200, `status` "NoLimits", `upperNaturalProcessLimit` null, `outlierWorkItemIds` an empty array | The NoLimits arm of `CycleTimeOutliersDto.From` answers 0 instead of null: fails, `Expected: Null But was: Number` |
| `API/DTO/CycleTimeOutliersDtoTest` (2 tests) | `From` maps `Limited` to Ready with the unrounded limit and the ids, and maps `NoLimits` to NoLimits, null and an empty list | The same 0-instead-of-null break: fails. The definition threshold raised to 3: the Ready row fails |
| `Modules/FlowMetrics/Application/CycleTimeOutlierDefinitionTest` (3 tests) | Fewer than 2 baseline Cycle Times (0 or 1) give `NoLimits`, and exactly 2 give `Limited`. Only items strictly above the limit are outliers, in the order shown. An item on the limit (30) is not an outlier. | `Count < 2` changed to `< 3`: the 2-value row fails. `IsExceededBy` changed from `>` to `>=`: fails, because the 30-day item is listed |

**About review finding (a).** If only the `!owner.IsCycleTimeDefinitionValid(definition)` check is removed, the new
row stays green: that mutant is equivalent. A definition that is not valid names a state that is not in the workflow.
`NamedCycleTimeWindow` therefore finds no position for that state and gives no Cycle Time for any item. The baseline
then holds fewer than 2 Cycle Times, and the answer is `NoLimits` anyway. The check is a shortcut, not the only guard.
The row pins the outcome, and it fails for the wrong behaviour a change could bring in: a fallback to the default
Cycle Time.

### Accepted survivors (`XmRCalculator.cs`)

This Story changed only the new limit object (lines 11-26), `UpperNaturalProcessLimitOf` and `AverageMovingRange`
(lines 31-53), and the use of the limit in `Calculate` and `ApplyLargeChangeRule`. Every survivor below except line 37
is in sigma-rule code that the Story did not change.

| Line | Mutant | Reason |
| --- | --- | --- |
| 37 | second half of the refusal message to `""` | Message text. The test pins the counts in the first half. |
| 80 | `lnpl < 0` to `<= 0` | Equivalent: at 0 both arms give 0. |
| 139 | `lnpl > 0` to `>= 0` | Equivalent for Cycle Times: when the lower limit is 0, no value is below it, because a Cycle Time is never negative. |
| 157, 239 | `twoSigmaLower > 0`, `oneSigmaLower > 0` to `>= 0` | Equivalent for the same reason: no value is below a line at 0. |
| 375 (2 mutants) | `cause.Count > 0 ? sorted : []` to always sorted | Equivalent: when the set is empty, the sorted list is empty too. |
| 109 | `2 * sigma` to `2 / sigma` | A real gap in existing code: no test reaches a point that only the 2-sigma rule sets apart. Not this Story's change. |
| 181, 212, 263, 294 | `>` / `<` to `>=` / `<=` against a sigma line | A real boundary gap in existing code, hard to reach: a sigma line is `average ± mrBar / 1.128`, and whole-day values are exactly on it only in contrived data. Not this Story's change. |
| 224, 306, 339 | `values[j] < average` to `<=` | A real boundary gap in existing code: no test puts a point exactly on the average inside a signal window below it. Not this Story's change. |

The real gaps at lines 109 and 181-339 belong in a Continuous Improvement item for the process behaviour chart rules.
They are outside this Story's gate, which stopped after the first kill round.

### Not mutated

| File | Lines changed | Why not mutated, and how the change is covered |
| --- | --- | --- |
| `Modules/FlowMetrics/Application/BaseMetricsService.cs` | about 80 of 1424 | If the whole file were mutated, the score of the untouched metrics would hide the score of the change. `BuildCycleTimeOutliers`, `CycleTimeMeasure` and `CycleTimeObservations` are covered by the two oracle port classes and by `CycleTimeOutliersAtTheEdgeOfTheirLimitsTest`: invalid baseline, unknown and invalid named Cycle Time, closing order, and a named Cycle Time with limits. |
| `Modules/FlowMetrics/Application/TeamMetricsService.cs`, `PortfolioMetricsService.cs` | 12 of 983, 12 of 743 | One delegating method each, plus the injected rule. The port scenarios ask both, for a Team and for a Portfolio. |
| `API/TeamMetricsController.cs`, `API/PortfolioMetricsController.cs` | 12 of 680, 12 of 693 | One action each. `CycleTimeOutliersWireContractTest` pins the routes, the status and the JSON names, and `CycleTimeOutliersNoLimitsWireContractTest` pins the NoLimits shape. The start-after-end `BadRequest` branch is the controllers' shared guard and has no row specific to outliers. |
| `Models/UsageData/UsageDataEventName.cs`, `Program.cs` | an enum member, one registration | They contain nothing useful to mutate. |

## Frontend

Run 2026-11-17 against the worktree at `5af9db680` plus the kill-round tests. StrykerJS 10, `coverageAnalysis: perTest`,
`disableTypeChecks: false`, in place, incremental (`reports/mutation/stryker-incremental-6655.json`).

| run | file | score | killed | survived | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| first | `CycleTimeScatterPlotChart.tsx` | 87.21 % | 75 | 11 | 0 | |
| first | `outlierHeaderChip.tsx` | 88.89 % | 8 | 1 | 0 | |
| first | `outlierWording.ts` | 100.00 % | 8 | 0 | 0 | |
| first | `outliers.ts` | 100.00 % | 3 | 0 | 0 | |
| first | `MetricsService.ts` | 0.00 % | 0 | 0 | 8 | |
| first | `BaseMetricsView.tsx` | 14.29 % | 1 | 5 | 1 | |
| first | all | 78.51 % | 95 | 17 | 9 | 7 m 54 s |
| after the kill round | `CycleTimeScatterPlotChart.tsx` | 94.19 % | 81 | 5 | 0 | |
| after the kill round | `outlierHeaderChip.tsx` | 100.00 % | 9 | 0 | 0 | |
| after the kill round | `outlierWording.ts` | 100.00 % | 8 | 0 | 0 | |
| after the kill round | `outliers.ts` | 100.00 % | 3 | 0 | 0 | |
| after the kill round | `MetricsService.ts` | 100.00 % | 8 | 0 | 0 | |
| after the kill round | `BaseMetricsView.tsx` | 14.29 % | 1 | 5 | 1 | |
| after the kill round | all | **90.91 %** | 110 | 10 | 1 | 2 m 45 s (incremental) |

The gate (80 %) passes.

Scope: only the lines this story changed (`git diff 2c351d04a..HEAD`), as line ranges in `mutate`.
`CycleTimeScatterPlotChart.tsx`, `outlierHeaderChip.tsx`, `outlierWording.ts`, `outliers.ts`, `MetricsService.ts`
(`getCycleTimeOutliers`), `BaseMetricsView.tsx` (the outlier fetcher) and `widgetNodes.tsx` (the props handed to the
chart; it produced no mutants). Changed lines that hold only imports, interface members or comments are left out.
`src/models/Metrics/CycleTimeOutliers.ts` (types only) and `UsageDataService.ts` (one map entry) were not mutated:
they hold nothing worth mutating.

Configs: `stryker.6655.frontend.json` and `vitest.stryker.mutation.ts`. Copy the Vitest config into
`Lighthouse.Frontend/` for the run and remove it after it. Run from `Lighthouse.Frontend/`:

```bash
pnpm exec stryker run ../docs/feature/story-6655-outlier-toggle-and-unpl/mutation/stryker.6655.frontend.json --incremental
```

### Closed by the kill round

- **No-limits text with a renamed Cycle Time term** (review finding, pinned whatever the score).
  `CycleTimeScatterPlotChart.verdictQuestion.test.tsx`: with Cycle Time renamed to Lead Time, a `NoLimits` verdict shows
  "No outliers: the Lead Time process behaviour chart has no limits for this date range" in the header, and no
  "Cycle Time".
- **`ownerId ?? null`, `startDate?.toISOString() ?? null`, `endDate?.toISOString() ?? null` to `&& null` (lines
  255-257).** Same file: with the same fetcher, a new owner, then a new end, then a new start each ask the backend again.
- **`definitionId ?? "default"` to `&& "default"` (line 259).** Same file: picking one named Cycle Time and then
  another asks for each (`null`, `11`, `12`); the mutant served the first named answer for the second.
- **`if (!fetcher)` to `false` (line 266).** Same file: a chart handed no fetcher is ready at once and shows no chip.
- **`(item) => item.cycleTime` to `() => undefined` (line 426).** Same file: the list opened from the chip names the
  outliers longest first, whatever order they were plotted in.
- **`disabled || verdictPending` to `&&` (`outlierHeaderChip.tsx:40`).** New `outlierHeaderChip.test.tsx`: the chip is
  disabled when the chart disables it with the verdict landed, and usable when neither disables it.
- **`getCycleTimeOutliers` (8 no-coverage mutants).** `MetricsService.test.ts`: a team's default Cycle Time is asked
  without `definitionId`, a portfolio's named one appends `&definitionId=3`, and a failed request propagates its error.

### Accepted survivors

| File:line | Mutant | Reason |
| --- | --- | --- |
| Chart:196 | `lastFetcherId += 1` to `-= 1` | Equivalent: the ids count down instead of up and stay unique per fetcher. |
| Chart:253 | `"cycleTimeOutliers"` to `""` | Equivalent: the key prefix only has to differ from other queries, and no other query key starts with the owner kind. |
| Chart:259 | `"default"` to `""` | Equivalent: it only has to differ from every definition id, which are numbers. |
| Chart:266, 269 | status `"ready"` to `""` | Equivalent at runtime: the widget shell and the chart treat every status other than `"loading"` and `"error"` as ready. |
| BaseMetricsView:235-241 (5 survived, 1 no coverage) | the memoised outlier fetcher emptied, forced or its dependencies dropped | Not killed in this round. `BaseMetricsView.test.tsx` mocks the scatterplot without the fetcher prop, and its flow-metrics renders did not show the scatterplot within this pass without the blackout-period and query providers the loading suite builds. A test there that clicks a fetcher exposed by the mock and checks it is called with the Team's id, a 30-day range and the definition id would close all six. Owner: product (test gap), not the oracle. |
