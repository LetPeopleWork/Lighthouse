using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.TeamData;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.TeamData
{
    public class TeamDataServiceTest
    {
        private Mock<ITeamMetricsService> teamMetricsServiceMock;
        private Mock<IWorkItemService> workItemServiceMock;
        private Mock<IDomainEventDispatcher> domainEventDispatcherMock;

        private int idCounter = 0;

        [SetUp]
        public void Setup()
        {
            teamMetricsServiceMock = new Mock<ITeamMetricsService>();
            workItemServiceMock = new Mock<IWorkItemService>();
            domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();
            domainEventDispatcherMock
                .Setup(x => x.PublishAsync(It.IsAny<TeamDataRefreshed>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
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
        public async Task ExecuteAsync_PublishesTeamDataRefreshedForTeam()
        {
            var team = CreateTeam(DateTime.Now.AddDays(-1));

            var subject = CreateSubject();

            await subject.UpdateTeamData(team);

            domainEventDispatcherMock.Verify(
                x => x.PublishAsync(It.Is<TeamDataRefreshed>(e => e.TeamId == team.Id), It.IsAny<CancellationToken>()),
                Times.Once);
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
            return new TeamDataService(Mock.Of<ILogger<TeamDataService>>(), teamMetricsServiceMock.Object, workItemServiceMock.Object, domainEventDispatcherMock.Object);
        }
    }
}
