# S1 id table: Slice07WhereTheLimitsActuallyMovedTest

Host ids are in `Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime.Slice07WhereTheLimitsActuallyMovedTest`.
Unit ids are in `Lighthouse.Backend.Tests.Modules.FlowMetrics.Application`.
Each break is applied to the production line in the working tree, the named unit row is run, and the line is put back (`s1-break-evidence.txt`, run by `s1_breaks.py`).

| # | Old host id | Now proven by | Break (production line) | Red row |
|---|---|---|---|---|
| 1 | A_team_fills_in_every_behaviour_it_reports_and_not_one_fewer | ProcessBehaviorSnapshotWriterTest.ATeam_ReportsExactlyThroughputWorkItemAgeWipCycleTimeAndArrivals | B1: the Arrivals line removed from FamiliesFor(Team) | the same row |
| 2 | A_portfolio_fills_in_every_behaviour_it_reports_and_not_one_fewer | ProcessBehaviorSnapshotWriterTest.APortfolio_ReportsExactlyThroughputWorkItemAgeWipCycleTimeArrivalsAndFeatureSize | B1b: the FeatureSize line removed from FamiliesFor(Portfolio) | the same row |
| 3 | How_big_deliveries_are_getting_is_filled_in_for_a_portfolio_and_never_for_a_team | ProcessBehaviorSnapshotWriterTest.HowBigDeliveriesAreGetting_IsAFamilyOfAPortfolioAndNeverOfATeam | B1b | the same row |
| 4 | A_stretch_pinned_further_back_than_the_team_keeps_work_reports_no_limits | RetentionEdgeTest.LimitsDrawnFromAPinnedStretchThatBeginsBeforeTheEdge_ComeBackNotReady_EvenForAWindowInsideWhatIsKept (existing) | B5: the guard ignores the pinned stretch start | the same row |
| 5 | A_stretch_in_which_the_team_finished_nothing_reports_no_band_rather_than_a_flat_zero_one | ProcessBehaviorSnapshotWriterTest.ADayWhoseReadyLimitsAreAFlatZeroBand_GetsNoRow and ...ADayWhoseLimitsAreNotReady_GetsNoRow_EvenWhenTheChartCarriesABand | B2: the zero-band gate removed; B2b: the not-ready gate removed | the flat-zero row; the not-ready row |
| 6 | A_team_that_fixed_the_stretch_its_limits_come_from_reads_steady_limits_not_an_empty_chart | RetentionEdgeTest.ATeamsPinnedStretchOutOfReachToday_IsReadWhenTheEdgeIsDrawnFromItsLastRefresh (reads as of the last refresh), with RetentionEdgeTest.APinnedStretchInReachOnTheLastRefresh_IsOutOfReachOfAnEdgeDrawnOnALaterDay (the contrast the host seeded); steady half: TeamMetricsServiceTests.GetThroughputProcessBehaviourChart_PinnedBaseline_GivesTheSameLimitsForEveryDisplayedPeriodHoweverThePeriodsThroughputMoves | B3: the edge drawn 25 days later than the last refresh; B6: the pinned baseline replaced by the displayed period in ResolveBaseline | the team row; the steady row |
| 7 | A_portfolio_that_fixed_the_stretch_its_limits_come_from_reads_steady_limits_too | RetentionEdgeTest.APortfoliosPinnedStretchOutOfReachToday_IsReadWhenTheEdgeIsDrawnFromItsLastRefresh; steady half: the same TeamMetricsServiceTests row (the baseline is resolved in BaseMetricsService, shared by team and portfolio) | B3; B6 | the portfolio row; the steady row |
| 8 | A_team_that_did_not_fix_the_stretch_reads_limits_drawn_from_each_days_own_history | ProcessBehaviorSnapshotWriterTest.ADayWithoutLimits_IsReadOverTheSpanEndingOnThatDay_AsOfThatDay (existing) | B4: FillDayIfAbsent passes asOf null; B4b: its window ends today | the same row |
| 9 | Limits_worked_out_afterwards_read_the_same_as_the_day_they_were_watched | ProcessBehaviorSnapshotWriterTest.ADayFilledInAfterwards_ReadsTheSameAsWhenItWasWatched | B4; B4b | the same row |
| 10 | Limits_stop_where_the_team_stopped_being_watched_and_a_second_look_changes_nothing | stays (the one limits-route row) | - | - |

Not claimed by the unit rows: that the metrics service reads "asOf null" as today (a different class).
