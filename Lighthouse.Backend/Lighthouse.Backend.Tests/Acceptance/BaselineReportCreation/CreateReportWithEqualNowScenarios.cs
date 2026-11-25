using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Modules.Reports.Application.Metrics;
using Lighthouse.Backend.Modules.Reports.Application;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.API.Integration.Reports;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Lighthouse.Backend.Tests.Acceptance.BaselineReportCreation
{
    /// <summary>
    /// Creating a Then &amp; Now report, for one Team or for several at once, at the Reports services with no web host.
    /// Nobody chooses how long Now is: it is always as long as Then and ends on the instance's today, which here is
    /// Friday 2 Oct 2026. A request that still names a Now of another length is refused and nothing is written.
    /// </summary>
    [TestFixture]
    public class CreateReportWithEqualNowScenarios
    {
        private const string NowLengthDiffersFromThen = "now-length-differs-from-then";

        private static readonly DateOnly Today = new(2026, 10, 2);

        private static readonly string[] CycleTimeShown = [CycleTimeReportMetric.MetricKey];

        private ServicePortHarness harness = null!;

        [SetUp]
        public void GiveTheScenarioItsOwnDatabase()
        {
            harness = new ServicePortHarness(services =>
            {
                foreach (var registration in ReportsAtThePortTest.WhatItRegisters)
                {
                    services.Add(registration);
                }
            });
        }

        [TearDown]
        public void RemoveTheScenariosDatabase() => harness.Dispose();

        [Test]
        public async Task A_report_left_at_its_default_end_starts_back_to_back_and_reads_a_Now_as_long_as_Then_ending_today()
        {
            var lightspeed = ATeam("Lightspeed");

            var created = await CreateAReport(lightspeed, ThenOf(Today.AddDays(-30), 30, nowLengthDays: null));

            TheReportReadsWindows(lightspeed, created,
                then: new ReportWindow(new DateOnly(2026, 8, 4), new DateOnly(2026, 9, 2), 30),
                now: new ReportWindow(new DateOnly(2026, 9, 3), Today, 30));
        }

        [TestCase(45, NowLengthDiffersFromThen, TestName = "A 90-day Then asking for a 45-day Now is refused because Now is always as long as Then")]
        [TestCase(0, NowLengthDiffersFromThen, TestName = "A 90-day Then asking for a Now of 0 days is refused because Now is always as long as Then")]
        [TestCase(14, NowLengthDiffersFromThen, TestName = "A 90-day Then asking for the shortest Now of 14 days is refused because Now is always as long as Then")]
        [TestCase(90, null, TestName = "A 90-day Then asking for a 90-day Now is created")]
        [TestCase(null, null, TestName = "A 90-day Then asking for no Now length is created")]
        public async Task A_Now_length_is_accepted_only_when_absent_or_equal_to_Thens(int? nowLengthDays, string? expectedRefusal)
        {
            var lightspeed = ATeam("Lightspeed");

            var outcome = await CreateAReport(lightspeed, ThenOf(Today, 90, nowLengthDays));

            if (expectedRefusal is null)
            {
                TheReportReadsWindows(lightspeed, outcome,
                    then: new ReportWindow(new DateOnly(2026, 7, 5), Today, 90),
                    now: new ReportWindow(new DateOnly(2026, 7, 5), Today, 90));
                return;
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.Status, Is.EqualTo(ReportOutcomeStatus.Refused),
                    $"WHAT: a create naming a {nowLengthDays}-day Now for a 90-day Then was {outcome.Status}. WHY: Now is always as long as Then, so another length must be refused. HOW: refuse with {expectedRefusal} before anything is frozen.");
                Assert.That(outcome.Code, Is.EqualTo(expectedRefusal),
                    $"WHAT: the refusal code was {outcome.Code}. WHY: clients word an unequal Now by this code. HOW: answer {expectedRefusal}.");
                Assert.That(TheReportsOf(lightspeed), Is.Empty,
                    "WHAT: a refused create left a report behind. WHY: a refusal writes nothing. HOW: refuse before the report is stored.");
            }
        }

        [Test]
        public async Task Reports_created_for_several_Teams_at_once_each_read_a_Now_as_long_as_Then_ending_today()
        {
            ReportOwner[] teams = [ATeam("Zenith"), ATeam("Gravity"), ATeam("Pulsar")];
            var writeAccess = new Mock<IReportOwnerWriteAccess>();
            writeAccess.Setup(access => access.MayWriteAsync(It.IsAny<ReportOwner>())).ReturnsAsync(true);

            IReadOnlyList<ReportOwnerOutcome> outcomes;
            using (var scope = harness.NewScope())
            {
                outcomes = await scope.ServiceProvider.GetRequiredService<IReportBulkCommands>()
                    .CreateForEach(teams, ThenOf(Today.AddDays(-30), 30, nowLengthDays: null), writeAccess.Object);
            }

            Assert.That(outcomes.Select(outcome => outcome.Status), Is.All.EqualTo(ReportCreationStatus.Created),
                "WHAT: not every Team got its report. WHY: each Team keeps a year of history, enough for a 30-day Then and Now. HOW: create for each Team.");
            foreach (var (team, outcome) in teams.Zip(outcomes))
            {
                var read = Read(team, outcome.ReportId!.Value);
                Assert.That(read.NowWindow, Is.EqualTo(new ReportWindow(new DateOnly(2026, 9, 3), Today, 30)),
                    $"WHAT: Team {team.Id}'s Now read {read.NowWindow}. WHY: Now is always as long as Then and ends today. HOW: derive each owner's Now from its Then's length.");
            }
        }

        /// <summary>
        /// The law that a Then accepted at create has a Now of its length that starts on or after the oldest kept day (L2),
        /// over seeded Thens of 14 to 400 days ending up to 800 days back, for Teams keeping 1 to 900 days or every item.
        /// </summary>
        [Test]
        public async Task A_Then_accepted_at_create_has_a_Now_of_its_length_inside_the_kept_history()
        {
            const int seed = 6625;
            var random = new Random(seed);
            var cases = new List<(int Length, int DaysBack, int KeepsDays)>
            {
                (30, 0, 365),
                (90, 0, 0),
            };
            for (var i = 0; i < 24; i++)
            {
                cases.Add((random.Next(14, 401), random.Next(0, 801), random.Next(0, 901)));
            }

            foreach (var (length, daysBack, keepsDays) in cases.OrderBy(c => c.Length + c.DaysBack))
            {
                var team = ATeam($"Team {Guid.NewGuid():N}", keepsDays);
                var earliestStart = TheOldestKeptDayOf(team);
                await TheLawHoldsFor(team, earliestStart, Today.AddDays(-daysBack), length, $"seed {seed}, Then {length} days ending {daysBack} days back, kept {keepsDays} days");
            }

            var startingOnTheOldestKeptDay = ATeam("Kept 120 days", 120);
            var oldest = TheOldestKeptDayOf(startingOnTheOldestKeptDay)!.Value;
            await TheLawHoldsFor(startingOnTheOldestKeptDay, oldest, oldest.AddDays(59), 60, "Then starting exactly on the oldest kept day");
        }

        private async Task TheLawHoldsFor(ReportOwner team, DateOnly? earliestStart, DateOnly thenEnd, int length, string replay)
        {
            var thenStart = thenEnd.AddDays(-(length - 1));
            var outcome = await CreateAReport(team, ThenOf(thenEnd, length, nowLengthDays: null));

            if (earliestStart is not null && thenStart < earliestStart)
            {
                Assert.That(outcome.Code, Is.EqualTo(ReportRefusalCodes.ThenBeforeCutoff),
                    $"WHAT: a Then starting {thenStart} before the oldest kept day {earliestStart} was not refused ({replay}). WHY: Then must lie in the kept history. HOW: refuse then-before-cutoff.");
                return;
            }

            Assert.That(outcome.Status, Is.EqualTo(ReportOutcomeStatus.Done),
                $"WHAT: a Then inside the kept history was {outcome.Status} {outcome.Code} ({replay}). WHY: a valid Then always leaves room for a Now of its length. HOW: accept it.");
            var now = Read(team, outcome.Value!.Id).NowWindow;
            var nowStart = Today.AddDays(-(length - 1));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(now, Is.EqualTo(new ReportWindow(nowStart, Today, length)),
                    $"WHAT: Now read {now} ({replay}). WHY: Now is as long as Then and ends today. HOW: build Now from Then's length.");
                Assert.That(earliestStart is null || now.Start >= earliestStart, Is.True,
                    $"WHAT: Now starts {now.Start} before the oldest kept day {earliestStart} ({replay}). WHY: Now must lie in the kept history. HOW: a Then that fits leaves a Now that fits.");
            }
        }

        private static NewReport ThenOf(DateOnly thenEnd, int thenLengthDays, int? nowLengthDays) =>
            new(ThenAndNowTemplate.TemplateKey, "Engagement start", CycleTimeShown, new NewThenAndNow(thenEnd, thenLengthDays, nowLengthDays));

        private void TheReportReadsWindows(ReportOwner owner, ReportOutcome<ReportRead> created, ReportWindow then, ReportWindow now)
        {
            Assert.That(created.Status, Is.EqualTo(ReportOutcomeStatus.Done),
                $"WHAT: the create was {created.Status} {created.Code}. WHY: an absent or equal Now is accepted. HOW: create the report.");
            var read = Read(owner, created.Value!.Id);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.ThenWindow, Is.EqualTo(then), "WHAT: the Then read back differs from the Then asked for. WHY: Then is frozen as asked. HOW: freeze the requested Then.");
                Assert.That(read.NowWindow, Is.EqualTo(now), "WHAT: Now is not as long as Then or does not end today. WHY: Now is always as long as Then. HOW: derive Now from Then's length.");
            }
        }

        private ThenAndNowRead Read(ReportOwner owner, int reportId)
        {
            using var scope = harness.NewScope();
            return scope.ServiceProvider.GetRequiredService<IReportQueries>().Get(owner, reportId).Value!.ThenAndNow!;
        }

        private IReadOnlyList<ReportSummary> TheReportsOf(ReportOwner owner)
        {
            using var scope = harness.NewScope();
            return scope.ServiceProvider.GetRequiredService<IReportQueries>().List(owner).Value!.Reports;
        }

        private DateOnly? TheOldestKeptDayOf(ReportOwner owner)
        {
            using var scope = harness.NewScope();
            return scope.ServiceProvider.GetRequiredService<IReportQueries>().Templates(owner).Value!.EarliestStart;
        }

        private async Task<ReportOutcome<ReportRead>> CreateAReport(ReportOwner owner, NewReport request)
        {
            using var scope = harness.NewScope();
            return await scope.ServiceProvider.GetRequiredService<IReportCommands>().Create(owner, request);
        }

        /// <summary>A Team is stored that keeps its finished Work Items for this many days, or for ever when it is 0.</summary>
        private ReportOwner ATeam(string name, int keepsFinishedItemsForDays = 365)
        {
            using var scope = harness.NewScope();
            var team = new Team
            {
                Name = name,
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Name = $"Connection {Guid.NewGuid():N}",
                    WorkTrackingSystem = WorkTrackingSystems.Jira,
                },
                DataRetrievalValue = "project = LIGHTSPEED",
                WorkItemTypes = ["User Story", "Bug"],
                ToDoStates = ["Backlog"],
                DoingStates = ["In Progress"],
                DoneStates = ["Done"],
                DoneItemsCutoffDays = keepsFinishedItemsForDays,
            };
            var teams = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();
            teams.Add(team);
            teams.Save().GetAwaiter().GetResult();
            return ReportOwner.Team(team.Id);
        }
    }
}
