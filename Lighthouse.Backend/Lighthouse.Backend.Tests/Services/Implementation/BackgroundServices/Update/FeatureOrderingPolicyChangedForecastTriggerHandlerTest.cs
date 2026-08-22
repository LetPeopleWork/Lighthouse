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
    /// The simulation draws each day's throughput from the first few Features in order, so handing the
    /// order over - or giving it back - moves every date. Without this handler the places move and the
    /// dates do not, which is the one failure mode indistinguishable from success on a feature whose
    /// promise is "the forecast follows your priority".
    /// </summary>
    public class FeatureOrderingPolicyChangedForecastTriggerHandlerTest : UpdateServiceTestBase
    {
        private const int SiblingTeamId = 42;

        private Mock<IRepository<Portfolio>> portfolioRepositoryMock;
        private Mock<IForecastUpdater> forecastUpdaterMock;
        private Mock<IForecastService> forecastServiceMock;
        private Mock<IUpdateStatusStore> updateStatusStoreMock;
        private Mock<IDomainEventDispatcher> domainEventDispatcherMock;

        [SetUp]
        public void SetUp()
        {
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
        public async Task HandleAsync_GivingTheOrderBack_RefreshesEveryPortfoliosForecast()
        {
            GivenThePortfolios(1, 2, 3);

            await WhenTheOrderChangesTo(FeatureOrderingPolicy.SourceOrder);

            using (Assert.EnterMultipleScope())
            {
                forecastUpdaterMock.Verify(u => u.TriggerImmediateUpdate(1), Times.Once);
                forecastUpdaterMock.Verify(u => u.TriggerImmediateUpdate(2), Times.Once);
                forecastUpdaterMock.Verify(u => u.TriggerImmediateUpdate(3), Times.Once);
            }
        }

        // Skipping this one looks safe, because seeding the ranks from the sequence already on screen
        // cannot move anybody. That only holds the first time - taking the order over again, after the
        // work tracking system has re-ranked everything underneath, moves plenty.
        [Test]
        public async Task HandleAsync_TakingTheOrderOver_AlsoRefreshesEveryPortfoliosForecast()
        {
            GivenThePortfolios(1, 2);

            await WhenTheOrderChangesTo(FeatureOrderingPolicy.ManualOrder);

            using (Assert.EnterMultipleScope())
            {
                forecastUpdaterMock.Verify(u => u.TriggerImmediateUpdate(1), Times.Once);
                forecastUpdaterMock.Verify(u => u.TriggerImmediateUpdate(2), Times.Once);
            }
        }

        [Test]
        public async Task HandleAsync_AnInstanceWithNoPortfolios_RefreshesNothing()
        {
            GivenThePortfolios();

            await WhenTheOrderChangesTo(FeatureOrderingPolicy.ManualOrder);

            forecastUpdaterMock.Verify(u => u.TriggerImmediateUpdate(It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// A person changed the order and is watching for the dates to move, so this must not queue up
        /// behind a refresh they did not ask for. The real updater is used rather than a stand-in: with a
        /// sibling Team of the same Portfolio still queued, the ordinary route parks the forecast until
        /// that Team lands, and only the immediate route runs it now.
        /// </summary>
        [Test]
        public async Task HandleAsync_AChangeToTheOrderOfFeatures_ForecastsStraightAwayEvenMidRefresh()
        {
            var portfolio = APortfolioWorkedOnBy(new Team { Name = "Sibling Team", Id = SiblingTeamId });
            GivenTheOnlyPortfolioIs(portfolio);
            GivenTheSiblingTeamStandsQueued();

            await WhenTheOrderChangesTo(FeatureOrderingPolicy.ManualOrder, ARealForecastUpdater());

            forecastServiceMock.Verify(f => f.UpdateForecastsForPortfolio(portfolio), Times.Once);
        }

        private void GivenThePortfolios(params int[] portfolioIds)
        {
            var portfolios = portfolioIds.Select(id => new Portfolio { Id = id }).ToList();
            portfolioRepositoryMock.Setup(r => r.GetAll()).Returns(portfolios);
        }

        private void GivenTheOnlyPortfolioIs(Portfolio portfolio)
        {
            portfolioRepositoryMock.Setup(r => r.GetAll()).Returns([portfolio]);
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

        private Task WhenTheOrderChangesTo(FeatureOrderingPolicy policy)
        {
            return WhenTheOrderChangesTo(policy, forecastUpdaterMock.Object);
        }

        private Task WhenTheOrderChangesTo(FeatureOrderingPolicy policy, IForecastUpdater forecastUpdater)
        {
            var subject = new FeatureOrderingPolicyChangedForecastTriggerHandler(
                portfolioRepositoryMock.Object,
                forecastUpdater);

            return subject.HandleAsync(new FeatureOrderingPolicyChanged(policy), CancellationToken.None);
        }
    }
}
