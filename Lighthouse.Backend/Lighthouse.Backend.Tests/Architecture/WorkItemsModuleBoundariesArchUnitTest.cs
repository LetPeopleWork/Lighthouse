using System.Reflection;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.WorkItems.Contracts;
using Lighthouse.Backend.Modules.WorkItemsFixtures.Boundaries.Domain;
using Lighthouse.Backend.Modules.WorkItemsFixtures.Boundaries.Services;
using Lighthouse.Backend.Modules.WorkItemsFixtures.BoundariesOutbound.HoldingADbSet.Application;
using Lighthouse.Backend.Modules.WorkItemsFixtures.BoundariesOutbound.ListedFlowMetricsQuery.Application;
using Lighthouse.Backend.Modules.WorkItemsFixtures.BoundariesOutbound.ReachingAnUnlistedFlowMetricsType.Application;
using Lighthouse.Backend.Modules.WorkItemsFixtures.BoundariesOutbound.ReachingSetUpAndALooseService.Application;
using Lighthouse.Backend.Modules.WorkItemsFixtures.BoundariesOutbound.ReadingASecondLicenceType.Application;
using Lighthouse.Backend.Modules.WorkItemsFixtures.BoundariesOutbound.ReadingTheDataLayer.Application;
using Lighthouse.Backend.Modules.WorkItemsFixtures.BoundariesOutbound.ShapingAnApiType.Application;
using Lighthouse.Backend.WorkItemsBoundariesFixtures.ApplicationNewcomer;
using Lighthouse.Backend.WorkItemsBoundariesFixtures.Connectors;
using Lighthouse.Backend.WorkItemsBoundariesFixtures.DomainNewcomer;
using Lighthouse.Backend.WorkItemsBoundariesFixtures.ListedCallers;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Work Items is a module with rules of its own, checked by the shared module rules with Work Items' namespaces and
    /// wording and read from the compiled production assembly: every Work Items type lives in its layer and none is left
    /// under Services; its Domain reaches only the shared kernel and the base library; outside code reaches its Domain only
    /// from the listed types and its Application only from the API, the composition root, Flow metrics, Forecasting,
    /// Refinement, Promises' Application and the listed types; its Application reaches no EF Core, data-layer, API or
    /// module adapter type, and Flow metrics, Access, Licensing, Organisation set-up and the update queue only through the
    /// listed edges; its policies hold nothing but domain-event handlers; and no connector reaches a Work Items type. Every list is compared both ways.
    /// Flow metrics' own inbound list no longer names TeamDataService, which now only publishes that a Team was refreshed.
    /// The same rules run on made-up types: each offender is named, and its compliant twin is not.
    /// </summary>
    /// <remarks>
    /// The IL reader does not see types used only in a catch clause, an event or a named attribute argument.
    /// </remarks>
    [TestFixture]
    public class WorkItemsModuleBoundariesArchUnitTest
    {
        private const string WorkItemsModule = @"^Lighthouse\.Backend\.Modules\.WorkItems(\.|$)";

        private const string WorkItemsDomain = @"^Lighthouse\.Backend\.Modules\.WorkItems\.Domain(\.|$)";

        private const string WorkItemsApplication = @"^Lighthouse\.Backend\.Modules\.WorkItems\.(Application|Contracts)(\.|$)";

        private const string WorkItemsPolicies = @"^Lighthouse\.Backend\.Modules\.WorkItems\.Application\.Policies(\.|$)";

        private const string FlowMetricsApplication = @"^Lighthouse\.Backend\.Modules\.FlowMetrics\.(Application|Contracts)(\.|$)";

        // Promises' Application calls this module by right; its policies and its Domain do not, so they stay outside.
        private const string PromisesApplicationOutsideItsPolicies = @"^Lighthouse\.Backend\.Modules\.Promises\.Application(?!\.Policies(\.|$))(\.|$)";

        private const string TheCompositionRootAndItsNestedTypes = @"^Lighthouse\.Backend\.Program(\+|$)";

        private const string EntityFrameworkCore = @"^Microsoft\.EntityFrameworkCore(\.|$)";

        // The types still loose in Services.Implementation and Services.Interfaces that Organisation set-up's neighbours reach or must not reach.
        // They are matched by full name, nested types included, never as a namespace, so each rule sees exactly the types it saw before the move.
        internal const string StrayLooseImplementation = @"^(?:Lighthouse\.Backend\.Services\.Implementation\.(EstimateNormalizationResult|EstimateNormalizationBatchResult|EstimateNormalizer|FeatureListActiveWork|FeatureListOwners|FeatureListService|FeaturePositionMap|InProgressFeatureResolution|OrphanedFeatureCleanupService)|Lighthouse\.Backend\.Modules\.Forecasting\.Application\.WriteBack\.(WriteBackCollector|WriteBackRoundContext|WriteBackRound|WriteBackService|WriteBackTriggerService))(\+.*)?$";

        internal const string StrayLooseInterfaces = @"^(?:Lighthouse\.Backend\.Services\.Interfaces\.(IFeatureListService|FeatureListRead|TeamAtWork|IFeaturePositionMap|IOrphanedFeatureCleanupService)|Lighthouse\.Backend\.Modules\.Forecasting\.Application\.WriteBack\.(IWriteBackCollector|IWriteBackService|IWriteBackTriggerService))(\+.*)?$";

        internal static readonly string StrayLooseServices = $"(?:{StrayLooseImplementation})|(?:{StrayLooseInterfaces})";

        private const string AnyModuleAdapters = @"^Lighthouse\.Backend\.Modules\.[^.]+\.Adapters(\.|$)";

        private const string AccessAndLicensing = @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.(Auth|Authorization|Licensing)(\.|$)" + "|" + ModuleRules.AccessModule + "|" + ModuleRules.LicensingModule;

        private const string OrganisationSetUp = @"^Lighthouse\.Backend\.Modules\.OrganisationSetup(\.|$)|^Lighthouse\.Backend\.Platform\.(DemoData|Encryption|Host|Logging|Persistence|Releases|UpdateQueue)(\.|$)";

        private const string TheDomainEventHandlerPort = "Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler`1";

        private const string FlowMetricsInboundList = "OtherOutsideTypesReachingTheApplication";

        private const string TeamDataService = "Lighthouse.Backend.Modules.WorkItems.Application.TeamDataService";

        private const string MadeUpModule = @"^Lighthouse\.Backend\.Modules\.WorkItemsFixtures\.Boundaries(\.|$)";

        private const string MadeUpDomain = @"^Lighthouse\.Backend\.Modules\.WorkItemsFixtures\.Boundaries\.Domain(\.|$)";

        private const string MadeUpDomainNamespace = "Lighthouse.Backend.Modules.WorkItemsFixtures.Boundaries.Domain";

        private const string MadeUpFormerHome = @"^Lighthouse\.Backend\.Modules\.WorkItemsFixtures\.Boundaries\.Services(\.|$)";

        private const string MadeUpApplication = @"^Lighthouse\.Backend\.Modules\.WorkItemsFixtures\.Boundaries\.Application(\.|$)";

        private const string MadeUpPolicies = @"^Lighthouse\.Backend\.Modules\.WorkItemsFixtures\.Boundaries\.Application\.Policies(\.|$)";

        private const string MadeUpApi = @"^Lighthouse\.Backend\.WorkItemsBoundariesFixtures\.Api(\.|$)";

        private const string MadeUpCompositionRoot = @"^Lighthouse\.Backend\.WorkItemsBoundariesFixtures\.CompositionRoot\.AWorkItemsCompositionRoot(\+|$)";

        private const string MadeUpOutsiders =
            @"^Lighthouse\.Backend\.(Modules\.WorkItemsFixtures\.Boundaries|WorkItemsBoundariesFixtures\.(Api|CompositionRoot|ListedCallers|DomainNewcomer|ApplicationNewcomer))(\.|$)";

        private const string MadeUpConnectors = @"^Lighthouse\.Backend\.WorkItemsBoundariesFixtures\.Connectors(\.|$)";

        private const string MadeUpOutboundCases = "Lighthouse.Backend.Modules.WorkItemsFixtures.BoundariesOutbound";

        private static readonly ModuleWording WorkItemsWording = new("Work Items", "the API, the Flow metrics, Forecasting and Refinement modules and Promises' Application");

        // Generic types carry their arity in the compiled name.
        private static readonly Dictionary<string, string[]> TypesByLayer = new(StringComparer.Ordinal)
        {
            ["Lighthouse.Backend.Modules.WorkItems.Domain"] = ["SyncModeResolver", "FetchFingerprint"],
            ["Lighthouse.Backend.Modules.WorkItems.Domain.Dependencies"] =
            [
                "DependencyHonourPolicy", "IDependencyHonourPolicy", "DependencyHonourInput", "FeatureDependencyFacts",
                "DependencyCycleDetector", "DependencyLoops", "DependencyLoop", "DependencyFacts",
            ],
            ["Lighthouse.Backend.Modules.WorkItems.Application"] =
            [
                "WorkItemService", "IWorkItemService", "BlockedItemService", "TeamDataService", "ITeamDataService",
                "RuleEvaluator`1", "WorkItemFieldProvider", "FeatureFieldProvider",
                "ParentPlacementReport", "RecordPlacement", "RefreshLogService",
            ],
            [ContextContractsMembership.ContractsOf("WorkItems")] =
            [
                "IBlockedItemService", "IRuleEvaluator`1", "IRuleFieldProvider`1", "IRefreshLogService", "IWhatTheForecastWaitsFor",
            ],
            ["Lighthouse.Backend.Modules.WorkItems.Application.Dependencies"] =
            [
                "DependencyDecision", "IDependencyDecision", "DependencyReconciler", "IDependencyReconciler", "DependencyRefreshReporter",
                "IDependencyRefreshReporter", "FeatureDependencyRendering", "DependenciesAsRead", "WhatTheForecastWaitsFor",
            ],
            ["Lighthouse.Backend.Modules.WorkItems.Application.Policies"] =
            [
                "WorkItemBlockedTransitionCaptureHandler", "WorkItemBlockedTransitionCloseHandler", "FeatureBlockedTransitionCaptureHandler",
                "FeatureBlockedTransitionCloseHandler", "TeamDeletedRefreshLogCleanupHandler", "ConnectionFetchShapeChangedUpdateTriggerHandler",
                "PortfolioDeletedOrphanedFeatureCleanupHandler",
            ],
        };

        // The outside types that reach Work Items' Domain. The list only shrinks.
        private static readonly string[] OutsideTypesReachingTheDomain =
        [
            "Lighthouse.Backend.API.Helpers.WorkTrackingSystemOptionsOwnerExtensions",
            "Lighthouse.Backend.Modules.OrganisationSetup.Application.Connections.ConnectionSaveService",
        ];

        // The outside types, besides the API, the composition root, Flow metrics, Forecasting, Refinement and Promises'
        // Application, that reach Work Items' Application. The list only shrinks.
        private static readonly string[] OtherOutsideTypesReachingTheApplication =
        [
            "Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.PortfolioUpdater",
            "Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.TeamUpdater",
            "Lighthouse.Backend.Platform.UpdateQueue.UpdateNotificationHub",
            "Lighthouse.Backend.Services.Implementation.FeatureListService",
            "Lighthouse.Backend.Services.Implementation.Seeding.RefreshLogSeeder",
            "Lighthouse.Backend.Services.Interfaces.FeatureListRead",
        ];

        // Work Items' two queries of Flow metrics stay until Flow metrics answers them itself. The list only shrinks.
        private static readonly string[] EdgesOfTheApplicationIntoFlowMetrics =
        [
        ];

        // Work Items builds on Organisation set-up through its Contracts, so its edges into them are legal and not listed;
        // the services still in the loose Services namespaces and the platform's clock and update queue are listed until they move.
        private static readonly string[] EdgesOfTheApplicationIntoAccessLicensingOrganisationSetUpAndTheUpdateQueue =
        [
            "Lighthouse.Backend.Modules.WorkItems.Application.Policies.PortfolioDeletedOrphanedFeatureCleanupHandler -> Lighthouse.Backend.Services.Interfaces.IOrphanedFeatureCleanupService",
            "Lighthouse.Backend.Modules.WorkItems.Application.WorkItemService -> Lighthouse.Backend.Platform.UpdateQueue.UpdateCancellationContext",
            "Lighthouse.Backend.Modules.WorkItems.Application.WorkItemService -> Lighthouse.Backend.Platform.Host.ILighthouseClock",
        ];

        private static string TheLayersBelowTheApplication =>
            $"(?:{EntityFrameworkCore})|(?:{ApplicationCoreRules.TheData})|(?:{ApplicationCoreRules.TheApi})|(?:{AnyModuleAdapters})|(?:{ModuleRules.SetUpBehindItsContracts})";

        private static string AccessLicensingOrganisationSetUpAndTheUpdateQueue =>
            $"^(?!{ModuleRules.SetUpContracts})(?:(?:{AccessAndLicensing})|(?:{OrganisationSetUp})|(?:{KernelRules.TheUpdateQueue}))";

        private static string AccessAndLicensingBehindTheirContracts => ModuleRules.WithoutTheContractsOf(AccessLicensingOrganisationSetUpAndTheUpdateQueue, "Access", "Licensing");

        private static readonly string WorkItemsLegalCallers =
            $"(?:{ApplicationCoreRules.TheApi})|(?:{ModuleRules.FlowMetricsModule})|(?:{ModuleRules.ForecastingModule})|(?:{ModuleBoundariesArchUnitTest.RefinementPattern})|(?:{PromisesApplicationOutsideItsPolicies})";

        private static readonly string FlowMetricsLegalCallers =
            $"(?:{ApplicationCoreRules.TheApi})|(?:{ModuleBoundariesArchUnitTest.ReportsPattern})|(?:{ModuleBoundariesArchUnitTest.RefinementPattern})|(?:{ModuleRules.ForecastingModule})";

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(WorkItemsModuleBoundariesArchUnitTest).Assembly));

        public enum ListedRule
        {
            OutsideTypesReachingTheDomain,
            OutsideTypesReachingTheApplication,
            ApplicationEdgesIntoFlowMetrics,
            ApplicationEdgesIntoAccessLicensingOrganisationSetUpAndTheUpdateQueue,
            FlowMetricsInboundList,
        }

        public enum OutboundTarget
        {
            LayersBelowTheApplication,
            FlowMetrics,
            AccessLicensingOrganisationSetUpAndTheUpdateQueue,
        }

        private static IEnumerable<string> Layers => TypesByLayer.Keys;

        private static string OrphanedFeatureCleanup => typeof(global::Lighthouse.Backend.Services.Interfaces.IOrphanedFeatureCleanupService).FullName!;

        private static string SetUpRecurrence => typeof(global::Lighthouse.Backend.Modules.OrganisationSetup.Domain.WeeklyRecurrence).FullName!;

        private static string SetUpSettings => typeof(global::Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IAppSettingService).FullName!;

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [TestCaseSource(nameof(Layers))]
        public void EveryWorkItemsType_LivesInItsLayer_AndNoneIsLeftUnderServices(string layer)
        {
            var verdict = ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, layer, TypesByLayer[layer]);

            ThenTheRuleHolds(verdict.Check, $"Work Items' layer {layer}");
        }

        [Test]
        public void TheWorkItemsDomain_ReachesOnlyTheSharedKernelAndTheBaseLibrary()
        {
            var verdict = ModuleRules.DomainReachesOnlyTheKernelAndTheBaseLibrary(ProductionModule.Value, WorkItemsDomain);

            ThenTheRuleHolds(verdict.Check, "Work Items' Domain");
        }

        [TestCase(ListedRule.OutsideTypesReachingTheDomain)]
        [TestCase(ListedRule.OutsideTypesReachingTheApplication)]
        [TestCase(ListedRule.ApplicationEdgesIntoFlowMetrics)]
        [TestCase(ListedRule.ApplicationEdgesIntoAccessLicensingOrganisationSetUpAndTheUpdateQueue)]
        [TestCase(ListedRule.FlowMetricsInboundList)]
        public void AListedRule_MatchesWhatTheCompiledCodeReaches_InBothDirections(ListedRule rule)
        {
            var (check, _) = TheProductRule(rule);

            ThenTheRuleHolds(check, rule.ToString());
        }

        [TestCase(ListedRule.OutsideTypesReachingTheDomain)]
        [TestCase(ListedRule.OutsideTypesReachingTheApplication)]
        [TestCase(ListedRule.ApplicationEdgesIntoAccessLicensingOrganisationSetUpAndTheUpdateQueue)]
        [TestCase(ListedRule.FlowMetricsInboundList)]
        public void EveryEntryDeletedFromAListedRule_FailsTheRuleAndIsNamed_AndAnEntryWithNoEdgeIsNamedForDeletion(ListedRule rule)
        {
            var (_, withList) = TheProductRule(rule);

            ThenEachDeletedEntryIsNamedAndAMadeUpEntryIsNamedForDeletion(rule, withList);
        }

        [Test]
        public void TheWorkItemsApplication_ReachesNoEntityFrameworkCoreDataApiOrModuleAdapterType()
        {
            var verdict = ApplicationBelowRule(ProductionModule.Value, WorkItemsApplication, []);

            ThenTheRuleHolds(verdict.Check, "Work Items' Application towards the layers below it");
        }

        [Test]
        public void TheWorkItemsPolicies_HoldNothingButDomainEventHandlers()
        {
            ThenTheRuleHolds(PoliciesHoldOnlyHandlers(ProductionModule.Value, WorkItemsPolicies).Check, "Work Items' policies");
        }

        [Test]
        public void NoConnector_ReachesAWorkItemsType()
        {
            var verdict = ConnectorsReachNoWorkItemsType(ProductionModule.Value, ModuleBoundariesArchUnitTest.TrackerIntegrationPattern);

            ThenTheRuleHolds(verdict.Check, "the connectors");
        }

        [Test]
        public void FlowMetricsInboundList_NoLongerNamesTeamDataService_AndFailsWhenItIsPutBack()
        {
            var list = TheFlowMetricsInboundList();

            var withTeamDataServiceBack = FlowMetricsInboundRule([.. list, TeamDataService]);

            ThenTeamDataServiceIsOffTheListAndPuttingItBackIsNamedForDeletion(list, withTeamDataServiceBack);
        }

        [Test]
        public void AWorkItemsTypeLeftUnderServices_IsNamed_AndAWellPlacedTwinIsNot()
        {
            var twin = ModuleRules.TypesLiveInTheirLayer(TestModule.Value, MadeUpDomainNamespace, [nameof(AWorkItemsCalculation)], MadeUpFormerHome);
            var leftBehind = ModuleRules.TypesLiveInTheirLayer(TestModule.Value, MadeUpDomainNamespace, [nameof(ALeftBehindCalculation)], MadeUpFormerHome);

            ThenTheRuleNamesTheOffenderAndPassesItsTwin(leftBehind.Check, typeof(ALeftBehindCalculation).FullName!, twin.Check, "the layer rule");
        }

        [Test]
        public void AWorkItemsDomainTypeReachingServices_IsNamed_AndItsCompliantTwinIsNot()
        {
            var verdict = ModuleRules.DomainReachesOnlyTheKernelAndTheBaseLibrary(TestModule.Value, MadeUpDomain);

            ThenTheFailureNamesOnly(verdict.Check, typeof(ADomainCalculationAskingTheLicence).FullName!, [typeof(AWorkItemsCalculation).FullName!], "the Domain rule");
        }

        [Test]
        public void AnOutsideTypeOffTheList_ReachingTheWorkItemsDomain_IsNamed_AndTheListedCallerIsNot()
        {
            var verdict = DomainInboundRule(TestModule.Value, [typeof(AListedDomainCaller).FullName!], MadeUpModule, MadeUpDomain, MadeUpOutsiders);

            ThenTheFailureNamesOnly(verdict.Check, typeof(ANewcomerReachingTheDomain).FullName!, [typeof(AListedDomainCaller).FullName!], "the Domain inbound rule");
        }

        [Test]
        public void AnOutsideTypeOffTheList_ReachingTheWorkItemsApplication_IsNamed_AndTheApiTheCompositionRootAndTheListedCallerAreNot()
        {
            var verdict = ApplicationInboundRule(TestModule.Value, [typeof(AListedApplicationCaller).FullName!], MadeUpModule, MadeUpApplication, MadeUpApi, MadeUpCompositionRoot, MadeUpOutsiders);

            ThenTheFailureNamesOnly(
                verdict.Check,
                typeof(ANewcomerReachingTheApplication).FullName!,
                [typeof(AListedApplicationCaller).FullName!, "Lighthouse.Backend.WorkItemsBoundariesFixtures.Api.AWorkItemsController", "Lighthouse.Backend.WorkItemsBoundariesFixtures.CompositionRoot.AWorkItemsCompositionRoot"],
                "the Application inbound rule");
        }

        [TestCaseSource(nameof(ApplicationTypesReachingAForbiddenOrUnlistedType))]
        public void AWorkItemsApplicationTypeReachingAForbiddenOrUnlistedType_IsNamed(Type offender, string[] listed, OutboundTarget target, string newEdge)
        {
            var verdict = OutboundRule(target)(TestModule.Value, OutboundCase(offender), listed);

            ThenTheFailureNamesOnly(verdict.Check, $"Not on the list, and offending: {newEdge}. ", [], "the Application outbound rule");
        }

        [Test]
        public void AWorkItemsApplicationTypeReachingSetUpsContractsSetUpsDomainAndALooseService_IsNamedForTheDomainAndTheLooseServiceOnly()
        {
            var offender = typeof(AWorkItemServiceAskingSetUpAndTheOrphanCleanup);
            var verdict = AccessLicensingOrganisationSetUpAndUpdateQueueOutboundRule(TestModule.Value, OutboundCase(offender), []);

            ThenTheFailureNamesOnly(
                verdict.Check,
                $"Not on the list, and offending: {Edge(offender, SetUpRecurrence)}, {Edge(offender, OrphanedFeatureCleanup)}. ",
                [Edge(offender, SetUpSettings)],
                "the Application outbound rule into Access, Licensing, Organisation set-up and the update queue");
        }

        [Test]
        public void AWorkItemsApplicationTypeReachingFlowMetricsThroughItsContracts_PassesTheRuleWithNothingListed()
        {
            var offender = typeof(AWorkItemQueryService);
            var verdict = FlowMetricsOutboundRule(TestModule.Value, OutboundCase(offender), []);

            ThenTheRuleHolds(verdict.Check, "a Flow metrics query through its Contracts");
        }

        [Test]
        public void ANonHandlerInTheWorkItemsPolicies_IsNamed_AndAHandlerTwinIsNot()
        {
            var verdict = PoliciesHoldOnlyHandlers(TestModule.Value, MadeUpPolicies);

            ThenTheFailureNamesOnly(
                verdict.Check,
                "Lighthouse.Backend.Modules.WorkItemsFixtures.Boundaries.Application.Policies.ANonHandlerInThePolicies",
                ["Lighthouse.Backend.Modules.WorkItemsFixtures.Boundaries.Application.Policies.AHandlerInThePolicies"],
                "the policies rule");
        }

        [Test]
        public void AConnectorReachingAWorkItemsType_IsNamed_AndOneReachingANamesakeOutsideTheModuleIsNot()
        {
            var verdict = ConnectorsReachNoWorkItemsType(TestModule.Value, MadeUpConnectors);

            ThenTheFailureNamesOnly(verdict.Check, typeof(AConnectorReachingTheWorkItemsModule).FullName!, [typeof(AConnectorReachingANamesake).FullName!], "the connector leaf rule");
        }

        private static IEnumerable<TestCaseData> ApplicationTypesReachingAForbiddenOrUnlistedType()
        {
            yield return new TestCaseData(typeof(AWorkItemServiceHoldingADbSet), Array.Empty<string>(), OutboundTarget.LayersBelowTheApplication, Edge(typeof(AWorkItemServiceHoldingADbSet), "Microsoft.EntityFrameworkCore.DbSet`1"))
                .SetName("AWorkItemsApplicationTypeHoldingAnEntityFrameworkCoreDbSet_IsNamed");
            yield return new TestCaseData(typeof(AWorkItemServiceReadingTheDataLayer), Array.Empty<string>(), OutboundTarget.LayersBelowTheApplication, Edge(typeof(AWorkItemServiceReadingTheDataLayer), "Lighthouse.Backend.Data.LighthouseAppContext"))
                .SetName("AWorkItemsApplicationTypeReadingTheDatabaseContext_IsNamed");
            yield return new TestCaseData(typeof(AWorkItemServiceShapingAnApiType), Array.Empty<string>(), OutboundTarget.LayersBelowTheApplication, Edge(typeof(AWorkItemServiceShapingAnApiType), "Lighthouse.Backend.API.DTO.FeatureDto"))
                .SetName("AWorkItemsApplicationTypeShapingAnApiType_IsNamed");
            yield return new TestCaseData(
                    typeof(AWorkItemServiceReadingTheLicenceTwice),
                    Array.Empty<string>(),
                    OutboundTarget.AccessLicensingOrganisationSetUpAndTheUpdateQueue, Edge(typeof(AWorkItemServiceReadingTheLicenceTwice), "Lighthouse.Backend.Modules.Licensing.Application.ILicenseVerifier"))
                .SetName("AWorkItemsApplicationTypeReachingASecondLicenceType_IsNamed");
        }

        private static Func<ModuleDefinition, string, IReadOnlyCollection<string>, OutboundVerdict> OutboundRule(OutboundTarget target) => target switch
        {
            OutboundTarget.LayersBelowTheApplication => ApplicationBelowRule,
            OutboundTarget.FlowMetrics => FlowMetricsOutboundRule,
            OutboundTarget.AccessLicensingOrganisationSetUpAndTheUpdateQueue => AccessLicensingOrganisationSetUpAndUpdateQueueOutboundRule,
            _ => throw new ArgumentOutOfRangeException(nameof(target)),
        };

        private static string Edge(Type dependent, string target) => $"{dependent.FullName} -> {target}";

        private static (Action Check, Func<IReadOnlyCollection<string>, Action> WithList) TheProductRule(ListedRule rule)
        {
            var module = ProductionModule.Value;

            return rule switch
            {
                ListedRule.OutsideTypesReachingTheDomain => Inbound(listed => DomainInboundRule(module, listed, WorkItemsModule, WorkItemsDomain, ModuleRules.AnyNamespace), OutsideTypesReachingTheDomain),
                ListedRule.OutsideTypesReachingTheApplication => Inbound(
                    listed => ApplicationInboundRule(module, listed, WorkItemsModule, WorkItemsApplication, WorkItemsLegalCallers, TheCompositionRootAndItsNestedTypes, ModuleRules.AnyNamespace),
                    OtherOutsideTypesReachingTheApplication),
                ListedRule.ApplicationEdgesIntoFlowMetrics => Outbound(FlowMetricsOutboundRule(module, WorkItemsApplication, EdgesOfTheApplicationIntoFlowMetrics)),
                ListedRule.ApplicationEdgesIntoAccessLicensingOrganisationSetUpAndTheUpdateQueue => Outbound(
                    AccessLicensingOrganisationSetUpAndUpdateQueueOutboundRule(module, WorkItemsApplication, EdgesOfTheApplicationIntoAccessLicensingOrganisationSetUpAndTheUpdateQueue)),
                ListedRule.FlowMetricsInboundList => Inbound(listed => FlowMetricsInboundRule(listed), TheFlowMetricsInboundList()),
                _ => throw new ArgumentOutOfRangeException(nameof(rule)),
            };
        }

        private static (Action, Func<IReadOnlyCollection<string>, Action>) Inbound(Func<IReadOnlyCollection<string>, InboundVerdict> rule, IReadOnlyCollection<string> listed)
        {
            var verdict = rule(listed);

            return (verdict.Check, other => (verdict with { Listed = other }).Check);
        }

        private static (Action, Func<IReadOnlyCollection<string>, Action>) Outbound(OutboundVerdict verdict)
            => (verdict.Check, other => (verdict with { Listed = other }).Check);

        private static InboundVerdict DomainInboundRule(ModuleDefinition module, IReadOnlyCollection<string> listed, string moduleNamespaces, string domain, string outside)
            => ModuleRules.DomainIsReachedFromOutsideOnlyBy(module, listed, moduleNamespaces, domain, outside, WorkItemsWording);

        private static InboundVerdict ApplicationInboundRule(ModuleDefinition module, IReadOnlyCollection<string> listed, string moduleNamespaces, string application, string legalCallers, string compositionRoot, string outside)
            => ModuleRules.ApplicationIsReachedFromOutsideOnlyBy(module, listed, moduleNamespaces, application, legalCallers, compositionRoot, outside, WorkItemsWording);

        private static InboundVerdict FlowMetricsInboundRule(IReadOnlyCollection<string> listed)
            => ModuleRules.ApplicationIsReachedFromOutsideOnlyBy(
                ProductionModule.Value,
                listed,
                ModuleRules.FlowMetricsModule,
                FlowMetricsApplication,
                FlowMetricsLegalCallers,
                TheCompositionRootAndItsNestedTypes,
                ModuleRules.AnyNamespace);

        private static OutboundVerdict ApplicationBelowRule(ModuleDefinition module, string application, IReadOnlyCollection<string> listed)
            => ModuleRules.OutboundEdges(
                module,
                application,
                TheLayersBelowTheApplication,
                listed,
                $"Types in Work Items' Application, namespaces matching \"{application}\", should not depend on any type in namespaces matching " +
                $"\"{TheLayersBelowTheApplication}\", because Work Items' Application decides how tracker data becomes stored Work Items and Features " +
                "and reaches storage only through repository ports. An Application type that holds a DbSet or the database context, shapes an API " +
                "type or runs a module's adapter ties that decision to how records are stored and shown. Read and write through a repository port " +
                "and return a Models type.");

        private static OutboundVerdict FlowMetricsOutboundRule(ModuleDefinition module, string application, IReadOnlyCollection<string> listed)
            => ModuleRules.OutboundEdges(
                module,
                application,
                ModuleRules.WithoutTheContextsOf(ModuleRules.FlowMetricsModule, "FlowMetrics"),
                listed,
                $"Types in Work Items' Application, namespaces matching \"{application}\", should reach Flow metrics, namespaces matching " +
                $"\"{ModuleRules.FlowMetricsModule}\", only through the listed edges, because Flow metrics builds on Work Items, not the other way " +
                "round: Work Items says what changed by publishing a domain event, and Flow metrics reacts. Every new edge makes Work Items wait on " +
                "or command Flow metrics again. Publish a domain event, or let Flow metrics ask for what it needs.");

        private static OutboundVerdict AccessLicensingOrganisationSetUpAndUpdateQueueOutboundRule(ModuleDefinition module, string application, IReadOnlyCollection<string> listed)
            => ModuleRules.OutboundEdges(
                module,
                application,
                AccessAndLicensingBehindTheirContracts,
                listed,
                $"Types in Work Items' Application, namespaces matching \"{application}\", should reach Access, Licensing, the services still in " +
                $"the loose services named by full name (matching \"{StrayLooseServices}\"), set-up's adapters and the update queue, namespaces matching \"{AccessLicensingOrganisationSetUpAndTheUpdateQueue}\", " +
                "only through the listed edges, because each of them is another context's decision: who may see what, what the licence allows, " +
                "which clock applies, and when an update stops. Every new edge spreads one of those decisions into Work Items. Set-up's Application " +
                "and Domain are open to Work Items, which builds on them: ask set-up there, or where the edge already asks.",
                StrayLooseServices);

        private static KernelRuleVerdict PoliciesHoldOnlyHandlers(ModuleDefinition module, string policies)
        {
            var examined = IlDependencies.TypesIn(module, policies);

            var notHandlers = examined
                .Where(fullName => module.GetType(fullName) is { } type
                    && !type.Interfaces.Any(port => port.InterfaceType.FullName.StartsWith(TheDomainEventHandlerPort, StringComparison.Ordinal)))
                .Select(fullName => new IlDependent(fullName, [TheDomainEventHandlerPort]))
                .ToList();

            return new KernelRuleVerdict(
                examined,
                notHandlers,
                $"Types in namespaces matching \"{policies}\" should each implement {TheDomainEventHandlerPort}, because Work Items' policies are " +
                "where the module reacts to domain events, and a type there that reacts to nothing hides what reacts to what. Move the type to " +
                "the layer it belongs to, or make it handle a domain event.",
                "implements no")
            {
                MissingSubjects = examined.Count == 0 ? [$"Work Items' policies, any type in a namespace matching \"{policies}\","] : [],
            };
        }

        private static KernelRuleVerdict ConnectorsReachNoWorkItemsType(ModuleDefinition module, string connectors)
        {
            var examined = IlDependencies.TypesIn(module, connectors);

            return new KernelRuleVerdict(
                examined,
                IlDependencies.DependentsIn(module, connectors, type => type is not GenericParameter && IlDependencies.ResidesInNamespaceMatching(type, WorkItemsModule)),
                $"Types in namespaces matching \"{connectors}\" should not depend on any type in namespaces matching \"{WorkItemsModule}\", because a " +
                "connector is a leaf input adapter: it translates what the tracker sent, and Work Items calls it, never the other way round. A " +
                "connector that reaches a Work Items type ties how a tracker is read to how Work Items stores what it read. Hand the connector the " +
                "values it needs from the caller.")
            {
                MissingSubjects = examined.Count == 0 ? [$"The connectors, any type in a namespace matching \"{connectors}\","] : [],
            };
        }

        // The list as the Flow metrics rule test holds it, so the two cannot drift apart.
        private static string[] TheFlowMetricsInboundList()
        {
            var field = typeof(FlowMetricsInboundApplicationArchUnitTest).GetField(FlowMetricsInboundList, BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(
                field,
                Is.Not.Null,
                $"{nameof(FlowMetricsInboundApplicationArchUnitTest)} has no list called {FlowMetricsInboundList}. Why it matters: that list is the " +
                "baseline of what may reach Flow metrics' Application, and this check reads it. How to fix: keep the list under that name.");

            return (string[])field!.GetValue(null)!;
        }

        // Each made-up outbound case lives in a namespace of its own, so the rule handed one sees only that case.
        private static string OutboundCase(Type madeUpType)
        {
            var caseName = madeUpType.Namespace![(MadeUpOutboundCases.Length + 1)..].Split('.')[0];

            return $@"^{System.Text.RegularExpressions.Regex.Escape($"{MadeUpOutboundCases}.{caseName}.Application")}(\.|$)";
        }

        private static void ThenTheRuleHolds(Action check, string subject)
            => Assert.DoesNotThrow(
                () => check(),
                $"The rule for {subject} failed on the compiled product; the failure above names each offender, and each listed entry that no " +
                "longer offends. Why it matters: Work Items keeps its own boundaries only while every rule holds and its lists match the code. " +
                "How to fix: undo the new dependency the failure names, or delete the entry it names for deletion; never add an entry to pass.");

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

        private static void ThenTheRuleNamesTheOffenderAndPassesItsTwin(Action offenderCheck, string offender, Action twinCheck, string rule)
        {
            Assert.DoesNotThrow(() => twinCheck(), $"{rule} failed on a type that lives in its layer with no copy left behind. Why it matters: a rule that flags a correct move hides the real offenders. How to fix: report only a type missing from its layer or still present in its former home.");

            ThenTheFailureNamesOnly(offenderCheck, offender, [], rule);
        }

        private static void ThenEachDeletedEntryIsNamedAndAMadeUpEntryIsNamedForDeletion(ListedRule rule, Func<IReadOnlyCollection<string>, Action> withList)
        {
            var listed = ListOf(rule);
            const string anEntryWithNoEdge = "Lighthouse.Backend.WorkItemsBoundariesFixtures.NoSuchCaller";

            Assert.That(listed, Is.Not.Empty, $"{rule} has an empty list, so there is no entry to delete. Why it matters: this check proves each listed entry is load-bearing. How to fix: run it only on a rule with a list.");

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
            ListedRule.ApplicationEdgesIntoFlowMetrics => EdgesOfTheApplicationIntoFlowMetrics,
            ListedRule.ApplicationEdgesIntoAccessLicensingOrganisationSetUpAndTheUpdateQueue => EdgesOfTheApplicationIntoAccessLicensingOrganisationSetUpAndTheUpdateQueue,
            ListedRule.FlowMetricsInboundList => TheFlowMetricsInboundList(),
            _ => throw new ArgumentOutOfRangeException(nameof(rule)),
        };

        private static void ThenTeamDataServiceIsOffTheListAndPuttingItBackIsNamedForDeletion(string[] list, InboundVerdict withTeamDataServiceBack)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(list, Does.Not.Contain(TeamDataService), "Flow metrics' inbound list still names TeamDataService. Why it matters: Work Items now only publishes that a Team was refreshed and Flow metrics reacts, so the entry would let the old command come back unseen. How to fix: delete the entry.");
                Assert.That(list, Has.Length.EqualTo(3), $"Flow metrics' inbound list holds [{string.Join(", ", list)}]; expected the three outside callers that still reach Flow metrics' Application. Why it matters: the list is the ratchet on who calls Flow metrics. How to fix: the both-ways rule names each entry to add or delete.");
            }

            var failure = Assert.Throws<AssertionException>(() => withTeamDataServiceBack.Check(), "Putting TeamDataService back on Flow metrics' inbound list passed. Why it matters: the list must match the code both ways, and TeamDataService no longer reaches Flow metrics. How to fix: compare the list with the code both ways.");
            Assert.That(failure!.Message, Does.Contain($"On the list, but no longer offending: {TeamDataService}"), "The failure must name TeamDataService for deletion.");
        }
    }
}

namespace Lighthouse.Backend.Modules.WorkItemsFixtures.Boundaries.Domain
{
    internal static class AWorkItemsCalculation
    {
        public static int Age(global::Lighthouse.Backend.Models.Feature feature) => feature.Name.Length;
    }

    internal static class ADomainCalculationAskingTheLicence
    {
        public static bool Allows(global::Lighthouse.Backend.Modules.Licensing.Contracts.ILicenseService licence) => licence is not null;
    }
}

namespace Lighthouse.Backend.Modules.WorkItemsFixtures.Boundaries.Services
{
    internal static class ALeftBehindCalculation
    {
        public static int Age() => 1;
    }
}

namespace Lighthouse.Backend.Modules.WorkItemsFixtures.Boundaries.Application
{
    internal static class AWorkItemAnswerService
    {
        public static int Answer() => global::Lighthouse.Backend.Modules.WorkItemsFixtures.Boundaries.Domain.AWorkItemsCalculation.Age(new global::Lighthouse.Backend.Models.Feature());
    }

    internal sealed class ANamesakeRecord
    {
        public int Id { get; init; }
    }
}

namespace Lighthouse.Backend.Modules.WorkItemsFixtures.Boundaries.Application.Policies
{
    internal sealed class AHandlerInThePolicies : global::Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler<global::Lighthouse.Backend.Modules.WorkItems.Contracts.TeamDataRefreshed>
    {
        public Task HandleAsync(global::Lighthouse.Backend.Modules.WorkItems.Contracts.TeamDataRefreshed domainEvent, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    internal static class ANonHandlerInThePolicies
    {
        public static int Answer() => 1;
    }
}

namespace Lighthouse.Backend.WorkItemsBoundariesFixtures.Api
{
    internal static class AWorkItemsController
    {
        public static int Get() => global::Lighthouse.Backend.Modules.WorkItemsFixtures.Boundaries.Application.AWorkItemAnswerService.Answer();
    }
}

namespace Lighthouse.Backend.WorkItemsBoundariesFixtures.CompositionRoot
{
    internal static class AWorkItemsCompositionRoot
    {
        public static int Register() => Registrations.Register();

        private static class Registrations
        {
            public static int Register() => global::Lighthouse.Backend.Modules.WorkItemsFixtures.Boundaries.Application.AWorkItemAnswerService.Answer();
        }
    }
}

namespace Lighthouse.Backend.WorkItemsBoundariesFixtures.ListedCallers
{
    internal static class AListedDomainCaller
    {
        public static int Answer() => global::Lighthouse.Backend.Modules.WorkItemsFixtures.Boundaries.Domain.AWorkItemsCalculation.Age(new global::Lighthouse.Backend.Models.Feature());
    }

    internal static class AListedApplicationCaller
    {
        public static int Answer() => global::Lighthouse.Backend.Modules.WorkItemsFixtures.Boundaries.Application.AWorkItemAnswerService.Answer();
    }
}

namespace Lighthouse.Backend.WorkItemsBoundariesFixtures.DomainNewcomer
{
    internal static class ANewcomerReachingTheDomain
    {
        public static int Answer() => global::Lighthouse.Backend.Modules.WorkItemsFixtures.Boundaries.Domain.AWorkItemsCalculation.Age(new global::Lighthouse.Backend.Models.Feature());
    }
}

namespace Lighthouse.Backend.WorkItemsBoundariesFixtures.ApplicationNewcomer
{
    // Reaches the Application only inside an async method, which a dependency graph of a Release build would not see.
    internal static class ANewcomerReachingTheApplication
    {
        public static async Task<int> AnswerAsync()
        {
            await Task.Yield();

            return global::Lighthouse.Backend.Modules.WorkItemsFixtures.Boundaries.Application.AWorkItemAnswerService.Answer();
        }
    }
}

namespace Lighthouse.Backend.WorkItemsBoundariesFixtures.Connectors
{
    internal static class AConnectorReachingTheWorkItemsModule
    {
        public static bool Hands(global::Lighthouse.Backend.Modules.WorkItems.Application.IWorkItemService service) => service is not null;
    }

    internal static class AConnectorReachingANamesake
    {
        public static int Reads(global::Lighthouse.Backend.Modules.WorkItemsFixtures.Boundaries.Application.ANamesakeRecord record) => record.Id;
    }
}

namespace Lighthouse.Backend.Modules.WorkItemsFixtures.BoundariesOutbound.ListedFlowMetricsQuery.Application
{
    internal static class AWorkItemQueryService
    {
        public static bool Asks(global::Lighthouse.Backend.Modules.FlowMetrics.Contracts.IPortfolioMetricsService metrics) => metrics is not null;
    }
}

namespace Lighthouse.Backend.Modules.WorkItemsFixtures.BoundariesOutbound.HoldingADbSet.Application
{
    internal sealed class AWorkItemServiceHoldingADbSet
    {
        public global::Microsoft.EntityFrameworkCore.DbSet<global::Lighthouse.Backend.Models.Feature>? Features { get; init; }
    }
}

namespace Lighthouse.Backend.Modules.WorkItemsFixtures.BoundariesOutbound.ReadingTheDataLayer.Application
{
    internal static class AWorkItemServiceReadingTheDataLayer
    {
        public static bool Reads(global::Lighthouse.Backend.Data.LighthouseAppContext context) => context is not null;
    }
}

namespace Lighthouse.Backend.Modules.WorkItemsFixtures.BoundariesOutbound.ShapingAnApiType.Application
{
    internal static class AWorkItemServiceShapingAnApiType
    {
        public static bool Shapes(global::Lighthouse.Backend.API.DTO.FeatureDto dto) => dto is not null;
    }
}

namespace Lighthouse.Backend.Modules.WorkItemsFixtures.BoundariesOutbound.ReachingAnUnlistedFlowMetricsType.Application
{
    internal static class AWorkItemServiceUpdatingTeamMetrics
    {
        public static bool Asks(global::Lighthouse.Backend.Modules.FlowMetrics.Contracts.IPortfolioMetricsService metrics) => metrics is not null;

        public static bool Commands(global::Lighthouse.Backend.Modules.FlowMetrics.Contracts.ITeamMetricsService metrics) => metrics is not null;
    }
}

namespace Lighthouse.Backend.Modules.WorkItemsFixtures.BoundariesOutbound.ReadingASecondLicenceType.Application
{
    internal static class AWorkItemServiceReadingTheLicenceTwice
    {
        public static bool Asks(global::Lighthouse.Backend.Modules.Licensing.Contracts.ILicenseService licence) => licence is not null;

        public static bool Verifies(global::Lighthouse.Backend.Modules.Licensing.Application.ILicenseVerifier verifier) => verifier is not null;
    }
}

namespace Lighthouse.Backend.Modules.WorkItemsFixtures.BoundariesOutbound.ReachingSetUpAndALooseService.Application
{
    internal static class AWorkItemServiceAskingSetUpAndTheOrphanCleanup
    {
        public static bool Asks(global::Lighthouse.Backend.Modules.OrganisationSetup.Contracts.IAppSettingService settings) => settings is not null;

        public static DateOnly Recurs(DateOnly day) => global::Lighthouse.Backend.Modules.OrganisationSetup.Domain.WeeklyRecurrence.MondayOfWeek(day);

        public static bool Cleans(global::Lighthouse.Backend.Services.Interfaces.IOrphanedFeatureCleanupService cleanup) => cleanup is not null;
    }
}
