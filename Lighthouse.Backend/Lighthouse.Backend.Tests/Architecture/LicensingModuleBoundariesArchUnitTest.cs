using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using Lighthouse.Backend.LicensingBoundariesFixtures.AccessApplication;
using Lighthouse.Backend.LicensingBoundariesFixtures.Api;
using Lighthouse.Backend.LicensingBoundariesFixtures.ApplicationNewcomer;
using Lighthouse.Backend.LicensingBoundariesFixtures.CompositionRoot;
using Lighthouse.Backend.LicensingBoundariesFixtures.HttpNewcomer;
using Lighthouse.Backend.LicensingBoundariesFixtures.ListedCallers;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.LicensingFixtures.Boundaries.Application;
using Lighthouse.Backend.Modules.LicensingFixtures.Boundaries.Services;
using Lighthouse.Backend.Modules.LicensingFixtures.BoundariesOutbound.AskingTheAppSettings.Application;
using Lighthouse.Backend.Modules.LicensingFixtures.BoundariesOutbound.AskingWorkItems.Application;
using Lighthouse.Backend.Modules.LicensingFixtures.BoundariesOutbound.Compliant.Application;
using Lighthouse.Backend.Modules.LicensingFixtures.BoundariesOutbound.HoldingADbSet.Application;
using Lighthouse.Backend.Modules.LicensingFixtures.BoundariesOutbound.ReachingAccess.Application;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Licensing is a module with rules of its own, checked by the shared module rules with Licensing's namespaces and
    /// Licensing's wording, and read from the compiled production assembly: its types live in Application and
    /// Adapters.Http and none is left in the old Services namespaces; it reaches no Access type and no other context;
    /// outside code reaches its Application only from the API, the composition root, Access's Application or as listed,
    /// and its Http adapters only from the API; its Application reaches the platform only through the listed edges.
    /// Every list is compared both ways. The Work Items and Forecasting edge lists name Access and Licensing types only
    /// under their modules. The same rules run on made-up types: each rule names its made-up offender and passes its
    /// compliant twin.
    /// </summary>
    /// <remarks>
    /// The IL reader does not see types used only in a catch clause, an event or a named attribute argument.
    /// </remarks>
    [TestFixture]
    public class LicensingModuleBoundariesArchUnitTest
    {
        private const string LicensingApplicationNamespace = "Lighthouse.Backend.Modules.Licensing.Application";

        private const string LicensingAdaptersHttpNamespace = "Lighthouse.Backend.Modules.Licensing.Adapters.Http";

        private const string LicensingApplication = @"^Lighthouse\.Backend\.Modules\.Licensing\.Application(\.|$)";

        private const string LicensingAdaptersHttp = @"^Lighthouse\.Backend\.Modules\.Licensing\.Adapters\.Http(\.|$)";

        private const string TheOldLicensingNamespaces = @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.Licensing(\.|$)";

        private const string TheOldAccessAndLicensingNamespaces = @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.(Auth|Authorization|Licensing)(\.|$)";

        private const string AccessApplication = @"^Lighthouse\.Backend\.Modules\.Access\.Application(\.|$)";

        private const string TheCompositionRootAndItsNestedTypes = @"^Lighthouse\.Backend\.Program(\+|$)";

        private const string EntityFrameworkCore = @"^Microsoft\.EntityFrameworkCore(\.|$)";

        private const string AnyModuleAdapters = @"^Lighthouse\.Backend\.Modules\.[^.]+\.Adapters(\.|$)";

        private const string ThePlatform = @"^Lighthouse\.Backend\.(Services|Platform)(\.|$)";

        private const string TheOtherModules = @"^Lighthouse\.Backend\.Modules\.(WorkItems|Forecasting|FlowMetrics|TrackerIntegration|Promises)(\.|$)";

        private const string TheUpdateCode = @"^Lighthouse\.Backend\.Services\.(Implementation\.BackgroundServices\.Update|Interfaces\.Update)(\.|$)"
            + @"|^Lighthouse\.Backend\.Platform\.UpdateQueue(\.|$)";

        private const string RefinementAndReports = @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.Reports(\.|$)|^Lighthouse\.Backend\.Modules\.Refinement(\.|$)";

        private const string WorkItemsEdgeList = "EdgesOfTheApplicationIntoAccessLicensingOrganisationSetUpAndTheUpdateQueue";

        private const string ForecastingEdgeList = "EdgesOfTheApplicationBelowItOrIntoTheLicence";

        private const string MadeUpModule = @"^Lighthouse\.Backend\.Modules\.LicensingFixtures\.Boundaries(\.|$)";

        private const string MadeUpApplicationNamespace = "Lighthouse.Backend.Modules.LicensingFixtures.Boundaries.Application";

        private const string MadeUpApplication = @"^Lighthouse\.Backend\.Modules\.LicensingFixtures\.Boundaries\.Application(\.|$)";

        private const string MadeUpAdaptersHttp = @"^Lighthouse\.Backend\.Modules\.LicensingFixtures\.Boundaries\.Adapters\.Http(\.|$)";

        private const string MadeUpFormerHome = @"^Lighthouse\.Backend\.Modules\.LicensingFixtures\.Boundaries\.Services(\.|$)";

        private const string MadeUpOutside = "Lighthouse.Backend.LicensingBoundariesFixtures";

        private const string AGoneEntry = "Lighthouse.Backend.LicensingBoundariesFixtures.Gone.AFormerLicenceCaller";

        private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

        private static readonly ModuleWording LicensingWording = new("Licensing", "the API and Access");

        // The types each Licensing layer holds, measured from the compiled code.
        private static readonly Dictionary<string, IReadOnlyList<string>> TypesByLayer = new(StringComparer.Ordinal)
        {
            [LicensingApplicationNamespace] = AccessAndLicensingMembership.LicensingApplication,
            [LicensingAdaptersHttpNamespace] = AccessAndLicensingMembership.LicensingAdaptersHttp,
        };

        // The edges from anywhere in Licensing into Access or another context. The list only shrinks.
        private static readonly string[] EdgesOfTheModuleIntoAccessOrAnotherContext =
        [
        ];

        // The outside types, besides the API, the composition root and Access's Application, that ask Licensing's
        // Application. Each of them checks the licence itself. The list only shrinks.
        private static readonly string[] OtherOutsideTypesReachingTheApplication =
        [
            "Lighthouse.Backend.Modules.Forecasting.Adapters.Background.ForecastUpdater",
            "Lighthouse.Backend.Modules.Forecasting.Application.ForecastFilterRuleService",
            "Lighthouse.Backend.Modules.WorkItems.Application.Dependencies.DependencyDecision",
            "Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.PortfolioUpdater",
            "Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.TeamUpdater",
            "Lighthouse.Backend.Platform.UsageData.UsageDataConsentPruningService",
            "Lighthouse.Backend.Services.Implementation.Reports.ReportCommands",
            "Lighthouse.Backend.Services.Implementation.Reports.ReportQueries",
            "Lighthouse.Backend.Platform.UsageData.UsageDataConsentService",
            "Lighthouse.Backend.Platform.UsageData.UsageDataInstanceProperties",
            "Lighthouse.Backend.Services.Implementation.WriteBackTriggerService",
        ];

        // The outside types, besides the API, that reach Licensing's Http adapters. The list only shrinks.
        private static readonly string[] OtherOutsideTypesReachingTheHttpAdapters =
        [
        ];

        // The edges from Licensing's Application into EF Core, the data layer, the API, any module's adapters or the
        // platform: the licence service reads the clock and keeps the licence through the generic repository.
        private static readonly string[] EdgesOfTheApplicationBelowItOrIntoThePlatform =
        [
            "Lighthouse.Backend.Modules.Licensing.Application.LicenseService -> Lighthouse.Backend.Platform.Host.ILighthouseClock",
            "Lighthouse.Backend.Modules.Licensing.Application.LicenseService -> Lighthouse.Backend.Services.Interfaces.Repositories.IRepository`1",
        ];

        // The Access and Licensing entries the neighbours' edge lists hold, under the modules' names.
        private static readonly Dictionary<Neighbour, string[]> AccessAndLicensingEntriesOfTheNeighbours = new()
        {
            [Neighbour.WorkItems] =
            [
                "Lighthouse.Backend.Modules.WorkItems.Application.Dependencies.DependencyDecision -> Lighthouse.Backend.Modules.Licensing.Application.ILicenseService",
                "Lighthouse.Backend.Modules.WorkItems.Application.Dependencies.FeatureDependencyRendering -> Lighthouse.Backend.Modules.Access.Application.IRbacAdministrationService",
            ],
            [Neighbour.Forecasting] =
            [
                "Lighthouse.Backend.Modules.Forecasting.Application.ForecastFilterRuleService -> Lighthouse.Backend.Modules.Licensing.Application.ILicenseService",
            ],
        };

        // The write-back round has no namespace of its own, so its types are named one by one.
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

        private static readonly string AccessAndTheOtherContexts =
            $"(?:{ModuleRules.AccessModule})|(?:{TheOldAccessAndLicensingNamespaces})|(?:{TheOtherModules})|(?:{TheUpdateCode})|(?:{RefinementAndReports})";

        private static readonly string AccessAndLicensing = $"(?:{TheOldAccessAndLicensingNamespaces})|(?:{ModuleRules.AccessModule})|(?:{ModuleRules.LicensingModule})";

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(LicensingModuleBoundariesArchUnitTest).Assembly));

        public enum ListedRule
        {
            ModuleEdgesIntoAccessOrAnotherContext,
            OutsideTypesReachingTheApplication,
            OutsideTypesReachingTheHttpAdapters,
            ApplicationEdgesBelowItOrIntoThePlatform,
        }

        public enum OutboundTarget
        {
            BelowOrPlatform,
            AccessOrAnotherContext,
        }

        public enum Neighbour
        {
            WorkItems,
            Forecasting,
        }

        private static IEnumerable<string> Layers => TypesByLayer.Keys;

        private static string MadeUpCompositionRoot => typeof(ALicensingCompositionRoot).FullName!;

        private static string MadeUpController => typeof(ALicenceController).FullName!;

        private static string MadeUpAccessService => typeof(AnAccessServiceAskingTheLicence).FullName!;

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [TestCaseSource(nameof(Layers))]
        public void EveryLicensingType_LivesInItsLayer_AndNoneIsLeftInTheOldServicesNamespaces(string layer)
        {
            var placement = ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, layer, TypesByLayer[layer], TheOldLicensingNamespaces);

            ThenTheRuleHolds(placement.Check, $"Licensing's layer {layer}");
        }

        [TestCase(ListedRule.ModuleEdgesIntoAccessOrAnotherContext)]
        [TestCase(ListedRule.OutsideTypesReachingTheApplication)]
        [TestCase(ListedRule.OutsideTypesReachingTheHttpAdapters)]
        [TestCase(ListedRule.ApplicationEdgesBelowItOrIntoThePlatform)]
        public void ALicensingListedRule_MatchesWhatTheCompiledCodeReaches_InBothDirections(ListedRule rule)
        {
            var (check, list) = TheProductRule(rule);

            ThenTheRuleHolds(() => check(list), rule.ToString());
        }

        [TestCase(ListedRule.ModuleEdgesIntoAccessOrAnotherContext)]
        [TestCase(ListedRule.OutsideTypesReachingTheApplication)]
        [TestCase(ListedRule.OutsideTypesReachingTheHttpAdapters)]
        [TestCase(ListedRule.ApplicationEdgesBelowItOrIntoThePlatform)]
        public void EveryEntryDeletedFromALicensingList_IsNamedAsOffending_AndAnEntryWithNoEdgeIsNamedForDeletion(ListedRule rule)
        {
            var (check, list) = TheProductRule(rule);

            ModuleBoundaryChecks.ThenEachDeletedEntryIsNamedAndAGoneEntryIsNamedForDeletion(rule.ToString(), check, list, AGoneEntry);
        }

        [Test]
        public void AReaderOfTheLicenceDroppedFromItsList_TheForecastUpdaterFirst_IsNamedAsOffending()
        {
            const string forecastUpdater = "Lighthouse.Backend.Modules.Forecasting.Adapters.Background.ForecastUpdater";
            var (check, list) = TheProductRule(ListedRule.OutsideTypesReachingTheApplication);

            var failure = Assert.Throws<AssertionException>(
                () => check([.. list.Where(listed => listed != forecastUpdater)]),
                $"Licensing's Application rule passed with {forecastUpdater} off its list. Why it matters: the background forecast run checks " +
                "the licence too, and a reader the list misses would go in unseen. How to fix: report every outside type that reaches the " +
                "Application and is not on the list.");

            Assert.That(failure?.Message, Does.Contain($"Not on the list, and offending: {forecastUpdater}"), $"The failure must name {forecastUpdater} as offending.");
        }

        [TestCase(Neighbour.WorkItems)]
        [TestCase(Neighbour.Forecasting)]
        public void TheEdgeListOfANeighbourModule_KeepsItsAccessAndLicensingEntriesUnderTheModules(Neighbour neighbour)
        {
            var list = TheEdgeListOf(neighbour);

            ThenTheListNamesAccessAndLicensingOnlyUnderTheModules(neighbour.ToString(), list);
            ThenTheListKeepsItsAccessAndLicensingEntries(neighbour, list);
        }

        [Test]
        public void ANeighbourListEntryUnderAnOldServicesName_IsNamed_AndItsTwinUnderTheModuleIsNot()
        {
            const string underTheModule = "Lighthouse.Backend.Modules.WorkItems.Application.Dependencies.DependencyDecision -> Lighthouse.Backend.Modules.Licensing.Application.ILicenseService";
            const string underTheOldName = "Lighthouse.Backend.Modules.WorkItems.Application.Dependencies.DependencyDecision -> Lighthouse.Backend.Services.Interfaces.Licensing.ILicenseService";
            const string underAGoneModuleType = "Lighthouse.Backend.Modules.WorkItems.Application.WorkItemService -> Lighthouse.Backend.Modules.Access.Application.IAFormerAccessPort";
            const string notAccessOrLicensing = "Lighthouse.Backend.Modules.WorkItems.Application.WorkItemService -> Lighthouse.Backend.Platform.Host.ILighthouseClock";

            var failure = Assert.Throws<AssertionException>(
                () => ThenTheListNamesAccessAndLicensingOnlyUnderTheModules("a made-up list", [underTheModule, underTheOldName, underAGoneModuleType, notAccessOrLicensing]),
                $"A list naming {underTheOldName} passed the check. Why it matters: an entry under the old name matches no edge the code has, " +
                "so the real edge is unguarded. How to fix: report every Access or Licensing entry not under the modules or not in the code.");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure!.Message, Does.Contain(underTheOldName), $"The failure must name {underTheOldName}.");
                Assert.That(failure.Message, Does.Contain(underAGoneModuleType), $"The failure must name {underAGoneModuleType}, whose type the code does not hold.");
                Assert.That(failure.Message, Does.Not.Contain(underTheModule), $"{underTheModule} names a type under the module that the code holds, so it must not be named.");
                Assert.That(failure.Message, Does.Not.Contain(notAccessOrLicensing), $"{notAccessOrLicensing} is neither Access nor Licensing, so it must not be named.");
                Assert.DoesNotThrow(
                    () => ThenTheListNamesAccessAndLicensingOnlyUnderTheModules("a made-up list", [underTheModule, notAccessOrLicensing]),
                    "A list whose Access and Licensing entries all sit under the modules must pass the check.");
            }
        }

        [Test]
        public void ALicensingTypeLeftInItsFormerHome_IsNamed_AndAWellPlacedTwinIsNot()
        {
            var twin = ModuleRules.TypesLiveInTheirLayer(TestModule.Value, MadeUpApplicationNamespace, [nameof(ALicenceAnswerService)], MadeUpFormerHome);
            var leftBehind = ModuleRules.TypesLiveInTheirLayer(TestModule.Value, MadeUpApplicationNamespace, [nameof(ALeftBehindLicenceAnswer)], MadeUpFormerHome);

            ModuleBoundaryChecks.ThenTheRuleNamesTheOffenderAndPassesItsTwin(leftBehind.Check, typeof(ALeftBehindLicenceAnswer).FullName!, twin.Check, "the Licensing layer rule");
        }

        [Test]
        public void AnOutsideTypeOffTheList_ReachingTheLicensingApplication_IsNamed_AndTheApiAccessTheCompositionRootAndTheListedCallerAreNot()
        {
            var verdict = ApplicationInboundRule(
                TestModule.Value,
                [typeof(AListedLicenceCaller).FullName!],
                MadeUpModule,
                MadeUpApplication,
                $"(?:{OutsideFixture("Api")})|(?:{OutsideFixture("AccessApplication")})",
                ModuleBoundaryChecks.RootPattern(MadeUpCompositionRoot),
                OutsideFixtures("Api", "AccessApplication", "CompositionRoot", "ListedCallers", "ApplicationNewcomer"));

            ModuleBoundaryChecks.ThenTheFailureNamesOnly(
                verdict.Check,
                typeof(ANewcomerAskingTheLicence).FullName!,
                [typeof(AListedLicenceCaller).FullName!, MadeUpController, MadeUpAccessService, MadeUpCompositionRoot],
                "Licensing's Application inbound rule");
        }

        [Test]
        public void AnOutsideTypeReachingTheLicensingHttpAdapters_IsNamed_AndTheApiIsNot()
        {
            var verdict = ModuleRulesAskedByName.HttpAdaptersAreReachedFromOutsideOnlyBy(
                TestModule.Value,
                [],
                MadeUpModule,
                MadeUpAdaptersHttp,
                OutsideFixture("Api"),
                OutsideFixtures("Api", "HttpNewcomer"),
                LicensingWording);

            ModuleBoundaryChecks.ThenTheFailureNamesOnly(verdict.Check, typeof(ANewcomerGuardedByTheLicence).FullName!, [MadeUpController], "Licensing's Http adapter rule");
            ModuleBoundaryChecks.ThenTheFailureSpeaksOf(verdict.Check, "Licensing", "Licensing's Http adapter rule");
        }

        [Test]
        public void ALicensingApplicationReachingOnlyAListedPlatformEdge_PassesBothOutboundRules()
        {
            var listedEdge = ModuleBoundaryChecks.Edge(typeof(ACompliantLicenceService), "Lighthouse.Backend.Platform.Host.ILighthouseClock");
            var namespaces = ModuleBoundaryChecks.NamespacePatternOf(typeof(ACompliantLicenceService));

            var belowOrPlatform = ApplicationOutboundRule(TestModule.Value, namespaces, [listedEdge]);
            var accessOrAnotherContext = ModuleIntoAccessOrAnotherContextRule(TestModule.Value, namespaces, []);

            Assert.That(
                belowOrPlatform.Edges,
                Is.EquivalentTo(new[] { listedEdge }),
                $"Expected the outbound rule to find exactly [{listedEdge}]. Why it matters: a rule that misses an edge proves nothing by passing. " +
                "How to fix: report every edge into the namespaces the rule is handed.");
            ThenTheRuleHolds(belowOrPlatform.Check, "Licensing's Application towards the layers below it and the platform");
            ThenTheRuleHolds(accessOrAnotherContext.Check, "Licensing towards Access and the other contexts");
        }

        [TestCaseSource(nameof(LicensingTypesReachingForbiddenCode))]
        public void ALicensingTypeReachingForbiddenCode_IsNamedWithTheEdge(Type offender, OutboundTarget target, string forbidden)
        {
            var namespaces = ModuleBoundaryChecks.NamespacePatternOf(offender);
            var verdict = target == OutboundTarget.BelowOrPlatform
                ? ApplicationOutboundRule(TestModule.Value, namespaces, [])
                : ModuleIntoAccessOrAnotherContextRule(TestModule.Value, namespaces, []);

            ModuleBoundaryChecks.ThenTheFailureNamesOnly(verdict.Check, $"Not on the list, and offending: {ModuleBoundaryChecks.Edge(offender, forbidden)}", [], $"the {target} rule");
        }

        private static IEnumerable<TestCaseData> LicensingTypesReachingForbiddenCode()
        {
            yield return new TestCaseData(typeof(ALicenceServiceAskingAccess), OutboundTarget.AccessOrAnotherContext, "Lighthouse.Backend.Modules.Access.Application.IRbacAdministrationService")
                .SetName("ALicensingTypeReachingAccess_IsNamed");
            yield return new TestCaseData(typeof(ALicenceServiceAskingWorkItems), OutboundTarget.AccessOrAnotherContext, "Lighthouse.Backend.Modules.WorkItems.Application.IWorkItemService")
                .SetName("ALicensingTypeReachingAnotherContext_IsNamed");
            yield return new TestCaseData(typeof(ALicenceServiceHoldingADbSet), OutboundTarget.BelowOrPlatform, "Microsoft.EntityFrameworkCore.DbSet`1")
                .SetName("ALicensingApplicationTypeHoldingAnEntityFrameworkCoreDbSet_IsNamed");
            yield return new TestCaseData(typeof(ALicenceServiceAskingTheAppSettings), OutboundTarget.BelowOrPlatform, "Lighthouse.Backend.Services.Interfaces.IAppSettingService")
                .SetName("ALicensingApplicationTypeReachingAnUnlistedPlatformType_IsNamed");
        }

        private static (Action<IReadOnlyCollection<string>> Check, string[] List) TheProductRule(ListedRule rule)
        {
            var module = ProductionModule.Value;

            return rule switch
            {
                ListedRule.ModuleEdgesIntoAccessOrAnotherContext => (
                    listed => ModuleIntoAccessOrAnotherContextRule(module, ModuleRules.LicensingModule, listed).Check(),
                    EdgesOfTheModuleIntoAccessOrAnotherContext),
                ListedRule.OutsideTypesReachingTheApplication => (
                    listed => ApplicationInboundRule(module, listed, ModuleRules.LicensingModule, LicensingApplication, $"(?:{ApplicationCoreRules.TheApi})|(?:{AccessApplication})", TheCompositionRootAndItsNestedTypes, ModuleRules.AnyNamespace).Check(),
                    OtherOutsideTypesReachingTheApplication),
                ListedRule.OutsideTypesReachingTheHttpAdapters => (
                    listed => ModuleRulesAskedByName.HttpAdaptersAreReachedFromOutsideOnlyBy(module, listed, ModuleRules.LicensingModule, LicensingAdaptersHttp, ApplicationCoreRules.TheApi, ModuleRules.AnyNamespace, LicensingWording).Check(),
                    OtherOutsideTypesReachingTheHttpAdapters),
                ListedRule.ApplicationEdgesBelowItOrIntoThePlatform => (
                    listed => ApplicationOutboundRule(module, LicensingApplication, listed).Check(),
                    EdgesOfTheApplicationBelowItOrIntoThePlatform),
                _ => throw new ArgumentOutOfRangeException(nameof(rule)),
            };
        }

        private static InboundVerdict ApplicationInboundRule(ModuleDefinition module, IReadOnlyCollection<string> listed, string moduleNamespaces, string application, string legalCallers, string compositionRoot, string outside)
            => ModuleRules.ApplicationIsReachedFromOutsideOnlyBy(module, listed, moduleNamespaces, application, legalCallers, compositionRoot, outside, LicensingWording);

        private static OutboundVerdict ApplicationOutboundRule(ModuleDefinition module, string application, IReadOnlyCollection<string> listed)
            => ModuleRules.OutboundEdges(
                module,
                application,
                TheLayersBelowTheApplicationAndThePlatform,
                listed,
                $"Types in Licensing's Application, namespaces matching \"{application}\", should not depend on any type in namespaces matching " +
                $"\"{TheLayersBelowTheApplicationAndThePlatform}\" except through the listed edges, because Licensing's Application answers whether " +
                "the licence allows a premium feature and keeps the licence only through the listed platform ports. An Application type that holds " +
                "a DbSet or the database context, runs a module's adapter, shapes an API type or reaches a new platform service ties the licence " +
                "check to how the rest of the system stores and shows its data. Read through a listed port and return a Models type.");

        private static OutboundVerdict ModuleIntoAccessOrAnotherContextRule(ModuleDefinition module, string moduleNamespaces, IReadOnlyCollection<string> listed)
            => new(
                IlDependencies.TypesIn(module, moduleNamespaces),
                [.. IlDependencies.DependentsIn(module, moduleNamespaces, IsAccessOrAnotherContext)
                    .SelectMany(dependent => dependent.Targets.Select(reached => $"{dependent.TypeName} -> {reached}"))],
                listed,
                $"Types in namespaces matching \"{moduleNamespaces}\" should not depend on Access or on another context (Work Items, Forecasting, " +
                "Flow metrics, Tracker integration, Promises, the update code, Refinement, Reports or the write-back round) except through the " +
                "listed edges, because Licensing sits below all of them: they ask it whether the licence allows a feature, and Access asks it too. " +
                "A Licensing type that reaches one of them turns that dependency around, so the licence answer would depend on the code that " +
                "asks for it. Hand Licensing what it needs as a value, or let the other context react to a domain event.",
                moduleNamespaces);

        private static bool IsAccessOrAnotherContext(TypeReference type)
            => ModuleBoundaryChecks.IsATypeIn(type, AccessAndTheOtherContexts)
                || (type.DeclaringType is null && TheWriteBackRound.Contains(type.FullName, StringComparer.Ordinal));

        private static string[] TheEdgeListOf(Neighbour neighbour)
        {
            var (owner, fieldName) = neighbour switch
            {
                Neighbour.WorkItems => (typeof(WorkItemsModuleBoundariesArchUnitTest), WorkItemsEdgeList),
                Neighbour.Forecasting => (typeof(ForecastingModuleBoundariesArchUnitTest), ForecastingEdgeList),
                _ => throw new ArgumentOutOfRangeException(nameof(neighbour)),
            };

            var field = owner.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(
                field,
                Is.Not.Null,
                $"{owner.Name} has no list called {fieldName}. Why it matters: that list holds the module's edges into Access and Licensing, and " +
                "this check reads it. How to fix: keep the list under that name.");

            return (string[])field!.GetValue(null)!;
        }

        private static void ThenTheListNamesAccessAndLicensingOnlyUnderTheModules(string owner, IReadOnlyCollection<string> list)
        {
            var offenders = AccessAndLicensingEntriesOf(list)
                .Where(entry => !IsUnderTheModulesAndInTheCode(TargetOf(entry)))
                .ToList();

            Assert.That(
                offenders,
                Is.Empty,
                $"The edge list of {owner} names Access or Licensing types that are not under Modules.Access or Modules.Licensing, or that the " +
                $"code does not hold: [{string.Join(", ", offenders)}]. Why it matters: an entry under an old name matches no edge the code has, " +
                "so the real edge is not on the list and the entry guards nothing. How to fix: write the target under its module's namespace, as " +
                "the compiled code names it.");
        }

        private static void ThenTheListKeepsItsAccessAndLicensingEntries(Neighbour neighbour, IReadOnlyCollection<string> list)
        {
            var expected = AccessAndLicensingEntriesOfTheNeighbours[neighbour];

            Assert.That(
                AccessAndLicensingEntriesOf(list),
                Is.EquivalentTo(expected),
                $"Expected the {neighbour} edge list to keep exactly these Access and Licensing entries: [{string.Join(", ", expected)}]. Why it " +
                "matters: these are the module's real edges into Access and Licensing; one that goes missing or comes in unseen changes who " +
                "depends on the two modules. How to fix: keep the entries under the modules' names, and change the list only with the code.");
        }

        private static IEnumerable<string> AccessAndLicensingEntriesOf(IEnumerable<string> list)
            => list.Where(entry => Regex.IsMatch(TargetOf(entry), AccessAndLicensing, RegexOptions.None, PatternTimeout));

        private static bool IsUnderTheModulesAndInTheCode(string target)
            => (Regex.IsMatch(target, ModuleRules.AccessModule, RegexOptions.None, PatternTimeout) || Regex.IsMatch(target, ModuleRules.LicensingModule, RegexOptions.None, PatternTimeout))
                && ProductionModule.Value.GetType(target) is not null;

        private static string TargetOf(string entry) => entry[(entry.IndexOf(" -> ", StringComparison.Ordinal) + " -> ".Length)..];

        private static string OutsideFixture(string name) => $"^{Regex.Escape(MadeUpOutside)}\\.{name}(\\.|$)";

        private static string OutsideFixtures(params string[] namespaces)
            => $"^{Regex.Escape(MadeUpOutside)}\\.({string.Join('|', namespaces)})(\\.|$)";

        private static void ThenTheRuleHolds(Action check, string what)
            => Assert.DoesNotThrow(
                () => check(),
                $"The rule for {what} failed; the failure above names each type or edge to fix. Why it matters: Licensing keeps its shape only " +
                "while every list matches what the compiled code reaches, in both directions. How to fix: undo the new dependency, or delete " +
                "the entry the failure names as no longer offending.");
    }

    /// <summary>
    /// A shared module rule asked for by its name and parameter names, so a module's rule tests compile before the rule
    /// exists and fail on an assertion that names the missing rule.
    /// </summary>
    internal static class ModuleRulesAskedByName
    {
        private const string HttpAdapterRule = "HttpAdaptersAreReachedFromOutsideOnlyBy";

        public static InboundVerdict HttpAdaptersAreReachedFromOutsideOnlyBy(
            ModuleDefinition module,
            IReadOnlyCollection<string> listed,
            string moduleNamespaces,
            string httpAdapters,
            string legalCallerNamespaces,
            string outside,
            ModuleWording wording)
        {
            var verdict = SharedRule(HttpAdapterRule, new Dictionary<string, object?>
            {
                ["module"] = module,
                ["listed"] = listed,
                ["moduleNamespaces"] = moduleNamespaces,
                ["httpAdapters"] = httpAdapters,
                ["legalCallerNamespaces"] = legalCallerNamespaces,
                ["outside"] = outside,
                ["wording"] = wording,
            });

            Assert.That(
                verdict,
                Is.InstanceOf<InboundVerdict>(),
                $"The shared rule {HttpAdapterRule} returned {verdict?.GetType().FullName ?? "nothing"} instead of an InboundVerdict. Why it " +
                "matters: callers compare the outside types it found with their list through that verdict. How to fix: return an InboundVerdict.");

            return (InboundVerdict)verdict!;
        }

        private static object? SharedRule(string ruleName, IReadOnlyDictionary<string, object?> arguments)
        {
            var rule = typeof(ModuleRules).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .SingleOrDefault(method => method.Name == ruleName);

            Assert.That(
                rule,
                Is.Not.Null,
                $"Expected ModuleRules to hold the rule {ruleName}, found none. Why it matters: a module's Http adapters (the attributes, " +
                "filters and authentication handlers the API applies) need an inbound rule that lets the API reach them; the adapter rule " +
                "for storage and background code allows only the composition root. How to fix: add the rule to ModuleRules, taking " +
                $"[{string.Join(", ", arguments.Keys)}] and returning an InboundVerdict.");

            var parameters = rule!.GetParameters();
            var notTaken = arguments.Keys.Except(parameters.Select(parameter => parameter.Name!), StringComparer.Ordinal).ToList();
            var notHandedIn = parameters.Where(parameter => !arguments.ContainsKey(parameter.Name!) && !parameter.HasDefaultValue).Select(parameter => parameter.Name!).ToList();

            var wrongParameters =
                $"Expected ModuleRules.{ruleName} to take exactly [{string.Join(", ", arguments.Keys)}], but it does not take " +
                $"[{string.Join(", ", notTaken)}] and also needs [{string.Join(", ", notHandedIn)}]. Why it matters: a rule that does not " +
                "take the module's namespaces and legal callers as parameters can check only one module. How to fix: give the rule those parameters.";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(notTaken, Is.Empty, wrongParameters);
                Assert.That(notHandedIn, Is.Empty, wrongParameters);
            }

            var values = parameters
                .Select(parameter => arguments.TryGetValue(parameter.Name!, out var value) ? value : parameter.DefaultValue)
                .ToArray();

            try
            {
                return rule.Invoke(null, values);
            }
            catch (TargetInvocationException invocation) when (invocation.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(invocation.InnerException).Throw();
                throw;
            }
        }
    }

    /// <summary>
    /// The Then steps the Access and Licensing rule tests share: a rule holds, names its made-up offender and nothing
    /// allowed, passes its twin, names a deleted list entry and a gone one, and speaks of its module.
    /// </summary>
    internal static class ModuleBoundaryChecks
    {
        public static void ThenEachDeletedEntryIsNamedAndAGoneEntryIsNamedForDeletion(string rule, Action<IReadOnlyCollection<string>> check, string[] list, string gone)
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

                ThenTheGoneEntryIsNamedForDeletion(() => check([.. list, gone]), gone, rule);
            }
        }

        public static void ThenTheGoneEntryIsNamedForDeletion(Action check, string gone, string what)
        {
            var failure = Assert.Throws<AssertionException>(
                () => check(),
                $"{what} passed with {gone} on its list although nothing like it is in the code. Why it matters: a list that keeps a gone entry " +
                "lets that dependency come back unseen. How to fix: compare the list with what was found in both directions.");

            Assert.That(failure?.Message, Does.Contain($"On the list, but no longer offending: {gone}. Good - delete it"), $"{what} must name {gone} for deletion.");
        }

        public static void ThenTheFailureNamesOnly(Action check, string offender, IReadOnlyCollection<string> notNamed, string what)
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

        public static void ThenTheRuleNamesTheOffenderAndPassesItsTwin(Action offending, string offender, Action twin, string what)
        {
            ThenTheFailureNamesOnly(offending, offender, [], what);

            Assert.DoesNotThrow(() => twin(), $"{what} failed on a compliant twin. Why it matters: a rule that fails on compliant code gets switched off. How to fix: report only what the rule forbids.");
        }

        public static void ThenTheFailureSpeaksOf(Action check, string moduleName, string what)
        {
            var failure = Assert.Throws<AssertionException>(() => check(), $"{what} passed with a newcomer; there is no failure text to read.");

            Assert.That(
                failure!.Message,
                Does.Contain($"{moduleName}'s").And.Not.Contain("Flow metrics"),
                $"A failing {what} must speak of {moduleName}, through the wording it is handed, not of Flow metrics. Why it matters: the reader " +
                "is sent to the wrong module. How to fix: build the rule text from the module's wording.");
        }

        // A generic parameter takes the namespace of the type that declares it; it is a placeholder, not a dependency.
        public static bool IsATypeIn(TypeReference type, string namespacePattern)
            => type is not GenericParameter && IlDependencies.ResidesInNamespaceMatching(type, namespacePattern);

        public static string RootPattern(string compositionRoot) => $"^{Regex.Escape(compositionRoot)}(\\+|$)";

        public static string NamespacePatternOf(Type type) => $"^{Regex.Escape(type.Namespace!)}(\\.|$)";

        public static string Edge(Type dependent, string target) => $"{dependent.FullName} -> {target}";
    }
}

namespace Lighthouse.Backend.Modules.LicensingFixtures.Boundaries.Application
{
    internal static class ALicenceAnswerService
    {
        public static bool AllowsPremium(int seats) => seats > 0;
    }
}

namespace Lighthouse.Backend.Modules.LicensingFixtures.Boundaries.Services
{
    internal sealed record ALeftBehindLicenceAnswer(int Seats);
}

namespace Lighthouse.Backend.Modules.LicensingFixtures.Boundaries.Adapters.Http
{
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class ALicenceGuardAttribute : Attribute;
}

namespace Lighthouse.Backend.LicensingBoundariesFixtures.Api
{
    [global::Lighthouse.Backend.Modules.LicensingFixtures.Boundaries.Adapters.Http.ALicenceGuard]
    internal static class ALicenceController
    {
        public static bool Get(int seats) => global::Lighthouse.Backend.Modules.LicensingFixtures.Boundaries.Application.ALicenceAnswerService.AllowsPremium(seats);
    }
}

namespace Lighthouse.Backend.LicensingBoundariesFixtures.AccessApplication
{
    internal static class AnAccessServiceAskingTheLicence
    {
        public static bool MayManage(int seats) => global::Lighthouse.Backend.Modules.LicensingFixtures.Boundaries.Application.ALicenceAnswerService.AllowsPremium(seats);
    }
}

namespace Lighthouse.Backend.LicensingBoundariesFixtures.CompositionRoot
{
    internal static class ALicensingCompositionRoot
    {
        public static bool Register(int seats) => global::Lighthouse.Backend.Modules.LicensingFixtures.Boundaries.Application.ALicenceAnswerService.AllowsPremium(seats);
    }
}

namespace Lighthouse.Backend.LicensingBoundariesFixtures.ListedCallers
{
    internal static class AListedLicenceCaller
    {
        public static bool Ask(int seats) => global::Lighthouse.Backend.Modules.LicensingFixtures.Boundaries.Application.ALicenceAnswerService.AllowsPremium(seats);
    }
}

namespace Lighthouse.Backend.LicensingBoundariesFixtures.ApplicationNewcomer
{
    internal static class ANewcomerAskingTheLicence
    {
        public static bool Ask(int seats) => global::Lighthouse.Backend.Modules.LicensingFixtures.Boundaries.Application.ALicenceAnswerService.AllowsPremium(seats + 1);
    }
}

namespace Lighthouse.Backend.LicensingBoundariesFixtures.HttpNewcomer
{
    [global::Lighthouse.Backend.Modules.LicensingFixtures.Boundaries.Adapters.Http.ALicenceGuard]
    internal sealed record ANewcomerGuardedByTheLicence(string Name);
}

namespace Lighthouse.Backend.Modules.LicensingFixtures.BoundariesOutbound.Compliant.Application
{
    internal sealed class ACompliantLicenceService(global::Lighthouse.Backend.Platform.Host.ILighthouseClock clock)
    {
        public int Describe() => clock.GetHashCode();
    }
}

namespace Lighthouse.Backend.Modules.LicensingFixtures.BoundariesOutbound.ReachingAccess.Application
{
    internal sealed class ALicenceServiceAskingAccess(global::Lighthouse.Backend.Modules.Access.Application.IRbacAdministrationService rbac)
    {
        public int Describe() => rbac.GetHashCode();
    }
}

namespace Lighthouse.Backend.Modules.LicensingFixtures.BoundariesOutbound.AskingWorkItems.Application
{
    internal sealed class ALicenceServiceAskingWorkItems(global::Lighthouse.Backend.Modules.WorkItems.Application.IWorkItemService workItems)
    {
        public int Describe() => workItems.GetHashCode();
    }
}

namespace Lighthouse.Backend.Modules.LicensingFixtures.BoundariesOutbound.HoldingADbSet.Application
{
    internal sealed class ALicenceServiceHoldingADbSet(global::Microsoft.EntityFrameworkCore.DbSet<global::Lighthouse.Backend.Models.LicenseInformation> licences)
    {
        public global::Microsoft.EntityFrameworkCore.DbSet<global::Lighthouse.Backend.Models.LicenseInformation> Licences { get; } = licences;
    }
}

namespace Lighthouse.Backend.Modules.LicensingFixtures.BoundariesOutbound.AskingTheAppSettings.Application
{
    internal sealed class ALicenceServiceAskingTheAppSettings(global::Lighthouse.Backend.Services.Interfaces.IAppSettingService settings)
    {
        public int Describe() => settings.GetHashCode();
    }
}
