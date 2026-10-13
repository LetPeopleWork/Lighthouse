namespace Lighthouse.Backend.Tests.API.Integration.Reports
{
    /// <summary>
    /// The {Cycle Time} panel counts Now's finished Work Items that lie beyond the process limits of Then - above
    /// the upper limit and below the lower one. The limits are worked out from Then's own finished Work Items, in
    /// the order they finished, and frozen unrounded at creation; neither Now's data nor the Team's own process
    /// behaviour baseline ever moves them. A lower limit at 0 cannot be fallen below, so that count is "—"; so
    /// are both counts when Then has fewer than two points to work limits out from.
    ///
    /// Gravity's Then (2-31 Aug 2026) finished eight Work Items alternating 10 and 14 days: average 12, average
    /// moving range 4, so the limits are 12 ± 2.66 × 4 = 22.64 and 1.36.
    ///
    /// Driving ports: POST/GET /teams/{id}/reports.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5878-baseline")]
    [Category("slice-03")]
    public class Slice03CycleTimeBeyondThenLimitsScenarios : ReportsAcceptanceTest
    {
        private static readonly DateOnly ThenEnds = new(2026, 8, 31);

        private static readonly DateOnly NowStarts = new(2026, 9, 3);

        // @driving_port @real-io @us-03 @slice-03 @kpi-OUT-5878-K2-reports-reopened @contract-shape:pure-function
        [Test]
        [Ignore(PendingSlice03)]
        public async Task Unusually_slow_Work_Items_are_counted_above_the_limits_Then_set()
        {
            var gravity = GivenGravitysThenAlternatedTenAndFourteenDays();
            var reportId = await GivenTheAdminCreated(gravity, ThirtyDaysToTheEndOfAugust());

            TheTeamFinished(gravity,
                new Finished(NowStarts.AddDays(7), 41),
                new Finished(NowStarts.AddDays(9), 38),
                new Finished(NowStarts.AddDays(12), 12),
                new Finished(NowStarts.AddDays(17), 9));

            var report = await TheReport(gravity, reportId);
            using (Assert.EnterMultipleScope())
            {
                ThenTheCountIs(AboveThenLimitsIn(report, CycleTime), 2);
                ThenTheCountIs(BelowThenLimitsIn(report, CycleTime), 0);
            }
        }

        // @driving_port @real-io @us-03 @slice-03 @boundary @contract-shape:pure-function
        // Rounded, the limits would be 23 and 1, and neither a 23-day nor a 1-day Work Item would count.
        [Test]
        [Ignore(PendingSlice03)]
        public async Task The_limits_Then_set_are_frozen_unrounded()
        {
            var gravity = GivenGravitysThenAlternatedTenAndFourteenDays();
            var reportId = await GivenTheAdminCreated(gravity, ThirtyDaysToTheEndOfAugust());

            TheTeamFinished(gravity,
                new Finished(NowStarts.AddDays(7), 23),
                new Finished(NowStarts.AddDays(9), 1),
                new Finished(NowStarts.AddDays(12), 12));

            var report = await TheReport(gravity, reportId);
            using (Assert.EnterMultipleScope())
            {
                ThenTheCountIs(AboveThenLimitsIn(report, CycleTime), 1);
                ThenTheCountIs(BelowThenLimitsIn(report, CycleTime), 1);
            }
        }

        // @driving_port @real-io @us-03 @slice-03 @error @contract-shape:pure-function
        // Orion's Then (2, 30, 2, 30 days) puts the lower limit below zero, where it is clamped to 0.
        [Test]
        [Ignore(PendingSlice03)]
        public async Task A_lower_limit_at_zero_shows_a_dash_below_and_still_counts_above()
        {
            var orion = ATeam("Team Orion");
            TheTeamFinished(orion,
                new Finished(ThenEnds.AddDays(-25), 2),
                new Finished(ThenEnds.AddDays(-20), 30),
                new Finished(ThenEnds.AddDays(-15), 2),
                new Finished(ThenEnds.AddDays(-10), 30));
            var reportId = await GivenTheAdminCreated(orion, ThirtyDaysToTheEndOfAugust());

            TheTeamFinished(orion, new Finished(NowStarts.AddDays(10), 95), new Finished(NowStarts.AddDays(11), 1));

            var report = await TheReport(orion, reportId);
            using (Assert.EnterMultipleScope())
            {
                ThenTheCountIs(AboveThenLimitsIn(report, CycleTime), 1);
                ThenItIsADashFor(BelowThenLimitsIn(report, CycleTime), Reason.LowerLimitZero);
            }
        }

        // @driving_port @real-io @us-03 @slice-03 @error @contract-shape:pure-function
        [TestCase(0)]
        [TestCase(1)]
        [Ignore(PendingSlice03)]
        public async Task Fewer_than_two_Then_points_show_a_dash_on_both_sides(int finishedInThen)
        {
            var pulsar = ATeam("Team Pulsar");
            TheTeamFinished(pulsar, [.. Enumerable.Range(0, finishedInThen).Select(i => new Finished(ThenEnds.AddDays(-i - 3), 11))]);
            var reportId = await GivenTheAdminCreated(pulsar, ThirtyDaysToTheEndOfAugust());

            TheTeamFinished(pulsar, new Finished(NowStarts.AddDays(4), 60));

            var report = await TheReport(pulsar, reportId);
            using (Assert.EnterMultipleScope())
            {
                ThenItIsADashFor(AboveThenLimitsIn(report, CycleTime), Reason.TooFewPoints);
                ThenItIsADashFor(BelowThenLimitsIn(report, CycleTime), Reason.TooFewPoints);
            }
        }

        // @driving_port @real-io @us-03 @slice-03 @boundary @contract-shape:unbounded-preservation
        [Test]
        [Ignore(PendingSlice03)]
        public async Task The_Teams_process_behaviour_baseline_does_not_move_the_counts()
        {
            var gravity = GivenGravitysThenAlternatedTenAndFourteenDays();
            var reportId = await GivenTheAdminCreated(gravity, ThirtyDaysToTheEndOfAugust());
            TheTeamFinished(gravity, new Finished(NowStarts.AddDays(7), 41), new Finished(NowStarts.AddDays(9), 38));
            var before = AboveThenLimitsIn(await TheReport(gravity, reportId), CycleTime);

            TheTeamsProcessBehaviourBaselineBecomes(gravity, NowStarts, NowStarts.AddDays(20));

            var after = AboveThenLimitsIn(await TheReport(gravity, reportId), CycleTime);
            using (Assert.EnterMultipleScope())
            {
                ThenTheCountIs(before, 2);
                ThenTheCountIs(after, 2);
            }
        }

        // @driving_port @real-io @us-03 @slice-03 @boundary @contract-shape:unbounded-preservation
        // A report created before limits were frozen has nothing to count against, and must say so.
        [Test]
        [Ignore(PendingSlice03)]
        public async Task A_report_created_before_the_count_existed_has_no_limits_captured()
        {
            var gravity = GivenGravitysThenAlternatedTenAndFourteenDays();
            var reportId = await GivenTheAdminCreated(gravity, ThirtyDaysToTheEndOfAugust());
            TheStoredPayloadIsReshaped(reportId, payload => WithoutTheCaptureOf(payload, CycleTime, "limits"));

            var report = await TheReport(gravity, reportId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(LimitsAreCapturedIn(report, CycleTime), Is.False);
                Assert.That(ThenValueIn(report, CycleTime, P85).IsPresent, Is.True, "the rest of the panel still reads");
            }
        }

        // --- Steps ---

        private static readonly int[] AlternatingCycleTimes = [10, 14, 10, 14, 10, 14, 10, 14];

        private static NewReport ThirtyDaysToTheEndOfAugust() => new()
        {
            ThenEnd = ThenEnds,
            ThenLengthDays = 30,
            NowLengthDays = 30,
        };

        private OwnerUnderTest GivenGravitysThenAlternatedTenAndFourteenDays()
        {
            var gravity = ATeam("Team Gravity");
            var firstFinished = new DateOnly(2026, 8, 3);

            TheTeamFinished(gravity, [.. AlternatingCycleTimes.Select((took, i) => new Finished(firstFinished.AddDays(2 * i), took))]);
            return gravity;
        }
    }
}
