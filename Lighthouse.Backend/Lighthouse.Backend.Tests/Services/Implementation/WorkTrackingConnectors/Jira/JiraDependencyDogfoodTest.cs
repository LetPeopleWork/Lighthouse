using Lighthouse.Backend.Services.Implementation.DeliverySources;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// The link mapping against the real demo Jira, rather than against a payload written here. A fixture
    /// can only ever say that the code handles the shape its author expected; this says the shape is what
    /// Jira sends.
    ///
    /// Four Epics in the demo project carry Blocks links between them, and two of them carry only the
    /// far end of somebody else's. Those two are the point: they must read empty, because a mapper that
    /// walked both ends would double every dependency in the instance and still look plausible.
    /// </summary>
    [Category("Integration")]
    [Category("JiraIntegration")]
    public class JiraDependencyDogfoodTest
    {
        private const string TheDemoEpics = "project = LGHTHSDMO AND issuetype = Epic AND key in (LGHTHSDMO-7, LGHTHSDMO-8, LGHTHSDMO-9, LGHTHSDMO-10)";

        private const string SpotlightFinder = "LGHTHSDMO-7";
        private const string SnapShareHub = "LGHTHSDMO-8";
        private const string BlinkListDirectory = "LGHTHSDMO-9";
        private const string TrendSpotterInsights = "LGHTHSDMO-10";

        [Test]
        public async Task GetFeaturesForProject_ReadsTheBlocksLinksTheDemoProjectReallyHas()
        {
            var features = await TheDemoEpicsAsLighthouseReadsThem();

            var waitedOnBy = features.ToDictionary(
                feature => feature.ReferenceId,
                feature => feature.DependsOnReferences.Select(reference => reference.ReferenceId).Order().ToList());

            var spotlightWaitsOnBoth = new[] { SnapShareHub, BlinkListDirectory }.Order().ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(waitedOnBy[SpotlightFinder], Is.EqualTo(spotlightWaitsOnBoth),
                    "Spotlight Finder is blocked by both of the others in the demo project.");
                Assert.That(waitedOnBy[BlinkListDirectory], Is.EqualTo(new[] { TrendSpotterInsights }),
                    "BlinkList Directory is blocked by TrendSpotter Insights.");

                Assert.That(waitedOnBy[SnapShareHub], Is.Empty,
                    "SnapShare Hub only blocks another Epic. Reading that end too would record every dependency in "
                    + "the instance twice, and the count would still look like a believable number.");
                Assert.That(waitedOnBy[TrendSpotterInsights], Is.Empty,
                    "Same for TrendSpotter Insights: it blocks, and waits on nothing.");
            }
        }

        [Test]
        public async Task GetFeaturesForProject_TheLinkNameTheMapperLooksForIsTheOneThisInstanceUses()
        {
            var features = await TheDemoEpicsAsLighthouseReadsThem();

            Assert.That(features.Exists(feature => feature.DependsOnReferences.Count > 0), Is.True,
                "A Jira administrator can rename the inward link name. If this instance had renamed it, every Epic "
                + "here would read as waiting on nothing - which is exactly what an instance with no dependencies "
                + "looks like, and why the connector says so in the log rather than leaving it at that.");
        }

        private static async Task<List<Feature>> TheDemoEpicsAsLighthouseReadsThem()
        {
            var authStrategyFactory = TestAuthStrategyFactory.CreateRealFactory(new FakeCryptoService());
            var subject = new JiraWorkTrackingConnector(
                new IssueFactory(Mock.Of<ILogger<IssueFactory>>()),
                Mock.Of<ILogger<JiraWorkTrackingConnector>>(),
                authStrategyFactory,
                new Lighthouse.Backend.Cache.Cache<string, object>(),
                new DeliveryForecastBlockRenderer());

            return await subject.GetFeaturesForProject(TheDemoPortfolio());
        }

        private static Portfolio TheDemoPortfolio()
        {
            var username = Environment.GetEnvironmentVariable("JiraLighthouseIntegrationTestUsername") ?? "atlassian.pushchair@huser-berta.com";
            var apiToken = Environment.GetEnvironmentVariable("JiraLighthouseIntegrationTestToken")
                ?? throw new NotSupportedException("Can run test only if Environment Variable 'JiraLighthouseIntegrationTestToken' is set!");

            var connection = new WorkTrackingSystemConnection
            {
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                Name = "Demo Jira",
                AuthenticationMethodKey = AuthenticationMethodKeys.JiraCloud,
            };

            connection.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Url, Value = "https://letpeoplework.atlassian.net", IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Username, Value = username, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.ApiToken, Value = apiToken, IsSecret = true },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.RequestTimeoutInSeconds, Value = "100", IsSecret = false },
            ]);

            var portfolio = new Portfolio
            {
                Name = "Lighthouse Demo",
                DataRetrievalValue = TheDemoEpics,
                WorkTrackingSystemConnection = connection,
            };

            portfolio.WorkItemTypes.Clear();
            portfolio.WorkItemTypes.Add("Epic");

            portfolio.ToDoStates.Clear();
            portfolio.ToDoStates.Add("To Do");
            portfolio.DoingStates.Clear();
            portfolio.DoingStates.Add("In Progress");
            portfolio.DoneStates.Clear();
            portfolio.DoneStates.Add("Done");

            return portfolio;
        }
    }
}
