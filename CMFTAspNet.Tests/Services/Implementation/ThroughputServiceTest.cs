using CMFTAspNet.Models.Teams;
using CMFTAspNet.Services.Implementation;
using CMFTAspNet.Services.Interfaces;
using Moq;

namespace CMFTAspNet.Tests.Services.Implementation
{
    public class ThroughputServiceTest
    {
        [Test]
        public async Task GetThroughput_GetsClosedItemsFromAzureDevops()
        {
            int[] closedItemsPerDay = [0, 0, 1, 3, 12, 3, 0];
            var workItemServiceMock = new Mock<IWorkItemService>();

            var teamConfiguration = new AzureDevOpsTeamConfiguration();
            var team = new Team("Team", 1);
            team.UpdateTeamConfiguration(teamConfiguration);

            workItemServiceMock.Setup(x => x.GetClosedWorkItemsForTeam(7, teamConfiguration)).Returns(Task.FromResult(closedItemsPerDay));

            var subject = new ThroughputService(team, workItemServiceMock.Object);

            await subject.UpdateThroughput(7);

            Assert.That(team.Throughput.History, Is.EqualTo(closedItemsPerDay.Length));
            for (var index = 0; index < closedItemsPerDay.Length; index++)
            {
                Assert.That(team.Throughput.GetThroughputOnDay(index), Is.EqualTo(closedItemsPerDay[index]));
            }
        }
    }
}
