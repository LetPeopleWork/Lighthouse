# Mutation testing — 6404 (Epic 6400 slice 01: the forecast lane and the Features band)

Gate is an 80 % kill rate on each stack the slice touched.

| stack | score | tested | killed | survived | timeout | wall clock |
| --- | --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.0.0), first run | 75.00 % | 192 | 130 | 42 | 14 (+6 no coverage) | 14 m 27 s |
| Backend, kill round (3 files narrowed) + `UsageDataEventShapes.cs` from the first run | **82.81 %** | 192 | 146 | 31 | 13 (+2 no coverage) | 13 m 43 s |
| Frontend (StrykerJS 10.0.0), first run | 93.26 % | 89 | 83 | 6 | 0 | 5 m 38 s |
| Frontend, incremental re-run after the kill round | **96.63 %** | 89 (6 reused) | 86 | 3 | 0 | 5 m 39 s |

Configs: `stryker.6404.backend.json` (first run), `stryker.6404.backend.kill.json` (kill round), `stryker.6404.frontend.json` and `vitest.stryker.mutation.ts`. The vitest config is copied into
`Lighthouse.Frontend/` (gitignored there); the run is from `Lighthouse.Frontend/`:

```
TZ=Europe/Zurich pnpm exec stryker run ../docs/feature/epic-6400-fast-with-many-features/mutation/stryker.6404.frontend.json --incremental
```

`coverageAnalysis: "perTest"`, incremental file `reports/mutation/stryker-incremental-6404.json`
(gitignored), concurrency 2, `inPlace`, `disableTypeChecks: false`, root-anchored `ignorePatterns`.
The re-run reused only 6 results because the kill round edited the spec that covers nearly every mutant.

## Backend

Run 2026-10-25 on local `main` (HEAD `77154a43e` for the first run). Stryker.NET 5.0.0, run from
`Lighthouse.Backend/Lighthouse.Backend.Tests/`, `coverage-analysis: perTestInIsolation`, concurrency 4.
Gate: 80 % kill rate over the included set. The kill-round tests and the config landed in `1da2ba85a`.

| run | score | mutants scored | killed | survived | timeout | no coverage | wall clock |
| --- | --- | --- | --- | --- | --- | --- | --- |
| First run, 4 files (`stryker.6404.backend.json`) | 75.00 % | 192 | 130 | 42 | 14 | 6 | 14 m 27 s |
| Kill round, 3 files narrowed | 81.25 % | 176 | 130 | 31 | 13 | 2 | 13 m 43 s |
| **Included set after the kill round** (kill round + `UsageDataEventShapes.cs` from the first run) | **82.81 %** | 192 | 146 | 31 | 13 | 2 | |

### Scope

`mutate`, whole files (DEVOPS decision): `ForecastLane.cs`, `ForecastUpdater.cs`, `ForecastService.cs`,
`UsageDataEventShapes.cs`.

Not mutated:

- `StartupBanner.cs` — the slice adds one line, pinned by `StartupBannerForecastLaneLineTest`.
- `UsageDataController.cs` and `PostHogUsageDataPublisher.cs` — one line each, pinned by the controller and
  publisher tests.
- `Program.cs` — composition root; the lane's registration is exercised by the acceptance suite. (Stryker lists
  it in the report with every mutant Ignored or CompileError; it contributes nothing to the score.)
- The `UsageDataFeaturesHeld` enum — no behaviour to mutate.
- The band converter — a one-line class; the controller tests read through it.

`test-case-filter` (first run): the forecast unit tests (`Tests.Services.Implementation.Forecast.`), the update
services (`Tests.Services.Implementation.BackgroundServices.Update.`), `UsageDataEventShapesTests`, the usage-data
service tests (`Tests.Services.Implementation.UsageData.`), `UsageDataControllerTests` and `StartupBanner*`,
excluding Dogfood, `API.Integration`, `Integration.Containers` and the live-connector categories: 781 tests,
5 s as a plain run. The kill round kept only the forecast and update-service parts and dropped
`UsageDataEventShapes.cs`, which had no survivor.

### Per file

| file | first run | kill round | killed | survived | timeout | no coverage |
| --- | --- | --- | --- | --- | --- | --- |
| ForecastLane.cs | 68.00 % (17/25) | **84.00 %** (21/25) | 13 | 4 | 8 | 0 |
| ForecastUpdater.cs | 78.33 % (47/60) | **91.67 %** (55/60) | 52 | 3 | 3 | 2 |
| ForecastService.cs | 70.33 % (64/91) | 73.63 % (67/91) | 65 | 24 | 2 | 0 |
| UsageDataEventShapes.cs | **100 %** (16/16) | not re-run | 16 | 0 | 0 | 0 |

Kill-round counts per file; `UsageDataEventShapes.cs` from the first run. `ForecastService.cs` stays below 80 %
on its own: 21 of its 24 survivors are log statements and log text in a file the slice touched in two places
(the timings on the outcome and the scheduler the runs follow).

### Survivors killed (15)

| mutant | test that now kills it | what it pins |
| --- | --- | --- |
| ForecastLane.cs:77 `return` removed; :94 `false ? left : Zero`; :94 `left < Zero` | `ForecastLaneTest.Disposing_the_lane_waits_for_a_busy_thread_that_ends_in_time_and_reports_nothing` | Dispose waits for a busy lane thread that ends within the grace period, joins it, and reports nothing |
| ForecastLane.cs:93 `-` → `+` | `ForecastLaneTest.Disposing_the_lane_waits_for_all_its_busy_threads_together_rather_than_one_after_another` | two stuck threads share one grace period, so Dispose returns in about 5 s, not 5 s per thread, and reports once |
| ForecastUpdater.cs:165 `?? None` keeps only the right; :178 `!` dropped; :180 log removed; :180 log text emptied | `ForecastUpdaterTest.Update_ShouldEndWithoutAFailureAndSaySo_WhenLighthouseStopsDuringTheForecast` | Lighthouse stopping cancels the round; the refresh is recorded as cancelled, the queue sees no failure, and an Information line says Lighthouse is stopping |
| ForecastUpdater.cs:178 `&&` → `\|\|` | `ForecastUpdaterTest.Update_ShouldRecordAFailureNotACancel_WhenACancellationArrivesThatNobodyAskedFor` (one assertion added) | a cancellation nobody asked for, while Lighthouse keeps running, still reaches the queue as a failure |
| ForecastUpdater.cs:43 `return` removed; :120 `\|\|` → `&&` | `ForecastUpdaterTest.TriggerUpdate_ShouldNotAskForASecondForecast_WhenOneIsAlreadyHeld` | a forecast already held for the Portfolio means a new request runs nothing |
| ForecastUpdater.cs:103 `stillToClear.Add(forecastKey)` removed | `ForecastUpdaterTest.Update_ShouldHoldTheWaitingForecastAgain_WhenAForecastJoinedTheQueueWhileItWaited` | a waiting forecast whose teams cleared, but which finds a forecast queued meanwhile, holds again on that forecast instead of running |
| ForecastService.cs:119 `Computed` initialiser emptied | `ForecastServiceTest.UpdateForecastsForPortfolio_ComputedForecast_TellsHowLongTheSaveTook` | a computed run reports how long its save took |
| ForecastService.cs:108 `ConfirmedUnchanged` initialiser emptied | `ForecastServiceTest.UpdateForecastsForPortfolio_ConfirmedUnchanged_TellsHowLongTheGatherTook` | a skipped run reports how long its gather took |
| ForecastService.cs:410 block removed | `ForecastServiceTest.UpdateForecastsForPortfolio_EachTeamsForecastSaysWhatItsThroughputLeftOutAndWhetherItWasEnough` | a Team's forecast carries its throughput status: filter applied, what was left out, whether the data was enough |

### Survivors accepted (33)

| line | mutant | why it is accepted |
| --- | --- | --- |
| ForecastLane.cs:76 | `waiting.Dispose()` removed | releases the queue's wait handle only; work handed over afterwards is refused either way, so no test can tell |
| ForecastLane.cs:81 | warning text emptied | log text; the warning itself is pinned (`Times.Once`) |
| ForecastLane.cs:94 | `true ? left : Zero` | differs only when the shared wait overshoots by a whole millisecond or more; `Thread.Join` truncates to milliseconds, so a sub-millisecond overshoot is clamped either way. Pinning it would need a timing test at sub-millisecond resolution |
| ForecastLane.cs:94 | `>` → `>=` | equivalent: at exactly zero both branches return zero |
| ForecastUpdater.cs:147, :152 (no coverage) | `NotSupportedException` message emptied | exception text in the periodic-refresh overrides; the forecast updater has no periodic refresh, so nothing calls them |
| ForecastUpdater.cs:220 | `stopwatch.Stop()` removed | the elapsed time is read right after; the difference is microseconds |
| ForecastUpdater.cs:327 (×2) | warning removed / text emptied | log only; the forecast's own failure is pinned by `Update_ShouldStillReportTheForecastsOwnFailure_WhenForgettingTheInputsFails` |
| ForecastService.cs:35, 36, 45, 60, 67, 72, 94, 99, 107, 151, 355, 357 (21) | log statement removed / template emptied | log text and Debug/Information lines only |
| ForecastService.cs:221 | empty-plan short-circuit removed | equivalent: a plan with no rows runs no row and returns no start forecasts anyway |
| ForecastService.cs:313, :334 | `OrderBy` → `OrderByDescending` | equivalent: every reader of these counts re-sorts them (`ForecastBase.SimulationResult` is a `SortedDictionary`), so the order they are written in leaves no trace |

### Configs

- `stryker.6404.backend.json` — the first run (committed).
- `stryker.6404.backend.kill.json` — the kill round: the same config with `mutate` narrowed to `ForecastLane.cs`, `ForecastUpdater.cs` and
  `ForecastService.cs`, and `test-case-filter` narrowed to
  `(FullyQualifiedName~Tests.Services.Implementation.Forecast.|FullyQualifiedName~Tests.Services.Implementation.BackgroundServices.Update.)`
  plus the same exclusions.

## Frontend

Frontend code run against `main` @ `6a6e2ea81` (the slice's last frontend commit).

### What was mutated

Whole files, because the slice wrote them: `src/models/UsageData/featuresHeldOf.ts` and
`src/services/UsageData/usageDataFeaturesHeld.ts`.

Line ranges, one entry per range, from `git diff -U0 64289af73^..HEAD` (import-only hunks skipped):
`usageDataEvents.ts:77-79`, `:92-109`, `:111-119`, `:122-132`, `:163-163`; `TeamDetail.tsx:131-136` and
`PortfolioDetail.tsx:100-108` (the `useFeaturesHeldForUsageData` call on each page).

The log confirms 5 files instrumented with 89 mutants, and every file appears in the per-file table.

### Not mutated

`usageDataBuffer.ts` and `UsageDataService.ts`: each carries one optional `featuresHeld` field through
untouched. The band tests assert that field on what is handed in, so a broken pass-through fails them.

### Suites run

`featuresHeldOf.test.ts`, `usageDataEvents.featuresHeld.test.tsx` (renders the real Team and Portfolio
detail pages), `usageDataEvents.test.ts`, `usageDataReporter.test.ts` and
`SystemSettingsTab.usageDataHandIn.test.tsx`; the last two mount the tab-opening detector too.

### Per file

| file | first run | after kill round | killed | survived |
| --- | --- | --- | --- | --- |
| `featuresHeldOf.ts` | 95.65 % | 95.65 % | 22 | 1 |
| `usageDataFeaturesHeld.ts` | 91.89 % | 97.30 % | 36 | 1 |
| `usageDataEvents.ts` (slice lines) | 89.47 % | 94.74 % | 18 | 1 |
| `TeamDetail.tsx` (hook call) | 100.00 % | 100.00 % | 5 | 0 |
| `PortfolioDetail.tsx` (hook call) | 100.00 % | 100.00 % | 5 | 0 |

### Closed by this pass

Three tests added to `usageDataEvents.featuresHeld.test.tsx`. Each was also checked by applying its
mutant by hand: the new test, and only it, fails.

- **`usageDataEvents.ts:126`** `if (isStillSending.current)` → `if (true)`. "sends nothing that was still
  waiting for its Team once the person withdraws": a withdrawal empties the buffer, and without the guard
  the opening still waiting for its Team went into it afterwards and left at the next hand-in, because the
  browser keeps its token after saying no.
- **`usageDataFeaturesHeld.ts:75`** `loadedId !== undefined &&` → `true &&`. "goes as Unknown at the next
  hand-in when the Portfolio fails to load, while the person stays": without the clause a page whose
  owner failed to load never hands in Unknown, so its opening waits until the person leaves. The earlier
  failed-load test hid the tab, and hiding sends Unknown by another route.
- **`usageDataFeaturesHeld.ts:19`** the key `${kind}:${id}` → `""`. "never takes the band a Portfolio handed
  in for a Team page, nor for another Portfolio": with one shared key every page read whichever band was
  handed in last.

### Accepted survivors

- **`featuresHeldOf.ts:19`** `count === undefined` → `false`. Equivalent: `Number.isInteger(undefined)` is
  false, so the next clause returns Unknown for the same input. The clause is there for the type checker.
- **`usageDataEvents.ts:96`** `stopWaiting?.()` → `stopWaiting()`. Unreachable: `noticeWith` is only
  reached through the listener, which is registered after `stopWaiting` is assigned, and the first call
  removes the listener and clears the fallback, so it never runs a second time.
- **`usageDataFeaturesHeld.ts:43`** `key === undefined ? undefined : held.get(key)` → `held.get(key)`.
  Equivalent: `Map.get(undefined)` returns `undefined` when no key is `undefined`, and none ever is.
