# S1 id table: Slice07WhereTheLimitsActuallyMovedTest

Host ids are in `Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime.Slice07WhereTheLimitsActuallyMovedTest`.
Unit ids are in `Lighthouse.Backend.Tests.Modules.FlowMetrics.Application`.
Each break is applied to the production line in the working tree, the named unit row is run, and the line is put back (`s1-break-evidence.txt`, run by `s1_breaks.py`).

| # | Old host id | Now proven by | Break (production line) | Red row |
|---|---|---|---|---|
| 1 | A_team_fills_in_every_behaviour_it_reports_and_not_one_fewer | ProcessBehaviorSnapshotWriterTest.ATeam_ReportsExactlyThroughputWorkItemAgeWipCycleTimeAndArrivals; and that the filler fills every family on each past day: OverTimeHistoryFillerTest.ADayWorkedOut_IsWorkedOutForEveryOneOfTheOwnersLimitFamilies | B1: the Arrivals line removed from FamiliesFor(Team); B7: the fill loop in OverTimeHistoryFiller cut to Take(1) | the same row; the filler row |
| 2 | A_portfolio_fills_in_every_behaviour_it_reports_and_not_one_fewer | ProcessBehaviorSnapshotWriterTest.APortfolio_ReportsExactlyThroughputWorkItemAgeWipCycleTimeArrivalsAndFeatureSize; the filler row of row 1 (the loop runs over any owner's families) | B1b: the FeatureSize line removed from FamiliesFor(Portfolio); B7 | the same row; the filler row |
| 3 | How_big_deliveries_are_getting_is_filled_in_for_a_portfolio_and_never_for_a_team | ProcessBehaviorSnapshotWriterTest.HowBigDeliveriesAreGetting_IsAFamilyOfAPortfolioAndNeverOfATeam | B1b | the same row |
| 4 | A_stretch_pinned_further_back_than_the_team_keeps_work_reports_no_limits | RetentionEdgeTest.LimitsDrawnFromAPinnedStretchThatBeginsBeforeTheEdge_ComeBackNotReady_EvenForAWindowInsideWhatIsKept (existing) | B5: the guard ignores the pinned stretch start | the same row |
| 5 | A_stretch_in_which_the_team_finished_nothing_reports_no_band_rather_than_a_flat_zero_one | ProcessBehaviorSnapshotWriterTest.ADayWhoseReadyLimitsAreAFlatZeroBand_GetsNoRow and ...ADayWhoseLimitsAreNotReady_GetsNoRow_EvenWhenTheChartCarriesABand | B2: the zero-band gate removed; B2b: the not-ready gate removed | the flat-zero row; the not-ready row |
| 6 | A_team_that_fixed_the_stretch_its_limits_come_from_reads_steady_limits_not_an_empty_chart | RetentionEdgeTest.ATeamsPinnedStretchOutOfReachToday_IsReadWhenTheEdgeIsDrawnFromItsLastRefresh (reads as of the last refresh), with RetentionEdgeTest.APinnedStretchInReachOnTheLastRefresh_IsOutOfReachOfAnEdgeDrawnOnALaterDay (the contrast the host seeded); steady half: TeamMetricsServiceTests.GetThroughputProcessBehaviourChart_PinnedBaseline_GivesTheSameLimitsForEveryDisplayedPeriodHoweverThePeriodsThroughputMoves (two 15-day periods whose own throughput is 1 and 6 a day, same limits asserted for both; day-by-day steadiness follows from a baseline that does not read the day) | B3: the edge drawn 25 days later than the last refresh; B6: the pinned baseline replaced by the displayed period in ResolveBaseline | the team row; the steady row |
| 7 | A_portfolio_that_fixed_the_stretch_its_limits_come_from_reads_steady_limits_too | RetentionEdgeTest.APortfoliosPinnedStretchOutOfReachToday_IsReadWhenTheEdgeIsDrawnFromItsLastRefresh; steady half: PortfolioMetricsServiceTests.GetThroughputProcessBehaviourChart_PinnedBaseline_GivesTheSameLimitsForEveryDisplayedPeriodHoweverThePeriodsThroughputMoves (the same two periods over features closed per day) | B3; B6 (one line in the shared BaseMetricsService turns the team and the portfolio row red) | the portfolio row; the portfolio steady row |
| 8 | A_team_that_did_not_fix_the_stretch_reads_limits_drawn_from_each_days_own_history | ProcessBehaviorSnapshotWriterTest.ADayWithoutLimits_IsReadOverTheSpanEndingOnThatDay_AsOfThatDay (existing) | B4: FillDayIfAbsent passes asOf null; B4b: its window ends today | the same row |
| 9 | Limits_worked_out_afterwards_read_the_same_as_the_day_they_were_watched | ProcessBehaviorSnapshotWriterTest.ADayFilledInAfterwards_ReadsTheSameAsWhenItWasWatched | B4; B4b | the same row |
| 10 | Limits_stop_where_the_team_stopped_being_watched_and_a_second_look_changes_nothing | stays (the one limits-route row) | - | - |

Not claimed by the unit rows: that the metrics service reads "asOf null" as today (a different class).

## Touched paths (test files outside the host class that this slice changed)

| File | Why it changed | Pre-existing rows and the break that turns them red |
|---|---|---|
| `Modules/FlowMetrics/Application/ProcessBehaviorSnapshotWriterTest.cs` | new rows for the limit families, the gates and the refill rule; its writer set-up gained a second overload that takes a clock, and the old one calls it with the same instant (2026-09-22 09:00 UTC) it built before, so the four rows that were already there run on unchanged input | `ADayWithoutLimits_IsReadOverTheSpanEndingOnThatDay_AsOfThatDay` (B4, B4b), `ADayThatAlreadyCarriesLimits_KeepsWhatWasObserved` (B8), `ARowRefusedBecauseAnotherCopyStoredThatDayFirst_IsAbsorbed` (B10), `ARowRefusedWhileOnlyAnotherFamilyOfThatDayIsStored_IsThrownOn` (B9, B10); each is red in the current run of `s1-break-evidence.txt` |
| `Modules/FlowMetrics/Application/RetentionEdgeTest.cs` | added rows only, no pre-existing line removed | none changed; the rows have their own breaks above |
| `Modules/FlowMetrics/Application/TeamMetricsServiceTests.cs` and `PortfolioMetricsServiceTests.cs` | added rows only (the pinned-baseline rows); no pre-existing line removed | none changed; B6 turns the added rows red |
| `Modules/FlowMetrics/Adapters/Background/OverTimeHistoryFillerTest.cs` | added rows only; no pre-existing line removed | none changed; B7 turns the added every-family row red |
