using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DeliverySources;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.TeamData;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices.Update
{
    public class PortfolioUpdaterTest : UpdateServiceTestBase
    {
        private const string ConnectionName = "Company Jira";

        private const string SecretFieldKey = "Personal Access Token";

        private const string UnreadableValue = "unreadable-stored-value";

        private static readonly string[] TheOrderOneRefreshGoesIn = ["fetched", "synced", "saved", "forecast asked for"];

        private Mock<IRepository<Portfolio>> projectRepoMock;
        private Mock<IAppSettingService> appSettingServiceMock;
        private Mock<IWorkItemService> workItemServiceMock;
        private Mock<IForecastService> forecastServiceMock;
        private Mock<IDomainEventDispatcher> domainEventDispatcherMock;
        private Mock<ILicenseService> licenseServiceMock;
        private Mock<IDeliveryRepository> deliveryRepositoryMock;
        private Mock<IDeliveryRuleService> deliveryRuleServiceMock;
        private Mock<IDeliverySourceSyncService> deliverySourceSyncServiceMock;
        private Mock<IWriteBackTriggerService> writeBackTriggerServiceMock;
        private Mock<IRefreshLogService> refreshLogServiceMock;
        private Mock<IOrphanedFeatureCleanupService> cleanupServiceMock;
        private Mock<IForecastUpdater> forecastUpdaterMock;
        private Mock<ICryptoService> cryptoServiceMock;
        private Mock<ILogger<PortfolioUpdater>> loggerMock;

        private Mock<IRepository<Team>> teamRepoMock;
        private Mock<ITeamDataService> teamDataServiceMock;
        private Mock<ILogger<TeamUpdater>> teamLoggerMock;

        private RefreshLog? recordedRefresh;

        private int idCounter;

        [SetUp]
        public void SetUp()
        {
            projectRepoMock = new Mock<IRepository<Portfolio>>();
            appSettingServiceMock = new Mock<IAppSettingService>();
            forecastServiceMock = new Mock<IForecastService>();
            workItemServiceMock = new Mock<IWorkItemService>();
            workItemServiceMock
                .Setup(x => x.UpdateFeaturesForPortfolio(It.IsAny<Portfolio>()))
                .ReturnsAsync(SyncOutcome.None);
            domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();
            domainEventDispatcherMock
                .Setup(x => x.PublishAsync(It.IsAny<PortfolioFeaturesRefreshed>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            domainEventDispatcherMock
                .Setup(x => x.PublishAsync(It.IsAny<PortfolioForecastsUpdated>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            licenseServiceMock = new Mock<ILicenseService>();
            deliveryRepositoryMock = new Mock<IDeliveryRepository>();
            deliveryRuleServiceMock = new Mock<IDeliveryRuleService>();
            deliverySourceSyncServiceMock = new Mock<IDeliverySourceSyncService>();
            writeBackTriggerServiceMock = new Mock<IWriteBackTriggerService>();
            refreshLogServiceMock = new Mock<IRefreshLogService>();
            cleanupServiceMock = new Mock<IOrphanedFeatureCleanupService>();
            forecastUpdaterMock = new Mock<IForecastUpdater>();
            loggerMock = new Mock<ILogger<PortfolioUpdater>>();
            teamLoggerMock = new Mock<ILogger<TeamUpdater>>();
            teamRepoMock = new Mock<IRepository<Team>>();
            teamDataServiceMock = new Mock<ITeamDataService>();
            teamRepoMock.Setup(x => x.GetAll()).Returns([]);

            // A crypto double with no Read set up hands back null, and null is not what the port promises -
            // the code under test would then be exercised against a shape production can never produce.
            cryptoServiceMock = new Mock<ICryptoService>();
            cryptoServiceMock
                .Setup(x => x.Read(It.IsAny<string>()))
                .Returns((string storedValue) => new SecretReadResult(SecretState.Envelope, storedValue, "current"));

            recordedRefresh = null;
            refreshLogServiceMock
                .Setup(x => x.LogRefreshAsync(It.IsAny<RefreshLog>()))
                .Callback((RefreshLog entry) => recordedRefresh = entry)
                .Returns(Task.CompletedTask);

            SetupServiceProviderMock(cryptoServiceMock.Object);
            SetupServiceProviderMock(teamRepoMock.Object);
            SetupServiceProviderMock(teamDataServiceMock.Object);
            SetupServiceProviderMock(projectRepoMock.Object);
            SetupServiceProviderMock(appSettingServiceMock.Object);
            SetupServiceProviderMock(forecastServiceMock.Object);
            SetupServiceProviderMock(workItemServiceMock.Object);
            SetupServiceProviderMock(licenseServiceMock.Object);
            SetupServiceProviderMock(deliveryRepositoryMock.Object);
            SetupServiceProviderMock(deliveryRuleServiceMock.Object);
            SetupServiceProviderMock(deliverySourceSyncServiceMock.Object);
            SetupServiceProviderMock(writeBackTriggerServiceMock.Object);
            SetupServiceProviderMock(refreshLogServiceMock.Object);

            SetupRefreshSettings(10, 10);
        }

        [Test]
        public void UpdateProject_TriggersFeatureUpdateForProject()
        {
            var team = CreateTeam();

            var project = CreateProject(team);
            SetupProjects(project);

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

            workItemServiceMock.Verify(x => x.UpdateFeaturesForPortfolio(project));
        }

        [Test]
        public void UpdateProject_AsksForAReforecastInsteadOfRunningOneItself()
        {
            var team = CreateTeam();

            var project = CreateProject(team);
            SetupProjects(project);

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

            using (Assert.EnterMultipleScope())
            {
                forecastUpdaterMock.Verify(x => x.TriggerUpdate(project.Id), Times.Once);
                forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(It.IsAny<Portfolio>()), Times.Never,
                    "Forecasting inside the feature refresh is invisible to the check that collapses a bulk refresh into one run, so the portfolio ends up forecast several times over.");
            }
        }

        [Test]
        public void UpdateProject_LeavesAnnouncingTheNewDeliveryDateToTheForecastRun()
        {
            var team = CreateTeam();

            var project = CreateProject(team);
            SetupProjects(project);

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

            using (Assert.EnterMultipleScope())
            {
                domainEventDispatcherMock.Verify(
                    x => x.PublishAsync(It.IsAny<PortfolioForecastsUpdated>(), It.IsAny<CancellationToken>()),
                    Times.Never,
                    "Announcing here as well as at the end of the forecast run gives one round two announcements, and everything listening - the delivery snapshot above all - records that round twice.");
                domainEventDispatcherMock.Verify(
                    x => x.PublishAsync(It.Is<PortfolioFeaturesRefreshed>(e => e.PortfolioId == project.Id), It.IsAny<CancellationToken>()),
                    Times.Once,
                    "The features pass still has its own announcement; only the forecast one moved.");
            }
        }

        [Test]
        public async Task ExecuteAsync_ReadyToRefresh_RefreshesAllProjectsAsync()
        {
            var project = CreateProject(DateTime.Now.AddDays(-1));
            SetupProjects(project);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);
            await WaitForEnqueue(project.Id);

            Mock.Get(UpdateQueueService).Verify(x => x.EnqueueUpdate(UpdateType.Features, project.Id, It.IsAny<Func<IServiceProvider, Task>>()));
        }

        [Test]
        public async Task ExecuteAsync_PublishesPortfolioFeaturesRefreshedAfterUpdate()
        {
            var project = CreateProject(DateTime.Now.AddDays(-1));
            SetupProjects(project);
            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            await WaitUntilVerified(() => domainEventDispatcherMock.Verify(
                x => x.PublishAsync(It.Is<PortfolioFeaturesRefreshed>(e => e.PortfolioId == project.Id), It.IsAny<CancellationToken>()),
                Times.AtLeastOnce));
        }

        [Test]
        public async Task ExecuteAsync_MultipleProjects_RefreshesAllProjectsAsync()
        {
            var project1 = CreateProject(DateTime.Now.AddDays(-1));
            var project2 = CreateProject(DateTime.Now.AddDays(-1));
            SetupProjects(project1, project2);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);
            await WaitForEnqueue(project1.Id);
            await WaitForEnqueue(project2.Id);

            Mock.Get(UpdateQueueService).Verify(x => x.EnqueueUpdate(UpdateType.Features, project1.Id, It.IsAny<Func<IServiceProvider, Task>>()));
            Mock.Get(UpdateQueueService).Verify(x => x.EnqueueUpdate(UpdateType.Features, project2.Id, It.IsAny<Func<IServiceProvider, Task>>()));
        }

        [Test]
        public async Task ExecuteAsync_MultipleProjects_RefreshesOnlyProjectsWhereLastRefreshIsOlderThanConfiguredSetting()
        {
            var project1 = CreateProject(DateTime.Now.AddDays(-1));
            var project2 = CreateProject(DateTime.Now);

            SetupRefreshSettings(10, 360);

            SetupProjects(project1, project2);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);
            await WaitForEnqueue(project1.Id);

            Mock.Get(UpdateQueueService).Verify(x => x.EnqueueUpdate(UpdateType.Features, project1.Id, It.IsAny<Func<IServiceProvider, Task>>()));
            Mock.Get(UpdateQueueService).Verify(x => x.EnqueueUpdate(UpdateType.Features, project2.Id, It.IsAny<Func<IServiceProvider, Task>>()), Times.Never);
        }

        [Test]
        public async Task ExecuteAsync_ShouldBeRefreshed_NoPremiumLicense_MoreThanOneProject_DoesNotRefresh()
        {
            var project = CreateProject(DateTime.Now.AddDays(-1));

            SetupRefreshSettings(10, 360);

            SetupProjects(project, CreateProject(DateTime.Now));

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            workItemServiceMock.Verify(x => x.UpdateFeaturesForPortfolio(project), Times.Never);
        }

        [Test]
        public async Task ExecuteAsync_ShouldBeRefreshed_PremiumLicense_MoreThanOneProject_Refreshes()
        {
            var project = CreateProject(DateTime.Now.AddDays(-1));
            SetupRefreshSettings(10, 360);
            SetupProjects(project, CreateProject(DateTime.Now));

            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);

            var subject = CreateSubject();

            await subject.StartAsync(CancellationToken.None);

            await WaitUntilVerified(() => workItemServiceMock.Verify(x => x.UpdateFeaturesForPortfolio(project), Times.Once));
        }

        [Test]
        public void UpdateProject_TriggersDeliveryRuleRecompute()
        {
            var team = CreateTeam();
            var project = CreateProject(team);
            SetupProjects(project);

            var expectedDeliveries = new RecordableDeliveries([]);
            deliveryRepositoryMock.Setup(x => x.GetRecordableByPortfolio(project.Id)).Returns(expectedDeliveries);

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

            deliveryRuleServiceMock.Verify(x => x.RecomputeRuleBasedDeliveries(project, expectedDeliveries), Times.Once);
        }

        [Test]
        public void UpdateProject_AsksEveryBoundDeliveryWhatItsSourceNowSays()
        {
            var team = CreateTeam();
            var project = CreateProject(team);
            SetupProjects(project);

            var theDeliveriesTheRefreshMayWriteTo = new RecordableDeliveries([]);
            deliveryRepositoryMock.Setup(x => x.GetRecordableByPortfolio(project.Id)).Returns(theDeliveriesTheRefreshMayWriteTo);

            CreateSubject().TriggerUpdate(project.Id);

            deliverySourceSyncServiceMock.Verify(
                x => x.ResyncSourceBoundDeliveries(project, theDeliveriesTheRefreshMayWriteTo), Times.Once);
        }

        /// <summary>
        /// Every neighbour of the source sync is load-bearing and each end of it is pinned here.
        ///
        /// It cannot move above the Feature fetch: it narrows what the source says to the Features this
        /// Portfolio tracks, and the fetch is what brings those in - run first, it would narrow against
        /// last refresh's set. It cannot move below the save: the refresh saves Deliveries once, so a
        /// sync after it would hold the new date in memory, write nothing, and be thrown away with the
        /// scope, leaving the screen on the old date. And it has to precede the forecast, because that
        /// is what raises the event the daily snapshot records the target from - after it, the moved
        /// target would not reach the Delivery's history until the following day.
        /// </summary>
        [Test]
        public void UpdateProject_AsksTheSourcesAfterTheFetchTheyNarrowAgainstAndBeforeTheSaveThatKeepsWhatTheySay()
        {
            var team = CreateTeam();
            var project = CreateProject(team);
            SetupProjects(project);

            deliveryRepositoryMock.Setup(x => x.GetRecordableByPortfolio(project.Id)).Returns(new RecordableDeliveries([]));

            var whatHappenedInWhatOrder = new List<string>();
            workItemServiceMock
                .Setup(x => x.UpdateFeaturesForPortfolio(It.IsAny<Portfolio>()))
                .Callback(() => whatHappenedInWhatOrder.Add("fetched"))
                .ReturnsAsync(SyncOutcome.None);
            deliverySourceSyncServiceMock
                .Setup(x => x.ResyncSourceBoundDeliveries(It.IsAny<Portfolio>(), It.IsAny<RecordableDeliveries>()))
                .Callback(() => whatHappenedInWhatOrder.Add("synced"))
                .Returns(Task.CompletedTask);
            deliveryRepositoryMock
                .Setup(x => x.TrySaveRecomputedDeliveries())
                .Callback(() => whatHappenedInWhatOrder.Add("saved"))
                .ReturnsAsync(true);
            forecastUpdaterMock
                .Setup(x => x.TriggerUpdate(It.IsAny<int>()))
                .Callback(() => whatHappenedInWhatOrder.Add("forecast asked for"));

            CreateSubject().TriggerUpdate(project.Id);

            Assert.That(whatHappenedInWhatOrder, Is.EqualTo(TheOrderOneRefreshGoesIn));
        }

        [Test]
        public void UpdateProject_SavesDeliveryChanges()
        {
            var team = CreateTeam();
            var project = CreateProject(team);
            SetupProjects(project);

            deliveryRepositoryMock.Setup(x => x.GetRecordableByPortfolio(project.Id)).Returns(new RecordableDeliveries([]));

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

            deliveryRepositoryMock.Verify(x => x.TrySaveRecomputedDeliveries(), Times.Once);
        }

        [Test]
        public void UpdateProject_ADeliveryWasRetiredWhileTheRefreshWasRunning_LetsTheRefreshFinish()
        {
            var team = CreateTeam();
            var project = CreateProject(team);
            SetupProjects(project);

            deliveryRepositoryMock.Setup(x => x.GetRecordableByPortfolio(project.Id)).Returns(new RecordableDeliveries([]));
            deliveryRepositoryMock.Setup(x => x.TrySaveRecomputedDeliveries()).ReturnsAsync(false);

            CreateSubject().TriggerUpdate(project.Id);

            Assert.That(recordedRefresh?.Success, Is.True,
                "Somebody retiring a Delivery in the middle of a refresh is ordinary. Reporting the whole refresh " +
                "as failed sends an operator looking for a fault that is not there.");

            forecastUpdaterMock.Verify(x => x.TriggerUpdate(project.Id), Times.Once);
        }

        [Test]
        public void UpdateProject_TriggersFeatureWriteBackForPortfolio()
        {
            var team = CreateTeam();

            var project = CreateProject(team);
            SetupProjects(project);

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

            writeBackTriggerServiceMock.Verify(x => x.ResolveFeatureWriteBackForPortfolio(project), Times.Once);
        }

        [Test]
        public void Update_AfterRefreshCompletes_InvokesOrphanedFeatureCleanup()
        {
            var team = CreateTeam();
            var project = CreateProject(team);
            SetupProjects(project);

            var subject = CreateSubject();
            subject.TriggerUpdate(project.Id);

            cleanupServiceMock.Verify(c => c.CleanupAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void Update_WhenRefreshThrows_StillInvokesCleanup()
        {
            var team = CreateTeam();
            var project = CreateProject(team);
            SetupProjects(project);

            workItemServiceMock
                .Setup(s => s.UpdateFeaturesForPortfolio(It.IsAny<Portfolio>()))
                .ThrowsAsync(new InvalidOperationException("boom"));

            var subject = CreateSubject();

            try
            {
                subject.TriggerUpdate(project.Id);
            }
            catch (InvalidOperationException)
            {
            }
            catch (AggregateException)
            {
            }

            cleanupServiceMock.Verify(c => c.CleanupAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void TriggerUpdate_CredentialCannotBeRead_RecordsTheRefreshAsUnsuccessful()
        {
            var project = SetupPortfolioWhoseCredentialCannotBeRead();

            CreateSubject().TriggerUpdate(project.Id);

            Assert.That(recordedRefresh?.Success, Is.False,
                "A refresh that never reached the work tracking system is not a refresh that worked.");
        }

        [Test]
        public void TriggerUpdate_CredentialCannotBeRead_TheReasonNamesTheConnectionAndTheField()
        {
            var project = SetupPortfolioWhoseCredentialCannotBeRead();

            CreateSubject().TriggerUpdate(project.Id);

            var summary = ReadUpdateSummary(loggerMock);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(summary, Does.Contain(ConnectionName),
                    "An operator running several connections needs the reason to say which one to open.");
                Assert.That(summary, Does.Contain(SecretFieldKey),
                    "Without the field, the operator re-enters credentials until the refresh stops failing.");
            }
        }

        [Test]
        public void TriggerUpdate_CredentialCannotBeRead_ThePortfolioAndTheTeamSurfacesSayTheSameThing()
        {
            var project = SetupPortfolioWhoseCredentialCannotBeRead();
            var team = SetupTeamWhoseCredentialCannotBeRead();

            CreateSubject().TriggerUpdate(project.Id);
            CreateTeamSubject().TriggerUpdate(team.Id);

            // Derived, never transcribed: a sentence copied into two test files is exactly the thing that drifts,
            // and an operator who meets two phrasings for one failure has two things to learn instead of one.
            var expected = ReasonProbe.Build(
                CreateUnreadableSecretException(),
                project.WorkTrackingSystemConnection,
                cryptoServiceMock.Object);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ReadReason(loggerMock), Is.EqualTo(expected));
                Assert.That(ReadReason(teamLoggerMock), Is.EqualTo(expected));
            }
        }

        [Test]
        public void TriggerUpdate_CredentialCannotBeRead_TheFailureStillPropagatesAndIsReportedOnce()
        {
            var project = SetupPortfolioWhoseCredentialCannotBeRead();

            CreateSubject().TriggerUpdate(project.Id);

            var errors = ReadErrors(loggerMock);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(errors, Has.One.Contains("An exception occurred while updating"),
                    "Swallowing the failure to attach a reason would leave the refresh looking like it merely returned nothing.");
                Assert.That(errors, Has.Count.EqualTo(1),
                    "Logging where the reason is attached prints the same failure twice for one broken credential.");
            }
        }

        [Test]
        public void TriggerUpdate_EveryCredentialReads_RecordsSuccessAndSaysNothingAboutEncryption()
        {
            var project = CreateProject(DateTime.Now.AddDays(-1));
            SetupProjects(project);

            CreateSubject().TriggerUpdate(project.Id);

            var summary = ReadUpdateSummary(loggerMock);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(recordedRefresh?.Success, Is.True);
                Assert.That(summary, Does.Not.Contain("reason="),
                    "A healthy refresh has nothing to explain, and a reason on every line is what made these logs unreadable.");
                Assert.That(summary, Does.Not.Contain("encryption").IgnoreCase);
                Assert.That(summary, Does.Not.Contain("credential").IgnoreCase);
            }
        }

        private Portfolio SetupPortfolioWhoseCredentialCannotBeRead()
        {
            var project = CreateProject(DateTime.Now.AddDays(-1));
            AddUnreadableCredential(project.WorkTrackingSystemConnection);
            SetupProjects(project);

            workItemServiceMock
                .Setup(x => x.UpdateFeaturesForPortfolio(project))
                .ThrowsAsync(CreateUnreadableSecretException());

            return project;
        }

        private Team SetupTeamWhoseCredentialCannotBeRead()
        {
            var team = CreateTeam();
            team.UpdateTime = DateTime.Now.AddDays(-1);
            AddUnreadableCredential(team.WorkTrackingSystemConnection);

            teamRepoMock.Setup(x => x.GetAll()).Returns([team]);
            teamRepoMock.Setup(x => x.GetById(team.Id)).Returns(team);

            teamDataServiceMock
                .Setup(x => x.UpdateTeamData(team))
                .ThrowsAsync(CreateUnreadableSecretException());

            return team;
        }

        private void AddUnreadableCredential(WorkTrackingSystemConnection connection)
        {
            connection.Name = ConnectionName;
            connection.Options.Add(new WorkTrackingSystemConnectionOption
            {
                Key = SecretFieldKey,
                Value = UnreadableValue,
                IsSecret = true,
            });

            cryptoServiceMock
                .Setup(x => x.Read(UnreadableValue))
                .Returns(new SecretReadResult(SecretState.Unreadable, null, "retired"));
        }

        private static UnreadableSecretException CreateUnreadableSecretException()
        {
            return new UnreadableSecretException(SecretState.Unreadable, "retired");
        }

        private static string ReadUpdateSummary(Mock loggerMock)
        {
            var summary = loggerMock.Invocations
                .Where(i => (LogLevel)i.Arguments[0] == LogLevel.Information)
                .Select(i => i.Arguments[2]?.ToString() ?? string.Empty)
                .SingleOrDefault(message => message.Contains("Update completed", StringComparison.Ordinal));

            Assert.That(summary, Is.Not.Null, "The summary line is the record an operator reads for a finished refresh.");

            return summary!;
        }

        private static string ReadReason(Mock loggerMock)
        {
            const string marker = "reason=";

            var summary = ReadUpdateSummary(loggerMock);
            var reasonStart = summary.IndexOf(marker, StringComparison.Ordinal);

            Assert.That(reasonStart, Is.GreaterThanOrEqualTo(0),
                "A refresh that stopped because a stored credential could not be read has something to explain.");

            return summary[(reasonStart + marker.Length)..];
        }

        private static List<string> ReadErrors(Mock loggerMock)
        {
            return loggerMock.Invocations
                .Where(i => (LogLevel)i.Arguments[0] == LogLevel.Error)
                .Select(i => i.Arguments[2]?.ToString() ?? string.Empty)
                .ToList();
        }

        private TeamUpdater CreateTeamSubject()
        {
            return new TeamUpdater(teamLoggerMock.Object, ServiceScopeFactory, UpdateQueueService);
        }

        private Task WaitForEnqueue(int projectId)
        {
            return WaitUntilVerified(() => Mock.Get(UpdateQueueService).Verify(x => x.EnqueueUpdate(UpdateType.Features, projectId, It.IsAny<Func<IServiceProvider, Task>>())));
        }

        private void SetupProjects(params Portfolio[] projects)
        {
            projectRepoMock.Setup(x => x.GetAll()).Returns(projects);

            foreach (var project in projects)
            {
                projectRepoMock.Setup(x => x.GetById(project.Id)).Returns(project);
            }
        }

        private void SetupRefreshSettings(int interval, int refreshAfter)
        {
            var refreshSettings = new RefreshSettings { Interval = interval, RefreshAfter = refreshAfter, StartDelay = 0 };
            appSettingServiceMock.Setup(x => x.GetFeatureRefreshSettings()).Returns(refreshSettings);
        }


        private Team CreateTeam()
        {
            var team = new Team { Name = "Team", Id = idCounter++ };

            team.WorkItemTypes.Add("User Story");

            var workTrackingConnection = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.Jira };
            team.WorkTrackingSystemConnection = workTrackingConnection;

            return team;
        }

        private Portfolio CreateProject(params Team[] teams)
        {
            return CreateProject(DateTime.Now, teams);
        }

        private Portfolio CreateProject(DateTime lastUpdateTime, params Team[] teams)
        {
            var portfolio = new Portfolio
            {
                Id = idCounter++,
                Name = "Release 1",
            };

            foreach (var team in teams)
            {
                var feature = new Feature(team, 12) { Id = idCounter++ };
                portfolio.Features.Add(feature);
            }

            portfolio.WorkItemTypes.Add("Feature");

            var workTrackingConnection = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.Jira };
            portfolio.WorkTrackingSystemConnection = workTrackingConnection;

            portfolio.UpdateTime = lastUpdateTime;

            return portfolio;
        }

        private PortfolioUpdater CreateSubject()
        {
            return new PortfolioUpdater(loggerMock.Object, ServiceScopeFactory, UpdateQueueService, cleanupServiceMock.Object, domainEventDispatcherMock.Object, forecastUpdaterMock.Object);
        }
    }
}
