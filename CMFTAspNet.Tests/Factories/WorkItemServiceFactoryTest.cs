using CMFTAspNet.Services.Factories;
using CMFTAspNet.Services.Implementation.AzureDevOps;
using CMFTAspNet.WorkTracking;

namespace CMFTAspNet.Tests.Factories
{
    public class WorkItemServiceFactoryTest
    {
        [Test]
        public void CreateWorkItemService_GivenAzureDevOpsn_ReturnsAzureDevOpsWorkItemService()
        {
            var subject = new WorkItemServiceFactory();

            var workItemService = subject.GetWorkItemServiceForWorkTrackingSystem(WorkTrackingSystems.AzureDevOps);

            Assert.That(workItemService, Is.InstanceOf<AzureDevOpsWorkItemService>());
        }

        [Test]
        public void CreateWorkItemService_CreateMultipleSerivcesForSameWorkTrackingSystem_ReturnsSameService()
        {
            var subject = new WorkItemServiceFactory();

            var workItemService1 = subject.GetWorkItemServiceForWorkTrackingSystem(WorkTrackingSystems.AzureDevOps);
            var workItemService2 = subject.GetWorkItemServiceForWorkTrackingSystem(WorkTrackingSystems.AzureDevOps);

            Assert.That(workItemService2, Is.EqualTo(workItemService1));
        }

        [Test]
        public void CreateWorkItemService_GivenUnknownWorkTrackingSystem_Throws()
        {
            var subject = new WorkItemServiceFactory();

            Assert.Throws<NotSupportedException>(() => subject.GetWorkItemServiceForWorkTrackingSystem(WorkTrackingSystems.Unknown));
        }
    }
}
