using System.Text.Json;

namespace Lighthouse.Backend.Tests.API.Integration.Reports
{
    /// <summary>
    /// {WIP} and Total {Work Item Age} each summarise one value per day of the window as four lines: Average,
    /// Median, Min and Max. A day's {WIP} is how many Work Items were in progress that day; its total age is the
    /// sum of their ages. Every day counts, a day with nothing in progress as a real 0, so neither metric is ever
    /// "—". The median is the window's middle day when the days are sorted, and for an even number of days the
    /// lower of the two middle ones - the same rule as the Metrics tab's 50th percentile, so a report's Median and
    /// its 50th never disagree. Every value rests on the window's number of days.
    ///
    /// Then is 2-31 Aug 2026 and Now 3 Sep - 2 Oct 2026, both 30 days, unless a scenario says otherwise. A Work
    /// Item is in progress from the day it started until the day before it finished; it is 1 day old on the day
    /// it started, so one started on 1 Jul is 33 days old on 2 Aug, 62 on 31 Aug, 65 on 3 Sep and 94 on 2 Oct.
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

        private static readonly DateOnly ThenStarts = new(2026, 8, 2);

        private static readonly DateOnly ThenEnds = new(2026, 8, 31);

        [Test]
        public async Task A_Team_can_be_reported_on_all_four_metrics()
        {
            var gravity = ATeam("Team Gravity");

            TheCallerAdministers(gravity);
            var templates = await TheTypesOfReportFor(gravity);

            Assert.That(MetricKeysOfThenAndNowIn(templates), Is.EquivalentTo(AllFourMetrics));
        }

        // Then: 6 in progress on 2-11 Aug, 9 on 12-21 Aug, 12 on 22-31 Aug.
        // Now: 9 on 3-12 Sep, 7 on 13-22 Sep, 5 on 23 Sep - 2 Oct.
        [Test]
        public async Task Lower_WIP_shows_on_every_line()
        {
            var gravity = GivenGravityLoweredItsWipThroughSeptember();

            var reportId = await GivenTheAdminCreated(gravity, ThirtyDaysToTheEndOfAugust());

            var report = await TheReport(gravity, reportId);
            using (Assert.EnterMultipleScope())
            {
                ThenTheLinesRead(ThenValueIn, report, Wip, new Lines(Average: 9, Median: 9, Min: 6, Max: 12, Days: 30));
                ThenTheLinesRead(NowValueIn, report, Wip, new Lines(Average: 7, Median: 7, Min: 5, Max: 9, Days: 30));
            }
        }

        // Then is the 15 days to 31 Aug (17-31 Aug): 2 in progress on 17-23 Aug, 4 on 24-31 Aug. Sorted, the
        // 7th of 15 days is the last 2 - the day just below the middle, where a textbook median would take the
        // 8th, a 4. Now's 30 days: 4 in progress on 3-17 Sep, 2 on 18 Sep - 2 Oct. Sorted, the 15th and 16th
        // days are 2 and 4: the Median is the lower, 2, never their mean, 3.
        [Test]
        public async Task The_median_is_the_lower_middle_day_for_an_even_and_an_odd_number_of_days()
        {
            var vega = ATeam("Team Vega");
            TheTeamWorksOn(vega, 2, FirstOfJuly);
            TheTeamWorksOn(vega, 2, new DateOnly(2026, 8, 24), finishedOn: new DateOnly(2026, 9, 18));

            var reportId = await GivenTheAdminCreated(vega, new NewReport
            {
                ThenEnd = ThenEnds,
                ThenLengthDays = 15,
                NowLengthDays = 30,
                Shown = AllFourMetrics,
            });

            var report = await TheReport(vega, reportId);
            using (Assert.EnterMultipleScope())
            {
                ThenTheLinesRead(ThenValueIn, report, Wip, new Lines(Average: 46d / 15, Median: 2, Min: 2, Max: 4, Days: 15));
                ThenTheLinesRead(NowValueIn, report, Wip, new Lines(Average: 3, Median: 2, Min: 2, Max: 4, Days: 30));
            }
        }

        // Ten Work Items in progress since 1 Jul: Then's daily totals run 330, 340, … 620 days and Now's 650,
        // 660, … 940. The Median is the 15th of 30 days, 470 and 790, not the textbook 475 and 795.
        [Test]
        public async Task Older_work_in_progress_shows_a_growing_total_age_on_every_line()
        {
            var lightspeed = ATeam(Lightspeed);
            TheTeamWorksOn(lightspeed, 10, FirstOfJuly);

            var reportId = await GivenTheAdminCreated(lightspeed, ThirtyDaysToTheEndOfAugust());

            var report = await TheReport(lightspeed, reportId);
            using (Assert.EnterMultipleScope())
            {
                ThenTheLinesRead(ThenValueIn, report, WorkItemAge, new Lines(Average: 475, Median: 470, Min: 330, Max: 620, Days: 30));
                ThenTheLinesRead(NowValueIn, report, WorkItemAge, new Lines(Average: 795, Median: 790, Min: 650, Max: 940, Days: 30));
            }
        }

        // The Metrics tab's own total age, asked for each day of Then and summarised by the test itself.
        [Test]
        public async Task Thens_total_age_agrees_with_the_Metrics_tab_day_by_day()
        {
            var lightspeed = ATeam(Lightspeed);
            TheTeamWorksOn(lightspeed, 10, FirstOfJuly);
            TheTeamWorksOn(lightspeed, 3, new DateOnly(2026, 8, 20));
            TheTeamWorksOn(lightspeed, 2, new DateOnly(2026, 8, 5), finishedOn: new DateOnly(2026, 8, 25));

            var reportId = await GivenTheAdminCreated(lightspeed, ThirtyDaysToTheEndOfAugust());

            var report = await TheReport(lightspeed, reportId);
            var metricsTab = new List<double>();
            for (var day = ThenStarts; day <= ThenEnds; day = day.AddDays(1))
            {
                var totalAge = await TheMetricsTabTotalWorkItemAge(lightspeed, day);
                metricsTab.Add(totalAge.GetDouble());
            }

            var sorted = metricsTab.Order().ToList();
            ThenTheLinesRead(ThenValueIn, report, WorkItemAge, new Lines(
                Average: metricsTab.Average(),
                Median: sorted[Math.Max(1, sorted.Count / 2) - 1],
                Min: sorted[0],
                Max: sorted[^1],
                Days: 30));
        }

        // Ten Work Items start on 17 Aug: 2-16 Aug have nothing in progress, 15 days of 0 that take part in
        // every line. Total age on 17-31 Aug: 10, 20, … 150 days, so the Average is 1,200 / 30 = 40, not 80.
        [Test]
        public async Task Days_with_nothing_in_progress_count_as_zero_on_every_line()
        {
            var lightspeed = ATeam(Lightspeed);
            TheTeamWorksOn(lightspeed, 10, new DateOnly(2026, 8, 17));

            var reportId = await GivenTheAdminCreated(lightspeed, ThirtyDaysToTheEndOfAugust());

            var report = await TheReport(lightspeed, reportId);
            using (Assert.EnterMultipleScope())
            {
                ThenTheLinesRead(ThenValueIn, report, Wip, new Lines(Average: 5, Median: 0, Min: 0, Max: 10, Days: 30));
                ThenTheLinesRead(ThenValueIn, report, WorkItemAge, new Lines(Average: 40, Median: 0, Min: 0, Max: 150, Days: 30));
            }
        }

        [Test]
        public async Task With_nothing_in_progress_every_line_is_a_real_zero()
        {
            var nova = ATeam("Team Nova");

            var reportId = await GivenTheAdminCreated(nova, ThirtyDaysToTheEndOfAugust());

            var report = await TheReport(nova, reportId);
            var nothing = new Lines(Average: 0, Median: 0, Min: 0, Max: 0, Days: 30);
            using (Assert.EnterMultipleScope())
            {
                ThenTheLinesRead(ThenValueIn, report, Wip, nothing);
                ThenTheLinesRead(NowValueIn, report, Wip, nothing);
                ThenTheLinesRead(ThenValueIn, report, WorkItemAge, nothing);
                ThenTheLinesRead(NowValueIn, report, WorkItemAge, nothing);
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

        /// <summary>
        /// Six Work Items since 1 Jul, three more from 12 Aug and three from 22 Aug. The three of 22 Aug finish on
        /// 1 Sep, two of the first six on 13 Sep and two more on 23 Sep.
        /// </summary>
        private OwnerUnderTest GivenGravityLoweredItsWipThroughSeptember()
        {
            var gravity = ATeam("Team Gravity");
            TheTeamWorksOn(gravity, 2, FirstOfJuly);
            TheTeamWorksOn(gravity, 2, FirstOfJuly, finishedOn: new DateOnly(2026, 9, 13));
            TheTeamWorksOn(gravity, 2, FirstOfJuly, finishedOn: new DateOnly(2026, 9, 23));
            TheTeamWorksOn(gravity, 3, new DateOnly(2026, 8, 12));
            TheTeamWorksOn(gravity, 3, new DateOnly(2026, 8, 22), finishedOn: new DateOnly(2026, 9, 1));
            return gravity;
        }

        private sealed record Lines(double Average, double Median, double Min, double Max, int Days);

        /// <summary>
        /// The four lines of one side of a panel. Each is shown - a number, never a "—" - and rests on every day
        /// of the window.
        /// </summary>
        private static void ThenTheLinesRead(Func<JsonElement, string, string, CapturedValue> side, JsonElement report, string metric, Lines expected)
        {
            (string Member, double Value)[] lines =
            [
                (Average, expected.Average),
                (Median, expected.Median),
                (Min, expected.Min),
                (Max, expected.Max),
            ];

            using (Assert.EnterMultipleScope())
            {
                foreach (var (member, value) in lines)
                {
                    var shown = side(report, metric, member);
                    Assert.That(shown.IsPresent, Is.True, $"{metric} {member} is not in the report");
                    Assert.That(shown.Value, Is.EqualTo(value).Within(1e-9), $"{metric} {member}");
                    Assert.That(shown.SampleSize, Is.EqualTo(expected.Days), $"{metric} {member} rests on every day of the window");
                    Assert.That(shown.AbsentReason, Is.Null, $"{metric} {member} is never a dash");
                }
            }
        }
    }
}
