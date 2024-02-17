using CMFTAspNet.Models.Teams;
using CMFTAspNet.Services.Factories;
using CMFTAspNet.Services.Implementation;

namespace CMFTAspNet.Tests.Services.Factories
{
    public class ThroughputServiceFactoryTest
    {
        [Test]
        public void CreateThroughputServiceFactoryForTeam_TeamConfigIsDefault_ThrowsNotSupportedException()
        {
            var subject = new ThroughputServiceFactory(new WorkItemServiceFactory());
            var team = new Team("Team", 1);

            Assert.Throws<NotSupportedException>(() => subject.CreateThroughputServiceForTeam(team));
        }

        [Test]
        public void CreateThroughputServiceFactoryForTeam_AzureDevOpsTeamConfiguration_CreatesAzureDevOpsThroughputService()
        {
            var subject = new ThroughputServiceFactory(new WorkItemServiceFactory());
            var team = new Team("Team", 1);
            team.UpdateTeamConfiguration(new AzureDevOpsTeamConfiguration());

            var azureDevOpsThroughputService = subject.CreateThroughputServiceForTeam(team);

            Assert.That(azureDevOpsThroughputService, Is.InstanceOf<ThroughputService>());
        }
    }
}
