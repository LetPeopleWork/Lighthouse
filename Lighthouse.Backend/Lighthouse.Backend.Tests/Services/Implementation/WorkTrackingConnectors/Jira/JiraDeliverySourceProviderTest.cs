using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Csv;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// Which delivery sources a work tracking connection offers a Delivery to bind its date to.
    ///
    /// Kept out of JiraWorkTrackingConnectorTest on purpose: that class is marked as a live-Jira
    /// integration suite, and the test filter every developer and the build server use excludes it,
    /// so a specification written there would silently never run.
    ///
    /// The subject here is built from mocks and handed a connection with no url and no credentials.
    /// Any attempt to reach a real Jira would fail rather than pass, which is how these specifications
    /// hold the answer to being computed rather than fetched.
    /// </summary>
    [TestFixture]
    public class JiraDeliverySourceProviderTest
    {
        private static readonly Type[] SystemsThatOfferNothing =
        [
            typeof(AzureDevOpsWorkTrackingConnector),
            typeof(CsvWorkTrackingConnector),
            typeof(LinearWorkTrackingConnector),
            typeof(ServiceNowWorkTrackingConnector),
        ];

        [Test]
        public void A_Jira_connection_offers_its_Releases_as_a_delivery_source()
        {
            var subject = CreateSubject();

            var sources = subject.AvailableSources(UnreachableJiraConnection());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(sources, Has.Count.EqualTo(1),
                    "Jira offers exactly one thing a Delivery date can be bound to today.");
                Assert.That(sources[0].Key, Is.EqualTo("jira-release"),
                    "the key travels in a url and is what a create payload names, so it stays lowercase and stable.");
                Assert.That(sources[0].DisplayName, Is.EqualTo("Jira Release"),
                    "Release is Jira's own word for what is being bound, so it is never renamed to the tenant's vocabulary.");
            }
        }

        [Test]
        public void The_other_work_tracking_systems_offer_no_delivery_sources_at_all()
        {
            var offering = SystemsThatOfferNothing
                .Where(connector => connector.IsAssignableTo(typeof(IDeliverySourceProvider)))
                .ToArray();

            Assert.That(offering, Is.Empty,
                "a system that cannot offer sources says so by not implementing the capability; there is no flag to switch off and no registry to stay out of.");
        }

        private static WorkTrackingSystemConnection UnreachableJiraConnection()
        {
            return new WorkTrackingSystemConnection
            {
                Id = 5565,
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                Name = "A Jira connection that was never reached",
            };
        }

        private static JiraWorkTrackingConnector CreateSubject()
        {
            return new JiraWorkTrackingConnector(
                Mock.Of<IIssueFactory>(),
                Mock.Of<ILogger<JiraWorkTrackingConnector>>(),
                Mock.Of<IWorkTrackingAuthStrategyFactory>());
        }
    }
}
