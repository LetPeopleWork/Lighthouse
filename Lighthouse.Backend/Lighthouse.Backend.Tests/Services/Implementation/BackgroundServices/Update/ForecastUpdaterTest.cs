using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Implementation.DatabaseManagement;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Concurrent;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices.Update
{
    public class ForecastUpdaterTest : UpdateServiceTestBase
    {
        private Mock<IRepository<Portfolio>> portfolioRepositoryMock;
        private Mock<IAppSettingService> appSettingServiceMock;
        private Mock<IForecastService> forecastServiceMock;
        private Mock<IWriteBackTriggerService> writeBackTriggerServiceMock;
        private Mock<IDomainEventDispatcher> domainEventDispatcherMock;
        private Mock<IRefreshLogService> refreshLogServiceMock;
        private Mock<IUpdateStatusStore> updateStatusStoreMock;
        private InProcessUpdateStatusStore inProcessUpdateStatusStore;

        private int idCounter = 0;

        private static readonly string[] WriteBackThenEventDispatchOrder = ["forecastWriteBack", "forecastsUpdatedEvent"];

        private const int SlowForecastMilliseconds = 60;

        private const int ShortestCredibleDurationMilliseconds = 25;

        private const int TeamOutsideThePortfolioId = 98;

        private const int PortfolioRefreshedElsewhereId = 99;

        [SetUp]
        public void Setup()
        {
            portfolioRepositoryMock = new Mock<IRepository<Portfolio>>();
            appSettingServiceMock = new Mock<IAppSettingService>();
            forecastServiceMock = new Mock<IForecastService>();
            writeBackTriggerServiceMock = new Mock<IWriteBackTriggerService>();
            domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();
            domainEventDispatcherMock
                .Setup(x => x.PublishAsync(It.IsAny<PortfolioForecastsUpdated>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            updateStatusStoreMock = new Mock<IUpdateStatusStore>();
            inProcessUpdateStatusStore = new InProcessUpdateStatusStore(new ConcurrentDictionary<UpdateKey, UpdateStatus>());

            refreshLogServiceMock = new Mock<IRefreshLogService>();
            refreshLogServiceMock
                .Setup(x => x.LogRefreshAsync(It.IsAny<RefreshLog>()))
                .Returns(Task.CompletedTask);

            SetupServiceProviderMock(appSettingServiceMock.Object);
            SetupServiceProviderMock(portfolioRepositoryMock.Object);
            SetupServiceProviderMock(forecastServiceMock.Object);
            SetupServiceProviderMock(writeBackTriggerServiceMock.Object);
            SetupServiceProviderMock(refreshLogServiceMock.Object);
        }

        [Test]
        public void Update_ShouldDoNothing_WhenProjectNotFound()
        {
            // Arrange
            portfolioRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).Returns((Portfolio)null);

            var subject = CreateSubject();

            // Act
            subject.TriggerUpdate(1);

            // Assert
            portfolioRepositoryMock.Verify(x => x.GetById(It.IsAny<int>()), Times.AtLeastOnce);
            portfolioRepositoryMock.VerifyNoOtherCalls();
            forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(It.IsAny<Portfolio>()), Times.Never);
        }

        [Test]
        public void Update_ShouldCallUpdateForecastsForProject_WhenProjectIsFound()
        {
            // Arrange
            var project = CreatePortfolio();

            portfolioRepositoryMock.Setup(x => x.GetById(project.Id)).Returns(project);

            var subject = CreateSubject();

            // Act
            subject.TriggerUpdate(project.Id);

            // Assert
            forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(project), Times.Once);
        }
        
        [Test]
        public void Update_ShouldTriggerForecastWriteBackForPortfolio_WhenProjectIsFound()
        {
            var portfolio = CreatePortfolio();

            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);

            var subject = CreateSubject();

            subject.TriggerUpdate(portfolio.Id);

            writeBackTriggerServiceMock.Verify(x => x.ResolveForecastWriteBackForPortfolio(portfolio), Times.Once);
        }

        [Test]
        public void Update_ShouldNotTriggerWriteBack_WhenProjectNotFound()
        {
            portfolioRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).Returns((Portfolio)null);

            var subject = CreateSubject();

            subject.TriggerUpdate(1);

            writeBackTriggerServiceMock.Verify(x => x.ResolveForecastWriteBackForPortfolio(It.IsAny<Portfolio>()), Times.Never);
        }

        [Test]
        public void Update_ShouldHandleException_WhenUpdateForecastsForProjectThrows()
        {
            // Arrange
            var portfolio = CreatePortfolio();

            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            forecastServiceMock.Setup(x => x.UpdateForecastsForPortfolio(It.IsAny<Portfolio>())).ThrowsAsync(new Exception("Test exception"));

            var subject = CreateSubject();

            // Act & Assert
            Assert.DoesNotThrow(() => subject.TriggerUpdate(portfolio.Id));
            forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(portfolio), Times.Once);
        }

        [Test]
        public void Update_ShouldPublishPortfolioForecastsUpdatedExactlyOnce_AfterForecastWriteBack()
        {
            var portfolio = CreatePortfolio();
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);

            var dispatchSequence = new List<string>();
            writeBackTriggerServiceMock
                .Setup(x => x.ResolveForecastWriteBackForPortfolio(portfolio))
                .Callback(() => dispatchSequence.Add("forecastWriteBack"))
                .Returns([]);
            domainEventDispatcherMock
                .Setup(x => x.PublishAsync(It.Is<PortfolioForecastsUpdated>(e => e.PortfolioId == portfolio.Id), It.IsAny<CancellationToken>()))
                .Callback(() => dispatchSequence.Add("forecastsUpdatedEvent"))
                .Returns(Task.CompletedTask);

            var subject = CreateSubject();
            subject.TriggerUpdate(portfolio.Id);

            domainEventDispatcherMock.Verify(x => x.PublishAsync(It.Is<PortfolioForecastsUpdated>(e => e.PortfolioId == portfolio.Id), It.IsAny<CancellationToken>()), Times.Once);
            Assert.That(dispatchSequence, Is.EqualTo(WriteBackThenEventDispatchOrder));
        }

        [Test]
        public void Update_ShouldNotPublishPortfolioForecastsUpdated_WhenProjectNotFound()
        {
            portfolioRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).Returns((Portfolio)null);

            var subject = CreateSubject();

            subject.TriggerUpdate(1);

            domainEventDispatcherMock.Verify(x => x.PublishAsync(It.IsAny<PortfolioForecastsUpdated>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void Update_ShouldRecordRefreshLogForForecast_WhenPortfolioIsFound()
        {
            var portfolio = CreatePortfolio();
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);

            var recorded = CaptureRefreshLog();

            var subject = CreateSubject();
            subject.TriggerUpdate(portfolio.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(recorded.Entry.Type, Is.EqualTo(RefreshType.Forecast));
                Assert.That(recorded.Entry.EntityId, Is.EqualTo(portfolio.Id));
                Assert.That(recorded.Entry.EntityName, Is.EqualTo(portfolio.Name));
                Assert.That(recorded.Entry.Success, Is.True);
            }
        }

        [Test]
        public void Update_ShouldRecordHowLongTheForecastRan()
        {
            var portfolio = CreatePortfolio();
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            forecastServiceMock
                .Setup(x => x.UpdateForecastsForPortfolio(portfolio))
                .Returns(Task.Delay(SlowForecastMilliseconds));

            var recorded = CaptureRefreshLog();

            var subject = CreateSubject();
            subject.TriggerUpdate(portfolio.Id);

            Assert.That(recorded.Entry.DurationMs, Is.GreaterThanOrEqualTo(ShortestCredibleDurationMilliseconds));
        }

        [Test]
        public void Update_ShouldRecordUnsuccessfulRefreshLog_WhenForecastThrows()
        {
            var portfolio = CreatePortfolio();
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            forecastServiceMock
                .Setup(x => x.UpdateForecastsForPortfolio(It.IsAny<Portfolio>()))
                .ThrowsAsync(new Exception("Test exception"));

            var recorded = CaptureRefreshLog();

            var subject = CreateSubject();
            subject.TriggerUpdate(portfolio.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(recorded.Entry.Type, Is.EqualTo(RefreshType.Forecast));
                Assert.That(recorded.Entry.EntityId, Is.EqualTo(portfolio.Id));
                Assert.That(recorded.Entry.Success, Is.False);
            }
        }

        [Test]
        public void Update_ShouldNotForecastYet_WhenSiblingTeamIsStillWaitingToRefresh()
        {
            var team = new Team { Name = "Sibling Team", Id = 42 };
            var portfolio = CreatePortfolioWorkedOnBy(team);
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);

            updateStatusStoreMock
                .Setup(x => x.HasQueuedWork(It.Is<IReadOnlyCollection<UpdateKey>>(keys => keys.Contains(new UpdateKey(UpdateType.Team, team.Id)))))
                .Returns(true);

            var subject = CreateSubject();

            subject.TriggerUpdate(portfolio.Id);

            forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(It.IsAny<Portfolio>()), Times.Never);
        }

        [Test]
        public void Update_ShouldForecast_WhenNoTeamOfThePortfolioIsWaitingToRefresh()
        {
            var team = new Team { Name = "Sibling Team", Id = 42 };
            var portfolio = CreatePortfolioWorkedOnBy(team);
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);

            updateStatusStoreMock
                .Setup(x => x.HasQueuedWork(It.IsAny<IReadOnlyCollection<UpdateKey>>()))
                .Returns(false);

            var subject = CreateSubject();

            subject.TriggerUpdate(portfolio.Id);

            forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(portfolio), Times.Once);
        }

        [Test]
        public void Update_ShouldForecast_WhenTheOnlyTeamOfThePortfolioHasNothingPending()
        {
            var team = new Team { Name = "Only Team", Id = 42 };
            var portfolio = CreatePortfolioWorkedOnBy(team);
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);

            var subject = CreateSubject(inProcessUpdateStatusStore);

            subject.TriggerUpdate(portfolio.Id);

            forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(portfolio), Times.Once);
        }

        // A team announces that it finished refreshing from inside its own update run, so at the moment the
        // forecast is asked for, that team's key still reads as running. It only ever goes back to queued
        // after the run has ended, and then only when another refresh for it was folded into the first.
        [Test]
        public void Update_ShouldForecast_WhenTheTeamThatAskedForItIsStillRunningItsOwnRefresh()
        {
            var team = new Team { Name = "Refreshing Team", Id = 42 };
            var portfolio = CreatePortfolioWorkedOnBy(team);
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);

            RecordUpdate(UpdateType.Team, team.Id, UpdateProgress.InProgress);

            var subject = CreateSubject(inProcessUpdateStatusStore);

            subject.TriggerUpdate(portfolio.Id);

            forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(portfolio), Times.Once);
        }

        [Test]
        public void Update_ShouldForecast_WhenTheQueuedWorkBelongsToTeamsThatDoNotWorkOnThePortfolio()
        {
            var team = new Team { Name = "Refreshing Team", Id = 42 };
            var portfolio = CreatePortfolioWorkedOnBy(team);
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);

            RecordUpdate(UpdateType.Team, team.Id, UpdateProgress.InProgress);
            RecordUpdate(UpdateType.Team, TeamOutsideThePortfolioId, UpdateProgress.Queued);
            RecordUpdate(UpdateType.Features, PortfolioRefreshedElsewhereId, UpdateProgress.Queued);

            var subject = CreateSubject(inProcessUpdateStatusStore);

            subject.TriggerUpdate(portfolio.Id);

            forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(portfolio), Times.Once);
        }

        [Test]
        public void Update_ShouldForecastBothPortfolios_WhenTheSameTeamWorksOnTwoOfThem()
        {
            var team = new Team { Name = "Shared Team", Id = 42 };
            var firstPortfolio = CreatePortfolioWorkedOnBy(team);
            var secondPortfolio = CreatePortfolioWorkedOnBy(team);
            portfolioRepositoryMock.Setup(x => x.GetById(firstPortfolio.Id)).Returns(firstPortfolio);
            portfolioRepositoryMock.Setup(x => x.GetById(secondPortfolio.Id)).Returns(secondPortfolio);

            RecordUpdate(UpdateType.Team, team.Id, UpdateProgress.InProgress);

            var subject = CreateSubject(inProcessUpdateStatusStore);

            subject.TriggerUpdate(firstPortfolio.Id);
            subject.TriggerUpdate(secondPortfolio.Id);

            forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(firstPortfolio), Times.Once);
            forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(secondPortfolio), Times.Once);
        }

        [Test]
        public void Update_ShouldForecast_WhenTheOrderOfTheFeaturesChanged()
        {
            var firstFeature = CreateFeatureWorkedOnBy(new Team { Name = "Team On The First Feature", Id = 42 });
            var secondFeature = CreateFeatureWorkedOnBy(new Team { Name = "Team On The Second Feature", Id = 43 });
            var portfolio = CreatePortfolio(firstFeature, secondFeature);
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);

            portfolio.UpdateFeatures([secondFeature, firstFeature]);

            var subject = CreateSubject(inProcessUpdateStatusStore);

            subject.TriggerUpdate(portfolio.Id);

            forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(portfolio), Times.Once);
        }

        [Test]
        public async Task Update_ShouldStillForecast_WhenTheLastTeamOfThePortfolioFailsToRefresh()
        {
            var firstTeam = new Team { Name = "First Team", Id = 42 };
            var lastTeam = new Team { Name = "Last Team", Id = 43 };
            var portfolio = CreatePortfolio(CreateFeatureWorkedOnBy(firstTeam), CreateFeatureWorkedOnBy(lastTeam));
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);

            using var updateQueue = CreateRealUpdateQueue();

            var firstTeamHasFinished = new TaskCompletionSource();
            updateQueue.EnqueueUpdate(UpdateType.Team, firstTeam.Id, _ => firstTeamHasFinished.Task);
            updateQueue.EnqueueUpdate(UpdateType.Team, lastTeam.Id, _ => throw new InvalidOperationException("The last team could not be refreshed"));

            var subject = CreateSubject(inProcessUpdateStatusStore, updateQueue);
            subject.TriggerUpdate(portfolio.Id);

            forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(portfolio), Times.Never);

            firstTeamHasFinished.SetResult();

            await WaitUntilVerified(() => forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(portfolio), Times.Once));
        }

        [Test]
        public void TriggerImmediateUpdate_ShouldForecast_WhenSiblingTeamIsStillWaitingToRefresh()
        {
            var team = new Team { Name = "Sibling Team", Id = 42 };
            var portfolio = CreatePortfolioWorkedOnBy(team);
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);

            updateStatusStoreMock
                .Setup(x => x.HasQueuedWork(It.Is<IReadOnlyCollection<UpdateKey>>(keys => keys.Contains(new UpdateKey(UpdateType.Team, team.Id)))))
                .Returns(true);

            var subject = CreateSubject();

            subject.TriggerImmediateUpdate(portfolio.Id);

            forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(portfolio), Times.Once);
        }

        [Test]
        public void TriggerImmediateUpdate_ShouldForecast_WhenAForecastForThePortfolioIsAlreadyOwed()
        {
            var portfolio = CreatePortfolio();
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);

            Mock.Get(UpdateQueueService)
                .Setup(x => x.IsHeld(new UpdateKey(UpdateType.Forecasts, portfolio.Id)))
                .Returns(true);
            updateStatusStoreMock
                .Setup(x => x.HasQueuedWork(It.Is<IReadOnlyCollection<UpdateKey>>(keys => keys.Contains(new UpdateKey(UpdateType.Forecasts, portfolio.Id)))))
                .Returns(true);

            var subject = CreateSubject();

            subject.TriggerImmediateUpdate(portfolio.Id);

            forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(portfolio), Times.Once);
        }

        [Test]
        public async Task TriggerImmediateUpdate_ShouldLeaveAWaitingForecastToStillRunExactlyOnce()
        {
            var runningTeam = new Team { Name = "Running Team", Id = 42 };
            var waitingTeam = new Team { Name = "Waiting Team", Id = 43 };
            var portfolio = CreatePortfolio(CreateFeatureWorkedOnBy(runningTeam), CreateFeatureWorkedOnBy(waitingTeam));
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);

            using var updateQueue = CreateRealUpdateQueue();

            var runningTeamHasFinished = new TaskCompletionSource();
            updateQueue.EnqueueUpdate(UpdateType.Team, runningTeam.Id, _ => runningTeamHasFinished.Task);
            updateQueue.EnqueueUpdate(UpdateType.Team, waitingTeam.Id, _ => Task.CompletedTask);

            var subject = CreateSubject(inProcessUpdateStatusStore, updateQueue);

            subject.TriggerUpdate(portfolio.Id);
            subject.TriggerImmediateUpdate(portfolio.Id);

            runningTeamHasFinished.SetResult();

            await WaitUntilVerified(() => forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(portfolio), Times.Exactly(2)));

            await updateQueue.DrainAsync();

            forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(portfolio), Times.Exactly(2));
        }

        private void RecordUpdate(UpdateType updateType, int id, UpdateProgress progress)
        {
            inProcessUpdateStatusStore.TryAdmit(
                new UpdateKey(updateType, id),
                new UpdateStatus { UpdateType = updateType, Id = id, Status = progress });
        }

        private RecordedRefreshLog CaptureRefreshLog()
        {
            var recorded = new RecordedRefreshLog();
            refreshLogServiceMock
                .Setup(x => x.LogRefreshAsync(It.IsAny<RefreshLog>()))
                .Callback<RefreshLog>(recorded.Record)
                .Returns(Task.CompletedTask);

            return recorded;
        }

        private sealed class RecordedRefreshLog
        {
            private RefreshLog? entry;

            public RefreshLog Entry => entry ?? throw new AssertionException("No refresh was recorded for the forecast.");

            public void Record(RefreshLog refreshLog)
            {
                entry = refreshLog;
            }
        }

        private ForecastUpdater CreateSubject()
        {
            return CreateSubject(updateStatusStoreMock.Object);
        }

        private ForecastUpdater CreateSubject(IUpdateStatusStore statusStore)
        {
            return CreateSubject(statusStore, UpdateQueueService);
        }

        private ForecastUpdater CreateSubject(IUpdateStatusStore statusStore, IUpdateQueueService updateQueueService)
        {
            return new ForecastUpdater(Mock.Of<ILogger<ForecastUpdater>>(), ServiceScopeFactory, updateQueueService, domainEventDispatcherMock.Object, statusStore);
        }

        private UpdateQueueService CreateRealUpdateQueue()
        {
            var hubContextMock = new Mock<IHubContext<UpdateNotificationHub>>();
            hubContextMock.Setup(hub => hub.Clients.Group(It.IsAny<string>())).Returns(Mock.Of<IClientProxy>());

            return new UpdateQueueService(
                Mock.Of<ILogger<UpdateQueueService>>(),
                hubContextMock.Object,
                new UpdateSubstrate(inProcessUpdateStatusStore, new InProcessUpdateExecutionLock(), new InProcessUpdateCompletionNotifier()),
                ServiceScopeFactory,
                new DatabaseMaintenanceGate(inProcessUpdateStatusStore),
                new WriteBackRoundContext());
        }

        private Portfolio CreatePortfolioWorkedOnBy(Team team)
        {
            return CreatePortfolio(CreateFeatureWorkedOnBy(team));
        }

        private Feature CreateFeatureWorkedOnBy(Team team)
        {
            var feature = new Feature { Name = "Feature", Id = idCounter++ };
            feature.FeatureWork.Add(new FeatureWork(team, 3, 5, feature));

            return feature;
        }

        private Portfolio CreatePortfolio(params Feature[] features)
        {
            var portfolio = CreatePortfolio(DateTime.UtcNow, features);

            return portfolio;
        }

        private Portfolio CreatePortfolio(DateTime lastUpdatedTime, params Feature[] features)
        {
            var portfolio = new Portfolio
            {
                Name = "Project",
                Id = idCounter++,
                UpdateTime = lastUpdatedTime,
            };
            portfolio.UpdateFeatures(features);
            return portfolio;
        }
    }
}
