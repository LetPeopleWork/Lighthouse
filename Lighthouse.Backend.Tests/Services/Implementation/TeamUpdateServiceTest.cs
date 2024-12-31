using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.Update;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestHelpers;
using Lighthouse.Backend.WorkTracking;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class TeamUpdateServiceTest : UpdateServiceTestBase
    {
        private Mock<IWorkItemService> workItemServiceMock = new Mock<IWorkItemService>();
        private Team team;

        [SetUp]
        public void Setup()
        {
            workItemServiceMock = new Mock<IWorkItemService>();

            var workItemServiceFactoryMock = new Mock<IWorkItemServiceFactory>();
            workItemServiceFactoryMock.Setup(x => x.GetWorkItemServiceForWorkTrackingSystem(It.IsAny<WorkTrackingSystems>())).Returns(workItemServiceMock.Object);
            SetupServiceProviderMock(workItemServiceFactoryMock.Object);

            team = new Team
            {
                Id = 1886,
                Name = "Team",
                ThroughputHistory = 7,
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    WorkTrackingSystem = WorkTrackingSystems.AzureDevOps
                },
            };

            var repositoryMock = new Mock<IRepository<Team>>();
            repositoryMock.Setup(x => x.GetById(It.IsAny<int>())).Returns(team);
            SetupServiceProviderMock(repositoryMock.Object);
        }

        [Test]
        public void UpdateTeam_GetsClosedItemsFromWorkItemService_UpdatesThroughput()
        {
            int[] closedItemsPerDay = [0, 0, 1, 3, 12, 3, 0];
            workItemServiceMock.Setup(x => x.GetClosedWorkItems(7, team)).Returns(Task.FromResult(closedItemsPerDay));

            var subject = CreateSubject();

            subject.TriggerUpdate(team.Id);

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
        [TestCase(new[] { "12" }, new[] { "12" })]
        [TestCase(new[] { "" }, new[] { "" })]
        [TestCase(new[] { "12", "12" }, new[] { "12" })]
        [TestCase(new[] { "12", "" }, new[] { "12", "" })]
        [TestCase(new[] { "12", "123", "34", "231324" }, new[] { "12", "123", "34", "231324" })]
        [TestCase(new string[0], new string[0])]
        public void UpdateTeam_GetsOpenItemsFromWorkItemService_SetsActualWip(string[] inProgressFeatures, string[] expectedFeaturesInProgress)
        {
            workItemServiceMock.Setup(x => x.GetFeaturesInProgressForTeam(team)).ReturnsAsync(inProgressFeatures);

            var subject = CreateSubject();

            subject.TriggerUpdate(team.Id);

            Assert.That(team.FeaturesInProgress, Is.EquivalentTo(expectedFeaturesInProgress));
        }

        [Test]
        [TestCase(true, new[] { "13", "37", "42" }, 3)]
        [TestCase(false, new[] { "13", "37", "42" }, 2)]
        public void UpdateTeam_UpdatesFeatureWIP_DependingOnSetting(bool automaticallyAdjustFeatureWIP, string[] inProgressFeatures, int expectedFeatureWIP)
        {
            workItemServiceMock.Setup(x => x.GetFeaturesInProgressForTeam(team)).ReturnsAsync(inProgressFeatures);
            team.FeatureWIP = 2;
            team.AutomaticallyAdjustFeatureWIP = automaticallyAdjustFeatureWIP;

            var subject = CreateSubject();

            subject.TriggerUpdate(team.Id);

            Assert.That(team.FeatureWIP, Is.EqualTo(expectedFeatureWIP));
        }

        private TeamUpdateService CreateSubject()
        {
            return new TeamUpdateService(Mock.Of<ILogger<TeamUpdateService>>(), UpdateQueueService);
        }
    }
}
