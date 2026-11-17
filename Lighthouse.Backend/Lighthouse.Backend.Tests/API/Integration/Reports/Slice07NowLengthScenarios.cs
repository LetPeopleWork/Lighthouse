using System.Net;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.API.Integration.Reports
{
    /// <summary>
    /// Now is always as long as Then and ends today, used in full even where it overlaps Then. The report also
    /// says how many days ago Then ended. A create asking for a Now of another length is refused; nothing a
    /// reader puts in the address changes Now.
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

        [TestCase(2026, 7, 31, 63)]
        [TestCase(2026, 10, 2, 0)]
        public async Task The_report_says_how_many_days_ago_Then_ended(int year, int month, int day, int daysAgo)
        {
            var lightspeed = ATeam(Lightspeed);

            var reportId = await GivenTheAdminCreated(lightspeed, new NewReport { ThenEnd = new DateOnly(year, month, day) });

            Assert.That(DaysSinceThenEndedIn(await TheReport(lightspeed, reportId)), Is.EqualTo(daysAgo));
        }

        [Test]
        public async Task Without_a_Now_length_Now_is_as_long_as_Then()
        {
            var gravity = ATeam("Team Gravity");

            var reportId = await GivenTheAdminCreated(gravity, new NewReport { ThenLengthDays = 45, NowLengthDays = null });

            Assert.That(NowWindowIn(await TheReport(gravity, reportId)).LengthDays, Is.EqualTo(45));
        }

        [TestCase(13)]
        [TestCase(0)]
        [TestCase(30)]
        public async Task A_Now_of_another_length_than_Then_is_refused(int nowLengthDays)
        {
            var gravity = ATeam("Team Gravity");

            TheCallerAdministers(gravity);
            using var refused = await CreateAReport(gravity, new NewReport { NowLengthDays = nowLengthDays });

            await ThenItIsRefusedFor(refused, Reason.NowLengthDiffersFromThen);
            await ThenTheOwnerHoldsNoReport(gravity);
        }

        // 366 days ending 2 Oct 2026 start on 2 Oct 2025, the oldest day the Team keeps.
        [Test]
        public async Task A_Then_and_Now_starting_on_the_oldest_kept_day_are_accepted()
        {
            var gravity = ATeam("Team Gravity");

            var reportId = await GivenTheAdminCreated(gravity, new NewReport { ThenLengthDays = 366 });

            Assert.That(NowWindowIn(await TheReport(gravity, reportId)), Is.EqualTo(new ReportWindow("2025-10-02", "2026-10-02", 366)));
        }

        // Now follows Then's length; a reader cannot change it, not even for one view.
        [Test]
        public async Task Asking_for_another_Now_in_the_address_changes_nothing()
        {
            var gravity = ATeam("Team Gravity");
            var reportId = await GivenTheAdminCreated(gravity, new NewReport { ThenEnd = new DateOnly(2026, 9, 2), ThenLengthDays = 30 });

            TheCallerOnlyReads(gravity);
            using var asked = await Client.GetAsync($"{ReportsOf(gravity)}/{reportId}?nowLengthDays=7");
            var body = await asked.Content.ReadAsStringAsync();

            Assert.That(asked.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

            using var document = JsonDocument.Parse(body);
            var nowLength = NowWindowIn(document.RootElement).LengthDays;

            Assert.That(nowLength, Is.EqualTo(30));
        }

    }
}
