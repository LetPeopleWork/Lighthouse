using System.Reflection;
using System.Text.Json;
using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Models.DeliverySources;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Implementation.Licensing;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    /// <summary>
    /// What a Portfolio's connection offers a Delivery to bind its date to, and what those bindable
    /// things currently are.
    ///
    /// The connector is a mock throughout: whether a connection can read delivery sources is decided by
    /// whether its connector implements the reading port at all, so a mock that does and a mock that does
    /// not is exactly the distinction under specification here.
    /// </summary>
    [TestFixture]
    public class DeliverySourcesControllerTest
    {
        private const int PortfolioId = 1;
        private const string JiraReleaseSourceKey = "jira-release";
        private static readonly DeliverySourceProject TheJiraProject = new("LGH", "Lighthouse");

        private static readonly WorkTrackingSystems[] SystemsThatOfferNothing =
        [
            WorkTrackingSystems.AzureDevOps,
            WorkTrackingSystems.Csv,
            WorkTrackingSystems.Linear,
            WorkTrackingSystems.ServiceNow,
        ];

        private static readonly DeliverySourceDescriptor[] TheJiraReleaseSource =
            [new DeliverySourceDescriptor(JiraReleaseSourceKey, "Jira Release")];

        private static readonly DateTime TheDateOfTheDatedRelease = new(2026, 8, 22, 0, 0, 0, DateTimeKind.Utc);

        private static readonly DeliverySourceOption[] ADatedAndAnUndatedRelease =
        [
            new DeliverySourceOption("10004", "Release 1.0", TheDateOfTheDatedRelease, TheJiraProject, false, true, null),
            new DeliverySourceOption("10005", "Release 2.0", null, TheJiraProject, false, false, SourceOptionBlockReason.NoDateSet),
        ];

        private static readonly JsonSerializerOptions WireOptions = new(JsonSerializerDefaults.Web);

        private Mock<IRepository<Portfolio>> portfolioRepositoryMock;
        private Mock<IWorkTrackingConnectorFactory> connectorFactoryMock;
        private Mock<IDeliverySourceProvider> deliverySourceProviderMock;
        private DeliverySourcesController subject;

        [SetUp]
        public void SetUp()
        {
            portfolioRepositoryMock = new Mock<IRepository<Portfolio>>();
            connectorFactoryMock = new Mock<IWorkTrackingConnectorFactory>();
            deliverySourceProviderMock = new Mock<IDeliverySourceProvider>();

            subject = new DeliverySourcesController(
                portfolioRepositoryMock.Object,
                connectorFactoryMock.Object);
        }

        [TestCaseSource(nameof(SystemsThatOfferNothing))]
        public void A_Portfolio_on_a_non_Jira_connection_is_told_it_has_no_delivery_sources_rather_than_handed_an_error(
            WorkTrackingSystems system)
        {
            GivenAPortfolioOn(system, offersDeliverySources: false);

            var result = subject.GetDeliverySources(PortfolioId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.TypeOf<OkObjectResult>(),
                    "a connection with nothing to offer is not a failure, so the caller gets an answer rather than an error to render.");
                Assert.That(SourcesIn(result), Is.Empty,
                    "an empty list is what makes the extra tab disappear; anything else would put a broken tab in front of the user.");
            }
        }

        [Test]
        public void A_Jira_connection_offers_its_Releases_as_a_delivery_source()
        {
            GivenAPortfolioOn(WorkTrackingSystems.Jira, offersDeliverySources: true);
            deliverySourceProviderMock.Setup(p => p.AvailableSources(It.IsAny<WorkTrackingSystemConnection>()))
                .Returns(TheJiraReleaseSource);

            var result = subject.GetDeliverySources(PortfolioId);

            var sources = SourcesIn(result);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(sources, Has.Count.EqualTo(1));
                Assert.That(sources[0].Key, Is.EqualTo(JiraReleaseSourceKey),
                    "the key travels in a url and is what a create payload names, so the wire repeats the adapter's key untouched.");
                Assert.That(sources[0].DisplayName, Is.EqualTo("Jira Release"));
            }
        }

        [Test]
        public void A_Portfolio_that_does_not_exist_has_no_delivery_sources_to_list()
        {
            portfolioRepositoryMock.Setup(r => r.GetById(PortfolioId)).Returns((Portfolio?)null);

            var result = subject.GetDeliverySources(PortfolioId);

            Assert.That(result, Is.TypeOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task A_Release_that_nobody_dated_arrives_with_no_date_at_all_rather_than_a_stand_in_one()
        {
            GivenAPortfolioOn(WorkTrackingSystems.Jira, offersDeliverySources: true);
            GivenTheConnectionOffersJiraReleases();
            deliverySourceProviderMock
                .Setup(p => p.GetOptions(It.IsAny<WorkTrackingSystemConnection>(), JiraReleaseSourceKey))
                .ReturnsAsync(ADatedAndAnUndatedRelease);

            var result = await subject.GetOptions(PortfolioId, JiraReleaseSourceKey);

            var options = OptionsIn(result);
            using var undatedOnTheWire = JsonDocument.Parse(JsonSerializer.Serialize(options[1], WireOptions));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(options, Has.Count.EqualTo(2));
                Assert.That(options[0].Id, Is.EqualTo("10004"));
                Assert.That(options[0].Name, Is.EqualTo("Release 1.0"));
                Assert.That(options[0].Date, Is.EqualTo(TheDateOfTheDatedRelease));
                Assert.That(options[0].ProjectKey, Is.EqualTo("LGH"),
                    "two projects on one connection routinely name a Release the same thing, so a row has to say which project it came from.");
                Assert.That(options[0].ProjectName, Is.EqualTo("Lighthouse"));
                Assert.That(options[1].Date, Is.Null,
                    "a Jira Release without a release date is the common case on a real instance, not an edge case.");
                Assert.That(undatedOnTheWire.RootElement.TryGetProperty("date", out _), Is.False,
                    "the field has to be absent rather than null, so no reader can mistake a default date for a real one.");
                Assert.That(options[1].IsSelectable, Is.False,
                    "the server decides what may be bound, so a direct POST cannot bind what the picker greys out.");
                Assert.That(options[1].BlockedBecause, Is.EqualTo(SourceOptionBlockReason.NoDateSet),
                    "the reason is passed through from the adapter rather than worked out again here.");
            }
        }

        [Test]
        public async Task A_source_key_the_connection_does_not_offer_is_not_found()
        {
            GivenAPortfolioOn(WorkTrackingSystems.Jira, offersDeliverySources: true);
            GivenTheConnectionOffersJiraReleases();

            var result = await subject.GetOptions(PortfolioId, "jira-sprint");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.TypeOf<NotFoundObjectResult>());
                deliverySourceProviderMock.Verify(
                    p => p.GetOptions(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<string>()),
                    Times.Never,
                    "a key nobody offers is refused before the remote is called, so a made-up request cannot be reported as the remote being unwell.");
            }
        }

        [TestCaseSource(nameof(SystemsThatOfferNothing))]
        public async Task Asking_a_connection_that_offers_nothing_for_options_is_not_found(WorkTrackingSystems system)
        {
            GivenAPortfolioOn(system, offersDeliverySources: false);

            var result = await subject.GetOptions(PortfolioId, JiraReleaseSourceKey);

            Assert.That(result, Is.TypeOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task A_Portfolio_that_does_not_exist_has_no_options_to_offer()
        {
            portfolioRepositoryMock.Setup(r => r.GetById(PortfolioId)).Returns((Portfolio?)null);

            var result = await subject.GetOptions(PortfolioId, JiraReleaseSourceKey);

            Assert.That(result, Is.TypeOf<NotFoundObjectResult>());
        }

        [Test]
        public void The_routes_are_nested_under_the_Portfolio_so_that_the_access_guard_can_find_its_scope()
        {
            var routes = typeof(DeliverySourcesController)
                .GetCustomAttributes<RouteAttribute>()
                .Select(r => r.Template)
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(routes, Does.Contain("api/v1/portfolios/{portfolioId:int}/delivery-sources"));
                Assert.That(routes, Does.Contain("api/latest/portfolios/{portfolioId:int}/delivery-sources"),
                    "every route is served under both the pinned version and the moving alias.");
            }
        }

        [Test]
        public void Listing_the_sources_needs_only_read_access_and_no_licence()
        {
            var guard = GuardOn(nameof(DeliverySourcesController.GetDeliverySources));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(guard.Requirement, Is.EqualTo(RbacGuardRequirement.PortfolioRead));
                Assert.That(guard.ScopeIdRouteKey, Is.EqualTo("portfolioId"));
                Assert.That(
                    typeof(DeliverySourcesController).GetMethod(nameof(DeliverySourcesController.GetDeliverySources))!
                        .GetCustomAttribute<LicenseGuardAttribute>(),
                    Is.Null,
                    "the tab has to be able to render its own locked state, which it cannot do if asking what exists is itself gated.");
            }
        }

        [Test]
        public void Reading_what_can_be_bound_needs_write_access()
        {
            var guard = GuardOn(nameof(DeliverySourcesController.GetOptions));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(guard.Requirement, Is.EqualTo(RbacGuardRequirement.PortfolioWrite));
                Assert.That(guard.ScopeIdRouteKey, Is.EqualTo("portfolioId"));
            }
        }

        private static RbacGuardAttribute GuardOn(string actionName)
        {
            return typeof(DeliverySourcesController).GetMethod(actionName)!
                .GetCustomAttribute<RbacGuardAttribute>()!;
        }

        private static List<DeliverySourceDto> SourcesIn(IActionResult result)
        {
            return (List<DeliverySourceDto>)((OkObjectResult)result).Value!;
        }

        private static List<DeliverySourceOptionDto> OptionsIn(IActionResult result)
        {
            return (List<DeliverySourceOptionDto>)((OkObjectResult)result).Value!;
        }

        private void GivenTheConnectionOffersJiraReleases()
        {
            deliverySourceProviderMock.Setup(p => p.AvailableSources(It.IsAny<WorkTrackingSystemConnection>()))
                .Returns(TheJiraReleaseSource);
        }

        private void GivenAPortfolioOn(WorkTrackingSystems system, bool offersDeliverySources)
        {
            var portfolio = new Portfolio
            {
                Id = PortfolioId,
                Name = "Lighthouse",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Name = "Connection",
                    WorkTrackingSystem = system,
                },
            };

            portfolioRepositoryMock.Setup(r => r.GetById(PortfolioId)).Returns(portfolio);

            var connector = offersDeliverySources
                ? deliverySourceProviderMock.As<IWorkTrackingConnector>().Object
                : Mock.Of<IWorkTrackingConnector>();

            connectorFactoryMock.Setup(f => f.GetWorkTrackingConnector(system)).Returns(connector);
        }
    }
}
