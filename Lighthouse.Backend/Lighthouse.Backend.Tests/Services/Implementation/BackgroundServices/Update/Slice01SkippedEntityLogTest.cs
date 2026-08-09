using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.TeamData;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices.Update
{
    /// <summary>
    /// DISTILL acceptance scenarios (Epic 5687 — Faster Updates), slice 01, AC-1.4: a cycle that decides
    /// an entity is not due says nothing about it at Information level, and keeps the check available at
    /// Debug.
    ///
    /// This is the only slice-01 promise about the background loop rather than a single update, and the
    /// loop is what the integration host removes (<c>RemoveAll&lt;IHostedService&gt;</c>) — so it is
    /// driven here, through <c>StartAsync</c>, in the idiom the other updater tests already use.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5687-faster-updates")]
    [Category("slice-01")]
    public class Slice01SkippedEntityLogTest : UpdateServiceTestBase
    {
        private const string TeamName = "Zenith";

        private const string LastUpdateCheck = "Checking last update";

        private Mock<IAppSettingService> appSettingServiceMock;
        private Mock<IRepository<Team>> teamRepoMock;
        private Mock<ITeamDataService> teamDataServiceMock;
        private Mock<ILicenseService> licenseServiceMock;
        private Mock<IWriteBackTriggerService> writeBackTriggerServiceMock;
        private Mock<IRefreshLogService> refreshLogServiceMock;
        private Mock<ILogger<TeamUpdater>> loggerMock;

        [SetUp]
        public void Setup()
        {
            teamRepoMock = new Mock<IRepository<Team>>();
            appSettingServiceMock = new Mock<IAppSettingService>();
            licenseServiceMock = new Mock<ILicenseService>();
            teamDataServiceMock = new Mock<ITeamDataService>();
            writeBackTriggerServiceMock = new Mock<IWriteBackTriggerService>();
            refreshLogServiceMock = new Mock<IRefreshLogService>();
            loggerMock = new Mock<ILogger<TeamUpdater>>();

            SetupServiceProviderMock(teamRepoMock.Object);
            SetupServiceProviderMock(appSettingServiceMock.Object);
            SetupServiceProviderMock(licenseServiceMock.Object);
            SetupServiceProviderMock(teamDataServiceMock.Object);
            SetupServiceProviderMock(writeBackTriggerServiceMock.Object);
            SetupServiceProviderMock(refreshLogServiceMock.Object);
        }

        // @AC-1.4 — a cycle over ten teams that updates none of them should read as one cycle, not ten.
        [Test]
        public async Task A_cycle_that_skips_a_team_says_nothing_to_the_operator_about_that_team()
        {
            GivenATeamThatWasJustRefreshed();

            var subject = await WhenTheBackgroundCycleRuns();

            ThenTheTeamWasNotRefreshed();
            ThenNothingAboutThatTeamReachedTheOperator();

            await subject.StopAsync(CancellationToken.None);
        }

        // @AC-1.4 — demoted, never dropped: the check is still there for whoever is debugging a stuck cycle.
        [Test]
        public async Task The_skipped_check_is_still_available_to_whoever_asks_for_it()
        {
            GivenATeamThatWasJustRefreshed();

            var subject = await WhenTheBackgroundCycleRuns();

            ThenTheCheckIsStillRecordedAtDebug();

            await subject.StopAsync(CancellationToken.None);
        }

        // --- Given ---

        private void GivenATeamThatWasJustRefreshed()
        {
            appSettingServiceMock
                .Setup(x => x.GetTeamDataRefreshSettings())
                .Returns(new RefreshSettings { Interval = 10, RefreshAfter = 360, StartDelay = 0 });

            var team = new Team
            {
                Id = 1,
                Name = TeamName,
                ThroughputHistory = 7,
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.AzureDevOps },
                UpdateTime = DateTime.UtcNow,
            };

            teamRepoMock.Setup(x => x.GetAll()).Returns([team]);
            teamRepoMock.Setup(x => x.GetById(team.Id)).Returns(team);
        }

        // --- When ---

        private async Task<TeamUpdater> WhenTheBackgroundCycleRuns()
        {
            var subject = new TeamUpdater(loggerMock.Object, ServiceScopeFactory, UpdateQueueService);

            await subject.StartAsync(CancellationToken.None);
            await WaitUntilTheCycleHasLookedAtTheTeam();

            return subject;
        }

        /// <summary>
        /// The cycle is asynchronous and every assertion here is a negative one, so it needs a positive
        /// signal that the cycle actually ran — otherwise "nothing was logged" passes before the loop has
        /// started. The wait is level-agnostic on purpose: what level the check arrives at is the thing
        /// under test, and waiting on Debug would turn a red assertion into a fixture timeout.
        /// </summary>
        private async Task WaitUntilTheCycleHasLookedAtTheTeam()
        {
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (!AllLines().Any(line => line.Contains(LastUpdateCheck, StringComparison.OrdinalIgnoreCase)))
            {
                if (DateTime.UtcNow >= deadline)
                {
                    Assert.Fail($"The cycle never recorded '{LastUpdateCheck}' at any level. Lines seen: " + string.Join(" | ", AllLines()));
                }

                await Task.Delay(10);
            }
        }

        // --- Then ---

        private void ThenTheTeamWasNotRefreshed()
            => teamDataServiceMock.Verify(x => x.UpdateTeamData(It.IsAny<Team>()), Times.Never);

        private void ThenNothingAboutThatTeamReachedTheOperator()
        {
            var operatorVisible = LinesAt(LogLevel.Information)
                .Concat(LinesAt(LogLevel.Warning))
                .Concat(LinesAt(LogLevel.Error))
                .Where(line => line.Contains(TeamName, StringComparison.OrdinalIgnoreCase)
                    || line.Contains(LastUpdateCheck, StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.That(operatorVisible, Is.Empty,
                "A cycle that updated nothing should cost the operator no reading. Lines: " + string.Join(" | ", operatorVisible));
        }

        private void ThenTheCheckIsStillRecordedAtDebug()
            => Assert.That(LinesAt(LogLevel.Debug), Has.Some.Contains(LastUpdateCheck),
                "Noise is demoted, never dropped — a stuck cycle is debugged from exactly this line.");

        // --- Reading the log ---

        private IEnumerable<string> LinesAt(LogLevel level)
            => loggerMock.Invocations
                .Where(invocation => invocation.Method.Name == nameof(ILogger.Log) && (LogLevel)invocation.Arguments[0] == level)
                .Select(invocation => invocation.Arguments[2]?.ToString() ?? string.Empty);

        private IEnumerable<string> AllLines()
            => loggerMock.Invocations
                .Where(invocation => invocation.Method.Name == nameof(ILogger.Log))
                .Select(invocation => $"[{invocation.Arguments[0]}] {invocation.Arguments[2]}");
    }
}
