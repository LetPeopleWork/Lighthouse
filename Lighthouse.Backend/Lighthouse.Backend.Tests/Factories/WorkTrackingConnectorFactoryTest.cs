using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Csv;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Factories
{
    public class WorkTrackingConnectorFactoryTest
    {
        private Mock<IServiceProvider> serviceProviderMock;

        [SetUp]
        public void SetUp()
        {
            serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
            .Setup(x => x.GetService(typeof(IAzureDevOpsWorkTrackingConnector)))
            .Returns(new AzureDevOpsWorkTrackingConnector(Mock.Of<ILogger<AzureDevOpsWorkTrackingConnector>>(), new FakeCryptoService()));

            serviceProviderMock
            .Setup(x => x.GetService(typeof(IJiraWorkTrackingConnector)))
            .Returns(new JiraWorkTrackingConnector(Mock.Of<IIssueFactory>(), Mock.Of<ILogger<JiraWorkTrackingConnector>>(), new FakeCryptoService()));

            serviceProviderMock
            .Setup(x => x.GetService(typeof(CsvWorkTrackingConnector)))
            .Returns(new CsvWorkTrackingConnector(Mock.Of<ILogger<CsvWorkTrackingConnector>>()));
        }

        [Test]
        public void CreateWorkItemService_GivenAzureDevOps_ReturnsAzureDevOpsWorkItemService()
        {
            var subject = CreateSubject();

            var workItemService = subject.GetWorkTrackingConnector(WorkTrackingSystems.AzureDevOps);

            Assert.That(workItemService, Is.InstanceOf<AzureDevOpsWorkTrackingConnector>());
        }

        [Test]
        public void CreateWorkItemService_GivenJira_ReturnsJiraWorkItemService()
        {
            var subject = CreateSubject();

            var workItemService = subject.GetWorkTrackingConnector(WorkTrackingSystems.Jira);

            Assert.That(workItemService, Is.InstanceOf<JiraWorkTrackingConnector>());
        }

        [Test]
        public void CreateWorkItemService_CreateMultipleSerivcesForSameWorkTrackingSystem_ReturnsSameService()
        {
            var subject = CreateSubject();

            var workItemService1 = subject.GetWorkTrackingConnector(WorkTrackingSystems.AzureDevOps);
            var workItemService2 = subject.GetWorkTrackingConnector(WorkTrackingSystems.AzureDevOps);

            Assert.That(workItemService2, Is.EqualTo(workItemService1));
        }

        [Test]
        public void CreateWorkItemService_GivenCsv_ReturnsCsvWorkItemService()
        {
            var subject = CreateSubject();

            var workItemService = subject.GetWorkTrackingConnector(WorkTrackingSystems.Csv);

            Assert.That(workItemService, Is.InstanceOf<CsvWorkTrackingConnector>());
        }

        private WorkTrackingConnectorFactory CreateSubject()
        {
            return new WorkTrackingConnectorFactory(serviceProviderMock.Object, Mock.Of<ILogger<WorkTrackingConnectorFactory>>());
        }
    }
}
