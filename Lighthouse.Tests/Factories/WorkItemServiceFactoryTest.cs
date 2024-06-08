using Castle.Core.Logging;
using Lighthouse.Factories;
using Lighthouse.Services.Factories;
using Lighthouse.Services.Implementation.WorkItemServices;
using Lighthouse.WorkTracking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Tests.Factories
{
    public class WorkItemServiceFactoryTest
    {
        [Test]
        public void CreateWorkItemService_GivenAzureDevOps_ReturnsAzureDevOpsWorkItemService()
        {
            var subject = CreateSubject();

            var workItemService = subject.GetWorkItemServiceForWorkTrackingSystem(WorkTrackingSystems.AzureDevOps);

            Assert.That(workItemService, Is.InstanceOf<AzureDevOpsWorkItemService>());
        }

        [Test]
        public void CreateWorkItemService_GivenJira_ReturnsJiraWorkItemService()
        {
            var subject = CreateSubject();

            var workItemService = subject.GetWorkItemServiceForWorkTrackingSystem(WorkTrackingSystems.Jira);

            Assert.That(workItemService, Is.InstanceOf<JiraWorkItemService>());
        }

        [Test]
        public void CreateWorkItemService_CreateMultipleSerivcesForSameWorkTrackingSystem_ReturnsSameService()
        {
            var subject = CreateSubject();

            var workItemService1 = subject.GetWorkItemServiceForWorkTrackingSystem(WorkTrackingSystems.AzureDevOps);
            var workItemService2 = subject.GetWorkItemServiceForWorkTrackingSystem(WorkTrackingSystems.AzureDevOps);

            Assert.That(workItemService2, Is.EqualTo(workItemService1));
        }

        [Test]
        public void CreateWorkItemService_GivenUnknownWorkTrackingSystem_Throws()
        {
            var subject = CreateSubject();

            Assert.Throws<NotSupportedException>(() => subject.GetWorkItemServiceForWorkTrackingSystem(WorkTrackingSystems.Unknown));
        }

        private WorkItemServiceFactory CreateSubject()
        {
            return new WorkItemServiceFactory(Mock.Of<IIssueFactory>(), Mock.Of<ILexoRankService>(), Mock.Of<ILogger<WorkItemServiceFactory>>(), Mock.Of<ILogger<AzureDevOpsWorkItemService>>(), Mock.Of<ILogger<JiraWorkItemService>>());
        }
    }
}
