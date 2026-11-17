using System.Reflection;
using System.Text.RegularExpressions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.TrackerIntegrationFixtures.Boundaries.Domain;
using Lighthouse.Backend.TrackerIntegrationBoundariesFixtures.AdaptersNewcomer;
using Lighthouse.Backend.TrackerIntegrationBoundariesFixtures.ApplicationNewcomer;
using Lighthouse.Backend.TrackerIntegrationBoundariesFixtures.DomainNewcomer;
using Lighthouse.Backend.TrackerIntegrationBoundariesFixtures.ListedCallers;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Tracker integration is a module with rules of its own, checked by the shared module rules with Tracker integration's
    /// namespaces and wording and read from the compiled production assembly: its Domain reaches only the shared kernel and
    /// the base library; outside code reaches its Domain only from the listed types, its Application only from the API, the
    /// composition root, Work Items, Promises' Application and the listed types, and its Adapters only from the
    /// composition root and the listed types; its Application reaches EF Core, the data layer, the API, any module's
    /// adapters and the platform (stored records, the key ring, demo data) only through the listed edges; the module reaches no other context except through
    /// the listed edge; and no module type reaches a repository except the listed Application services. Every list is
    /// compared both ways. The same rules run on made-up types: each offender is named, and its compliant twin is not.
    /// That each type lives in its layer is pinned by the module's four layout tests, not here. The old leaf rule over the
    /// connector folders is gone, and live-suite selection no longer knows those folders.
    /// </summary>
    /// <remarks>
    /// The IL reader does not see types used only in a catch clause, an event or a named attribute argument.
    /// </remarks>
    [TestFixture]
    public class TrackerIntegrationModuleBoundariesArchUnitTest
    {
        private const string TrackerIntegrationModule = @"^Lighthouse\.Backend\.Modules\.TrackerIntegration(\.|$)";

        private const string TrackerIntegrationDomain = @"^Lighthouse\.Backend\.Modules\.TrackerIntegration\.Domain(\.|$)";

        private const string TrackerIntegrationApplication = @"^Lighthouse\.Backend\.Modules\.TrackerIntegration\.Application(\.|$)";

        private const string TrackerIntegrationAdapters = @"^Lighthouse\.Backend\.Modules\.TrackerIntegration\.Adapters(\.|$)";

        private const string WorkItemsModule = @"^Lighthouse\.Backend\.Modules\.WorkItems(\.|$)";

        // Promises' Application calls this module by right; its policies and its Domain do not, so they stay outside.
        private const string PromisesApplicationOutsideItsPolicies = @"^Lighthouse\.Backend\.Modules\.Promises\.Application(?!\.Policies(\.|$))(\.|$)";

        private const string TheCompositionRootAndItsNestedTypes = @"^Lighthouse\.Backend\.Program(\+|$)";

        private const string EntityFrameworkCore = @"^Microsoft\.EntityFrameworkCore(\.|$)";

        private const string AnyModuleAdapters = @"^Lighthouse\.Backend\.Modules\.[^.]+\.Adapters(\.|$)";

        private const string TheKeyRing = @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.Encryption(\.|$)";

        private const string TheDemoData = @"^Lighthouse\.Backend\.Factories(\.|$)";

        private const string OtherModules = @"^Lighthouse\.Backend\.Modules\.(WorkItems|Forecasting|FlowMetrics)(\.|$)";

        private const string PromisesAndTheUpdateCode = @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.(DeliverySources|BackgroundServices|Update)(\.|$)|^Lighthouse\.Backend\.Modules\.Promises(\.|$)";

        private const string RefinementReportsAccessAndLicensing = @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.(Refinement|Reports|Auth|Authorization|Licensing)(\.|$)";

        private const string RetiredLeafPattern = "WorkTrackingIntegrationPattern";

        private const string RetiredLeafRule = "WorkTrackingIntegration_IsALeafInputAdapter";

        private const string RetiredRepositoryRule = "WorkTrackingConnectors_DoNotDependOnAnyRepository";

        private const string MadeUpModule = @"^Lighthouse\.Backend\.Modules\.TrackerIntegrationFixtures\.Boundaries(\.|$)";

        private const string MadeUpDomain = @"^Lighthouse\.Backend\.Modules\.TrackerIntegrationFixtures\.Boundaries\.Domain(\.|$)";

        private const string MadeUpApplication = @"^Lighthouse\.Backend\.Modules\.TrackerIntegrationFixtures\.Boundaries\.Application(\.|$)";

        private const string MadeUpAdapters = @"^Lighthouse\.Backend\.Modules\.TrackerIntegrationFixtures\.Boundaries\.Adapters(\.|$)";

        private const string MadeUpLegalCallers = @"^Lighthouse\.Backend\.TrackerIntegrationBoundariesFixtures\.(Api|WorkItems)(\.|$)";

        private const string MadeUpCompositionRoot = "Lighthouse.Backend.TrackerIntegrationBoundariesFixtures.CompositionRoot.ATrackerCompositionRoot";

        private const string MadeUpCompositionRootAndItsNestedTypes = @"^Lighthouse\.Backend\.TrackerIntegrationBoundariesFixtures\.CompositionRoot\.ATrackerCompositionRoot(\+|$)";

        private const string MadeUpOutsiders = @"^Lighthouse\.Backend\.(Modules\.TrackerIntegrationFixtures\.Boundaries|TrackerIntegrationBoundariesFixtures)(\.|$)";

        private const string MadeUpOutboundCases = "Lighthouse.Backend.Modules.TrackerIntegrationFixtures.BoundariesOutbound";

        private const string AnEntryWithNoEdge = "Lighthouse.Backend.TrackerIntegrationBoundariesFixtures.NoSuchCaller";

        private const string AnEdgeThatIsGone = "Lighthouse.Backend.TrackerIntegrationBoundariesFixtures.NoSuchService -> Lighthouse.Backend.TrackerIntegrationBoundariesFixtures.NoSuchTarget";

        private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

        private static readonly ModuleWording TrackerIntegrationWording = new("Tracker integration", "the API, the Work Items module and Promises' Application");

        // The outside types that reach Tracker integration's Domain. The list only shrinks.
        private static readonly string[] OutsideTypesReachingTheDomain =
        [
            "Lighthouse.Backend.Modules.WorkItems.Application.WorkItemService",
        ];

        // The outside types, besides the API, the composition root, Work Items and Promises' Application, that reach Tracker
        // integration's Application. The list only shrinks.
        private static readonly string[] OtherOutsideTypesReachingTheApplication =
        [
            "Lighthouse.Backend.Factories.DemoDataFactory",
            "Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.PortfolioUpdater",
            "Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.TeamUpdater",
            "Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.UpdateServiceBase`1",
            "Lighthouse.Backend.Services.Implementation.WriteBackService",
        ];

        // The outside types, besides the composition root, that reach Tracker integration's Adapters. The list only shrinks.
        private static readonly string[] OtherOutsideTypesReachingTheAdapters =
        [
            "Lighthouse.Backend.API.WorkTrackingSystemConnectionController",
            "Lighthouse.Backend.API.WorkTrackingSystemConnectionsController",
        ];

        // The edges from Tracker integration's Application into EF Core, the data layer, the API, any module's adapters,
        // the stored records, the key ring and demo data. The factories and the auth schema name the connectors until the
        // composition root builds them; the OAuth and connection health services keep their own records. The list only shrinks.
        private static readonly string[] EdgesOfTheApplicationBelowItIntoAdaptersOrThePlatform =
        [
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.AuthenticationMethodSchema -> Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.AzureDevOps.AzureDevOpsWorkTrackingOptionNames",
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.AuthenticationMethodSchema -> Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Jira.JiraWorkTrackingOptionNames",
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.ConnectionHealth.ConnectionHealthService -> Lighthouse.Backend.Services.Implementation.Encryption.ConnectionSecrets",
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.ConnectionHealth.ConnectionHealthService -> Lighthouse.Backend.Services.Implementation.Repositories.ConnectionHealthVerdictRepository",
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.ConnectionHealth.ConnectionHealthService -> Lighthouse.Backend.Services.Implementation.Repositories.RepositoryBase`1",
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.ConnectionHealth.ConnectionHealthService -> Lighthouse.Backend.Services.Interfaces.Repositories.IRepository`1",
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.OAuth.OAuthService -> Lighthouse.Backend.Services.Implementation.Encryption.UnreadableSecretException",
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.OAuth.OAuthService -> Lighthouse.Backend.Services.Interfaces.Repositories.IRepository`1",
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.WorkTrackingConnectorFactory -> Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Csv.CsvWorkTrackingConnector",
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.WorkTrackingSystemFactory -> Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.AzureDevOps.AzureDevOpsWorkTrackingOptionNames",
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.WorkTrackingSystemFactory -> Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Jira.JiraWorkTrackingOptionNames",
        ];

        // The edges from Tracker integration into another context. The Jira connector merges the forecast block through a
        // port Tracker integration owns, so the module reaches no other context. The list only shrinks.
        private static readonly string[] EdgesOfTheModuleIntoOtherContexts = [];

        private static readonly string[] TheApplicationTypesThatNameConnectors =
        [
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.AuthenticationMethodSchema",
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.WorkTrackingConnectorFactory",
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.WorkTrackingSystemFactory",
        ];

        private static readonly string[] TheServicesThatKeepTheirOwnRecords =
        [
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.ConnectionHealth.ConnectionHealthService",
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.OAuth.OAuthService",
        ];

        private static readonly string TheLayersBelowTheApplicationAndThePlatform =
            $"(?:{EntityFrameworkCore})|(?:{ApplicationCoreRules.TheData})|(?:{ApplicationCoreRules.TheApi})|(?:{AnyModuleAdapters})" +
            $"|(?:{RepositoryRecognition.StoredRecordsPattern})|(?:{TheKeyRing})|(?:{TheDemoData})";

        private static readonly string OtherContexts =
            $"(?:{OtherModules})|(?:{PromisesAndTheUpdateCode})|(?:{RefinementReportsAccessAndLicensing})";

        private static readonly string TrackerIntegrationLegalCallers = $"(?:{ApplicationCoreRules.TheApi})|(?:{WorkItemsModule})|(?:{PromisesApplicationOutsideItsPolicies})";

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(TrackerIntegrationModuleBoundariesArchUnitTest).Assembly));

        public enum ListedRule
        {
            OutsideTypesReachingTheDomain,
            OutsideTypesReachingTheApplication,
            OutsideTypesReachingTheAdapters,
            ApplicationEdgesBelowItIntoAdaptersOrThePlatform,
            ModuleEdgesIntoOtherContexts,
            ModuleEdgesIntoRepositories,
        }

        // The repository edges are the Application's listed edges into the stored records, so the two lists cannot drift apart.
        private static string[] EdgesOfTheModuleIntoRepositories =>
            [.. EdgesOfTheApplicationBelowItIntoAdaptersOrThePlatform.Where(edge => Regex.IsMatch(TargetOf(edge), RepositoryRecognition.StoredRecordsPattern, RegexOptions.None, PatternTimeout))];

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void TheTrackerIntegrationDomain_ReachesOnlyTheSharedKernelAndTheBaseLibrary()
        {
            var verdict = ModuleRules.DomainReachesOnlyTheKernelAndTheBaseLibrary(ProductionModule.Value, TrackerIntegrationDomain);

            ThenTheRuleHolds(verdict.Check, "Tracker integration's Domain");
        }

        [TestCase(ListedRule.OutsideTypesReachingTheDomain)]
        [TestCase(ListedRule.OutsideTypesReachingTheApplication)]
        [TestCase(ListedRule.OutsideTypesReachingTheAdapters)]
        [TestCase(ListedRule.ApplicationEdgesBelowItIntoAdaptersOrThePlatform)]
        [TestCase(ListedRule.ModuleEdgesIntoOtherContexts)]
        [TestCase(ListedRule.ModuleEdgesIntoRepositories)]
        public void AListedTrackerIntegrationRule_MatchesWhatTheCompiledCodeReaches_InBothDirections(ListedRule rule)
        {
            var (check, _) = TheProductRule(rule);

            ThenTheRuleHolds(check, rule.ToString());
        }

        [TestCase(ListedRule.OutsideTypesReachingTheDomain)]
        [TestCase(ListedRule.OutsideTypesReachingTheApplication)]
        [TestCase(ListedRule.OutsideTypesReachingTheAdapters)]
        [TestCase(ListedRule.ApplicationEdgesBelowItIntoAdaptersOrThePlatform)]
        [TestCase(ListedRule.ModuleEdgesIntoOtherContexts)]
        [TestCase(ListedRule.ModuleEdgesIntoRepositories)]
        public void EveryEntryDeletedFromATrackerIntegrationList_FailsTheRuleAndIsNamed_AndAnEntryWithNoEdgeIsNamedForDeletion(ListedRule rule)
        {
            var (_, withList) = TheProductRule(rule);

            ThenEachDeletedEntryIsNamedAndAMadeUpEntryIsNamedForDeletion(rule, withList);
        }

        [Test]
        public void TrackerIntegration_ReachesNoOtherContext()
        {
            Assert.That(
                EdgesOfTheModuleIntoOtherContexts,
                Is.Empty,
                "Tracker integration's list of edges into other contexts is not empty. " +
                "Why it matters: the module is a leaf that translates what trackers send, and another context that needs something from " +
                "a connector implements a port Tracker integration owns. How to fix: undo the edge the rule names, through such a port.");
        }

        [Test]
        public void TheFactoriesAndTheAuthSchemaThatNameConnectors_AreListedByTheirExactNames_AndNoOtherApplicationTypeReachesTheAdapters()
        {
            var sourcesReachingTheAdapters = EdgesOfTheApplicationBelowItIntoAdaptersOrThePlatform
                .Where(edge => Regex.IsMatch(TargetOf(edge), TrackerIntegrationAdapters, RegexOptions.None, PatternTimeout))
                .Select(SourceOf)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal);

            Assert.That(
                sourcesReachingTheAdapters,
                Is.EqualTo(TheApplicationTypesThatNameConnectors),
                "The Application types listed as reaching Tracker integration's own Adapters are not exactly the two factories and the auth " +
                "schema. Why it matters: those three name the connectors until the composition root builds them, and each exemption is " +
                "one exact type, never a namespace or a layer. How to fix: undo the new edge the rule names; never add a type to pass.");
        }

        [Test]
        public void OnlyTheOAuthAndConnectionHealthServices_ReachARepository_AndBothAreListed()
        {
            var sourcesReachingARepository = EdgesOfTheModuleIntoRepositories.Select(SourceOf).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);

            Assert.That(
                sourcesReachingARepository,
                Is.EqualTo(TheServicesThatKeepTheirOwnRecords),
                "The module types listed as reaching a repository are not exactly the OAuth service and the connection health service. " +
                "Why it matters: a connector only translates what the tracker sent and is handed what it needs about stored records; " +
                "only the two services that keep their own records read them. How to fix: hand the connector the values from its caller.");
        }

        [Test]
        public void TheOldLeafRuleOverTheConnectorFolders_IsRetired_SoOnlyTheModuleRulesGuardTrackerIntegration()
        {
            var formerRules = typeof(ModuleBoundariesArchUnitTest);
            const BindingFlags anyMember = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    formerRules.GetField(RetiredLeafPattern, anyMember),
                    Is.Null,
                    $"{formerRules.Name}.{RetiredLeafPattern} still exists. Why it matters: it keeps the connector folders the module " +
                    "replaced alive as a second, looser definition of Tracker integration. How to fix: delete it and point every rule " +
                    "that read it at the module namespace.");
                Assert.That(
                    formerRules.GetMethod(RetiredLeafRule, anyMember),
                    Is.Null,
                    $"{formerRules.Name}.{RetiredLeafRule} still exists. Why it matters: the module's rule against reaching other contexts " +
                    "replaces it, with its one listed edge. How to fix: delete the old rule.");
                Assert.That(
                    formerRules.GetMethod(RetiredRepositoryRule, anyMember),
                    Is.Null,
                    $"{formerRules.Name}.{RetiredRepositoryRule} still exists. Why it matters: the module's repository rule replaces it, with " +
                    "the OAuth and connection health services listed by name instead of exempted. How to fix: delete the old rule and its exemption.");
            }
        }

        [TestCase("Scripts/test-selection/path-classifier.sh")]
        [TestCase("Scripts/test-selection/dev-test.ps1")]
        public void LiveSuiteSelection_NoLongerKnowsTheOldConnectorFolders_AndStillSelectsEachTrackerUnderTheModule(string selectionScript)
        {
            var selection = File.ReadAllText(Path.Combine(RepositoryRoot(), selectionScript.Replace('/', Path.DirectorySeparatorChar)));

            using (Assert.EnterMultipleScope())
            {
                foreach (var oldFolder in new[] { "/WorkTrackingConnectors", "/Factories" })
                {
                    Assert.That(
                        selection,
                        Does.Not.Contain(oldFolder),
                        $"{selectionScript} still selects live suites by the old folder {oldFolder}. Why it matters: nothing of Tracker " +
                        "integration lives there any more, so the alternative only hides which paths really select a tracker. How to fix: " +
                        "delete that alternative and keep the module paths.");
                }

                foreach (var moduleFolder in new[] { "Jira/", "AzureDevOps/", "Linear/", "ServiceNow/", "(Auth|OAuth)/" })
                {
                    Assert.That(
                        selection,
                        Does.Contain($"Modules/TrackerIntegration/Adapters/Connectors/{moduleFolder}"),
                        $"{selectionScript} no longer selects by the module folder Adapters/Connectors/{moduleFolder}. Why it matters: a change " +
                        "to that connector must still run its live suite. How to fix: keep the module path alternative.");
                }
            }
        }

        [Test]
        public void ATrackerIntegrationDomainTypeAskingTheLicence_IsNamed_AndItsCompliantTwinIsNot()
        {
            var verdict = ModuleRules.DomainReachesOnlyTheKernelAndTheBaseLibrary(TestModule.Value, MadeUpDomain);

            ThenTheFailureNamesOnly(verdict.Check, typeof(ADomainRuleAskingTheLicence).FullName!, [typeof(ATranslationRule).FullName!], "the Domain rule");
        }

        [Test]
        public void AnOutsideTypeOffTheList_ReachingTheTrackerIntegrationDomain_IsNamed_AndTheListedCallerIsNot()
        {
            var verdict = DomainInboundRule(TestModule.Value, [typeof(AListedDomainCaller).FullName!], MadeUpModule, MadeUpDomain, MadeUpOutsiders);

            ThenTheFailureNamesOnly(verdict.Check, typeof(ANewcomerReachingTheDomain).FullName!, [typeof(AListedDomainCaller).FullName!], "the Domain inbound rule");
        }

        [Test]
        public void AnOutsideTypeOffTheList_ReachingTheTrackerIntegrationApplication_IsNamed_AndTheApiWorkItemsTheCompositionRootAndTheListedCallerAreNot()
        {
            var verdict = ApplicationInboundRule(TestModule.Value, [typeof(AListedApplicationCaller).FullName!], MadeUpModule, MadeUpApplication, MadeUpLegalCallers, MadeUpCompositionRootAndItsNestedTypes, MadeUpOutsiders);

            ThenTheFailureNamesOnly(
                verdict.Check,
                typeof(ANewcomerReachingTheApplication).FullName!,
                [
                    typeof(AListedApplicationCaller).FullName!,
                    "Lighthouse.Backend.TrackerIntegrationBoundariesFixtures.Api.AConnectionsController",
                    "Lighthouse.Backend.TrackerIntegrationBoundariesFixtures.WorkItems.AWorkItemRefresh",
                    MadeUpCompositionRoot,
                ],
                "the Application inbound rule");
        }

        [Test]
        public void AnOutsideTypeOffTheList_ReachingTheTrackerIntegrationAdapters_IsNamed_AndTheCompositionRootAndTheListedCallerAreNot()
        {
            var verdict = AdaptersInboundRule(TestModule.Value, MadeUpCompositionRoot, [typeof(AListedAdaptersCaller).FullName!], MadeUpModule, MadeUpAdapters, MadeUpOutsiders);

            // The rule's own text names the composition root as the allowed caller, so the root is checked as an offender line only.
            ThenTheFailureNamesOnly(verdict.Check, typeof(ANewcomerReachingTheAdapters).FullName!, [typeof(AListedAdaptersCaller).FullName!, $"{MadeUpCompositionRoot} reaches"], "the Adapters inbound rule");
        }

        [TestCaseSource(nameof(ApplicationTypesReachingBelowItIntoAdaptersOrThePlatform))]
        public void ATrackerIntegrationApplicationTypeReachingBelowItAnAdapterOrThePlatformOffTheList_IsNamed(string caseName, string newEdge)
        {
            var verdict = ApplicationOutboundRule(TestModule.Value, OutboundCase(caseName, ".Application"), OutboundCase(caseName, ".Adapters"), []);

            ThenTheFailureNamesOnly(verdict.Check, $"Not on the list, and offending: {newEdge}. ", [], "the Application outbound rule");
        }

        [Test]
        public void ATrackerIntegrationApplicationTypeBuildingAConnectorThroughAListedEdge_PassesTheRule()
        {
            var verdict = ApplicationOutboundRule(
                TestModule.Value,
                OutboundCase("ListedConnectorConstruction", ".Application"),
                OutboundCase("ListedConnectorConstruction", ".Adapters"),
                [$"{MadeUpOutboundCases}.ListedConnectorConstruction.Application.AConnectorFactory -> {MadeUpOutboundCases}.ListedConnectorConstruction.Adapters.Connectors.AConnector"]);

            ThenTheRuleHolds(verdict.Check, "a listed connector construction edge");
        }

        [TestCaseSource(nameof(ModuleTypesReachingAnotherContext))]
        public void ATrackerIntegrationTypeReachingAnotherContextOffTheList_IsNamed(string caseName, string newEdge)
        {
            var verdict = OtherContextsRule(TestModule.Value, OutboundCase(caseName, string.Empty), [ListedRendererEdge(caseName)]);

            ThenTheFailureNamesOnly(verdict.Check, $"Not on the list, and offending: {newEdge}. ", [ListedRendererEdge(caseName)], "the other-contexts rule");
        }

        [Test]
        public void ATrackerIntegrationConnectorReachingARepository_IsNamed_AndTheListedServiceKeepingItsRecordsIsNot()
        {
            var listedService = $"{MadeUpOutboundCases}.ConnectorReadingARepository.Application.AServiceKeepingItsRecords -> {RepositoryContract}";
            var verdict = RepositoriesRule(TestModule.Value, OutboundCase("ConnectorReadingARepository", string.Empty), [listedService]);

            ThenTheFailureNamesOnly(
                verdict.Check,
                $"Not on the list, and offending: {MadeUpOutboundCases}.ConnectorReadingARepository.Adapters.Connectors.AConnectorReadingStoredFeatures -> {RepositoryContract}. ",
                [listedService],
                "the repository rule");
        }

        private static string RepositoryContract => "Lighthouse.Backend.Services.Interfaces.Repositories.IRepository`1";

        private static IEnumerable<TestCaseData> ApplicationTypesReachingBelowItIntoAdaptersOrThePlatform()
        {
            yield return OutboundCaseData("HoldingADbSet", "AConnectionServiceHoldingADbSet", "Microsoft.EntityFrameworkCore.DbSet`1", "ATrackerIntegrationApplicationTypeHoldingAnEntityFrameworkCoreDbSet_IsNamed");
            yield return OutboundCaseData("ReadingTheDataLayer", "AConnectionServiceReadingTheDataLayer", "Lighthouse.Backend.Data.LighthouseAppContext", "ATrackerIntegrationApplicationTypeReadingTheDatabaseContext_IsNamed");
            yield return OutboundCaseData("ShapingAnApiType", "AConnectionServiceShapingAnApiType", "Lighthouse.Backend.API.DTO.FeatureDto", "ATrackerIntegrationApplicationTypeShapingAnApiType_IsNamed");
            yield return OutboundCaseData(
                "BuildingAnUnlistedConnector", "AConnectorFactory", $"{MadeUpOutboundCases}.BuildingAnUnlistedConnector.Adapters.Connectors.AConnector", "ATrackerIntegrationApplicationTypeBuildingAConnectorOffTheList_IsNamed");
            yield return OutboundCaseData(
                "RunningAnotherModulesAdapter", "AConnectionServiceRunningAForecastJob", "Lighthouse.Backend.Modules.Forecasting.Adapters.Background.ForecastUpdater", "ATrackerIntegrationApplicationTypeRunningAnotherModulesAdapter_IsNamed");
            yield return OutboundCaseData("ReadingAnUnlistedRepository", "AConnectionServiceReadingCredentials", "Lighthouse.Backend.Services.Implementation.Repositories.OAuthCredentialRepository", "ATrackerIntegrationApplicationTypeReadingARepositoryOffTheList_IsNamed");
            yield return OutboundCaseData("ReadingTheKeyRing", "AConnectionServiceReadingSecrets", "Lighthouse.Backend.Services.Implementation.Encryption.ConnectionSecrets", "ATrackerIntegrationApplicationTypeReadingTheKeyRingOffTheList_IsNamed");
        }

        private static TestCaseData OutboundCaseData(string caseName, string dependent, string target, string name)
            => new TestCaseData(caseName, $"{MadeUpOutboundCases}.{caseName}.Application.{dependent} -> {target}").SetName(name);

        private static IEnumerable<TestCaseData> ModuleTypesReachingAnotherContext()
        {
            yield return OtherContextCase("ReachingWorkItems", "Lighthouse.Backend.Modules.WorkItems.Application.IWorkItemService", "ATrackerIntegrationTypeReachingWorkItems_IsNamed");
            yield return OtherContextCase("ReachingForecasting", "Lighthouse.Backend.Modules.Forecasting.Application.IForecastService", "ATrackerIntegrationTypeReachingForecasting_IsNamed");
            yield return OtherContextCase("ReachingFlowMetrics", "Lighthouse.Backend.Modules.FlowMetrics.Application.IPortfolioMetricsService", "ATrackerIntegrationTypeReachingFlowMetrics_IsNamed");
            yield return OtherContextCase("ReachingASecondPromisesType", "Lighthouse.Backend.Modules.Promises.Application.IDeliveryForecastPublishingService", "ATrackerIntegrationTypeReachingPromisesBeyondTheListedRenderer_IsNamed");
            yield return OtherContextCase("ReachingTheUpdateCode", "Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.UpdateKey", "ATrackerIntegrationTypeReachingTheUpdateCode_IsNamed");
            yield return OtherContextCase("ReachingRefinement", "Lighthouse.Backend.Services.Interfaces.Refinement.IRefinementViewQuery", "ATrackerIntegrationTypeReachingRefinement_IsNamed");
            yield return OtherContextCase("ReachingReports", "Lighthouse.Backend.Services.Interfaces.Reports.IReportQueries", "ATrackerIntegrationTypeReachingReports_IsNamed");
            yield return OtherContextCase("ReachingAccess", "Lighthouse.Backend.Services.Interfaces.Authorization.IRbacAdministrationService", "ATrackerIntegrationTypeReachingAccess_IsNamed");
            yield return OtherContextCase("ReachingTheLicence", "Lighthouse.Backend.Services.Interfaces.Licensing.ILicenseService", "ATrackerIntegrationTypeReachingTheLicence_IsNamed");
        }

        private static TestCaseData OtherContextCase(string caseName, string target, string name)
            => new TestCaseData(caseName, $"{MadeUpOutboundCases}.{caseName}.Adapters.Connectors.AConnector -> {target}").SetName(name);

        // Every other-context case also holds a connector using the forecast block renderer through a listed edge, which the rule must not name.
        private static string ListedRendererEdge(string caseName)
            => $"{MadeUpOutboundCases}.{caseName}.Adapters.Connectors.AConnectorMergingTheForecastBlock -> Lighthouse.Backend.Modules.Promises.Application.IDeliveryForecastBlockRenderer";

        private static (Action Check, Func<IReadOnlyCollection<string>, Action> WithList) TheProductRule(ListedRule rule)
        {
            var module = ProductionModule.Value;

            return rule switch
            {
                ListedRule.OutsideTypesReachingTheDomain => Inbound(
                    listed => DomainInboundRule(module, listed, TrackerIntegrationModule, TrackerIntegrationDomain, ModuleRules.AnyNamespace), OutsideTypesReachingTheDomain),
                ListedRule.OutsideTypesReachingTheApplication => Inbound(
                    listed => ApplicationInboundRule(module, listed, TrackerIntegrationModule, TrackerIntegrationApplication, TrackerIntegrationLegalCallers, TheCompositionRootAndItsNestedTypes, ModuleRules.AnyNamespace),
                    OtherOutsideTypesReachingTheApplication),
                ListedRule.OutsideTypesReachingTheAdapters => Inbound(
                    listed => AdaptersInboundRule(module, ModuleRules.TheCompositionRoot, listed, TrackerIntegrationModule, TrackerIntegrationAdapters, ModuleRules.AnyNamespace),
                    OtherOutsideTypesReachingTheAdapters),
                ListedRule.ApplicationEdgesBelowItIntoAdaptersOrThePlatform => Outbound(
                    listed => ApplicationOutboundRule(module, TrackerIntegrationApplication, TrackerIntegrationAdapters, listed), EdgesOfTheApplicationBelowItIntoAdaptersOrThePlatform),
                ListedRule.ModuleEdgesIntoOtherContexts => Outbound(listed => OtherContextsRule(module, TrackerIntegrationModule, listed), EdgesOfTheModuleIntoOtherContexts),
                ListedRule.ModuleEdgesIntoRepositories => Outbound(listed => RepositoriesRule(module, TrackerIntegrationModule, listed), EdgesOfTheModuleIntoRepositories),
                _ => throw new ArgumentOutOfRangeException(nameof(rule)),
            };
        }

        private static (Action, Func<IReadOnlyCollection<string>, Action>) Inbound(Func<IReadOnlyCollection<string>, InboundVerdict> rule, IReadOnlyCollection<string> listed)
            => (rule(listed).Check, other => rule(other).Check);

        private static (Action, Func<IReadOnlyCollection<string>, Action>) Outbound(Func<IReadOnlyCollection<string>, OutboundVerdict> rule, IReadOnlyCollection<string> listed)
            => (rule(listed).Check, other => rule(other).Check);

        private static InboundVerdict DomainInboundRule(ModuleDefinition module, IReadOnlyCollection<string> listed, string moduleNamespaces, string domain, string outside)
            => ModuleRules.DomainIsReachedFromOutsideOnlyBy(module, listed, moduleNamespaces, domain, outside, TrackerIntegrationWording);

        private static InboundVerdict ApplicationInboundRule(ModuleDefinition module, IReadOnlyCollection<string> listed, string moduleNamespaces, string application, string legalCallers, string compositionRoot, string outside)
            => ModuleRules.ApplicationIsReachedFromOutsideOnlyBy(module, listed, moduleNamespaces, application, legalCallers, compositionRoot, outside, TrackerIntegrationWording);

        // The shared rule allows only the composition root; the listed outside types join it, each by its exact name.
        private static InboundVerdict AdaptersInboundRule(ModuleDefinition module, string compositionRoot, IReadOnlyCollection<string> listed, string moduleNamespaces, string adapters, string outside)
        {
            var verdict = ModuleRules.AdaptersAreReachedFromOutsideOnlyByTheCompositionRoot(module, compositionRoot, moduleNamespaces, adapters, outside, TrackerIntegrationWording);

            return verdict with { Listed = [compositionRoot, .. listed], Rule = $"{verdict.Rule} Besides the composition root, only the listed types may; the list only shrinks." };
        }

        private static OutboundVerdict ApplicationOutboundRule(ModuleDefinition module, string application, string ownAdapters, IReadOnlyCollection<string> listed)
        {
            var below = $"{TheLayersBelowTheApplicationAndThePlatform}|(?:{ownAdapters})";

            return ModuleRules.OutboundEdges(
                module,
                application,
                below,
                listed,
                $"Types in Tracker integration's Application, namespaces matching \"{application}\", should reach namespaces matching \"{below}\" " +
                "only through the listed edges, because the Application is the module's port: it says what Work Items and Promises may ask of " +
                "a tracker, and reaches storage, the key ring and the connectors only where the list allows. An Application type that holds a " +
                "DbSet or the database context, shapes an API type, builds or runs an adapter, or reads a repository or a secret ties the port " +
                "to how records are stored and how trackers are reached. Take a port in the constructor and let the composition root hand in the adapter.");
        }

        private static OutboundVerdict OtherContextsRule(ModuleDefinition module, string trackerIntegration, IReadOnlyCollection<string> listed)
            => ModuleRules.OutboundEdges(
                module,
                trackerIntegration,
                OtherContexts,
                listed,
                $"Types in Tracker integration, namespaces matching \"{trackerIntegration}\", should reach other contexts, namespaces matching " +
                $"\"{OtherContexts}\", only through the listed edges, because Tracker integration is a leaf: it translates what the trackers send " +
                "and write, and Work Items, Forecasting, Flow metrics, Promises, Refinement, Reports, Access and Licensing call it, never the " +
                "other way round. Every new edge makes reading a tracker wait on another context's decision. Hand the connector the values it " +
                "needs from its caller.");

        private static OutboundVerdict RepositoriesRule(ModuleDefinition module, string trackerIntegration, IReadOnlyCollection<string> listed)
            => ModuleRules.OutboundEdges(
                module,
                trackerIntegration,
                RepositoryRecognition.StoredRecordsPattern,
                listed,
                $"Types in Tracker integration, namespaces matching \"{trackerIntegration}\", should reach the stored records, namespaces matching " +
                $"\"{RepositoryRecognition.StoredRecordsPattern}\", only through the listed edges, because a connector only translates what the " +
                "tracker sent: whatever it needs to know about stored records, such as which Features this instance holds, is handed to it by " +
                "the caller. Only the services that keep their own records, credentials and health verdicts, read them. Hand the values in instead.");

        private static string SourceOf(string edge) => edge[..edge.IndexOf(" -> ", StringComparison.Ordinal)];

        private static string TargetOf(string edge) => edge[(edge.IndexOf(" -> ", StringComparison.Ordinal) + 4)..];

        // Each made-up outbound case lives in a namespace of its own, so the rule handed one sees only that case.
        private static string OutboundCase(string caseName, string layer)
            => $@"^{Regex.Escape($"{MadeUpOutboundCases}.{caseName}{layer}")}(\.|$)";

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory?.Parent, Is.Not.Null, "Lighthouse.sln could not be found to anchor the read of the selection scripts.");

            return directory!.Parent!.FullName;
        }

        private static void ThenTheRuleHolds(Action check, string subject)
            => Assert.DoesNotThrow(
                () => check(),
                $"The rule for {subject} failed on the compiled product; the failure above names each offender, and each listed entry that no " +
                "longer offends. Why it matters: Tracker integration keeps its own boundaries only while every rule holds and its lists match " +
                "the code. How to fix: undo the new dependency the failure names, or delete the entry it names for deletion; never add an entry to pass.");

        private static void ThenTheFailureNamesOnly(Action check, string offender, IReadOnlyCollection<string> twins, string rule)
        {
            var failure = Assert.Throws<AssertionException>(
                () => check(),
                $"{rule} passed with {offender} breaking it. Why it matters: a rule that misses a made-up offender misses a real one too, for " +
                "example because it looks in the wrong namespace. How to fix: make the rule look at every type its namespaces hold.");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure!.Message, Does.Contain(offender), $"{rule} must name the offender {offender}, so the reader knows what to undo.");

                foreach (var twin in twins)
                {
                    Assert.That(failure.Message, Does.Not.Contain(twin), $"{rule} named the compliant twin {twin}. Why it matters: a rule that flags allowed code gets switched off. How to fix: report only what breaks the rule.");
                }
            }
        }

        private static void ThenEachDeletedEntryIsNamedAndAMadeUpEntryIsNamedForDeletion(ListedRule rule, Func<IReadOnlyCollection<string>, Action> withList)
        {
            var listed = ListOf(rule);
            var anEntryWithNoEdge = rule is ListedRule.OutsideTypesReachingTheDomain or ListedRule.OutsideTypesReachingTheApplication or ListedRule.OutsideTypesReachingTheAdapters
                ? AnEntryWithNoEdge
                : AnEdgeThatIsGone;

            foreach (var entry in listed)
            {
                var failure = Assert.Throws<AssertionException>(() => withList([.. listed.Where(other => other != entry)])(), $"{rule} passed with {entry} deleted from its list. Why it matters: an entry the code still needs must fail when it goes. How to fix: compare the list with the code both ways.");
                Assert.That(failure!.Message, Does.Contain($"Not on the list, and offending: {entry}"), $"{rule} must name {entry} as offending once it is off the list.");
            }

            var stale = Assert.Throws<AssertionException>(() => withList([.. listed, anEntryWithNoEdge])(), $"{rule} passed with an entry that reaches nothing. Why it matters: a list that keeps a gone entry lets that dependency come back unseen. How to fix: compare the list with the code both ways.");
            Assert.That(stale!.Message, Does.Contain($"On the list, but no longer offending: {anEntryWithNoEdge}"), $"{rule} must name the entry with no edge for deletion.");
        }

        private static string[] ListOf(ListedRule rule) => rule switch
        {
            ListedRule.OutsideTypesReachingTheDomain => OutsideTypesReachingTheDomain,
            ListedRule.OutsideTypesReachingTheApplication => OtherOutsideTypesReachingTheApplication,
            ListedRule.OutsideTypesReachingTheAdapters => OtherOutsideTypesReachingTheAdapters,
            ListedRule.ApplicationEdgesBelowItIntoAdaptersOrThePlatform => EdgesOfTheApplicationBelowItIntoAdaptersOrThePlatform,
            ListedRule.ModuleEdgesIntoOtherContexts => EdgesOfTheModuleIntoOtherContexts,
            ListedRule.ModuleEdgesIntoRepositories => EdgesOfTheModuleIntoRepositories,
            _ => throw new ArgumentOutOfRangeException(nameof(rule)),
        };
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.Boundaries.Domain
{
    internal static class ATranslationRule
    {
        public static int Weight(global::Lighthouse.Backend.Models.Feature feature) => feature.Name.Length;
    }

    internal static class ADomainRuleAskingTheLicence
    {
        public static bool Allows(global::Lighthouse.Backend.Services.Interfaces.Licensing.ILicenseService licence) => licence is not null;
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.Boundaries.Application
{
    internal static class AConnectionAnswerService
    {
        public static int Answer() => global::Lighthouse.Backend.Modules.TrackerIntegrationFixtures.Boundaries.Domain.ATranslationRule.Weight(new global::Lighthouse.Backend.Models.Feature());
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.Boundaries.Adapters.Connectors
{
    internal static class AConnector
    {
        public static int Read() => global::Lighthouse.Backend.Modules.TrackerIntegrationFixtures.Boundaries.Application.AConnectionAnswerService.Answer();
    }
}

namespace Lighthouse.Backend.TrackerIntegrationBoundariesFixtures.Api
{
    internal static class AConnectionsController
    {
        public static int Get() => global::Lighthouse.Backend.Modules.TrackerIntegrationFixtures.Boundaries.Application.AConnectionAnswerService.Answer();
    }
}

namespace Lighthouse.Backend.TrackerIntegrationBoundariesFixtures.WorkItems
{
    internal static class AWorkItemRefresh
    {
        public static int Refresh() => global::Lighthouse.Backend.Modules.TrackerIntegrationFixtures.Boundaries.Application.AConnectionAnswerService.Answer();
    }
}

namespace Lighthouse.Backend.TrackerIntegrationBoundariesFixtures.CompositionRoot
{
    internal static class ATrackerCompositionRoot
    {
        public static int Register() => global::Lighthouse.Backend.Modules.TrackerIntegrationFixtures.Boundaries.Adapters.Connectors.AConnector.Read() + Registrations.Register();

        private static class Registrations
        {
            public static int Register() => global::Lighthouse.Backend.Modules.TrackerIntegrationFixtures.Boundaries.Application.AConnectionAnswerService.Answer();
        }
    }
}

namespace Lighthouse.Backend.TrackerIntegrationBoundariesFixtures.ListedCallers
{
    internal static class AListedDomainCaller
    {
        public static int Answer() => global::Lighthouse.Backend.Modules.TrackerIntegrationFixtures.Boundaries.Domain.ATranslationRule.Weight(new global::Lighthouse.Backend.Models.Feature());
    }

    internal static class AListedApplicationCaller
    {
        public static int Answer() => global::Lighthouse.Backend.Modules.TrackerIntegrationFixtures.Boundaries.Application.AConnectionAnswerService.Answer();
    }

    internal static class AListedAdaptersCaller
    {
        public static int Answer() => global::Lighthouse.Backend.Modules.TrackerIntegrationFixtures.Boundaries.Adapters.Connectors.AConnector.Read();
    }
}

namespace Lighthouse.Backend.TrackerIntegrationBoundariesFixtures.DomainNewcomer
{
    internal static class ANewcomerReachingTheDomain
    {
        public static int Answer() => global::Lighthouse.Backend.Modules.TrackerIntegrationFixtures.Boundaries.Domain.ATranslationRule.Weight(new global::Lighthouse.Backend.Models.Feature());
    }
}

namespace Lighthouse.Backend.TrackerIntegrationBoundariesFixtures.ApplicationNewcomer
{
    // Reaches the Application only inside an async method, which a dependency graph of a Release build would not see.
    internal static class ANewcomerReachingTheApplication
    {
        public static async Task<int> AnswerAsync()
        {
            await Task.Yield();

            return global::Lighthouse.Backend.Modules.TrackerIntegrationFixtures.Boundaries.Application.AConnectionAnswerService.Answer();
        }
    }
}

namespace Lighthouse.Backend.TrackerIntegrationBoundariesFixtures.AdaptersNewcomer
{
    internal static class ANewcomerReachingTheAdapters
    {
        public static int Answer() => global::Lighthouse.Backend.Modules.TrackerIntegrationFixtures.Boundaries.Adapters.Connectors.AConnector.Read();
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.BoundariesOutbound.HoldingADbSet.Application
{
    internal sealed class AConnectionServiceHoldingADbSet
    {
        public global::Microsoft.EntityFrameworkCore.DbSet<global::Lighthouse.Backend.Models.Feature>? Features { get; init; }
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.BoundariesOutbound.ReadingTheDataLayer.Application
{
    internal static class AConnectionServiceReadingTheDataLayer
    {
        public static bool Reads(global::Lighthouse.Backend.Data.LighthouseAppContext context) => context is not null;
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.BoundariesOutbound.ShapingAnApiType.Application
{
    internal static class AConnectionServiceShapingAnApiType
    {
        public static bool Shapes(global::Lighthouse.Backend.API.DTO.FeatureDto dto) => dto is not null;
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.BoundariesOutbound.BuildingAnUnlistedConnector.Adapters.Connectors
{
    internal sealed class AConnector
    {
        public int Reads { get; init; }
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.BoundariesOutbound.BuildingAnUnlistedConnector.Application
{
    internal static class AConnectorFactory
    {
        public static object Build() => new global::Lighthouse.Backend.Modules.TrackerIntegrationFixtures.BoundariesOutbound.BuildingAnUnlistedConnector.Adapters.Connectors.AConnector();
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.BoundariesOutbound.ListedConnectorConstruction.Adapters.Connectors
{
    internal sealed class AConnector
    {
        public int Reads { get; init; }
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.BoundariesOutbound.ListedConnectorConstruction.Application
{
    internal static class AConnectorFactory
    {
        public static object Build() => new global::Lighthouse.Backend.Modules.TrackerIntegrationFixtures.BoundariesOutbound.ListedConnectorConstruction.Adapters.Connectors.AConnector();
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.BoundariesOutbound.RunningAnotherModulesAdapter.Application
{
    internal static class AConnectionServiceRunningAForecastJob
    {
        public static bool Runs(global::Lighthouse.Backend.Modules.Forecasting.Adapters.Background.ForecastUpdater updater) => updater is not null;
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.BoundariesOutbound.ReadingAnUnlistedRepository.Application
{
    internal static class AConnectionServiceReadingCredentials
    {
        public static bool Reads(global::Lighthouse.Backend.Services.Implementation.Repositories.OAuthCredentialRepository credentials) => credentials is not null;
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.BoundariesOutbound.ReadingTheKeyRing.Application
{
    internal static class AConnectionServiceReadingSecrets
    {
        public static int Reads(global::Lighthouse.Backend.Models.WorkTrackingSystemConnection connection) => global::Lighthouse.Backend.Services.Implementation.Encryption.ConnectionSecrets.FieldsThatCannotBeRead(connection, null!).Count();
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.BoundariesOutbound.ReachingWorkItems.Adapters.Connectors
{
    internal static class AConnector
    {
        public static bool Reaches(global::Lighthouse.Backend.Modules.WorkItems.Application.IWorkItemService other) => other is not null;
    }

    internal static class AConnectorMergingTheForecastBlock
    {
        public static bool Merges(global::Lighthouse.Backend.Modules.Promises.Application.IDeliveryForecastBlockRenderer renderer) => renderer is not null;
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.BoundariesOutbound.ReachingForecasting.Adapters.Connectors
{
    internal static class AConnector
    {
        public static bool Reaches(global::Lighthouse.Backend.Modules.Forecasting.Application.IForecastService other) => other is not null;
    }

    internal static class AConnectorMergingTheForecastBlock
    {
        public static bool Merges(global::Lighthouse.Backend.Modules.Promises.Application.IDeliveryForecastBlockRenderer renderer) => renderer is not null;
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.BoundariesOutbound.ReachingFlowMetrics.Adapters.Connectors
{
    internal static class AConnector
    {
        public static bool Reaches(global::Lighthouse.Backend.Modules.FlowMetrics.Application.IPortfolioMetricsService other) => other is not null;
    }

    internal static class AConnectorMergingTheForecastBlock
    {
        public static bool Merges(global::Lighthouse.Backend.Modules.Promises.Application.IDeliveryForecastBlockRenderer renderer) => renderer is not null;
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.BoundariesOutbound.ReachingASecondPromisesType.Adapters.Connectors
{
    internal static class AConnector
    {
        public static bool Reaches(global::Lighthouse.Backend.Modules.Promises.Application.IDeliveryForecastPublishingService other) => other is not null;
    }

    internal static class AConnectorMergingTheForecastBlock
    {
        public static bool Merges(global::Lighthouse.Backend.Modules.Promises.Application.IDeliveryForecastBlockRenderer renderer) => renderer is not null;
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.BoundariesOutbound.ReachingTheUpdateCode.Adapters.Connectors
{
    internal static class AConnector
    {
        public static bool Reaches(global::Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.UpdateKey other) => other is not null;
    }

    internal static class AConnectorMergingTheForecastBlock
    {
        public static bool Merges(global::Lighthouse.Backend.Modules.Promises.Application.IDeliveryForecastBlockRenderer renderer) => renderer is not null;
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.BoundariesOutbound.ReachingRefinement.Adapters.Connectors
{
    internal static class AConnector
    {
        public static bool Reaches(global::Lighthouse.Backend.Services.Interfaces.Refinement.IRefinementViewQuery other) => other is not null;
    }

    internal static class AConnectorMergingTheForecastBlock
    {
        public static bool Merges(global::Lighthouse.Backend.Modules.Promises.Application.IDeliveryForecastBlockRenderer renderer) => renderer is not null;
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.BoundariesOutbound.ReachingReports.Adapters.Connectors
{
    internal static class AConnector
    {
        public static bool Reaches(global::Lighthouse.Backend.Services.Interfaces.Reports.IReportQueries other) => other is not null;
    }

    internal static class AConnectorMergingTheForecastBlock
    {
        public static bool Merges(global::Lighthouse.Backend.Modules.Promises.Application.IDeliveryForecastBlockRenderer renderer) => renderer is not null;
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.BoundariesOutbound.ReachingAccess.Adapters.Connectors
{
    internal static class AConnector
    {
        public static bool Reaches(global::Lighthouse.Backend.Services.Interfaces.Authorization.IRbacAdministrationService other) => other is not null;
    }

    internal static class AConnectorMergingTheForecastBlock
    {
        public static bool Merges(global::Lighthouse.Backend.Modules.Promises.Application.IDeliveryForecastBlockRenderer renderer) => renderer is not null;
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.BoundariesOutbound.ReachingTheLicence.Adapters.Connectors
{
    internal static class AConnector
    {
        public static bool Reaches(global::Lighthouse.Backend.Services.Interfaces.Licensing.ILicenseService other) => other is not null;
    }

    internal static class AConnectorMergingTheForecastBlock
    {
        public static bool Merges(global::Lighthouse.Backend.Modules.Promises.Application.IDeliveryForecastBlockRenderer renderer) => renderer is not null;
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.BoundariesOutbound.ConnectorReadingARepository.Adapters.Connectors
{
    internal static class AConnectorReadingStoredFeatures
    {
        public static bool Reads(global::Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<global::Lighthouse.Backend.Models.Feature> features) => features is not null;
    }
}

namespace Lighthouse.Backend.Modules.TrackerIntegrationFixtures.BoundariesOutbound.ConnectorReadingARepository.Application
{
    internal static class AServiceKeepingItsRecords
    {
        public static bool Reads(global::Lighthouse.Backend.Services.Interfaces.Repositories.IRepository<global::Lighthouse.Backend.Models.Feature> features) => features is not null;
    }
}
