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

---

## 6. Suite 1 slice S4 — the other fill-base classes (screened 2026-11-14)

Read before screening, in full mode, fresh. Last line of each file, quoted back: `End of SKILL.md` and `End of backend.md`. The screen rules and words are those of sections 1 to 5.

Files for all four: `API/Integration/PercentilesOverTime/<Class>Scenarios.cs`, `...Specifications.cs`, base `ReconstructOverTimeHistoryAcceptanceTest.cs` (one host per fixture in `[OneTimeSetUp]`, one scope and one `Save()` per `SeedItemFinishedOn` call, one batched save per `SeedItemsFinishedOn` call). Rows are counted from the source and agree with `evidence/step1/family-ranking.txt` (12 classes, 111 rows: 3+5+12+22+15+19+11+10+5+3+4+2).

**Seconds, and what is NOT known.** Only Story6084TheFillRunsEverywhereTest is in the class ranking (rank 17, 18.3 s, 2 rows, about 9.2 s a row, an average). Slice08, Slice09 and Slice10 are not in it: the ranking lists 30 classes and ends at 14.8 s, so each of the three is at most 14.8 s, and that is all that is known per class. A second bound from the family file: 282.1 s minus Slice05 (89.7), Slice07 (88.3), Slice06 (48.6) and Story6084 (18.3) leaves 37.2 s for the other eight classes together. Those eight are Slice01, 02, 03, 03b, 04 (no fill) and the three below, so Slice08 + Slice09 + Slice10 are at most 37.2 s combined, and less by whatever Slice01 to 04 take. No per-class figure is claimed. The split between "fill" and "set-up" below is read from the source and not measured.

All four classes are on `HostUsersStillToMove` (`TestHostUseGuardTest.HostUsersStillToMove.cs`, lines 138 to 141). None is on `TypesAllowedToBuildAHostPerTest` in `TestHostPerFixtureGuardTest.cs` (it holds no PercentilesOverTime type), so `MostTypesTheListMayHold` (52) does not change for any of them. The only list edit a class leaving the host needs is its line on `HostUsersStillToMove`.

Production owners the rows go to (all exist): `OverTimeHistoryFiller.WalkAsync` / `IsOutsideWhatTheStoredItemsSupport`, `OverTimeFillTarget.For` (wraps every family in `retention.Guard` with the method group `.Select(retention.Guard)`, and passes `retention.NoEarlierThanTheEdge(...)` as `EarliestDayTheItemsSupport`), `RetentionEdge`, `PercentileSnapshotWriter`, `ProcessBehaviorSnapshotWriter`, `PercentilesOverTimeRecordingHandler`, `ProcessBehaviorRecordingHandler`.

Note on `OverTimeFillTarget`: a search for `.Guard(` in production finds nothing, because the call is a method group. The hop "the target applies the guard to each family and the edge to the first day" has no unit row today: `OverTimeFillTargetTest` has seven rows, none of them with a non-zero `DoneItemsCutoffDays` and none reading a family. `RetentionEdgeTest` pins the edge itself. Slice10 needs new rows on that hop (below).

### 6.1 Slice08NothingToShowTest (5 rows; seconds unknown, at most 14.8 s)

Where the time goes (from the source). Set-up per row: the fixture's host is built once; each row seeds a team (one scope, one save), then `GivenTheTeamFinishedOneItemADayFrom` makes one scope and one save per day: 121, 0, 81, 41 and 121 saves for rows 1 to 5. Fill: rows 1, 3 and 5 fill nothing (every asked day is outside what the stored items support, so the walk steps over all of them), row 2 has no items, only row 4 fills (up to 41 days, each day for every chart of the team). So the time is mostly set-up saves, with one real fill. The rows are cheap per row compared with Slice05.

| # | Test method | What it asserts | Rule or wire | Destination |
|---|---|---|---|---|
| 1 | A_period_that_predates_everything_the_team_holds_stays_empty_and_nothing_is_invented | A period wholly before the first finished item reads empty and no day is written; an in-progress item since 800 days back is there so that no absence gate hides a wrong write | rule | move (pin exists): `OverTimeHistoryFiller.IsOutsideWhatTheStoredItemsSupport` (`day < earliestDayTheItemsSupport`), `OverTimeHistoryFillerTest.TheFirstAndTheLastDayTheStoredItemsSupport_AreBothWorkedOut_AndTheDaysEitherSideAreNot` (`VerifyCycleTimeWorkedOutOn(firstFinished.AddDays(-1), Times.Never())`); with the writer a double the in-progress guard on the Given is not needed. Reads as stored: `GapAskingSeriesQueryTest.APercentileSeries_IsServedAsStored_AndTheDaysItHoldsArePassedOnForThePeriodAsked`. Also `ReconstructionMemoTest.DaysBeforeTheFirstFinishedItem_AreRuledOut_AndTheDayItFinishedOnIsNot` |
| 2 | A_team_with_nothing_in_it_at_all_stays_empty_and_nothing_is_invented | A team that never finished anything gets no day written | rule | move, one new row: `OverTimeHistoryFillerTest`, `AnOwnerThatHasNeverFinishedAnything_HasNoDayWorkedOut` (a team observed today, no finished item, ask 3 days, drain, every writer `Times.Never()`). Existing: `OverTimeFillTargetTest.ATeamThatHasNeverFinishedAnything_HasNoHistoryToFillFrom` pins the null first day, and `ReconstructionMemoTest.AnOwnerThatHasNeverFinishedAnything_HasEveryDayRuledOut` pins the memo, but no filler row pins that a null first day means no write. A break that removes `earliestDayTheItemsSupport is null ||` from `IsOutsideWhatTheStoredItemsSupport` makes `day < null` false and so lets the days through: red only on the new row |
| 3 | A_team_nobody_is_syncing_any_more_stays_empty_for_the_period_since_it_stopped | No day after the team's last refresh is written | rule | move (pin exists): same as Slice05 row 3: `TheFirstAndTheLastDay...` (`lastObserved.AddDays(1)` `Times.Never()`) and `OverTimeFillTargetTest.ATeamLastSeenInTheEveningWestOfUtc_WasLastObservedOnThatEveningsDay` |
| 4 | A_period_that_reaches_further_back_than_the_team_does_still_returns_the_part_it_covers | A 400-day period over 41 days of history still gets the 41 covered days (at least one held) | rule | rewrite at `OverTimeHistoryFiller` in `OverTimeHistoryFillerTest`: new row `DaysBeforeTheFirstFinishedItem_DoNotSpendThePassAllowance` (a team with its first finished item 5 days back, ask the 100 days before it and then those 5, a pass allowance of 90: all 5 are worked out). A break moves `progress.TriedADay()` ahead of the `continue` in `WalkAsync` (skipped days then eat the 90): the 5 days are not reached, red. The old host row says "at least one day", so it is weaker than the rewrite; say so in the id table. The reconciler side ("hand over every day of a long period, the pass steps over the unsupported ones for free") is pinned by `OverTimeGapReconcilerTest.APeriodSpanningCenturies_HandsOverTenYearsOfDaysAndNoMore` |
| 5 | The_limits_chart_stays_empty_for_a_period_that_predates_everything_the_team_holds | Same as row 1 for the limits chart | rule | move (pin exists): the walk is the same code for both chart kinds (`FillOneDayAsync` runs after the same gate); `OverTimeHistoryFillerTest.ADayWorkedOut_IsWorkedOutForTheOwnersLimitsToo` shows the limits writer is called for a worked-out day. One break of `day < earliestDayTheItemsSupport` turns row 1's pin red and this row's host test red together; the id table says that this one break stands for rows 1 and 5 |

Shared steps.

| Step | Asserts or does | Rule or wire | Where it goes |
|---|---|---|---|
| `GivenTheTeamFinishedOneItemADayFrom` (one scope and save per day) | Builds the Given | precondition, the set-up cost | gone with the move; unit rows use 1 to 6 items or none |
| `GivenAnItemTheTeamHasHadInProgressSinceBeforeThePeriod` | Guard that the write gates cannot hide a wrong write | guard on the Given | not needed at the filler (writer is a double) |
| `ThenTheChartComesBackEmpty` (`DatesIn`, JSON member `recordedAt`) | Empty series on the route | wire (member name) plus rule (empty) | the empty-series wire is shown by the stays of Slice05 row 12 and Slice07 row 10; nothing new stays here |
| `ThenNothingWasInventedToFillIt` (rows held before and after) | No row written | rule | writer `Times.Never()` at the filler rows above |
| `ThenTheChartStillCoversThePeriodItCan` | At least one covered day held | rule | row 4 rewrite |
| `WhenTheFlowCoachOpensTheCycleTimeTrend`, `WhenTheDeliveryLeadOpensTheLimits`, `WhenTheChartHasFinishedFillingIn` | Route and `DrainAsync` | wire, wiring | already stays elsewhere in the family |

Count: 4 move (one of them with a new row) + 1 rewrite + 0 stay = 5 rows.

Is it worth moving? Seconds unknown (at most 14.8 s). The class has no assertion about route, status, JSON or setting that is not shown by a row that stays elsewhere, so the whole class can leave: no host is started afterwards, its line comes off `HostUsersStillToMove`, and the fixture's host build goes too. New code: two filler rows (rows 2 and 4), both on `OverTimeHistoryFillerTest`, which slice S3 also edits, so S4 starts after S3 is committed. Break plan: one deliberate break per rule: (a) `day < earliestDayTheItemsSupport`, (b) `earliestDayTheItemsSupport is null ||`, (c) `day > lastObservedOn`, (d) `TriedADay()` order.

### 6.2 Slice09TheFillShipsOptInTest (3 rows; seconds unknown, at most 14.8 s)

Where the time goes. No fill pass runs in any row. Row 1 seeds 121 items (one batched save) and publishes `TeamDataRefreshed`, which runs both recording handlers (percentiles: three look-backs and age; limits: five families). Row 2 seeds 51 items and one in-progress item and does the same. Row 3 seeds 6 recorded percentile days and reads three times with a command interceptor. So the cost is the host, the seeding and one refresh event per row, and no per-day fill; this class is cheaper per row than the history-fill classes.

| # | Test method | What it asserts | Rule or wire | Destination |
|---|---|---|---|---|
| 1 | The_daily_recording_still_writes_today | After a refresh, today is held at all three cycle-time look-backs and no other day is written (percentiles and limits) | rule | move (pin exists): `PercentilesOverTimeRecordingHandlerTests.TeamDataRefreshed_RecordsOneRowPerHorizon_WithHorizonScopedPercentiles` and `...RecordsOnlyToday_NeverFabricatesEarlierDays`; limits: `ProcessBehaviorRecordingHandlerTests.TeamDataRefreshed_ReadyBaseline_RecordsTodaysThroughputLimits`. The "only today" half has a unit row for percentiles but none by name for the limits handler: the crafter breaks `RecordToday` day source (`clock.Today` in `ProcessBehaviorSnapshotWriter.RecordToday`) and, if only the host row goes red, adds one limits row first |
| 2 | A_quiet_day_is_still_left_blank_by_the_daily_recording | Cycle time is left blank for today while the age reading is written in the same refresh | rule | move (pin exists) for the blank rule: `PercentileSnapshotWriterTest.ATodayOnWhichNothingFinished_GetsNoRowFromTheDailyRecording` (exists now; section 1 row 8 asked for it); age beside it: `PercentilesOverTimeRecordingHandlerTests.TeamDataRefreshed_RecordsTodaysWorkItemAgePercentiles_AsASingleHorizonlessRow`. The two together in one refresh (the host row's "recording ran and chose blank") has no handler row: add `TeamDataRefreshed_NothingFinishedInAnyLookBack_RecordsNoCycleTimeRow_YetStillRecordsTodaysAge` |
| 3 | Opening_a_chart_never_looks_up_a_behaviour_setting | No command sent to the store while a chart opens names `OptionalFeatures`; the capture is not empty | wire (a negative about what the host sends) | stays: the only observer of the commands a read sends is an interceptor on the real host; a unit or architecture test cannot see what the controller, the reader and the reconciler together send. An ArchUnit rule that no FlowMetrics type depends on `OptionalFeature` would pin a smaller claim and is left to the maintainer, not assumed |

Shared steps: `CommandsSentToTheStore` (interceptor), `WhatOpeningTheCycleTimeTrendAsksOfTheStore` and `ThenNoBehaviourSettingWasLookedUp`: wiring, stay with row 3. `EverythingTheChartsHoldFor`, `ThenTodayWasRecordedOverEveryCycleTimeLookBack`, `ThenNothingButTodayWasWritten`, `ThenTheAgeReadingWasRecordedForToday`, `ThenNoCycleTimeReadingWasRecordedForToday`: rule, go with rows 1 and 2. `GivenEveryDayWasAlreadyRecordedFrom`: Given of row 3.

Count: 2 move (one with a new row, one that may need one) + 0 rewrite + 1 stay = 3 rows.

Is it worth moving? Probably not. Two rows leave a class that still starts a host (row 3 keeps `ConfigureAdditionalServices` and the fixture host), so its line stays on `HostUsersStillToMove` and the host build stays; what is saved is two seed-plus-refresh bodies, with no fill involved. Those cost less per row than the fill rows of the other classes. Seconds unknown. Add to this that the moved rows need up to two new handler rows to hold the same claim.

### 6.3 Slice10OnlyWhatTheTeamStillKeepsTest (4 rows; seconds unknown, at most 14.8 s)

Where the time goes. Every row seeds 201 finished items (one batched save), a team that keeps work for 90 days, and then fills: row 1 about 60 days (the period is day 90 back to day 30 back), row 2 the same, row 3 two teams (402 items) and about 11 days each, row 4 about 30 days. Each filled day is worked out for every chart of the team (four percentile families and five limit families), so the fill dominates: this is the same pattern as Slice05, with periods of 11 to 60 days. Of the three smaller classes this is the one expected to be slowest, but that is an inference from the source, not a measurement.

| # | Test method | What it asserts | Rule or wire | Destination |
|---|---|---|---|---|
| 1 | Cycle_time_is_filled_in_only_for_days_whose_whole_window_the_team_still_keeps | No 30-day cycle-time day reads past the edge; every day whose window lies inside it is filled | rule | move (pin exists): `RetentionEdgeTest.APercentileWindowReachingPastTheEdge_ComesBackEmpty_WithoutBeingRead` and `APercentileWindowStartingOnTheEdge_IsRead` (both sides of the boundary, the host row's `-30` offset); the window a day reads is `day - horizon` (`PercentileSnapshotWriterTest.ADayWithoutRows_IsWorkedOutAtEveryHorizon_OverAWindowEndingOnThatDay`). Plus new rows on `OverTimeFillTargetTest` (see below) for the hop that applies the guard |
| 2 | Limits_are_filled_in_only_for_days_whose_whole_stretch_the_team_still_keeps | Same for throughput limits, a stretch of 29 days back for a team with a throughput history of 30 | rule | move (pin exists): `RetentionEdgeTest.LimitsWhoseOwnWindowReachesPastTheEdge_ComeBackNotReady` and `LimitsWhoseWindowLiesInsideWhatIsKept_AreReadAsOfTheDayAsked`; the 29 days: `ProcessBehaviorRecordingHandlerTests.TeamDataRefreshed_ReadsTheSameAsOfTodayWindowTheWidgetUses` (window `ThroughputHistory - 1`, `LookbackDaysFor`). Plus the new target row for limit families |
| 3 | Limits_drawn_from_a_pinned_stretch_the_team_no_longer_keeps_are_not_filled_in | A stretch pinned before the edge gives no limits, judged from the last refresh, and a stretch pinned inside gives limits; a guard on the Given checks the stretch reads as of the oldest day opened | rule | move (pin exists): `RetentionEdgeTest.LimitsDrawnFromAPinnedStretchThatBeginsBeforeTheEdge_ComeBackNotReady_EvenForAWindowInsideWhatIsKept`, `ATeamsPinnedStretchOutOfReachToday_IsReadWhenTheEdgeIsDrawnFromItsLastRefresh`, `APinnedStretchInReachOnTheLastRefresh_IsOutOfReachOfAnEdgeDrawnOnALaterDay`. These are the same rows that pin Slice07 rows 6 and 7 (screen section 2); the break for the last-refresh anchor made there stands for this row as well, and the id table says so. The Given guard `GivenThatStretchStillReadsAsOfTheOldestDayOpened` is a check on the metrics service and goes away with the move |
| 4 | Work_item_age_is_filled_in_from_the_oldest_day_the_team_still_keeps_and_not_before | Age days before the edge are not written; every day from the edge on is | rule | move (pin exists): `RetentionEdgeTest.FinishedWorkOlderThanWhatIsKept_MovesTheFirstDayTheWalkMayWorkOutUpToTheEdge` and `FinishedWorkThatBeganInsideWhatIsKept_LeavesTheFirstDayWhereTheWorkBegan`, and the walk `OverTimeHistoryFillerTest.TheFirstAndTheLastDay...`. Plus the new target row on the first day |

New rows needed on the hop that no unit pins today, all on `OverTimeFillTargetTest` (its `ServicesKnowing` already supplies the team, the items and the clock; it passes empty limit families and a bare metrics double, so a family or two is added to it):

- `ATeamsPercentileFamilies_ReadNothingForAWindowReachingPastWhatItKeeps`: a team that keeps 30 days, observed on a fixed day; a family from `target.PercentileFamilies` asked for a window starting the day before the edge returns no reading and the metrics double is not called; a window starting on the edge is read. A break removes `.Select(retention.Guard)` for the team in `ForTeam`: red.
- `APortfoliosPercentileFamilies_...`: the same for `ForPortfolio`, so each line of the two has its own break.
- `ATeamsLimitFamilies_...`: one limit family returned by the writer double comes back wrapped (not ready for a stretch past the edge).
- `ATeamThatKeepsWorkForThirtyDays_HasNoHistoryBeforeTheEdge`: items finished well before the edge, `EarliestDayTheItemsSupport()` equals the edge, not the item's day. A break of `retention.NoEarlierThanTheEdge(...)` in `ForTeam` (the call replaced by the bare earliest day): red. A portfolio twin for `ForPortfolio`.

Shared steps: `SeedTeamObservedUntil(..., doneItemsCutoffDays)`, `GivenTheTeamFinishedOneItemADayFrom`: set-up, gone. `PinTheTeamsBaselineTo`: Given, gone. `ThroughputLimitsWorkedOutOver`: the Given guard, gone. `CycleTimeDaysHeldFor`, `AgeDaysHeldFor`, `LimitDaysHeldFor`, `DaysFrom`, all `Then*` steps: rule, unit rows above. `ReadTeamPercentileTrend` and `ReadTeamLimitTrend` are the wire and are shown by Slice05 row 12 and Slice07 row 10, which stay.

Count: 4 move (each with its pin; 4 to 6 new `OverTimeFillTargetTest` rows added) + 0 rewrite + 0 stay = 4 rows.

Is it worth moving? Yes, as far as a saving can be judged without a measurement: four rows, each with a fill of 11 to 60 days over nine charts, so it is the likeliest of the three to hold most of the unknown seconds (at most 14.8 s). The class asserts nothing about route or wiring, so it leaves the host completely: its line comes off `HostUsersStillToMove`. Break plan, one per rule: (a) the boundary comparison in `RetentionEdge.ReachesPastTheEdge`, (b) the limits own-window judgment in `Guard`, (c) the pinned-stretch judgment, (d) `NoEarlierThanTheEdge`, (e) each of the two `.Select(retention.Guard)` lines and each of the two `NoEarlierThanTheEdge` lines in `OverTimeFillTarget`.

### 6.4 Story6084TheFillRunsEverywhereTest (2 rows, 18.3 s median, rank 17, about 9.2 s a row)

Row count checked against the source: 2 `[Test]` methods, no `TestCase`. Agrees with the ranking.

Where the time goes. Row 1 seeds a team of 121 items (one batched save) and a portfolio of 121 deliveries (one `SeedDeliveryFinishedOn` call per day), rewrites the stored setting and the one-time record straight in the store, opens both charts and fills a 31-day period for each, on all of the team's and the portfolio's charts. Row 2 seeds two teams of 121 items, runs the seeders, holds a real pass open on a dedicated thread, opens a second chart, drops an old setting into the store, then fills both owners. The fill, for two owners, dominates both rows; the 9 s average is the highest per row of the family.

| # | Test method | What it asserts | Rule or wire | Destination |
|---|---|---|---|---|
| 1 | After_the_upgrade_an_instance_whose_admin_had_switched_the_fill_off_fills_in_both_over_time_charts | With an old "fill off" setting and the one-time record stored, opening both charts still fills every day for a team and a portfolio | wiring (the stored setting flows through the host and nothing reads it) | stays: the only row that proves the stored value is in the store and the real instance still fills. Day coverage for team and portfolio is pinned already (`OverTimeHistoryFillerTest.EveryDayOfTheAskedPeriod_IsWorkedOutForEveryPercentileFamilyOfATeam` and `...OfAPortfolio`, `ADayWorkedOut_IsWorkedOutForTheOwnersLimitsToo`), but "no setting stops it" is a negative that the filler's constructor shows and no unit row can fail |
| 2 | A_chart_waiting_its_turn_still_fills_in_whatever_an_old_fill_setting_says | A second owner waiting behind a held pass fills although an old "off" setting appears meanwhile | wiring, needs a held pass | stays: needs a real pass parked on its own thread (`AReconstructionPassHeldInFlight`) and a setting written into the real store; no port scenario holds a pass. Same reason as Slice05 row 18 |

Shared steps: `GivenTheLastReleaseLeftTheFillSwitchedOffByTheAdmin`, `WhenAnOldSwitchedOffFillSettingTurnsUp`, `GivenTheInstanceRunsThisRelease` (the seeders): wiring, stay. `ThenTheChartHoldsEveryDayFrom`: rule shown at the filler; asserts coverage only.

Count: 0 move + 0 rewrite + 2 stay = 2 rows.

Is it worth moving? No. Both rows stay under the screen's own table (a stored setting flowing through the host; a held pass on its own thread). The one saving available is not a move: row 1 could use 61 days of history instead of 121, since the period is 31 days and the window 30; that is an edit to a Given, not a rewrite of an assertion, and is left to the maintainer. The class stays on `HostUsersStillToMove`. Note the sibling `BehaviourSettings.Story6084HistoryFillAlwaysOnTest` (21.0 s, 12 rows, rank 11) has a similar name and is not in this family: do not confuse them.

### 6.5 Recommendation for S4 (last slice of suite 1)

| Class | In S4? | Reason |
|---|---|---|
| Slice10OnlyWhatTheTeamStillKeepsTest | Yes | 4 of 4 rows are rules with a pin on `RetentionEdgeTest`; fill-heavy; the class leaves the host entirely; needs 4 to 6 small new rows on `OverTimeFillTargetTest` for a hop no unit pins today |
| Slice08NothingToShowTest | Yes | 5 of 5 rows leave (4 move, 1 rewrite); the class leaves the host entirely; needs 2 new filler rows; cheap per row, so the saving is smaller than Slice10's, but the rows share the same two test classes and the same walk |
| Slice09TheFillShipsOptInTest | No | No fill in any row; row 3 (negative about what the host sends) must stay, so the class keeps its host and its guard line, and only two seed-plus-refresh bodies leave; up to two new handler rows to hold the same claim |
| Story6084TheFillRunsEverywhereTest | No | Both rows stay (setting through the host; a held pass); nothing to move |

S4 therefore carries Slice10 and Slice08: 9 host rows leave, two classes leave the host, and the new rows land on `OverTimeFillTargetTest` and `OverTimeHistoryFillerTest`. Dependency on S3: S3 edits `OverTimeHistoryFillerTest` and `PercentileSnapshotWriterTest`, and S4 adds rows to `OverTimeHistoryFillerTest`, so S4 starts after S3 is committed. The saving per class is not measured: Slice08 plus Slice10 are at most 37.2 s together by the family bound, and at most 14.8 s each by the ranking cutoff, and the measured saving comes from the step 3 before/after runs. After S4, what is left of the family on the host is: Slice01 to Slice04 (different base, wire and read shape, no fill), Slice09, Story6084 and the stays of Slices 05, 06 and 07.

---

## 7. Suite 2 — the ConfigureOncePerConnection family (screened 2026-11-14)

Read before screening, in full mode, fresh. Last line of each file, quoted back: `.claude/skills/test-writing/SKILL.md`: `End of SKILL.md`; `.claude/skills/test-writing/references/backend.md`: `End of backend.md`.

Rules used: "The screen: what a unit test can pin" of feature-delta.md. Three of its rows decide most of the family: a rule whose Given is built through the real save route stays unless a builder on the model can make the Given; logic in a controller (mapping, status choice) stays and is recorded for #6508; whatever needs the update queue stays. The words (move / move (pin exists) / rewrite / stays) are those of section 1. **No rewrite is proposed**: no host row of this family is slow in a fill (there is none); a row that has no unit twin gets the same Given on the connector's or the save service's own doubles, written here as `move (add row)`. So the maintainer's yes to rewrites is not needed for suite 2.

**This family is fake-tracker only. No row touches a live tracker.** Jira rows run the real `JiraWorkTrackingConnector` over a mocked `HttpMessageHandler` (`AJiraAnsweringForThatInstance`); Azure DevOps rows run the real `AzureDevOpsWorkTrackingConnector` over a mocked `WorkItemTrackingHttpClient` (`AnAzureDevOpsOrganisation`); the ConnectionDefaults classes hand the host a `Mock<IWorkTrackingConnector>`. Rows that are connector-specific code (Jira, Azure DevOps) are marked **[connector]**. They move only to the connector's own unit class, which already uses the same kind of double. None of them is proven only through a live connector, so none stays for that reason.

### How the seconds were got

Rows: counted from the source (each `[Test]` and each `[TestCase]`): 13 + 7 + 24 + 19 + 18 + 32 + 5 + 9 + 3 + 5 + 6 + 20 + 2 = 163, which agrees with `evidence/step1/family-ranking.txt` (13 classes, 163 rows, 164.4 s). Measured per-class CI seconds exist only for the five classes in the top 30 of `evidence/step1/runs-and-class-ranking.txt`: Slice05b 19.6 s / 32 rows, Slice02 19.1 s / 7, Slice04 16.5 s / 19, Slice12 15.8 s / 20, Slice06 15.2 s / 5. Together 86.2 s. **Bounds, not measurements, for the other eight classes** (Slice01, 03, 05a, 08, 09, 10, 11, ClockIsolation): the cut-off of the top 30 is 14.8 s, so each is at most 14.8 s; together they hold 164.4 - 86.2 = 78.2 s over 80 rows (an average of about 0.98 s a row, which is arithmetic, not a measurement). Where a split inside a class between "set-up" and "refresh" is given, it is read from the source and is not measured per row.

### Where the time goes in this family (read from the source)

Two bases, same pattern. `ConnectionDefaultsAcceptanceTest` (Slice05a, 05b, 06, 08, 09, 10, 11, 12): one host built per fixture (`OneTimeSetUp`); per row `rootFactory.UseFreshDatabase()`, `EnsureCreated`, every `ISeeder`, then `SeedThePaymentsDepartment` (2 connections, 4 Teams, 2 Portfolios, each through its repository) and, at tear-down, `TheQueueGoesIdle` and `EnsureDeleted`. `GivenEveryTeamAndPortfolioHasBeenUpdatedOnce` then runs **6 refreshes** (4 Teams, 2 Portfolios), each one a `TriggerUpdate` and a wait for the queue to go idle; `WhatRanSinceTheSave` polls every 50 ms up to 30 s. `NamedDependencyLinkAcceptanceTest` (Slice01 to 04, ClockIsolation): one host per fixture; per row `UseFreshDatabase`, `EnsureCreated`, seeders, the Portfolio saved, then one `TriggerUpdate` plus a wait for the queue (60 s ceiling) for each refresh the row asks for; tear-down `LeaveTheHostIdleOrFail` and `EnsureDeleted`. So the cost per row is set-up and refresh, never a fill. This is why seconds a row differ: Slice02 (2.7 s a row) and Slice06 (3.0 s a row) are slow because their rows run 4 to many refreshes (Slice02 rows 6 and 7 refresh until a cheap run, then once more than the retention count; Slice06 runs the 6-refresh Given in 4 of 5 rows); Slice05b (0.61 s), Slice12 (0.79 s) and Slice04 (0.87 s) are mostly per-row set-up. A saved row moved to unit level removes its set-up and refreshes. Each class that keeps even one host row keeps its host build (the backend half measures 1.8 s for a host build outside a TRX); that cost is **not measured here**.

Names of the unit homes used below (all exist; found with `ctx_search`): `ConnectionSaveServiceTest` (Commit_* rows: logs, followers, publish matrix `Commit_UpdatesExactlyTheOwnersWhoseFetchTheSaveMoved`, refusal of a removed field), `EffectiveSettingTest` (source and default per setting, for Team and Portfolio), `IncomingSettingTest` (18-row matrix of what a save sends), `OwnerSettingRefusalsTest`, `EstimationBundleTest`, `SupportedConnectionDefaultsTest`, `ConnectionDefaultsDtoTest`, `SettingsOwnerDtoRoundTripTest`, `WorkTrackingSystemConnectionUsageTest` (followers), `RefreshLogServiceTest`, `JiraNamedDependencyLinkTest` **[connector]**, `AzureDevOpsNamedRelationEndTest` **[connector]**. "Pin exists" is claimed **by name only**: the crafter still shows, by one deliberate break per rule, that the existing row goes red for the same production line; if it does not, add one row first.

All 13 classes are on `HostUsersStillToMove` (lines 41 to 48 of `TestHostUseGuardTest.HostUsersStillToMove.cs` name ClockIsolation and Slice01 to 06; the remaining lines were not read for this screen). A class leaves the list in the commit that takes its last host row away.

---

### 7.1 Slice05bReferencedFieldsAndOlderPayloadsScenarios (19.6 s, 32 rows, about 0.61 s a row)

Base: ConnectionDefaults. Time: per-row set-up plus the save route; rows 4 to 8 and 16 to 29 are TestCase groups. A row that removes a field reads the seeded Teams and Portfolios.

| # | Test method (short) | What it asserts | Rule or wire | Destination / reason |
|---|---|---|---|---|
| 1 | A_field_the_default_and_a_Team_use_cannot_be_removed... | 409, code `additional-field-in-use`, exact message, nothing saved | wire plus rule | **stays** (wire: status, code, the saved-or-not read-back). The wording is pinned by the rule rows below |
| 2 | A_field_only_a_Team_uses_as_its_own... | refusal names "Ledger uses it as its own" | rule | move (pin exists): `ConnectionSaveServiceTest.Commit_RemovingAFieldTeamsOnThisConnectionChoseThemselves_IsRefusedNamingThoseTeamsInTheOrderTheyWereCreated` |
| 3 | A_field_only_a_Teams_estimation_uses... | message for the Team's estimation field | rule | move (add row): same class, estimation-field variant (`WorkTrackingSystemConnectionUsage` / refusal builder) |
| 4-8 | A_field_only_a_Portfolio_uses_as_its_own (5 TestCase: Parent, Dependency, Size, Owner, Estimation) | message per setting | rule | move (pin exists, partial): `Commit_RemovingAFieldPortfoliosOnThisConnectionChoseForAnySetting_...` and the four `Commit_RemovingTheFieldThePortfolios{Dependency,SizeEstimate,FeatureOwner,Estimation}DefaultNames_...`; add one TestCase per setting that has none |
| 9 | A_field_only_a_default_names_cannot_be_removed | message "It is the Teams' Parent sources default" | rule | move (pin exists): `Commit_RemovingTheFieldThePortfoliosDefaultNames_IsRefusedNamingThatDefault` and `Commit_RemovingAFieldBothDefaultsName_...`; add the Teams-default variant if the break does not flip |
| 10 | A_field_nobody_uses_is_removed_as_today | an unused field is removed | rule | move (add row): `ConnectionSaveServiceTest`, the happy path of a removal (`Commit_RemovingAFieldATeamListedBeforeItWentBackToTheDefault_IsSaved` is the nearest) |
| 11 | A_Team_that_starts_using_the_field_while_the_removal_is_being_saved_still_blocks_it | a save that lands in the gap (`ASaveLandsInTheGap` decorates `IConnectionSaveService`) | race on the real store | **stays**: needs the real save pipeline and the real store; a mock would assert itself |
| 12 | A_stale_copy_that_removes_a_field_in_use_is_refused_as_stale | 409 `concurrency-conflict`, nothing changed | wire plus concurrency token | **stays**: the token check lives in the real store; the status is the controller's. Unit side already: `Commit_TheCopyIsStale_LetsTheConflictThroughAndLogsAndPublishesNothing` |
| 13 | A_Team_cannot_name_a_field_its_connection_does_not_have | 400, code `team-field-not-on-connection`, nothing saved | wire plus rule | **stays** (400 and code through the controller). The rule: `OwnerSettingRefusalsTest.One_field_the_connection_lacks_refuses_a_list_whose_other_fields_it_holds` |
| 14 | A_new_Team_cannot_name_a_field_its_connection_does_not_have | same for a new Team | rule | move (pin exists): same `OwnerSettingRefusalsTest` row; the "new Team is not listed" half is the controller's and is the same family as row 13 |
| 15 | A_Team_saved_with_a_source_Lighthouse_does_not_know_is_refused | code `team-setting-source-unknown` for source 99 | rule (the 99 as a JSON number is the wire half) | move (pin exists): `OwnerSettingRefusalsTest.A_source_this_version_does_not_know_is_refused_as_the_source_of_the_list`; JSON enum binding: `SettingsOwnerDtoRoundTripTest` |
| 16-22 | A_Team_saved_by_a_client_that_may_not_send_the_source (7 TestCase) | source and reading after a save with or without source | rule | move (pin exists): `IncomingSettingTest` matrix (18 rows) covers all seven pairs (follower sends nothing / what it reads / another; owner sends nothing / another; None; as-following) |
| 23-29 | A_script_that_saves_back_what_it_read_changes_nothing (7 TestCase) | echoing every property or dropping the unknown ones keeps source and reading | rule plus DTO shape | move (pin exists): `IncomingSettingTest` plus `ConnectionDefaultsDtoTest` / `TeamExtensionsTest` / `PortfolioExtensionsTest` round-trip rows (`A_follower_saved_back_without_its_source_keeps_following`, `An_older_clients_round_trip_keeps_...`) and `SettingsOwnerDtoRoundTripTest.EverySettingOf{ATeam,APortfolio}SurvivesBeingSavedAndReadBack` |
| 30 | A_stale_copy_saved_back_without_its_source_makes_the_Team_own_the_old_default | follower becomes Own with the old value | rule | move (pin exists): `IncomingSettingTest` (ConnectionDefault, sends another field) |
| 31 | A_stale_copy_saved_back_with_its_source_keeps_the_Team_following_the_new_default | stays following | rule | move (pin exists): `IncomingSettingTest` (ConnectionDefault, sends what the owner reads) |
| 32 | A_stale_copy_of_a_follower_still_naming_a_removed_field_keeps_the_Team_following | follower keeps following after the field is removed | rule | move (pin exists): same matrix row; the removal half is `ConnectionSaveServiceTest` |

Count: move 28 + rewrite 0 + stays 4 = 32. After the moves the class still starts a host (4 rows). Projected saving: 19.6 s x 28/32 = **17.2 s** (a projection; the stays are the rows with the real store).

---

### 7.2 Slice02NamedLinkThatMatchedNothingScenarios (19.1 s, 7 rows, about 2.7 s a row) [connector]

Base: NamedDependencyLink. Time: each row seeds a Jira Portfolio and runs one refresh through `IPortfolioUpdater`; rows 6 and 7 refresh until a cheap (Delta) run (2 to 4 refreshes), then row 6 saves a setting and refreshes again, and row 7 refreshes `GetRefreshLogRetentionRuns() + 1` times. That is where the 19.1 s sits. Jira is a mocked `HttpMessageHandler`. Production: `JiraWorkTrackingConnector` (finding `NamedLinkMatchedNothing`, `RefreshFinding`), `RefreshLogService` (which finding survives pruning), the Refresh History answer (`SystemInfoController`).

| # | Test method (short) | What it asserts | Rule or wire | Destination / reason |
|---|---|---|---|---|
| 1 | Nothing_matched_the_named_link_and_Refresh_History_lists_the_links_that_were_seen | one finding of kind, setting, named, seen; `FindingsAsOf` = newest run; one aggregated log line | wire plus rule | **stays**: the one row that runs refresh, log and the Refresh History route end to end. The rule halves are pinned below |
| 2 | Several_Features_without_the_link_share_one_finding_and_one_line | one finding, one line for 3 Features | rule | move (pin exists): `JiraNamedDependencyLinkTest.Features_carrying_only_the_outward_end_of_other_links_are_told_the_named_link_matched_nothing`; confirm that the pin counts findings, else add the count |
| 3 | Something_matched_so_Refresh_History_shows_no_finding | no finding when one Feature carries the link | rule | move (add row): `JiraNamedDependencyLinkTest`, a Feature that carries the link and one that does not |
| 4 | A_Portfolio_whose_Features_carry_no_links_at_all_is_told_nothing | no finding without any link | rule | move (add row): same class |
| 5 | A_Dependency_Field_naming_an_ordinary_field_stays_silent | no finding for an ordinary field | rule | move (pin exists): `JiraNamedDependencyLinkTest.A_Dependency_Field_naming_a_field_reads_what_was_typed_into_it` |
| 6 | A_cheap_refresh_keeps_showing_the_full_downloads_finding_and_its_time... | after a Delta run the finding and its time are those of the last Full run; a later Full run that found it removes it | rule (which run's findings read) | move (pin exists): `RefreshLogServiceTest.LogRefreshAsync_NewerFullRunFoundNothing_PrunesTheOlderRunsFindings` and `..._MoreCheapRunsThanKept_KeepsTheNewestRunThatRecordedFindings`. The read side that fills `FindingsAsOf` and `NewestRunMode` was not located in this screen: locate it with `ctx_search` (not `FindingsAsOf`, which is the DTO word) and add a row there if the pin does not cover the time label |
| 7 | The_finding_still_shows_after_more_cheap_refreshes_than_Refresh_History_keeps | the full run's row is not pruned | rule | move (pin exists): `RefreshLogServiceTest.LogRefreshAsync_MoreCheapRunsThanKept_KeepsTheNewestRunThatRecordedFindings` (a direct match) |

Count: move 6 + rewrite 0 + stays 1 = 7. Class keeps its host (1 row). Projected saving: 19.1 s x 6/7 = **16.4 s**. The two rows that do most of the refreshing (6 and 7) both leave, so the real saving may be higher than the share says.

---

### 7.3 Slice04AzureDevOpsDependenciesFromANamedRelationEndScenarios (16.5 s, 19 rows, about 0.87 s a row) [connector]

Base: NamedDependencyLink, Azure DevOps through a mocked `WorkItemTrackingHttpClient` (each row stores a Portfolio on an organisation and refreshes once, some twice). Unit home: `AzureDevOpsNamedRelationEndTest` (it has the same kind of double).

| # | Test method (short) | What it asserts | Rule or wire | Destination / reason |
|---|---|---|---|---|
| 1 | A_Feature_waits_on_the_work_item_at_the_other_end_of_the_named_relation | a Feature lists the work item at the named end, source `TrackerLink` | rule plus read route | **stays** (the class's one end-to-end row: refresh, store, Feature Dependencies route). Rule: pinned by `GetFeaturesForProject_WhenTheNamedEndIsCarried_ReadsThatEndAloneAsALinkDrawnInTheTracker` |
| 2 | Naming_the_reverse_end_reads_the_other_direction | reverse end | rule | move (add row): same class, reverse end |
| 3 | Naming_a_relation_end_replaces_Predecessor_for_that_Portfolio | named end replaces Predecessor | rule | move (add row): same class |
| 4 | A_Parent_Override_Field_on_the_same_Portfolio_does_not_stop_the_relation_being_read | both settings read | rule | move (add row): same class |
| 5 | A_relation_type_named_without_its_end_reads_nothing_and_names_both_ends | nothing read, both ends named | rule | move (pin exists): `..._WhenTheBaseNameIsNamed_ReadsNothingAndNamesBothEnds` |
| 6 | Related_has_no_direction_so_it_reads_nothing_and_says_so | Related reads nothing | rule | move (pin exists): `..._WhenARelationTypeWithoutDirectionIsNamed_ReadsNothingAndSaysWhy` |
| 7 | Nothing_carrying_the_named_end_says_so_once_and_lists_the_relation_types_seen | one finding, types seen | rule | move (pin exists): `..._WhenNoFeatureCarriesTheNamedEnd_SaysSoNamingOnlyTheWorkItemLinksSeen` |
| 8 | Features_carrying_only_their_hierarchy_say_nothing_about_the_named_end | no finding for hierarchy only | rule | move (pin exists, confirm): same row; add a hierarchy-only case if missing |
| 9 | The_hierarchy_is_left_out_of_the_relation_types_seen | hierarchy not in the seen list | rule | move (pin exists, confirm): same row |
| 10 | Features_carrying_no_relations_at_all_say_nothing_about_the_named_end | no finding | rule | move (pin exists): `..._WhenTheFeaturesCarryNoRelationsAtAll_SaysNothing` |
| 11 | Resource_links_are_left_out_of_the_relation_types_seen | resource links not in the list | rule | move (add row): same class |
| 12 | A_relation_type_named_without_its_end_validates_so_the_refresh_can_say_which_ends_it_has | validate accepts the name | rule | move (pin exists): `ValidateConnection_WhenAReferenceNamesARelationType_AcceptsIt` |
| 13 | Related_validates_so_the_refresh_can_say_it_has_no_direction | validate accepts Related | rule | move (pin exists): same TestCase class |
| 14 | A_name_without_an_end_whose_ends_are_not_both_listed_is_still_a_typo | refused | rule | move (pin exists): `ValidateConnection_WhenAReferenceNamesNoFieldAndNoRelationType_RefusesIt` |
| 15 | Naming_a_relation_end_costs_one_relation_type_listing_and_nothing_per_work_item | one listing request | rule | move (add row): counting client in `AzureDevOpsNamedRelationEndTest` |
| 16 | Two_organisations_on_one_shared_connector_each_answer_with_their_own_relation_types | the connector keys the listing by organisation | rule | move (add row): same class, one connector instance, two organisations |
| 17 | A_refresh_that_cannot_list_the_relation_types_stops_and_keeps_what_the_last_one_read | the refresh stops, stored dependencies stay | updater plus store | **stays**: the connector half is `GetFeaturesForProject_WhenTheDependencyFieldCannotBeLookedUp_StopsWithASentenceAnOperatorCanActOn`; "keeps what was stored" runs through the updater and the store, which T10 does not widen into a port |
| 18 | Refreshing_the_first_organisation_again_lists_its_relation_types_again | no stale cache | rule | move (add row): same class as row 16 |
| 19 | A_Portfolio_naming_nothing_still_reads_Predecessor_without_listing_relation_types | no listing when nothing is named | rule | move (pin exists): `..._WhenNoRelationEndIsNamed_SaysNothingAboutThePredecessorLinkGoingUnmatched` and `..._WhenOnlyAnUnusedDefinitionNamesNoField_DoesNotListTheRelationTypes` |

Count: move 17 + rewrite 0 + stays 2 = 19. Class keeps its host (2 rows). Projected saving: 16.5 s x 17/19 = **14.8 s**.

---

### 7.4 Slice12EstimationDefaultScenarios (15.8 s, 20 rows, about 0.79 s a row)

Base: ConnectionDefaults. Rows 1 to 5 refresh nothing (estimation is read on page load) so most of the cost is set-up.

| # | Test method (short) | What it asserts | Rule or wire | Destination / reason |
|---|---|---|---|---|
| 1 | Every_Team_without_an_estimation_shows_the_default_on_its_next_page_load_and_nothing_updates | followers read the default; 4 follow; log; no update | rule | move (pin exists): `EstimationBundleTest` (following reads the default), `ConnectionSaveServiceTest.Commit_OnlyTheCategoriesOfTheTeamsEstimationDefaultChanged_...` and the publish matrix; the follower count of the Estimation default goes on `WorkTrackingSystemConnectionUsageTest` if not covered (add row) |
| 2 | Changing_only_the_unit_of_the_Estimation_default_is_logged_with_the_whole_default_before_and_after | log line | rule | move (pin exists): `ConnectionSaveServiceTest.Commit_OnlyTheCategoriesOfTheTeamsEstimationDefaultChanged_LogsOneEntryNamingTheWholeDefaultBeforeAndAfter`, `Commit_TheTeamsEstimationDefaultSetToANumericField_...` |
| 3 | Clearing_the_Estimation_default_leaves_the_followers_without_an_estimation | followers read nothing | rule | move (add row): `EstimationBundleTest`, a following Team with no default reads no field |
| 4 | A_Portfolio_with_its_own_estimation_keeps_it_and_the_Teams_default_never_reaches_a_Portfolio | own kept; no cross-grain | rule | move (pin exists): `EstimationBundleTest.A_following_Portfolio_reads_the_Portfolios_default_and_not_the_Teams`, `EffectiveSettingTest.A_Portfolio_has_no_Teams_default_to_follow` |
| 5 | A_following_Portfolio_shows_the_Portfolios_Estimation_default | Portfolio reads its own default | rule | move (pin exists): same |
| 6 | A_Team_admin_without_connection_rights_sees_the_Estimation_default_with_its_unit | the read-only view a Team admin gets, tracker name "Jira" | RBAC and wire | **stays** (the same family as Slice05a row 5; 403/401 and view shape belong to the wire) |
| 7-8 | A_script_that_sends_the_estimation_categories_as_null... (2 TestCase) | save accepted, owner keeps following | rule plus DTO | move (pin exists): `EstimationBundleTest.A_bundle_without_a_field_leaves_the_Team_following`, `EstimationFieldSyncTest.Sync*EstimationCategoryValues`; add a null-categories DTO row to `SettingsOwnerDtoRoundTripTest` if the break does not flip |
| 9-10 | A_script_that_saves_back_a_followers_estimation_leaves_it_following (2 TestCase) | echo or drop-unknown keeps following | rule | move (pin exists): `EstimationBundleTest.A_bundle_equal_to_what_the_Team_reads_leaves_it_following` |
| 11 | A_script_that_drops_the_unit_and_switch_of_a_non_numeric_default_makes_the_follower_own_a_numeric_estimation | follower becomes Own, numeric | rule | move (pin exists): `EstimationBundleTest.A_bundle_differing_in_one_part_becomes_the_Teams_own_whole` |
| 12-13 | An_owner_sent_back_an_empty_estimation_without_a_source_keeps_its_own (2 TestCase) | owner keeps own | rule | move (pin exists): `EstimationBundleTest.A_bundle_without_a_field_leaves_an_owning_Team_its_own_bundle` |
| 14 | An_owner_saved_with_an_Estimation_source_Lighthouse_does_not_know_is_refused (Team) | 400, code, exact message | wire plus rule | **stays** (400 and wording through the controller); rule pinned by `OwnerSettingRefusalsTest` |
| 15 | same, Portfolio | code `portfolio-setting-source-unknown` | rule | move (pin exists): `OwnerSettingRefusalsTest.A_portfolios_refusal_carries_its_own_code_and_noun` and the unknown-source row |
| 16 | A_Team_moved_to_another_connection_with_an_Estimation_source_Lighthouse_does_not_know_is_refused_and_keeps_its_Work_Items | refusal, Work Items kept | rule | move (pin exists): `OwnerSettingRefusalsTest` unknown-source row; "kept" = nothing saved, the same half as row 14 |
| 17-18 | An_owner_moved_to_another_connection_while_owning_an_Estimation_Field_of_the_old_one_is_refused (Team, Portfolio) | code, message with noun | rule | move (pin exists): `OwnerSettingRefusalsTest.One_field_the_connection_lacks_...` and `A_portfolios_refusal_carries_its_own_code_and_noun` |
| 19-20 | A_new_owner_created_with_an_Estimation_source_Lighthouse_does_not_know_is_refused (Team, Portfolio) | code, not listed | rule | move (pin exists): same unknown-source row |

Count: move 18 + rewrite 0 + stays 2 = 20. Class keeps its host (2 rows). Projected saving: 15.8 s x 18/20 = **14.2 s**.

---

### 7.5 Slice06DefaultChangeUpdatesTheFollowersScenarios (15.2 s, 5 rows, about 3.0 s a row)

Base: ConnectionDefaults. Time: rows 1 to 4 run `GivenEveryTeamAndPortfolioHasBeenUpdatedOnce` (6 refreshes, each waiting for the queue), then the save, then `WhatRanSinceTheSave` (poll up to 30 s). Row 5 the same.

| # | Test method (short) | What it asserts | Rule or wire | Destination / reason |
|---|---|---|---|---|
| 1 | Save_and_update_updates_exactly_the_Teams_that_follow_the_changed_default | listed in the Task Manager, ran Full, items hang under the new default, one log entry | wiring (the walking skeleton of the feature) | **stays**: the one row that proves save, queue, follower update, reading and log together |
| 2 | Save_only_starts_no_update_and_still_names_the_followers | nothing ran; log names the followers | rule | move (pin exists): `ConnectionSaveServiceTest.Commit_UpdatesExactlyTheOwnersWhoseFetchTheSaveMoved` (startUpdates false rows) and `Commit_TheTeamsDefaultChanged_LogsOneEntryNamingTheFollowersInTheOrderTheyWereCreated` |
| 3 | After_save_only_a_follower_picks_the_change_up_at_its_next_scheduled_update | the next scheduled update downloads everything once | rule (fetch-shape change) plus queue | **stays**: needs the update queue; its rule (the fetch fingerprint moved, so a Full run) was not located in this screen. If the crafter finds the unit that decides Full versus Delta by the fetch fingerprint, this row moves; the Given can also shrink to one follower updated once instead of six |
| 4 | When_nobody_follows_the_default_nothing_updates_and_the_entry_says_none_follow | no update; entry "no Team follows it" | rule | move (pin exists): `Commit_TheTeamsDefaultSetButEveryTeamOwnsItsField_LogsOneEntrySayingNoTeamFollowsIt` |
| 5 | A_changed_default_does_not_update_the_owners_or_the_Portfolios | followers updated, owners and Portfolios not | rule | move (pin exists): the publish matrix (`Own`, `ChangesTheTeamsDefault`, startUpdates true: nothing) |

Count: move 3 + rewrite 0 + stays 2 = 5. Class keeps its host (2 rows). Projected saving: 15.2 s x 3/5 = **9.1 s**. The formula undercounts: the three leaving rows are 3 of the 4 six-refresh Givens.

---

### 7.6 The eight classes without a measured time (bound: at most 14.8 s each, 78.2 s together over 80 rows)

**Slice01JiraDependenciesFromANamedLinkScenarios (13 rows) [connector]** (Jira over a mocked HTTP handler, as 7.2):

| # | Test method (short) | Rule or wire | Destination / reason |
|---|---|---|---|
| 1 | A_Feature_waits_on_what_its_named_one_way_link_points_at | rule plus read route | **stays** (the class's end-to-end row: refresh, store, Dependencies route, source `TrackerLink`) |
| 2 | The_other_ends_phrase_reads_the_other_direction | rule | move (pin exists): `JiraNamedDependencyLinkTest.A_phrase_typed_exactly_as_one_end_reads_picks_that_end_even_where_the_type_shares_its_name` |
| 3 | Naming_blocks_on_the_stock_Blocks_type_reads_the_end_that_phrase_names | rule | move (pin exists): same |
| 4 | A_renamed_phrase_typed_in_any_case_is_read_like_the_stock_one | rule | move (add row): same class, case-insensitive match on a renamed phrase |
| 5 | A_real_field_carrying_the_name_of_a_link_phrase_is_read_as_the_field | rule | move (pin exists): `A_Dependency_Field_naming_a_field_reads_what_was_typed_into_it` |
| 6 | A_link_types_own_name_reads_nothing_and_Refresh_History_names_both_phrases | rule | move (pin exists): `The_types_name_reads_nothing_and_says_it_points_both_ways` |
| 7 | A_phrase_both_ends_share_reads_nothing_and_says_so | rule | move (pin exists, confirm): same TestCase class; add `relates to` as a typed value if missing |
| 8 | Naming_a_link_replaces_is_blocked_by_for_that_Portfolio | rule | move (add row): same class |
| 9 | A_refused_link_type_list_stops_the_refresh_and_leaves_the_stored_dependencies_as_they_were | updater plus store | **stays**: connector half pinned by `A_link_type_list_that_cannot_be_read_stops_the_refresh_and_says_how_Jira_answered`; "stored dependencies stay" needs the updater and the store |
| 10 | An_unreadable_link_type_list_stops_the_refresh_and_leaves_the_stored_dependencies_as_they_were | updater plus store | **stays**: same as row 9 |
| 11 | A_Portfolio_naming_an_ordinary_field_reads_its_keys_exactly_as_before | rule | move (pin exists): same as row 5 |
| 12 | A_Portfolio_naming_nothing_still_reads_is_blocked_by | rule | move (add row): same class |
| 13 | Naming_a_link_costs_the_Jira_instance_no_extra_request | rule (a request count) | move (add row): counting handler in `JiraNamedDependencyLinkTest` |

Count: move 10 + stays 3 = 13. Keeps its host (3 rows). Saving: at most 14.8 x 10/13 = 11.4 s.

**Slice03AzureDevOpsRelationTypeAcceptedScenarios (24 rows) [connector]** (24 rows = 16 `[Test]` + 8 TestCase rows). Most rows call the validate route only (no refresh), so they are cheaper than a refresh row; that is read from the source, not measured.

| # | Test method (short) | Rule or wire | Destination / reason |
|---|---|---|---|
| 1 | A_relation_type_reference_with_its_end_validates_where_a_field_name_goes | wire plus rule | **stays** (the validate route: status, `isValid`, message member). The one validate wire row |
| 2 | A_relation_type_reference_matches_in_any_case | rule | move (pin exists): `ValidateConnection_WhenAReferenceNamesARelationType_AcceptsIt(reference)` |
| 3 | A_field_that_exists_validates_without_asking_for_the_relation_types | rule | move (pin exists): `ValidateConnection_WhenEveryReferenceIsAField_DoesNotListTheRelationTypes` |
| 4 | The_shared_connector_keeps_no_relation_type_listing_between_calls | rule (lifetime of a cache) | move (add row): the connector, two calls, two listings |
| 5 | A_typo_still_fails_and_lists_the_relation_types_the_organisation_defines | rule | move (pin exists): `ValidateConnection_WhenReferencesNameNothing_ListsThemAndTheRelationTypesThatDoExist` |
| 6-7 | A_link_to_something_other_than_a_work_item... / The_relation_types_offered_on_a_typo_leave_out_links_to_anything_but_a_work_item | rule | move (pin exists, confirm): same row; add a non-work-item type to its double if missing |
| 8-9 | An_organisation_that_listed_no_relation_types... (2 TestCase) | rule | move (pin exists): `ValidateConnection_WhenTheOrganisationListsNoRelationTypes_SaysSo` |
| 10 | An_organisation_that_will_not_list_its_relation_types_fails_validation_saying_so | rule | move (pin exists): `ValidateConnection_WhenTheRelationTypesAreRefused_SaysTheReferenceCouldNotBeChecked` |
| 11-13 | A_relation_type_listing_that_failed_on_the_way... (3 TestCase) | rule | move (pin exists): `..._WhenListingTheRelationTypesTimesOut_ReadsItAsUnreadable` and `..._FailsUnexpectedly_DoesNotPassItOffAsARefusal` |
| 14 | Validate_asks_the_organisation_for_its_relation_types_at_most_once | rule | move (add row): counting client |
| 15 | A_refresh_naming_only_fields_asks_for_no_relation_types_even_with_a_stale_Additional_Field... | rule | move (pin exists): `GetFeaturesForProject_WhenOnlyAnUnusedDefinitionNamesNoField_DoesNotListTheRelationTypes` |
| 16 | A_refresh_does_not_report_a_relation_type_reference_as_a_missing_field | rule | move (pin exists): `GetFeaturesForProject_WhenSettingsThatReadAFieldNameARelationType_SaysSoForEachAndListsTheTypesOnce` |
| 17 | A_relation_type_named_as_Parent_Override_Field_keeps_the_native_parent_and_says_why | rule | move (pin exists): `..._WhenTheParentOverrideFieldNamesARelationTypeWithoutItsEnd_SaysItReadsNoParentFromIt` |
| 18 | A_field_named_exactly_like_a_relation_type_end_is_read_as_the_field | rule | move (add row): same class |
| 19-21 | A_relation_type_named_as_a_Teams_Parent_Override_Field... (x2) and A_Teams_parent_source_naming_nothing_the_organisation_defines_reads_no_parent | rule (the Team path of the connector) | move (add row): `AzureDevOpsNamedRelationEndTest` has only `GetFeaturesForProject` rows, so the Team path (`GetWorkItemsForTeam`) needs new rows; confirm the method in the step |
| 22-24 | A_relation_type_named_in_another_setting_reads_as_nothing_named_and_says_why (3 TestCase: Size, Owner, Estimation) | rule | move (pin exists): `..._WhenSettingsThatReadAFieldNameARelationType_SaysSoForEachAndListsTheTypesOnce` |

Count: move 23 + stays 1 = 24. Keeps its host (1 row). Saving: at most 14.8 x 23/24 = 14.2 s (the class is an upper bound; many rows are validate-only and cheap).

**Slice05aTeamsParentOverrideDefaultScenarios (18 rows)** (ConnectionDefaults):

| # | Test method (short) | Rule or wire | Destination / reason |
|---|---|---|---|
| 1 | One_save_on_the_connection_and_every_Team_that_named_nothing_reads_its_parents_from_the_default | wiring | **stays** (skeleton: save, follower update reads the default, settings page, count, log) |
| 2 | A_Team_with_its_own_Parent_Override_Field_is_unaffected_by_the_default | rule | move (pin exists): `EffectiveSettingTest.A_Team_reads_its_own_Parent_Override_Field_or_the_connections_default_by_its_source` (Own, field, default) |
| 3 | A_Team_that_chose_None_keeps_the_trackers_own_parent_whatever_the_default_says | rule | move (pin exists): same test (Own, null, default); follower count 2: `WorkTrackingSystemConnectionUsageTest.FollowersByConnection_CountsOnlyTheOwnersThatFollow...` |
| 4 | A_new_Team_follows_the_default_from_the_start | rule | move (pin exists, confirm): `IncomingSettingTest` / `TeamExtensionsTest.A_new_Team_copied_from_a_follower_...`; add a "new Team follows" row to `EffectiveSettingTest` if the break does not flip |
| 5 | A_Team_admin_without_connection_rights_sees_which_field_the_Team_follows | RBAC and wire | **stays** |
| 6 | Someone_without_connection_rights_cannot_save_the_connection | RBAC | **stays** (403) |
| 7-8 | Jira_and_Azure_DevOps_offer_every_default (2 TestCase) | rule (what a tracker offers) | move (pin exists): `SupportedConnectionDefaultsTest.A_tracker_honours_exactly_the_defaults_it_offers` |
| 9 | A_CSV_connection_offers_only_Feature_Owner_and_Estimation | rule | move (pin exists): same |
| 10-11 | Linear_and_ServiceNow_offer_no_default (2 TestCase) | rule | move (pin exists): same |
| 12 | A_default_the_tracker_does_not_honour_is_refused_and_nothing_is_saved | wire plus rule | **stays** (400 and code `connection-default-not-supported`, nothing saved, through the controller); the rule is `SupportedConnectionDefaultsTest`. The JSON names of the "offered" list (`dependencyFieldForPortfolios`, ...) are wire: check that `ConnectionDefaultsDtoTest` pins them, else add one row to the wire contract rather than keep rows 7 to 11 |
| 13 | A_save_that_changes_no_default_writes_no_log_entry | rule | move (pin exists): `Commit_TheTeamsDefaultUnchanged_LogsNothing`, `Commit_OnlyTheNameChanged_PublishesNothing` |
| 14 | A_default_nobody_follows_still_writes_an_entry_saying_none_follow | rule | move (pin exists): `Commit_TheTeamsDefaultSetButEveryTeamOwnsItsField_LogsOneEntrySayingNoTeamFollowsIt` |
| 15 | A_save_without_the_defaults_keeps_every_default | rule | move (pin exists, confirm): `ConnectionDefaultsDtoTest.An_Estimation_default_left_out_of_the_save_is_not_sent`; add the Parent-default variant if missing |
| 16 | Clearing_a_default_stops_the_followers_reading_it_and_is_logged | rule | move (pin exists): `Commit_TheTeamsDefaultCleared_LogsOneEntryNamingTheFieldItWas` plus `EffectiveSettingTest` (follower, no default) |
| 17 | The_admin_whose_copy_is_stale_is_refused_and_the_first_default_stands | concurrency token | **stays** (real store, as 7.1 row 12); unit side: `Commit_TheCopyIsStale_...` |
| 18 | A_field_added_in_the_same_save_can_be_the_default | rule | move (pin exists): `Commit_TheDefaultNamesAFieldAddedInThisSave_PointsTheDefaultAtTheStoredField` |

Count: move 13 + stays 5 = 18. Keeps its host (5 rows). Saving: at most 14.8 x 13/18 = 10.7 s.

**Slice08PortfoliosParentOverrideDefaultScenarios (9 rows)**:

| # | Test method (short) | Rule or wire | Destination / reason |
|---|---|---|---|
| 1 | A_Portfolio_that_names_nothing_hangs_its_Features_under_what_the_Portfolios_default_names | wiring (Portfolio side of the default) | **stays** (skeleton: the Portfolio follower's update reads the Portfolios default, 6-refresh Given) |
| 2 | The_Teams_default_never_reaches_a_Portfolio | rule | move (pin exists): `EffectiveSettingTest.A_Portfolio_has_no_Teams_default_to_follow`; the log half (no Portfolio named) needs one row on `ConnectionSaveServiceTest` (add row) |
| 3 | The_Portfolios_default_never_reaches_a_Team | rule | move (pin exists): `EffectiveSettingTest.A_Team_has_no_Portfolios_default_to_follow` |
| 4 | A_Portfolio_with_its_own_field_is_unaffected | rule | move (pin exists): `EffectiveSettingTest.A_Portfolio_reads_its_own_Parent_Override_Field_or_the_connections_Portfolio_default_by_its_source` |
| 5-6 | A_save_that_leaves_one_default_out_keeps_it (2 TestCase) | rule | move (add row): `ConnectionDefaultsDtoTest`, an older page's save without the Portfolio and the Teams parent default |
| 7 | A_field_added_in_the_same_save_can_be_the_Portfolios_default | rule | move (pin exists): `Commit_PointingThePortfoliosDefaultAtAFieldAddedInThisSaveFails_...` and the Teams twin |
| 8-9 | A_default_sent_as_empty_is_cleared (2 TestCase) | rule | move (pin exists): `ConnectionDefaultsDtoTest.An_explicit_null_list_clears_the_default_rather_than_saying_nothing`; the log half `Commit_TheTeamsDefaultCleared_...` (add a Portfolio-grain TestCase) |

Count: move 8 + stays 1 = 9. Keeps its host (1 row). Saving: at most 14.8 x 8/9 = 13.2 s.

**Slice09PortfoliosDependencyFieldDefaultScenarios (3 rows)**, **Slice10PortfoliosSizeEstimateFieldDefaultScenarios (5 rows)**, **Slice11PortfoliosFeatureOwnerFieldDefaultScenarios (6 rows)**: three copies of one pattern, one per Portfolio setting.

| Class | # | Test method (short) | Rule or wire | Destination / reason |
|---|---|---|---|---|
| 09 | 1 | A_following_Portfolio_reads_its_dependencies_from_the_default_and_an_owner_is_left_alone | rule (what the connector is asked to read) | move (pin exists): `EffectiveSettingTest.A_Portfolio_reads_its_own_Dependency_Field_or_the_connections_default_by_its_source`, `A_new_Portfolio_follows_the_connections_Dependency_Field_default`; the updates queued: Portfolio grain on the publish matrix (add TestCase). The break must be on the effective getter the updater hands the connector |
| 09 | 2 | The_settings_page_names_the_Dependency_Field_default_not_the_Parent_Override_one | rule plus DTO | move (add row): `SettingsOwnerDtoRoundTripTest` with two different defaults set |
| 09 | 3 | Ignore_Dependencies_is_never_taken_from_the_connection | rule | move (pin exists, partial): `SupportedConnectionDefaultsTest` and `ConnectionDefaultsDtoTest.A_saved_Dependency_Field_default_binds_to_the_default_this_build_holds`; add a save-back row on `PortfolioExtensionsTest` for `ignoreDependencies` |
| 10 | 1 | A_following_Portfolio_reads_its_sizes_from_the_default_and_an_owner_is_left_alone | rule | move (pin exists): `EffectiveSettingTest...Size_Estimate_Field...` |
| 10 | 2 | The_settings_page_names_the_Size_Estimate_Field_default_not_the_Dependency_one | rule plus DTO | move (add row): as 09 row 2 |
| 10 | 3 | A_following_Feature_is_sized_at_what_the_default_field_holds | rule over a fake tracker | **stays**: the fake tracker computes the size from the field it is asked for, so the host row proves updater, fake and store together; the owner of "an empty size takes the Portfolio's default size" was not located in this screen. If `ctx_search` finds it in a Models or service class, the row moves |
| 10 | 4 | A_following_Feature_with_no_size_in_the_default_field_takes_the_Portfolios_default_size | rule | **stays**: same reason as row 3 |
| 10 | 5 | A_Portfolio_that_owns_its_size_field_starts_no_update_when_the_default_changes | rule | move (add row): Portfolio grain on the publish matrix |
| 11 | 1 | A_following_Portfolio_reads_its_Feature_owners_from_the_default_and_an_owner_is_left_alone | rule | move (pin exists): `EffectiveSettingTest...Feature_Owner_Field...` |
| 11 | 2 | The_settings_page_names_the_Feature_Owner_Field_default_not_the_Size_Estimate_one | rule plus DTO | move (add row): as 09 row 2 |
| 11 | 3 | A_following_Feature_is_owned_by_the_Team_the_default_field_names | rule over a fake tracker | **stays**: as 10 row 3 |
| 11 | 4 | A_following_Feature_on_a_CSV_connection_is_owned_by_the_Team_the_default_column_names | rule over a fake tracker | **stays**: as 10 row 3, and it adds a CSV connection |
| 11 | 5 | A_following_Feature_with_no_owner_in_the_default_field_is_shared_by_every_Team | rule over a fake tracker | **stays**: as 10 row 3 |
| 11 | 6 | The_Owning_Team_is_never_taken_from_the_connection | rule | move (pin exists): `SupportedConnectionDefaultsTest`, `ConnectionDefaultsDtoTest.A_save_following_the_Feature_Owner_Field_default_keeps_the_Portfolios_own_Owning_Team` |

Counts: Slice09 move 3 + stays 0 = 3 (**the class leaves the host entirely**, if the JSON names of `dependencyFieldForPortfolios` and the settings page names are pinned by an existing DTO or wire row; check in the step). Slice10 move 3 + stays 2 = 5 (keeps its host). Slice11 move 3 + stays 3 = 6 (keeps its host). Savings, bounds only: Slice09 at most 14.8 s; Slice10 at most 14.8 x 3/5 = 8.9 s; Slice11 at most 14.8 x 3/6 = 7.4 s. They cannot all reach their bound: with Slice01, 03, 05a, 08 and ClockIsolation they must fit in 78.2 s.

**ClockIsolationScenarios (2 rows)**:

| # | Test method (short) | Rule or wire | Destination / reason |
|---|---|---|---|
| 1 | A_scenario_that_lets_a_minute_pass_begins_at_the_instant_every_scenario_begins_at | fixture self-test: the base class resets the host clock in `SetUp` | **stays** while `NamedDependencyLinkAcceptanceTest` has a user (Slice01 to 04 keep rows) |
| 2 | Another_scenario_that_lets_a_minute_pass_begins_at_the_same_instant | same | **stays** |

Count: stays 2 = 2. Keeps its host. Nothing leaves. Time unmeasured (at most 14.8 s; two rows with no refresh, so most likely its host build and one set-up each: not measured).

### 7.7 Counts for the family

| Class | Rows | Move | Rewrite | Stays | Host after? |
|---|---|---|---|---|---|
| Slice05b | 32 | 28 | 0 | 4 | yes (4) |
| Slice02 | 7 | 6 | 0 | 1 | yes (1) |
| Slice04 | 19 | 17 | 0 | 2 | yes (2) |
| Slice12 | 20 | 18 | 0 | 2 | yes (2) |
| Slice06 | 5 | 3 | 0 | 2 | yes (2) |
| Slice01 | 13 | 10 | 0 | 3 | yes (3) |
| Slice03 | 24 | 23 | 0 | 1 | yes (1) |
| Slice05a | 18 | 13 | 0 | 5 | yes (5) |
| Slice08 | 9 | 8 | 0 | 1 | yes (1) |
| Slice09 | 3 | 3 | 0 | 0 | **no** (conditional on one JSON-name check) |
| Slice10 | 5 | 3 | 0 | 2 | yes (2) |
| Slice11 | 6 | 3 | 0 | 3 | yes (3) |
| ClockIsolation | 2 | 0 | 0 | 2 | yes (2) |
| **Total** | **163** | **135** | **0** | **28** | 12 of 13 |

Of the 135 moves, the pin is found **by name** for most; the rows marked "add row" (about 30) need a new row on an existing unit class with the same Given on that class's doubles. The count of "pin exists" versus "add row" is read by eye from the tables, not computed. The stays fall in four groups: the end-to-end rows (one per flow), the real-store concurrency rows, the wire rows (RBAC, status and code of a refusal, the validate route), and the rows whose owner I did not find (fake-tracker feature size and owner, Slice06 row 3).

### 7.8 Suite 2 slices (at most 5 nwdes slices, largest projected saving first)

The saving is the class seconds x the share of rows leaving, as in step 2 of the delta. **It is a projection, not a measurement**: the measured saving comes from the before/after runs of step 3. A part with "at most" belongs to a class with no measured seconds.

| Order | Slice | Classes | Rows leaving | Projected saving | Unit homes |
|---|---|---|---|---|---|
| 1 | Save pipeline: field removal, refusals, older payloads, defaults offered | Slice05b, Slice05a | 28 + 13 = 41 | 17.2 s (Slice05b) + at most 10.7 s (Slice05a) | `ConnectionSaveServiceTest`, `IncomingSettingTest`, `OwnerSettingRefusalsTest`, `EffectiveSettingTest`, `SupportedConnectionDefaultsTest`, `ConnectionDefaultsDtoTest` |
| 2 | Jira named link and what Refresh History says | Slice02, Slice01 | 6 + 10 = 16 | 16.4 s (Slice02) + at most 11.4 s (Slice01) | `JiraNamedDependencyLinkTest` **[connector]**, `RefreshLogServiceTest`, the Refresh History read side |
| 3 | Azure DevOps named relation end and relation types | Slice04, Slice03 | 17 + 23 = 40 | 14.8 s (Slice04) + at most 14.2 s (Slice03) | `AzureDevOpsNamedRelationEndTest` **[connector]** |
| 4 | Estimation default and the other Portfolio defaults | Slice12, Slice09, Slice10, Slice11 | 18 + 3 + 3 + 3 = 27 | 14.2 s (Slice12) + at most 14.8 + 8.9 + 7.4 s (together bounded by the 78.2 s the unmeasured classes hold) | `EstimationBundleTest`, `EffectiveSettingTest`, `OwnerSettingRefusalsTest`, `SettingsOwnerDtoRoundTripTest`, `ConnectionSaveServiceTest` |
| 5 | Default change updates the followers (Team and Portfolio grain) | Slice06, Slice08 | 3 + 8 = 11 | 9.1 s (Slice06) + at most 13.2 s (Slice08) | `ConnectionSaveServiceTest` (publish matrix, logs), `EffectiveSettingTest` |

Measured part of the projection: 17.2 + 16.4 + 14.8 + 14.2 + 9.1 = **71.7 s** of the 86.2 s that the five measured classes take on CI. The unmeasured classes add at most 78.2 x 63/80 = 61.6 s if every row cost the same (63 of their 80 rows leave); that figure is arithmetic on an assumption, not a measurement, and it assumes each class's seconds are spread evenly over its rows, which the measured classes show is false (0.61 to 3.0 s a row). Read the total as "at most about 133 s of the 164.4 s", and expect less; the host builds of the 12 classes that keep a row are not subtracted.

Order and dependency. Slice 1 and Slice 4 and Slice 5 all add rows to `ConnectionSaveServiceTest`, `EffectiveSettingTest` and `IncomingSettingTest`: run them one after another (1, then 4, then 5), never in parallel, as S3 and S4 of suite 1 were. Slices 2 and 3 touch only their own connector's unit class and `RefreshLogServiceTest`, so they do not conflict with the others. If the crafter wants the unit-class edits ordered by value, the order in the table (largest measured saving first) already does it.

**Classes I would leave out of the five slices**, and why:

- **ClockIsolationScenarios**: 2 rows that guard the base class's clock reset; nothing is rule-shaped. It stays as long as Slice01 to 04 keep a host row on that base. Nothing to move, so no slice.
- **The 28 stays**: see the four groups in 7.7. They are not backlog work. The wire rows (status and code of a refusal, RBAC, the validate route) and the real-store concurrency rows would fit better together in one `Contracts` class on `WireContractTest` with one host than in seven fixtures with seven hosts; that is a follow-up after the slices land, because the backend half measures that a split that adds host fixtures can be slower (27.0 s against 24.4 s), and the same holds for a merge that is not measured. It is not part of the savings above.
- **Slice06 row 3 and the feature-size and owner rows of Slice10 and Slice11** (7 rows): kept only because the owner of the rule was not found in this screen. If the crafter finds it while working a slice, the row moves in that slice, and the saving only rises.

### 7.9 What surprised me

- The family is not slow in a fill: no row of 163 saves a long history. The cost is per-row set-up (`EnsureCreated`, every seeder, the Payments department) and refreshes that wait for the queue. So the 5 measured classes show 0.61 to 3.0 s a row, and the slowest per row (Slice02, Slice06) are the ones with 4 to 6 or more refreshes in the Given.
- Slice02's 19.1 s comes from only 2 of its 7 rows (4 and more refreshes each), and both have a direct unit pin on `RefreshLogServiceTest`.
- Rows that look like host tests are mostly rules the save service, `EffectiveSetting` or `IncomingSetting` already prove: `IncomingSettingTest` has an 18-row matrix that names every combination Slice05b rows 16 to 29 re-check.
- `AzureDevOpsNamedRelationEndTest` has only `GetFeaturesForProject` and `ValidateConnection` rows, so the Team-path rows of Slice03 (19 to 21) are not just moves; they need new rows.
- There is no live tracker anywhere in the family. All trackers are doubles; the connector-specific rows move to the connector's own unit class, which already uses the same double.
- Slice09 can leave the host entirely, and no other class can; every other class keeps at least one row and so its own host build.
- `TestHostUseGuardTest.HostUsersStillToMove` names these classes (lines 41 to 48 read: ClockIsolation, Slice01 to 06). Only Slice09 can come off that list in suite 2; the other 12 stay on it because each keeps at least one host row, which this screen does not propose to remove.
