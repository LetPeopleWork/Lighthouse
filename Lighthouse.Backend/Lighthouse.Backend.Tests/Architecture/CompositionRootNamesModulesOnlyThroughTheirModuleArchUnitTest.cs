using System.Text.RegularExpressions;
using Lighthouse.Backend.Data;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Flow metrics, Forecasting, Promises, Licensing and Access each start themselves up: each has a static module class at
    /// its root with one Add method for the service collection, only the composition root calls it, and the composition root
    /// itself registers no type of these contexts but the domain-event handlers it keeps in one ordered block (the dispatcher
    /// runs them in registration order across contexts), and looks up none but what that block forwards to and the two services
    /// it needs to set the host up. Registrations and lookups are compared with their own lists, both ways, so a service the
    /// host looks up may still not be registered there. The same checks run over made-up contexts and composition roots, one
    /// made-up type per way a check can be broken, so a check that names nothing cannot pass.
    /// </summary>
    [TestFixture]
    public class CompositionRootNamesModulesOnlyThroughTheirModuleArchUnitTest
    {
        private const string TheCompositionRoot = "Lighthouse.Backend.Program";

        private const string AnyNamespace = "^";

        private const string Registered = "registered";

        private const string LookedUp = "looked up";

        private const string TheServiceCollection = "Microsoft.Extensions.DependencyInjection.IServiceCollection";

        private const string TheExtensionAttribute = "System.Runtime.CompilerServices.ExtensionAttribute";

        private const string TheMadeUpRoot = "Lighthouse.Backend.Tests.Architecture.CompositionRootFixtures";

        private const string TheMadeUpModulesRoot = TheMadeUpRoot + ".Modules";

        private const string TheMadeUpAlpha = TheMadeUpModulesRoot + ".Alpha.Application";

        private const string TheMadeUpTypes = @"^Lighthouse\.Backend\.Tests\.Architecture\.CompositionRootFixtures(\.|$)";

        private const string ARootRegisteringAlphasTypesItself = TheMadeUpRoot + ".Roots.ARootRegisteringAlphasTypesItself";

        private const string ARootLookingUpAlphasTypes = TheMadeUpRoot + ".Roots.ARootLookingUpAlphasTypes";

        private const string ARootRegisteringAHostSetUpTypeItself = TheMadeUpRoot + ".Roots.ARootRegisteringAHostSetUpTypeItself";

        private const string ARootOnlyLookingUpAHostSetUpType = TheMadeUpRoot + ".Roots.ARootOnlyLookingUpAHostSetUpType";

        private const string ARootStartingAlphaThroughItsModule = TheMadeUpRoot + ".Roots.ARootStartingAlphaThroughItsModule";

        private const string ARootKeepingCrossContextWiring = TheMadeUpRoot + ".Roots.ARootKeepingCrossContextWiring";

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(LighthouseAppContext).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(CompositionRootNamesModulesOnlyThroughTheirModuleArchUnitTest).Assembly));

        private static string TheModulesRoot => ContextContractsMembership.TheModulesRoot;

        private static IReadOnlyList<string> TheFiveContexts => ["Access", "FlowMetrics", "Forecasting", "Licensing", "Promises"];

        // The dispatcher runs the handlers of an event in the order the composition root registers them, across contexts, so
        // they stay in the composition root's one ordered block rather than moving into each context's module.
        private static Dictionary<string, IReadOnlyList<string>> RegisteredByTheHandlerBlock => new(StringComparer.Ordinal)
        {
            ["Access"] = [],
            ["FlowMetrics"] =
            [
                $"{TheModulesRoot}.FlowMetrics.Application.Policies.BlackoutConfigurationChangedMetricsInvalidationHandler",
                $"{TheModulesRoot}.FlowMetrics.Application.Policies.BlockedCountSnapshotRecordingHandler",
                $"{TheModulesRoot}.FlowMetrics.Application.Policies.DemoBlockedHistoryBackfillHandler",
                $"{TheModulesRoot}.FlowMetrics.Application.Policies.DemoPercentilesBackfillHandler",
                $"{TheModulesRoot}.FlowMetrics.Application.Policies.PercentilesOverTimeRecordingHandler",
                $"{TheModulesRoot}.FlowMetrics.Application.Policies.PortfolioFeaturesRefreshedMetricsInvalidationHandler",
                $"{TheModulesRoot}.FlowMetrics.Application.Policies.ProcessBehaviorRecordingHandler",
                $"{TheModulesRoot}.FlowMetrics.Application.Policies.TeamDataRefreshedTeamMetricsUpdateHandler",
                $"{TheModulesRoot}.FlowMetrics.Application.Policies.WithinSleRecordingHandler",
                $"{TheModulesRoot}.FlowMetrics.Application.ReconstructionMemo",
            ],
            ["Forecasting"] =
            [
                $"{TheModulesRoot}.Forecasting.Application.Policies.FeatureOrderingPolicyChangedForecastTriggerHandler",
                $"{TheModulesRoot}.Forecasting.Application.Policies.FeatureRankChangedForecastTriggerHandler",
                $"{TheModulesRoot}.Forecasting.Application.Policies.TeamDataRefreshedForecastTriggerHandler",
                $"{TheModulesRoot}.Forecasting.Application.Policies.TeamDeletedForecastRetriggerHandler",
                $"{TheModulesRoot}.Forecasting.Contracts.PortfolioForecastsUpdated",
            ],
            ["Licensing"] = [],
            ["Promises"] =
            [
                $"{TheModulesRoot}.Promises.Application.Policies.DeliveryForecastPublishingHandler",
                $"{TheModulesRoot}.Promises.Application.Policies.DeliveryMetricSnapshotRecordingHandler",
            ],
        };

        // The memo stands as a handler of the two refresh events by forwarding to its one instance; the forecast thread count
        // is read for the start-up banner; the group writer is called by the sign-in event of the identity provider.
        private static Dictionary<string, IReadOnlyList<string>> LookedUpByTheHandlerBlockAndToSetTheHostUp => new(StringComparer.Ordinal)
        {
            ["Access"] = [$"{TheModulesRoot}.Access.Application.IOidcGroupSnapshotWriter"],
            ["FlowMetrics"] = [$"{TheModulesRoot}.FlowMetrics.Application.ReconstructionMemo"],
            ["Forecasting"] = [$"{TheModulesRoot}.Forecasting.Application.ForecastLane"],
            ["Licensing"] = [],
            ["Promises"] = [],
        };

        // A statement that builds one context's service from another context's type, or registers it as the other context's
        // service, names two contexts; no context's own module may name another context, so the statement stays here.
        private static Dictionary<string, IReadOnlyList<string>> RegisteredByCrossContextWiring => new(StringComparer.Ordinal)
        {
            ["Access"] = [],
            ["FlowMetrics"] =
            [
                $"{TheModulesRoot}.FlowMetrics.Contracts.IForecastDataSufficiency",
                $"{TheModulesRoot}.FlowMetrics.Contracts.IHowManyForecaster",
                $"{TheModulesRoot}.FlowMetrics.Contracts.IThroughputFilter",
            ],
            ["Forecasting"] = [],
            ["Licensing"] = [],
            ["Promises"] = [$"{TheModulesRoot}.Promises.Application.DeliveryForecastBlockRenderer"],
        };

        private static Dictionary<string, IReadOnlyList<string>> LookedUpByCrossContextWiring => new(StringComparer.Ordinal)
        {
            ["Access"] = [],
            ["FlowMetrics"] = [],
            ["Forecasting"] =
            [
                $"{TheModulesRoot}.Forecasting.Application.ForecastFilterRuleService",
                $"{TheModulesRoot}.Forecasting.Application.ForecastingAnswersToFlowMetrics",
            ],
            ["Licensing"] = [],
            ["Promises"] = [],
        };

        private static IEnumerable<TestCaseData> EveryAllowedEntry
            => TheFiveContexts.SelectMany(context =>
                MayRegister(context).Select(entry => new TestCaseData(context, Registered, entry))
                    .Concat(MayLookUp(context).Select(entry => new TestCaseData(context, LookedUp, entry))));

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [TestCaseSource(nameof(TheFiveContexts))]
        public void EachOfTheFiveContexts_RegistersThroughTheAddMethodOfItsOwnModule(string context)
        {
            var problem = WhenTheStartUpModuleIsLookedFor(ProductionModule.Value, TheModulesRoot, context);

            ThenTheModuleIsThere(problem, TheModulesRoot, context);
        }

        [TestCaseSource(nameof(TheFiveContexts))]
        public void EachOfTheFiveContextsModules_IsStartedByTheCompositionRootAlone(string context)
        {
            var namers = WhenTheTypesNamingTheModuleAreRead(ProductionModule.Value, TheModulesRoot, context, AnyNamespace);

            ThenOnlyTheCompositionRootNamesTheModule(namers, TheCompositionRoot, TheModulesRoot, context);
        }

        [TestCaseSource(nameof(TheFiveContexts))]
        public void TheCompositionRoot_RegistersNoTypeOfTheFiveContexts_ButTheirDomainEventHandlersAndWhatItLooksUpToSetTheHostUp(string context)
        {
            var verdict = WhenTheCompositionRootIsComparedWithTheLists(ProductionModule.Value, TheCompositionRoot, TheModulesRoot, context, MayRegister(context), MayLookUp(context));

            ThenTheCompositionRootRegistersAndLooksUpOnlyTheListed(verdict, TheCompositionRoot, context);
        }

        [TestCaseSource(nameof(EveryAllowedEntry))]
        public void AnAllowedEntryOfTheCompositionRoot_IsNamedOnceItIsTakenOffItsList(string context, string list, string entry)
        {
            var registeredList = MayRegister(context);
            var lookedUpList = MayLookUp(context);
            var full = WhenTheCompositionRootIsComparedWithTheLists(ProductionModule.Value, TheCompositionRoot, TheModulesRoot, context, registeredList, lookedUpList);

            var withoutTheEntry = WhenTheCompositionRootIsComparedWithTheLists(
                ProductionModule.Value, TheCompositionRoot, TheModulesRoot, context,
                list == Registered ? [.. registeredList.Where(allowed => allowed != entry)] : registeredList,
                list == LookedUp ? [.. lookedUpList.Where(allowed => allowed != entry)] : lookedUpList);

            ThenOnlyTheEntryTakenOffIsAddedToWhatIsNamed(entry, list, full, withoutTheEntry);
        }

        [Test]
        public void AMadeUpCompositionRootRegisteringAContextsTypes_IsReportedNamingEachTypeForEveryWayOfRegistering_ButNotATypeOfAContextWhoseNameBeginsAlike()
        {
            var verdict = WhenTheCompositionRootIsComparedWithTheLists(TestModule.Value, ARootRegisteringAlphasTypesItself, TheMadeUpModulesRoot, "Alpha", [], []);

            ThenEveryWayOfRegisteringIsNamed(verdict);
        }

        [Test]
        public void AMadeUpCompositionRootLookingUpAContextsTypes_IsReportedNamingEachTypeForEveryWayOfLookingUp()
        {
            var verdict = WhenTheCompositionRootIsComparedWithTheLists(TestModule.Value, ARootLookingUpAlphasTypes, TheMadeUpModulesRoot, "Alpha", [], []);

            ThenEveryWayOfLookingUpIsNamed(verdict);
        }

        [Test]
        public void AMadeUpCompositionRootRegisteringAServiceListedOnlyAsLookedUp_IsReported()
        {
            var verdict = WhenTheCompositionRootIsComparedWithTheLists(TestModule.Value, ARootRegisteringAHostSetUpTypeItself, TheMadeUpModulesRoot, "Alpha", [], [$"{TheMadeUpAlpha}.AlphaBanner"]);

            ThenOnlyTheRegistrationOfTheLookedUpServiceIsNamed(verdict);
        }

        [Test]
        public void AMadeUpCompositionRootOnlyLookingUpAServiceListedAsLookedUp_IsNotReported()
        {
            var verdict = WhenTheCompositionRootIsComparedWithTheLists(TestModule.Value, ARootOnlyLookingUpAHostSetUpType, TheMadeUpModulesRoot, "Alpha", [], [$"{TheMadeUpAlpha}.AlphaBanner"]);

            ThenNothingIsNamed(verdict, "a made-up composition root that only looks up the one service its lookup list names");
        }

        [Test]
        public void AMadeUpCrossContextWiringLine_PassesWhileListed_AndIsNamedOnceItIsTakenOffTheList()
        {
            var crossContextRegistration = $"{TheMadeUpAlpha}.IAlphaService";

            var listed = WhenTheCompositionRootIsComparedWithTheLists(TestModule.Value, ARootKeepingCrossContextWiring, TheMadeUpModulesRoot, "Alpha", [crossContextRegistration], []);
            var takenOff = WhenTheCompositionRootIsComparedWithTheLists(TestModule.Value, ARootKeepingCrossContextWiring, TheMadeUpModulesRoot, "Alpha", [], []);

            ThenTheCrossContextLineIsNamedOnlyOnceTakenOff(listed, takenOff, crossContextRegistration);
        }

        [Test]
        public void AnAllowedEntryAMadeUpCompositionRootDoesNotRegisterOrLookUp_IsReportedAsGone()
        {
            var goneRegistration = $"{TheMadeUpAlpha}.AGoneHandler";
            var goneLookup = $"{TheMadeUpAlpha}.AGoneBanner";

            var verdict = WhenTheCompositionRootIsComparedWithTheLists(TestModule.Value, ARootStartingAlphaThroughItsModule, TheMadeUpModulesRoot, "Alpha", [goneRegistration], [goneLookup]);

            ThenOnlyTheGoneEntriesAreNamed(verdict, goneRegistration, goneLookup);
        }

        [Test]
        public void AMadeUpCompositionRootStartingAContextOnlyThroughItsModule_IsNotReported()
        {
            var problem = WhenTheStartUpModuleIsLookedFor(TestModule.Value, TheMadeUpModulesRoot, "Alpha");
            var namers = WhenTheTypesNamingTheModuleAreRead(TestModule.Value, TheMadeUpModulesRoot, "Alpha", TheMadeUpTypes);
            var verdict = WhenTheCompositionRootIsComparedWithTheLists(TestModule.Value, ARootStartingAlphaThroughItsModule, TheMadeUpModulesRoot, "Alpha", [], []);

            ThenTheCompliantRootIsNotReported(problem, namers, verdict);
        }

        [Test]
        public void AMadeUpModuleOrOtherTypeStartingAContextsModule_IsReportedBesideTheCompositionRoot()
        {
            var namers = WhenTheTypesNamingTheModuleAreRead(TestModule.Value, TheMadeUpModulesRoot, "Alpha", TheMadeUpTypes);

            ThenTheOutsidersAreNamed(namers);
        }

        [TestCase("Gamma", "no type")]
        [TestCase("Delta", "no public static AddDelta")]
        [TestCase("Epsilon", "not a public static class")]
        public void AMadeUpContextWithoutAWellFormedStartUpModule_IsReported(string context, string expectedProblem)
        {
            var problem = WhenTheStartUpModuleIsLookedFor(TestModule.Value, TheMadeUpModulesRoot, context);

            ThenTheModuleProblemIsNamed(problem, context, expectedProblem);
        }

        private static IReadOnlyList<string> MayRegister(string context) => [.. RegisteredByTheHandlerBlock[context], .. RegisteredByCrossContextWiring[context]];

        private static IReadOnlyList<string> MayLookUp(string context) => [.. LookedUpByTheHandlerBlockAndToSetTheHostUp[context], .. LookedUpByCrossContextWiring[context]];

        private static string ContextPattern(string modulesRoot, string context) => $"^{Regex.Escape(modulesRoot)}\\.{context}(\\.|$)";

        private static string ModuleTypeOf(string modulesRoot, string context) => $"{modulesRoot}.{context}.{context}Module";

        private static string Listed(IEnumerable<string> names) => $"[{string.Join(" | ", names)}]";

        private static string? WhenTheStartUpModuleIsLookedFor(ModuleDefinition module, string modulesRoot, string context)
        {
            Assert.That(IlDependencies.TypesIn(module, ContextPattern(modulesRoot, context)), Is.Not.Empty,
                $"No type lives in {modulesRoot}.{context}, so the check looked for the start-up module of a context the code does not hold. " +
                "Why it matters: a check of a context that is not there passes while it guards nothing. How to fix: name a context the compiled code holds.");

            var name = ModuleTypeOf(modulesRoot, context);
            var type = module.Types.SingleOrDefault(candidate => candidate.FullName == name);
            if (type is null)
            {
                return $"no type {name}";
            }

            if (!type.IsPublic || !type.IsAbstract || !type.IsSealed)
            {
                return $"{name} is not a public static class";
            }

            var addMethod = $"Add{context}";
            var isAnExtensionOfTheServiceCollection = type.Methods.Any(method =>
                method.Name == addMethod
                && method.IsPublic
                && method.IsStatic
                && method.Parameters.Count > 0
                && method.Parameters[0].ParameterType.FullName == TheServiceCollection
                && method.CustomAttributes.Any(attribute => attribute.AttributeType.FullName == TheExtensionAttribute));

            return isAnExtensionOfTheServiceCollection ? null : $"{name} has no public static {addMethod} extending the service collection";
        }

        private static List<string> WhenTheTypesNamingTheModuleAreRead(ModuleDefinition module, string modulesRoot, string context, string outside)
        {
            var moduleType = ModuleTypeOf(modulesRoot, context);

            return [.. IlDependencies.DependentsIn(module, outside, type => IlDependencies.DeclaringTypeOf(type.FullName.Replace('/', '+')).Split('+')[0] == moduleType)
                .Select(dependent => dependent.TypeName.Split('+')[0])
                .Where(dependent => dependent != moduleType)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)];
        }

        private static CompositionRootVerdict WhenTheCompositionRootIsComparedWithTheLists(ModuleDefinition module, string compositionRoot, string modulesRoot, string context, IReadOnlyCollection<string> mayRegister, IReadOnlyCollection<string> mayLookUp)
        {
            var use = CompositionRootRegistrations.Of(module, compositionRoot, ContextPattern(modulesRoot, context));

            return new CompositionRootVerdict(
                [.. use.Registered.Except(mayRegister, StringComparer.Ordinal).Order(StringComparer.Ordinal)],
                [.. mayRegister.Except(use.Registered, StringComparer.Ordinal).Order(StringComparer.Ordinal)],
                [.. use.LookedUp.Except(mayLookUp, StringComparer.Ordinal).Order(StringComparer.Ordinal)],
                [.. mayLookUp.Except(use.LookedUp, StringComparer.Ordinal).Order(StringComparer.Ordinal)]);
        }

        private static void ThenTheModuleIsThere(string? problem, string modulesRoot, string context)
        {
            Assert.That(problem, Is.Null,
                $"The {context} context has no start-up module of its own: {problem}. " +
                "Why it matters: until the context registers its own services, the composition root holds them, and a registration of the context can be dropped, doubled or reordered among hundreds of others without anyone owning it. " +
                $"How to fix: add public static class {ModuleTypeOf(modulesRoot, context)} with a public static Add{context}(this IServiceCollection services, ...) holding the context's registration lines, moved unchanged and in their order, and call it from the composition root.");
        }

        private static void ThenOnlyTheCompositionRootNamesTheModule(List<string> namers, string compositionRoot, string modulesRoot, string context)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(namers, Does.Contain(compositionRoot),
                    $"The composition root {compositionRoot} does not call {ModuleTypeOf(modulesRoot, context)}; the types naming it are {Listed(namers)}. " +
                    "Why it matters: a module nobody starts leaves its context's services out of the container, and the application fails when it first asks for one. " +
                    $"How to fix: call Add{context} from the composition root's registration method, at the position that keeps the order of every service registered more than once.");
                Assert.That(namers.Where(namer => namer != compositionRoot), Is.Empty,
                    $"These types name {ModuleTypeOf(modulesRoot, context)} besides the composition root: {Listed(namers.Where(namer => namer != compositionRoot))}. " +
                    "Why it matters: a second caller registers the context twice or out of order, and a module starting another module hides one context's start-up inside another's. " +
                    $"How to fix: call Add{context} from the composition root only.");
            }
        }

        private static void ThenTheCompositionRootRegistersAndLooksUpOnlyTheListed(CompositionRootVerdict verdict, string compositionRoot, string context)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.UnlistedRegistrations, Is.Empty,
                    $"The composition root {compositionRoot} still registers these types of the {context} context: {Listed(verdict.UnlistedRegistrations)}. " +
                    "Why it matters: the context's start-up is meant to live in its own module, so a registration left in the composition root is one its owner cannot see. " +
                    $"How to fix: move the registration lines into Add{context} unchanged and in their order; only a domain-event handler of the ordered block stays, and is listed here.");
                Assert.That(verdict.UnlistedLookups, Is.Empty,
                    $"The composition root {compositionRoot} looks up these types of the {context} context: {Listed(verdict.UnlistedLookups)}. " +
                    "Why it matters: a lookup in the composition root is a dependency on the context's inside that its module does not own. " +
                    $"How to fix: move the line that looks it up into Add{context}; only a forward of the handler block or a service the host needs to set itself up stays, and is listed here.");
                Assert.That(verdict.GoneRegistrations.Concat(verdict.GoneLookups), Is.Empty,
                    $"These listed types of the {context} context are no longer registered (registrations {Listed(verdict.GoneRegistrations)}) or looked up (lookups {Listed(verdict.GoneLookups)}) by the composition root. " +
                    "Why it matters: a list entry the code no longer needs lets the same line come back unseen. How to fix: delete the entries from their list.");
            }
        }

        private static void ThenOnlyTheEntryTakenOffIsAddedToWhatIsNamed(string entry, string list, CompositionRootVerdict full, CompositionRootVerdict withoutTheEntry)
        {
            var expected = list == Registered
                ? full with { UnlistedRegistrations = [.. full.UnlistedRegistrations.Append(entry).Order(StringComparer.Ordinal)] }
                : full with { UnlistedLookups = [.. full.UnlistedLookups.Append(entry).Order(StringComparer.Ordinal)] };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(withoutTheEntry.UnlistedRegistrations, Is.EqualTo(expected.UnlistedRegistrations),
                    $"With {entry} taken off the {list} list, the check named {Listed(withoutTheEntry.UnlistedRegistrations)} as registered and not listed. Expected {Listed(expected.UnlistedRegistrations)}. " +
                    "Why it matters: an entry the composition root does not need, or one the check never compares, lets the same line come back unseen once it is deleted. " +
                    "How to fix: keep on each list only what the composition root registers, or looks up, and compare each list with the code both ways.");
                Assert.That(withoutTheEntry.UnlistedLookups, Is.EqualTo(expected.UnlistedLookups),
                    $"With {entry} taken off the {list} list, the check named {Listed(withoutTheEntry.UnlistedLookups)} as looked up and not listed. Expected {Listed(expected.UnlistedLookups)}. " +
                    "Why it matters: the two lists are separate, so taking an entry off one must change only what that list excuses. How to fix: compare registrations and lookups each with its own list.");
                Assert.That(withoutTheEntry.GoneRegistrations.Concat(withoutTheEntry.GoneLookups), Is.EqualTo(full.GoneRegistrations.Concat(full.GoneLookups)),
                    $"With {entry} taken off the {list} list, what the check names as gone changed. Why it matters: taking one entry off must change nothing else. How to fix: compare each entry on its own.");
            }
        }

        private static void ThenEveryWayOfRegisteringIsNamed(CompositionRootVerdict verdict)
        {
            string[] expectedRegistrations =
            [
                $"{TheMadeUpAlpha}.AlphaByType",
                $"{TheMadeUpAlpha}.AlphaClient",
                $"{TheMadeUpAlpha}.AlphaClock",
                $"{TheMadeUpAlpha}.AlphaDescribed",
                $"{TheMadeUpAlpha}.AlphaDescribedByType",
                $"{TheMadeUpAlpha}.AlphaKeyed",
                $"{TheMadeUpAlpha}.AlphaMessageHandler",
                $"{TheMadeUpAlpha}.AlphaOptions",
                $"{TheMadeUpAlpha}.AlphaRecord",
                $"{TheMadeUpAlpha}.AlphaRecordReader",
                $"{TheMadeUpAlpha}.AlphaService",
                $"{TheMadeUpAlpha}.AlphaTypedClient",
                $"{TheMadeUpAlpha}.AlphaWorker",
                $"{TheMadeUpAlpha}.IAlphaReader`1",
                $"{TheMadeUpAlpha}.IAlphaService",
            ];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.UnlistedRegistrations, Is.EqualTo(expectedRegistrations.Order(StringComparer.Ordinal)),
                    $"The check named {Listed(verdict.UnlistedRegistrations)} as registered by the made-up composition root. Expected exactly the Alpha types it registers: " +
                    "with Add, TryAdd and Configure, as a hosted service, as a typed HTTP client with its message handler, as a typed client of a named HTTP client, as a keyed service, " +
                    "with typeof through a non-generic overload, through a generic service descriptor and through a descriptor built with typeof, and the types a registered generic type is built from, each by its full name; " +
                    "and not the type of AlphaPlus, a context whose name begins like Alpha's, nor a type outside the contexts. " +
                    "Why it matters: a way of registering the check does not read lets a registration stay in the composition root unseen, and a check that confuses two contexts reports the wrong owner. " +
                    "How to fix: read every registration form of the service collection, the HTTP client builder and the service descriptor, and every typeof, in the composition root and its nested types, and match the context by its whole name.");
                Assert.That(verdict.UnlistedLookups, Is.EqualTo(new[] { $"{TheMadeUpAlpha}.AlphaJob" }),
                    $"The check named {Listed(verdict.UnlistedLookups)} as looked up by the made-up composition root. Expected exactly the type a registration's factory looks up inside its lambda, and none of the registered types. " +
                    "Why it matters: a lookup counted as a registration, or the other way round, lets one list excuse what the other must name. How to fix: keep registrations and lookups apart.");
                Assert.That(verdict.GoneRegistrations.Concat(verdict.GoneLookups), Is.Empty, "The check named entries as gone, from empty lists.");
            }
        }

        private static void ThenEveryWayOfLookingUpIsNamed(CompositionRootVerdict verdict)
        {
            string[] expectedLookups =
            [
                $"{TheMadeUpAlpha}.AlphaClock",
                $"{TheMadeUpAlpha}.AlphaKeyed",
                $"{TheMadeUpAlpha}.AlphaService",
                $"{TheMadeUpAlpha}.IAlphaService",
            ];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.UnlistedLookups, Is.EqualTo(expectedLookups),
                    $"The check named {Listed(verdict.UnlistedLookups)} as looked up by the made-up composition root. Expected exactly the Alpha types it asks for with GetServices, a required keyed lookup, " +
                    "a keyed lookup and the activator utilities, and not the type of AlphaPlus it looks up with GetService. " +
                    "Why it matters: a way of looking up the check does not read lets the composition root reach into a context unseen. " +
                    "How to fix: read GetService, GetRequiredService, GetServices, their keyed forms and the activator utilities.");
                Assert.That(verdict.UnlistedRegistrations, Is.Empty,
                    $"The check named {Listed(verdict.UnlistedRegistrations)} as registered by a made-up composition root that only looks types up. " +
                    "Why it matters: a lookup counted as a registration makes the host set-up lookups impossible to tell apart from registrations. How to fix: keep registrations and lookups apart.");
            }
        }

        private static void ThenOnlyTheRegistrationOfTheLookedUpServiceIsNamed(CompositionRootVerdict verdict)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.UnlistedRegistrations, Is.EqualTo(new[] { $"{TheMadeUpAlpha}.AlphaBanner" }),
                    $"The check named {Listed(verdict.UnlistedRegistrations)} as registered by a made-up composition root that registers the service its lookup list names, and looks it up. " +
                    "Expected exactly that service. Why it matters: a service the host may look up must still be registered by its module, so the lookup list must not excuse its registration. " +
                    "How to fix: compare registrations only with the registration list.");
                Assert.That(verdict.UnlistedLookups.Concat(verdict.GoneRegistrations).Concat(verdict.GoneLookups), Is.Empty,
                    "The lookup of the listed service was named, or an entry was named as gone. Why it matters: the lookup is allowed. How to fix: compare lookups with the lookup list.");
            }
        }

        private static void ThenTheCrossContextLineIsNamedOnlyOnceTakenOff(CompositionRootVerdict listed, CompositionRootVerdict takenOff, string crossContextRegistration)
        {
            ThenNothingIsNamed(listed, "a made-up composition root keeping one statement that registers an Alpha service built from an AlphaPlus type, with that registration listed");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(takenOff.UnlistedRegistrations, Is.EqualTo(new[] { crossContextRegistration }),
                    $"With the cross-context line's registration taken off the list, the check named {Listed(takenOff.UnlistedRegistrations)} as registered and not listed. Expected exactly {crossContextRegistration}. " +
                    "Why it matters: cross-context wiring kept in the composition root is an exception by name, so a line nobody listed must be named. " +
                    "How to fix: compare the cross-context entries with the registrations like every other entry.");
                Assert.That(takenOff.UnlistedLookups.Concat(takenOff.GoneRegistrations).Concat(takenOff.GoneLookups), Is.Empty,
                    "Taking the registration off the list named a lookup or a gone entry of Alpha. Why it matters: the line looks up a type of another context, which is not Alpha's. How to fix: match the context by its whole name.");
            }
        }

        private static void ThenNothingIsNamed(CompositionRootVerdict verdict, string subject)
        {
            Assert.That(verdict.UnlistedRegistrations.Concat(verdict.UnlistedLookups).Concat(verdict.GoneRegistrations).Concat(verdict.GoneLookups), Is.Empty,
                $"The check named registrations {Listed(verdict.UnlistedRegistrations)}, lookups {Listed(verdict.UnlistedLookups)}, gone {Listed(verdict.GoneRegistrations.Concat(verdict.GoneLookups))} for {subject}. " +
                "Why it matters: a compliant composition root must pass, or the lists grow with entries nobody needs. How to fix: report only what the lists do not allow.");
        }

        private static void ThenOnlyTheGoneEntriesAreNamed(CompositionRootVerdict verdict, string goneRegistration, string goneLookup)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.GoneRegistrations, Is.EqualTo(new[] { goneRegistration }),
                    $"The check named {Listed(verdict.GoneRegistrations)} as listed and no longer registered. Expected exactly the listed registration the made-up composition root does not make. " +
                    "Why it matters: a list compared only one way keeps dead entries that hide a later registration. How to fix: compare the list both ways.");
                Assert.That(verdict.GoneLookups, Is.EqualTo(new[] { goneLookup }),
                    $"The check named {Listed(verdict.GoneLookups)} as listed and no longer looked up. Expected exactly the listed lookup the made-up composition root does not make. " +
                    "Why it matters: a dead lookup entry hides a later lookup. How to fix: compare the list both ways.");
                Assert.That(verdict.UnlistedRegistrations.Concat(verdict.UnlistedLookups), Is.Empty,
                    "The check named what a made-up composition root starting Alpha only through its module registers or looks up. Why it matters: what the module registers is the module's. How to fix: read only the composition root and its nested types.");
            }
        }

        private static void ThenTheCompliantRootIsNotReported(string? problem, List<string> namers, CompositionRootVerdict verdict)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(problem, Is.Null, $"The made-up Alpha module was refused: {problem}. Why it matters: a well-formed module must pass, or the check refuses every context. How to fix: accept a public static class with a public static Add method extending the service collection.");
                Assert.That(namers, Does.Contain(ARootStartingAlphaThroughItsModule),
                    $"The made-up composition root that calls AddAlpha was not found among the types naming the Alpha module: {Listed(namers)}. " +
                    "Why it matters: a check that cannot see the call reports every module as never started. How to fix: read the calls of the module's methods from the IL, lambdas included.");
            }

            ThenNothingIsNamed(verdict, "a made-up composition root that starts Alpha only through its module and registers a type outside the contexts");
        }

        private static void ThenTheOutsidersAreNamed(List<string> namers)
        {
            string[] expected =
            [
                $"{TheMadeUpRoot}.Elsewhere.AnOutsiderStartingAlpha",
                $"{TheMadeUpModulesRoot}.Beta.BetaModule",
                ARootStartingAlphaThroughItsModule,
            ];

            Assert.That(namers, Is.EqualTo(expected.Order(StringComparer.Ordinal)),
                $"The check named {Listed(namers)} as naming the made-up Alpha module. Expected exactly the made-up composition root, a made-up Beta module that starts Alpha inside its own Add method, and a made-up type elsewhere that starts Alpha, and not Alpha's own module nor the composition roots that register or look up Alpha's types themselves. " +
                "Why it matters: a check that misses a second caller lets a context be started twice or by another context. How to fix: read every type that references the module, nested types included, and leave out the module itself.");
        }

        private static void ThenTheModuleProblemIsNamed(string? problem, string context, string expectedProblem)
        {
            Assert.That(problem, Does.Contain(expectedProblem).And.Contain(ModuleTypeOf(TheMadeUpModulesRoot, context)),
                $"The check said [{problem ?? "nothing"}] about the made-up {context} context. Expected it to name {ModuleTypeOf(TheMadeUpModulesRoot, context)} and say {expectedProblem}. " +
                "Why it matters: a context whose module is missing, is not a static class, or lacks its Add method extending the service collection cannot be started the way the others are. " +
                "How to fix: look the module up by its full name and check its shape and its Add method.");
        }

        private sealed record CompositionRootVerdict(List<string> UnlistedRegistrations, List<string> GoneRegistrations, List<string> UnlistedLookups, List<string> GoneLookups);
    }
}

namespace Lighthouse.Backend.Tests.Architecture.CompositionRootFixtures.Modules.Alpha
{
    public static class AlphaModule
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddAlpha(this Microsoft.Extensions.DependencyInjection.IServiceCollection services)
            => Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddScoped<Application.IAlphaService, Application.AlphaService>(services);
    }
}

namespace Lighthouse.Backend.Tests.Architecture.CompositionRootFixtures.Modules.Alpha.Application
{
    public interface IAlphaService
    {
        int Serve(int days);
    }

    public interface IAlphaReader<out TRecord>
    {
        TRecord Read();
    }

    public sealed class AlphaService : IAlphaService
    {
        public int Serve(int days) => days + 1;
    }

    public sealed class AlphaJob : IAlphaService
    {
        public int Serve(int days) => days + 2;
    }

    public sealed class AlphaClock
    {
        public int Tick(int days) => days + 3;
    }

    public sealed class AlphaOptions
    {
        public int Limit { get; set; }
    }

    public sealed record AlphaRecord(int Days);

    public sealed class AlphaRecordReader : IAlphaReader<AlphaRecord>
    {
        public AlphaRecord Read() => new(4);
    }

    public sealed class AlphaWorker : Microsoft.Extensions.Hosting.BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
    }

    public sealed class AlphaClient(HttpClient client)
    {
        public Uri? Address => client.BaseAddress;
    }

    public sealed class AlphaTypedClient(HttpClient client)
    {
        public Uri? Address => client.BaseAddress;
    }

    public sealed class AlphaMessageHandler : DelegatingHandler;

    public sealed class AlphaKeyed
    {
        public int Tick(int days) => days + 10;
    }

    public sealed class AlphaByType
    {
        public int Tick(int days) => days + 11;
    }

    public sealed class AlphaDescribed
    {
        public int Tick(int days) => days + 12;
    }

    public sealed class AlphaDescribedByType
    {
        public int Tick(int days) => days + 13;
    }

    public sealed class AlphaBanner
    {
        public int Threads { get; } = 2;
    }
}

namespace Lighthouse.Backend.Tests.Architecture.CompositionRootFixtures.Modules.AlphaPlus.Application
{
    public sealed class AlphaPlusService
    {
        public int Serve(int days) => days + 5;
    }

    public sealed class AlphaPlusAnswersToAlpha : Alpha.Application.IAlphaService
    {
        public int Serve(int days) => days + 14;
    }
}

namespace Lighthouse.Backend.Tests.Architecture.CompositionRootFixtures.Modules.Beta
{
    public static class BetaModule
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddBeta(this Microsoft.Extensions.DependencyInjection.IServiceCollection services)
            => Alpha.AlphaModule.AddAlpha(services);
    }
}

namespace Lighthouse.Backend.Tests.Architecture.CompositionRootFixtures.Modules.Gamma.Application
{
    public sealed class GammaService
    {
        public int Serve(int days) => days + 6;
    }
}

namespace Lighthouse.Backend.Tests.Architecture.CompositionRootFixtures.Modules.Delta
{
    public static class DeltaModule
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection StartDelta(this Microsoft.Extensions.DependencyInjection.IServiceCollection services)
            => Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddScoped<Application.DeltaService>(services);
    }
}

namespace Lighthouse.Backend.Tests.Architecture.CompositionRootFixtures.Modules.Delta.Application
{
    public sealed class DeltaService
    {
        public int Serve(int days) => days + 7;
    }
}

namespace Lighthouse.Backend.Tests.Architecture.CompositionRootFixtures.Modules.Epsilon
{
    public sealed class EpsilonModule
    {
        public int Started { get; private set; }

        public Microsoft.Extensions.DependencyInjection.IServiceCollection AddEpsilon(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
        {
            Started++;
            return Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddScoped<Application.EpsilonService>(services);
        }
    }
}

namespace Lighthouse.Backend.Tests.Architecture.CompositionRootFixtures.Modules.Epsilon.Application
{
    public sealed class EpsilonService
    {
        public int Serve(int days) => days + 8;
    }
}

namespace Lighthouse.Backend.Tests.Architecture.CompositionRootFixtures.Shared
{
    public sealed class AClockOutsideTheContexts
    {
        public int Tick(int days) => days + 9;
    }
}

namespace Lighthouse.Backend.Tests.Architecture.CompositionRootFixtures.Roots
{
    using Lighthouse.Backend.Tests.Architecture.CompositionRootFixtures.Modules.Alpha;
    using Lighthouse.Backend.Tests.Architecture.CompositionRootFixtures.Modules.Alpha.Application;
    using Lighthouse.Backend.Tests.Architecture.CompositionRootFixtures.Modules.AlphaPlus.Application;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

    public static class ARootRegisteringAlphasTypesItself
    {
        public static void Compose(IServiceCollection services)
        {
            services.AddScoped<IAlphaService, AlphaService>();
            services.AddSingleton<IAlphaService>(provider => provider.GetRequiredService<AlphaJob>());
            services.TryAddSingleton<AlphaClock>();
            services.Configure<AlphaOptions>(options => options.Limit = 1);
            services.AddScoped<IAlphaReader<AlphaRecord>, AlphaRecordReader>();
            services.AddHostedService<AlphaWorker>();
            services.AddHttpClient<AlphaClient>().AddHttpMessageHandler<AlphaMessageHandler>();
            services.AddHttpClient("alpha").AddTypedClient<AlphaTypedClient>();
            services.AddKeyedSingleton<AlphaKeyed>("alpha");
#pragma warning disable CA2263 // The rule must be shown to catch a registration written with the Type overload, so this fixture keeps that form on purpose.
            services.AddScoped(typeof(AlphaByType));
#pragma warning restore CA2263
            services.Add(ServiceDescriptor.Singleton<AlphaDescribed, AlphaDescribed>());
            services.Add(new ServiceDescriptor(typeof(AlphaDescribedByType), typeof(AlphaDescribedByType), ServiceLifetime.Scoped));
            services.AddScoped<AlphaPlusService>();
            services.AddSingleton<Shared.AClockOutsideTheContexts>();
        }
    }

    public static class ARootLookingUpAlphasTypes
    {
        public static object?[] Use(IServiceProvider provider)
            =>
            [
                provider.GetServices<IAlphaService>(),
                provider.GetRequiredKeyedService<AlphaKeyed>("alpha"),
                provider.GetKeyedService<AlphaClock>("alpha"),
                ActivatorUtilities.CreateInstance<AlphaService>(provider),
                provider.GetService<AlphaPlusService>(),
            ];
    }

    public static class ARootRegisteringAHostSetUpTypeItself
    {
        public static void Compose(IServiceCollection services) => services.AddSingleton<AlphaBanner>();

        public static int Print(IServiceProvider provider) => provider.GetRequiredService<AlphaBanner>().Threads;
    }

    public static class ARootOnlyLookingUpAHostSetUpType
    {
        public static int Print(IServiceProvider provider) => provider.GetRequiredService<AlphaBanner>().Threads;
    }

    public static class ARootKeepingCrossContextWiring
    {
        public static void Compose(IServiceCollection services)
            => services.AddScoped<IAlphaService>(provider => provider.GetRequiredService<AlphaPlusAnswersToAlpha>());
    }

    public static class ARootStartingAlphaThroughItsModule
    {
        public static void Compose(IServiceCollection services)
        {
            services.AddAlpha();
            services.AddSingleton<Shared.AClockOutsideTheContexts>();
        }
    }
}

namespace Lighthouse.Backend.Tests.Architecture.CompositionRootFixtures.Elsewhere
{
    public static class AnOutsiderStartingAlpha
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection Start(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
            => Modules.Alpha.AlphaModule.AddAlpha(services);
    }
}
