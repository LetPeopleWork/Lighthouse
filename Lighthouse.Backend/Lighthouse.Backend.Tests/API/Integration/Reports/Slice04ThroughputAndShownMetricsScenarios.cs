namespace Lighthouse.Backend.Tests.API.Integration.Reports
{
    /// <summary>
    /// A report shows the metrics its creator ticked, but freezes every metric that applies to its owner, so a
    /// metric ticked later still shows Then as it was. {Throughput} is the total finished in the window and the
    /// average per day, Then and Now; Now's days beyond Then's limits are counted against the daily series.
    ///
    /// Gravity's Then (2-31 Aug 2026) finished 1 and 2 Work Items on alternate days: 45 in 30 days, 1.5 a day,
    /// average moving range 1, so the upper limit is 1.5 + 2.66 = 4.16 and the lower one is clamped to 0.
    ///
    /// Driving ports: GET /teams/{id}/reports/templates, POST/GET /teams/{id}/reports.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5878-baseline")]
    [Category("slice-04")]
    public class Slice04ThroughputAndShownMetricsScenarios : ReportsAcceptanceTest
    {
        private const string NotInTheCatalog = "velocity";

        private static readonly DateOnly ThenStarts = new(2026, 8, 2);

        private static readonly DateOnly NowStarts = new(2026, 9, 3);

        // @driving_port @real-io @us-04 @slice-04 @contract-shape:pure-function
        [Test]
        [Ignore(PendingSlice04)]
        public async Task A_Team_can_be_reported_on_Cycle_Time_and_Throughput()
        {
            var gravity = ATeam("Team Gravity");

            TheCallerAdministers(gravity);
            var templates = await TheTypesOfReportFor(gravity);

            Assert.That(MetricKeysOfThenAndNowIn(templates), Is.SupersetOf(CycleTimeAndThroughput));
        }

        // @driving_port @real-io @us-04 @slice-04 @kpi-OUT-5878-K2-reports-reopened @contract-shape:pure-function
        // Then is the 90 days to 31 Jul 2026 (3 May - 31 Jul), Now the last 90 days (5 Jul - 2 Oct): the two
        // overlap by 27 days, and the Work Items finished in those days count on both sides.
        [Test]
        [Ignore(PendingSlice04)]
        public async Task Higher_Throughput_is_shown_as_the_total_and_the_average_per_day()
        {
            var lightspeed = GivenLightspeedFinishedFiftyTwoWorkItemsInThenAndSixtyFiveInNow();
            var reportId = await GivenTheAdminCreated(lightspeed, new NewReport
            {
                ThenEnd = new DateOnly(2026, 7, 31),
                ThenLengthDays = 90,
                NowLengthDays = 90,
                Shown = [CycleTime, Throughput],
            });

            var report = await TheReport(lightspeed, reportId);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ThenValueIn(report, Throughput, Total), Is.EqualTo(Shown(52, 90)));
                Assert.That(ThenValueIn(report, Throughput, PerDay).Value, Is.EqualTo(52d / 90).Within(1e-9));
                Assert.That(ThenValueIn(report, Throughput, PerDay).SampleSize, Is.EqualTo(90));
                Assert.That(NowValueIn(report, Throughput, Total), Is.EqualTo(Shown(65, 90)));
                Assert.That(NowValueIn(report, Throughput, PerDay).Value, Is.EqualTo(65d / 90).Within(1e-9));
                Assert.That(NowValueIn(report, Throughput, PerDay).SampleSize, Is.EqualTo(90));
            }
        }

        // @driving_port @real-io @us-04 @slice-04 @contract-shape:pure-function
        [Test]
        [Ignore(PendingSlice04)]
        public async Task Days_of_Now_with_Throughput_above_Thens_limit_are_counted()
        {
            var gravity = GivenGravityFinishedOneAndTwoWorkItemsOnAlternateDaysInAugust();
            var reportId = await GivenTheAdminCreated(gravity, ThirtyDaysToTheEndOfAugust([CycleTime, Throughput]));

            GivenGravityFinishedTwoADayInNowWithTwoDaysOfFive(gravity);

            var report = await TheReport(gravity, reportId);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ThenValueIn(report, Throughput, Total), Is.EqualTo(Shown(45, 30)));
                Assert.That(ThenValueIn(report, Throughput, PerDay), Is.EqualTo(Shown(1.5, 30)));
                Assert.That(NowValueIn(report, Throughput, Total), Is.EqualTo(Shown(66, 30)));
                Assert.That(NowValueIn(report, Throughput, PerDay), Is.EqualTo(Shown(2.2, 30)));
                ThenTheCountIs(AboveThenLimitsIn(report, Throughput), 2);
                ThenItIsADashFor(BelowThenLimitsIn(report, Throughput), Reason.LowerLimitZero);
            }
        }

        // @driving_port @real-io @us-04 @slice-04 @contract-shape:bounded-change
        // Every metric that applies is frozen, ticked or not, so ticking it later still shows Then as it was.
        [Test]
        [Ignore(PendingSlice04)]
        public async Task A_metric_that_is_not_shown_is_still_frozen()
        {
            var gravity = GivenGravityFinishedOneAndTwoWorkItemsOnAlternateDaysInAugust();

            var reportId = await GivenTheAdminCreated(gravity, ThirtyDaysToTheEndOfAugust([CycleTime]));

            var report = await TheReport(gravity, reportId);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ShownMetricsIn(report), Is.EqualTo(OnlyCycleTime));
                Assert.That(ThenValueIn(report, Throughput, Total), Is.EqualTo(Shown(45, 30)));
            }
        }

        // @driving_port @real-io @us-04 @slice-04 @error @contract-shape:unbounded-preservation
        [Test]
        [Ignore(PendingSlice04)]
        public async Task A_report_that_shows_no_metric_is_refused()
        {
            var gravity = ATeam("Team Gravity");

            TheCallerAdministers(gravity);
            using var refused = await CreateAReport(gravity, ThirtyDaysToTheEndOfAugust([]));

            await ThenItIsRefusedFor(refused, Reason.NoMetricShown);
            await ThenTheOwnerHoldsNoReport(gravity);
        }

        // @driving_port @real-io @us-04 @slice-04 @error @contract-shape:unbounded-preservation
        [Test]
        [Ignore(PendingSlice04)]
        public async Task A_metric_that_is_not_in_the_catalog_is_refused()
        {
            var gravity = ATeam("Team Gravity");

            TheCallerAdministers(gravity);
            using var refused = await CreateAReport(gravity, ThirtyDaysToTheEndOfAugust([CycleTime, NotInTheCatalog]));

            await ThenItIsRefusedFor(refused, Reason.UnknownMetric);
            await ThenTheOwnerHoldsNoReport(gravity);
        }

        // @driving_port @real-io @us-04 @slice-04 @boundary @contract-shape:unbounded-preservation
        [Test]
        [Ignore(PendingSlice04)]
        public async Task An_older_report_did_not_capture_Throughput()
        {
            var gravity = GivenGravityFinishedOneAndTwoWorkItemsOnAlternateDaysInAugust();
            var reportId = await GivenTheAdminCreated(gravity, ThirtyDaysToTheEndOfAugust([CycleTime]));
            TheStoredPayloadIsReshaped(reportId, payload => WithoutTheCaptureOf(payload, Throughput));

            var report = await TheReport(gravity, reportId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(MetricIn(report, Throughput), Is.Null, "a metric the report never captured is not captured, not zero");
                Assert.That(ThenValueIn(report, CycleTime, P85).IsPresent, Is.True);
            }
        }

        // @driving_port @real-io @us-04 @slice-04 @error @contract-shape:pure-function
        // Nothing finished on any day of Then: a real zero to show, but no spread to draw limits from.
        [Test]
        [Ignore(PendingSlice04)]
        public async Task A_Then_where_nothing_finished_shows_zero_and_a_dash_for_the_days_beyond()
        {
            var nova = ATeam("Team Nova");
            var reportId = await GivenTheAdminCreated(nova, ThirtyDaysToTheEndOfAugust([Throughput]));

            TheTeamFinishedPerDay(nova, NowStarts, 1, 1, 1);

            var report = await TheReport(nova, reportId);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ThenValueIn(report, Throughput, Total), Is.EqualTo(Shown(0, 30)));
                Assert.That(ThenValueIn(report, Throughput, PerDay), Is.EqualTo(Shown(0, 30)));
                ThenItIsADashFor(AboveThenLimitsIn(report, Throughput), Reason.NoProcess);
                ThenItIsADashFor(BelowThenLimitsIn(report, Throughput), Reason.NoProcess);
            }
        }

        // --- Steps ---

        private static readonly string[] CycleTimeAndThroughput = [CycleTime, Throughput];

        private static readonly string?[] OnlyCycleTime = [CycleTime];

        private static NewReport ThirtyDaysToTheEndOfAugust(string[] shown) => new()
        {
            ThenEnd = new DateOnly(2026, 8, 31),
            ThenLengthDays = 30,
            NowLengthDays = 30,
            Shown = shown,
        };

        /// <summary>
        /// From 3 May to 2 Oct 2026, one Work Item a day on chosen days: 39 from 3 May to 4 Jul, 13 from 5 to 31
        /// Jul, 52 from 1 Aug to 2 Oct - so 52 in Then and 65 in Now.
        /// </summary>
        private OwnerUnderTest GivenLightspeedFinishedFiftyTwoWorkItemsInThenAndSixtyFiveInNow()
        {
            var lightspeed = ATeam(Lightspeed);
            int[] beforeTheOverlap = [.. Enumerable.Range(0, 63).Select(day => day < 39 ? 1 : 0)];
            int[] theOverlap = [.. Enumerable.Range(0, 27).Select(day => day % 2 == 0 && day < 26 ? 1 : 0)];
            int[] afterTheOverlap = [.. Enumerable.Range(0, 63).Select(day => day < 52 ? 1 : 0)];

            TheTeamFinishedPerDay(lightspeed, new DateOnly(2026, 5, 3), [.. beforeTheOverlap, .. theOverlap, .. afterTheOverlap]);
            return lightspeed;
        }

        private OwnerUnderTest GivenGravityFinishedOneAndTwoWorkItemsOnAlternateDaysInAugust()
        {
            var gravity = ATeam("Team Gravity");
            TheTeamFinishedPerDay(gravity, ThenStarts, [.. Enumerable.Range(0, 30).Select(day => day % 2 == 0 ? 1 : 2)]);
            return gravity;
        }

        /// <summary>3 Sep - 2 Oct 2026: two a day, except 10 and 20 Sep with five each - 66 in 30 days.</summary>
        private void GivenGravityFinishedTwoADayInNowWithTwoDaysOfFive(OwnerUnderTest gravity)
            => TheTeamFinishedPerDay(gravity, NowStarts, [.. Enumerable.Range(0, 30).Select(day => day is 7 or 17 ? 5 : 2)]);
    }
}
