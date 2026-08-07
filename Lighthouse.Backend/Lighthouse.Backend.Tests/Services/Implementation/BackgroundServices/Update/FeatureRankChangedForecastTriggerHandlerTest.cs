using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices.Update
{
    /// <summary>
    /// A Feature that changed places changed every date its Portfolios show (ADR-133). Which Portfolios
    /// those are is the handler's question, not the mover's — the move command carries an identity and
    /// nothing else.
    /// </summary>
    public class FeatureRankChangedForecastTriggerHandlerTest
    {
        private Mock<IRepository<Feature>> featureRepositoryMock;
        private Mock<IForecastUpdater> forecastUpdaterMock;

        [SetUp]
        public void SetUp()
        {
            featureRepositoryMock = new Mock<IRepository<Feature>>();
            forecastUpdaterMock = new Mock<IForecastUpdater>();
        }

        [Test]
        public async Task HandleAsync_AFeatureTwoPortfoliosShare_RefreshesBoth()
        {
            GivenTheMovedFeatureBelongsTo(7, 1, 2);

            await WhenTheFeatureChangedPlaces(7);

            using (Assert.EnterMultipleScope())
            {
                forecastUpdaterMock.Verify(u => u.TriggerUpdate(1), Times.Once);
                forecastUpdaterMock.Verify(u => u.TriggerUpdate(2), Times.Once);
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

            forecastUpdaterMock.Verify(u => u.TriggerUpdate(It.IsAny<int>()), Times.Never);
        }

        [Test]
        public async Task HandleAsync_AFeatureInNoPortfolio_RefreshesNothing()
        {
            GivenTheMovedFeatureBelongsTo(7);

            await WhenTheFeatureChangedPlaces(7);

            forecastUpdaterMock.Verify(u => u.TriggerUpdate(It.IsAny<int>()), Times.Never);
        }

        private void GivenTheMovedFeatureBelongsTo(int featureId, params int[] portfolioIds)
        {
            var feature = new Feature { Id = featureId };
            feature.Portfolios.AddRange(portfolioIds.Select(id => new Portfolio { Id = id }));

            featureRepositoryMock.Setup(r => r.GetById(featureId)).Returns(feature);
        }

        private Task WhenTheFeatureChangedPlaces(int featureId)
        {
            var subject = new FeatureRankChangedForecastTriggerHandler(
                featureRepositoryMock.Object,
                forecastUpdaterMock.Object);

            return subject.HandleAsync(new FeatureRankChanged(featureId), CancellationToken.None);
        }
    }
}
