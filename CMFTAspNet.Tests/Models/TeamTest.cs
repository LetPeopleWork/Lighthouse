using CMFTAspNet.Models;
using CMFTAspNet.Models.Teams;

namespace CMFTAspNet.Tests.Models
{
    public class TeamTest
    {
        [Test]
        public void UpdateThroughput_SetsThroughput()
        {
            var team = new Team("Team", 1);

            var throughput = new Throughput([1, 3, 0, 0, 0, 1, 3]);

            team.UpdateThroughput(throughput);

            Assert.That(team.Throughput, Is.EqualTo(throughput));
        }

        [Test]
        public void CreateNewTeam_InitializesWithDefaultConfig()
        {
            var team = new Team("Team", 1);

            Assert.That(team.TeamConfiguration, Is.InstanceOf<DefaultTeamConfiguration>());
        }

        [Test]
        public void UpdateTeamConfiguration_SetsNewTeamConfiguration()
        {
            var team = new Team("Team", 1);

            var azureDevOpsTeamConfig = new AzureDevOpsTeamConfiguration();

            team.UpdateTeamConfiguration(azureDevOpsTeamConfig);

            Assert.That(team.TeamConfiguration, Is.EqualTo(azureDevOpsTeamConfig));
        }
    }
}
