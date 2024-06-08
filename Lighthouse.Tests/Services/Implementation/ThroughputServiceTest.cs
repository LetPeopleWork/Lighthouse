using Lighthouse.Models;
using Lighthouse.Services.Factories;
using Lighthouse.Services.Implementation;
using Lighthouse.Services.Interfaces;
using Lighthouse.WorkTracking;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Tests.Services.Implementation
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
                WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
            };

            workItemServiceMock.Setup(x => x.GetClosedWorkItems(7, team)).Returns(Task.FromResult(closedItemsPerDay));

            var subject = new ThroughputService(workItemServiceFactoryMock.Object, Mock.Of<ILogger<ThroughputService>>());

            await subject.UpdateThroughput(team);

            Assert.That(team.Throughput.History, Is.EqualTo(closedItemsPerDay.Length));
            for (var index = 0; index < closedItemsPerDay.Length; index++)
            {
                Assert.That(team.Throughput.GetThroughputOnDay(index), Is.EqualTo(closedItemsPerDay[index]));
            }
        }

        [Test]
        public async Task UpdateThroughput_UnknownWorkTrackingSystem_Throws()
        {
            var team = new Team
            {
                Name = "Team",
                ThroughputHistory = 7,
                WorkTrackingSystem = WorkTrackingSystems.Unknown,
            };

            var subject = new ThroughputService(Mock.Of<IWorkItemServiceFactory>(), Mock.Of<ILogger<ThroughputService>>());

            var exceptionThrown = false;
            try
            {
                await subject.UpdateThroughput(team);
            }
            catch (NotSupportedException)
            {
                exceptionThrown = true;
            }

            Assert.That(exceptionThrown, Is.True);
        }
    }
}
