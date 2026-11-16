using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Modules.WorkItems.Application;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Modules.WorkItems.Application
{
    public class TeamDataServiceTest
    {
        private Mock<IWorkItemService> workItemServiceMock;
        private Mock<IDomainEventDispatcher> domainEventDispatcherMock;

        private int idCounter = 0;

        [SetUp]
        public void Setup()
        {
            workItemServiceMock = new Mock<IWorkItemService>();
            domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();
            domainEventDispatcherMock
                .Setup(x => x.PublishAsync(It.IsAny<TeamDataRefreshed>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        [Test]
        public async Task ExecuteAsync_UpdatesWorkItemsForTeam()
        {
            var team = CreateTeam();

            var subject = CreateSubject();

            await subject.UpdateTeamData(team);

            workItemServiceMock.Verify(x => x.UpdateWorkItemsForTeam(team), Times.Once);
        }

        [Test]
        public async Task ExecuteAsync_PublishesTeamDataRefreshedForTeam()
        {
            var team = CreateTeam();

            var subject = CreateSubject();

            await subject.UpdateTeamData(team);

            domainEventDispatcherMock.Verify(
                x => x.PublishAsync(It.Is<TeamDataRefreshed>(e => e.TeamId == team.Id), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        private Team CreateTeam()
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
            };
        }

        private TeamDataService CreateSubject()
        {
            return new TeamDataService(Mock.Of<ILogger<TeamDataService>>(), workItemServiceMock.Object, domainEventDispatcherMock.Object);
        }
    }
}
