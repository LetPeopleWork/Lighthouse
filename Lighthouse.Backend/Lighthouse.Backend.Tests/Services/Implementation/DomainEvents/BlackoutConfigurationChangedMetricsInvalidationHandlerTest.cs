using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.DomainEvents;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.DomainEvents
{
    public class BlackoutConfigurationChangedMetricsInvalidationHandlerTest
    {
        private Mock<IRepository<Team>> teamRepositoryMock;
        private Mock<IRepository<Portfolio>> portfolioRepositoryMock;
        private Mock<ITeamMetricsService> teamMetricsServiceMock;
        private Mock<IPortfolioMetricsService> portfolioMetricsServiceMock;

        [SetUp]
        public void SetUp()
        {
            teamRepositoryMock = new Mock<IRepository<Team>>();
            portfolioRepositoryMock = new Mock<IRepository<Portfolio>>();
            teamMetricsServiceMock = new Mock<ITeamMetricsService>();
            portfolioMetricsServiceMock = new Mock<IPortfolioMetricsService>();

            teamRepositoryMock.Setup(x => x.GetAll()).Returns(Array.Empty<Team>().AsQueryable());
            portfolioRepositoryMock.Setup(x => x.GetAll()).Returns(Array.Empty<Portfolio>().AsQueryable());
        }

        [Test]
        public async Task HandleAsync_InvalidatesMetricsForEveryTeam()
        {
            var teamA = new Team { Id = 1, Name = "Alpha" };
            var teamB = new Team { Id = 2, Name = "Beta" };
            teamRepositoryMock.Setup(x => x.GetAll()).Returns(new[] { teamA, teamB }.AsQueryable());

            var subject = CreateSubject();
            await subject.HandleAsync(new BlackoutConfigurationChanged(), CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                teamMetricsServiceMock.Verify(x => x.InvalidateTeamMetrics(teamA), Times.Once);
                teamMetricsServiceMock.Verify(x => x.InvalidateTeamMetrics(teamB), Times.Once);
            }
        }

        [Test]
        public async Task HandleAsync_InvalidatesMetricsForEveryPortfolio()
        {
            var portfolioA = new Portfolio { Id = 10, Name = "Release 1" };
            var portfolioB = new Portfolio { Id = 11, Name = "Release 2" };
            portfolioRepositoryMock.Setup(x => x.GetAll()).Returns(new[] { portfolioA, portfolioB }.AsQueryable());

            var subject = CreateSubject();
            await subject.HandleAsync(new BlackoutConfigurationChanged(), CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                portfolioMetricsServiceMock.Verify(x => x.InvalidatePortfolioMetrics(portfolioA), Times.Once);
                portfolioMetricsServiceMock.Verify(x => x.InvalidatePortfolioMetrics(portfolioB), Times.Once);
            }
        }

        [Test]
        public async Task HandleAsync_NoTeamsOrPortfolios_DoesNotInvalidate()
        {
            var subject = CreateSubject();
            await subject.HandleAsync(new BlackoutConfigurationChanged(), CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                teamMetricsServiceMock.Verify(x => x.InvalidateTeamMetrics(It.IsAny<Team>()), Times.Never);
                portfolioMetricsServiceMock.Verify(x => x.InvalidatePortfolioMetrics(It.IsAny<Portfolio>()), Times.Never);
            }
        }

        private BlackoutConfigurationChangedMetricsInvalidationHandler CreateSubject()
        {
            return new BlackoutConfigurationChangedMetricsInvalidationHandler(
                teamRepositoryMock.Object,
                portfolioRepositoryMock.Object,
                teamMetricsServiceMock.Object,
                portfolioMetricsServiceMock.Object);
        }
    }
}
