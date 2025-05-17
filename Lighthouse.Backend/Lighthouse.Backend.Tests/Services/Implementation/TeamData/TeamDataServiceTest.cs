using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.TeamData;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.TeamData
{
    public class TeamDataServiceTest
    {
        private Mock<ITeamMetricsService> teamMetricsServiceMock;
        private Mock<IWorkItemService> workItemServiceMock;
        private Mock<IForecastUpdater> forecastUpdaterMock;

        private int idCounter = 0;

        [SetUp]
        public void Setup()
        {
            teamMetricsServiceMock = new Mock<ITeamMetricsService>();
            workItemServiceMock = new Mock<IWorkItemService>();
            forecastUpdaterMock = new Mock<IForecastUpdater>();
        }

        [Test]
        public async Task ExecuteAsync_UpdatesWorkItemsForTeam()
        {
            var team = CreateTeam(DateTime.Now.AddDays(-1));

            var subject = CreateSubject();

            await subject.UpdateTeamData(team);

            workItemServiceMock.Verify(x => x.UpdateWorkItemsForTeam(team), Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_TeamPartOfProject_TriggersForecastForProject()
        {
            var team = CreateTeam(DateTime.Now.AddDays(-1));
            team.Projects.Add(new Project { Id = 1, Name = "Project" });


            var subject = CreateSubject();

            await subject.UpdateTeamData(team);

            forecastUpdaterMock.Verify(x => x.TriggerUpdate(1), Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_TeamPartOfMultipleProjects_TriggersForecastForEachProject()
        {
            var team = CreateTeam(DateTime.Now.AddDays(-1));
            team.Projects.Add(new Project { Id = 1, Name = "Project" });
            team.Projects.Add(new Project { Id = 2, Name = "Project" });


            var subject = CreateSubject();

            await subject.UpdateTeamData(team);

            forecastUpdaterMock.Verify(x => x.TriggerUpdate(1), Times.Once);
            forecastUpdaterMock.Verify(x => x.TriggerUpdate(2), Times.Once);
            forecastUpdaterMock.Verify(x => x.TriggerUpdate(It.IsAny<int>()), Times.Exactly(2));
        }

        [Test]
        public async Task ExecuteAsync_TeamNotPartOfProject_DoesNotTriggerForecastUpdate()
        {
            var team = CreateTeam(DateTime.Now.AddDays(-1));

            var subject = CreateSubject();

            await subject.UpdateTeamData(team);

            forecastUpdaterMock.Verify(x => x.TriggerUpdate(It.IsAny<int>()), Times.Never);
        }

        private Team CreateTeam(DateTime lastThroughputUpdateTime)
        {
            return new Team
            {
                Id = idCounter++,
                Name = "Team",
                ThroughputHistory = 7,
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    WorkTrackingSystem = WorkTrackingSystems.AzureDevOps
                },
                UpdateTime = lastThroughputUpdateTime
            };
        }

        private TeamDataService CreateSubject()
        {
            return new TeamDataService(Mock.Of<ILogger<TeamDataService>>(), teamMetricsServiceMock.Object, workItemServiceMock.Object, forecastUpdaterMock.Object);
        }
    }
}
