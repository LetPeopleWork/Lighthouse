# Mutation testing — 6563 (Within SLE and Within SLE Over Time)

Run 2026-11-27 against the worktree @ `ecf3ba745`. Gate is 80 % kill rate on both stacks.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend first run (Stryker.NET 5.1.0) | **82.52 %** | 246 | 203 | 30 (+13 no coverage) | 0 | 20 m 20 s |
| Backend after kill round (first run + narrowed re-run of `ServiceLevelExpectation.cs`) | **86.59 %** | 246 | 213 | 20 (+13 no coverage) | 0 | 3 m 19 s (kill round) |
| Frontend first run (StrykerJS) | **72.87 %** | 258 | 186 | 61 (+9 no coverage) | 2 | 17 m 40 s |
| Frontend after kill round (full run, `--incremental --force`) | **81.40 %** | 258 | 208 | 48 | 2 | 20 m 34 s |

Configs: `stryker.6563.backend.json`, `stryker.6563.frontend.json`, `vitest.stryker.mutation.ts`.
Backend runs from `Lighthouse.Backend.Tests/`. Frontend runs from `Lighthouse.Frontend/`. Copy
`vitest.stryker.mutation.ts` to `Lighthouse.Frontend/` for the run, because the frontend config names it
by a path relative to the run directory, and delete the copy afterwards.
`pnpm exec stryker run ../docs/feature/story-6563-sle-attainment/mutation/stryker.6563.frontend.json --incremental`

In the backend, "tested" is the number of mutants the score is worked out on: killed, survived and no
coverage together. Compile errors and ignored mutants are left out.

## Backend

The test filter selects the fast service-port acceptance tests in `Acceptance.WithinSle` and the unit
tests under `Tests.Modules.FlowMetrics`, 812 tests in all. The acceptance tests are in because they are
the tests that pin this feature at the application-service port. They start no web host. The filter
leaves out `API.Integration`, `Dogfood` and the live connector categories. Coverage capture under
`perTestInIsolation` took 12 of the 20 minutes.

Per file in the first run (killed / survived / no coverage):

| file | killed | survived | no coverage |
| --- | --- | --- | --- |
| OverTimeHistoryFiller.cs | 81 | 12 | 0 |
| WithinSleSnapshotWriter.cs | 24 | 3 | 1 |
| RetentionEdge.cs | 23 | 1 | 0 |
| OverTimeFillTarget.cs | 16 | 0 | 0 |
| WithinSleInfoQuery.cs | 12 | 7 | 1 |
| ServiceLevelExpectation.cs | 10 | 4 | 7 |
| WithinSleSnapshotRepository.cs | 10 | 2 | 0 |
| WithinSleRecordingHandler.cs | 8 | 0 | 2 |
| WithinSleShare.cs | 8 | 0 | 0 |
| WithinSleOverTimeSeriesQuery.cs | 7 | 0 | 2 |
| WithinSleFamilies.cs | 3 | 1 | 0 |
| GapAskingWithinSleOverTimeSeriesQuery.cs | 1 | 0 | 0 |

`WithinSleFamilyReader.cs` produced no mutants that could be tested.

### Closed by this pass

- **`ServiceLevelExpectation.cs`**: 4 survivors and 7 mutants with no coverage became 20 killed and 1
  survived (narrowed kill round: `mutate` only this file, filter `ServiceLevelExpectationTest` plus
  `Acceptance.WithinSle`).
  - The survivors were in `Of`'s bounds `percentile is < 1 or > 100 || days < 1`. The mutations were
    `<=` / `>=` and `||` → `&&`.
  - The mutants with no coverage were in `Equals`, `Equals(object)` and `GetHashCode`.
  - The new `Modules/FlowMetrics/Domain/ServiceLevelExpectationTest.cs` pins both edges of the valid
    range (1 and 100 %, 1 day). It also pins the values just outside it, a percentile out of range with
    a valid day count, and value equality.
  - The one survivor left is `GetHashCode` with its block removed (it returns 0). That is equivalent
    for correctness: equal values still have equal hashes.

### Accepted survivors

- **`OverTimeHistoryFiller.cs` (12)**
  - 6 are log-message strings and removed `LogInformation` / `ReportStandingDownForMaintenance` calls
    (lines 154, 189, 256, 272, 273, 307, 308, 417).
  - `:65` drops `FullMode = Wait` from the bounded channel options. `Wait` is the default mode, so
    this mutant is equivalent.
  - `:270` changes `>=` to `>` on the pass time budget. This boundary can only be hit with an exact
    elapsed-time match, which no test can arrange.
  - `:367` reorders the null-coalescing `percentilesFailure ?? limitsFailure ?? withinSleFailure`
    (2 mutants). It needs two families to fail on the same day. That reports one failure or another,
    and both are logged the same way.
  - The file was mutated whole-file. The feature changed 22 of its 478 lines (the third family). Most
    of its survivors are in earlier code.
- **`WithinSleInfoQuery.cs` (7 + 1 no coverage)**
  - 4 survivors mutate the date-range strings of the comparison detail rows (lines 52, 53).
  - `:44` changes the `"none"` direction label of the empty comparison.
  - `:20` changes `AddDays(-1)` to `AddDays(+1)` for the previous window's end. That only shifts the
    comparison row, which no oracle reads in detail.
  - `:36` drops `?? []`. The family readers never return null.
  - The mutant with no coverage is the message of an `ArgumentOutOfRangeException` in a total switch.
  - Missing test, low value: the comparison rows' dates and direction.
- **`WithinSleSnapshotWriter.cs` (3 + 1 no coverage)**
  - `:22` and `:62` are statement removals of an early `return` and a `continue`. They write a row
    the next statement would also skip, so they are equivalent.
  - `:113` changes `&&` to `||` in the stored-row lookup. It needs two owners recorded on the same
    day. Missing test, low value.
  - The mutant with no coverage is a `continue` on a guard the callers never reach.
- **`WithinSleSnapshotRepository.cs` (2)**
  - `:18` changes `&&` to `||` in the owner filter. It needs two owners in the store at once. Missing
    test, low value.
  - `:32` reverses `OrderBy`. The query re-sorts the series by day.
- **`RetentionEdge.cs:59`** changes `>` to `>=`. When the two days are equal, both sides of the
  conditional are the same day, so this mutant is equivalent.
- **`WithinSleFamilies.cs:28`** changes `cycleTime >= 0` to `> 0`. A cycle time of 0 days never occurs,
  because a closed item counts at least one day. Equivalent.
- **No coverage in `WithinSleRecordingHandler.cs` (2), `WithinSleOverTimeSeriesQuery.cs` (2) and the
  invariant message of `WithinSleSnapshotRepository`.** These are early `return`s on defensive guards
  and the text of an invariant exception that the public port cannot reach.

### Not mutated

- `API/PortfolioMetricsController.cs` and `API/TeamMetricsController.cs`. They have about 720 lines
  each, of which this feature changed about 40: two thin endpoints that pass straight through to the
  queries above. Mutating them whole-file would measure years of earlier endpoints.
- Left out because they hold wiring or configuration, not logic: `Data/LighthouseAppContext.cs`,
  `Program.cs`, `FlowMetricsModule.cs` (DI registration).
- Left out because they are DTOs, enums, an entity or interfaces with no logic: `WithinSleDayDto`,
  `WithinSleInfoDto`, `WithinSleOverTimeDto`, `WithinSleVerdict`, `UsageDataEventName`,
  `WithinSleSnapshot`, `WithinSleSnapshotOrigin`, the `I*` interfaces.

## Frontend

`mutate` is scoped to the changed line ranges of 19 files.

Left out:
- type-only model files (`WithinSleInfo.ts`, `WithinSleOverTime.ts`);
- imports and interface members;
- the `UsageDataService` event-name constants;
- `OverTimeMetricsService.ts` and `FeatureSizeMetricsService.ts`. Their code was moved out of
  `MetricsService.ts`, not written new: `buildPercentilesOverTimeQuery` was at `MetricsService.ts:267`
  at `455c1a88f`.

### Closed by this pass

The new test files below are none of them oracle files.

- `services/Api/MetricsService.withinSle.test.ts`: the two Within SLE calls had no coverage (6
  mutants). It pins both URLs and that the answer is returned.
- `pages/Common/MetricsView/withinSleLines.ordering.test.ts`: days sent out of order are drawn in
  calendar order, and a timestamp is cut to its day (`withinSleLines.ts:25`, `:26`).
- `pages/Common/MetricsView/withinSleFooterAndMetadata.test.ts` pins:
  - the neutral footer before the answer arrives (`widgetFooters.ts:208-210`);
  - the TooFew and Measured tips (`ragRulesTimeAndAge.ts:153`, `:161`);
  - Within SLE Over Time's `none` trend policy and its empty fetch requirements (`categoryMetadata.ts:123`,
    `:287`).
- `pages/Common/MetricsView/WithinSleWidget.oneVerdict.test.tsx`: the widget shows only the verdict it
  was given (`WithinSleWidget.tsx:34`, `:52`).
- `pages/Common/MetricsView/WithinSleShareMark.test.tsx`: solid and open markers, a hidden series, and
  the empty default context (`WithinSleShareMark.tsx:6`, `:12`, `:16`, which had 3 mutants with no
  coverage).

A first incremental re-run reported 79.07 %. That run reused stale "survived" results for the
module-level constants in `categoryMetadata.ts`, so the score recorded above comes from a full
`--incremental --force` run.

### Accepted survivors (48)

- **Styling and chart-only props (about 40).**
  - `WithinSleOverTimeWidget.tsx` (29):
    - the `sx` / `style` object literals and their strings (lines 71, 87, 90, 127, 129, 132-137);
    - the chart's `label`, `curve`, `showMark`, `scaleType` and `slots` props (lines 91-108). The
      Vitest specs mock `@mui/x-charts`, so these props never reach anything a test can read;
    - `:145`, the toggle group's `aria-label`;
    - `:152`, `selected` forced to `true`;
    - `:57`, the `next === horizon` early return;
    - `:64` (2), the `anyTooFew` note. The oracles assert the switch and the note through the
      rendered view. The mutants left are either equivalent under the mocked chart (`:57`: switching
      to the horizon already shown runs the same query key) or would need a direct render of the
      widget outside `BaseMetricsView` (`:64`, `:145`, `:152`). Missing test, recorded.
  - `WithinSleWidget.tsx` (14): all `sx` literals and their strings. jsdom computes no layout.
- **`useMetricsData.ts:620`, `:625`**: the fetch label and the error-log message.
- **`useWithinSleOverTime.ts:33`**: the first element of the TanStack query key. The key stays unique
  through its other parts.
- **`WidgetShell.tsx:599`**: `viewData?.onOpened?.()` becomes `viewData.onOpened?.()`. Every widget that
  opens a list passes `viewData`, so the optional chain only guards a shape that never reaches this line.
- **`withinSleLines.ts:33`**: `if (stored.length === 0) return lines;` forced to `false`. With no
  days, `last` is `undefined`, the loop runs zero times and the same empty lines come back. Equivalent.
