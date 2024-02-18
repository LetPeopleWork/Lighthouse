using CMFTAspNet.Models.Teams;
using CMFTAspNet.Services.Factories;
using CMFTAspNet.Services.Implementation.AzureDevOps;

namespace CMFTAspNet.Tests.Factories
{
    public class WorkItemServiceFactoryTest
    {
        [Test]
        public void CreateWorkItemService_GivenAzureDevOpsConfiguration_ReturnsAzureDevOpsWorkItemService()
        {
            var teamConfiguration = new AzureDevOpsTeamConfiguration();
            var subject = new WorkItemServiceFactory();

            var workItemService = subject.CreateWorkItemServiceForTeam(teamConfiguration);

            Assert.That(workItemService, Is.InstanceOf<AzureDevOpsWorkItemService>());
        }

        [Test]
        public void CreateWorkItemService_CreateMultipleSerivcesForSameTeamConfiguration_ReturnsSameService()
        {
            var teamConfiguration = new AzureDevOpsTeamConfiguration();
            var subject = new WorkItemServiceFactory();

            var workItemService1 = subject.CreateWorkItemServiceForTeam(teamConfiguration);
            var workItemService2 = subject.CreateWorkItemServiceForTeam(teamConfiguration);

            Assert.That(workItemService2, Is.EqualTo(workItemService1));
        }

        [Test]
        public void CreateWorkItemService_CreateMultipleSerivcesForSameTypeOfTeamConfiguration_ReturnsSameService()
        {
            var subject = new WorkItemServiceFactory();

            var workItemService1 = subject.CreateWorkItemServiceForTeam(new AzureDevOpsTeamConfiguration());
            var workItemService2 = subject.CreateWorkItemServiceForTeam(new AzureDevOpsTeamConfiguration());

            Assert.That(workItemService2, Is.EqualTo(workItemService1));
        }

        [Test]
        public void CreateWorkItemService_GivenDefaultTeamConfiguration_Throws()
        {
            var teamConfiguration = new DefaultTeamConfiguration();
            var subject = new WorkItemServiceFactory();

            Assert.Throws<NotSupportedException>(() => subject.CreateWorkItemServiceForTeam(teamConfiguration));
        }
    }
}
