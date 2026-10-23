using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Models.Validation;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.ConnectionHealth;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.TeamData;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices.Update
{
    public class TeamUpdaterTest : UpdateServiceTestBase
    {
        private const string ConnectionName = "Company Jira";

        private const string SecretFieldKey = "Personal Access Token";

        private const string UnreadableValue = "unreadable-stored-value";

        private const string RefusalSentence = "Field 'sprnt' does not exist or you do not have permission to view it.";

        private const string RefusedQuery = "project = PROJ AND sprnt is not EMPTY";

        private const string RefusedTeamName = "Payments";

        private static readonly RefreshFinding ARelationTypeAsParentOverride = new(
            RefreshFindingKind.RelationTypeCannotBeReadHere, RefreshFindingSetting.ParentOverrideField, "Contoso.DependsOn-Forward", []);

        private static readonly RefreshFinding[] TheFindingTheRefreshMade = [ARelationTypeAsParentOverride];

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
            var team = CreateTeam(Now.AddDays(-1));
            SetupTeams([team]);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            await WaitUntilVerified(() => teamDataServiceMock.Verify(x => x.UpdateTeamData(team), Times.Once));
        }

        [Test]
        public async Task ExecuteAsync_MultipleTeams_RefreshesAllTeams()
        {
            var team1 = CreateTeam(Now.AddDays(-1));
            var team2 = CreateTeam(Now.AddDays(-1));
            SetupTeams([team1, team2]);
            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            await WaitUntilVerified(() => teamDataServiceMock.Verify(x => x.UpdateTeamData(team1), Times.Once));
            await WaitUntilVerified(() => teamDataServiceMock.Verify(x => x.UpdateTeamData(team2), Times.Once));
        }

        [Test]
        public async Task ExecuteAsync_MultipleTeams_RefreshesOnlyTeamsWhereLastRefreshIsOlderThanConfiguredSetting()
        {
            var team1 = CreateTeam(Now.AddDays(-1));
            var team2 = CreateTeam(Now);

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
            var team = CreateTeam(Now.AddDays(-1));

            SetupRefreshSettings(10, 360);

            SetupTeams([team, CreateTeam(Now), CreateTeam(Now), CreateTeam(Now)]);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            teamDataServiceMock.Verify(x => x.UpdateTeamData(team), Times.Never);
        }

        [Test]
        public async Task ExecuteAsync_ShouldBeRefreshed_PremiumLicense_MoreThanThreeTeams_Refreshes()
        {
            var team = CreateTeam(Now.AddDays(-1));
            SetupRefreshSettings(10, 360);
            SetupTeams([team, CreateTeam(Now), CreateTeam(Now), CreateTeam(Now)]);

            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            await WaitUntilVerified(() => teamDataServiceMock.Verify(x => x.UpdateTeamData(team), Times.Once));
        }

        [Test]
        public async Task ExecuteAsync_ReadyToRefresh_TriggersWriteBackForTeam()
        {
            var team = CreateTeam(Now.AddDays(-1));
            SetupTeams([team]);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            await WaitUntilVerified(() => writeBackTriggerServiceMock.Verify(x => x.ResolveWriteBackForTeam(team), Times.Once));
        }

        [Test]
        public void TriggerUpdate_RecordsTheRefreshAtTheClocksInstant()
        {
            var team = CreateTeam(Now.AddDays(-1));
            SetupTeams([team]);

            CreateSubject().TriggerUpdate(team.Id);

            Assert.That(recordedRefresh?.ExecutedAt, Is.EqualTo(Now));
        }

        [Test]
        public async Task ExecuteAsync_TeamLastRefreshedExactlyTheConfiguredMinutesAgo_IsRefreshed()
        {
            SetupRefreshSettings(10, 360);
            var team = CreateTeam(Now.AddMinutes(-360));
            SetupTeams([team]);

            await CreateSubject().StartAsync(CancellationToken.None);

            await WaitUntilVerified(() => teamDataServiceMock.Verify(x => x.UpdateTeamData(team), Times.Once));
        }

        [Test]
        public void TriggerUpdate_NoPremiumLicense_ThreeTeams_StillRefreshes()
        {
            var team = CreateTeam(Now.AddDays(-1));
            SetupTeams([team, CreateTeam(Now), CreateTeam(Now)]);

            CreateSubject().TriggerUpdate(team.Id);

            teamDataServiceMock.Verify(x => x.UpdateTeamData(team), Times.Once);
        }

        [Test]
        public void TriggerUpdate_NoPremiumLicense_MoreThanThreeTeams_RefreshesNothingAndReportsWhy()
        {
            var team = CreateTeam(Now.AddDays(-1));
            SetupTeams([team, CreateTeam(Now), CreateTeam(Now), CreateTeam(Now)]);

            CreateSubject().TriggerUpdate(team.Id);

            using (Assert.EnterMultipleScope())
            {
                teamDataServiceMock.Verify(x => x.UpdateTeamData(It.IsAny<Team>()), Times.Never);
                Assert.That(recordedRefresh, Is.Null);
                Assert.That(ReadErrors(), Has.Count.EqualTo(1));
            }
        }

        [Test]
        public void TriggerUpdate_TeamNoLongerExists_DoesNothingAndDoesNotFail()
        {
            SetupTeams([CreateTeam(Now)]);

            CreateSubject().TriggerUpdate(9999);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(WhatTheRefreshThrew, Is.Null);
                Assert.That(recordedRefresh, Is.Null);
            }
        }

        [Test]
        public void TriggerUpdate_RefreshSucceeds_TellsConnectionHealthItSucceeded()
        {
            var health = RegisterConnectionHealth();
            var team = CreateTeam(Now.AddDays(-1));
            SetupTeams([team]);

            CreateSubject().TriggerUpdate(team.Id);

            using (Assert.EnterMultipleScope())
            {
                health.Verify(x => x.RecordRefreshSucceededAsync(team.WorkTrackingSystemConnection), Times.Once);
                health.Verify(x => x.RecordRefreshFailedAsync(It.IsAny<WorkTrackingSystemConnection>()), Times.Never);
            }
        }

        [Test]
        public void TriggerUpdate_RefreshFails_TellsConnectionHealthItFailed()
        {
            var health = RegisterConnectionHealth();
            var team = CreateTeam(Now.AddDays(-1));
            SetupTeams([team]);
            teamDataServiceMock.Setup(x => x.UpdateTeamData(team)).ThrowsAsync(new InvalidOperationException("boom"));

            CreateSubject().TriggerUpdate(team.Id);

            using (Assert.EnterMultipleScope())
            {
                health.Verify(x => x.RecordRefreshFailedAsync(team.WorkTrackingSystemConnection), Times.Once);
                health.Verify(x => x.RecordRefreshSucceededAsync(It.IsAny<WorkTrackingSystemConnection>()), Times.Never);
            }
        }

        [Test]
        public void TriggerUpdate_Cancelled_TellsConnectionHealthNothing()
        {
            var health = RegisterConnectionHealth();
            var team = CreateTeam(Now.AddDays(-1));
            SetupTeams([team]);
            teamDataServiceMock.Setup(x => x.UpdateTeamData(team)).ThrowsAsync(new OperationCanceledException());

            CreateSubject().TriggerUpdate(team.Id);

            using (Assert.EnterMultipleScope())
            {
                health.Verify(x => x.RecordRefreshFailedAsync(It.IsAny<WorkTrackingSystemConnection>()), Times.Never);
                health.Verify(x => x.RecordRefreshSucceededAsync(It.IsAny<WorkTrackingSystemConnection>()), Times.Never);
            }
        }

        private Mock<IConnectionHealthService> RegisterConnectionHealth()
        {
            var health = new Mock<IConnectionHealthService>();
            health.Setup(x => x.RecordRefreshSucceededAsync(It.IsAny<WorkTrackingSystemConnection>())).Returns(Task.CompletedTask);
            health.Setup(x => x.RecordRefreshFailedAsync(It.IsAny<WorkTrackingSystemConnection>())).Returns(Task.CompletedTask);
            SetupServiceProviderMock(health.Object);
            return health;
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
            var team = CreateTeam(Now.AddDays(-1));
            SetupTeams([team]);

            WriteBackCollectorMock
                .Setup(c => c.FlushAsync())
                .ThrowsAsync(new InvalidOperationException("The tracker is unreachable"));

            var subject = CreateSubject();

            Assert.DoesNotThrow(() => subject.TriggerUpdate(team.Id));

            var errors = loggerMock.Invocations
                .Where(i => i.Arguments[0] is LogLevel.Error)
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
            var team = CreateTeam(Now.AddDays(-1));
            SetupTeams([team]);

            CreateSubject().TriggerUpdate(team.Id);

            var summary = ReadUpdateSummary();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(recordedRefresh?.Success, Is.True);

                Assert.That(recordedRefresh?.Cancelled, Is.False,
                    "Nobody stopped this one. Marked cancelled, every healthy refresh would be excluded "
                    + "from the success rate and the panel would report nothing at all.");
                Assert.That(summary, Does.Not.Contain("reason="),
                    "A healthy refresh has nothing to explain, and a reason on every line is what made these logs unreadable.");
                Assert.That(summary, Does.Not.Contain("read").IgnoreCase);
                Assert.That(summary, Does.Not.Contain("encryption").IgnoreCase);
                Assert.That(summary, Does.Not.Contain("credential").IgnoreCase);
            }
        }

        [Test]
        public void TriggerUpdate_Cancelled_RecordsTheRefreshAsCancelledRatherThanFailed()
        {
            var team = CreateTeam(Now.AddDays(-1));
            SetupTeams([team]);
            teamDataServiceMock
                .Setup(x => x.UpdateTeamData(team))
                .ThrowsAsync(new OperationCanceledException());

            CreateSubject().TriggerUpdate(team.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(recordedRefresh?.Cancelled, Is.True,
                    "An operator pressed stop. Refresh history showing a red row for that sends somebody "
                    + "looking for a broken connection that was working perfectly.");

                Assert.That(recordedRefresh?.Success, Is.False,
                    "It is still not a refresh that finished - the work it was asked to do did not happen, "
                    + "and a run counted as successful would inflate the success rate with runs nobody ran.");

                Assert.That(WhatTheRefreshThrew, Is.InstanceOf<OperationCanceledException>(),
                    "Recording the cancel is half of it. Swallowed here the refresh returns as though it "
                    + "finished, and the queue tells the browser the work completed.");
            }
        }

        [Test]
        public void TriggerUpdate_WorkTrackingSystemUnreachable_BlamesTheWorkTrackingSystemAndSaysNothingAboutACredential()
        {
            var team = CreateTeam(Now.AddDays(-1));
            SetupTeams([team]);
            teamDataServiceMock
                .Setup(x => x.UpdateTeamData(team))
                .ThrowsAsync(new HttpRequestException("The work tracking system is unreachable"));

            CreateSubject().TriggerUpdate(team.Id);

            var summary = ReadUpdateSummary();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(recordedRefresh?.Success, Is.False);
                Assert.That(WhatTheRefreshThrew?.Message, Does.Contain("The work tracking system is unreachable"),
                    "An outage has always been reported as an outage. It now travels out to the queue rather than "
                    + "stopping inside the refresh, so the browser is told the refresh failed instead of being told it completed.");
                Assert.That(summary, Does.Not.Contain("cannot be read").IgnoreCase,
                    "Blaming a credential for an outage sends the operator to rotate a key that was never broken.");
                Assert.That(summary, Does.Not.Contain("credential").IgnoreCase);
            }
        }

        [Test]
        public void TriggerUpdate_ConnectorRefusedTheQuery_TheReasonNamesTheTeamTheAnswerAndTheQuery()
        {
            var team = SetupTeamWhoseQueryIsRefused();

            CreateSubject().TriggerUpdate(team.Id);

            var summary = ReadUpdateSummary();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(summary, Does.Contain(team.Name),
                    "An instance refreshing several teams at once writes several of these lines, and a "
                    + "refusal that names none of them cannot be paired with the team that caused it.");
                Assert.That(summary, Does.Contain(RefusalSentence),
                    "What the work tracking system said is the only part of the failure anyone can act on - a "
                    + "status code on its own says nothing about what to change.");
                Assert.That(summary, Does.Contain(RefusedQuery),
                    "Without the query, an operator cannot tell which of the configured filters was refused.");
            }
        }

        [Test]
        public void TriggerUpdate_ConnectorRefusedTheQuery_TheFailureStillPropagatesAndTheRefreshIsUnsuccessful()
        {
            var team = SetupTeamWhoseQueryIsRefused();

            CreateSubject().TriggerUpdate(team.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(WhatTheRefreshThrew, Is.InstanceOf<WorkTrackingRefusedException>(),
                    "Swallowing the failure to attach a reason would leave the refresh looking like it merely "
                    + "returned nothing, and every surface reading the outcome would be told it completed.");
                Assert.That(recordedRefresh?.Success, Is.False,
                    "A refresh the work tracking system refused to run is not a refresh that worked.");
            }
        }

        [Test]
        public void TriggerUpdate_TheRefreshFoundSomethingWrongWithASetting_RecordsTheFindingOnItsRow()
        {
            var team = CreateTeam(Now.AddDays(-1));
            SetupTeams([team]);
            teamDataServiceMock
                .Setup(x => x.UpdateTeamData(team))
                .ReturnsAsync(SyncOutcome.FullSync(2) with { Findings = [ARelationTypeAsParentOverride] });

            CreateSubject().TriggerUpdate(team.Id);

            Assert.That(recordedRefresh?.Findings, Is.EqualTo(TheFindingTheRefreshMade),
                "Refresh History reads the finding off the Team's row, so the row has to carry it.");
        }

        [Test]
        public void TriggerUpdate_ACheapRefresh_LeavesTheFindingsOfItsRowUnsaid()
        {
            var team = CreateTeam(Now.AddDays(-1));
            SetupTeams([team]);
            teamDataServiceMock
                .Setup(x => x.UpdateTeamData(team))
                .ReturnsAsync(SyncOutcome.DeltaSync(10, 2) with { Findings = [ARelationTypeAsParentOverride] });

            CreateSubject().TriggerUpdate(team.Id);

            Assert.That(recordedRefresh?.Findings, Is.Null,
                "A cheap refresh reads only what moved, so it cannot tell a setting is now fine. Anything it said "
                + "would replace the finding the last whole download recorded.");
        }

        [Test]
        public void TriggerUpdate_AWholeDownloadThatFailedPartWay_LeavesTheFindingsOfItsRowUnsaid()
        {
            var team = CreateTeam(Now.AddDays(-1));
            SetupTeams([team]);
            teamDataServiceMock
                .Setup(x => x.UpdateTeamData(team))
                .ReturnsAsync(SyncOutcome.FullSync(2) with { Findings = [ARelationTypeAsParentOverride] });
            writeBackTriggerServiceMock
                .Setup(x => x.ResolveWriteBackForTeam(team))
                .Throws(new InvalidOperationException("the write-back could not be resolved"));

            CreateSubject().TriggerUpdate(team.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(recordedRefresh?.Success, Is.False);
                Assert.That(recordedRefresh?.Findings, Is.Null,
                    "A refresh that did not finish has not judged the configuration, so it must not replace "
                    + "what the last one that did finish said.");
            }
        }

        [Test]
        public void TriggerUpdate_ConnectorCouldNotReadWhatTheRefreshNeeds_TheFailureStillStopsTheRefresh()
        {
            var team = CreateTeam(Now.AddDays(-1));
            SetupTeams([team]);
            teamDataServiceMock
                .Setup(x => x.UpdateTeamData(team))
                .ThrowsAsync(new AzureDevOpsReadException(ConnectionValidationResult.Failure("relation_types_unreadable", "The relation types could not be listed.")));

            CreateSubject().TriggerUpdate(team.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(WhatTheRefreshThrew, Is.InstanceOf<WorkTrackingReadException>(),
                    "Attaching a reason must not swallow the failure, or the refresh reads as one that worked.");
                Assert.That(recordedRefresh?.Success, Is.False);
            }
        }

        private Team SetupTeamWhoseQueryIsRefused()
        {
            var team = CreateTeam(Now.AddDays(-1));
            team.Name = RefusedTeamName;

            SetupTeams([team]);

            teamDataServiceMock
                .Setup(x => x.UpdateTeamData(team))
                .ThrowsAsync(new WorkTrackingRefusedException(RefusalSentence, RefusedQuery, HttpStatusCode.BadRequest));

            return team;
        }

        private Team SetupTeamWhoseCredentialCannotBeRead()
        {
            var team = CreateTeam(Now.AddDays(-1));
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
                .Where(i => i.Arguments[0] is LogLevel.Information)
                .Select(i => i.Arguments[2]?.ToString() ?? string.Empty)
                .SingleOrDefault(message => message.Contains("Update completed", StringComparison.Ordinal));

            Assert.That(summary, Is.Not.Null, "The summary line is the record an operator reads for a finished refresh.");

            return summary!;
        }

        private List<string> ReadErrors()
        {
            return loggerMock.Invocations
                .Where(i => i.Arguments[0] is LogLevel.Error)
                .Select(i => i.Arguments[2]?.ToString() ?? string.Empty)
                .ToList();
        }

        private TeamUpdater CreateSubject()
        {
            return new TeamUpdater(loggerMock.Object, ServiceScopeFactory, UpdateQueueService, Time);
        }
    }
}
