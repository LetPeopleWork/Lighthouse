using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

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

        private int idCounter = 0;

        private static readonly string[] WriteBackThenEventDispatchOrder = ["forecastWriteBack", "forecastsUpdatedEvent"];

        private const int SlowForecastMilliseconds = 60;

        private const int ShortestCredibleDurationMilliseconds = 25;

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
            portfolioRepositoryMock.Verify(x => x.GetById(It.IsAny<int>()), Times.Once);
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
            return new ForecastUpdater(Mock.Of<ILogger<ForecastUpdater>>(), ServiceScopeFactory, UpdateQueueService, domainEventDispatcherMock.Object);
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
