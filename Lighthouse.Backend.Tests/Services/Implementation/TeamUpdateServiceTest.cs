using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.WorkTracking;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class TeamUpdateServiceTest
    {
        private Mock<IWorkItemService> workItemServiceMock = new Mock<IWorkItemService>();
        private Mock<IWorkItemServiceFactory> workItemServiceFactoryMock;

        private Team team;

        [SetUp]
        public void Setup()
        {
            workItemServiceMock = new Mock<IWorkItemService>();
            workItemServiceFactoryMock = new Mock<IWorkItemServiceFactory>();

            workItemServiceFactoryMock.Setup(x => x.GetWorkItemServiceForWorkTrackingSystem(It.IsAny<WorkTrackingSystems>())).Returns(workItemServiceMock.Object);

            team = new Team
            {
                Name = "Team",
                ThroughputHistory = 7,
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    WorkTrackingSystem = WorkTrackingSystems.AzureDevOps
                },
            };
        }

        [Test]
        public async Task UpdateTeam_GetsClosedItemsFromWorkItemService_UpdatesThroughput()
        {
            int[] closedItemsPerDay = [0, 0, 1, 3, 12, 3, 0];
            workItemServiceMock.Setup(x => x.GetClosedWorkItems(7, team)).Returns(Task.FromResult(closedItemsPerDay));

            var subject = CreateSubject();

            await subject.UpdateTeam(team);

            Assert.Multiple(() =>
            {
                Assert.That(team.Throughput.History, Is.EqualTo(closedItemsPerDay.Length));
                for (var index = 0; index < closedItemsPerDay.Length; index++)
                {
                    Assert.That(team.Throughput.GetThroughputOnDay(index), Is.EqualTo(closedItemsPerDay[index]));
                }
            });
        }

        [Test]
        [TestCase(new[] { "12" }, 1)]
        [TestCase(new[] { "" }, 1)]
        [TestCase(new[] { "12", "12" }, 1)]
        [TestCase(new[] { "12", "" }, 2)]
        [TestCase(new[] { "12", "123", "34", "231324" }, 4)]
        [TestCase(new string[0] , 0)]
        public async Task UpdateTeam_GetsOpenItemsFromWorkItemService_SetsActualWip(string[] inProgressFeatures, int expectedActaulFeatureWIP)
        {
            workItemServiceMock.Setup(x => x.GetFeaturesInProgressForTeam(team)).ReturnsAsync(inProgressFeatures);

            var subject = CreateSubject();

            await subject.UpdateTeam(team);

            Assert.That(team.ActualFeatureWIP, Is.EqualTo(expectedActaulFeatureWIP));
        }

        private TeamUpdateService CreateSubject()
        {
            return new TeamUpdateService(workItemServiceFactoryMock.Object, Mock.Of<ILogger<TeamUpdateService>>());
        }
    }
}
