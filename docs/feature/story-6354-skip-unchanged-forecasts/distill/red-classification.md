# RED classification — Story 6354

2026-10-23. Every pending test was un-skipped once (not committed) and run: the backend classes with
`dotnet test --filter` and a TRX log, the frontend file with `pnpm vitest run`. All 71 failed; none failed on setup,
seeding, routing or types. The 7 active guards passed in the same run.

A second, throwaway probe run then disabled only the "a quiet round skipped" precondition and the explicit
`skipped` reads, to prove the rest of each acceptance scenario reaches its own assertion today: every change in the
re-forecast cases moved what the forecast reads (Feature order, rows, WIP, dependency, membership; the throughput
cases through the faked metrics) and drew numbers; the presses answered 2xx and produced a forecast; the held forecast
was held and stopped or let go; the summary line of the Team round was found. So a scenario still red after DELIVER
fails on behaviour, not on the harness.

Classes: **MISSING_FUNCTIONALITY** — the assertion fires because the behaviour is not built; **SCAFFOLD** — the call
reaches a `__SCAFFOLD__` body that throws `NotImplementedException` (the repository's RED convention), so the test
body is reached and only the production code is missing.

Paths: backend under `Lighthouse.Backend/Lighthouse.Backend.Tests/`, frontend under `Lighthouse.Frontend/src/`.

## Slice 01

### `API/Integration/SkipUnchangedForecasts/Slice01UnchangedRoundsSkipScenarios.cs`

| Scenario | Class | Failure seen |
|---|---|---|
| A background round that changed nothing skips the forecast and says so | MISSING_FUNCTIONALITY | "Nothing the forecast reads changed since 10:00: the forecast simulated 12800 numbers"; line read `… \| forecast=23ms`; row carries no `skipped` |
| A background round after an input changed forecasts in full ×14 | MISSING_FUNCTIONALITY | Precondition: "A refresh of FSS that changed nothing the forecast reads still simulated 12800 numbers" (no skip exists to be undone) |
| A Portfolio with nothing confirmed yet is forecast in full and the next quiet round skips | MISSING_FUNCTIONALITY | "the refresh history records no computed forecast" (no `skipped: false` on the row); quiet round simulated 12800 numbers |
| After a forecast that failed the next quiet round forecasts in full | MISSING_FUNCTIONALITY | precondition skip absent (12800 numbers) |
| After a forecast that was stopped the next quiet round forecasts in full | MISSING_FUNCTIONALITY | precondition skip absent |
| Update forecast always forecasts in full ×2 | MISSING_FUNCTIONALITY | precondition skip absent |
| Update forecast pressed while a forecast runs still ends in a computed forecast ×2 | MISSING_FUNCTIONALITY | precondition skip absent; probe: two rows after the press, the last without `skipped: false` |
| A skipped forecast is still announced and the day's snapshot is not doubled | MISSING_FUNCTIONALITY | quiet round simulated 12800 numbers |
| The first round of a day forecasts in full and records that day's snapshot | MISSING_FUNCTIONALITY | precondition skip absent |

### `Services/Implementation/Forecast/ForecastInputFingerprintTest.cs`

| Test | Class | Failure seen |
|---|---|---|
| Names every input the simulation is handed and nothing else | MISSING_FUNCTIONALITY | expected 14 members, `RegisteredMembers` is empty |
| Every simulation limit is one of its inputs | MISSING_FUNCTIONALITY | unregistered: `Trials`, `MostDaysOneSimulatedRunMayCover`, `MostRunsAtOnce` |
| Every input has a case above that changes it on its own | MISSING_FUNCTIONALITY | "The fingerprint names no inputs at all." |
| Same inputs twice; short lower-case digest; another order; Turkish culture; one input changed on its own ×20 | SCAFFOLD | `NotImplementedException: The fingerprint of 3 Features is not written yet` (403 for the digest case) |

### `Services/Implementation/Repositories/ForecastConfirmationRepositoryRelationalTest.cs` (slice 01 part)

All 5 SCAFFOLD: `NotImplementedException: Recording a Portfolio's forecast inputs is not written yet (Portfolio 1)`
(4242 for the missing Portfolio). The seeding, and in the stale-copy case the read of the old copy, ran first.

### `Services/Implementation/RefreshRoundSummaryLineSkippedForecastTest.cs`

| Test | Class | Failure seen |
|---|---|---|
| A Team round whose forecast was skipped ends with the skip and its time | MISSING_FUNCTIONALITY | line ends `… \| forecast=812ms` |
| A round that only forecast names the skip where the forecast time goes | MISSING_FUNCTIONALITY | line reads `… \| forecast=812ms \| duration=812ms …` |
| A skip reported into a refresh round reaches the line as a skip | MISSING_FUNCTIONALITY | `summary.ForecastSkipped` was `False` |

### `pages/Settings/SystemInfo/RefreshHistorySection.skippedForecasts.test.tsx`

| Scenario | Class | Failure seen |
|---|---|---|
| Averages, shortest and longest over the forecasts that computed | MISSING_FUNCTIONALITY | Avg Duration received `102.46 s` (all eight runs) |
| Says how many of the runs were skipped | MISSING_FUNCTIONALITY | `Unable to find … /^Skipped \(nothing changed\): 5 of the last 8 runs$/` |
| Says the last run was skipped, and when | MISSING_FUNCTIONALITY | Last Run holds the time only |
| Gap in the duration line at each skip | MISSING_FUNCTIONALITY | received `[240.1, 268.2, 305.9, 0.9, 1.1, …]`, expected `null` at each skip |
| All view average and longest over computed runs | MISSING_FUNCTIONALITY | Avg Duration received the average of all ten runs |
| Each entity's bar averages computed runs | MISSING_FUNCTIONALITY | `expected 102.46 to be 271.4` |

## Slice 02

### `API/Integration/SkipUnchangedForecasts/Slice02UserRefreshAndConfirmedNowScenarios.cs`

| Scenario | Class | Failure seen |
|---|---|---|
| A refresh a person starts forecasts in full even when nothing changed ×4 | MISSING_FUNCTIONALITY | precondition skip absent (12800 numbers) |
| A skipped forecast reads as confirmed at that round with its dates unchanged | MISSING_FUNCTIONALITY | quiet round simulated 12800 numbers |

`ForecastConfirmationRepositoryRelationalTest.Confirming_stamps_every_forecast_of_the_named_Features_and_changes_no_value`:
SCAFFOLD, `NotImplementedException: Confirming the forecasts of 1 Features is not written yet` (seeding and the
first read ran).

## Active guards (green today, must stay green)

- `ForecastInputFingerprintTest.The_dependency_the_cases_start_from_is_one_the_forecast_waits_on`
- `RefreshRoundSummaryLineSkippedForecastTest.A_computed_forecast_still_reads_as_its_time_alone`
- `Slice02…A_round_whose_forecast_failed_changes_no_Features_time_or_dates`
- `Slice02…A_computed_forecast_reads_as_last_updated_at_its_round_with_its_new_dates`
- Refresh History: no "Skipped" when every forecast computed; a skip counts as a successful run; an older server's
  rows read as computed
