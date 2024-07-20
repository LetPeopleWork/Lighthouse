using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.WorkTracking;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class ThroughputServiceTest
    {
        [Test]
        public async Task GetThroughput_GetsClosedItemsFromAzureDevops()
        {
            int[] closedItemsPerDay = [0, 0, 1, 3, 12, 3, 0];

            var workItemServiceFactoryMock = new Mock<IWorkItemServiceFactory>();
            var workItemServiceMock = new Mock<IWorkItemService>();

            workItemServiceFactoryMock.Setup(x => x.GetWorkItemServiceForWorkTrackingSystem(It.IsAny<WorkTrackingSystems>())).Returns(workItemServiceMock.Object);
            
            var team = new Team
            {
                Name = "Team",
                ThroughputHistory = 7,
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.AzureDevOps },
            };

            workItemServiceMock.Setup(x => x.GetClosedWorkItems(7, team)).Returns(Task.FromResult(closedItemsPerDay));

            var subject = new ThroughputService(workItemServiceFactoryMock.Object, Mock.Of<ILogger<ThroughputService>>());

            await subject.UpdateThroughputForTeam(team);

            Assert.That(team.Throughput.History, Is.EqualTo(closedItemsPerDay.Length));
            for (var index = 0; index < closedItemsPerDay.Length; index++)
            {
                Assert.That(team.Throughput.GetThroughputOnDay(index), Is.EqualTo(closedItemsPerDay[index]));
            }
        }
    }
}
