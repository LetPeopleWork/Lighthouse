using System.Reflection;
using System.Text.RegularExpressions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.PromisesFixtures.Boundaries.Domain;
using Lighthouse.Backend.Modules.PromisesFixtures.Boundaries.Services;
using Lighthouse.Backend.Modules.PromisesFixtures.BoundariesOutbound.AskingTheAppSettings.Application;
using Lighthouse.Backend.Modules.PromisesFixtures.BoundariesOutbound.AskingTheLicence.Application;
using Lighthouse.Backend.Modules.PromisesFixtures.BoundariesOutbound.Compliant.Application;
using Lighthouse.Backend.Modules.PromisesFixtures.BoundariesOutbound.HoldingADbSet.Application;
using Lighthouse.Backend.Modules.PromisesFixtures.BoundariesOutbound.RunningAForecastingAdapter.Application;
using Lighthouse.Backend.Modules.PromisesFixtures.BoundariesOutbound.RunningTheWriteBackRound.Application;
using Lighthouse.Backend.Modules.PromisesFixtures.BoundariesOutbound.TouchingTheUpdateCode.Application;
using Lighthouse.Backend.PromisesBoundariesFixtures.ApplicationNewcomer;
using Lighthouse.Backend.PromisesBoundariesFixtures.CompositionRoot;
using Lighthouse.Backend.PromisesBoundariesFixtures.DomainNewcomer;
using Lighthouse.Backend.PromisesBoundariesFixtures.ListedCallers;
using Lighthouse.Backend.PromisesBoundariesFixtures.PoliciesNewcomer;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Promises is a module with rules of its own, checked by the shared module rules with Promises' namespaces and
    /// Promises' wording, and read from the compiled production assembly: each Promises type lives in its layer and
    /// none is left behind; its Domain reaches only the shared kernel; outside code reaches its Domain, Application and
    /// Policies only as listed; its Application reaches no EF Core, data-layer, API or module adapter type and the
    /// platform only through the listed edges; and the module reaches no other module's adapters, the update code,
    /// the write-back round, Access or Licensing. Every list is compared both ways. The Work Items and Tracker
    /// integration inbound lists name no Promises type, because Promises calls them by right. The same rules run on
    /// made-up types: each rule names its made-up offender and passes its compliant twin.
    /// </summary>
    /// <remarks>
    /// The IL reader does not see types used only in a catch clause, an event or a named attribute argument.
    /// </remarks>
    [TestFixture]
    public class PromisesModuleBoundariesArchUnitTest
    {
        private const string PromisesDomainNamespace = "Lighthouse.Backend.Modules.Promises.Domain";

        private const string PromisesApplicationNamespace = "Lighthouse.Backend.Modules.Promises.Application";

        private const string PromisesPoliciesNamespace = "Lighthouse.Backend.Modules.Promises.Application.Policies";

        private const string PromisesDomain = @"^Lighthouse\.Backend\.Modules\.Promises\.Domain(\.|$)";

        private const string PromisesApplication = @"^Lighthouse\.Backend\.Modules\.Promises\.Application(\.|$)";

        private const string PromisesPolicies = @"^Lighthouse\.Backend\.Modules\.Promises\.Application\.Policies(\.|$)";

        private const string TheCompositionRootAndItsNestedTypes = @"^Lighthouse\.Backend\.Program(\+|$)";

        private const string EntityFrameworkCore = @"^Microsoft\.EntityFrameworkCore(\.|$)";

        private const string AnyModuleAdapters = @"^Lighthouse\.Backend\.Modules\.[^.]+\.Adapters(\.|$)";

        private const string ThePlatform = @"^Lighthouse\.Backend\.Services(\.|$)";

        private const string TheUpdateCode = @"^Lighthouse\.Backend\.Services\.(Implementation\.BackgroundServices\.Update|Interfaces\.Update)(\.|$)";

        private const string AccessAndLicensing = @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.(Auth|Authorization|Licensing)(\.|$)" + "|" + ModuleRules.AccessModule + "|" + ModuleRules.LicensingModule;

        // Tracker integration's edges into Promises are held by Tracker integration's own rule, which lists each one
        // exactly and goes to none, so this rule looks at every other outside namespace.
        private const string OutsideTrackerIntegration = @"^(?!Lighthouse\.Backend\.Modules\.TrackerIntegration(\.|$))";

        private const string NeighbourInboundList = "OtherOutsideTypesReachingTheApplication";

        private const string MadeUpModule = @"^Lighthouse\.Backend\.Modules\.PromisesFixtures\.Boundaries(\.|$)";

        private const string MadeUpDomainNamespace = "Lighthouse.Backend.Modules.PromisesFixtures.Boundaries.Domain";

        private const string MadeUpDomain = @"^Lighthouse\.Backend\.Modules\.PromisesFixtures\.Boundaries\.Domain(\.|$)";

        private const string MadeUpFormerHome = @"^Lighthouse\.Backend\.Modules\.PromisesFixtures\.Boundaries\.Services(\.|$)";

        private const string MadeUpApplication = @"^Lighthouse\.Backend\.Modules\.PromisesFixtures\.Boundaries\.Application(\.|$)";

        private const string MadeUpPolicies = @"^Lighthouse\.Backend\.Modules\.PromisesFixtures\.Boundaries\.Application\.Policies(\.|$)";

        private const string MadeUpOutside = "Lighthouse.Backend.PromisesBoundariesFixtures";

        private const string MadeUpApi = @"^Lighthouse\.Backend\.PromisesBoundariesFixtures\.Api(\.|$)";

        private const string MadeUpController = "Lighthouse.Backend.PromisesBoundariesFixtures.Api.ADeliveriesController";

        private const string AGoneEntry = "Lighthouse.Backend.PromisesBoundariesFixtures.Gone.AFormerDeliveryCaller";

        private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

        private static readonly ModuleWording PromisesWording = new("Promises", "the API");

        // The types each Promises layer holds, and the namespaces they moved out of. The lists name every type the
        // layer holds, so a type added to the module is placed here on purpose.
        private static readonly Dictionary<string, (string[] Types, string FormerHome)> TypesByLayer = new(StringComparer.Ordinal)
        {
            [PromisesDomainNamespace] = (["DeliveryForecastBlock", "DeliveryForecastBlockPercentile"], KernelRules.TheModels),
            [PromisesApplicationNamespace] =
            (
                [
                    "DeliveryForecastBlockRenderer",
                    "DeliveryForecastPublishingService",
                    "DeliveryMetricValues",
                    "DeliveryMetricValuesProjector",
                    "DeliveryRuleService",
                    "DeliverySourceResolver",
                    "DeliverySourceSyncService",
                    "IDeliveryForecastBlockRenderer",
                    "IDeliveryForecastPublishingService",
                    "IDeliveryRuleService",
                    "IDeliverySourceResolver",
                    "IDeliverySourceSyncService",
                    "PortfolioSourcePreview",
                ],
                KernelRules.TheServices
            ),
            [PromisesPoliciesNamespace] = (["DeliveryForecastPublishingHandler", "DeliveryMetricSnapshotRecordingHandler"], KernelRules.TheServices),
        };

        // The outside types that reach Promises' Domain. The list only shrinks.
        private static readonly string[] OutsideTypesReachingTheDomain =
        [
        ];

        // The outside types, besides the API and the composition root, that reach Promises' Application. The Portfolio
        // refresh calls the rule and sync services until it reacts to an event instead. The list only shrinks.
        private static readonly string[] OtherOutsideTypesReachingTheApplication =
        [
            "Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.PortfolioUpdater",
        ];

        // The outside types, besides the composition root, that reach Promises' Policies. The list only shrinks.
        private static readonly string[] OtherOutsideTypesReachingThePolicies =
        [
        ];

        // The edges from Promises' Application into EF Core, the data layer, the API, any module's adapters or the
        // platform. Promises reads the clock, its Delivery records through the platform repositories (and the generic
        // repository they extend), the blackout periods its metrics leave out, and reacts through the domain event
        // handler contract; the repositories and the blackout periods have no module of their own yet. The list only shrinks.
        private static readonly string[] EdgesOfTheApplicationBelowItOrIntoThePlatform =
        [
            "Lighthouse.Backend.Modules.Promises.Application.DeliveryForecastPublishingService -> Lighthouse.Backend.Services.Interfaces.ILighthouseClock",
            "Lighthouse.Backend.Modules.Promises.Application.DeliveryForecastPublishingService -> Lighthouse.Backend.Services.Interfaces.Repositories.RecordableDeliveries",
            "Lighthouse.Backend.Modules.Promises.Application.DeliveryMetricValuesProjector -> Lighthouse.Backend.Services.Interfaces.IBlackoutPeriodService",
            "Lighthouse.Backend.Modules.Promises.Application.DeliveryRuleService -> Lighthouse.Backend.Services.Interfaces.Repositories.RecordableDeliveries",
            "Lighthouse.Backend.Modules.Promises.Application.DeliverySourceSyncService -> Lighthouse.Backend.Services.Interfaces.ILighthouseClock",
            "Lighthouse.Backend.Modules.Promises.Application.DeliverySourceSyncService -> Lighthouse.Backend.Services.Interfaces.Repositories.RecordableDeliveries",
            "Lighthouse.Backend.Modules.Promises.Application.IDeliveryForecastPublishingService -> Lighthouse.Backend.Services.Interfaces.Repositories.RecordableDeliveries",
            "Lighthouse.Backend.Modules.Promises.Application.IDeliveryRuleService -> Lighthouse.Backend.Services.Interfaces.Repositories.RecordableDeliveries",
            "Lighthouse.Backend.Modules.Promises.Application.IDeliverySourceSyncService -> Lighthouse.Backend.Services.Interfaces.Repositories.RecordableDeliveries",
            "Lighthouse.Backend.Modules.Promises.Application.Policies.DeliveryForecastPublishingHandler -> Lighthouse.Backend.Services.Interfaces.DomainEvents.IDomainEventHandler`1",
            "Lighthouse.Backend.Modules.Promises.Application.Policies.DeliveryForecastPublishingHandler -> Lighthouse.Backend.Services.Interfaces.Repositories.IDeliveryRepository",
            "Lighthouse.Backend.Modules.Promises.Application.Policies.DeliveryForecastPublishingHandler -> Lighthouse.Backend.Services.Interfaces.Repositories.IRepository`1",
            "Lighthouse.Backend.Modules.Promises.Application.Policies.DeliveryForecastPublishingHandler -> Lighthouse.Backend.Services.Interfaces.Repositories.RecordableDeliveries",
            "Lighthouse.Backend.Modules.Promises.Application.Policies.DeliveryMetricSnapshotRecordingHandler -> Lighthouse.Backend.Services.Interfaces.DomainEvents.IDomainEventHandler`1",
            "Lighthouse.Backend.Modules.Promises.Application.Policies.DeliveryMetricSnapshotRecordingHandler -> Lighthouse.Backend.Services.Interfaces.ILighthouseClock",
            "Lighthouse.Backend.Modules.Promises.Application.Policies.DeliveryMetricSnapshotRecordingHandler -> Lighthouse.Backend.Services.Interfaces.Repositories.IDeliveryMetricSnapshotRepository",
            "Lighthouse.Backend.Modules.Promises.Application.Policies.DeliveryMetricSnapshotRecordingHandler -> Lighthouse.Backend.Services.Interfaces.Repositories.IDeliveryRepository",
            "Lighthouse.Backend.Modules.Promises.Application.Policies.DeliveryMetricSnapshotRecordingHandler -> Lighthouse.Backend.Services.Interfaces.Repositories.IRepository`1",
            "Lighthouse.Backend.Modules.Promises.Application.Policies.DeliveryMetricSnapshotRecordingHandler -> Lighthouse.Backend.Services.Interfaces.Repositories.RecordableDeliveries",
        ];

        // The edges from anywhere in Promises into another module's adapters, the update code, the write-back round,
        // Access or Licensing. The list only shrinks.
        private static readonly string[] EdgesOfTheModuleIntoUpstreamCode =
        [
        ];

        // The write-back round has no namespace of its own: it sits in the top-level Services namespaces, so its types
        // are named one by one.
        private static readonly string[] TheWriteBackRound =
        [
            "Lighthouse.Backend.Services.Implementation.WriteBackCollector",
            "Lighthouse.Backend.Services.Implementation.WriteBackRound",
            "Lighthouse.Backend.Services.Implementation.WriteBackRoundContext",
            "Lighthouse.Backend.Services.Implementation.WriteBackService",
            "Lighthouse.Backend.Services.Implementation.WriteBackTriggerService",
            "Lighthouse.Backend.Services.Interfaces.IWriteBackCollector",
            "Lighthouse.Backend.Services.Interfaces.IWriteBackService",
            "Lighthouse.Backend.Services.Interfaces.IWriteBackTriggerService",
        ];

        private static readonly string TheLayersBelowTheApplicationAndThePlatform =
            $"(?:{EntityFrameworkCore})|(?:{ApplicationCoreRules.TheData})|(?:{ApplicationCoreRules.TheApi})|(?:{AnyModuleAdapters})|(?:{ThePlatform})";

        private static readonly string TheNamespacedUpstreamCode =
            $"(?:{AnyModuleAdapters})|(?:{TheUpdateCode})|(?:{AccessAndLicensing})";

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(PromisesModuleBoundariesArchUnitTest).Assembly));

        public enum ListedRule
        {
            OutsideTypesReachingTheDomain,
            OutsideTypesReachingTheApplication,
            OutsideTypesReachingThePolicies,
            ApplicationEdgesBelowItOrIntoThePlatform,
            ModuleEdgesIntoUpstreamCode,
        }

        public enum OutboundTarget
        {
            BelowOrPlatform,
            Upstream,
        }

        public enum Neighbour
        {
            WorkItems,
            TrackerIntegration,
        }

        private static IEnumerable<string> Layers => TypesByLayer.Keys;

        private static string MadeUpCompositionRoot => typeof(APromisesCompositionRoot).FullName!;

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [TestCaseSource(nameof(Layers))]
        public void EveryPromisesType_LivesInItsLayer_AndNoneIsLeftBehind(string layer)
        {
            var (types, formerHome) = TypesByLayer[layer];

            var placement = ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, layer, types, formerHome);

            ThenTheRuleHolds(placement.Check, $"Promises' layer {layer}");
            ThenTheRuleHolds(() => LayerHoldsExactly(ProductionModule.Value, layer, types), $"what Promises' layer {layer} holds");
        }

        [Test]
        public void ThePromisesDomain_ReachesOnlyTheSharedKernelAndTheBaseLibrary()
        {
            var verdict = ModuleRules.DomainReachesOnlyTheKernelAndTheBaseLibrary(ProductionModule.Value, PromisesDomain);

            ThenTheRuleHolds(verdict.Check, "Promises' Domain");
        }

        [TestCase(ListedRule.OutsideTypesReachingTheDomain)]
        [TestCase(ListedRule.OutsideTypesReachingTheApplication)]
        [TestCase(ListedRule.OutsideTypesReachingThePolicies)]
        [TestCase(ListedRule.ApplicationEdgesBelowItOrIntoThePlatform)]
        [TestCase(ListedRule.ModuleEdgesIntoUpstreamCode)]
        public void APromisesListedRule_MatchesWhatTheCompiledCodeReaches_InBothDirections(ListedRule rule)
        {
            var (check, list) = TheProductRule(rule);

            ThenTheRuleHolds(() => check(list), rule.ToString());
        }

        [TestCase(ListedRule.OutsideTypesReachingTheDomain)]
        [TestCase(ListedRule.OutsideTypesReachingTheApplication)]
        [TestCase(ListedRule.OutsideTypesReachingThePolicies)]
        [TestCase(ListedRule.ApplicationEdgesBelowItOrIntoThePlatform)]
        [TestCase(ListedRule.ModuleEdgesIntoUpstreamCode)]
        public void EveryEntryDeletedFromAPromisesList_IsNamedAsOffending_AndAnEntryWithNoEdgeIsNamedForDeletion(ListedRule rule)
        {
            var (check, list) = TheProductRule(rule);

            ThenEachDeletedEntryIsNamedAndAGoneEntryIsNamedForDeletion(rule, check, list);
        }

        [TestCase(Neighbour.WorkItems)]
        [TestCase(Neighbour.TrackerIntegration)]
        public void TheInboundListOfANeighbourModule_NamesNoPromisesType(Neighbour neighbour)
        {
            var list = TheInboundListOf(neighbour);

            ThenTheListNamesNoPromisesType(neighbour.ToString(), list);
        }

        [Test]
        public void AnInboundListNamingAPromisesType_IsNamed_AndAListOfOtherTypesIsNot()
        {
            const string aPromisesCaller = "Lighthouse.Backend.Modules.Promises.Application.AMadeUpPromisesCaller";
            const string anUpdater = "Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.PortfolioUpdater";

            var failure = Assert.Throws<AssertionException>(
                () => ThenTheListNamesNoPromisesType("a made-up module", [anUpdater, aPromisesCaller]),
                $"A list naming {aPromisesCaller} passed the check. Why it matters: a Promises entry left on a neighbour's list would go unseen. " +
                "How to fix: report every entry in a namespace of the Promises module.");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure!.Message, Does.Contain(aPromisesCaller), $"The failure must name {aPromisesCaller}.");
                Assert.That(failure.Message, Does.Not.Contain(anUpdater), $"{anUpdater} is not a Promises type, so the failure must not name it.");
                Assert.DoesNotThrow(() => ThenTheListNamesNoPromisesType("a made-up module", [anUpdater]), "A list without a Promises type must pass the check.");
            }
        }

        [Test]
        public void APromisesTypeLeftInItsFormerHome_IsNamed_AndAWellPlacedTwinIsNot()
        {
            var twin = ModuleRules.TypesLiveInTheirLayer(TestModule.Value, MadeUpDomainNamespace, [nameof(ADeliveryBlock)], MadeUpFormerHome);
            var leftBehind = ModuleRules.TypesLiveInTheirLayer(TestModule.Value, MadeUpDomainNamespace, [nameof(ALeftBehindDeliveryBlock)], MadeUpFormerHome);

            ThenTheRuleNamesTheOffenderAndPassesItsTwin(leftBehind.Check, typeof(ALeftBehindDeliveryBlock).FullName!, twin.Check, "the layer rule");
        }

        [Test]
        public void ATypeInAPromisesLayerThatIsNotOnItsList_IsNamed_AndTheFullListPasses()
        {
            ThenTheRuleNamesTheOffenderAndPassesItsTwin(
                () => LayerHoldsExactly(TestModule.Value, MadeUpDomainNamespace, [nameof(ADeliveryBlock)]),
                typeof(ADeliveryBlockAskingTheLicence).FullName!,
                () => LayerHoldsExactly(TestModule.Value, MadeUpDomainNamespace, [nameof(ADeliveryBlock), nameof(ADeliveryBlockAskingTheLicence)]),
                "what the layer holds");
        }

        [Test]
        public void APromisesDomainTypeReachingServices_IsNamed_AndItsCompliantTwinIsNot()
        {
            var verdict = ModuleRules.DomainReachesOnlyTheKernelAndTheBaseLibrary(TestModule.Value, MadeUpDomain);

            ThenTheFailureNamesOnly(verdict.Check, typeof(ADeliveryBlockAskingTheLicence).FullName!, [typeof(ADeliveryBlock).FullName!], "the Domain rule");
        }

        [Test]
        public void AnOutsideTypeOffTheList_ReachingThePromisesDomain_IsNamed_AndTheListedCallerIsNot()
        {
            var verdict = DomainInboundRule(TestModule.Value, [typeof(AListedDomainCaller).FullName!], MadeUpModule, MadeUpDomain, OutsideFixtures("ListedCallers", "DomainNewcomer"));

            ThenTheFailureNamesOnly(verdict.Check, typeof(ANewcomerReachingTheDomain).FullName!, [typeof(AListedDomainCaller).FullName!], "the Domain inbound rule");
        }

        [Test]
        public void AnOutsideTypeOffTheList_ReachingThePromisesApplication_IsNamed_AndTheApiTheCompositionRootAndTheListedCallerAreNot()
        {
            var verdict = ApplicationInboundRule(
                TestModule.Value,
                [typeof(AListedApplicationCaller).FullName!],
                MadeUpModule,
                MadeUpApplication,
                MadeUpApi,
                RootPattern(MadeUpCompositionRoot),
                OutsideFixtures("Api", "CompositionRoot", "ListedCallers", "ApplicationNewcomer"));

            ThenTheFailureNamesOnly(
                verdict.Check,
                typeof(ANewcomerReachingTheApplication).FullName!,
                [typeof(AListedApplicationCaller).FullName!, MadeUpController, MadeUpCompositionRoot],
                "the Application inbound rule");
            ThenTheFailureSpeaksOfPromises(verdict.Check);
        }

        [Test]
        public void AnOutsideTypeReachingThePromisesPolicies_IsNamed_AndTheCompositionRootIsNot()
        {
            var verdict = PoliciesInboundRule(TestModule.Value, [], MadeUpModule, MadeUpPolicies, RootPattern(MadeUpCompositionRoot), OutsideFixtures("CompositionRoot", "ListedCallers", "PoliciesNewcomer"));

            ThenTheFailureNamesOnly(verdict.Check, typeof(ANewcomerReachingThePolicies).FullName!, [MadeUpCompositionRoot], "the Policies inbound rule");
        }

        [Test]
        public void AListedCallerThatNoLongerReachesThePromisesApplication_IsNamedForDeletion()
        {
            var verdict = ApplicationInboundRule(
                TestModule.Value,
                [typeof(AListedApplicationCaller).FullName!, AGoneEntry],
                MadeUpModule,
                MadeUpApplication,
                MadeUpApi,
                RootPattern(MadeUpCompositionRoot),
                OutsideFixtures("Api", "CompositionRoot", "ListedCallers"));

            ThenTheGoneEntryIsNamedForDeletion(verdict.Check, AGoneEntry, "the Application inbound rule");
        }

        [Test]
        public void APromisesApplicationReachingOnlyAListedPlatformEdgeAndWorkItemsAndTrackerIntegrationApplications_PassesBothOutboundRules()
        {
            var listedEdge = Edge(typeof(ACompliantDeliveryService), "Lighthouse.Backend.Services.Interfaces.ILighthouseClock");

            var belowOrPlatform = ApplicationOutboundRule(TestModule.Value, NamespacePatternOf(typeof(ACompliantDeliveryService)), TheLayersBelowTheApplicationAndThePlatform, [listedEdge]);
            var upstream = ModuleUpstreamRule(TestModule.Value, NamespacePatternOf(typeof(ACompliantDeliveryService)), []);

            Assert.That(
                belowOrPlatform.Edges,
                Is.EquivalentTo(new[] { listedEdge }),
                $"Expected the outbound rule to find exactly [{listedEdge}]. Why it matters: a rule that misses an edge proves nothing by passing, " +
                "and one that counts Work Items' or Tracker integration's Application flags a call Promises may make. How to fix: report every edge " +
                "into the namespaces the rule is handed, and nothing in a legal module's Application.");
            ThenTheRuleHolds(belowOrPlatform.Check, "Promises' Application towards the layers below it and the platform");
            ThenTheRuleHolds(upstream.Check, "Promises towards upstream code");
        }

        [TestCaseSource(nameof(PromisesTypesReachingForbiddenCode))]
        public void APromisesTypeReachingForbiddenCode_IsNamedWithTheEdge(Type offender, OutboundTarget target, string forbidden)
        {
            var namespaces = NamespacePatternOf(offender);
            var verdict = target == OutboundTarget.BelowOrPlatform
                ? ApplicationOutboundRule(TestModule.Value, namespaces, TheLayersBelowTheApplicationAndThePlatform, [])
                : ModuleUpstreamRule(TestModule.Value, namespaces, []);

            ThenTheFailureNamesOnly(verdict.Check, $"Not on the list, and offending: {Edge(offender, forbidden)}", [], $"the {target} rule");
        }

        private static IEnumerable<TestCaseData> PromisesTypesReachingForbiddenCode()
        {
            yield return new TestCaseData(typeof(ADeliveryServiceHoldingADbSet), OutboundTarget.BelowOrPlatform, "Microsoft.EntityFrameworkCore.DbSet`1")
                .SetName("APromisesApplicationTypeHoldingAnEntityFrameworkCoreDbSet_IsNamed");
            yield return new TestCaseData(typeof(ADeliveryServiceAskingTheAppSettings), OutboundTarget.BelowOrPlatform, "Lighthouse.Backend.Services.Interfaces.IAppSettingService")
                .SetName("APromisesApplicationTypeReachingAnUnlistedPlatformType_IsNamed");
            yield return new TestCaseData(typeof(ADeliveryServiceRunningAForecastingAdapter), OutboundTarget.BelowOrPlatform, "Lighthouse.Backend.Modules.Forecasting.Adapters.Background.ForecastUpdater")
                .SetName("APromisesApplicationTypeReachingAModuleAdapter_IsNamedByTheBelowRule");
            yield return new TestCaseData(typeof(ADeliveryServiceRunningAForecastingAdapter), OutboundTarget.Upstream, "Lighthouse.Backend.Modules.Forecasting.Adapters.Background.ForecastUpdater")
                .SetName("APromisesTypeReachingAnotherModulesAdapter_IsNamedByTheUpstreamRule");
            yield return new TestCaseData(typeof(ADeliveryServiceRunningTheWriteBackRound), OutboundTarget.Upstream, "Lighthouse.Backend.Services.Interfaces.IWriteBackTriggerService")
                .SetName("APromisesTypeReachingTheWriteBackRound_IsNamed");
            yield return new TestCaseData(typeof(ADeliveryServiceTouchingTheUpdateCode), OutboundTarget.Upstream, "Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.UpdateKey")
                .SetName("APromisesTypeReachingTheUpdateCode_IsNamed");
            yield return new TestCaseData(typeof(ADeliveryServiceAskingTheLicence), OutboundTarget.Upstream, "Lighthouse.Backend.Services.Interfaces.Licensing.ILicenseService")
                .SetName("APromisesTypeReachingLicensing_IsNamed");
        }

        private static (Action<IReadOnlyCollection<string>> Check, string[] List) TheProductRule(ListedRule rule)
        {
            var module = ProductionModule.Value;

            return rule switch
            {
                ListedRule.OutsideTypesReachingTheDomain => (
                    listed => DomainInboundRule(module, listed, ModuleRules.PromisesModule, PromisesDomain, ModuleRules.AnyNamespace).Check(),
                    OutsideTypesReachingTheDomain),
                ListedRule.OutsideTypesReachingTheApplication => (
                    listed => ApplicationInboundRule(module, listed, ModuleRules.PromisesModule, PromisesApplication, ApplicationCoreRules.TheApi, TheCompositionRootAndItsNestedTypes, OutsideTrackerIntegration).Check(),
                    OtherOutsideTypesReachingTheApplication),
                ListedRule.OutsideTypesReachingThePolicies => (
                    listed => PoliciesInboundRule(module, listed, ModuleRules.PromisesModule, PromisesPolicies, TheCompositionRootAndItsNestedTypes, ModuleRules.AnyNamespace).Check(),
                    OtherOutsideTypesReachingThePolicies),
                ListedRule.ApplicationEdgesBelowItOrIntoThePlatform => (
                    listed => ApplicationOutboundRule(module, PromisesApplication, TheLayersBelowTheApplicationAndThePlatform, listed).Check(),
                    EdgesOfTheApplicationBelowItOrIntoThePlatform),
                ListedRule.ModuleEdgesIntoUpstreamCode => (
                    listed => ModuleUpstreamRule(module, ModuleRules.PromisesModule, listed).Check(),
                    EdgesOfTheModuleIntoUpstreamCode),
                _ => throw new ArgumentOutOfRangeException(nameof(rule)),
            };
        }

        private static InboundVerdict DomainInboundRule(ModuleDefinition module, IReadOnlyCollection<string> listed, string moduleNamespaces, string domain, string outside)
            => ModuleRules.DomainIsReachedFromOutsideOnlyBy(module, listed, moduleNamespaces, domain, outside, PromisesWording);

        private static InboundVerdict ApplicationInboundRule(ModuleDefinition module, IReadOnlyCollection<string> listed, string moduleNamespaces, string application, string legalCallers, string compositionRoot, string outside)
            => ModuleRules.ApplicationIsReachedFromOutsideOnlyBy(module, listed, moduleNamespaces, application, legalCallers, compositionRoot, outside, PromisesWording);

        private static InboundVerdict PoliciesInboundRule(ModuleDefinition module, IReadOnlyCollection<string> listed, string moduleNamespaces, string policies, string compositionRoot, string outside)
        {
            var insideTheModule = IlDependencies.TypesIn(module, moduleNamespaces).ToHashSet(StringComparer.Ordinal);

            var dependents = IlDependencies.DependentsIn(module, outside, type => IsATypeIn(type, policies))
                .Where(dependent => !insideTheModule.Contains(dependent.TypeName))
                .Where(dependent => !Regex.IsMatch(dependent.TypeName, compositionRoot, RegexOptions.None, PatternTimeout))
                .ToList();

            return new InboundVerdict(
                dependents,
                listed,
                $"Types outside namespaces matching \"{moduleNamespaces}\" should reach types in namespaces matching \"{policies}\" only from the " +
                $"composition root matching \"{compositionRoot}\" or from the types on the list, because {PromisesWording.Of("Policies")} are how " +
                "Promises reacts to what happens elsewhere: the composition root registers them and the domain events reach them. An outside " +
                "type that calls a policy directly runs Promises' reaction out of order and ties itself to how Promises reacts. Publish the " +
                "domain event the policy reacts to instead; the list only shrinks.");
        }

        private static OutboundVerdict ApplicationOutboundRule(ModuleDefinition module, string application, string below, IReadOnlyCollection<string> listed)
            => ModuleRules.OutboundEdges(
                module,
                application,
                below,
                listed,
                $"Types in Promises' Application, namespaces matching \"{application}\", should not depend on any type in namespaces matching " +
                $"\"{below}\" except through the listed edges, because Promises' Application decides what a Delivery promises and reads its " +
                "records only through the listed platform ports. An Application type that holds a DbSet or the database context, runs a " +
                "module's adapter, shapes an API type or reaches a new platform service ties Promises to how the rest of the system stores and " +
                "shows its data. Read through a listed port, ask Work Items, Tracker integration, Forecasting or Flow metrics through their " +
                "Application, and return a Models type.");

        private static OutboundVerdict ModuleUpstreamRule(ModuleDefinition module, string moduleNamespaces, IReadOnlyCollection<string> listed)
            => new(
                IlDependencies.TypesIn(module, moduleNamespaces),
                [.. IlDependencies.DependentsIn(module, moduleNamespaces, IsUpstreamCode)
                    .SelectMany(dependent => dependent.Targets.Select(reached => $"{dependent.TypeName} -> {reached}"))],
                listed,
                $"Types in namespaces matching \"{moduleNamespaces}\" should not depend on another module's adapters, the update code, the " +
                "write-back round, Access or Licensing except through the listed edges, because Promises sits downstream of them: it answers " +
                "what a Delivery promises from what the other modules hand it. A Promises type that runs another module's adapter, queues an " +
                "update, takes part in the write-back round or checks a permission or the licence pulls that work into Promises and turns the " +
                "dependency around. Let the owning module do it and react to the domain event it publishes.",
                moduleNamespaces);

        private static bool IsUpstreamCode(TypeReference type)
            => IsATypeIn(type, TheNamespacedUpstreamCode)
                || (type.DeclaringType is null && TheWriteBackRound.Contains(type.FullName, StringComparer.Ordinal));

        // A generic parameter takes the namespace of the type that declares it; it is a placeholder, not a dependency.
        private static bool IsATypeIn(TypeReference type, string namespacePattern)
            => type is not GenericParameter && IlDependencies.ResidesInNamespaceMatching(type, namespacePattern);

        private static void LayerHoldsExactly(ModuleDefinition module, string layerNamespace, IReadOnlyCollection<string> typeNames)
        {
            var held = IlDependencies.TypesIn(module, $"^{Regex.Escape(layerNamespace)}$")
                .Select(fullName => fullName.Split('+')[0])
                .Distinct(StringComparer.Ordinal);

            InlineBaseline.AssertMatches(
                held,
                typeNames.Select(name => $"{layerNamespace}.{name}"),
                $"Namespace \"{layerNamespace}\" should hold exactly the types its list names, because the list is where a type is placed in " +
                "Promises' layer on purpose. A type that lands in the layer unlisted was placed there without that decision. Add it to the " +
                "layer's list if it belongs to Promises, or move it to the module that owns it.");
        }

        private static string[] TheInboundListOf(Neighbour neighbour)
        {
            var owner = neighbour switch
            {
                Neighbour.WorkItems => typeof(WorkItemsModuleBoundariesArchUnitTest),
                Neighbour.TrackerIntegration => typeof(TrackerIntegrationModuleBoundariesArchUnitTest),
                _ => throw new ArgumentOutOfRangeException(nameof(neighbour)),
            };

            var field = owner.GetField(NeighbourInboundList, BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(
                field,
                Is.Not.Null,
                $"{owner.Name} has no list called {NeighbourInboundList}. Why it matters: that list is the baseline of who may reach the " +
                "module's Application, and this check reads it. How to fix: keep the list under that name.");

            return (string[])field!.GetValue(null)!;
        }

        private static string OutsideFixtures(params string[] namespaces)
            => $"^{Regex.Escape(MadeUpOutside)}\\.({string.Join('|', namespaces)})(\\.|$)";

        private static string RootPattern(string compositionRoot) => $"^{Regex.Escape(compositionRoot)}(\\+|$)";

        private static string NamespacePatternOf(Type type) => $"^{Regex.Escape(type.Namespace!)}(\\.|$)";

        private static string Edge(Type dependent, string target) => $"{dependent.FullName} -> {target}";

        private static void ThenTheRuleHolds(Action check, string what)
            => Assert.DoesNotThrow(
                () => check(),
                $"The rule for {what} failed; the failure above names each type or edge to fix. Why it matters: Promises keeps its shape only " +
                "while every list matches what the compiled code reaches, in both directions. How to fix: undo the new dependency, or delete " +
                "the entry the failure names as no longer offending.");

        private static void ThenTheListNamesNoPromisesType(string owner, IReadOnlyCollection<string> list)
        {
            var promisesEntries = list.Where(entry => Regex.IsMatch(entry, ModuleRules.PromisesModule, RegexOptions.None, PatternTimeout)).ToList();

            Assert.That(
                promisesEntries,
                Is.Empty,
                $"The inbound list of {owner} still names Promises types: [{string.Join(", ", promisesEntries)}]. Why it matters: Promises builds " +
                "on that module by design, so its calls are legal by namespace; an entry for one keeps a ratchet on a dependency that is allowed " +
                "and hides the list's real size. How to fix: make Promises' Application a legal caller of that module's Application by namespace " +
                "and delete exactly these entries from the list.");
        }

        private static void ThenEachDeletedEntryIsNamedAndAGoneEntryIsNamedForDeletion(ListedRule rule, Action<IReadOnlyCollection<string>> check, string[] list)
        {
            using (Assert.EnterMultipleScope())
            {
                foreach (var entry in list)
                {
                    var failure = Assert.Throws<AssertionException>(
                        () => check([.. list.Where(listed => listed != entry)]),
                        $"{rule} passed with {entry} deleted from its list. Why it matters: an edge the list misses would go in unseen. How to " +
                        "fix: report every offender that is not on the list.");

                    Assert.That(failure?.Message, Does.Contain("Not on the list, and offending: ").And.Contain(entry), $"{rule} must name {entry} as offending once it is off the list.");
                }

                ThenTheGoneEntryIsNamedForDeletion(() => check([.. list, AGoneEntry]), AGoneEntry, rule.ToString());
            }
        }

        private static void ThenTheGoneEntryIsNamedForDeletion(Action check, string gone, string what)
        {
            var failure = Assert.Throws<AssertionException>(
                () => check(),
                $"{what} passed with {gone} on its list although nothing like it is in the code. Why it matters: a list that keeps a gone entry " +
                "lets that dependency come back unseen. How to fix: compare the list with what was found in both directions.");

            Assert.That(failure?.Message, Does.Contain($"On the list, but no longer offending: {gone}. Good - delete it"), $"{what} must name {gone} for deletion.");
        }

        private static void ThenTheFailureNamesOnly(Action check, string offender, IReadOnlyCollection<string> notNamed, string what)
        {
            var failure = Assert.Throws<AssertionException>(
                () => check(),
                $"{what} passed although {offender} breaks it. Why it matters: a rule that cannot fail on a made-up offender guards nothing. " +
                "How to fix: report every type or edge the rule forbids.");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure!.Message, Does.Contain(offender), $"{what} must name {offender}.");

                foreach (var compliant in notNamed)
                {
                    Assert.That(failure.Message, Does.Not.Match($"{Regex.Escape(compliant)}(?![A-Za-z0-9+`])"), $"{what} must not name {compliant}, which is allowed.");
                }
            }
        }

        private static void ThenTheRuleNamesTheOffenderAndPassesItsTwin(Action offending, string offender, Action twin, string what)
        {
            ThenTheFailureNamesOnly(offending, offender, [], what);

            Assert.DoesNotThrow(() => twin(), $"{what} failed on a compliant twin. Why it matters: a rule that fails on compliant code gets switched off. How to fix: report only what the rule forbids.");
        }

        private static void ThenTheFailureSpeaksOfPromises(Action check)
        {
            var failure = Assert.Throws<AssertionException>(() => check(), "The Application inbound rule passed with a newcomer; there is no failure text to read.");

            Assert.That(
                failure!.Message,
                Does.Contain("Promises' Application is the module's front door for the API").And.Not.Contain("Flow metrics"),
                "A failing Promises rule must speak of Promises and its legal callers, not of Flow metrics. Why it matters: the reader is sent " +
                "to the wrong module. How to fix: hand the rule Promises' wording.");
        }
    }
}

namespace Lighthouse.Backend.Modules.PromisesFixtures.Boundaries.Domain
{
    internal sealed record ADeliveryBlock(DateOnly ExpectedDate)
    {
        public int DaysFrom(DateOnly today) => ExpectedDate.DayNumber - today.DayNumber;
    }

    internal sealed class ADeliveryBlockAskingTheLicence(global::Lighthouse.Backend.Services.Interfaces.Licensing.ILicenseService licence)
    {
        public bool CanShow() => licence.CanUsePremiumFeatures();
    }
}

namespace Lighthouse.Backend.Modules.PromisesFixtures.Boundaries.Services
{
    internal sealed record ALeftBehindDeliveryBlock(DateOnly ExpectedDate);
}

namespace Lighthouse.Backend.Modules.PromisesFixtures.Boundaries.Application
{
    internal static class ADeliveryAnswerService
    {
        public static int Answer(DateOnly today) => new global::Lighthouse.Backend.Modules.PromisesFixtures.Boundaries.Domain.ADeliveryBlock(today.AddDays(14)).DaysFrom(today);
    }
}

namespace Lighthouse.Backend.Modules.PromisesFixtures.Boundaries.Application.Policies
{
    internal static class ADeliveryReaction
    {
        public static int React(DateOnly today) => global::Lighthouse.Backend.Modules.PromisesFixtures.Boundaries.Application.ADeliveryAnswerService.Answer(today);
    }
}

namespace Lighthouse.Backend.PromisesBoundariesFixtures.Api
{
    internal static class ADeliveriesController
    {
        public static int Get(DateOnly today) => global::Lighthouse.Backend.Modules.PromisesFixtures.Boundaries.Application.ADeliveryAnswerService.Answer(today);
    }
}

namespace Lighthouse.Backend.PromisesBoundariesFixtures.CompositionRoot
{
    internal static class APromisesCompositionRoot
    {
        public static int Register(DateOnly today) => global::Lighthouse.Backend.Modules.PromisesFixtures.Boundaries.Application.Policies.ADeliveryReaction.React(today);

        internal static class Registrations
        {
            public static int Register(DateOnly today) => global::Lighthouse.Backend.Modules.PromisesFixtures.Boundaries.Application.ADeliveryAnswerService.Answer(today);
        }
    }
}

namespace Lighthouse.Backend.PromisesBoundariesFixtures.ListedCallers
{
    internal static class AListedDomainCaller
    {
        public static int Answer(DateOnly today) => new global::Lighthouse.Backend.Modules.PromisesFixtures.Boundaries.Domain.ADeliveryBlock(today).DaysFrom(today);
    }

    internal static class AListedApplicationCaller
    {
        public static int Answer(DateOnly today) => global::Lighthouse.Backend.Modules.PromisesFixtures.Boundaries.Application.ADeliveryAnswerService.Answer(today);
    }
}

namespace Lighthouse.Backend.PromisesBoundariesFixtures.DomainNewcomer
{
    internal static class ANewcomerReachingTheDomain
    {
        public static int Answer(DateOnly today) => new global::Lighthouse.Backend.Modules.PromisesFixtures.Boundaries.Domain.ADeliveryBlock(today).DaysFrom(today.AddDays(-1));
    }
}

namespace Lighthouse.Backend.PromisesBoundariesFixtures.ApplicationNewcomer
{
    internal static class ANewcomerReachingTheApplication
    {
        public static async Task<int> AnswerAsync(DateOnly today)
        {
            await Task.Yield();

            return global::Lighthouse.Backend.Modules.PromisesFixtures.Boundaries.Application.ADeliveryAnswerService.Answer(today);
        }
    }
}

namespace Lighthouse.Backend.PromisesBoundariesFixtures.PoliciesNewcomer
{
    internal static class ANewcomerReachingThePolicies
    {
        public static int React(DateOnly today) => global::Lighthouse.Backend.Modules.PromisesFixtures.Boundaries.Application.Policies.ADeliveryReaction.React(today);
    }
}

namespace Lighthouse.Backend.Modules.PromisesFixtures.BoundariesOutbound.Compliant.Application
{
    internal sealed class ACompliantDeliveryService(
        global::Lighthouse.Backend.Services.Interfaces.ILighthouseClock clock,
        global::Lighthouse.Backend.Modules.WorkItems.Application.ITeamDataService teams,
        global::Lighthouse.Backend.Modules.TrackerIntegration.Application.IWorkTrackingConnectorFactory connectors)
    {
        public string Describe() => $"{clock.GetHashCode()} {teams.GetHashCode()} {connectors.GetHashCode()}";
    }
}

namespace Lighthouse.Backend.Modules.PromisesFixtures.BoundariesOutbound.HoldingADbSet.Application
{
    internal sealed class ADeliveryServiceHoldingADbSet(global::Microsoft.EntityFrameworkCore.DbSet<global::Lighthouse.Backend.Models.Delivery> deliveries)
    {
        public global::Microsoft.EntityFrameworkCore.DbSet<global::Lighthouse.Backend.Models.Delivery> Deliveries { get; } = deliveries;
    }
}

namespace Lighthouse.Backend.Modules.PromisesFixtures.BoundariesOutbound.AskingTheAppSettings.Application
{
    internal sealed class ADeliveryServiceAskingTheAppSettings(global::Lighthouse.Backend.Services.Interfaces.IAppSettingService settings)
    {
        public int Describe() => settings.GetHashCode();
    }
}

namespace Lighthouse.Backend.Modules.PromisesFixtures.BoundariesOutbound.RunningAForecastingAdapter.Application
{
    internal static class ADeliveryServiceRunningAForecastingAdapter
    {
        public static string Describe() => typeof(global::Lighthouse.Backend.Modules.Forecasting.Adapters.Background.ForecastUpdater).Name;
    }
}

namespace Lighthouse.Backend.Modules.PromisesFixtures.BoundariesOutbound.RunningTheWriteBackRound.Application
{
    internal sealed class ADeliveryServiceRunningTheWriteBackRound(global::Lighthouse.Backend.Services.Interfaces.IWriteBackTriggerService trigger)
    {
        public int Describe() => trigger.GetHashCode();
    }
}

namespace Lighthouse.Backend.Modules.PromisesFixtures.BoundariesOutbound.TouchingTheUpdateCode.Application
{
    internal static class ADeliveryServiceTouchingTheUpdateCode
    {
        public static string Describe() => typeof(global::Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.UpdateKey).Name;
    }
}

namespace Lighthouse.Backend.Modules.PromisesFixtures.BoundariesOutbound.AskingTheLicence.Application
{
    internal sealed class ADeliveryServiceAskingTheLicence(global::Lighthouse.Backend.Services.Interfaces.Licensing.ILicenseService licence)
    {
        public bool CanShow() => licence.CanUsePremiumFeatures();
    }
}
