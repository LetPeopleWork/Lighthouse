using Lighthouse.Backend.Models;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Organisation set-up is a module with rules of its own, checked by the shared module rules and read from the compiled
    /// production assembly: every set-up type lives in its layer and none is left under Services; its Domain reaches only
    /// the shared kernel and the base library; its Application reaches no API, module adapter, data layer, EF Core or
    /// downstream context (Work Items, Flow metrics, Forecasting, Promises, Refinement, Reports) except along the listed
    /// edges; and Tracker integration reaches set-up only along its one listed edge. Every list is compared both ways and
    /// only shrinks: deleting an entry names it, and an entry with no edge is named for deletion. Where set-up's events may
    /// be constructed is checked with the open-host rules.
    /// </summary>
    [TestFixture]
    public class OrganisationSetupModuleBoundariesArchUnitTest
    {
        private static string SetUpModule => @"^Lighthouse\.Backend\.Modules\.OrganisationSetup(\.|$)";

        private static string SetUpDomain => @"^Lighthouse\.Backend\.Modules\.OrganisationSetup\.Domain(\.|$)";

        private static string SetUpApplication => @"^Lighthouse\.Backend\.Modules\.OrganisationSetup\.Application(\.|$)";

        private static string TrackerIntegrationModule => @"^Lighthouse\.Backend\.Modules\.TrackerIntegration(\.|$)";

        private static string TheLayersBelowAndTheDownstreamContexts =>
            @"^Lighthouse\.Backend\.API(\.|$)|^Lighthouse\.Backend\.Modules\.[^.]+\.Adapters(\.|$)|^Lighthouse\.Backend\.Data(\.|$)" +
            @"|^Microsoft\.EntityFrameworkCore(\.|$)|^Lighthouse\.Backend\.Modules\.(WorkItems|FlowMetrics|Forecasting|Promises|Refinement|Reports)(\.|$)";

        private static string AMadeUpEntry => "Lighthouse.Backend.Modules.OrganisationSetup.Application.AMadeUpService -> Lighthouse.Backend.API.AMadeUpController";

        private static readonly ModuleWording SetUpWording = new("Organisation set-up", "every context that builds on it, by namespace");

        // Generic and nested types are left out: the rule names top-level types.
        private static readonly Dictionary<string, string[]> TypesByLayer = new(StringComparer.Ordinal)
        {
            ["Lighthouse.Backend.Modules.OrganisationSetup.Domain"] = ["RecurringBlackoutRuleExtensions", "WeeklyRecurrence"],
            ["Lighthouse.Backend.Modules.OrganisationSetup.Application"] =
            [
                "AppSettingService", "IAppSettingService", "BlackoutPeriodService", "IBlackoutPeriodService", "FeatureMovePlacement",
                "FeatureOrdering", "IFeatureOrdering", "FeatureOrderingPolicyProvider", "IFeatureOrderingPolicyProvider",
                "FeatureRankingService", "IFeatureRankingService", "FeatureRankSeeder", "IFeatureRankSeeder",
                "RecurringBlackoutRuleService", "IRecurringBlackoutRuleService", "TerminologyService", "ITerminologyService",
            ],
            ["Lighthouse.Backend.Modules.OrganisationSetup.Application.Connections"] =
            [
                "ChangedConnectionDefault", "ConnectionFollowers", "ConnectionSaveHandle", "ConnectionSaveOutcome", "ConnectionSaveService",
                "IConnectionSaveService", "ConnectionUsage", "FollowerGroup", "IWorkTrackingSystemConnectionUsage", "WorkTrackingSystemConnectionUsage",
                "NamedField", "OwnersMoved", "OwnerWords", "ParentSourcesMoved", "PendingDefault", "UpdatesQueued",
            ],
            ["Lighthouse.Backend.Modules.OrganisationSetup.Application.OptionalFeatures"] =
            [
                "DefaultOptionalFeatureApplier", "FeatureOrderingApplier", "IOptionalFeatureApplier", "OptionalFeatureApplierRegistry",
            ],
        };

        // Feature ranking writes its ranks in one transaction, and a saved connection compares its fetch shape with Work
        // Items' fingerprint. The list only shrinks.
        private static readonly string[] EdgesOfTheApplicationBelowItOrIntoDownstreamContexts =
        [
            "Lighthouse.Backend.Modules.OrganisationSetup.Application.Connections.ConnectionSaveService -> Lighthouse.Backend.Modules.WorkItems.Domain.FetchFingerprint",
            "Lighthouse.Backend.Modules.OrganisationSetup.Application.Connections.ConnectionSaveService -> Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions",
            "Lighthouse.Backend.Modules.OrganisationSetup.Application.Connections.ConnectionSaveService -> Microsoft.EntityFrameworkCore.Query.IIncludableQueryable`2",
            "Lighthouse.Backend.Modules.OrganisationSetup.Application.FeatureRankingService -> Lighthouse.Backend.Data.LighthouseAppContext",
            "Lighthouse.Backend.Modules.OrganisationSetup.Application.FeatureRankingService -> Microsoft.EntityFrameworkCore.DbContext",
            "Lighthouse.Backend.Modules.OrganisationSetup.Application.FeatureRankingService -> Microsoft.EntityFrameworkCore.DbSet`1",
            "Lighthouse.Backend.Modules.OrganisationSetup.Application.FeatureRankingService -> Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions",
            "Lighthouse.Backend.Modules.OrganisationSetup.Application.FeatureRankingService -> Microsoft.EntityFrameworkCore.ExecutionStrategyExtensions",
            "Lighthouse.Backend.Modules.OrganisationSetup.Application.FeatureRankingService -> Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade",
            "Lighthouse.Backend.Modules.OrganisationSetup.Application.FeatureRankingService -> Microsoft.EntityFrameworkCore.Query.UpdateSettersBuilder`1",
            "Lighthouse.Backend.Modules.OrganisationSetup.Application.FeatureRankingService -> Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction",
            "Lighthouse.Backend.Modules.OrganisationSetup.Application.FeatureRankingService -> Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy",
            "Lighthouse.Backend.Modules.OrganisationSetup.Application.FeatureRankSeeder -> Lighthouse.Backend.Data.LighthouseAppContext",
            "Lighthouse.Backend.Modules.OrganisationSetup.Application.FeatureRankSeeder -> Microsoft.EntityFrameworkCore.DbContext",
            "Lighthouse.Backend.Modules.OrganisationSetup.Application.FeatureRankSeeder -> Microsoft.EntityFrameworkCore.DbSet`1",
            "Lighthouse.Backend.Modules.OrganisationSetup.Application.FeatureRankSeeder -> Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions",
        ];

        // The connection-health cadence reads its interval from the settings. The list only shrinks.
        private static readonly string[] EdgesOfTrackerIntegrationIntoSetUp =
        [
            "Lighthouse.Backend.Modules.TrackerIntegration.Application.ConnectionHealth.ConnectionHealthCadence -> Lighthouse.Backend.Modules.OrganisationSetup.Application.IAppSettingService",
        ];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        public enum ListedRule
        {
            ApplicationBelowItOrIntoDownstreamContexts,
            TrackerIntegrationIntoSetUp,
        }

        private static IEnumerable<string> Layers => TypesByLayer.Keys;

        private static IEnumerable<TestCaseData> EveryListedEntry()
            => Enum.GetValues<ListedRule>().SelectMany(rule => ListOf(rule).Select(entry => new TestCaseData(rule, entry).SetArgDisplayNames(rule.ToString(), entry)));

        [OneTimeTearDown]
        public void DisposeModule()
        {
            if (ProductionModule.IsValueCreated)
            {
                ProductionModule.Value.Dispose();
            }
        }

        [TestCaseSource(nameof(Layers))]
        public void EverySetUpType_LivesInItsLayer_AndNoneIsLeftUnderServices(string layer)
        {
            var verdict = ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, layer, TypesByLayer[layer]);

            ThenTheRuleHolds(verdict.Check, SetUpWording.Of($"layer {layer}"));
        }

        [Test]
        public void TheSetUpDomain_ReachesOnlyTheSharedKernelAndTheBaseLibrary()
        {
            var verdict = ModuleRules.DomainReachesOnlyTheKernelAndTheBaseLibrary(ProductionModule.Value, SetUpDomain);

            ThenTheRuleHolds(verdict.Check, SetUpWording.Of("Domain"));
        }

        [TestCase(ListedRule.ApplicationBelowItOrIntoDownstreamContexts)]
        [TestCase(ListedRule.TrackerIntegrationIntoSetUp)]
        public void AListedRule_MatchesWhatTheCompiledCodeReaches_InBothDirections(ListedRule rule)
        {
            var verdict = TheRule(rule, ListOf(rule));

            ThenTheRuleHolds(verdict.Check, rule.ToString());
        }

        [TestCaseSource(nameof(EveryListedEntry))]
        public void AnEntryDeletedFromAListedRule_FailsTheRuleAndIsNamed(ListedRule rule, string entry)
        {
            var verdict = TheRule(rule, [.. ListOf(rule).Where(listed => listed != entry)]);

            ThenTheRuleFailsNaming(verdict.Check, entry, $"the compiled edge {entry} once it is taken off {rule}");
        }

        [TestCase(ListedRule.ApplicationBelowItOrIntoDownstreamContexts)]
        [TestCase(ListedRule.TrackerIntegrationIntoSetUp)]
        public void AnEntryWithNoCompiledEdge_IsNamedForDeletion(ListedRule rule)
        {
            var verdict = TheRule(rule, [.. ListOf(rule), AMadeUpEntry]);

            ThenTheRuleFailsNaming(verdict.Check, AMadeUpEntry, $"the made-up entry on {rule}, which no compiled edge carries");
        }

        private static string[] ListOf(ListedRule rule) => rule switch
        {
            ListedRule.ApplicationBelowItOrIntoDownstreamContexts => EdgesOfTheApplicationBelowItOrIntoDownstreamContexts,
            ListedRule.TrackerIntegrationIntoSetUp => EdgesOfTrackerIntegrationIntoSetUp,
            _ => throw new ArgumentOutOfRangeException(nameof(rule), rule, "Every listed rule has a list."),
        };

        private static OutboundVerdict TheRule(ListedRule rule, IReadOnlyCollection<string> listed) => rule switch
        {
            ListedRule.ApplicationBelowItOrIntoDownstreamContexts => ModuleRules.OutboundEdges(
                ProductionModule.Value,
                SetUpApplication,
                TheLayersBelowAndTheDownstreamContexts,
                listed,
                $"Types in {SetUpWording.Of("Application")}, namespaces matching \"{SetUpApplication}\", should reach the API, module " +
                $"adapters, the data layer, EF Core and the contexts that build on set-up, namespaces matching \"{TheLayersBelowAndTheDownstreamContexts}\", " +
                "only through the listed edges, because set-up is the host those contexts read from: an edge back into them, or into storage " +
                "beside the repositories, ties set-up to what it serves. Ask through a port of set-up's own or a domain event instead; the list only shrinks."),
            ListedRule.TrackerIntegrationIntoSetUp => ModuleRules.OutboundEdges(
                ProductionModule.Value,
                TrackerIntegrationModule,
                SetUpModule,
                listed,
                $"Types in Tracker integration, namespaces matching \"{TrackerIntegrationModule}\", should reach Organisation set-up, namespaces " +
                $"matching \"{SetUpModule}\", only through the listed edge, because Tracker integration translates what the trackers send and does " +
                "not build on set-up. Hand the value in from the caller instead; the list only shrinks."),
            _ => throw new ArgumentOutOfRangeException(nameof(rule), rule, "Every listed rule has a check."),
        };

        private static void ThenTheRuleHolds(Action check, string subject)
            => Assert.DoesNotThrow(() => check(), $"{subject}: the rule above names what broke it and how to fix it.");

        private static void ThenTheRuleFailsNaming(Action check, string offender, string what)
        {
            var failure = Assert.Throws<AssertionException>(() => check(), $"The rule passed with {what}, so the list could grow or keep stale entries unseen. Compare the list both ways.");

            Assert.That(failure!.Message, Does.Contain(offender),
                $"The rule failed for {what} but did not name it, so a reader could not tell which entry to change. Name every differing entry.");
        }
    }
}
