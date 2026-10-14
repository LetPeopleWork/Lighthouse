using System.Net;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.API.Integration.Reports
{
    /// <summary>
    /// How long Now is, is chosen when the report is created - Then's length unless the coach picks another -
    /// and stored on the report, so every reader, and later every scheduled send, sees the same window. Now is
    /// always that many days ending today, used in full even where it overlaps Then. The report also says how
    /// many days ago Then ended. A Now shorter than 14 days, or reaching back past the oldest finished Work Item
    /// the Team keeps, is refused; nothing a reader puts in the address changes it.
    ///
    /// Driving ports: POST/GET /teams/{id}/reports.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5878-baseline")]
    [Category("slice-07")]
    public class Slice07NowLengthScenarios : ReportsAcceptanceTest
    {
        private static readonly DateOnly EndOfJuly = new(2026, 7, 31);

        // @driving_port @real-io @us-07 @slice-07 @contract-shape:bounded-change
        [Test]
        public async Task A_coach_sets_a_shorter_Now_and_every_later_view_uses_it()
        {
            var gravity = ATeam("Team Gravity");
            var reportId = await GivenTheAdminCreated(gravity, new NewReport { NowLengthDays = 30 });

            TodayIs(new DateOnly(2026, 11, 30));
            TheCallerOnlyReads(gravity);
            var report = await TheReport(gravity, reportId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(NowWindowIn(report), Is.EqualTo(new ReportWindow("2026-11-01", "2026-11-30", 30)));
                Assert.That(DaysSinceThenEndedIn(report), Is.EqualTo(59));
            }
        }

        // @driving_port @real-io @us-07 @slice-07 @boundary @contract-shape:pure-function
        // 27 days of this Now lie inside Then; they are compared as they are, never clipped away.
        [Test]
        public async Task Now_may_overlap_Then_and_is_used_in_full()
        {
            var lightspeed = ATeam(Lightspeed);

            var reportId = await GivenTheAdminCreated(lightspeed, new NewReport { ThenEnd = EndOfJuly });

            var report = await TheReport(lightspeed, reportId);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ThenWindowIn(report), Is.EqualTo(new ReportWindow("2026-05-03", "2026-07-31", 90)));
                Assert.That(NowWindowIn(report), Is.EqualTo(NinetyDaysToToday));
            }
        }

        // @driving_port @real-io @us-07 @slice-07 @contract-shape:pure-function
        [TestCase(2026, 7, 31, 63)]
        [TestCase(2026, 10, 2, 0)]
        public async Task The_report_says_how_many_days_ago_Then_ended(int year, int month, int day, int daysAgo)
        {
            var lightspeed = ATeam(Lightspeed);

            var reportId = await GivenTheAdminCreated(lightspeed, new NewReport { ThenEnd = new DateOnly(year, month, day) });

            Assert.That(DaysSinceThenEndedIn(await TheReport(lightspeed, reportId)), Is.EqualTo(daysAgo));
        }

        // @driving_port @real-io @us-07 @slice-07 @boundary @contract-shape:bounded-change
        [Test]
        public async Task Without_a_Now_length_Now_is_as_long_as_Then()
        {
            var gravity = ATeam("Team Gravity");

            var reportId = await GivenTheAdminCreated(gravity, new NewReport { ThenLengthDays = 45, NowLengthDays = null });

            Assert.That(NowWindowIn(await TheReport(gravity, reportId)).LengthDays, Is.EqualTo(45));
        }

        // @driving_port @real-io @us-07 @slice-07 @error @contract-shape:unbounded-preservation
        [TestCase(13)]
        [TestCase(0)]
        public async Task A_Now_shorter_than_fourteen_days_is_refused(int nowLengthDays)
        {
            var gravity = ATeam("Team Gravity");

            TheCallerAdministers(gravity);
            using var refused = await CreateAReport(gravity, new NewReport { NowLengthDays = nowLengthDays });

            await ThenItIsRefusedFor(refused, Reason.NowTooShort);
            await ThenTheOwnerHoldsNoReport(gravity);
        }

        // @driving_port @real-io @us-07 @slice-07 @boundary @contract-shape:bounded-change
        // 366 days ending 2 Oct 2026 start on 2 Oct 2025, the oldest day the Team keeps.
        [Test]
        public async Task A_Now_starting_on_the_oldest_kept_day_is_accepted()
        {
            var gravity = ATeam("Team Gravity");

            var reportId = await GivenTheAdminCreated(gravity, new NewReport { NowLengthDays = 366 });

            Assert.That(NowWindowIn(await TheReport(gravity, reportId)), Is.EqualTo(new ReportWindow("2025-10-02", "2026-10-02", 366)));
        }

        // @driving_port @real-io @us-07 @slice-07 @error @contract-shape:unbounded-preservation
        [TestCase(367)]
        [TestCase(500)]
        public async Task A_Now_reaching_past_the_data_cutoff_is_refused(int nowLengthDays)
        {
            var gravity = ATeam("Team Gravity");

            TheCallerAdministers(gravity);
            using var refused = await CreateAReport(gravity, new NewReport { NowLengthDays = nowLengthDays });

            await ThenItIsRefusedFor(refused, Reason.NowBeforeCutoff);
            await ThenTheOwnerHoldsNoReport(gravity);
        }

        // @driving_port @real-io @us-07 @slice-07 @error @contract-shape:unbounded-preservation
        // Now is stored on the report; a reader cannot change it, not even for one view.
        [Test]
        public async Task Asking_for_another_Now_in_the_address_changes_nothing()
        {
            var gravity = ATeam("Team Gravity");
            var reportId = await GivenTheAdminCreated(gravity, new NewReport { NowLengthDays = 30 });

            TheCallerOnlyReads(gravity);
            using var asked = await Client.GetAsync($"{ReportsOf(gravity)}/{reportId}?nowLengthDays=7");
            var body = await asked.Content.ReadAsStringAsync();

            Assert.That(asked.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            using var document = JsonDocument.Parse(body);
            var nowLength = NowWindowIn(document.RootElement).LengthDays;

            Assert.That(nowLength, Is.EqualTo(30));
        }

        // @driving_port @real-io @us-04 @us-07 @slice-07 @contract-shape:pure-function
        // Totals over different lengths cannot be compared, so the change is read on the per-day average.
        [Test]
        public async Task Throughput_with_a_Now_of_another_length_is_compared_per_day()
        {
            var dev = ATeam("Team Gravity");
            var thenEnds = new DateOnly(2026, 8, 31);
            TheTeamFinishedPerDay(dev, thenEnds.AddDays(-89), [.. Enumerable.Range(0, 90).Select(day => day % 2 == 0 ? 1 : 0)]);
            TheTeamFinishedPerDay(dev, new DateOnly(2026, 9, 3), [.. Enumerable.Repeat(1, 24)]);

            var reportId = await GivenTheAdminCreated(dev, new NewReport
            {
                ThenEnd = thenEnds,
                ThenLengthDays = 90,
                NowLengthDays = 30,
                Shown = [CycleTime, Throughput],
            });

            var report = await TheReport(dev, reportId);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ThenValueIn(report, Throughput, Total), Is.EqualTo(Shown(45, 90)));
                Assert.That(ThenValueIn(report, Throughput, PerDay), Is.EqualTo(Shown(0.5, 90)));
                Assert.That(NowValueIn(report, Throughput, Total), Is.EqualTo(Shown(24, 30)));
                Assert.That(NowValueIn(report, Throughput, PerDay), Is.EqualTo(Shown(0.8, 30)));
            }
        }
    }
}
