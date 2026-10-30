namespace Lighthouse.Backend.Tests.API.Integration.Reports
{
    /// <summary>
    /// The {Cycle Time} panel adds the 95th percentile, the one forecasts at the highest certainty lean on. Then
    /// freezes it with the 85th, 70th and 50th; Now computes it on every read; it rests on the same finished Work
    /// Items as the other three, and is the 95th the Metrics tab shows for the same days. A report frozen before
    /// the 95th was captured has no Then 95th at all - neither a value nor a dash - and the page reads it as "Not
    /// captured for this report"; its Now 95th is shown like any other.
    ///
    /// Team Lightspeed's Then is the 90 days to 31 Jul 2026 (3 May - 31 Jul); Now is the 90 days to today, 2 Oct
    /// 2026 (5 Jul - 2 Oct). Twenty Work Items finished in May belong to Then alone, twenty finished in August to
    /// Now alone. A percentile is the Metrics tab's: the item at position floor(p% of the count), counted from 1
    /// in ascending order, so the 95th of twenty is the 19th.
    ///
    /// Driving ports: POST/GET /teams/{id}/reports, and the Metrics tab's cycleTimePercentiles and cycleTimeData
    /// for the parity check.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5878-baseline")]
    [Category("slice-05b")]
    public class Slice05bCycleTime95thScenarios : ReportsAcceptanceTest
    {
        private static readonly DateOnly ThenStarts = new(2026, 5, 3);

        private static readonly DateOnly ThenEnds = new(2026, 7, 31);

        /// <summary>Sorted: 95th 34 days, 85th 24, 70th 16, 50th 11.</summary>
        private static readonly int[] ThenCycleTimes = [3, 4, 5, 6, 6, 7, 8, 9, 10, 11, 12, 13, 14, 16, 18, 21, 24, 28, 34, 40];

        /// <summary>Sorted: 95th 19 days, 85th 15, 70th 11, 50th 8.</summary>
        private static readonly int[] NowCycleTimes = [2, 3, 3, 4, 5, 5, 6, 7, 7, 8, 9, 9, 10, 11, 12, 13, 15, 17, 19, 22];

        [Test]
        public async Task The_Cycle_Time_panel_shows_the_95th_for_Then_and_Now()
        {
            var lightspeed = GivenLightspeedFinishedSlowerInMayThanInAugust();

            var reportId = await GivenTheAdminCreated(lightspeed, NinetyDaysToTheEndOfJuly());

            var report = await TheReport(lightspeed, reportId);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ThenValueIn(report, CycleTime, P95), Is.EqualTo(Shown(34, 20)));
                Assert.That(NowValueIn(report, CycleTime, P95), Is.EqualTo(Shown(19, 20)));
                Assert.That(ThenValueIn(report, CycleTime, P85), Is.EqualTo(Shown(24, 20)), "the 95th joins the other three, it replaces none");
                Assert.That(NowValueIn(report, CycleTime, P85), Is.EqualTo(Shown(15, 20)), "the 95th joins the other three, it replaces none");
            }
        }

        // The report and the Metrics tab are read for the same inclusive days and must agree.
        [Test]
        public async Task Thens_95th_is_the_95th_the_Metrics_tab_shows_for_the_same_days()
        {
            var lightspeed = GivenLightspeedFinishedSlowerInMayThanInAugust();
            var reportId = await GivenTheAdminCreated(lightspeed, NinetyDaysToTheEndOfJuly());

            var report = await TheReport(lightspeed, reportId);
            var percentiles = await TheMetricsTabCycleTimePercentiles(lightspeed, ThenStarts, ThenEnds);
            var finished = await TheMetricsTabFinishedItems(lightspeed, ThenStarts, ThenEnds);

            var metricsTab95th = percentiles.EnumerateArray().Single(entry => entry.GetProperty("percentile").GetInt32() == 95).GetProperty("value").GetDouble();
            Assert.That(ThenValueIn(report, CycleTime, P95), Is.EqualTo(Shown(metricsTab95th, finished.GetArrayLength())));
        }

        // Reports created before the 95th was captured froze the 85th, 70th and 50th only. Nothing is added to them on
        // reading: Then's 95th stays missing, and the page reads it as never captured, not as a dash.
        [Test]
        public async Task A_report_frozen_before_the_95th_was_captured_has_no_Then_95th_and_still_shows_Nows()
        {
            var lightspeed = GivenLightspeedFinishedSlowerInMayThanInAugust();
            var reportId = await GivenTheAdminCreated(lightspeed, NinetyDaysToTheEndOfJuly());
            TheStoredPayloadIsReshaped(reportId, payload => WithoutTheCaptureOf(payload, CycleTime, P95));

            var report = await TheReport(lightspeed, reportId);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ThenValueIn(report, CycleTime, P95), Is.EqualTo(CapturedValue.Missing), "never captured is neither a value nor a dash");
                Assert.That(ThenValueIn(report, CycleTime, P85), Is.EqualTo(Shown(24, 20)));
                Assert.That(ThenValueIn(report, CycleTime, P70), Is.EqualTo(Shown(16, 20)));
                Assert.That(ThenValueIn(report, CycleTime, P50), Is.EqualTo(Shown(11, 20)));
                Assert.That(NowValueIn(report, CycleTime, P95), Is.EqualTo(Shown(19, 20)));
            }
        }

        // The first reports froze the 85th alone, so their Then lacks the 95th as well as the 70th and 50th.
        [Test]
        public async Task A_report_that_froze_only_the_85th_has_no_Then_95th_either()
        {
            var lightspeed = GivenLightspeedFinishedSlowerInMayThanInAugust();
            var reportId = await GivenTheAdminCreated(lightspeed, NinetyDaysToTheEndOfJuly());
            TheStoredPayloadIsReshaped(reportId, payload =>
            {
                WithoutTheCaptureOf(payload, CycleTime, P95);
                WithoutTheCaptureOf(payload, CycleTime, P70);
                WithoutTheCaptureOf(payload, CycleTime, P50);
            });

            var report = await TheReport(lightspeed, reportId);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ThenValueIn(report, CycleTime, P95), Is.EqualTo(CapturedValue.Missing), "never captured is neither a value nor a dash");
                Assert.That(ThenValueIn(report, CycleTime, P85), Is.EqualTo(Shown(24, 20)));
                Assert.That(NowValueIn(report, CycleTime, P95), Is.EqualTo(Shown(19, 20)));
                Assert.That(NowValueIn(report, CycleTime, P50), Is.EqualTo(Shown(8, 20)));
            }
        }

        // One finished Work Item of 12 days: every percentile, the 95th included, is that item.
        [Test]
        public async Task With_a_single_finished_Work_Item_the_95th_is_that_item_with_its_sample_size()
        {
            var pulsar = ATeam("Team Pulsar");
            TheTeamFinished(pulsar, new Finished(new DateOnly(2026, 8, 20), 12));

            var reportId = await GivenTheAdminCreated(pulsar, new NewReport());

            var report = await TheReport(pulsar, reportId);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ThenValueIn(report, CycleTime, P95), Is.EqualTo(Shown(12, 1)));
                Assert.That(NowValueIn(report, CycleTime, P95), Is.EqualTo(Shown(12, 1)));
            }
        }

        [Test]
        public async Task With_no_finished_Work_Items_the_95th_is_a_dash_with_the_reason_never_a_zero()
        {
            var nova = ATeam("Team Nova");

            var reportId = await GivenTheAdminCreated(nova, new NewReport());

            var report = await TheReport(nova, reportId);
            using (Assert.EnterMultipleScope())
            {
                ThenItIsADashFor(ThenValueIn(report, CycleTime, P95), Reason.NoFinishedItems);
                ThenItIsADashFor(NowValueIn(report, CycleTime, P95), Reason.NoFinishedItems);
            }
        }

        // --- Steps ---

        private static NewReport NinetyDaysToTheEndOfJuly() => new()
        {
            ThenEnd = ThenEnds,
            ThenLengthDays = 90,
            NowLengthDays = 90,
        };

        /// <summary>Twenty Work Items finished one a day from 10 May, and twenty quicker ones one a day from 3 Aug 2026.</summary>
        private OwnerUnderTest GivenLightspeedFinishedSlowerInMayThanInAugust()
        {
            var lightspeed = ATeam(Lightspeed);
            var firstInMay = new DateOnly(2026, 5, 10);
            var firstInAugust = new DateOnly(2026, 8, 3);

            TheTeamFinished(lightspeed, [
                .. ThenCycleTimes.Select((took, i) => new Finished(firstInMay.AddDays(i), took)),
                .. NowCycleTimes.Select((took, i) => new Finished(firstInAugust.AddDays(i), took)),
            ]);
            return lightspeed;
        }
    }
}
