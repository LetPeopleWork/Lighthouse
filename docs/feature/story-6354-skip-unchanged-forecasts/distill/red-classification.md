# RED classification — Story 6354

2026-10-23, after the review-gate revisions. Every pending test was un-skipped once (not committed) and run: the
backend classes with `dotnet test --filter` and a TRX log, the frontend file with `pnpm vitest run`. All 89 failed;
none failed on setup, seeding, routing or types. The 8 new active guards passed in the same run.

A throwaway probe class then disabled only the "a quiet round skipped" precondition and the explicit `skipped` reads,
to prove the rest of each acceptance scenario reaches its own assertion today: every change in the re-forecast cases
moved what the forecast reads and drew numbers; the walking skeleton's and the item-left case's lines read
`mode=Delta | scanned=5 | fetched=0` (the item-left round drew 12400 numbers against 12800); the presses answered 2xx
and produced a forecast, press-again and refresh-all each produced two rows; the held forecast was held and stopped
or let go; the restart step ran the seeders; Loyalty's Feature is in Payments Platform's forecast (13600 numbers on
Atlas's round); the Task Manager poll reached `Completed`. So a scenario still red after DELIVER fails on behaviour,
not on the harness.

Classes: **MISSING_FUNCTIONALITY** — the assertion fires because the behaviour is not built; **SCAFFOLD** — the call
reaches a `__SCAFFOLD__` body that throws `NotImplementedException` (the repository's RED convention), so the test
body is reached and only the production code is missing; **MISSING_SCHEMA** — the column the migration adds is absent.

Paths: backend under `Lighthouse.Backend/Lighthouse.Backend.Tests/`, frontend under `Lighthouse.Frontend/src/`.

## Slice 01

### `API/Integration/SkipUnchangedForecasts/` (five classes, 32 pending)

The common precondition message is "A refresh of FSS that changed nothing the forecast reads still simulated 12800
numbers" (`quiet.NumbersDrawn` expected 0): no skip exists yet to be undone.

| Class | Scenario | Class of RED | Failure seen |
|---|---|---|---|
| `Slice01QuietRoundsSkipScenarios` | A background round that changed nothing skips the forecast and says so | MISSING_FUNCTIONALITY | "Nothing the forecast reads changed since 10:00: the forecast simulated 12800 numbers"; line ends `forecast=Nms`; row carries no `skipped` |
| | A Portfolio with nothing confirmed yet is forecast in full and the next quiet round skips | MISSING_FUNCTIONALITY | "Nothing had been confirmed for this Portfolio, yet the refresh history records no computed forecast" (no `skipped: false`) |
| `Slice01InputChangeScenarios` | A background round after an input changed forecasts in full ×13 | MISSING_FUNCTIONALITY | precondition (12800 numbers) |
| | …after an item left a Team's query… though it downloaded nothing | MISSING_FUNCTIONALITY | precondition (12800 numbers) |
| | …after a Feature of another Portfolio its Teams work on changed… | MISSING_FUNCTIONALITY | precondition (27200 numbers: Payments Platform and Loyalty both forecast) |
| `Slice01UnfinishedRunsAndRestartScenarios` | After a forecast that failed / was stopped / after the instance starts again, the next quiet round forecasts in full | MISSING_FUNCTIONALITY | precondition (12800 numbers) ×3 |
| `Slice01UserPressScenarios` | A press forecasts in full even when nothing changed ×6 | MISSING_FUNCTIONALITY | precondition (12800 numbers) |
| | Update forecast pressed while a forecast runs ×2; pressed again while the first press's forecast runs; refresh-all with forecasts under way | MISSING_FUNCTIONALITY | precondition (12800 numbers) ×4 |
| `Slice01AnnouncedAndDailyScenarios` | A skipped forecast is still announced…; the first round of a day forecasts in full… | MISSING_FUNCTIONALITY | "Nothing the forecast reads changed since 10:00: … 12800 numbers"; precondition |

### `Services/Implementation/Forecast/ForecastInputFingerprintTest.cs` (32 pending)

| Test | Class | Failure seen |
|---|---|---|
| Names every input the simulation is handed and nothing else | MISSING_FUNCTIONALITY | `RegisteredMembers` is empty, expected equivalent to the 13 members |
| Every simulation limit is one of its inputs | MISSING_FUNCTIONALITY | unregistered: `Trials`, `MostDaysOneSimulatedRunMayCover` |
| Every input has a case above that changes it on its own | MISSING_FUNCTIONALITY | "The fingerprint names no inputs at all." |
| Same inputs twice; short lower-case digest; another order; Turkish culture; one input changed on its own ×20 (incl. the premium licence); something the simulation is not handed leaves it alone ×5 | SCAFFOLD | `NotImplementedException: The fingerprint of 3 Features is not written yet` (403 for the digest case) |

### `Services/Implementation/Repositories/ForecastConfirmationRepositoryRelationalTest.cs` (slice 01 part, 8 pending)

All 8 SCAFFOLD: `NotImplementedException: Recording a Portfolio's forecast inputs is not written yet (Portfolio 1)`,
4242 for the gone Portfolio, `(1 Portfolios)` where the first call is a forget. Seeding, and in the stale-copy case the read of the old copy, ran first. The forget-all
case reaches the record scaffold before the forget-all one.

### `Services/Implementation/RefreshRoundSummaryLineSkippedForecastTest.cs` (5 pending)

| Test | Class | Failure seen |
|---|---|---|
| A Team round whose forecast was skipped ends with the skip and its time | MISSING_FUNCTIONALITY | line ends `… \| forecast=812ms` |
| A round that only forecast names the skip where the forecast time goes | MISSING_FUNCTIONALITY | line reads `… \| forecast=812ms \| duration=812ms …` |
| A skip reported into a refresh round reaches the line as a skip | MISSING_FUNCTIONALITY | `summary.ForecastSkipped` was `False` |
| A round in which one forecast computed and a later one was skipped reads as computed | MISSING_FUNCTIONALITY | "The round's forecast time is not the time of the forecast that computed": 812, expected 269719 |
| A round in which every forecast was skipped reads as skipped | MISSING_FUNCTIONALITY | `ForecastSkipped` was `False` |

### `Integration/Containers/RefreshLogSkippedColumnMigrationTests.cs` (2 pending)

| Test | Class | Failure seen |
|---|---|---|
| On SQLite a row written by the previous release reads as a forecast that computed | MISSING_SCHEMA | "The refresh log has no column that marks a skipped forecast yet: no such column: log.Skipped" |
| On Postgres … | MISSING_SCHEMA | "…: 42703: column "Skipped" does not exist" |

The SQLite case passed on the first probe: SQLite reads an unknown double-quoted name on its own as a string literal,
so `SELECT "Skipped"` returned the text `Skipped`, which read as a value. The column is now read through a table alias
(`log."Skipped"`), which SQLite cannot take for a literal, and the case fails as it should.

### `pages/Settings/SystemInfo/RefreshHistorySection.skippedForecasts.test.tsx` (7 pending)

| Scenario | Class | Failure seen |
|---|---|---|
| Averages, shortest and longest over the forecasts that computed | MISSING_FUNCTIONALITY | Avg Duration received `102.46 s` (all eight runs) |
| Says how many of the runs were skipped | MISSING_FUNCTIONALITY | `Unable to find … /^Skipped \(nothing changed\): 5 of the last 8 runs$/` |
| Says the last run was skipped, and when | MISSING_FUNCTIONALITY | Last Run holds the time only, not `<time> · skipped (nothing changed)` |
| Shows a dash for the durations when every run was skipped | MISSING_FUNCTIONALITY | Avg/Min/Max show a duration, not `—` |
| Gap in the duration line at each skip | MISSING_FUNCTIONALITY | received `[240.1, 268.2, 305.9, 0.9, 1.1, …]`, expected `null` at each skip |
| All view average and longest over computed runs | MISSING_FUNCTIONALITY | Avg Duration received the average of all ten runs, not `169.64 s` |
| Each entity's bar averages computed runs | MISSING_FUNCTIONALITY | `expected 102.46 to be 271.4` |

## Slice 02

| Test | Class | Failure seen |
|---|---|---|
| `Slice02ConfirmedNowScenarios.A_skipped_forecast_reads_as_confirmed_at_that_round_with_its_dates_unchanged` | MISSING_FUNCTIONALITY | "Nothing the forecast reads changed since 10:00: … 12800 numbers" |
| `Slice02ConfirmedNowScenarios.A_skipped_forecast_also_confirms_a_Feature_of_another_Portfolio_its_Teams_work_on` | MISSING_FUNCTIONALITY | "… 13600 numbers" |
| `ForecastConfirmationRepositoryRelationalTest.Confirming_stamps_every_forecast_of_the_named_Features_and_changes_no_value` | SCAFFOLD | `NotImplementedException: Confirming the forecasts of 1 Features is not written yet` (seeding and the first read ran) |

## Active guards (green today, must stay green)

- `ForecastInputFingerprintTest.The_dependency_the_cases_start_from_is_one_the_forecast_waits_on`
- `RefreshRoundSummaryLineSkippedForecastTest.A_computed_forecast_still_reads_as_its_time_alone`
- `Slice02ConfirmedNowScenarios.A_round_whose_forecast_failed_changes_no_Features_time_or_dates` (asserts the round did
  fail)
- `Slice02ConfirmedNowScenarios.A_round_whose_forecast_was_stopped_changes_no_Features_time_or_dates`
- `Slice02ConfirmedNowScenarios.A_computed_forecast_reads_as_last_updated_at_its_round_with_its_new_dates`
- Refresh History ("reads as before wherever nothing was skipped"): no "Skipped" when every forecast computed; a skip
  counts as a successful run; an older server's rows read as computed
