using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.TeamData;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices.Update
{
    public class TeamUpdaterTest : UpdateServiceTestBase
    {
        private const string ConnectionName = "Company Jira";

        private const string SecretFieldKey = "Personal Access Token";

        private const string UnreadableValue = "unreadable-stored-value";

        private Mock<IAppSettingService> appSettingServiceMock;
        private Mock<IRepository<Team>> teamRepoMock;
        private Mock<ITeamDataService> teamDataServiceMock;
        private Mock<ILicenseService> licenseServiceMock;
        private Mock<IWriteBackTriggerService> writeBackTriggerServiceMock;
        private Mock<IRefreshLogService> refreshLogServiceMock;
        private Mock<ILogger<TeamUpdater>> loggerMock;
        private Mock<ICryptoService> cryptoServiceMock;

        private RefreshLog? recordedRefresh;

        private int idCounter = 0;

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
            cryptoServiceMock = new Mock<ICryptoService>();

            // A crypto double with no Read set up hands back null, and null is not what the port promises -
            // the code under test would then be exercised against a shape production can never produce.
            cryptoServiceMock
                .Setup(x => x.Read(It.IsAny<string>()))
                .Returns((string storedValue) => new SecretReadResult(SecretState.Envelope, storedValue, "current"));

            recordedRefresh = null;
            refreshLogServiceMock
                .Setup(x => x.LogRefreshAsync(It.IsAny<RefreshLog>()))
                .Callback((RefreshLog entry) => recordedRefresh = entry)
                .Returns(Task.CompletedTask);

            // NUnit reuses one fixture instance for the whole class, and the write-back collector double is
            // built in the base constructor - so without this it still carries every call the previous tests
            // made, and a "this never happened" assertion can never fail.
            WriteBackCollectorMock.Invocations.Clear();

            SetupServiceProviderMock(cryptoServiceMock.Object);
            SetupServiceProviderMock(teamRepoMock.Object);
            SetupServiceProviderMock(appSettingServiceMock.Object);
            SetupServiceProviderMock(licenseServiceMock.Object);
            SetupServiceProviderMock(teamDataServiceMock.Object);
            SetupServiceProviderMock(writeBackTriggerServiceMock.Object);
            SetupServiceProviderMock(refreshLogServiceMock.Object);

            // Epic #5687: without this the mock hands back a null outcome and the refresh log write throws.
            teamDataServiceMock
                .Setup(x => x.UpdateTeamData(It.IsAny<Team>()))
                .ReturnsAsync(SyncOutcome.None);

            SetupRefreshSettings(10, 10);
        }

        [Test]
        public async Task ExecuteAsync_ReadyToRefresh_RefreshesAllTeams()
        {
            var team = CreateTeam(DateTime.Now.AddDays(-1));
            SetupTeams([team]);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            await WaitUntilVerified(() => teamDataServiceMock.Verify(x => x.UpdateTeamData(team), Times.Once));
        }

        [Test]
        public async Task ExecuteAsync_MultipleTeams_RefreshesAllTeams()
        {
            var team1 = CreateTeam(DateTime.Now.AddDays(-1));
            var team2 = CreateTeam(DateTime.Now.AddDays(-1));
            SetupTeams([team1, team2]);
            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            await WaitUntilVerified(() => teamDataServiceMock.Verify(x => x.UpdateTeamData(team1), Times.Once));
            await WaitUntilVerified(() => teamDataServiceMock.Verify(x => x.UpdateTeamData(team2), Times.Once));
        }

        [Test]
        public async Task ExecuteAsync_MultipleTeams_RefreshesOnlyTeamsWhereLastRefreshIsOlderThanConfiguredSetting()
        {
            var team1 = CreateTeam(DateTime.Now.AddDays(-1));
            var team2 = CreateTeam(DateTime.Now);

            SetupRefreshSettings(10, 360);

            SetupTeams([team1, team2]);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            await WaitUntilVerified(() => teamDataServiceMock.Verify(x => x.UpdateTeamData(team1), Times.Once));
            teamDataServiceMock.Verify(x => x.UpdateTeamData(team2), Times.Never);
        }

        [Test]
        public async Task ExecuteAsync_ShouldBeRefreshed_NoPremiumLicense_MoreThanThreeTeams_DoesNotRefresh()
        {
            var team = CreateTeam(DateTime.Now.AddDays(-1));

            SetupRefreshSettings(10, 360);

            SetupTeams([team, CreateTeam(DateTime.Now), CreateTeam(DateTime.Now), CreateTeam(DateTime.Now)]);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            teamDataServiceMock.Verify(x => x.UpdateTeamData(team), Times.Never);
        }

        [Test]
        public async Task ExecuteAsync_ShouldBeRefreshed_PremiumLicense_MoreThanThreeTeams_Refreshes()
        {
            var team = CreateTeam(DateTime.Now.AddDays(-1));
            SetupRefreshSettings(10, 360);
            SetupTeams([team, CreateTeam(DateTime.Now), CreateTeam(DateTime.Now), CreateTeam(DateTime.Now)]);

            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            await WaitUntilVerified(() => teamDataServiceMock.Verify(x => x.UpdateTeamData(team), Times.Once));
        }

        [Test]
        public async Task ExecuteAsync_ReadyToRefresh_TriggersWriteBackForTeam()
        {
            var team = CreateTeam(DateTime.Now.AddDays(-1));
            SetupTeams([team]);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            await WaitUntilVerified(() => writeBackTriggerServiceMock.Verify(x => x.ResolveWriteBackForTeam(team), Times.Once));
        }

        private void SetupRefreshSettings(int interval, int refreshAfter)
        {
            var refreshSettings = new RefreshSettings { Interval = interval, RefreshAfter = refreshAfter, StartDelay = 0 };
            appSettingServiceMock.Setup(x => x.GetTeamDataRefreshSettings()).Returns(refreshSettings);
        }

        private void SetupTeams(IEnumerable<Team> teams)
        {
            teamRepoMock.Setup(x => x.GetAll()).Returns(teams);

            foreach (var team in teams)
            {
                teamRepoMock.Setup(x => x.GetById(team.Id)).Returns(team);
            }
        }
        private Team CreateTeam(DateTime lastThroughputUpdateTime)
        {
            return new Team
            {
                Id = idCounter++,
                Name = "Team",
                ThroughputHistory = 7,
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    WorkTrackingSystem = WorkTrackingSystems.AzureDevOps
                },
                UpdateTime = lastThroughputUpdateTime
            };
        }

        [Test]
        public void TriggerUpdate_TheWriteBackFlushThrows_LogsItAndStillFinishes()
        {
            var team = CreateTeam(DateTime.Now.AddDays(-1));
            SetupTeams([team]);

            WriteBackCollectorMock
                .Setup(c => c.FlushAsync())
                .ThrowsAsync(new InvalidOperationException("The tracker is unreachable"));

            var subject = CreateSubject();

            Assert.DoesNotThrow(() => subject.TriggerUpdate(team.Id));

            var errors = loggerMock.Invocations
                .Where(i => (LogLevel)i.Arguments[0] == LogLevel.Error)
                .Select(i => i.Arguments[2]?.ToString() ?? string.Empty);

            Assert.That(errors, Has.One.Contains("Write-back flush failed"),
                "The flush is the last thing an update does; a failure there must be visible and must not fail the round.");
        }

        [Test]
        public void TriggerUpdate_CredentialCannotBeRead_RecordsTheRefreshAsUnsuccessful()
        {
            var team = SetupTeamWhoseCredentialCannotBeRead();

            CreateSubject().TriggerUpdate(team.Id);

            Assert.That(recordedRefresh?.Success, Is.False,
                "A refresh that never reached the work tracking system is not a refresh that worked.");
        }

        [Test]
        public void TriggerUpdate_CredentialCannotBeRead_TheReasonNamesTheConnectionAndTheField()
        {
            var team = SetupTeamWhoseCredentialCannotBeRead();

            CreateSubject().TriggerUpdate(team.Id);

            var summary = ReadUpdateSummary();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(summary, Does.Contain(ConnectionName),
                    "An operator running several connections needs the reason to say which one to open.");
                Assert.That(summary, Does.Contain(SecretFieldKey),
                    "Without the field, the operator re-enters credentials until the refresh stops failing.");
            }
        }

        [Test]
        public void TriggerUpdate_CredentialCannotBeRead_TheReasonSaysItCouldNotBeRead_NotThatItWasRejected()
        {
            var team = SetupTeamWhoseCredentialCannotBeRead();

            CreateSubject().TriggerUpdate(team.Id);

            var summary = ReadUpdateSummary();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(summary, Does.Contain("cannot be read").IgnoreCase);
                Assert.That(summary, Does.Not.Contain("reject").IgnoreCase);
                Assert.That(summary, Does.Not.Contain("refus").IgnoreCase);
                Assert.That(summary, Does.Not.Contain("invalid").IgnoreCase);
                Assert.That(summary, Does.Not.Contain("expired").IgnoreCase,
                    "Rejection wording is what sent operators to reissue a token the work tracking system never saw.");
            }
        }

        [Test]
        public void TriggerUpdate_CredentialCannotBeRead_NothingIsSentToTheWorkTrackingSystemForThatConnection()
        {
            var team = SetupTeamWhoseCredentialCannotBeRead();

            CreateSubject().TriggerUpdate(team.Id);

            using (Assert.EnterMultipleScope())
            {
                WriteBackCollectorMock.Verify(
                    c => c.Stage(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()),
                    Times.Never,
                    "Staging a write-back would carry the same unreadable credential out to the work tracking system on the flush.");
                writeBackTriggerServiceMock.Verify(x => x.ResolveWriteBackForTeam(It.IsAny<Team>()), Times.Never);
            }
        }

        [Test]
        public void TriggerUpdate_EveryCredentialReads_RecordsSuccessAndSaysNothingAboutEncryption()
        {
            var team = CreateTeam(DateTime.Now.AddDays(-1));
            SetupTeams([team]);

            CreateSubject().TriggerUpdate(team.Id);

            var summary = ReadUpdateSummary();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(recordedRefresh?.Success, Is.True);
                Assert.That(summary, Does.Not.Contain("reason="),
                    "A healthy refresh has nothing to explain, and a reason on every line is what made these logs unreadable.");
                Assert.That(summary, Does.Not.Contain("read").IgnoreCase);
                Assert.That(summary, Does.Not.Contain("encryption").IgnoreCase);
                Assert.That(summary, Does.Not.Contain("credential").IgnoreCase);
            }
        }

        [Test]
        public void TriggerUpdate_WorkTrackingSystemUnreachable_BlamesTheWorkTrackingSystemAndSaysNothingAboutACredential()
        {
            var team = CreateTeam(DateTime.Now.AddDays(-1));
            SetupTeams([team]);
            teamDataServiceMock
                .Setup(x => x.UpdateTeamData(team))
                .ThrowsAsync(new HttpRequestException("The work tracking system is unreachable"));

            CreateSubject().TriggerUpdate(team.Id);

            var summary = ReadUpdateSummary();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(recordedRefresh?.Success, Is.False);
                Assert.That(ReadErrors(), Has.One.Contains("The work tracking system is unreachable"),
                    "An outage has always been reported as an outage, and this step must not have changed that.");
                Assert.That(summary, Does.Not.Contain("cannot be read").IgnoreCase,
                    "Blaming a credential for an outage sends the operator to rotate a key that was never broken.");
                Assert.That(summary, Does.Not.Contain("credential").IgnoreCase);
            }
        }

        private Team SetupTeamWhoseCredentialCannotBeRead()
        {
            var team = CreateTeam(DateTime.Now.AddDays(-1));
            team.WorkTrackingSystemConnection.Name = ConnectionName;
            team.WorkTrackingSystemConnection.Options.Add(new WorkTrackingSystemConnectionOption
            {
                Key = SecretFieldKey,
                Value = UnreadableValue,
                IsSecret = true,
            });

            SetupTeams([team]);

            cryptoServiceMock
                .Setup(x => x.Read(UnreadableValue))
                .Returns(new SecretReadResult(SecretState.Unreadable, null, "retired"));

            teamDataServiceMock
                .Setup(x => x.UpdateTeamData(team))
                .ThrowsAsync(new UnreadableSecretException(SecretState.Unreadable, "retired"));

            return team;
        }

        private string ReadUpdateSummary()
        {
            var summary = loggerMock.Invocations
                .Where(i => (LogLevel)i.Arguments[0] == LogLevel.Information)
                .Select(i => i.Arguments[2]?.ToString() ?? string.Empty)
                .SingleOrDefault(message => message.Contains("Update completed", StringComparison.Ordinal));

            Assert.That(summary, Is.Not.Null, "The summary line is the record an operator reads for a finished refresh.");

            return summary!;
        }

        private List<string> ReadErrors()
        {
            return loggerMock.Invocations
                .Where(i => (LogLevel)i.Arguments[0] == LogLevel.Error)
                .Select(i => i.Arguments[2]?.ToString() ?? string.Empty)
                .ToList();
        }

        private TeamUpdater CreateSubject()
        {
            return new TeamUpdater(loggerMock.Object, ServiceScopeFactory, UpdateQueueService);
        }
    }
}
