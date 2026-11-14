# Story 6537 (T10), stage 2: the screen of the top five non-container classes

Read before screening, in full mode, fresh. Last line of each file, quoted back:

- `.claude/skills/test-writing/SKILL.md`: `End of SKILL.md`
- `.claude/skills/test-writing/references/backend.md`: `End of backend.md`

The screen rules are those of feature-delta.md ("The screen: what a unit test can pin"). The maintainer's yes to rewrites is recorded at the end of that file.

How the numbers below were got. Rows are counted from the source (each `[Test]`, each `[TestCase]`, each `TestCaseSource` entry), and agree with the class ranking in `evidence/step1/runs-and-class-ranking.txt`. The CI ranking only gives seconds per class, so the split between "slow because of the fill" and "slow because of the set-up" is read from the source and is NOT measured per row. Where a seconds figure is given it is the class median divided by its rows, and it is an average, not a measurement.

Words used in the tables:

- **move**: the rule is pinned by a unit test (or a port scenario) of the named production class; the host row can go once the break proof is shown.
- **move (pin exists)**: the named unit test class already holds a row for this rule. The crafter still has to show, by one deliberate break per rule, that the existing row goes red for the same production line; if it does not, add one row first. Nothing here claims that an existing row already flips.
- **rewrite**: the rule is restated at the computing class on a few in-memory samples, because the host row is slow in its Given.
- **stays**: reason given.

A point that holds for the whole PercentilesOverTime family: `OverTimeHistoryFillerTest`, `PercentileSnapshotWriterTest`, `ProcessBehaviorSnapshotWriterTest`, `OverTimeFillTargetTest`, `OverTimeGapReconcilerTest`, `GapAskingSeriesQueryTest`, `RetentionEdgeTest`, `ReconstructionMemoTest`, `WorkItemAgeAsOfDateTest` and `DatabaseMaintenanceGateTest` already exist and pin much of what the host classes re-prove through 120 to 200 individually saved work items per row.

---

## 1. Slice05ReconstructCycleTimeHistoryTest (89.7 s, 19 rows, about 4.7 s a row)

Files: `API/Integration/PercentilesOverTime/Slice05ReconstructCycleTimeHistoryScenarios.cs`, `...Specifications.cs`, base `ReconstructOverTimeHistoryAcceptanceTest.cs`.

Where the time goes (from the source). Set-up per row: `EnsureDeleted`, `EnsureCreated` and every `ISeeder` run again, on a host built once per fixture. Fill per row: `GivenTheTeamFinishedOneItemADayFrom` adds one work item per day, each in its own scope with its own `Save()`, so 61 to 201 saves a row; then the filler walks up to 90 days, and each day is a query plus a save. The fill dominates the set-up on every row except 14 and 15.

### (a) Rows

| # | Test method | What it asserts | Rule or wire | Destination |
|---|---|---|---|---|
| 1 | The_flow_coach_reads_the_run_of_days_before_the_first_one_that_was_recorded | After the chart is opened and the fill ran, every day of the 31-day period is held | rule | rewrite at `OverTimeHistoryFiller.DrainAsync` (walk) in `OverTimeHistoryFillerTest`: 6 stored items, one asked-for request of 6 days, writer double; each day gets `FillDayIfAbsent`. A break flips `IsOutsideWhatTheStoredItemsSupport` or the `continue` in `WalkAsync`: no day is written |
| 2 | ...reads_across_the_stretch_the_instance_was_switched_off | The days between two recorded days are filled | rule | move (pin exists): `OverTimeGapReconciler.AskForTheDaysThatAreMissing`, `DaysTheChartHoldsAndDaysNoPassCanWrite_AreLeftOutOfTheAsk` |
| 3 | A_team_nobody_is_syncing_any_more_gains_no_days_since_it_stopped | No day after the team's last refresh is written | rule | move (pin exists): `OverTimeHistoryFiller` walk, `TheFirstAndTheLastDayTheStoredItemsSupport_AreBothWorkedOut_AndTheDaysEitherSideAreNot`; last-observed day from `OverTimeFillTarget.For`, `ATeamLastSeenInTheEveningWestOfUtc_...` |
| 4 | The_trend_reaches_back_only_as_far_as_the_team_has_finished_anything | No day before the earliest finished item | rule | move (pin exists): `OverTimeFillTarget.EarliestDayTheItemsSupport`, `ATeamsHistory_BeginsOnTheEarliestDayOneOfItsOwnItemsFinished` |
| 5 | A_year_wide_range_fills_in_over_several_visits_rather_than_all_at_once | One pass adds at most 90 days; a second visit adds more | rule | move (pin exists): filler cap `APassWorksOutNinetyDaysAtMost`, and `AnOwnerAskedForAgainOnceItsPassIsOver_IsTakenAgain`. This is the slowest row (201 items, two fills) |
| 6 | A_pass_that_runs_past_its_budget_stops_early_and_picks_up_on_the_next_visit | A pass given a budget of one tick writes some days but not all; the next visit writes more | rule | move (pin exists): filler budget, `APassOutOfTime_StillWorksOutItsFirstDay_AndGivesTheRestBack` |
| 7 | A_stretch_in_which_the_team_finished_nothing_stays_blank_instead_of_reading_zero | No row for days with nothing finished; no row of four zeros | rule | move (pin exists): `PercentileSnapshotWriter.FillDayIfAbsent`, `ADayOnWhichNothingFinished_GetsNoRow`, `AnyOnePercentileAboveZero_IsAReadingWorthARow` |
| 8 | A_quiet_day_is_left_blank_by_the_daily_recording_too | The daily recording path leaves a quiet day blank | rule | move: `PercentileSnapshotWriter.RecordToday`. The existing unit rows call `FillDayIfAbsent` only; add one `RecordToday` row with a reader that returns no values, expect no row |
| 9 | A_day_that_was_actually_watched_keeps_the_value_it_was_watched_at | A day with a stored row is not rewritten by the fill | rule | move (pin exists): `PercentileSnapshotWriter.FillDayIfAbsent`, `AHorizonThatAlreadyHasARow_KeepsWhatWasObserved` |
| 10 | Looking_at_the_same_period_twice_costs_nothing_the_second_time | A second visit leaves the held days unchanged | rule | move (pin exists): `OverTimeGapReconciler` (a full period asks for nothing, `APeriodTheChartAlreadyHoldsInFull_AsksForNothing`) together with row 9 |
| 11 | A_day_worked_out_afterwards_reads_the_same_as_the_day_that_was_watched | A day the recorder wrote, lost and refilled reads the same; wrong windows would read differently | rule | rewrite at `PercentileSnapshotWriter` (`RecordToday` against `FillDayIfAbsent`): a reader double that returns values built from its window start and end, one 3-horizon day recorded with a clock at day D and the same day filled afterwards; rows equal. A break changes `windowEnd.AddDays(-horizon)` in `ComputeDay` (or anchors on today): the two rows differ |
| 12 | Opening_the_trend_answers_with_what_is_there_and_writes_nothing_while_the_coach_waits | The read answers with the held days; nothing is written on the reader's thread | wire plus rule | stays: wire (route, `recordedAt` member, answer before any write). The rule half is pinned by `GapAskingSeriesQueryTest`. Kept as the class's read-route wiring row |
| 13 | The_days_the_first_visit_could_not_show_are_there_on_the_next_one | First read is empty, the fill runs, the second read has the days | wire | stays: the one skeleton for the loop read, ask, fill, read through the real host. Its Given can shrink to about 6 items and a 5-day period |
| 14 | An_operator_cannot_start_a_database_restore_while_the_chart_is_filling_in | While a pass is in flight, a restore is refused with a reason; allowed afterwards | rule | move (pin exists): `DatabaseMaintenanceGate.TryAcquire` with a filler double whose `HasPassInFlight` is true, `DatabaseMaintenanceGateTest`. Check that a row with a reason and a row for "allowed again" exist |
| 15 | The_chart_stops_filling_itself_in_while_the_operator_is_restoring_the_database | A pass stands down while a restore runs | rule | move (pin exists): `OverTimeHistoryFillerTest.ARestoreTurnedAwayByAPass_StopsThePassAtItsNextDay...` and `APass_WhileABackupRuns_LoadsNoOwner` |
| 16 | Two_copies_of_the_application_filling_the_same_day_leave_one_point_not_two | Two passes on the same day leave one row | rule | move to a port scenario: two scopes on `ServicePortHarness` (real SQLite, so the unique index is real); both stage `FillDayIfAbsent` for one day, both `SaveFilledDay`, one row stays. No host, no threads. Unit pin of the absorb path exists: `ARowRefusedBecauseAnotherCopyStoredThatDayFirst_IsAbsorbed` |
| 17 | A_day_another_copy_records_mid_pass_costs_that_day_and_not_the_rest_of_the_window | A day taken by another copy mid-pass is kept as recorded; the other 89 days are still written | rule | move (pin exists): writer absorb (row above) and filler "the rest of the pass continues", `OneDayThatCannotBeWritten_IsReportedOnce_WithItsCause_AndNoTally`. The host version needs latches and a dedicated thread |
| 18 | A_refresh_landing_mid_fill_neither_loses_its_day_nor_duplicates_one | A refresh during a held pass leaves every day once | rule, but needs a held background pass | stays: needs a parked filler pass on its own thread and the refresh event together. The "no duplicate" half is the same as row 16 |
| 19 | Backdated_demonstration_values_are_stepped_over_rather_than_corrected | A pre-existing backdated row is left alone | rule | move (pin exists): same as row 9 |

### (b) Shared steps

| Step | Asserts or does | Rule or wire | Where it goes |
|---|---|---|---|
| `SeedTeamObservedUntil`, `GivenTheTeamFinishedOneItemADayFrom` (1 item, 1 scope, 1 save per day) | Builds the Given | precondition; this is the fill cost | Not needed after the move. In the unit rows the Given is a list of 3 to 6 items |
| `ReadTeamPercentileTrend` (GET `percentiles-over-time`, query names `metricType`, `horizon`, `startDate`, `endDate`) | Route and parameter names | wire | stays with rows 12 and 13 |
| `DatesIn` (JSON member `recordedAt`) | Date format and member name | wire | stays with row 12 |
| `TheReconstructionPassRunsToCompletion` (`OverTimeHistoryFiller.DrainAsync` on the host singleton) | Runs the background pass | wiring | stays with rows 13 and 18 |
| `ThenTheTrendCoversEveryDayFrom`, `ThenTheTrendStopsOn`, `ThenTheTrendReachesBackNoFurtherThan`, `ThenTheDaysAreLeftAbsent`, `ThenNoDayReadsAsFourZeroes` | Which days are held | rule | the filler, writer and `OverTimeFillTarget` rows above |
| `ThenTheChartGainedAtMostOnePassWorthOfDays` | Cap of 90, using the test's own copy of the constant | rule | filler cap row. The test's own `ReconstructionCapInDays` repeats the product value, so the row cannot fail if both are changed together; the unit row should state 90 in writing |
| `GivenThatDayReadsDifferentlyOverAnyOtherStretch` | Guard that four wrong windows read differently | rule (a guard on the Given) | replaced in the row 11 rewrite by a reader double that makes every window read differently |
| `ThenStartingADatabaseRestoreIsRefused` / `...AllowedAgain` | Gate refusal with a reason | rule | `DatabaseMaintenanceGateTest` (row 14) |
| `AReconstructionPassHeldInFlight`, `FillPause`, `StagedDayPause`, `PausableFillWriter`, `HeldPass` | Latches that park a real pass on a thread | harness for concurrency | stays with row 18 only; rows 14, 15, 17 stop using it |
| `OneTimeSetUp` host, per-test `EnsureDeleted/Created` plus seeders | Fixture set-up | set-up | stays for rows 12, 13, 18; the class keeps one host |

### (c) Count

14 rule rows (move) + 2 rule rows (rewrite) + 3 rows that stay = 19 class rows.

Estimate (average, not measured): 16 rows leave at about 4.7 s, so about 75 s of the 89.7 s, less the cost of the new unit rows (milliseconds) and one port scenario (about 25 ms set-up, from the backend half's finding of 22 ms per port scenario).

---

## 2. Slice07WhereTheLimitsActuallyMovedTest (88.3 s, 10 rows, about 8.8 s a row)

Files: `Slice07WhereTheLimitsActuallyMovedScenarios.cs`, `...Specifications.cs`, same base as above.

Where the time goes: every row fills 200 days of items, one saved item per day (rows 6, 7, 8, 9 add several items per day through `SeedItemsFinishedOn`, up to 250 days), then each of five or six limit families is read and filled (a pass computes a process-behaviour chart per family per day). Row 9 also refreshes the team at a past day, loses the row and computes four wrong stretches.

### (a) Rows

| # | Test method | What it asserts | Rule or wire | Destination |
|---|---|---|---|---|
| 1 | A_team_fills_in_every_behaviour_it_reports_and_not_one_fewer | The held families are exactly Throughput, WorkItemAge, Wip, CycleTime, Arrivals | rule | move: `ProcessBehaviorSnapshotWriter.FamiliesFor(Team)` (new unit row on a writer built with metrics doubles, as `ProcessBehaviorSnapshotWriterTest` does). A break deletes one `new(...)` line of the list |
| 2 | A_portfolio_fills_in_every_behaviour_it_reports_and_not_one_fewer | Six families for a portfolio | rule | move: `FamiliesFor(Portfolio)`, same fixture |
| 3 | How_big_deliveries_are_getting_is_filled_in_for_a_portfolio_and_never_for_a_team | FeatureSize for the portfolio, none for the team | rule | move: `FamiliesFor(Team)` has no FeatureSize and `FamiliesFor(Portfolio)` has it (same unit rows as 1 and 2; keep the three id rows) |
| 4 | A_stretch_pinned_further_back_than_the_team_keeps_work_reports_no_limits | No limits when the pinned stretch is out of reach | rule | move (pin exists): `RetentionEdge`, `LimitsDrawnFromAPinnedStretchThatBeginsBeforeTheEdge_ComeBackNotReady...`; writer gate `chart.Status != Ready` |
| 5 | A_stretch_in_which_the_team_finished_nothing_reports_no_band_rather_than_a_flat_zero_one | No row where the chart is not ready or Average and UNPL are both zero | rule | move: `ProcessBehaviorSnapshotWriter.FillDayIfAbsent` (`WriteUnlessTheChartHasNoProcessToShow`). The six existing rows do not cover the collapsed-band gate; add two rows (not ready; ready but zero band) with a reader double. A break removes the `Average == 0 && Unpl == 0` line |
| 6 | A_team_that_fixed_the_stretch_its_limits_come_from_reads_steady_limits_not_an_empty_chart | A pinned stretch that is out of reach today but in reach as of the day still reads (steady) limits | rule | rewrite at `RetentionEdge.For(lastObservedOn, team)` and `Guard`, in `RetentionEdgeTest`: a pinned start 240 days back, 220 days kept, last refreshed 25 days ago, asked as of day D: reads; asked as of today: not ready. A break anchors the edge on today instead of on `lastObservedOn` |
| 7 | A_portfolio_that_fixed_the_stretch_its_limits_come_from_reads_steady_limits_too | Same for a portfolio | rule | rewrite at `RetentionEdge.For(lastObservedOn, portfolio)`, same fixture |
| 8 | A_team_that_did_not_fix_the_stretch_reads_limits_drawn_from_each_days_own_history | Each day's limits are read over the span ending on that day, as of that day; the values then differ | rule | move (pin exists): `ProcessBehaviorSnapshotWriterTest.ADayWithoutLimits_IsReadOverTheSpanEndingOnThatDay_AsOfThatDay` |
| 9 | Limits_worked_out_afterwards_read_the_same_as_the_day_they_were_watched | A refilled day equals the recorded day; wrong stretches or "as of today" read differently | rule | rewrite at `ProcessBehaviorSnapshotWriter` (`RecordToday` against `FillDayIfAbsent`): a chart double that returns a triple built from its start, end and `asOf`; the clock at day D versus the fill of D. A break passes `asOf: null` or today in `FillDayIfAbsent`. The part that `asOf` null means "today" inside the metrics service is a different class and is not claimed here |
| 10 | Limits_stop_where_the_team_stopped_being_watched_and_a_second_look_changes_nothing | No limits after the last refresh; a second look changes nothing | wire plus rule | stays: the one limits-route row (route `process-behavior-over-time`, `type` parameter, `recordedAt`). The rule half is pinned by the filler walk and writer rows (as Slice05 rows 3 and 9) |

### (b) Shared steps

| Step | Asserts or does | Rule or wire | Where it goes |
|---|---|---|---|
| `GivenTheTeamFinishedOneItemADayFrom`, `SeedItemsFinishedOn`, `SeedSizedDeliveriesFinishedOn` | The Given | precondition; fill cost | gone with the moved rows |
| `GivenTheTeamsStretchIsOutOfReachTodayAndInReachOverThePeriod` / `TheStretchIsOutOfReachTodayAndInReachAsOf` | Four guards that the seed makes the stretch matter | rule guard on the Given | becomes the numbers of the `RetentionEdge` rows 6 and 7 (written as the sample) |
| `WhenTheDeliveryLeadTogglesEvery...Behaviour` | One read per family through the route | wire | stays only with row 10 (one family); rows 1 to 3 go to `FamiliesFor` |
| `ReadTeamLimitTrend` / `ReadPortfolioLimitTrend` | Route, `type`, `startDate`, `endDate` | wire | stays with row 10 |
| `ThenEveryBehaviour...GotItsLimits`, `ThenDeliverySizeIsReported...` | The family set | rule | `FamiliesFor` rows |
| `ThenNoLimitsAreReportedFor`, `ThenNoDayReportsACollapsedBand` | No row for not-ready or collapsed bands | rule | writer gate rows (row 5) |
| `ThenTheLimitsHoldSteadyAcross`, `ThenTheLimitsAreFreeToMoveAcross` | Distinct triples per day or one triple | rule (a consequence of the window and the pinned stretch) | rows 6, 7, 8 |
| `ThroughputLimitsWorkedOutOver`, `GivenThatDayReadsDifferentlyWhenWorkedOutAnyOtherWay` | Guards on the watched day | rule guard | replaced by the chart double of row 9 |
| `GivenALimitDayTheRecorderWroteAndThenLost`, `TheRecordOfThatLimitDayIsLost` | Records then deletes a day | precondition | replaced by the double of row 9 |

### (c) Count

6 rule rows (move) + 3 rule rows (rewrite) + 1 row that stays = 10 class rows.

Estimate (average): 9 rows leave at about 8.8 s, so about 79 s of 88.3 s. This is the largest saving per row of the five and needs no new harness.

---

## 3. Slice06EveryPercentileTabSpansTheSamePeriodTest (48.6 s, 11 rows, about 4.4 s a row)

Files: `Slice06EveryPercentileTabSpansTheSamePeriodScenarios.cs`, `...Specifications.cs`, same base. Fill-bound: 61 to 201 individually saved items a row, and rows 8 and 9 and 10 read up to four tabs.

### (a) Rows

| # | Test method | What it asserts | Rule or wire | Destination |
|---|---|---|---|---|
| 1 | A_look_back_fills_in_every_day_of_the_period_asked_for | The 60-day tab holds every day of the period | rule | move (pin exists): writer horizons and window, `PercentileSnapshotWriterTest.ADayWithoutRows_IsWorkedOutAtEveryHorizon_OverAWindowEndingOnThatDay`; days from the filler walk (Slice05 row 1 rewrite) |
| 2 | A_longer_look_back_still_carries_a_slow_stretch_the_shorter_one_has_left_behind | 90-day reading is higher than the 30-day reading on the same day | rule | move (pin exists): same writer test (window = `horizon` days ending on the day, per horizon) |
| 3 | An_item_counts_towards_a_past_day_at_the_age_it_had_reached_by_then | Work item age as of a past day uses the age at that day | rule | move (pin exists): `WorkItemAgeAsOfDateTest.GetWorkItemAgePercentiles_ItemClosedAfterTheSelectedDay_IsIncludedAndAgedToThatDay` and `..._MatchHandComputedAgesOnTheSelectedDay` |
| 4 | An_age_day_worked_out_afterwards_reads_the_same_as_the_day_that_was_watched | A refilled age day equals the recorded one | rule | rewrite at `PercentileSnapshotWriter` for `MetricType.WorkItemAge` (`RecordToday` against `FillDayIfAbsent`, one row under `NoHorizon`), as Slice05 row 11. A break anchors the age reader on today |
| 5 | A_portfolio_fills_in_its_delivery_cycle_time_the_same_way_a_team_does | The portfolio fills cycle time | rule | move: `PercentileFamilies.For(Portfolio, ...)` (35 lines; no unit test found, add one row: the family set) plus the writer rows |
| 6 | A_portfolio_fills_in_its_delivery_age_tab_too | The portfolio fills the age tab | rule | move: `PercentileFamilies.For(Portfolio, ...)`, same new row |
| 7 | Flicking_between_tabs_does_not_start_the_filling_over_again | After the 30-day tab filled, opening the 60-day tab changes nothing in the 30-day tab | rule | move (pin exists): the writer fills all horizons in one pass; `OverTimeGapReconciler` asks for nothing when held |
| 8 | Every_tab_covers_the_same_period_once_the_chart_has_filled_in | Three cycle-time tabs and the age tab hold the same days | wire plus rule | stays: the one tab row (four tab and horizon parameters through the real route, one fill, four tabs read). The family set is pinned at `PercentileFamilies.For(Team, ...)` by the unit rows above |
| 9 | The_age_tab_stops_where_the_team_stopped_being_watched | Age tab: no day after the last refresh | rule | move (pin exists): filler walk and `OverTimeFillTarget` last-observed |
| 10 | The_portfolio_age_tab_stops_where_the_portfolio_stopped_being_watched | Same for a portfolio | rule | move (pin exists): `OverTimeFillTargetTest.APortfolioLastSeenInTheEveningWestOfUtc_...` and the walk |
| 11 | A_day_the_team_had_nothing_in_flight_is_left_blank_rather_than_drawn_at_zero | No age row on a day with nothing in flight | rule | move (pin exists): `PercentileSnapshotWriter` blank-day rows, with an age reader that returns nothing |

### (b) Shared steps

| Step | Asserts or does | Rule or wire | Where it goes |
|---|---|---|---|
| `GivenTheTeamFinishedOneItemADayFrom`, `GivenTheTeamTookWeeksOverEachItemFrom`, `GivenThePortfolioFinishedOneDeliveryADayFrom` | The Given | precondition; fill cost | gone with moved rows |
| `WhenTheFlowCoachOpens...Tab` (four helpers over `ReadTeamPercentileTrend` and `ReadPortfolioPercentileTrend`) | Route, `metricType`, `horizon`, `NoHorizon` mapping | wire | stays with row 8 |
| `ThenTheTabCoversEveryDayFrom`, `ThenEveryTabCoversTheSamePeriod`, `ThenTheTabStopsOn`, `ThenThoseDaysAreLeftBlank`, `ThenTheTabIsUnchangedSince` | Days held per tab | rule | writer, filler and `OverTimeFillTarget` rows |
| `ThenTheLongerLookBackReadsHigherOn` | P85 for 90 days above 30 days | rule | row 2 (the unit row uses a reader double whose value grows with the window start) |
| `ThenTheItemCountsTowardsThatDayAtTheAgeItHadThen` | P50 equals age 5 on a past day | rule | `WorkItemAgeAsOfDateTest` |
| `GivenAWorkItemAgeDayTheRecorderWroteAndThenLost` | Records, then deletes | precondition | replaced by row 4's double |

### (c) Count

9 rule rows (move) + 1 rule row (rewrite) + 1 row that stays = 11 class rows.

Estimate (average): 10 rows leave at about 4.4 s, so about 44 s of 48.6 s.

---

## 4. ConnectionFieldChange.Slice01FieldChangeUpdatesItsOwnersTest (40.8 s, 13 rows, about 3.1 s a row)

Files: `API/Integration/ConnectionFieldChange/Slice01FieldChangeUpdatesItsOwnersScenarios.cs`, `...Specifications.cs`, base `ConnectionFieldChangeAcceptanceTest.cs`. The rows are 9 methods; two have `TestCase` lists (3 and 2 cases), so 13 rows.

Where the time goes: not a history fill. Each row seeds three connections, five teams and two portfolios, and (for 11 of 13 rows) updates every one of the six owners once through the real update queue and waits for the queue to go idle, before the save under test. The set-up is per-row host database creation plus seeders; the Given is the queue.

Existing unit pins that matter: `Services/Implementation/Connections/ConnectionSaveServiceTest` already holds `Commit_FieldAdded_PublishesTheTeamsAndPortfoliosOnThisConnectionOnly`, `Commit_FieldAddedButUpdatesLeftForLater_PublishesNothing`, `Commit_OnlyTheNameChanged_PublishesNothing`, `Commit_FieldAddedToAConnectionNoTeamOrPortfolioUses_PublishesNothing`, `Commit_StartingUpdatesFailsAfterAStoredSave_ReportsSavedAndLogsOneError`, and a 12-case table `Commit_UpdatesExactlyTheOwnersWhoseFetchTheSaveMoved`; `FetchFingerprintTest` holds `ForConnection_AnEditToTheConnectionsFields_MovesExactlyWhenItShapesAFetch...`.

### (a) Rows

| # | Test method (case) | What it asserts | Rule or wire | Destination |
|---|---|---|---|---|
| 1 | Adding_a_field_lists_an_update_for_every_Team_and_Portfolio_on_the_connection_in_the_Task_Manager | The save is accepted; the Task Manager lists one update per owner on the connection | wire plus rule | stays: needs the update queue and the Task Manager listing behind a PUT. Which owners is already pinned by `ConnectionSaveServiceTest`. Kept as the class's skeleton |
| 2 | After_a_field_is_added_every_Team_and_then_every_Portfolio_downloads_everything_once | One full update each, teams before portfolios, field stored | rule plus queue order | stays: ordering is the update queue's behaviour; the "full because the fingerprint moved" half is pinned by the two classes above |
| 3a | Any_other_field_edit...(RenamesAFieldsLabel) | A label rename makes every owner download everything | rule | rewrite at `ConnectionSaveService.Commit` in `ConnectionSaveServiceTest`: add a `TheEdit` case that renames a field label; expect the owners of the connection published. Break: remove the label from what `FetchFingerprint.ForConnection` renders (the `definition.DisplayName` argument), so no owner is published |
| 3b | ...(ChangesAFieldsReference) | A reference change does the same | rule | rewrite: new case; break on `definition.Reference` in `FetchFingerprint.Render` |
| 3c | ...(RemovesAField) | A removal does the same | rule | rewrite: new case; break by making `Render(IEnumerable<AdditionalFieldDefinition>?)` ignore removed definitions |
| 4 | A_Team_already_updating_or_waiting_is_not_downloaded_in_full_twice | A running and a waiting update are not duplicated; run counts per owner | rule | stays: needs the update queue, a running update held on a latch, and the Task Manager (lanes and hosted services) |
| 5 | An_update_that_fails_leaves_the_field_saved_and_the_others_updating | A failing owner does not stop the others; the field is saved | rule | stays: needs the queue and a failing tracker; the "saved even if updates fail" part is pinned by `Commit_StartingUpdatesFailsAfterAStoredSave...` |
| 6 | The_Team_whose_update_failed_downloads_everything_at_its_next_update | After a failed update the next update is full | rule | rewrite at `WorkItemService.UpdateWorkItemsForTeam` (in `WorkItemServiceTest`, which already builds it with doubles): a team whose stored fingerprint differs, the connector throws once, then the second run asks for everything. The Given on the host goes through the real save route and the queue, which a builder can replace with `team.FetchFingerprint = <old value>`. Break: move the `team.FetchFingerprint = fetchShape.Fingerprint` line before the fetch |
| 7 | Adding_a_field_to_a_connection_only_Portfolios_read_from_lists_their_updates_in_the_Task_Manager | A portfolio-only connection starts its portfolios | rule plus Task Manager | stays: Task Manager listing. The portfolio-only case is in the 12-case table only for a connection with a team; add a portfolio-only case to `ConnectionSaveServiceTest` if this row is to lose its rule half |
| 8 | Adding_a_field_on_the_community_licence_updates_every_Team_and_Portfolio_on_the_connection | The small licence still updates its three teams and one portfolio | rule | stays: the limit is applied by the update queue's admission; needs the queue |
| 9 | A_save_that_asks_to_update_now_updates_every_Team_and_Portfolio_on_the_connection | `updateNow=true` updates everyone | wire plus rule | stays: the choice string comes in through the controller (query parameter); the value is pinned by the `startUpdates` column of `Commit_UpdatesExactlyTheOwners...` |
| 10 | A_save_that_asks_for_neither_now_nor_later_is_refused_and_stores_nothing ("maybe") | 400 and nothing stored | wire | stays: status code, parsing in the controller (for #6508) |
| 11 | ...("later") | same | wire | stays: same |

### (b) Shared steps

| Step | Asserts or does | Rule or wire | Where it goes |
|---|---|---|---|
| `GivenNordwindsConnectionsTeamsAndPortfolios` (3 connections, 5 teams, 2 portfolios, each a save) | The Given | precondition | stays for the host rows |
| `GivenEveryTeamAndPortfolioHasBeenUpdatedOnce` (six queued updates, wait for idle) | Makes the owners remember a fingerprint | precondition; the queue is the cost | stays for host rows; replaced by a stored fingerprint in the rewrites |
| `SaveTheConnection` / `Put` (PUT connection, `updateNow` query) | Route, DTO, status | wire | stays |
| `ThenTheSaveIsAccepted` / `...RefusedBecause...` | 200, 400, 403, 409 | wire | stays |
| `ThenTheTaskManagerListsOneUpdateForEachOf` (polls up to 5 s) | The owners listed | rule through the queue | stays (queue) |
| `ThenEachOfThemDownloadsEverythingExactlyOnce` | One full update per owner in the refresh history | rule through the queue | stays; the value half is pinned by the unit rows |
| `ThenTheTeamsWereUpdatedBeforeAnyPortfolio` | Order in the history | queue behaviour | stays |
| `ThenTheConnectionReads` / `TheConnectionAsThePageReadsIt` | The connection as the page reads it, JSON | wire | stays |

### (c) Count

0 rule rows (move) + 4 rule rows (rewrite) + 9 rows that stay = 13 class rows.

Estimate (average): 4 rows at about 3.1 s, so about 12 s of 40.8 s. Rows 3a to 3c share the cost of the six-owner Given with the staying rows, so the average may overstate the saving. This class is mostly a body-bound queue class and a poor candidate for suite 2.

---

## 5. FasterUpdates.Slice05FetchFingerprintTest (39.5 s, 47 rows, about 0.84 s a row)

Files: `API/Integration/FasterUpdates/Slice05FetchFingerprintScenarios.cs`, `...Specifications.cs`, base `FasterUpdatesAcceptanceTest.cs`. Rows: 13 plain methods, plus `TestCaseSource` lists of 8 (team, changes the fetch), 10 (portfolio, changes the fetch), 10 (team, changes nothing) and 6 (portfolio, changes nothing): 13 + 34 = 47.

Where the time goes: not a fill. Each row is short (under 1 s on average): per-row host database and seeders, one real PUT of the settings, one update through the update queue (polled every 20 ms), and a second update where the row first refreshes the owner. The cost per row is the host and the queue, not a body, so each row leaves for the cost of a unit row.

Existing pins: `FetchFingerprintTest` (every registered property changes the digest, every excluded group does not, collections reversed give the same digest, a connection field added moves it for the owner), `FetchShapingPropertyGuardTest` (architecture), `OwnersWhoseFetchMovedTest`. Not found: a unit row for the decision in `WorkItemService.UpdateWorkItemsForTeam` and `UpdateFeaturesForPortfolio` that a stored fingerprint different from the new one means a full download with reason `ConfigurationChanged`, and that a missing stored fingerprint means a full download with no reason (a search for `ConfigurationChanged` in `WorkItemServiceTest` finds nothing). Those rows must be added to `WorkItemServiceTest` first, because they are what the moved rows then rest on.

### (a) Rows

| # | Test method (case) | What it asserts | Rule or wire | Destination |
|---|---|---|---|---|
| 1 | A_query_edit_makes_the_next_refresh_download_everything_again | After a query edit saved through the route, the next refresh is full (3 scanned, 3 fetched), is logged and recorded, and the fingerprint is stored | wire plus rule | stays: the one skeleton (PUT, queue, summary line, refresh log). Its value half goes to the `WorkItemService` rows below |
| 2 | The_operator_is_told_that_a_configuration_change_is_why_everything_was_downloaded | The summary line carries `reason=configuration-changed` | rule (the line text is built from `SyncOutcome.Reason`) | move: `WorkItemService.UpdateWorkItemsForTeam` (new rows in `WorkItemServiceTest`): fingerprint changed and stored value not null gives `Reason == SyncOutcome.ConfigurationChanged`; the line text itself is pinned by the update-service summary test |
| 3 | An_edit_to_a_fetch_shaping_team_setting_makes_the_next_refresh_download_everything (8 cases: query, work item types, to-do states, doing states, done states, cutoff days, parent field, state mapping) | Each fetch-shaping team setting moves the fingerprint, so the next refresh is full with a reason | rule | move (pin exists): `FetchFingerprint.For`, `For_ARegisteredPropertyChanges_ProducesADifferentFingerprint`, plus the new `WorkItemService` rows. One seam is not covered by these: that the settings DTO field reaches the model field. Add one wire row to `Contracts` that sends every listed field through the real PUT and reads back the model's fingerprint (T5's "one contract row sends every input through the real door") |
| 4 | An_edit_to_a_fetch_shaping_portfolio_setting_makes_the_next_refresh_download_every_feature (10 cases: the eight above for portfolios, plus owning-team field and size-estimate field) | Same, for portfolios | rule | move (pin exists): `FetchFingerprint.For(Portfolio)`; same contract row for the portfolio DTO |
| 5 | Reading_a_tracker_state_as_a_different_state_makes_the_next_refresh_download_everything_even_though_the_query_is_unchanged | State mapping moves the fingerprint although the query is identical; history is kept at the save | rule | move (pin exists): `FetchFingerprint.For` with `StateMappings`. The "history kept" half is row 15 (stays) |
| 6 | Moving_a_state_to_a_different_column_makes_the_next_refresh_download_everything_even_though_the_query_is_unchanged | A state moved between columns moves the fingerprint (the union is unchanged) | rule | move (pin exists): `For_ARegisteredPropertyChanges` on the doing and done states. Check the sample moves a state from one column to the other, not an added state |
| 7 | Adding_a_field_to_the_connection_makes_the_next_refresh_download_everything_for_the_teams_that_use_it | A field defined on the connection moves the fingerprint of the team | rule | move (pin exists): `For_AFieldDefinitionAddedToTheConnection_ProducesADifferentFingerprintWithoutTouchingTheOwner` |
| 8 | Re_saving_the_same_states_in_a_different_order_costs_no_download | Reversed collections give the same fingerprint; the refresh is a delta | rule | move (pin exists): `For_EveryCollectionReversed_ProducesTheSameFingerprint` |
| 9 | An_edit_that_changes_nothing_about_what_is_fetched_costs_no_download (10 cases: wait state, blocked rule, staleness, blocked staleness, cycle time definition, SLE, system WIP, throughput window, feature WIP, estimates) | None of these moves the fingerprint, so the refresh stays a delta | rule | move (pin exists): `For_AnExcludedPropertyChanges_ProducesTheSameFingerprint`. The excluded-group list there is a representative list, not these ten; the crafter checks each of the ten against it, and adds a case for any that is missing |
| 10 | A_portfolio_edit_that_changes_nothing_about_what_is_fetched_costs_no_download (6 cases) | Same for portfolios | rule | move (pin exists): same test; same check of the six |
| 11 | An_instance_that_upgraded_into_this_release_downloads_everything_on_its_first_refresh | A stored fingerprint of null gives a full download with no reason | rule | move: `WorkItemService.UpdateWorkItemsForTeam` new row (null stored value, `Reason` is null, connector asked for everything, fingerprint stored) |
| 12 | A_team_edit_does_not_cost_its_portfolio_a_full_download | Fingerprints are per owner | rule | move: a unit row beside `FetchFingerprintTest`: a team and a portfolio on one connection, edit the team query, `For(portfolio)` is unchanged. Pure |
| 13 | A_portfolio_edit_does_not_cost_its_team_a_full_download | Same the other way | rule | move: same row |
| 14 | A_team_that_moves_to_a_different_connection_starts_from_nothing | Moving to another connection purges stored work and history | logic in the controller's save path | stays: the purge is decided by `WorkTrackingSystemOptionsOwnerExtensions` (`API/Helpers`) and run in the controller; recorded for #6508 |
| 15 | A_team_whose_query_changed_keeps_the_history_it_already_recorded | A query change does not purge | same | stays: same |
| 16 | A_portfolio_that_moves_to_a_different_connection_starts_from_nothing | Same for portfolios | same | stays: same |
| 17 | A_portfolio_whose_query_changed_keeps_the_features_it_already_stored | Same for portfolios | same | stays: same |

### (b) Shared steps

| Step | Asserts or does | Rule or wire | Where it goes |
|---|---|---|---|
| `GivenATeamWhoseTrackerCanBeScanned`, `SeedConnection`, `SeedTeam`, `SeedPortfolio`, `TheTrackerHolds...` | The Given | precondition | stays for rows 1, 14 to 17 only |
| `WhenTheOperatorEditsTheTeamsSettings` / `TheOperatorSavesTheTeamsSettings` (PUT `/api/latest/teams/{id}` with the DTO) | DTO to model mapping and the controller's purge decision | wire (seam) | stays with the host rows and the one new `Contracts` row |
| `TheTeamRefreshRuns` (`TriggerUpdate`, poll `HasActiveWork`) | Runs one update through the queue | wiring | stays with rows 1, 14 to 17 |
| `ThenTheWholeQueryWasDownloaded`, `ThenNothingWasDownloaded` (`FullDownloadsIssued`, `PayloadDownloads`) | Whether the connector was asked for everything | rule | `WorkItemService` rows (connector double records the call) |
| `ThenTheOperatorIsToldConfigurationIsWhy` / `...IsNotToldConfigurationIsWhy` / `TheSummaryLine` | `reason=` text in the log line | rule for the reason; the text is wire-like | `SyncOutcome.Reason` at `WorkItemService`; the text stays with the summary-line test and row 1 |
| `ThenTheRefreshReported...` (refresh log: mode, scanned, fetched, success) | Log row contents | rule through the queue | `WorkItemService` outcome (`SyncOutcome`) rows; the log row itself stays with row 1 |
| `ThenTheTeamRemembersWhatItAskedFor`, `TheStoredFetchFingerprintForTeam` | Fingerprint stored after the cycle | rule | `WorkItemService` rows (stored after success, kept on failure: `UpdateWorkItemsForTeam_JiraStatusListCouldNotBeRead...` already pins the failure) |
| `ThenWhatTheTrackerIsAskedForIsUnchanged` | Positive control that the query text is the same | guard on the Given | goes with rows 5 and 6 (the new rows compare fingerprints, so the guard is not needed) |
| `ThenTheTeamKeptTheHistoryItRecorded`, `ThenTheTeamStartedFromNothing`, `ThenThePortfolioStartedFromNothing`, `ThenThePortfolioStillHas` | Purge or keep | controller logic | stays with rows 14 to 17 |

### (c) Count

42 rule rows (move) + 0 rule rows (rewrite) + 5 rows that stay = 47 class rows.

Estimate (average): 42 rows at about 0.84 s, so about 35 s of 39.5 s, but with a different shape from the history classes. Each row is a short host row, so the saving is the host and queue overhead per row, and it holds only if the new `WorkItemService` rows and the one contract row are added first. Two additions are counted as new work, not as rows of this class: the `WorkItemService` rows for the changed-and-null decisions, and the contract row for the settings DTO seam.

---

## Suite view: the PercentilesOverTime family

12 classes, 111 rows, about 282 s median summed (feature-delta). Counted from the source. The three screened above are 40 of the 111 rows. The other nine, not screened row by row:

| Class | Rows | Same history-fill pattern? |
|---|---|---|
| Slice01PercentilesOverTimeTest | 3 | No. Base `PercentilesOverTimeAcceptanceTest`, reads of stored snapshots (no fill, no filler). Wire and read-shape rows; small |
| Slice02WorkItemAgePercentilesTest | 5 | No. Same base, stored snapshots. Small |
| Slice03ThroughputProcessBehaviorTest | 12 | No fill. Same base; limits read from stored snapshots. Likely mixed wire and rule |
| Slice03bDateRangeTest | 22 | No fill, but the largest of the nine by rows. Period parsing and ranges: mostly wire (query parameters, dates) with some rule rows on the range arithmetic |
| Slice04ProcessBehaviorMetricTypesTest | 15 | No fill. Metric-type to family mapping through the route; likely wire |
| Slice08NothingToShowTest | 5 | Yes, on the fill base `ReconstructOverTimeHistoryAcceptanceTest`, with the empty case (nothing to fill). Cheap Givens; the rules sit in `OverTimeFillTarget`, the writer and the memo |
| Slice09TheFillShipsOptInTest | 3 | Yes, on the fill base. About the opt-in switch for the fill; likely a wiring or configuration class |
| Slice10OnlyWhatTheTeamStillKeepsTest | 4 | Yes, on the fill base. About the retention edge; pinned by `RetentionEdgeTest` (same rule as Slice07 rows 4, 6 and 7) |
| Story6084TheFillRunsEverywhereTest | 2 | Yes, on the fill base. 18.3 s median for 2 rows, so about 9 s a row: the heaviest per row. The only other class of the family in the ranking top 20 (rank 17) |

Note: in the step 1 ranking only Slice05, Slice06, Slice07 and Story6084 of this family appear in the lines read, so the seconds of the other eight are not claimed here. Slice01 to Slice04 share a different base and carry no fill; do not expect the same saving per row.

## Candidate pilot

**Slice07WhereTheLimitsActuallyMovedTest.**

- Smallest harness change: none. Nine of its ten rows go to classes that already exist with doubles (`ProcessBehaviorSnapshotWriterTest` for rows 1, 2, 3, 5, 8, 9; `RetentionEdgeTest` for rows 4, 6, 7). No `ServicePortHarness` scenario is needed (Slice05 row 16 needs one).
- The computing classes are clear and small: `ProcessBehaviorSnapshotWriter.FamiliesFor`, `FillDayIfAbsent`, `RecordToday`, and `RetentionEdge`. Each rule has one production line a deliberate break can flip (a deleted family line, the zero-band gate, the retention anchor, the `asOf` argument).
- Largest saving per row of the five (about 8.8 s a row, 9 rows), so it settles the stop-line question of the pair early. The measured `u`, `c` and `hop` come from rows that move with little new code, which keeps the measurement clean.
- Caution for the crafter: rows 6, 7 and 9 are rewrites. Each needs its own break and an id row, and each old row stays until the new one is shown red then green. The "steady limits" claim of rows 6 and 7 is only partly the retention anchor; the claim that a pinned baseline gives the same triple on every day is computed by the metrics service and is not proved by the `RetentionEdge` rewrite. The id table should say so, and row 10 is left to cover the limits route.

Second choice: Slice06 (one class, one new tiny unit row on `PercentileFamilies`, ten rows leave), then Slice05 (largest row count, one port scenario).
