using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices.Update
{
    public class TeamDataRefreshedForecastTriggerHandlerTest
    {
        private Mock<IPortfolioRepository> portfolioRepositoryMock;
        private Mock<IForecastUpdater> forecastUpdaterMock;

        private int idCounter;

        [SetUp]
        public void Setup()
        {
            portfolioRepositoryMock = new Mock<IPortfolioRepository>();
            forecastUpdaterMock = new Mock<IForecastUpdater>();
        }

        [Test]
        public async Task HandleAsync_TeamWorksOnFeaturesOfMultiplePortfolios_TriggersForecastForEachPortfolio()
        {
            var teamId = NextTeamId();
            SetupPortfoliosForTeam(teamId, 1, 2);

            var subject = CreateSubject();

            await subject.HandleAsync(new TeamDataRefreshed(teamId), CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                forecastUpdaterMock.Verify(x => x.TriggerUpdate(1), Times.Once);
                forecastUpdaterMock.Verify(x => x.TriggerUpdate(2), Times.Once);
                forecastUpdaterMock.Verify(x => x.TriggerUpdate(It.IsAny<int>()), Times.Exactly(2));
            }
        }

        /// <summary>
        /// A team that has been deleted and a team that works on nothing are the same case here: the
        /// handler asks which portfolios the team works for and forwards the answer, so an empty answer
        /// is all either one can produce.
        /// </summary>
        [Test]
        public async Task HandleAsync_TeamWorksOnNoPortfoliosFeatures_DoesNotTriggerForecastUpdate()
        {
            var teamId = NextTeamId();
            SetupPortfoliosForTeam(teamId);

            var subject = CreateSubject();

            await subject.HandleAsync(new TeamDataRefreshed(teamId), CancellationToken.None);

            forecastUpdaterMock.Verify(x => x.TriggerUpdate(It.IsAny<int>()), Times.Never);
        }

        private void SetupPortfoliosForTeam(int teamId, params int[] portfolioIds)
        {
            portfolioRepositoryMock.Setup(x => x.GetPortfolioIdsForTeam(teamId)).Returns(portfolioIds);
        }

        private int NextTeamId()
        {
            return idCounter++;
        }

        private TeamDataRefreshedForecastTriggerHandler CreateSubject()
        {
            return new TeamDataRefreshedForecastTriggerHandler(portfolioRepositoryMock.Object, forecastUpdaterMock.Object);
        }
    }
}
