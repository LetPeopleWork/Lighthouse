using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Lighthouse.Backend.Tests.API.Integration.Reports
{
    /// <summary>
    /// Then can end on a day already past and be any length of at least 14 days. Its values are rebuilt once,
    /// from history, at creation - the same values the Metrics tab shows for those dates - and frozen from then
    /// on. A Then that ends after today, is shorter than 14 days, or starts before the oldest finished Work Item
    /// the Team still keeps is refused, and the refusal names the earliest day it may start.
    ///
    /// Windows count calendar days in the instance's zone, both ends included: 90 days ending 31 Jul 2026 are
    /// 3 May to 31 Jul.
    ///
    /// Driving ports: POST/GET /teams/{id}/reports, GET /teams/{id}/reports/templates, and the Metrics tab's
    /// cycleTimePercentiles and cycleTimeData for the parity check.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5878-baseline")]
    [Category("slice-02")]
    public class Slice02ThenInThePastScenarios : ReportsAcceptanceTest
    {
        private static readonly DateOnly EngagementStarted = new(2026, 8, 1);

        private static readonly DateOnly TheDayBeforeTheEngagement = new(2026, 7, 31);

        private static readonly DateOnly ThenStartsOn = new(2026, 5, 3);

        private static readonly int[] CycleTimesInsideThen = [4, 6, 7, 9, 10, 12, 15, 18, 21, 28];

        // @driving_port @real-io @us-02 @slice-02 @contract-shape:bounded-change
        [Test]
        public async Task A_coach_freezes_a_Then_that_ended_the_day_before_the_engagement_began()
        {
            var lightspeed = GivenLightspeedFinishedWorkItemsAroundTheEngagementStart();

            var reportId = await GivenTheAdminCreated(lightspeed, ThenOf(90, TheDayBeforeTheEngagement));

            var report = await TheReport(lightspeed, reportId);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ThenWindowIn(report), Is.EqualTo(new ReportWindow("2026-05-03", "2026-07-31", 90)));
                Assert.That(ThenValueIn(report, CycleTime, P85), Is.EqualTo(Shown(18, 10)));
            }
        }

        // @driving_port @real-io @us-02 @slice-02 @kpi-OUT-5878-G3 @contract-shape:pure-function
        // The rebuilt Then and the Metrics tab are read for the same inclusive days and must agree.
        [Test]
        public async Task The_rebuilt_Then_matches_the_Metrics_tab_for_the_same_days()
        {
            var lightspeed = GivenLightspeedFinishedWorkItemsAroundTheEngagementStart();
            var reportId = await GivenTheAdminCreated(lightspeed, ThenOf(90, TheDayBeforeTheEngagement));

            var report = await TheReport(lightspeed, reportId);
            var percentiles = await TheMetricsTabCycleTimePercentiles(lightspeed, ThenStartsOn, TheDayBeforeTheEngagement);
            var finished = await TheMetricsTabFinishedItems(lightspeed, ThenStartsOn, TheDayBeforeTheEngagement);

            var metricsTab85th = percentiles.EnumerateArray().Single(entry => entry.GetProperty("percentile").GetInt32() == 85).GetProperty("value").GetDouble();
            Assert.That(ThenValueIn(report, CycleTime, P85), Is.EqualTo(Shown(metricsTab85th, finished.GetArrayLength())));
        }

        // @driving_port @real-io @us-02 @slice-02 @contract-shape:bounded-change
        [Test]
        public async Task A_coach_freezes_a_Then_of_a_custom_length()
        {
            var gravity = ATeam("Team Gravity");

            var reportId = await GivenTheAdminCreated(gravity, ThenOf(45, new DateOnly(2026, 9, 13)));

            Assert.That(ThenWindowIn(await TheReport(gravity, reportId)), Is.EqualTo(new ReportWindow("2026-07-31", "2026-09-13", 45)));
        }

        // @driving_port @real-io @us-02 @slice-02 @boundary @contract-shape:bounded-change
        // Counting both ends, 1 to 14 Sep is 14 days; a rule that subtracted the dates would call it 13.
        [Test]
        [Ignore(PendingSlice02)]
        public async Task A_Then_of_exactly_fourteen_days_is_accepted()
        {
            var gravity = ATeam("Team Gravity");

            var reportId = await GivenTheAdminCreated(gravity, ThenOf(14, new DateOnly(2026, 9, 14)));

            Assert.That(ThenWindowIn(await TheReport(gravity, reportId)), Is.EqualTo(new ReportWindow("2026-09-01", "2026-09-14", 14)));
        }

        // @driving_port @real-io @us-02 @slice-02 @error @contract-shape:unbounded-preservation
        [TestCase(13)]
        [TestCase(10)]
        [TestCase(0)]
        [TestCase(-30)]
        [Ignore(PendingSlice02)]
        public async Task A_Then_shorter_than_fourteen_days_is_refused_and_nothing_is_created(int lengthDays)
        {
            var gravity = ATeam("Team Gravity");

            TheCallerAdministers(gravity);
            using var refused = await CreateAReport(gravity, ThenOf(lengthDays, new DateOnly(2026, 9, 14)));

            await ThenItIsRefusedFor(refused, Reason.ThenTooShort);
            await ThenTheOwnerHoldsNoReport(gravity);
        }

        // @driving_port @real-io @us-02 @slice-02 @error @contract-shape:unbounded-preservation
        [Test]
        [Ignore(PendingSlice02)]
        public async Task A_Then_ending_after_today_is_refused()
        {
            var lightspeed = ATeam(Lightspeed);

            TheCallerAdministers(lightspeed);
            using var refused = await CreateAReport(lightspeed, ThenOf(90, TodayOnTheInstance.AddDays(1)));

            await ThenItIsRefusedFor(refused, Reason.ThenEndsInTheFuture);
            await ThenTheOwnerHoldsNoReport(lightspeed);
        }

        // @driving_port @real-io @us-02 @slice-02 @error @contract-shape:unbounded-preservation
        // 90 days ending 15 Nov 2025 start on 18 Aug 2025; the Team keeps finished Work Items from 2 Oct 2025.
        [Test]
        [Ignore(PendingSlice02)]
        public async Task A_Then_reaching_past_the_data_cutoff_is_refused_naming_the_earliest_start()
        {
            var lightspeed = ATeam(Lightspeed);

            TheCallerAdministers(lightspeed);
            using var refused = await CreateAReport(lightspeed, ThenOf(90, new DateOnly(2025, 11, 15)));

            await ThenItIsRefusedFor(refused, Reason.ThenBeforeCutoff);
            Assert.That(await refused.Content.ReadAsStringAsync(), Does.Contain("2025-10-02"));
        }

        // @driving_port @real-io @us-02 @slice-02 @boundary @contract-shape:bounded-change
        [Test]
        [Ignore(PendingSlice02)]
        public async Task A_Then_starting_on_the_oldest_kept_day_is_accepted()
        {
            var lightspeed = ATeam(Lightspeed);

            var reportId = await GivenTheAdminCreated(lightspeed, ThenOf(90, new DateOnly(2025, 12, 30)));

            Assert.That(ThenWindowIn(await TheReport(lightspeed, reportId)), Is.EqualTo(new ReportWindow("2025-10-02", "2025-12-30", 90)));
        }

        // @driving_port @real-io @us-02 @slice-02 @boundary @contract-shape:unbounded-preservation
        // The cutoff is the Team's own setting, so a Team that keeps 180 days cannot reach back to April's 4th.
        [Test]
        [Ignore(PendingSlice02)]
        public async Task The_earliest_start_follows_how_long_the_Team_keeps_finished_Work_Items()
        {
            var gravity = ATeam("Team Gravity", keepsFinishedItemsForDays: 180);

            TheCallerAdministers(gravity);
            var templates = await TheTypesOfReportFor(gravity);
            using var refused = await CreateAReport(gravity, ThenOf(14, new DateOnly(2026, 4, 17)));

            Assert.That(TextOf(templates, "earliestStart"), Is.EqualTo("2026-04-05"));
            await ThenItIsRefusedFor(refused, Reason.ThenBeforeCutoff);
        }

        // @driving_port @real-io @us-02 @slice-02 @boundary @contract-shape:bounded-change
        // A Team set to keep every finished Work Item has no oldest kept day, so Then may start on any day.
        [Test]
        [Ignore(PendingSlice02)]
        public async Task A_Team_that_keeps_every_finished_Work_Item_has_no_earliest_start()
        {
            var pulsar = ATeam("Team Pulsar", keepsFinishedItemsForDays: 0);

            TheCallerAdministers(pulsar);
            var templates = await TheTypesOfReportFor(pulsar);
            using var created = await CreateAReport(pulsar, ThenOf(90, new DateOnly(2024, 6, 30)));

            Assert.That(templates.GetProperty("earliestStart").ValueKind, Is.EqualTo(JsonValueKind.Null));
            var report = await TheReport(pulsar, await ThenItWasCreated(created));
            Assert.That(ThenWindowIn(report), Is.EqualTo(new ReportWindow("2024-04-02", "2024-06-30", 90)));
        }

        // @driving_port @real-io @us-02 @slice-02 @boundary @contract-shape:unbounded-preservation
        // History that fills in later - a sync that brings an older finished Work Item - is not rebuilt into Then.
        [Test]
        public async Task Later_views_read_the_frozen_Then_not_a_rebuild()
        {
            var lightspeed = GivenLightspeedFinishedWorkItemsAroundTheEngagementStart();
            var reportId = await GivenTheAdminCreated(lightspeed, ThenOf(90, TheDayBeforeTheEngagement));

            TheTeamFinished(lightspeed, new Finished(new DateOnly(2026, 6, 15), 40), new Finished(new DateOnly(2026, 6, 16), 45));

            Assert.That(ThenValueIn(await TheReport(lightspeed, reportId), CycleTime, P85), Is.EqualTo(Shown(18, 10)));
        }

        // @driving_port @real-io @us-02 @slice-02 @error @contract-shape:unbounded-preservation
        [TestCase("thenEndDate")]
        [TestCase("thenLengthDays")]
        [Ignore(PendingSlice02)]
        public async Task A_Then_missing_its_end_or_its_length_is_refused(string leftOut)
        {
            var lightspeed = ATeam(Lightspeed);
            var request = ThenOf(90, TheDayBeforeTheEngagement).ToJson();
            request["thenAndNow"]!.AsObject().Remove(leftOut);

            TheCallerAdministers(lightspeed);
            using var refused = await CreateAReportFromRawRequest(lightspeed, request);

            Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), await refused.Content.ReadAsStringAsync());
            await ThenTheOwnerHoldsNoReport(lightspeed);
        }

        // @driving_port @real-io @us-02 @slice-02 @error @contract-shape:unbounded-preservation
        [TestCase("31.07.2026")]
        [TestCase("2026-02-30")]
        [TestCase("yesterday")]
        [Ignore(PendingSlice02)]
        public async Task A_Then_end_that_is_not_a_day_is_refused(string notADay)
        {
            var lightspeed = ATeam(Lightspeed);
            var request = ThenOf(90, TheDayBeforeTheEngagement).ToJson();
            request["thenAndNow"]!["thenEndDate"] = notADay;

            TheCallerAdministers(lightspeed);
            using var refused = await CreateAReportFromRawRequest(lightspeed, request);

            Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), await refused.Content.ReadAsStringAsync());
            await ThenTheOwnerHoldsNoReport(lightspeed);
        }

        // --- Steps ---

        private static NewReport ThenOf(int lengthDays, DateOnly endsOn) => new()
        {
            ThenEnd = endsOn,
            ThenLengthDays = lengthDays,
            NowLengthDays = lengthDays,
        };

        /// <summary>
        /// Ten Work Items finished inside 3 May - 31 Jul 2026 (85th percentile 18 days), plus one finished the
        /// day before that window and one the day after it, each slow enough to move the 85th percentile if a
        /// window were drawn a day too wide.
        /// </summary>
        private OwnerUnderTest GivenLightspeedFinishedWorkItemsAroundTheEngagementStart()
        {
            var lightspeed = ATeam(Lightspeed);

            TheTeamFinished(lightspeed, [.. CycleTimesInsideThen.Select((took, i) => new Finished(ThenStartsOn.AddDays(7 * i + 1), took))]);
            TheTeamFinished(lightspeed,
                new Finished(ThenStartsOn.AddDays(-1), 60),
                new Finished(EngagementStarted, 50));

            return lightspeed;
        }
    }
}
