using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices.Update
{
    /// <summary>
    /// A Feature that changed places changed every date its Portfolios show, because the simulation draws
    /// each day's throughput from the first few Features in order. Which Portfolios those are is the
    /// handler's question, not the mover's — the move command carries an identity and nothing else.
    /// </summary>
    public class FeatureRankChangedForecastTriggerHandlerTest : UpdateServiceTestBase
    {
        private const int SiblingTeamId = 42;

        private Mock<IRepository<Feature>> featureRepositoryMock;
        private Mock<IRepository<Portfolio>> portfolioRepositoryMock;
        private Mock<IForecastUpdater> forecastUpdaterMock;
        private Mock<IForecastService> forecastServiceMock;
        private Mock<IUpdateStatusStore> updateStatusStoreMock;
        private Mock<IDomainEventDispatcher> domainEventDispatcherMock;

        [SetUp]
        public void SetUp()
        {
            featureRepositoryMock = new Mock<IRepository<Feature>>();
            portfolioRepositoryMock = new Mock<IRepository<Portfolio>>();
            forecastUpdaterMock = new Mock<IForecastUpdater>();
            forecastServiceMock = new Mock<IForecastService>();
            updateStatusStoreMock = new Mock<IUpdateStatusStore>();

            domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();
            domainEventDispatcherMock
                .Setup(d => d.PublishAsync(It.IsAny<PortfolioForecastsUpdated>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var refreshLogServiceMock = new Mock<IRefreshLogService>();
            refreshLogServiceMock.Setup(l => l.LogRefreshAsync(It.IsAny<RefreshLog>())).Returns(Task.CompletedTask);

            SetupServiceProviderMock(portfolioRepositoryMock.Object);
            SetupServiceProviderMock(forecastServiceMock.Object);
            SetupServiceProviderMock(refreshLogServiceMock.Object);
            SetupServiceProviderMock(Mock.Of<IWriteBackTriggerService>());
            SetupServiceProviderMock(Mock.Of<IAppSettingService>());
        }

        [Test]
        public async Task HandleAsync_AFeatureTwoPortfoliosShare_RefreshesBoth()
        {
            GivenTheMovedFeatureBelongsTo(7, 1, 2);

            await WhenTheFeatureChangedPlaces(7);

            using (Assert.EnterMultipleScope())
            {
                forecastUpdaterMock.Verify(u => u.TriggerImmediateUpdate(1), Times.Once);
                forecastUpdaterMock.Verify(u => u.TriggerImmediateUpdate(2), Times.Once);
            }
        }

        // The move is committed before the event is published, so the Feature can be gone by the time the
        // handler reads it — a delete racing a move, or a sync that removed an orphan. Falling over here
        // would be swallowed by the dispatcher and look exactly like a forecast that quietly went stale.
        [Test]
        public async Task HandleAsync_TheFeatureIsGoneByTheTimeTheHandlerLooks_RefreshesNothingAndDoesNotThrow()
        {
            featureRepositoryMock.Setup(r => r.GetById(It.IsAny<int>())).Returns((Feature)null!);

            await WhenTheFeatureChangedPlaces(7);

            forecastUpdaterMock.Verify(u => u.TriggerImmediateUpdate(It.IsAny<int>()), Times.Never);
        }

        [Test]
        public async Task HandleAsync_AFeatureInNoPortfolio_RefreshesNothing()
        {
            GivenTheMovedFeatureBelongsTo(7);

            await WhenTheFeatureChangedPlaces(7);

            forecastUpdaterMock.Verify(u => u.TriggerImmediateUpdate(It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// A person dragged the Feature and is watching for the dates to move, so this must not queue up
        /// behind a refresh they did not ask for. The real updater is used rather than a stand-in: with a
        /// sibling Team of the same Portfolio still queued, the ordinary route parks the forecast until
        /// that Team lands, and only the immediate route runs it now.
        /// </summary>
        [Test]
        public async Task HandleAsync_AFeatureMovedByHand_ForecastsStraightAwayEvenMidRefresh()
        {
            var portfolio = APortfolioWorkedOnBy(new Team { Name = "Sibling Team", Id = SiblingTeamId });
            GivenTheMovedFeatureBelongsToThe(portfolio);
            GivenTheSiblingTeamStandsQueued();

            await WhenTheFeatureChangedPlaces(1, ARealForecastUpdater());

            forecastServiceMock.Verify(f => f.UpdateForecastsForPortfolio(portfolio), Times.Once);
        }

        private void GivenTheMovedFeatureBelongsTo(int featureId, params int[] portfolioIds)
        {
            var feature = new Feature { Id = featureId };
            feature.Portfolios.AddRange(portfolioIds.Select(id => new Portfolio { Id = id }));

            featureRepositoryMock.Setup(r => r.GetById(featureId)).Returns(feature);
        }

        private void GivenTheMovedFeatureBelongsToThe(Portfolio portfolio)
        {
            var feature = portfolio.Features[0];
            feature.Portfolios.Add(portfolio);

            featureRepositoryMock.Setup(r => r.GetById(feature.Id)).Returns(feature);
            portfolioRepositoryMock.Setup(r => r.GetById(portfolio.Id)).Returns(portfolio);
        }

        private void GivenTheSiblingTeamStandsQueued()
        {
            updateStatusStoreMock
                .Setup(s => s.HasQueuedWork(It.Is<IReadOnlyCollection<UpdateKey>>(
                    keys => keys.Contains(new UpdateKey(UpdateType.Team, SiblingTeamId)))))
                .Returns(true);
        }

        private static Portfolio APortfolioWorkedOnBy(Team team)
        {
            var feature = new Feature { Name = "Feature", Id = 1 };
            feature.FeatureWork.Add(new FeatureWork(team, 3, 5, feature));

            var portfolio = new Portfolio { Name = "Portfolio", Id = 1, UpdateTime = DateTime.UtcNow };
            portfolio.UpdateFeatures([feature]);

            return portfolio;
        }

        private ForecastUpdater ARealForecastUpdater()
        {
            return new ForecastUpdater(
                Mock.Of<ILogger<ForecastUpdater>>(),
                ServiceScopeFactory,
                UpdateQueueService,
                domainEventDispatcherMock.Object,
                updateStatusStoreMock.Object);
        }

        private Task WhenTheFeatureChangedPlaces(int featureId)
        {
            return WhenTheFeatureChangedPlaces(featureId, forecastUpdaterMock.Object);
        }

        private Task WhenTheFeatureChangedPlaces(int featureId, IForecastUpdater forecastUpdater)
        {
            var subject = new FeatureRankChangedForecastTriggerHandler(
                featureRepositoryMock.Object,
                forecastUpdater);

            return subject.HandleAsync(new FeatureRankChanged(featureId), CancellationToken.None);
        }
    }
}
