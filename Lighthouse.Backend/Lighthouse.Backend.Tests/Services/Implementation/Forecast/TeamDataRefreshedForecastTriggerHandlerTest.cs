using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Forecast
{
    public class TeamDataRefreshedForecastTriggerHandlerTest
    {
        private Mock<IRepository<Team>> teamRepositoryMock;
        private Mock<IForecastUpdater> forecastUpdaterMock;

        private int idCounter;

        [SetUp]
        public void Setup()
        {
            teamRepositoryMock = new Mock<IRepository<Team>>();
            forecastUpdaterMock = new Mock<IForecastUpdater>();
        }

        [Test]
        public async Task HandleAsync_TeamPartOfMultiplePortfolios_TriggersForecastForEachPortfolio()
        {
            var team = CreateTeam();
            team.Portfolios.Add(new Portfolio { Id = 1, Name = "Project" });
            team.Portfolios.Add(new Portfolio { Id = 2, Name = "Project" });
            SetupTeam(team);

            var subject = CreateSubject();

            await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                forecastUpdaterMock.Verify(x => x.TriggerUpdate(1), Times.Once);
                forecastUpdaterMock.Verify(x => x.TriggerUpdate(2), Times.Once);
                forecastUpdaterMock.Verify(x => x.TriggerUpdate(It.IsAny<int>()), Times.Exactly(2));
            }
        }

        [Test]
        public async Task HandleAsync_TeamNotPartOfAnyPortfolio_DoesNotTriggerForecastUpdate()
        {
            var team = CreateTeam();
            SetupTeam(team);

            var subject = CreateSubject();

            await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);

            forecastUpdaterMock.Verify(x => x.TriggerUpdate(It.IsAny<int>()), Times.Never);
        }

        [Test]
        public async Task HandleAsync_TeamNoLongerExists_DoesNotTriggerForecastUpdate()
        {
            var subject = CreateSubject();

            await subject.HandleAsync(new TeamDataRefreshed(404), CancellationToken.None);

            forecastUpdaterMock.Verify(x => x.TriggerUpdate(It.IsAny<int>()), Times.Never);
        }

        private void SetupTeam(Team team)
        {
            teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(team);
        }

        private Team CreateTeam()
        {
            return new Team { Id = idCounter++, Name = "Team" };
        }

        private TeamDataRefreshedForecastTriggerHandler CreateSubject()
        {
            return new TeamDataRefreshedForecastTriggerHandler(teamRepositoryMock.Object, forecastUpdaterMock.Object);
        }
    }
}
