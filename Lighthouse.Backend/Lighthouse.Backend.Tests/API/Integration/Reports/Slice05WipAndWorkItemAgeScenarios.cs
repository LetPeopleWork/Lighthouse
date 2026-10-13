namespace Lighthouse.Backend.Tests.API.Integration.Reports
{
    /// <summary>
    /// {WIP} is the average Work in Progress over the window with its range; {Work Item Age} is the total age of
    /// what is in progress and the average age per Work Item, each as the window's average and as the window's
    /// last day. A day with nothing in progress has no average age, so it is left out of the window's average,
    /// and with nothing in progress on any day the average age is "—". Now's days beyond Then's limits are
    /// counted for {WIP} and for the total {Work Item Age}.
    ///
    /// Then is 2-31 Aug 2026 and Now 3 Sep - 2 Oct 2026, both 30 days. A Work Item started on 1 Jul is 33 days
    /// old on 2 Aug, 62 on 31 Aug, 65 on 3 Sep and 94 on 2 Oct, counting both ends.
    ///
    /// Driving ports: GET /teams/{id}/reports/templates, POST/GET /teams/{id}/reports, and the Metrics tab's
    /// totalWorkItemAge for the parity check.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5878-baseline")]
    [Category("slice-05")]
    public class Slice05WipAndWorkItemAgeScenarios : ReportsAcceptanceTest
    {
        private static readonly DateOnly FirstOfJuly = new(2026, 7, 1);

        private static readonly DateOnly ThenEnds = new(2026, 8, 31);

        // @driving_port @real-io @us-05 @slice-05 @contract-shape:pure-function
        [Test]
        [Ignore(PendingSlice05)]
        public async Task A_Team_can_be_reported_on_all_four_metrics()
        {
            var gravity = ATeam("Team Gravity");

            TheCallerAdministers(gravity);
            var templates = await TheTypesOfReportFor(gravity);

            Assert.That(MetricKeysOfThenAndNowIn(templates), Is.EquivalentTo(AllFourMetrics));
        }

        // @driving_port @real-io @us-05 @slice-05 @contract-shape:pure-function
        // Two of Gravity's nine Work Items finish on 1 Sep, between Then and Now.
        [Test]
        [Ignore(PendingSlice05)]
        public async Task Lower_WIP_is_shown_as_the_average_and_its_range_with_the_days_below_Thens_limit()
        {
            var gravity = ATeam("Team Gravity");
            TheTeamWorksOn(gravity, 7, FirstOfJuly);
            TheTeamWorksOn(gravity, 2, FirstOfJuly, finishedOn: new DateOnly(2026, 9, 1));

            var reportId = await GivenTheAdminCreated(gravity, ThirtyDaysToTheEndOfAugust());

            var report = await TheReport(gravity, reportId);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ThenValueIn(report, Wip, Average), Is.EqualTo(Shown(9, 30)));
                Assert.That(ThenValueIn(report, Wip, Min).Value, Is.EqualTo(9));
                Assert.That(ThenValueIn(report, Wip, Max).Value, Is.EqualTo(9));
                Assert.That(NowValueIn(report, Wip, Average), Is.EqualTo(Shown(7, 30)));
                ThenTheCountIs(AboveThenLimitsIn(report, Wip), 0);
                ThenTheCountIs(BelowThenLimitsIn(report, Wip), 30);
            }
        }

        // @driving_port @real-io @us-05 @slice-05 @contract-shape:pure-function
        [Test]
        [Ignore(PendingSlice05)]
        public async Task Days_with_WIP_above_Thens_upper_limit_are_counted()
        {
            var gravity = ATeam("Team Gravity");
            TheTeamWorksOn(gravity, 9, FirstOfJuly);
            TheTeamWorksOn(gravity, 4, new DateOnly(2026, 9, 30));

            var reportId = await GivenTheAdminCreated(gravity, ThirtyDaysToTheEndOfAugust());

            var report = await TheReport(gravity, reportId);
            using (Assert.EnterMultipleScope())
            {
                ThenTheCountIs(AboveThenLimitsIn(report, Wip), 3);
                ThenTheCountIs(BelowThenLimitsIn(report, Wip), 0);
                Assert.That(NowValueIn(report, Wip, Max).Value, Is.EqualTo(13));
            }
        }

        // @driving_port @real-io @us-05 @slice-05 @contract-shape:pure-function
        [Test]
        [Ignore(PendingSlice05)]
        public async Task Older_work_in_progress_shows_both_totals_and_both_averages_on_each_side()
        {
            var lightspeed = ATeam(Lightspeed);
            TheTeamWorksOn(lightspeed, 10, FirstOfJuly);

            var reportId = await GivenTheAdminCreated(lightspeed, ThirtyDaysToTheEndOfAugust());

            var report = await TheReport(lightspeed, reportId);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ThenValueIn(report, WorkItemAge, TotalWindowAverage), Is.EqualTo(Shown(475, 30)));
                Assert.That(ThenValueIn(report, WorkItemAge, TotalLastDay).Value, Is.EqualTo(620));
                Assert.That(ThenValueIn(report, WorkItemAge, AverageWindowAverage), Is.EqualTo(Shown(47.5, 30)));
                Assert.That(ThenValueIn(report, WorkItemAge, AverageLastDay), Is.EqualTo(Shown(62, 10)));
                Assert.That(NowValueIn(report, WorkItemAge, TotalWindowAverage), Is.EqualTo(Shown(795, 30)));
                Assert.That(NowValueIn(report, WorkItemAge, TotalLastDay).Value, Is.EqualTo(940));
                Assert.That(NowValueIn(report, WorkItemAge, AverageWindowAverage), Is.EqualTo(Shown(79.5, 30)));
                Assert.That(NowValueIn(report, WorkItemAge, AverageLastDay), Is.EqualTo(Shown(94, 10)));
                ThenTheCountIs(AboveThenLimitsIn(report, WorkItemAge), 30);
                ThenTheCountIs(BelowThenLimitsIn(report, WorkItemAge), 0);
            }
        }

        // @driving_port @real-io @us-05 @slice-05 @kpi-OUT-5878-G3 @contract-shape:pure-function
        [Test]
        [Ignore(PendingSlice05)]
        public async Task Thens_last_day_total_age_matches_the_Metrics_tab_for_that_day()
        {
            var lightspeed = ATeam(Lightspeed);
            TheTeamWorksOn(lightspeed, 10, FirstOfJuly);
            TheTeamWorksOn(lightspeed, 3, new DateOnly(2026, 8, 20));

            var reportId = await GivenTheAdminCreated(lightspeed, ThirtyDaysToTheEndOfAugust());

            var report = await TheReport(lightspeed, reportId);
            var metricsTab = await TheMetricsTabTotalWorkItemAge(lightspeed, ThenEnds);
            Assert.That(ThenValueIn(report, WorkItemAge, TotalLastDay).Value, Is.EqualTo(metricsTab.GetDouble()));
        }

        // @driving_port @real-io @us-05 @slice-05 @boundary @contract-shape:pure-function
        // Ten Work Items start on 17 Aug: 2-16 Aug have nothing in progress and no average age to take part.
        [Test]
        [Ignore(PendingSlice05)]
        public async Task Days_with_nothing_in_progress_are_left_out_of_the_average_age_but_not_out_of_the_total()
        {
            var lightspeed = ATeam(Lightspeed);
            TheTeamWorksOn(lightspeed, 10, new DateOnly(2026, 8, 17));

            var reportId = await GivenTheAdminCreated(lightspeed, ThirtyDaysToTheEndOfAugust());

            var report = await TheReport(lightspeed, reportId);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ThenValueIn(report, WorkItemAge, AverageWindowAverage), Is.EqualTo(Shown(8, 15)));
                Assert.That(ThenValueIn(report, WorkItemAge, TotalWindowAverage), Is.EqualTo(Shown(40, 30)));
                Assert.That(ThenValueIn(report, WorkItemAge, AverageLastDay), Is.EqualTo(Shown(15, 10)));
            }
        }

        // @driving_port @real-io @us-05 @slice-05 @error @contract-shape:pure-function
        [Test]
        [Ignore(PendingSlice05)]
        public async Task With_nothing_in_progress_the_average_age_is_a_dash_and_the_total_a_real_zero()
        {
            var nova = ATeam("Team Nova");

            var reportId = await GivenTheAdminCreated(nova, ThirtyDaysToTheEndOfAugust());

            var report = await TheReport(nova, reportId);
            using (Assert.EnterMultipleScope())
            {
                ThenItIsADashFor(ThenValueIn(report, WorkItemAge, AverageWindowAverage), Reason.NoWip);
                ThenItIsADashFor(ThenValueIn(report, WorkItemAge, AverageLastDay), Reason.NoWip);
                Assert.That(ThenValueIn(report, WorkItemAge, TotalWindowAverage), Is.EqualTo(Shown(0, 30)));
                Assert.That(ThenValueIn(report, Wip, Average), Is.EqualTo(Shown(0, 30)));
            }
        }

        // --- Steps ---

        private static readonly string[] AllFourMetrics = [CycleTime, Throughput, Wip, WorkItemAge];

        private static NewReport ThirtyDaysToTheEndOfAugust() => new()
        {
            ThenEnd = ThenEnds,
            ThenLengthDays = 30,
            NowLengthDays = 30,
            Shown = AllFourMetrics,
        };
    }
}
