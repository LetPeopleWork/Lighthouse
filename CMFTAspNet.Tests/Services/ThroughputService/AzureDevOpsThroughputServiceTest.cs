using CMFTAspNet.Models;
using CMFTAspNet.Models.Teams;
using CMFTAspNet.Services.AzureDevOps;
using CMFTAspNet.Services.ThroughputService;
using Moq;

namespace CMFTAspNet.Tests.Services.ThroughputService
{
    public class AzureDevOpsThroughputServiceTest
    {
        [Test]
        public async Task GetThroughput_GetsClosedItemsFromAzureDevops()
        {
            int[] closedItemsPerDay = [0, 0, 1, 3, 12, 3, 0];
            var azureDevOpsWorkItemServiceMock = new Mock<IAzureDevOpsWorkItemService>();

            var teamConfiguration = new AzureDevOpsTeamConfiguration();
            var team = new Team(1);
            team.UpdateTeamConfiguration(teamConfiguration);

            azureDevOpsWorkItemServiceMock.Setup(x => x.GetClosedWorkItemsForTeam(teamConfiguration, 7)).Returns(Task.FromResult(closedItemsPerDay));

            var subject = new AzureDevOpsThroughputService(team, azureDevOpsWorkItemServiceMock.Object);

            await subject.UpdateThroughput(7);

            Assert.That(team.Throughput.History, Is.EqualTo(closedItemsPerDay.Length));
            for (var index = 0; index <  closedItemsPerDay.Length; index++)
            {
                Assert.That(team.Throughput.GetThroughputOnDay(index), Is.EqualTo(closedItemsPerDay[index]));
            }
        }
    }
}
