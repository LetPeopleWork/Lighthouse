using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.WorkItems.Contracts;
using Lighthouse.Backend.Modules.FlowMetrics.Application.Policies;
using Lighthouse.Backend.Modules.FlowMetrics.Contracts;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Moq;

namespace Lighthouse.Backend.Tests.Modules.FlowMetrics.Application.Policies
{
    public class TeamDataRefreshedTeamMetricsUpdateHandlerTest
    {
        private Mock<IRepository<Team>> teamRepositoryMock;
        private Mock<ITeamMetricsService> teamMetricsServiceMock;

        [SetUp]
        public void SetUp()
        {
            teamRepositoryMock = new Mock<IRepository<Team>>();
            teamMetricsServiceMock = new Mock<ITeamMetricsService>();
        }

        [Test]
        public async Task HandleAsync_UpdatesMetricsForTheTeamCarriedByTheEvent()
        {
            var team = new Team { Id = 12, Name = "Team" };
            teamRepositoryMock.Setup(x => x.GetById(team.Id)).Returns(team);

            var subject = CreateSubject();
            await subject.HandleAsync(new TeamDataRefreshed(team.Id), CancellationToken.None);

            teamMetricsServiceMock.Verify(x => x.UpdateTeamMetrics(team), Times.Once);
        }

        [Test]
        public async Task HandleAsync_TeamNoLongerExists_DoesNotUpdateMetrics()
        {
            teamRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).Returns((Team?)null);

            var subject = CreateSubject();
            await subject.HandleAsync(new TeamDataRefreshed(99), CancellationToken.None);

            teamMetricsServiceMock.Verify(x => x.UpdateTeamMetrics(It.IsAny<Team>()), Times.Never);
        }

        private TeamDataRefreshedTeamMetricsUpdateHandler CreateSubject()
        {
            return new TeamDataRefreshedTeamMetricsUpdateHandler(teamRepositoryMock.Object, teamMetricsServiceMock.Object);
        }
    }
}
