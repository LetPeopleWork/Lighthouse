using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices.Update
{
    /// <summary>
    /// The forecast draws from the order, so changing who owns it changes every date (ADR-133). Without
    /// this handler the places move and the dates do not, which is the one failure mode indistinguishable
    /// from success on a feature whose promise is "the forecast follows your priority".
    /// </summary>
    public class FeatureOrderingPolicyChangedForecastTriggerHandlerTest
    {
        private Mock<IRepository<Portfolio>> portfolioRepositoryMock;
        private Mock<IForecastUpdater> forecastUpdaterMock;

        [SetUp]
        public void SetUp()
        {
            portfolioRepositoryMock = new Mock<IRepository<Portfolio>>();
            forecastUpdaterMock = new Mock<IForecastUpdater>();
        }

        [Test]
        public async Task HandleAsync_GivingTheOrderBack_RefreshesEveryPortfoliosForecast()
        {
            GivenThePortfolios(1, 2, 3);

            await WhenTheOrderChangesTo(FeatureOrderingPolicy.SourceOrder);

            using (Assert.EnterMultipleScope())
            {
                forecastUpdaterMock.Verify(u => u.TriggerUpdate(1), Times.Once);
                forecastUpdaterMock.Verify(u => u.TriggerUpdate(2), Times.Once);
                forecastUpdaterMock.Verify(u => u.TriggerUpdate(3), Times.Once);
            }
        }

        // SA-16 offered to skip this one, on the grounds that seeding cannot move anybody. That only holds
        // the first time - taking the order over again after the tracker re-ranked (AC-5.3) moves plenty.
        [Test]
        public async Task HandleAsync_TakingTheOrderOver_AlsoRefreshesEveryPortfoliosForecast()
        {
            GivenThePortfolios(1, 2);

            await WhenTheOrderChangesTo(FeatureOrderingPolicy.ManualOrder);

            using (Assert.EnterMultipleScope())
            {
                forecastUpdaterMock.Verify(u => u.TriggerUpdate(1), Times.Once);
                forecastUpdaterMock.Verify(u => u.TriggerUpdate(2), Times.Once);
            }
        }

        [Test]
        public async Task HandleAsync_AnInstanceWithNoPortfolios_RefreshesNothing()
        {
            GivenThePortfolios();

            await WhenTheOrderChangesTo(FeatureOrderingPolicy.ManualOrder);

            forecastUpdaterMock.Verify(u => u.TriggerUpdate(It.IsAny<int>()), Times.Never);
        }

        private void GivenThePortfolios(params int[] portfolioIds)
        {
            var portfolios = portfolioIds.Select(id => new Portfolio { Id = id }).ToList();
            portfolioRepositoryMock.Setup(r => r.GetAll()).Returns(portfolios);
        }

        private Task WhenTheOrderChangesTo(FeatureOrderingPolicy policy)
        {
            var subject = new FeatureOrderingPolicyChangedForecastTriggerHandler(
                portfolioRepositoryMock.Object,
                forecastUpdaterMock.Object);

            return subject.HandleAsync(new FeatureOrderingPolicyChanged(policy), CancellationToken.None);
        }
    }
}
