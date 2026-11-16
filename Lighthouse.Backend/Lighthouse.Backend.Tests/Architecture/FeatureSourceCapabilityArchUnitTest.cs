using System.Reflection;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Jira, Azure DevOps, Linear and CSV can read the Features of a Portfolio; ServiceNow cannot. A connector that
    /// cannot must not claim it can, so reading Portfolio Features is its own capability that only those four
    /// connectors take on. A Portfolio refresh on a ServiceNow connection still fails, as it always has, and leaves
    /// the Portfolio's Features as they were.
    /// </summary>
    [TestFixture]
    public class FeatureSourceCapabilityArchUnitTest
    {
        private static readonly string FeatureSourceCapabilityName = typeof(IFeatureSource).FullName!;

        private const string FeatureReadMember = "GetFeaturesForPortfolio";

        private const string ParentReadMember = "GetParentFeaturesDetails";

        private static readonly string[] ConnectorsThatReadFeatures =
        [
            "JiraWorkTrackingConnector",
            "AzureDevOpsWorkTrackingConnector",
            "LinearWorkTrackingConnector",
            "CsvWorkTrackingConnector",
        ];

        private static readonly string[] EveryKnownConnector =
        [
            "JiraWorkTrackingConnector",
            "AzureDevOpsWorkTrackingConnector",
            "LinearWorkTrackingConnector",
            "CsvWorkTrackingConnector",
            "ServiceNowWorkTrackingConnector",
        ];

        private static readonly Type[] FeatureReadParameters = [typeof(Portfolio), typeof(CancellationToken)];

        private static readonly Type[] ParentReadParameters = [typeof(Portfolio), typeof(IEnumerable<string>), typeof(CancellationToken)];

        [Test]
        public void ReadingPortfolioFeaturesIsItsOwnCapability_AndNoLongerAskedOfEveryConnector()
        {
            var capability = GivenTheFeatureSourceCapability();

            ThenTheCapabilityDeclaresBothFeatureReads(capability);
            ThenEveryConnectorIsNoLongerAskedToReadFeatures();
        }

        [Test]
        public void OnlyJiraAzureDevOpsLinearAndCsvReadPortfolioFeatures()
        {
            var capability = GivenTheFeatureSourceCapability();

            var connectors = WhenEveryConnectorIsRead();

            ThenEveryKnownConnectorWasRead(connectors);
            ThenExactlyTheFourFeatureReadersTakeOnTheCapability(connectors, capability);
        }

        [Test]
        public void APortfolioRefreshOnAServiceNowConnection_FailsAndLeavesItsFeaturesAsTheyWere()
        {
            var scenario = GivenAServiceNowPortfolioHoldingOneFeature();

            var refresh = WhenThePortfolioIsRefreshed(scenario);

            ThenTheRefreshFailedAsUnsupported(refresh);
            ThenThePortfolioKeptItsFeaturesAndWasNotStamped(scenario);
        }

        private static Type GivenTheFeatureSourceCapability()
        {
            var capability = typeof(IWorkTrackingConnector).Assembly.GetType(FeatureSourceCapabilityName);

            Assert.That(
                capability,
                Is.Not.Null,
                $"WHAT: there is no {FeatureSourceCapabilityName} in Lighthouse.Backend. WHY: without a Feature-reading capability of its " +
                "own, every connector is asked to read Portfolio Features, and ServiceNow can only refuse what it claims to do. HOW: add the " +
                $"interface in Services/Interfaces/WorkTrackingConnectors, move {FeatureReadMember}(Portfolio, CancellationToken) and " +
                $"{ParentReadMember} onto it, and let only the Jira, Azure DevOps, Linear and CSV connectors take it on.");
            Assert.That(capability!.IsInterface, Is.True, $"WHAT: {FeatureSourceCapabilityName} is not an interface. WHY: a connector takes a capability on by implementing it. HOW: declare it as an interface.");

            return capability;
        }

        private static List<Type> WhenEveryConnectorIsRead()
            => [.. typeof(IWorkTrackingConnector).Assembly.GetTypes()
                .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IWorkTrackingConnector).IsAssignableFrom(type))];

        private static void ThenTheCapabilityDeclaresBothFeatureReads(Type capability)
        {
            var featureRead = capability.GetMethod(FeatureReadMember, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, FeatureReadParameters);
            var parentRead = capability.GetMethod(ParentReadMember, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, ParentReadParameters);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    featureRead?.ReturnType,
                    Is.EqualTo(typeof(Task<List<Feature>>)),
                    $"WHAT: {FeatureSourceCapabilityName} does not declare Task<List<Feature>> {FeatureReadMember}(Portfolio, CancellationToken). " +
                    "WHY: a Portfolio refresh asks the capability for every Feature its query returns. HOW: move the member from " +
                    "IWorkTrackingConnector onto the capability unchanged.");
                Assert.That(
                    parentRead?.ReturnType,
                    Is.EqualTo(typeof(Task<List<Feature>>)),
                    $"WHAT: {FeatureSourceCapabilityName} does not declare Task<List<Feature>> {ParentReadMember}(Portfolio, IEnumerable<string>, " +
                    "CancellationToken). WHY: a Portfolio refresh reads the parents of its Features from the same source as the Features. " +
                    "HOW: move the member from IWorkTrackingConnector onto the capability unchanged.");
            }
        }

        private static void ThenEveryConnectorIsNoLongerAskedToReadFeatures()
        {
            var featureReadOnTheBase = typeof(IWorkTrackingConnector).GetMethod(FeatureReadMember, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, FeatureReadParameters);
            var parentReadOnTheBase = typeof(IWorkTrackingConnector).GetMethod(ParentReadMember, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, ParentReadParameters);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    featureReadOnTheBase,
                    Is.Null,
                    $"WHAT: IWorkTrackingConnector still declares {FeatureReadMember}(Portfolio, CancellationToken). WHY: then ServiceNow must " +
                    "still claim a Feature read it can only refuse. HOW: remove the member from IWorkTrackingConnector; it lives on the " +
                    "Feature-reading capability.");
                Assert.That(
                    parentReadOnTheBase,
                    Is.Null,
                    $"WHAT: IWorkTrackingConnector still declares {ParentReadMember}. WHY: then ServiceNow must still claim a parent-Feature " +
                    "read it can only refuse. HOW: remove the member from IWorkTrackingConnector; it lives on the Feature-reading capability.");
            }
        }

        private static void ThenEveryKnownConnectorWasRead(List<Type> connectors)
        {
            var read = connectors.Select(type => type.Name).ToList();

            Assert.That(
                read,
                Is.SupersetOf(EveryKnownConnector),
                $"WHAT: the connectors read were [{string.Join(", ", read)}], which does not hold all five of [{string.Join(", ", EveryKnownConnector)}]. " +
                "WHY: a connector that is not read is never checked, and the answer below would hold without looking. HOW: each connector " +
                "must remain a concrete class in Lighthouse.Backend implementing IWorkTrackingConnector.");
        }

        private static void ThenExactlyTheFourFeatureReadersTakeOnTheCapability(List<Type> connectors, Type capability)
        {
            var readingFeatures = connectors.Where(capability.IsAssignableFrom).Select(type => type.Name).ToList();

            Assert.That(
                readingFeatures,
                Is.EquivalentTo(ConnectorsThatReadFeatures),
                $"WHAT: the connectors that take on the Feature-reading capability are [{string.Join(", ", readingFeatures)}], expected exactly " +
                $"[{string.Join(", ", ConnectorsThatReadFeatures)}]. WHY: ServiceNow cannot read Portfolio Features, so taking the capability " +
                "on would claim a read it can only refuse, and dropping any of the other four would stop its Portfolio refreshes. HOW: list " +
                "the capability on IJiraWorkTrackingConnector, IAzureDevOpsWorkTrackingConnector and ILinearWorkTrackingConnector, declare it " +
                "on CsvWorkTrackingConnector, and remove the Feature-read members from the ServiceNow connector.");
        }

        private static ServiceNowPortfolioScenario GivenAServiceNowPortfolioHoldingOneFeature()
        {
            var lastRefresh = new DateTime(2026, 11, 2, 8, 0, 0, DateTimeKind.Utc);
            var portfolio = new Portfolio
            {
                Id = 3,
                Name = "Service desk improvements",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection { Id = 4, Name = "A ServiceNow connection", WorkTrackingSystem = WorkTrackingSystems.ServiceNow },
                UpdateTime = lastRefresh,
            };

            var storedFeature = new Feature(new WorkItemBase { ReferenceId = "F-1", Name = "Stored before the refresh" });
            portfolio.Features.Add(storedFeature);

            return new ServiceNowPortfolioScenario(portfolio, storedFeature, lastRefresh, new Mock<IFeatureRepository>());
        }

        private static Task<SyncOutcome> WhenThePortfolioIsRefreshed(ServiceNowPortfolioScenario scenario)
        {
            var serviceNow = new ServiceNowWorkTrackingConnector(NullLogger<ServiceNowWorkTrackingConnector>.Instance, Mock.Of<IWorkTrackingAuthStrategyFactory>());

            var service = new WorkItemServiceTestBuilder()
                .WithConnector(serviceNow)
                .WithFeatureRepository(scenario.Features.Object)
                .Build();

            return service.UpdateFeaturesForPortfolio(scenario.Portfolio);
        }

        private static void ThenTheRefreshFailedAsUnsupported(Task<SyncOutcome> refresh)
        {
            Assert.That(
                async () => await refresh,
                Throws.TypeOf<NotSupportedException>(),
                "WHAT: a Portfolio refresh on a ServiceNow connection did not end in a NotSupportedException. WHY: ServiceNow cannot read " +
                "Portfolio Features, and its refresh has always failed; a refresh that completes would report an empty Portfolio as current. " +
                "HOW: when the connection's connector is not a Feature source, throw NotSupportedException naming its system at the " +
                "Portfolio Feature read, before anything is written.");
        }

        private static void ThenThePortfolioKeptItsFeaturesAndWasNotStamped(ServiceNowPortfolioScenario scenario)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    scenario.Portfolio.Features,
                    Is.EquivalentTo(new[] { scenario.StoredFeature }),
                    "WHAT: the Portfolio's Features changed during a failed ServiceNow refresh. WHY: a refresh that read nothing must leave " +
                    "what the Portfolio holds as it was. HOW: fail at the Portfolio Feature read, before the Features are rebuilt.");
                Assert.That(
                    scenario.Portfolio.UpdateTime,
                    Is.EqualTo(scenario.LastRefresh),
                    "WHAT: a failed ServiceNow refresh stamped the Portfolio as refreshed. WHY: the Portfolio would look current when nothing " +
                    "was read. HOW: fail before the refresh time is set.");
            }

            scenario.Features.Verify(repository => repository.Save(), Times.Never(), "WHAT: a failed ServiceNow refresh saved Features. WHY: nothing was read, so nothing may be stored. HOW: fail before any write.");
        }

        private sealed record ServiceNowPortfolioScenario(
            Portfolio Portfolio,
            Feature StoredFeature,
            DateTime LastRefresh,
            Mock<IFeatureRepository> Features);
    }
}
