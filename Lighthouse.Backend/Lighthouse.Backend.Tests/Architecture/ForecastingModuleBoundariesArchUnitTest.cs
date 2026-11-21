using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using Lighthouse.Backend.ForecastingModuleBoundariesFixtures.AdaptersNewcomer;
using Lighthouse.Backend.ForecastingModuleBoundariesFixtures.ApplicationNewcomer;
using Lighthouse.Backend.ForecastingModuleBoundariesFixtures.CompositionRoot;
using Lighthouse.Backend.ForecastingModuleBoundariesFixtures.DomainNewcomer;
using Lighthouse.Backend.ForecastingModuleBoundariesFixtures.FormerCallers;
using Lighthouse.Backend.ForecastingModuleBoundariesFixtures.ListedCallers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundaries.Adapters.Background;
using Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundaries.Application;
using Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundaries.Domain;
using Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundariesOutbound.CompliantAdapters.Adapters;
using Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundariesOutbound.CompliantApplication.Application;
using Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundariesOutbound.HoldingADbSet.Application;
using Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundariesOutbound.OnAGenericBase.Adapters;
using Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundariesOutbound.OnAGenericBase.SharedUpdateCode;
using Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundariesOutbound.ReachingAModuleAdapter.Application;
using Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundariesOutbound.SecondLicensingType.Application;
using Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundariesOutbound.SecondUpdateType.Adapters;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Forecasting is a module with rules of its own, checked by the shared module rules with Forecasting's namespaces
    /// and Forecasting's wording, and read from the compiled production assembly: outside code reaches its Domain only
    /// from the listed types, its Adapters only from the composition root, and its Application only from the API, the
    /// composition root, Refinement and the listed types; its Application reaches no EF Core, data-layer, API or module
    /// adapter type and the licence only through the listed edges; its Adapters reach the update code they share with
    /// the Team and Portfolio updaters only through the listed edges. Every list is compared both ways. Flow metrics'
    /// own inbound list names no Forecasting type any more, because Forecasting now calls Flow metrics by right. The
    /// same rules run on a made-up module: a newcomer and a gone list entry each fail and are named, a compliant twin
    /// passes, and a failing rule speaks of the module it checks.
    /// </summary>
    /// <remarks>
    /// The IL reader does not see types used only in a catch clause, an event or a named attribute argument.
    /// </remarks>
    [TestFixture]
    public class ForecastingModuleBoundariesArchUnitTest
    {
        private const string ForecastingModule = @"^Lighthouse\.Backend\.Modules\.Forecasting(\.|$)";

        private const string ForecastingDomain = @"^Lighthouse\.Backend\.Modules\.Forecasting\.Domain(\.|$)";

        private const string ForecastingApplication = @"^Lighthouse\.Backend\.Modules\.Forecasting\.Application(\.|$)";

        private const string ForecastingAdapters = @"^Lighthouse\.Backend\.Modules\.Forecasting\.Adapters(\.|$)";

        private const string FlowMetricsApplication = @"^Lighthouse\.Backend\.Modules\.FlowMetrics\.Application(\.|$)";

        private const string TheCompositionRootAndItsNestedTypes = @"^Lighthouse\.Backend\.Program(\+|$)";

        private const string EntityFrameworkCore = @"^Microsoft\.EntityFrameworkCore(\.|$)";

        private const string TheLicence = @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.Licensing(\.|$)" + "|" + ModuleRules.LicensingModule;

        private const string FlowMetricsInboundList = "OtherOutsideTypesReachingTheApplication";

        private const string WordingTypeName = "Lighthouse.Backend.Tests.Architecture.ModuleWording";

        private const string ForecastingInWords = "Forecasting";

        private const string ForecastingCallersInWords = "the API and the Refinement module";

        private const string FlowMetricsInWords = "Flow metrics";

        private const string ACallerOnlyFlowMetricsHas = "Reports";

        private const string MadeUpModule = @"^Lighthouse\.Backend\.Modules\.ForecastingFixtures\.ModuleBoundaries(\.|$)";

        private const string MadeUpDomain = @"^Lighthouse\.Backend\.Modules\.ForecastingFixtures\.ModuleBoundaries\.Domain(\.|$)";

        private const string MadeUpApplication = @"^Lighthouse\.Backend\.Modules\.ForecastingFixtures\.ModuleBoundaries\.Application(\.|$)";

        private const string MadeUpAdapters = @"^Lighthouse\.Backend\.Modules\.ForecastingFixtures\.ModuleBoundaries\.Adapters(\.|$)";

        private const string MadeUpApi = @"^Lighthouse\.Backend\.ForecastingModuleBoundariesFixtures\.Api(\.|$)";

        private const string MadeUpOutboundCases = "Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundariesOutbound";

        private const string Api = "Api";

        private const string CompositionRoot = "CompositionRoot";

        private const string ListedCallers = "ListedCallers";

        private const string FormerCallers = "FormerCallers";

        private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

        // The outside types that reach Forecasting's Domain. The list only shrinks.
        private static readonly string[] OutsideTypesReachingTheDomain =
        [
            "Lighthouse.Backend.Program",
        ];

        // The outside types, besides the API, the composition root and Refinement, that reach Forecasting's Application.
        // The list only shrinks.
        private static readonly string[] OtherOutsideTypesReachingTheApplication =
        [
            "Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.PortfolioUpdater",
        ];

        // The edges from Forecasting's Application into EF Core, the data layer, the API, its own Adapters or the licence.
        // The list only shrinks.
        private static readonly string[] EdgesOfTheApplicationBelowItOrIntoTheLicence =
        [
            "Lighthouse.Backend.Modules.Forecasting.Application.ForecastFilterRuleService -> Lighthouse.Backend.Services.Interfaces.Licensing.ILicenseService",
        ];

        // The edges from Forecasting's Adapters into the update code the Team and Portfolio updaters use too. It stays
        // where it is until the update code is split by module; the list only shrinks.
        private static readonly string[] EdgesOfTheAdaptersIntoTheSharedUpdateCode =
        [
            "Lighthouse.Backend.Modules.Forecasting.Adapters.Background.ForecastUpdater -> Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.UpdateCancellationContext",
            "Lighthouse.Backend.Modules.Forecasting.Adapters.Background.ForecastUpdater -> Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.UpdateKey",
            "Lighthouse.Backend.Modules.Forecasting.Adapters.Background.ForecastUpdater -> Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.UpdateServiceBase`1",
        ];

        private static readonly string TheLayersBelowTheApplicationAndTheLicence =
            $"(?:{EntityFrameworkCore})|(?:{ApplicationCoreRules.TheData})|(?:{ApplicationCoreRules.TheApi})|(?:{ForecastingAdapters})|(?:{TheLicence})";

        private static readonly string TheLayersBelowTheMadeUpApplicationAndTheLicence =
            $"(?:{EntityFrameworkCore})|(?:{ApplicationCoreRules.TheData})|(?:{ApplicationCoreRules.TheApi})|(?:{MadeUpAdapters})|(?:{TheLicence})";

        private static readonly string ForecastingsLegalCallers =
            $"(?:{ApplicationCoreRules.TheApi})|(?:{ModuleBoundariesArchUnitTest.RefinementPattern})";

        private static readonly string FlowMetricsLegalCallers =
            $"(?:{ApplicationCoreRules.TheApi})|(?:{ModuleBoundariesArchUnitTest.ReportsPattern})|(?:{ModuleBoundariesArchUnitTest.RefinementPattern})|(?:{ForecastingModule})";

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(ForecastingModuleBoundariesArchUnitTest).Assembly));

        public enum ReachedLayer
        {
            Domain,
            Adapters,
            Application,
        }

        private static string MadeUpCompositionRoot => typeof(AForecastCompositionRoot).FullName!;

        private static string LicenceService => typeof(ILicenseService).FullName!;

        private static string UpdateKeyType => typeof(UpdateKey).FullName!;

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void TheForecastingDomain_IsReachedFromOutsideOnlyByTheListedTypes()
        {
            var verdict = ForecastingsDomainRule(ProductionModule.Value, OutsideTypesReachingTheDomain, ForecastingModule, ForecastingDomain, ModuleRules.AnyNamespace);

            verdict.Check();
        }

        [Test]
        public void TheForecastingAdapters_AreReachedFromOutsideOnlyByTheCompositionRoot()
        {
            var verdict = ForecastingsAdaptersRule(ProductionModule.Value, ModuleRules.TheCompositionRoot, ForecastingModule, ForecastingAdapters, ModuleRules.AnyNamespace);

            verdict.Check();
        }

        [Test]
        public void TheForecastingApplication_IsReachedFromOutsideOnlyByTheApiTheCompositionRootRefinementAndTheListedTypes()
        {
            var verdict = ForecastingsApplicationRule(
                ProductionModule.Value,
                OtherOutsideTypesReachingTheApplication,
                ForecastingModule,
                ForecastingApplication,
                ForecastingsLegalCallers,
                TheCompositionRootAndItsNestedTypes,
                ModuleRules.AnyNamespace);

            verdict.Check();
        }

        [Test]
        public void TheForecastingApplication_ReachesNoEntityFrameworkCoreDataApiOrModuleAdapterType_AndTheLicenceOnlyThroughTheListedEdges()
        {
            var verdict = ForecastingsApplicationOutboundRule(ProductionModule.Value, ForecastingApplication, TheLayersBelowTheApplicationAndTheLicence, EdgesOfTheApplicationBelowItOrIntoTheLicence);

            verdict.Check();
        }

        [Test]
        public void TheForecastingAdapters_ReachTheSharedUpdateCodeOnlyThroughTheListedEdges()
        {
            var verdict = ForecastingsAdaptersOutboundRule(ProductionModule.Value, ForecastingAdapters, EdgesOfTheAdaptersIntoTheSharedUpdateCode);

            verdict.Check();
        }

        [Test]
        public void TheFlowMetricsInboundList_NamesNoForecastingType_AndStillMatchesWhatReachesFlowMetricsFromOutside()
        {
            var list = TheFlowMetricsInboundList();

            var verdict = ModuleRules.ApplicationIsReachedFromOutsideOnlyBy(
                ProductionModule.Value,
                list,
                ModuleRules.FlowMetricsModule,
                FlowMetricsApplication,
                FlowMetricsLegalCallers,
                TheCompositionRootAndItsNestedTypes,
                ModuleRules.AnyNamespace);

            ThenTheFlowMetricsListNamesNoForecastingTypeAndMatches(list, verdict);
        }

        [TestCase(ReachedLayer.Domain)]
        [TestCase(ReachedLayer.Adapters)]
        [TestCase(ReachedLayer.Application)]
        public void OnlyTheAllowedOutsideTypesReachingTheLayer_PassForecastingsRule(ReachedLayer layer)
        {
            var verdict = ForecastingsRuleOverMadeUpTypes(layer, [Api, CompositionRoot, ListedCallers], ListedFor(layer));

            ThenTheRuleSawExactlyAndPasses(verdict, layer, AllowedFor(layer));
        }

        [TestCase(ReachedLayer.Domain)]
        [TestCase(ReachedLayer.Adapters)]
        [TestCase(ReachedLayer.Application)]
        public void AnOutsideTypeNotOnTheList_FailsForecastingsRuleAndIsNamedWithWhatItReaches(ReachedLayer layer)
        {
            var verdict = ForecastingsRuleOverMadeUpTypes(layer, [Api, CompositionRoot, ListedCallers, NewcomerNamespace(layer)], ListedFor(layer));

            ThenTheRuleFailsNamingOnlyTheNewcomer(verdict, Newcomer(layer), ReachedBy(layer));
        }

        [TestCase(ReachedLayer.Domain)]
        [TestCase(ReachedLayer.Adapters)]
        [TestCase(ReachedLayer.Application)]
        public void AListedTypeThatNoLongerReachesTheLayer_FailsForecastingsRuleAndIsNamedForDeletion(ReachedLayer layer)
        {
            var former = typeof(AFormerForecastCaller).FullName!;

            // The Adapters rule allows only the composition root, so its gone entry is a root that registers nothing any
            // more, while the real one is left out of the outside types.
            var verdict = layer == ReachedLayer.Adapters
                ? ForecastingsRuleOverMadeUpTypes(layer, [Api, ListedCallers, FormerCallers], [], compositionRootNamed: former)
                : ForecastingsRuleOverMadeUpTypes(layer, [Api, CompositionRoot, ListedCallers, FormerCallers], [.. ListedFor(layer), former]);

            ThenTheRuleFailsNamingOnlyTheStaleEntryForDeletion(verdict, former);
        }

        [TestCase(ReachedLayer.Domain)]
        [TestCase(ReachedLayer.Adapters)]
        [TestCase(ReachedLayer.Application)]
        public void AFailingForecastingRule_SpeaksOfForecastingAndNotOfFlowMetrics(ReachedLayer layer)
        {
            var verdict = ForecastingsRuleOverMadeUpTypes(layer, [Api, CompositionRoot, ListedCallers, NewcomerNamespace(layer)], ListedFor(layer));

            ThenTheFailureSpeaksOfForecasting(verdict, layer);
        }

        [Test]
        public void AFailingFlowMetricsApplicationRule_SpeaksOfFlowMetricsByDefault()
        {
            var verdict = ModuleRules.ApplicationIsReachedFromOutsideOnlyBy(
                TestModule.Value,
                [],
                MadeUpModule,
                MadeUpApplication,
                MadeUpApi,
                $"^{Regex.Escape(MadeUpCompositionRoot)}(\\+|$)",
                OutsideFixtures([Api, CompositionRoot, ListedCallers]));

            ThenTheFailureSpeaksOfFlowMetrics(verdict);
        }

        [Test]
        public void AForecastingApplicationReachingOnlyItsModelsAndAListedLicenceEdge_PassesTheOutboundRule()
        {
            var compliantEdge = Edge(typeof(ACompliantForecastService), LicenceService);

            var verdict = MadeUpApplicationOutbound(typeof(ACompliantForecastService), [compliantEdge]);

            ThenTheOutboundRuleSawExactlyAndPasses(verdict, [compliantEdge]);
        }

        [TestCaseSource(nameof(ApplicationTypesReachingBelowOrIntoTheLicence))]
        public void AForecastingApplicationTypeReachingALayerBelowOrAnUnlistedLicenceType_FailsAndIsNamed(Type offender, string[] listed, string newEdge)
        {
            var verdict = MadeUpApplicationOutbound(offender, listed);

            ThenTheOutboundRuleFailsNamingOnlyTheNewEdge(verdict, newEdge);
        }

        [Test]
        public void AListedLicenceEdgeOfTheForecastingApplicationThatIsGone_FailsAndIsNamedForDeletion()
        {
            var compliantEdge = Edge(typeof(ACompliantForecastService), LicenceService);
            var goneEdge = Edge(typeof(ACompliantForecastService), typeof(ILicenseVerifier).FullName!);

            var verdict = MadeUpApplicationOutbound(typeof(ACompliantForecastService), [compliantEdge, goneEdge]);

            ThenTheOutboundRuleFailsNamingOnlyTheStaleEdgeForDeletion(verdict, goneEdge);
        }

        [Test]
        public void ForecastingAdaptersReachingTheSharedUpdateCodeOnlyThroughAListedEdge_PassTheOutboundRule()
        {
            var listedEdge = Edge(typeof(ACompliantForecastJob), UpdateKeyType);

            var verdict = MadeUpAdaptersOutbound(typeof(ACompliantForecastJob), [listedEdge]);

            ThenTheOutboundRuleSawExactlyAndPasses(verdict, [listedEdge]);
        }

        [Test]
        public void AForecastingAdapterReachingASecondTypeOfTheSharedUpdateCode_FailsAndNamesOnlyTheNewEdge()
        {
            var verdict = MadeUpAdaptersOutbound(typeof(AForecastJobReachingMoreUpdateCode), [Edge(typeof(AForecastJobReachingMoreUpdateCode), UpdateKeyType)]);

            ThenTheOutboundRuleFailsNamingOnlyTheNewEdge(verdict, Edge(typeof(AForecastJobReachingMoreUpdateCode), typeof(UpdateCancellationContext).FullName!));
        }

        [Test]
        public void AListedEdgeOfTheForecastingAdaptersIntoTheSharedUpdateCodeThatIsGone_FailsAndIsNamedForDeletion()
        {
            var listedEdge = Edge(typeof(ACompliantForecastJob), UpdateKeyType);
            var goneEdge = Edge(typeof(ACompliantForecastJob), typeof(UpdateCancellationContext).FullName!);

            var verdict = MadeUpAdaptersOutbound(typeof(ACompliantForecastJob), [listedEdge, goneEdge]);

            ThenTheOutboundRuleFailsNamingOnlyTheStaleEdgeForDeletion(verdict, goneEdge);
        }

        [Test]
        public void AForecastingAdapterBuiltOnAGenericBaseOfTheSharedUpdateCode_HasAnEdgeToTheBaseOnly_NotToItsTypeParameter()
        {
            var baseEdge = Edge(typeof(AForecastJobOnAGenericBase), typeof(AnUpdaterBase<>).FullName!);

            var verdict = ForecastingsAdaptersOutboundRule(
                TestModule.Value,
                LayerOf(typeof(AForecastJobOnAGenericBase), "Adapters"),
                [baseEdge],
                LayerOf(typeof(AForecastJobOnAGenericBase), "SharedUpdateCode"));

            ThenTheOutboundRuleSawExactlyAndPasses(verdict, [baseEdge]);
        }

        private static IEnumerable<TestCaseData> ApplicationTypesReachingBelowOrIntoTheLicence()
        {
            yield return new TestCaseData(typeof(AForecastServiceHoldingADbSet), Array.Empty<string>(), Edge(typeof(AForecastServiceHoldingADbSet), "Microsoft.EntityFrameworkCore.DbSet`1"))
                .SetName("AForecastingApplicationTypeHoldingAnEntityFrameworkCoreDbSet_FailsAndIsNamed");
            yield return new TestCaseData(typeof(AForecastServiceRunningAModuleAdapter), Array.Empty<string>(), Edge(typeof(AForecastServiceRunningAModuleAdapter), typeof(AForecastJob).FullName!))
                .SetName("AForecastingApplicationTypeReachingAModuleAdapter_FailsAndIsNamed");
            yield return new TestCaseData(
                    typeof(AForecastServiceReadingTheLicenceTwice),
                    new[] { Edge(typeof(AForecastServiceReadingTheLicenceTwice), typeof(ILicenseService).FullName!) },
                    Edge(typeof(AForecastServiceReadingTheLicenceTwice), typeof(ILicenseVerifier).FullName!))
                .SetName("AListedForecastingApplicationTypeReachingASecondLicenceType_FailsAndNamesOnlyTheNewEdge");
        }

        private static InboundVerdict ForecastingsDomainRule(ModuleDefinition module, IReadOnlyCollection<string> listed, string moduleNamespaces, string domain, string outside)
            => TheSharedInboundRule(
                nameof(ModuleRules.DomainIsReachedFromOutsideOnlyBy),
                new Dictionary<string, object?>
                {
                    ["module"] = module,
                    ["listed"] = listed,
                    ["moduleNamespaces"] = moduleNamespaces,
                    ["domain"] = domain,
                    ["outside"] = outside,
                },
                wordingIsRequired: false);

        private static InboundVerdict ForecastingsAdaptersRule(ModuleDefinition module, string compositionRoot, string moduleNamespaces, string adapters, string outside)
            => TheSharedInboundRule(
                nameof(ModuleRules.AdaptersAreReachedFromOutsideOnlyByTheCompositionRoot),
                new Dictionary<string, object?>
                {
                    ["module"] = module,
                    ["compositionRoot"] = compositionRoot,
                    ["moduleNamespaces"] = moduleNamespaces,
                    ["adapters"] = adapters,
                    ["outside"] = outside,
                },
                wordingIsRequired: false);

        // The Application rule's text names who may call the module, so it must take the module's wording.
        private static InboundVerdict ForecastingsApplicationRule(
            ModuleDefinition module,
            IReadOnlyCollection<string> listed,
            string moduleNamespaces,
            string application,
            string legalCallerNamespaces,
            string compositionRoot,
            string outside)
            => TheSharedInboundRule(
                nameof(ModuleRules.ApplicationIsReachedFromOutsideOnlyBy),
                new Dictionary<string, object?>
                {
                    ["module"] = module,
                    ["listed"] = listed,
                    ["moduleNamespaces"] = moduleNamespaces,
                    ["application"] = application,
                    ["legalCallerNamespaces"] = legalCallerNamespaces,
                    ["compositionRoot"] = compositionRoot,
                    ["outside"] = outside,
                },
                wordingIsRequired: true);

        private static OutboundVerdict ForecastingsApplicationOutboundRule(ModuleDefinition module, string application, string below, IReadOnlyCollection<string> listed)
            => ModuleRules.OutboundEdges(
                module,
                application,
                below,
                listed,
                $"Types in Forecasting's Application, namespaces matching \"{application}\", should not depend on any type in namespaces matching " +
                $"\"{below}\" except through the listed edges, because Forecasting's Application decides what a forecast answers and reads its " +
                "records only through repository ports. An Application type that holds a DbSet or the database context, runs one of the " +
                "module's adapters or shapes an API type ties forecasts to how they are stored and shown; one that asks the licence in a " +
                "new place spreads the premium check across the module. Read the records through a repository port, return a Models type, " +
                "and ask the licence where it is already asked.");

        private static OutboundVerdict ForecastingsAdaptersOutboundRule(ModuleDefinition module, string adapters, IReadOnlyCollection<string> listed, string sharedUpdateCode = KernelRules.TheUpdateQueue)
            => ModuleRules.OutboundEdges(
                module,
                adapters,
                sharedUpdateCode,
                listed,
                $"Types in Forecasting's Adapters, namespaces matching \"{adapters}\", should reach the update code in namespaces matching " +
                $"\"{sharedUpdateCode}\" only through the listed edges, because that code is shared with the Team and Portfolio " +
                "updaters and is still to be split by module; every new edge ties Forecasting's background work tighter to the other " +
                "updaters. Use the update contracts in Services.Interfaces.Update instead.");

        private static InboundVerdict ForecastingsRuleOverMadeUpTypes(ReachedLayer layer, IReadOnlyCollection<string> outsideNamespaces, IReadOnlyCollection<string> listed, string? compositionRootNamed = null)
            => layer switch
            {
                ReachedLayer.Domain => ForecastingsDomainRule(TestModule.Value, listed, MadeUpModule, MadeUpDomain, OutsideFixtures(outsideNamespaces)),
                ReachedLayer.Adapters => ForecastingsAdaptersRule(TestModule.Value, compositionRootNamed ?? MadeUpCompositionRoot, MadeUpModule, MadeUpAdapters, OutsideFixtures(outsideNamespaces)),
                ReachedLayer.Application => ForecastingsApplicationRule(
                    TestModule.Value,
                    listed,
                    MadeUpModule,
                    MadeUpApplication,
                    MadeUpApi,
                    $"^{Regex.Escape(MadeUpCompositionRoot)}(\\+|$)",
                    OutsideFixtures(outsideNamespaces)),
                _ => throw new ArgumentOutOfRangeException(nameof(layer)),
            };

        private static OutboundVerdict MadeUpApplicationOutbound(Type madeUpApplicationType, IReadOnlyCollection<string> listed)
            => ForecastingsApplicationOutboundRule(TestModule.Value, LayerOf(madeUpApplicationType, "Application"), TheLayersBelowTheMadeUpApplicationAndTheLicence, listed);

        private static OutboundVerdict MadeUpAdaptersOutbound(Type madeUpAdapterType, IReadOnlyCollection<string> listed)
            => ForecastingsAdaptersOutboundRule(TestModule.Value, LayerOf(madeUpAdapterType, "Adapters"), listed);

        // The list as the Flow metrics rule test holds it, so the two cannot drift apart.
        private static IReadOnlyList<string> TheFlowMetricsInboundList()
        {
            var field = typeof(FlowMetricsInboundApplicationArchUnitTest).GetField(FlowMetricsInboundList, BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(
                field,
                Is.Not.Null,
                $"{nameof(FlowMetricsInboundApplicationArchUnitTest)} has no list called {FlowMetricsInboundList}. Why it matters: that list is the " +
                "baseline of what may reach Flow metrics' Application, and this check reads it. How to fix: keep the list under that name.");

            return (string[])field!.GetValue(null)!;
        }

        // A shared rule is called by name, and handed Forecasting's wording when it takes one, so this class compiles
        // before and after the wording becomes a parameter.
        private static InboundVerdict TheSharedInboundRule(string ruleName, Dictionary<string, object?> arguments, bool wordingIsRequired)
        {
            var candidates = typeof(ModuleRules).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .Where(method => method.Name == ruleName)
                .ToList();
            var rule = candidates.FirstOrDefault(method => method.GetParameters().Any(TakesAWording)) ?? candidates.Single();
            var wordingParameter = rule.GetParameters().FirstOrDefault(TakesAWording);

            if (wordingIsRequired && wordingParameter is null)
            {
                Assert.Fail(
                    $"The shared rule {ruleName} takes no {WordingTypeName}, so its text keeps speaking of Flow metrics' callers when it checks " +
                    "Forecasting. Why it matters: a failing Forecasting rule that names Reports as a legal caller sends the reader to fix the " +
                    $"wrong module. How to fix: add a {WordingTypeName} parameter (the module's name in words, then its legal callers in words) " +
                    "with Flow metrics' wording as its default, and build the rule's text from it.");
            }

            if (wordingParameter is not null)
            {
                arguments[wordingParameter.Name!] = ForecastingsWording(Nullable.GetUnderlyingType(wordingParameter.ParameterType) ?? wordingParameter.ParameterType);
            }

            var parameters = rule.GetParameters();
            var notHandedIn = parameters.Where(parameter => !arguments.ContainsKey(parameter.Name!) && !parameter.HasDefaultValue).Select(parameter => parameter.Name!).ToList();

            Assert.That(
                notHandedIn,
                Is.Empty,
                $"The shared rule {ruleName} also needs [{string.Join(", ", notHandedIn)}]. Why it matters: Forecasting's rules hand it only the " +
                "module's namespaces, its list and its wording. How to fix: give any new parameter a default that keeps the rule's verdict.");

            var values = parameters.Select(parameter => arguments.TryGetValue(parameter.Name!, out var value) ? value : parameter.DefaultValue).ToArray();

            return (InboundVerdict)Unwrapped(() => rule.Invoke(null, values))!;
        }

        private static bool TakesAWording(ParameterInfo parameter)
            => (Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType).FullName == WordingTypeName;

        private static object ForecastingsWording(Type wordingType)
        {
            var constructor = wordingType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(candidate => candidate.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual([typeof(string), typeof(string)]));

            Assert.That(
                constructor,
                Is.Not.Null,
                $"{WordingTypeName} cannot be built from two strings. Why it matters: a module hands the rules its name in words and its legal " +
                "callers in words, nothing else. How to fix: give it a constructor taking the module's name in words, then its legal callers in words.");

            return constructor!.Invoke([ForecastingInWords, ForecastingCallersInWords]);
        }

        private static object? Unwrapped(Func<object?> call)
        {
            try
            {
                return call();
            }
            catch (TargetInvocationException invocation) when (invocation.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(invocation.InnerException).Throw();
                throw;
            }
        }

        // The made-up module is always among the types read, so a type inside it that reaches a layer is present and
        // must not count as outside.
        private static string OutsideFixtures(IEnumerable<string> outsideNamespaces)
            => $@"^Lighthouse\.Backend\.(Modules\.ForecastingFixtures\.ModuleBoundaries|ForecastingModuleBoundariesFixtures\.({string.Join('|', outsideNamespaces)}))(\.|$)";

        // Each made-up outbound case lives in a namespace of its own, so the rule handed one sees only that case.
        private static string LayerOf(Type madeUpType, string layer)
        {
            var caseName = madeUpType.Namespace![(MadeUpOutboundCases.Length + 1)..].Split('.')[0];

            return $@"^{Regex.Escape($"{MadeUpOutboundCases}.{caseName}.{layer}")}(\.|$)";
        }

        private static string Edge(Type dependent, string target) => $"{dependent.FullName} -> {target}";

        private static string[] ListedFor(ReachedLayer layer) => layer switch
        {
            ReachedLayer.Domain => [typeof(AListedDomainCaller).FullName!],
            ReachedLayer.Application => [typeof(AListedApplicationCaller).FullName!],
            _ => [],
        };

        private static string[] AllowedFor(ReachedLayer layer) => layer switch
        {
            ReachedLayer.Adapters => [MadeUpCompositionRoot],
            _ => ListedFor(layer),
        };

        private static string NewcomerNamespace(ReachedLayer layer) => $"{layer}Newcomer";

        private static string Newcomer(ReachedLayer layer) => layer switch
        {
            ReachedLayer.Domain => typeof(ANewcomerReachingTheDomain).FullName!,
            ReachedLayer.Adapters => typeof(ANewcomerReachingTheAdapters).FullName!,
            ReachedLayer.Application => typeof(ANewcomerReachingTheApplication).FullName!,
            _ => throw new ArgumentOutOfRangeException(nameof(layer)),
        };

        private static string ReachedBy(ReachedLayer layer) => layer switch
        {
            ReachedLayer.Domain => typeof(AForecastCalculation).FullName!,
            ReachedLayer.Adapters => typeof(AForecastJob).FullName!,
            ReachedLayer.Application => typeof(AForecastAnswerService).FullName!,
            _ => throw new ArgumentOutOfRangeException(nameof(layer)),
        };

        private static void ThenTheFlowMetricsListNamesNoForecastingTypeAndMatches(IReadOnlyList<string> list, InboundVerdict verdict)
        {
            var forecastingEntries = list.Where(entry => Regex.IsMatch(entry, ForecastingModule, RegexOptions.None, PatternTimeout)).ToList();

            Assert.That(
                forecastingEntries,
                Is.Empty,
                $"Flow metrics' inbound list still names Forecasting types: [{string.Join(", ", forecastingEntries)}]. Why it matters: Forecasting " +
                "builds on Flow metrics by design, so its calls are legal by namespace; an entry for one keeps a ratchet on a dependency that is " +
                "allowed and hides the list's real size. How to fix: make the Forecasting module a legal caller of Flow metrics' Application by " +
                "namespace and take its entries off the list.");

            Assert.DoesNotThrow(
                verdict.Check,
                "Flow metrics' inbound list, with Forecasting as a legal caller, no longer matches what reaches Flow metrics' Application from " +
                "outside. Why it matters: the list is compared both ways, so a missing or a stale entry hides a change in who calls Flow metrics. " +
                "How to fix: the failure above names each entry to add or to delete.");
        }

        private static void ThenTheRuleSawExactlyAndPasses(InboundVerdict verdict, ReachedLayer layer, string[] expected)
        {
            Assert.That(
                verdict.Dependents.Select(dependent => dependent.TypeName),
                Is.EquivalentTo(expected),
                $"Expected Forecasting's {layer} rule to find exactly [{string.Join(", ", expected)}] reaching the made-up module's {layer}. Why it " +
                "matters: a rule that misses a caller proves nothing by passing, and one that counts a type inside the module, a legal caller or " +
                "the composition root flags code that is allowed. How to fix: report only outside types that reference a type in that layer.");

            Assert.DoesNotThrow(
                verdict.Check,
                $"Forecasting's {layer} rule failed although only allowed types reached it. Why it matters: a rule that fails on compliant code " +
                "gets switched off. How to fix: compare what was found with the list, nothing more.");
        }

        private static void ThenTheRuleFailsNamingOnlyTheNewcomer(InboundVerdict verdict, string newcomer, string reached)
        {
            var failure = Assert.Throws<AssertionException>(
                verdict.Check,
                $"Forecasting's rule passed with {newcomer} reaching the layer from outside without being on the list. Why it matters: a new " +
                "dependency on Forecasting would go in unseen. How to fix: report every outside type that reaches the layer and is not on the list.");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure!.Message, Does.Contain($"Not on the list, and offending: {newcomer}. "), $"The failure must name {newcomer}, and only it, as not on the list.");
                Assert.That(failure.Message, Does.Contain($"{newcomer} reaches {reached}"), $"The failure must say that {newcomer} reaches {reached}, so the reader knows which dependency to undo.");
                Assert.That(failure.Message, Does.Not.Contain("no longer offending"), "Every listed type still reaches the layer, so the failure must not ask to delete one.");
            }
        }

        private static void ThenTheRuleFailsNamingOnlyTheStaleEntryForDeletion(InboundVerdict verdict, string stale)
        {
            var failure = Assert.Throws<AssertionException>(
                verdict.Check,
                $"Forecasting's rule passed with {stale} allowed although it no longer reaches the layer. Why it matters: a list that keeps a gone " +
                "entry lets that dependency come back unseen. How to fix: compare the list with what was found in both directions.");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure!.Message, Does.Contain($"On the list, but no longer offending: {stale}. Good - delete it"), $"The failure must name {stale}, and only it, with 'delete'.");
                Assert.That(failure.Message, Does.Not.Contain("Not on the list"), "Everything found is allowed, so the failure must not name a newcomer.");
            }
        }

        private static void ThenTheFailureSpeaksOfForecasting(InboundVerdict verdict, ReachedLayer layer)
        {
            var failure = Assert.Throws<AssertionException>(verdict.Check, $"Forecasting's {layer} rule passed with a newcomer reaching the made-up module; there is no failure text to read.");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    failure!.Message,
                    Does.Not.Contain(FlowMetricsInWords),
                    $"Forecasting's failing {layer} rule speaks of {FlowMetricsInWords}. Why it matters: the reader is sent to the wrong module. How to " +
                    "fix: build the rule's text from the wording the module hands in.");

                if (layer == ReachedLayer.Application)
                {
                    Assert.That(
                        failure.Message,
                        Does.Contain(ForecastingCallersInWords).And.Not.Contain(ACallerOnlyFlowMetricsHas),
                        $"Forecasting's failing Application rule must name Forecasting's legal callers, \"{ForecastingCallersInWords}\", and not " +
                        $"{ACallerOnlyFlowMetricsHas}, which may call Flow metrics but not Forecasting. Why it matters: the text tells the reader where " +
                        "a caller may go instead. How to fix: put the legal callers in words from the module's wording into the rule's text.");
                }
            }
        }

        private static void ThenTheFailureSpeaksOfFlowMetrics(InboundVerdict verdict)
        {
            var failure = Assert.Throws<AssertionException>(verdict.Check, "The Application rule passed with an empty list although callers reach the made-up Application; there is no failure text to read.");

            Assert.That(
                failure!.Message,
                Does.Contain(FlowMetricsInWords),
                $"Called without a wording, the shared Application rule must speak of {FlowMetricsInWords}, the module whose wording is the default. " +
                "Why it matters: the Flow metrics rule tests call it without one and keep their meaning only through that default. How to fix: make " +
                "Flow metrics' wording, its name in words and its legal callers in words, the default of the wording parameter.");
        }

        private static void ThenTheOutboundRuleSawExactlyAndPasses(OutboundVerdict verdict, string[] expected)
        {
            Assert.That(
                verdict.Edges,
                Is.EquivalentTo(expected),
                $"Expected the outbound rule to find exactly [{string.Join(", ", expected)}]. Why it matters: a rule that misses an edge proves " +
                "nothing by passing. How to fix: report every edge from the layer into the namespaces it is handed.");

            Assert.DoesNotThrow(verdict.Check, "The outbound rule failed although every edge found is listed. Why it matters: a rule that fails on compliant code gets switched off. How to fix: compare the edges with the list, nothing more.");
        }

        private static void ThenTheOutboundRuleFailsNamingOnlyTheNewEdge(OutboundVerdict verdict, string newEdge)
        {
            var failure = Assert.Throws<AssertionException>(
                verdict.Check,
                $"The outbound rule passed with the edge {newEdge} not on its list. Why it matters: Forecasting would reach a forbidden layer unseen. " +
                "How to fix: report every edge from the layer into the namespaces it is handed that is not on the list.");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure!.Message, Does.Contain($"Not on the list, and offending: {newEdge}. "), $"The failure must name the edge {newEdge}, and only it.");
                Assert.That(failure.Message, Does.Not.Contain("no longer offending"), "Every listed edge is still there, so the failure must not ask to delete one.");
            }
        }

        private static void ThenTheOutboundRuleFailsNamingOnlyTheStaleEdgeForDeletion(OutboundVerdict verdict, string stale)
        {
            var failure = Assert.Throws<AssertionException>(
                verdict.Check,
                $"The outbound rule passed with {stale} listed although the edge is gone. Why it matters: a list that keeps a gone edge lets it come " +
                "back unseen. How to fix: compare the list with the edges found in both directions.");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure!.Message, Does.Contain($"On the list, but no longer offending: {stale}. Good - delete it"), $"The failure must name {stale}, and only it, with 'delete'.");
                Assert.That(failure.Message, Does.Not.Contain("Not on the list"), "Every edge found is listed, so the failure must not name a new one.");
            }
        }
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundaries.Domain
{
    internal static class AForecastCalculation
    {
        public static int Percentile(int days) => days * 85 / 100;
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundaries.Application
{
    internal static class AForecastAnswerService
    {
        public static int Answer(int days) => global::Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundaries.Domain.AForecastCalculation.Percentile(days);
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundaries.Adapters.Background
{
    internal static class AForecastJob
    {
        public static int Run() => global::Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundaries.Application.AForecastAnswerService.Answer(30);
    }
}

namespace Lighthouse.Backend.ForecastingModuleBoundariesFixtures.Api
{
    internal static class AForecastController
    {
        public static int Get() => global::Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundaries.Application.AForecastAnswerService.Answer(30);
    }
}

namespace Lighthouse.Backend.ForecastingModuleBoundariesFixtures.CompositionRoot
{
    internal static class AForecastCompositionRoot
    {
        public static int Register() => global::Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundaries.Adapters.Background.AForecastJob.Run();

        internal static class Registrations
        {
            public static int Register() => global::Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundaries.Application.AForecastAnswerService.Answer(30);
        }
    }
}

namespace Lighthouse.Backend.ForecastingModuleBoundariesFixtures.ListedCallers
{
    internal static class AListedDomainCaller
    {
        public static int Answer() => global::Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundaries.Domain.AForecastCalculation.Percentile(30);
    }

    internal static class AListedApplicationCaller
    {
        public static int Answer() => global::Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundaries.Application.AForecastAnswerService.Answer(30);
    }
}

namespace Lighthouse.Backend.ForecastingModuleBoundariesFixtures.FormerCallers
{
    internal static class AFormerForecastCaller
    {
        public static int Answer() => 30;
    }
}

namespace Lighthouse.Backend.ForecastingModuleBoundariesFixtures.DomainNewcomer
{
    internal static class ANewcomerReachingTheDomain
    {
        public static int Answer() => global::Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundaries.Domain.AForecastCalculation.Percentile(14);
    }
}

namespace Lighthouse.Backend.ForecastingModuleBoundariesFixtures.AdaptersNewcomer
{
    internal static class ANewcomerReachingTheAdapters
    {
        public static int Answer() => global::Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundaries.Adapters.Background.AForecastJob.Run();
    }
}

namespace Lighthouse.Backend.ForecastingModuleBoundariesFixtures.ApplicationNewcomer
{
    internal static class ANewcomerReachingTheApplication
    {
        public static async Task<int> AnswerAsync()
        {
            await Task.Yield();

            return global::Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundaries.Application.AForecastAnswerService.Answer(14);
        }
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundariesOutbound.CompliantApplication.Application
{
    internal sealed class ACompliantForecastService(global::Lighthouse.Backend.Services.Interfaces.Licensing.ILicenseService licence)
    {
        public bool CanForecast(global::Lighthouse.Backend.Models.Feature feature) => licence.CanUsePremiumFeatures() && feature.Id > 0;
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundariesOutbound.HoldingADbSet.Application
{
    internal sealed class AForecastServiceHoldingADbSet(global::Microsoft.EntityFrameworkCore.DbSet<global::Lighthouse.Backend.Models.Feature> features)
    {
        public global::Microsoft.EntityFrameworkCore.DbSet<global::Lighthouse.Backend.Models.Feature> Features { get; } = features;
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundariesOutbound.ReachingAModuleAdapter.Application
{
    internal static class AForecastServiceRunningAModuleAdapter
    {
        public static int Answer() => global::Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundaries.Adapters.Background.AForecastJob.Run();
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundariesOutbound.SecondLicensingType.Application
{
    internal sealed class AForecastServiceReadingTheLicenceTwice(
        global::Lighthouse.Backend.Services.Interfaces.Licensing.ILicenseService licence,
        global::Lighthouse.Backend.Services.Interfaces.Licensing.ILicenseVerifier verifier)
    {
        public bool CanForecast(global::Lighthouse.Backend.Models.LicenseInformation license) => licence.CanUsePremiumFeatures() && verifier.VerifyLicense(license);
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundariesOutbound.CompliantAdapters.Adapters
{
    internal static class ACompliantForecastJob
    {
        public static string KeyName() => nameof(global::Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.UpdateKey) + typeof(global::Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.UpdateKey).Name;
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundariesOutbound.SecondUpdateType.Adapters
{
    internal static class AForecastJobReachingMoreUpdateCode
    {
        public static string Names() =>
            typeof(global::Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.UpdateKey).Name +
            typeof(global::Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.UpdateCancellationContext).Name;
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundariesOutbound.OnAGenericBase.SharedUpdateCode
{
    internal abstract class AnUpdaterBase<TEntity>
        where TEntity : class
    {
        protected static int Weigh(TEntity entity) => entity.GetHashCode() == 0 ? 0 : 1;
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundariesOutbound.OnAGenericBase.Adapters
{
    // Calling a base member whose signature uses the base's type parameter puts that parameter in the IL, where its
    // namespace is the base class's; it is a placeholder, not a dependency.
    internal sealed class AForecastJobOnAGenericBase : global::Lighthouse.Backend.Modules.ForecastingFixtures.ModuleBoundariesOutbound.OnAGenericBase.SharedUpdateCode.AnUpdaterBase<global::Lighthouse.Backend.Models.Feature>
    {
        public static int Run(global::Lighthouse.Backend.Models.Feature feature) => Weigh(feature);
    }
}
