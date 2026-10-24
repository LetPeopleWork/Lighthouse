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
using Lighthouse.Backend.Tests.TestDoubles;
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
        private UpdateCancellationContext cancellationContext;
        private Mock<ILogger<ForecastUpdater>> loggerMock;
        private Mock<IPortfolioRepository> forecastInputsMock;
        private Mock<IFeatureRepository> featureRepositoryMock;

        private int idCounter = 0;

        private static readonly string[] WriteBackThenEventDispatchOrder = ["forecastWriteBack", "forecastsUpdatedEvent"];

        private static readonly string[] WriteBackThenEventDispatchThenRecordOrder = ["forecastWriteBack", "forecastsUpdatedEvent", "recordInputs"];

        private static readonly string[] ConfirmThenWriteBackThenEventDispatchOrder = ["confirmForecasts", "forecastWriteBack", "forecastsUpdatedEvent"];

        private static readonly int[] TheFeaturesTheSkippedForecastRead = [3, 7, 12];

        private const string TheRunMark = "computing:5d0c3b7a9e1f4a2c8b6d0e2f4a6c8e1b";

        private const string InputsNotRecorded = "not recorded";

        private const int SlowForecastMilliseconds = 60;

        private const int ShortestCredibleDurationMilliseconds = 25;

        private const int TeamOutsideThePortfolioId = 98;

        private const int PortfolioRefreshedElsewhereId = 99;

        private static readonly DateOnly InstanceDay = new(2026, 3, 9);

        [SetUp]
        public void Setup()
        {
            portfolioRepositoryMock = new Mock<IRepository<Portfolio>>();
            appSettingServiceMock = new Mock<IAppSettingService>();
            forecastServiceMock = ForecastServiceDoubles.AnsweringComputed();
            writeBackTriggerServiceMock = new Mock<IWriteBackTriggerService>();
            domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();
            domainEventDispatcherMock
                .Setup(x => x.PublishAsync(It.IsAny<PortfolioForecastsUpdated>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            updateStatusStoreMock = new Mock<IUpdateStatusStore>();
            cancellationContext = new UpdateCancellationContext();
            loggerMock = new Mock<ILogger<ForecastUpdater>>();
            inProcessUpdateStatusStore = new InProcessUpdateStatusStore(new ConcurrentDictionary<UpdateKey, UpdateStatus>(), Clocks.SystemUtc);

            refreshLogServiceMock = new Mock<IRefreshLogService>();
            refreshLogServiceMock
                .Setup(x => x.LogRefreshAsync(It.IsAny<RefreshLog>()))
                .Returns(Task.CompletedTask);

            SetupServiceProviderMock(appSettingServiceMock.Object);
            forecastInputsMock = new Mock<IPortfolioRepository>();
            forecastInputsMock
                .Setup(x => x.TryRecordForecastInputs(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            forecastInputsMock
                .Setup(x => x.ForgetForecastInputs(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            featureRepositoryMock = new Mock<IFeatureRepository>();
            featureRepositoryMock
                .Setup(x => x.ConfirmForecastsAsOf(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            SetupServiceProviderMock(portfolioRepositoryMock.Object);
            SetupServiceProviderMock(forecastInputsMock.Object);
            SetupServiceProviderMock(featureRepositoryMock.Object);
            SetupServiceProviderMock(forecastServiceMock.Object);
            SetupServiceProviderMock(writeBackTriggerServiceMock.Object);
            SetupServiceProviderMock(refreshLogServiceMock.Object);
            SetupServiceProviderMock<ILighthouseClock>(new FakeLighthouseClock(new DateTimeOffset(InstanceDay, new TimeOnly(10, 0), TimeSpan.Zero)));
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
            forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(It.IsAny<Portfolio>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
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
            forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(project, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Test]
        public void Update_ShouldForecastForTheInstanceDayTheClockReports()
        {
            var portfolio = CreatePortfolio();
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);

            var subject = CreateSubject();
            subject.TriggerUpdate(portfolio.Id);

            forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(portfolio, InstanceDay, It.IsAny<CancellationToken>()), Times.Once);
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
            forecastServiceMock.Setup(x => x.UpdateForecastsForPortfolio(It.IsAny<Portfolio>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Test exception"));

            var subject = CreateSubject();

            // Act & Assert
            Assert.DoesNotThrow(() => subject.TriggerUpdate(portfolio.Id));
            forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(portfolio, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
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

                Assert.That(recorded.Entry.Cancelled, Is.False,
                    "Nobody stopped this one. Marked cancelled, every healthy forecast would be excluded "
                    + "from the success rate and the panel would report nothing at all.");
            }
        }

        [Test]
        public void Update_RecordsTheForecastAtTheClocksInstant()
        {
            var portfolio = CreatePortfolio();
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);

            var recorded = CaptureRefreshLog();

            CreateSubject().TriggerUpdate(portfolio.Id);

            Assert.That(recorded.Entry.ExecutedAt, Is.EqualTo(Now));
        }

        [Test]
        public void Update_ShouldRecordHowLongTheForecastRan()
        {
            var portfolio = CreatePortfolio();
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            forecastServiceMock
                .Setup(x => x.UpdateForecastsForPortfolio(portfolio, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    await Task.Delay(SlowForecastMilliseconds, CancellationToken.None);
                    return ForecastServiceDoubles.Computed;
                });

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
                .Setup(x => x.UpdateForecastsForPortfolio(It.IsAny<Portfolio>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
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
        public void Update_ShouldRecordTheRefreshAsCancelled_WhenTheForecastIsStopped()
        {
            var portfolio = CreatePortfolio();
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            TheForecastIsStopped();

            var recorded = CaptureRefreshLog();

            var subject = CreateSubject();
            subject.TriggerUpdate(portfolio.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(recorded.Entry.Cancelled, Is.True,
                    "Forecasts log their own refresh, so this half can report a cancel as a failure whatever "
                    + "the team and portfolio halves do.");

                Assert.That(recorded.Entry.Success, Is.False,
                    "Still not a refresh that finished - the forecast it was asked for never ran.");

                Assert.That(WhatTheRefreshThrew, Is.InstanceOf<OperationCanceledException>(),
                    "Recording the cancel is half of it. Swallowed here the forecast returns as though it "
                    + "finished, and the queue tells the browser the work completed.");
            }
        }

        [Test]
        public void Update_ShouldRecordAFailureNotACancel_WhenACancellationArrivesThatNobodyAskedFor()
        {
            var portfolio = CreatePortfolio();
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            forecastServiceMock
                .Setup(x => x.UpdateForecastsForPortfolio(It.IsAny<Portfolio>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TaskCanceledException("A call inside the forecast timed out."));

            var recorded = CaptureRefreshLog();

            var subject = CreateSubject();
            subject.TriggerUpdate(portfolio.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(recorded.Entry.Cancelled, Is.False,
                    "Nobody stopped this forecast, and the queue reports it as failed; the refresh history must agree.");
                Assert.That(recorded.Entry.Success, Is.False);
                Assert.That(ReadUpdateSummary(), Does.Contain(" | forecast=failed after "));
            }
        }

        [Test]
        public void Update_ShouldSayTheForecastWasCancelled_InTheSummaryLine_WhenTheForecastIsStopped()
        {
            var portfolio = CreatePortfolio();
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            TheForecastIsStopped();

            var subject = CreateSubject();
            subject.TriggerUpdate(portfolio.Id);

            Assert.That(ReadUpdateSummary(), Does.Contain(" | forecast=cancelled after "),
                "An operator who stopped a forecast would otherwise read that it broke.");
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

            forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(It.IsAny<Portfolio>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
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

            forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(portfolio, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void Update_ShouldForecast_WhenTheOnlyTeamOfThePortfolioHasNothingPending()
        {
            var team = new Team { Name = "Only Team", Id = 42 };
            var portfolio = CreatePortfolioWorkedOnBy(team);
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);

            var subject = CreateSubject(inProcessUpdateStatusStore);

            subject.TriggerUpdate(portfolio.Id);

            forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(portfolio, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
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

            forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(portfolio, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
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

            forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(portfolio, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
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

            forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(firstPortfolio, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
            forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(secondPortfolio, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
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

            forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(portfolio, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
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

            forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(portfolio, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);

            firstTeamHasFinished.SetResult();

            await WaitUntilVerified(() => forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(portfolio, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once));
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

            forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(portfolio, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
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

            forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(portfolio, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
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

            await WaitUntilVerified(() => forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(portfolio, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Exactly(2)));

            await updateQueue.DrainAsync(CancellationToken.None);

            forecastServiceMock.Verify(x => x.UpdateForecastsForPortfolio(portfolio, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Test]
        public void Update_ShouldRecordASkippedSuccessfulRefreshAndSaySo_WhenTheInputsAreConfirmedUnchanged()
        {
            var portfolio = CreatePortfolio();
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            TheForecastAnswers(new ForecastRunOutcome.ConfirmedUnchanged([]));

            var recorded = CaptureRefreshLog();

            var subject = CreateSubject();
            subject.TriggerUpdate(portfolio.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(recorded.Entry.Skipped, Is.True);
                Assert.That(recorded.Entry.Success, Is.True, "A skipped forecast is a successful one.");
                Assert.That(recorded.Entry.Cancelled, Is.False);
                Assert.That(ReadUpdateSummary(), Does.Contain(" | forecast=skipped after "));
            }
        }

        [Test]
        public void Update_ShouldStillStageAndAnnounceButRecordNothing_WhenTheInputsAreConfirmedUnchanged()
        {
            var portfolio = CreatePortfolio();
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            TheForecastAnswers(new ForecastRunOutcome.ConfirmedUnchanged([]));

            var subject = CreateSubject();
            subject.TriggerUpdate(portfolio.Id);

            writeBackTriggerServiceMock.Verify(x => x.ResolveForecastWriteBackForPortfolio(portfolio), Times.Once);
            domainEventDispatcherMock.Verify(x => x.PublishAsync(It.Is<PortfolioForecastsUpdated>(e => e.PortfolioId == portfolio.Id), It.IsAny<CancellationToken>()), Times.Once);
            forecastInputsMock.VerifyNoOtherCalls();
        }

        [Test]
        public void Update_ShouldRecordTheInputsLast_AfterStagingAndAnnouncing_WhenTheForecastWasComputed()
        {
            var portfolio = CreatePortfolio();
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            TheForecastAnswers(new ForecastRunOutcome.Computed(TheRunMark, "fresh"));

            var sequence = new List<string>();
            writeBackTriggerServiceMock
                .Setup(x => x.ResolveForecastWriteBackForPortfolio(portfolio))
                .Callback(() => sequence.Add("forecastWriteBack"))
                .Returns([]);
            domainEventDispatcherMock
                .Setup(x => x.PublishAsync(It.IsAny<PortfolioForecastsUpdated>(), It.IsAny<CancellationToken>()))
                .Callback(() => sequence.Add("forecastsUpdatedEvent"))
                .Returns(Task.CompletedTask);
            forecastInputsMock
                .Setup(x => x.TryRecordForecastInputs(portfolio.Id, TheRunMark, "fresh", It.IsAny<CancellationToken>()))
                .Callback(() => sequence.Add("recordInputs"))
                .ReturnsAsync(true);

            var subject = CreateSubject();
            subject.TriggerUpdate(portfolio.Id);

            Assert.That(sequence, Is.EqualTo(WriteBackThenEventDispatchThenRecordOrder));
        }

        [Test]
        public void Update_ShouldLeaveTheMarkAndNotFail_WhenTheComputedForecastHasNoFreshInputs()
        {
            var portfolio = CreatePortfolio();
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            TheForecastAnswers(new ForecastRunOutcome.Computed(TheRunMark, null));

            var recorded = CaptureRefreshLog();

            var subject = CreateSubject();
            subject.TriggerUpdate(portfolio.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(recorded.Entry.Success, Is.True);
                Assert.That(recorded.Entry.Skipped, Is.False);
                Assert.That(WhatTheRefreshThrew, Is.Null);
            }

            forecastInputsMock.VerifyNoOtherCalls();
        }

        [Test]
        public void Update_ShouldForgetTheInputsAndRecordNothing_WhenTheForecastFails()
        {
            var portfolio = CreatePortfolio();
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            forecastServiceMock
                .Setup(x => x.UpdateForecastsForPortfolio(It.IsAny<Portfolio>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("The forecast failed"));

            var subject = CreateSubject();
            subject.TriggerUpdate(portfolio.Id);

            forecastInputsMock.Verify(x => x.ForgetForecastInputs(It.Is<IReadOnlyCollection<int>>(ids => ids.Count == 1 && ids.Contains(portfolio.Id)), CancellationToken.None), Times.Once);
            forecastInputsMock.VerifyNoOtherCalls();
        }

        [Test]
        public void Update_ShouldForgetTheInputs_WhenTheForecastIsStopped()
        {
            var portfolio = CreatePortfolio();
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            TheForecastIsStopped();

            var subject = CreateSubject();
            subject.TriggerUpdate(portfolio.Id);

            forecastInputsMock.Verify(x => x.ForgetForecastInputs(It.Is<IReadOnlyCollection<int>>(ids => ids.Contains(portfolio.Id)), CancellationToken.None), Times.Once);
            forecastInputsMock.VerifyNoOtherCalls();
        }

        [Test]
        public void Update_ShouldStillReportTheForecastsOwnFailure_WhenForgettingTheInputsFails()
        {
            var portfolio = CreatePortfolio();
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            var forecastFailure = new InvalidOperationException("The forecast failed");
            forecastServiceMock
                .Setup(x => x.UpdateForecastsForPortfolio(It.IsAny<Portfolio>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(forecastFailure);
            forecastInputsMock
                .Setup(x => x.ForgetForecastInputs(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("The database is gone"));

            var recorded = CaptureRefreshLog();

            var subject = CreateSubject();
            subject.TriggerUpdate(portfolio.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(WhatTheRefreshThrew, Is.SameAs(forecastFailure));
                Assert.That(recorded.Entry.Success, Is.False);
            }
        }

        [Test]
        public void Update_ShouldRecordNothingAndSaySo_WhenTheRunCouldNotMarkItself()
        {
            var portfolio = CreatePortfolio();
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            TheForecastAnswers(new ForecastRunOutcome.Computed(null, "fresh"));

            var subject = CreateSubject();
            subject.TriggerUpdate(portfolio.Id);

            forecastInputsMock.VerifyNoOtherCalls();
            Assert.That(DebugLines(), Has.Some.Contains(InputsNotRecorded));
        }

        [Test]
        public void Update_ShouldSaySo_WhenTheInputsCouldNotReplaceTheMark()
        {
            var portfolio = CreatePortfolio();
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            TheForecastAnswers(new ForecastRunOutcome.Computed(TheRunMark, "fresh"));
            forecastInputsMock
                .Setup(x => x.TryRecordForecastInputs(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var subject = CreateSubject();
            subject.TriggerUpdate(portfolio.Id);

            Assert.That(DebugLines(), Has.Some.Contains(InputsNotRecorded));
        }

        [Test]
        public void Update_ShouldNotSaySo_WhenTheInputsWereRecorded()
        {
            var portfolio = CreatePortfolio();
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            TheForecastAnswers(new ForecastRunOutcome.Computed(TheRunMark, "fresh"));

            var subject = CreateSubject();
            subject.TriggerUpdate(portfolio.Id);

            Assert.That(DebugLines(), Has.None.Contains(InputsNotRecorded));
        }

        /// <summary>
        /// The forecasts were saved and the run's mark stands in place of the inputs, so the next round already
        /// cannot match. Nothing has to be forgotten, and the mark must not be swapped for the fresh inputs.
        /// </summary>
        [Test]
        public void Update_ShouldLeaveTheMarkAndForgetNothing_WhenStagingFailsAfterAComputedForecast()
        {
            var portfolio = CreatePortfolio();
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            TheForecastAnswers(new ForecastRunOutcome.Computed(TheRunMark, "fresh"));
            writeBackTriggerServiceMock
                .Setup(x => x.ResolveForecastWriteBackForPortfolio(portfolio))
                .Throws(new InvalidOperationException("Staging failed"));

            var recorded = CaptureRefreshLog();

            var subject = CreateSubject();
            subject.TriggerUpdate(portfolio.Id);

            Assert.That(recorded.Entry.Success, Is.False);
            forecastInputsMock.VerifyNoOtherCalls();
        }

        [Test]
        public void Update_ShouldLeaveTheMarkAndForgetNothing_WhenAnnouncingFailsAfterAComputedForecast()
        {
            var portfolio = CreatePortfolio();
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            TheForecastAnswers(new ForecastRunOutcome.Computed(TheRunMark, "fresh"));
            domainEventDispatcherMock
                .Setup(x => x.PublishAsync(It.IsAny<PortfolioForecastsUpdated>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Publishing failed"));

            var subject = CreateSubject();
            subject.TriggerUpdate(portfolio.Id);

            forecastInputsMock.VerifyNoOtherCalls();
        }

        /// <summary>
        /// A skipped run writes no mark, so the inputs it confirmed would still match next round. A failure part
        /// way through it is a reason to doubt them, and doubt means compute.
        /// </summary>
        [Test]
        public void Update_ShouldForgetTheInputs_WhenStagingFailsAfterASkippedForecast()
        {
            var portfolio = CreatePortfolio();
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            TheForecastAnswers(new ForecastRunOutcome.ConfirmedUnchanged([]));
            writeBackTriggerServiceMock
                .Setup(x => x.ResolveForecastWriteBackForPortfolio(portfolio))
                .Throws(new InvalidOperationException("Staging failed"));

            var subject = CreateSubject();
            subject.TriggerUpdate(portfolio.Id);

            forecastInputsMock.Verify(x => x.ForgetForecastInputs(It.Is<IReadOnlyCollection<int>>(ids => ids.Count == 1 && ids.Contains(portfolio.Id)), CancellationToken.None), Times.Once);
            forecastInputsMock.VerifyNoOtherCalls();
        }

        [Test]
        public void Update_ShouldTimeTheGatherTheSimulationTheSaveAndThePublishingApart_AtDebug()
        {
            var portfolio = CreatePortfolio();
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);

            var subject = CreateSubject();
            subject.TriggerUpdate(portfolio.Id);

            Assert.That(DebugLines().Where(line => line.StartsWith("Forecast round timings", StringComparison.Ordinal)), Has.Exactly(1).Match(
                $@"^Forecast round timings \| Portfolio '{portfolio.Name}' \| gather=\d+ms \| simulate=\d+ms \| save=\d+ms \| publish=\d+ms$"));
        }

        /// <summary>
        /// Confirming the stored forecasts is the only write of a quiet round, so it has to land inside the
        /// save figure rather than between the figures, where nothing would show it.
        /// </summary>
        [Test]
        public void Update_ShouldCountTheConfirmationInTheSaveFigure_WhenTheInputsAreConfirmedUnchanged()
        {
            var portfolio = CreatePortfolio();
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            TheForecastAnswers(new ForecastRunOutcome.ConfirmedUnchanged(TheFeaturesTheSkippedForecastRead));
            featureRepositoryMock
                .Setup(x => x.ConfirmForecastsAsOf(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .Returns(async () => await Task.Delay(SlowForecastMilliseconds, CancellationToken.None));

            var subject = CreateSubject();
            subject.TriggerUpdate(portfolio.Id);

            var timings = DebugLines().Single(line => line.StartsWith("Forecast round timings", StringComparison.Ordinal));
            var saveMs = int.Parse(timings.Split("save=")[1].Split("ms")[0], System.Globalization.CultureInfo.InvariantCulture);

            Assert.That(saveMs, Is.GreaterThanOrEqualTo(ShortestCredibleDurationMilliseconds));
        }

        private List<string> DebugLines()
        {
            return [.. loggerMock.Invocations
                .Where(i => i.Arguments[0] is LogLevel.Debug)
                .Select(i => i.Arguments[2]?.ToString() ?? string.Empty)];
        }

        [Test]
        public void Update_ShouldConfirmTheForecastsOfEveryFeatureItRead_AtTheRound_BeforeStagingAndAnnouncing_WhenTheInputsAreConfirmedUnchanged()
        {
            var portfolio = CreatePortfolio();
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            TheForecastAnswers(new ForecastRunOutcome.ConfirmedUnchanged(TheFeaturesTheSkippedForecastRead));

            var sequence = new List<string>();
            IReadOnlyCollection<int>? confirmedFeatures = null;
            var confirmedAt = DateTime.MinValue;
            featureRepositoryMock
                .Setup(x => x.ConfirmForecastsAsOf(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .Callback<IReadOnlyCollection<int>, DateTime, CancellationToken>((ids, at, _) =>
                {
                    sequence.Add("confirmForecasts");
                    confirmedFeatures = ids;
                    confirmedAt = at;
                })
                .Returns(Task.CompletedTask);
            writeBackTriggerServiceMock
                .Setup(x => x.ResolveForecastWriteBackForPortfolio(portfolio))
                .Callback(() => sequence.Add("forecastWriteBack"))
                .Returns([]);
            domainEventDispatcherMock
                .Setup(x => x.PublishAsync(It.IsAny<PortfolioForecastsUpdated>(), It.IsAny<CancellationToken>()))
                .Callback(() => sequence.Add("forecastsUpdatedEvent"))
                .Returns(Task.CompletedTask);

            var subject = CreateSubject();
            subject.TriggerUpdate(portfolio.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(confirmedFeatures, Is.EquivalentTo(TheFeaturesTheSkippedForecastRead));
                Assert.That(confirmedAt, Is.EqualTo(Now));
                Assert.That(sequence, Is.EqualTo(ConfirmThenWriteBackThenEventDispatchOrder));
            }
        }

        [Test]
        public void Update_ShouldConfirmNothing_WhenTheForecastWasComputed()
        {
            var portfolio = CreatePortfolio();
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            TheForecastAnswers(new ForecastRunOutcome.Computed(TheRunMark, "fresh"));

            var subject = CreateSubject();
            subject.TriggerUpdate(portfolio.Id);

            featureRepositoryMock.VerifyNoOtherCalls();
        }

        [Test]
        public void Update_ShouldConfirmNothing_WhenTheForecastFails()
        {
            var portfolio = CreatePortfolio();
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            forecastServiceMock
                .Setup(x => x.UpdateForecastsForPortfolio(It.IsAny<Portfolio>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("The simulation failed"));

            var subject = CreateSubject();
            subject.TriggerUpdate(portfolio.Id);

            featureRepositoryMock.VerifyNoOtherCalls();
        }

        [Test]
        public void Update_ShouldConfirmNothing_WhenTheForecastIsStopped()
        {
            var portfolio = CreatePortfolio();
            portfolioRepositoryMock.Setup(x => x.GetById(portfolio.Id)).Returns(portfolio);
            TheForecastIsStopped();

            var subject = CreateSubject();
            subject.TriggerUpdate(portfolio.Id);

            featureRepositoryMock.VerifyNoOtherCalls();
        }

        private void TheForecastAnswers(ForecastRunOutcome outcome)
        {
            forecastServiceMock
                .Setup(x => x.UpdateForecastsForPortfolio(It.IsAny<Portfolio>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(outcome);
        }

        private void RecordUpdate(UpdateType updateType, int id, UpdateProgress progress)
        {
            inProcessUpdateStatusStore.TryAdmit(
                new UpdateKey(updateType, id),
                new UpdateStatus { UpdateType = updateType, Id = id, Status = progress });
        }

        private void TheForecastIsStopped()
        {
            var stopped = new CancellationToken(canceled: true);
            cancellationContext.Current = stopped;

            forecastServiceMock
                .Setup(x => x.UpdateForecastsForPortfolio(It.IsAny<Portfolio>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException(stopped));
        }

        private string ReadUpdateSummary()
        {
            var summary = loggerMock.Invocations
                .Where(i => i.Arguments[0] is LogLevel.Information)
                .Select(i => i.Arguments[2]?.ToString() ?? string.Empty)
                .SingleOrDefault(message => message.Contains("Update completed", StringComparison.Ordinal));

            Assert.That(summary, Is.Not.Null, "The forecast wrote no summary line.");

            return summary!;
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
            return new ForecastUpdater(loggerMock.Object, ServiceScopeFactory, updateQueueService, domainEventDispatcherMock.Object, statusStore, cancellationContext, Time);
        }

        private UpdateQueueService CreateRealUpdateQueue()
        {
            var hubContextMock = new Mock<IHubContext<UpdateNotificationHub>>();
            hubContextMock.Setup(hub => hub.Clients.Group(It.IsAny<string>())).Returns(Mock.Of<IClientProxy>());

            return new UpdateQueueService(
                Mock.Of<ILogger<UpdateQueueService>>(),
                hubContextMock.Object,
                new UpdateSubstrate(inProcessUpdateStatusStore, new InProcessUpdateExecutionLock(), new InProcessUpdateCompletionNotifier(), new InProcessUpdateCancellationNotifier()),
                ServiceScopeFactory,
                new DatabaseMaintenanceGate(inProcessUpdateStatusStore),
                new WriteBackRoundContext(),
                new UpdateCancellationContext());
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
            var portfolio = CreatePortfolio(Now, features);

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
