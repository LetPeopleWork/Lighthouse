using Lighthouse.Backend.Models;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The five connectors with their helpers, mappers, verdicts, read scopes and option names, the authentication strategies
    /// with their factory, the OAuth schema helpers and Jira's issue factory live in the Tracker integration module's
    /// Adapters/Connectors folder, one sub-namespace per tracker plus Auth and OAuth, and none is left under Services or
    /// Factories. The types are named as strings and read from the compiled production assembly, so a type that is missing,
    /// renamed or left behind is a failure that names it. Only top-level types are named; nested ones live where their
    /// outer type lives. That a connector reaches no repository is already held for the whole module by
    /// ModuleBoundariesArchUnitTest.WorkTrackingConnectors_DoNotDependOnAnyRepository, and
    /// TrackerIntegrationRulesSeeTheModuleArchUnitTest shows that rule names a made-up offender below the module.
    /// </summary>
    [TestFixture]
    public class TrackerIntegrationConnectorsLayoutArchUnitTest
    {
        private const string ConnectorsNamespace = "Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors";

        private const string WhereTheTypesLivedBefore = @"^Lighthouse\.Backend\.(Services|Factories)(\.|$)";

        private const string FixtureConnectorsNamespace = "Lighthouse.Backend.Modules.TrackerIntegrationFixtures.ConnectorsLayout.Connectors";

        private const string FixtureFormerHome = @"^Lighthouse\.Backend\.Modules\.TrackerIntegrationFixtures\.ConnectorsLayout\.Services(\.|$)";

        private static readonly string[] AzureDevOpsTypes =
        [
            "AzureDevOpsFieldNames",
            "AzureDevOpsReadException",
            "AzureDevOpsWorkTrackingConnector",
            "AzureDevOpsWorkTrackingOptionNames",
            "RelationTypeListing",
            "WorkItemExtensions",
        ];

        private static readonly string[] CsvTypes =
        [
            "CsvWorkTrackingConnector",
            "CsvWorkTrackingOptionNames",
        ];

        private static readonly string[] JiraTypes =
        [
            "Issue",
            "IssueExtensions",
            "IIssueFactory",
            "IssueFactory",
            "JiraDeployment",
            "JiraFieldNames",
            "JiraOriginalNames",
            "JiraQueryRejectedException",
            "JiraReadException",
            "JiraReleaseSweepCost",
            "JiraReleaseSweepTally",
            "JiraReleaseVersionReader",
            "JiraRestRoutes",
            "JiraSiteAddress",
            "JiraWorkTrackingConnector",
            "JiraWorkTrackingOptionNames",
            "LinkDirection",
            "StandingLink",
        ];

        private static readonly string[] LinearTypes =
        [
            "LinearWorkTrackingConnector",
            "LinearWorkTrackingOptionNames",
        ];

        private static readonly string[] ServiceNowTypes =
        [
            "ServiceNowBoardMapper",
            "ServiceNowBoardVerdict",
            "ServiceNowClassLabels",
            "ServiceNowHistoryAvailability",
            "ServiceNowHistoryQuery",
            "ServiceNowHistoryVerdict",
            "ServiceNowReadException",
            "ServiceNowReadScope",
            "ServiceNowStateSpan",
            "ServiceNowStateSpanMapper",
            "ServiceNowTeamQueryVerdict",
            "ServiceNowValidationVerdict",
            "ServiceNowWorkItemMapper",
            "ServiceNowWorkTrackingConnector",
            "ServiceNowWorkTrackingOptionNames",
        ];

        private static readonly string[] AuthTypes =
        [
            "JiraCloudBasicAuthStrategy",
            "LinearApiKeyAuthStrategy",
            "NoOpAuthStrategy",
            "OAuthBearerAuthStrategy",
            "PatAuthStrategy",
            "ServiceNowBasicAuthStrategy",
            "WorkTrackingAuthStrategyFactory",
            "WorkTrackingAuthStrategyNotFoundException",
        ];

        private static readonly string[] OAuthTypes =
        [
            "IOAuthSchemaExtensions",
            "OAuthSchemaExtensions",
            "OAuthWorkTrackingOptionNames",
        ];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        private static readonly Lazy<ModuleDefinition> FixtureModule = new(() => IlDependencies.ReadModule(typeof(TrackerIntegrationConnectorsLayoutArchUnitTest).Assembly));

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, FixtureModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void TheAzureDevOpsConnectorAndItsHelpers_LiveInTheirTrackerFolder_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, $"{ConnectorsNamespace}.AzureDevOps", AzureDevOpsTypes, WhereTheTypesLivedBefore).Check();
        }

        [Test]
        public void TheCsvConnectorAndItsOptionNames_LiveInTheirTrackerFolder_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, $"{ConnectorsNamespace}.Csv", CsvTypes, WhereTheTypesLivedBefore).Check();
        }

        [Test]
        public void TheJiraConnectorItsHelpersAndItsIssueFactory_LiveInTheirTrackerFolder_AndNoneIsLeftUnderServicesOrFactories()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, $"{ConnectorsNamespace}.Jira", JiraTypes, WhereTheTypesLivedBefore).Check();
        }

        [Test]
        public void TheLinearConnectorAndItsOptionNames_LiveInTheirTrackerFolder_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, $"{ConnectorsNamespace}.Linear", LinearTypes, WhereTheTypesLivedBefore).Check();
        }

        [Test]
        public void TheServiceNowConnectorItsMappersVerdictsAndReadScope_LiveInTheirTrackerFolder_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, $"{ConnectorsNamespace}.ServiceNow", ServiceNowTypes, WhereTheTypesLivedBefore).Check();
        }

        [Test]
        public void TheAuthenticationStrategiesAndTheirFactory_LiveInTheAuthFolder_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, $"{ConnectorsNamespace}.Auth", AuthTypes, WhereTheTypesLivedBefore).Check();
        }

        [Test]
        public void TheOAuthSchemaHelpers_LiveInTheOAuthFolder_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, $"{ConnectorsNamespace}.OAuth", OAuthTypes, WhereTheTypesLivedBefore).Check();
        }

        [Test]
        public void ATypeLeftUnderItsFormerHome_IsNamed_AndAWellPlacedTwinIsNot()
        {
            var twinPlaced = ModuleRules.TypesLiveInTheirLayer(FixtureModule.Value, FixtureConnectorsNamespace, ["AWellPlacedConnector"], FixtureFormerHome);
            var leftBehind = ModuleRules.TypesLiveInTheirLayer(FixtureModule.Value, FixtureConnectorsNamespace, ["ALeftBehindConnector"], FixtureFormerHome);

            Assert.DoesNotThrow(twinPlaced.Check, "A type that lives in its connector folder and has no copy in its former home was reported. Why it matters: a rule that flags a correct move hides the real offenders. How to fix: report only a type missing from the folder or still present in the former home.");

            var failure = Assert.Throws<AssertionException>(leftBehind.Check, "A type left in its former home passed the layout rule.");
            Assert.That(failure!.Message, Does.Contain("ALeftBehindConnector"), "The failure must name the type that was left behind.");
        }

        [Test]
        public void ATypeThatIsMissingFromItsConnectorFolder_IsNamed()
        {
            var missing = ModuleRules.TypesLiveInTheirLayer(FixtureModule.Value, FixtureConnectorsNamespace, ["AConnectorThatWasRenamed"], FixtureFormerHome);

            var failure = Assert.Throws<AssertionException>(missing.Check, "A type that does not exist passed the layout rule.");
            Assert.That(failure!.Message, Does.Contain("AConnectorThatWasRenamed"), "The failure must name the type that cannot be found. Why it matters: a rule that looks at nothing passes unseen. How to fix: report every named type the compiled assembly does not hold in the folder.");
        }
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.ConnectorsLayout.Connectors
{
    internal sealed class AWellPlacedConnector
    {
        public int Answer => 42;
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.ConnectorsLayout.Services
{
    internal sealed class ALeftBehindConnector
    {
        public int Answer => 42;
    }
}
