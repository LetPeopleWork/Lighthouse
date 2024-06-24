using Castle.Core.Logging;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.WorkItemServices;
using Lighthouse.Backend.WorkTracking;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Factories
{
    public class WorkItemServiceFactoryTest
    {
        private Mock<IServiceProvider> serviceProviderMock;

        [SetUp]
        public void SetUp()
        {
            serviceProviderMock = new Mock<IServiceProvider>();

            serviceProviderMock
            .Setup(x => x.GetService(typeof(AzureDevOpsWorkItemService)))
            .Returns(new AzureDevOpsWorkItemService(Mock.Of<ILogger<AzureDevOpsWorkItemService>>()));

            serviceProviderMock
            .Setup(x => x.GetService(typeof(JiraWorkItemService)))
            .Returns(new JiraWorkItemService(Mock.Of<ILexoRankService>(), Mock.Of<IIssueFactory>(), Mock.Of<ILogger<JiraWorkItemService>>()));
        }

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
            return new WorkItemServiceFactory(serviceProviderMock.Object, Mock.Of<ILogger<WorkItemServiceFactory>>());
        }
    }
}
