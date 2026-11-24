# Mutation testing — 6656 (a named Cycle Time answers from its own population)

Run 2026-11-24 against `main`: the production code is frozen at `6a2d4e59d` (feature base `666e4a1c3`).
Gate is 80 % kill rate on both stacks.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Frontend (StrykerJS 10.1.1), first run | 83.12 % | 154 | 128 | 22 (+4 no coverage) | 0 | 4 m 46 s (+1 m 13 s dry run) |
| Frontend (StrykerJS 10.1.1), after the kill round | **92.86 %** | 154 | 143 | 7 (+4 no coverage) | 0 | 2 m 9 s (incremental, 31 re-tested) |
| Backend (Stryker.NET 5.1.0), changed lines, first run | 72.09 % | 43 | 31 | 9 (+3 no coverage) | 0 | 14 m 21 s (full run, 1045 mutants) |
| Backend (Stryker.NET 5.1.0), changed lines, after the kill round | **93.02 %** | 43 | 40 | 2 (+1 no coverage) | 0 | 2 m 47 s (kill round, 166 mutants) |
| Backend (Stryker.NET 5.1.0), whole files, first run | 48.83 % | 1325 | 646 | 398 (+280 no coverage) | 1 | 14 m 21 s |
| Backend (Stryker.NET 5.1.0), whole files, after the kill round | 49.66 % | 1325 | 657 | 389 (+278 no coverage) | 1 | — |

The backend gate is read on the changed lines: Stryker.NET mutates whole files, and the three metrics services
are 700 to 1,500 lines of which this fix changed about 300. Backend configs: `stryker.6656.backend.json` (the
full run) and `stryker.6656.backend.kill.json` (the kill round).

Configs: `stryker.6656.frontend.json`, `vitest.stryker.mutation.ts` (copied to
`Lighthouse.Frontend/vitest.stryker.6656.mutation.ts` for the run; that name is gitignored).

## Frontend

Run from `Lighthouse.Frontend/`:

```bash
cp ../docs/feature/bug-6656-named-cycle-time-own-population/mutation/vitest.stryker.mutation.ts vitest.stryker.6656.mutation.ts
pnpm exec stryker run ../docs/feature/bug-6656-named-cycle-time-own-population/mutation/stryker.6656.frontend.json --incremental
```

`mutate` holds only the lines changed since `666e4a1c3` (`git diff -U0`), and `cycleTimeMeasure.ts` whole,
since it is new. `coverageAnalysis` is `perTest` and the incremental file is
`reports/mutation/stryker-incremental-6656.json`, so the kill round re-tested 31 of 154 mutants.
`NamedCycleTime.ts`, `widgetNodes.tsx` and `widgetStatus.ts` are in `mutate` but their changed lines are
type declarations, a prop pass-through and a re-export, which produce no mutants.

| file | tested | killed | survived | no coverage | score |
| --- | --- | --- | --- | --- | --- |
| `components/Common/Charts/cycleTimeMeasure.ts` | 22 | 22 | 0 | 0 | 100 % |
| `components/Common/Charts/CycleTimeScatterPlotChart.tsx` | 95 | 85 | 6 | 4 | 89.47 % |
| `components/Common/WorkItemsDialog/workItemColumns.tsx` | 2 | 2 | 0 | 0 | 100 % |
| `hooks/useReportWidgetStatus.ts` | 10 | 10 | 0 | 0 | 100 % |
| `models/WorkItem.ts` | 6 | 6 | 0 | 0 | 100 % |
| `pages/Common/MetricsView/BaseMetricsView.tsx` | 2 | 1 | 1 | 0 | 50 % |
| `pages/Common/MetricsView/useNamedPercentilesScope.ts` | 8 | 8 | 0 | 0 | 100 % |
| `pages/Common/MetricsView/widgetViewData.ts` | 3 | 3 | 0 | 0 | 100 % |
| `services/Api/MetricsService.ts` | 6 | 6 | 0 | 0 | 100 % |

### Closed by this pass

Commit `3ef030881` (tests only):

- `CycleTimeScatterPlotChart.namedMeasure.test.tsx`
  - *keeps the previous measure's dots, dimmed, while the next measure's Work Items are on their way*:
    kills `placeholderData` → `undefined` and `data ?? null` → `data && null` (the default closed dots
    would show under the next measure's name).
  - *says the chart could not be loaded ...* now also asserts no dots are plotted at all: kills the
    error branch of `shownPopulation` being dropped (the default dots stayed mounted behind the message).
  - *names in its legend the types of the measure's own Work Items*: kills the type memo losing its
    `[shown]` dependency.
  - *asks again for the measure's Work Items when the owner or the date range changes*: kills the owner,
    start and end parts of the query key collapsing to `null`.
  - *goes back to the default Cycle Time when the selected measure is no longer offered*: kills
    `!answer || !definition` → `&&` (the chart crashed for one render on a removed definition).
- `useNamedPercentilesScope.test.ts`: the measure's own Work Items are fetched for the owner, range and
  definition; a late answer for an earlier definition is ignored (`isCurrent()` → `true`); the items are
  dropped on a snap back to Default after they arrived, and on an owner change.
- `widgetViewData.test.ts`: *lists nothing under the measure's name while its own Work Items are on their
  way* (`?? []`).
- `BaseMetricsView.test.tsx`: *the scatterplot fetches a named Cycle Time's own Work Items for this owner
  and date range*; the scatterplot mock gained a button that calls `onFetchNamedCycleTimeData`.

Each new test was checked red against its mutation by hand, then the line restored.

### Accepted survivors

- `CycleTimeScatterPlotChart.tsx:872`, `:877` (4, no coverage): the tooltip's `valueFormatter`. MUI X
  hides the chart's SVG and tooltip from jsdom, so nothing calls it in a component test. The wording it
  returns comes from `groupLabel`, which `cycleTimeMeasure.test.ts` pins (100 % there).
- `CycleTimeScatterPlotChart.tsx:334` (`"namedCycleTimeData"` → `""`): the key's first segment only
  separates this query from other queries in the same client; no other query has a key that would collide.
  Equivalent.
- `CycleTimeScatterPlotChart.tsx:356` (`{ answer: null, status: "ready" }` → `{}`, and `"ready"` → `""`)
  and `:361` (`"ready"` → `""`): the status only feeds `combineWidgetStatuses`, which reads anything that is
  neither `"error"` nor `"loading"` as ready, and a missing answer is falsy like `null`. Equivalent.
- `CycleTimeScatterPlotChart.tsx:650` (`if (!first) return` → never returns): a dot always holds at least
  one Work Item, so the list is never opened empty. Defensive guard, unreachable.
- `CycleTimeScatterPlotChart.tsx:659` (`definitionId === null` → always a scope): on the default measure
  the list would get `{ cycleTimeScope: null }` instead of no scope; `workItemColumns` finds no definition
  for `null` and shows the default Cycle Time either way. Equivalent.
- `BaseMetricsView.tsx:244` (`useCallback` dependencies → `[]`): the fetcher would keep the first date range.
  The scatterplot's query key also carries the owner and range, so a range change does ask again, but with
  the stale callback. Killing it means changing the page's date range through its pickers in a 7,000-line
  page test; the same dependency list on the neighbouring `onFetchNamedCycleTimePercentiles` is not pinned
  either. Accepted as a low-value wiring survivor.

### Observation (not a survivor)

`useNamedPercentilesScope` does not clear `scopedPercentileItems` when a new named definition is chosen, and
its snap back to Default does not bump the request generation. So an items answer that lands after a snap
back is still stored, and choosing a second measure keeps the first one's items until the second's arrive.
View Data reads the items only while a named scope is set, so the first case is not visible; the second
lists the earlier measure's Work Items under the later one's name for the length of one request. The
percentile values behave the same way (pre-existing). Not changed here: the gate forbids production edits.

## Backend

Run from `Lighthouse.Backend/Lighthouse.Backend.Tests/`:

```bash
dotnet tool restore
dotnet stryker --config-file ../../docs/feature/bug-6656-named-cycle-time-own-population/mutation/stryker.6656.backend.json
dotnet stryker --config-file ../../docs/feature/bug-6656-named-cycle-time-own-population/mutation/stryker.6656.backend.kill.json
```

`mutate` names every changed production file whole: `BaseMetricsService.cs`, `TeamMetricsService.cs`,
`PortfolioMetricsService.cs`, `NamedCycleTimeValue.cs`, `TeamMetricsController.cs` and
`PortfolioMetricsController.cs`. `ITeamMetricsService.cs` and `IPortfolioMetricsService.cs` changed only a
signature and produce no mutants, nor does the `NamedCycleTimeValue` record. "Total mutants will be tested" was
1045 of 24,117 created, which matches six files of about 5,000 lines between them.

`coverage-analysis` is `perTestInIsolation`. The `test-case-filter` takes the whole
`Tests.Modules.FlowMetrics` namespace, every `MetricsService` and `MetricsController` test, the
`CycleTimeDefinition` and `CycleTimeOutliers` tests, and the acceptance suite `Tests.Acceptance.NamedCycleTime`,
and leaves out `API.Integration`, `Dogfood` and `Contracts` (those start a web host).

**Why the acceptance suite is in the filter.** `Tests.Acceptance.NamedCycleTime` is the only suite that pins
the named population end to end at the application-service port: which Work Items a named Cycle Time shows,
the percentiles and widget over them, and the outliers judged among them. It starts no web host (ten
scenarios on `ServicePortHarness`, a database file of their own each) and is fast, so it costs the run little.
Left out, the core of the change would show as survivors that only it reaches.

A first run with a narrower filter (530 tests, without the rest of `Tests.Modules.FlowMetrics`) took
10 m 40 s and gave exactly the same figures, mutant for mutant; the recorded run is the wider one.

The kill round mutated the three services only, with the new test class alone in the filter. The "after"
figures count a mutant as killed when either run killed it.

### Per file

Changed lines are those of `git diff -U0 666e4a1c3 6a2d4e59d` in each file. Ignored mutants (Stryker's
"block already covered" filter) and compile errors are not counted, as in Stryker's own score.

| file | whole file, first run | whole file, after | changed lines, first run | changed lines, after |
| --- | --- | --- | --- | --- |
| `Modules/FlowMetrics/Application/BaseMetricsService.cs` | 68.48 % (289/422) | 70.14 % (296/422) | 74.19 % (23/31) | **90.32 %** (28/31) |
| `Modules/FlowMetrics/Application/TeamMetricsService.cs` | 28.52 % (79/277) | 29.24 % (81/277) | 50.00 % (2/4) | **100 %** (4/4) |
| `Modules/FlowMetrics/Application/PortfolioMetricsService.cs` | 25.13 % (50/199) | 26.13 % (52/199) | 50.00 % (2/4) | **100 %** (4/4) |
| `API/TeamMetricsController.cs` | 56.13 % (119/212) | 56.13 % (119/212) | 100 % (2/2) | 100 % (2/2) |
| `API/PortfolioMetricsController.cs` | 51.16 % (110/215) | 51.16 % (110/215) | 100 % (2/2) | 100 % (2/2) |
| `Models/Metrics/NamedCycleTimeValue.cs` | no mutants | | | |
| **total** | 48.83 % (647/1325) | 49.66 % (658/1325) | 72.09 % (31/43) | **93.02 %** (40/43) |

The whole-file scores are low because of code this bug did not touch. Most of those survivors are log message
and cache-key strings, and service paths that only `API.Integration` tests or other modules' tests reach. They
are "outside the change" and were not chased.

### Closed by this pass

Commit `1a9c63cd0` (tests only), `Tests/Modules/FlowMetrics/Application/NamedCycleTimeAnswerTest.cs`, unit tests
on the Team and Portfolio metrics services over repository doubles and a fake clock (5 December 2026):

- *The named items are listed by the day they reached the end state and by id on the same instant*: kills
  `OrderBy` → `OrderByDescending` and `ThenBy` → `ThenByDescending` in `EveryItemThatReachedTheEndState`
  (`BaseMetricsService.cs:433`). The acceptance suite compares the items as a set, so the order was free.
- *A named answer is remembered apart from the default answer and from other dates*: kills `"_Def_…"` → `""`
  in `CycleTimeMeasureCacheKeyPart` (`BaseMetricsService.cs:847`; the named data would be handed the default
  answer cached for the same dates) and the named data cache key → `""` (`TeamMetricsService.cs:418`).
- *The named percentiles widget shows no percentiles when nothing reached the end state in the dates*: kills
  `values.Count == 0 ? [] : …` → always `BuildPercentiles` in `PercentilesOrNone` (`BaseMetricsService.cs:412`),
  which would show four zero percentiles.
- *The outliers of one of several named measures are judged*: kills `Any` → `All` in `IsMeasurable`
  (`BaseMetricsService.cs:851`); with two measures the second would never get a limit.
- *A Portfolio remembers the named outliers apart from the default outliers*: kills the outliers cache key →
  `""` (`PortfolioMetricsService.cs:74`).
- *The default percentiles widget of a Team counts the items closed in the dates* and *… of a Portfolio counts
  the Features closed in the dates*: kill the removal of the default branch of `PercentilesForInfo`
  (`TeamMetricsService.cs:850`, `PortfolioMetricsService.cs:694`). No unit test reached that branch before.

### Accepted survivors (changed lines)

- `BaseMetricsService.cs:424` (no coverage, block removal of `return []` when the requested measure is unknown
  or invalid): without the early return, the filtered definition list is empty, so `NamedValuesForItem` yields
  nothing and the answer is empty all the same. The guard only saves loading the candidates. Equivalent.
- `BaseMetricsService.cs:847` (`"_Default"` → `""`): the default outliers key becomes
  `CycleTimeOutliers_<start>_<end>` while every named key still ends in `_Def_<id>`, and no other metric uses
  that prefix, so no two answers can share a key. Equivalent.
- `BaseMetricsService.cs:851` (`&&` → `||` in `IsMeasurable`): an unknown or invalid measure then passes the
  check, but `EveryItemThatReachedTheEndState` resolves only valid measures and returns no items, and the
  outlier definition answers "no limits" for fewer than two baseline values. The verdict is the same.
  Equivalent; the check is a short cut.

### Not mutated

Nothing left out: every changed production file is in `mutate`. The two interfaces and the record produce no
mutants.

## After the gate: one in-place fix

Commit `6286a444a` (after both runs) adds one statement to `useNamedPercentilesScope.ts`: the scoped View Data
items are cleared as soon as a named measure is chosen. That line's only mutant is removing it, and that is
exactly the RED the host observed for the regression test `e1b84fecd` before the fix (1 failed / 12 passed).
No Stryker re-run: the change is a single statement whose removal is already shown to be caught.
