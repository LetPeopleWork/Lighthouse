using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliverySources;
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

        /// <summary>
        /// Copied from what a real Jira answered on 2026-08-22. The middle entry has no releaseDate key at
        /// all - not a null, the key is simply absent - which is how Jira reports a Release nobody dated,
        /// and how two of the three Releases on that instance came back.
        /// </summary>
        private const string CapturedVersionsPayload = """
            [
              {
                "self": "https://example.atlassian.net/rest/api/3/version/10004",
                "id": "10004",
                "name": "Release 1.0",
                "archived": false,
                "released": true,
                "releaseDate": "2026-08-22",
                "projectId": 10001
              },
              {
                "self": "https://example.atlassian.net/rest/api/3/version/10005",
                "id": "10005",
                "name": "Release 2.0",
                "archived": false,
                "released": false,
                "projectId": 10001
              },
              {
                "self": "https://example.atlassian.net/rest/api/3/version/10006",
                "id": "10006",
                "name": "Release 0.9",
                "archived": true,
                "released": true,
                "releaseDate": "2025-01-15",
                "projectId": 10001
              }
            ]
            """;

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

        [Test]
        public void A_Release_with_no_release_date_is_offered_but_cannot_be_selected()
        {
            var options = JiraReleaseVersionReader.ReadOptions(CapturedVersionsPayload);

            var undated = options.Single(option => option.Id == "10005");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(undated.Name, Is.EqualTo("Release 2.0"),
                    "a Release nobody dated is still worth showing, so the reader has to get this far rather than give up on the entry.");
                Assert.That(undated.Date, Is.Null,
                    "Jira leaves the key out rather than sending null, and a missing key means no date - never a payload the reader failed to read.");
                Assert.That(undated.IsSelectable, Is.False);
                Assert.That(undated.BlockedBecause, Is.EqualTo(SourceOptionBlockReason.NoDateSet),
                    "the way out of this one is to set a date in Jira, which is a different errand than picking another Release.");
            }
        }

        [Test]
        public void A_Release_that_was_archived_in_Jira_cannot_be_selected()
        {
            var options = JiraReleaseVersionReader.ReadOptions(CapturedVersionsPayload);

            var archived = options.Single(option => option.Id == "10006");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(archived.IsRetiredAtSource, Is.True);
                Assert.That(archived.IsSelectable, Is.False);
                Assert.That(archived.BlockedBecause, Is.EqualTo(SourceOptionBlockReason.RetiredAtSource),
                    "an archived Release carries a date and is still refused, so having a date is not on its own enough.");
            }
        }

        [Test]
        public void A_Release_that_already_shipped_stays_selectable()
        {
            var options = JiraReleaseVersionReader.ReadOptions(CapturedVersionsPayload);

            var shipped = options.Single(option => option.Id == "10004");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(options, Has.Count.EqualTo(3),
                    "every version Jira returns becomes an option; none are filtered out on the way through.");
                Assert.That(shipped.IsReleasedAtSource, Is.True);
                Assert.That(shipped.Date, Is.EqualTo(new DateTime(2026, 8, 22, 0, 0, 0, DateTimeKind.Utc)));
                Assert.That(shipped.IsSelectable, Is.True,
                    "a shipped Release is routinely still being tracked to closure, which is exactly when a forecast is worth having.");
                Assert.That(shipped.BlockedBecause, Is.Null);
            }
        }

        [Test]
        public void A_source_key_the_connection_never_offered_is_refused_before_Jira_is_asked()
        {
            var subject = CreateSubject();

            Assert.ThrowsAsync<ArgumentException>(
                async () => await subject.GetOptions(UnreachableJiraConnection(), "jira-sprint", "LGH"),
                "the connection has no url and no credentials, so anything that reached the network would fail differently.");
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
