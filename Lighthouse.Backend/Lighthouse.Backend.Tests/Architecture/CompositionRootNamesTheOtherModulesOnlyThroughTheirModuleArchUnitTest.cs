using System.Text.RegularExpressions;
using Lighthouse.Backend.Data;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Work Items, Tracker integration, Refinement, Reports and Organisation set-up each start themselves up: each has a static
    /// module class at its root with one Add method for the service collection, only the composition root calls it, and the
    /// composition root itself registers and looks up no type of these contexts but four kinds, each with its own list: the
    /// domain-event handlers of its one ordered block (the dispatcher runs them in registration order across contexts), the
    /// registrations it keeps so that a service registered more than once keeps its order, what it looks up to set the host up,
    /// and the statements that wire two contexts together. Registrations and lookups are compared with their own lists, both
    /// ways. With the composition-root rule of the first five contexts this covers the ten. The same checks run over made-up
    /// contexts and composition roots, one made-up type per way a check can be broken, so a check that names nothing cannot pass.
    /// </summary>
    [TestFixture]
    public class CompositionRootNamesTheOtherModulesOnlyThroughTheirModuleArchUnitTest
    {
        private const string TheCompositionRoot = "Lighthouse.Backend.Program";

        private const string AnyNamespace = "^";

        private const string Registered = "registered";

        private const string LookedUp = "looked up";

        private const string TheServiceCollection = "Microsoft.Extensions.DependencyInjection.IServiceCollection";

        private const string TheExtensionAttribute = "System.Runtime.CompilerServices.ExtensionAttribute";

        private const string TheMadeUpRoot = "Lighthouse.Backend.Tests.Architecture.OtherCompositionRootFixtures";

        private const string TheMadeUpModulesRoot = TheMadeUpRoot + ".Modules";

        private const string TheMadeUpKappa = TheMadeUpModulesRoot + ".Kappa.Application";

        private const string TheMadeUpTypes = @"^Lighthouse\.Backend\.Tests\.Architecture\.OtherCompositionRootFixtures(\.|$)";

        private const string ARootRegisteringATypeOfEachOtherContext = TheMadeUpRoot + ".Roots.ARootRegisteringATypeOfEachOtherContext";

        private const string ARootLookingUpKappasService = TheMadeUpRoot + ".Roots.ARootLookingUpKappasService";

        private const string ARootKeepingKappasStepsInOrder = TheMadeUpRoot + ".Roots.ARootKeepingKappasStepsInOrder";

        private const string ARootStartingKappaThroughItsModule = TheMadeUpRoot + ".Roots.ARootStartingKappaThroughItsModule";

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(LighthouseAppContext).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(CompositionRootNamesTheOtherModulesOnlyThroughTheirModuleArchUnitTest).Assembly));

        private static string TheModulesRoot => ContextContractsMembership.TheModulesRoot;

        private static IReadOnlyList<string> TheOtherFiveContexts => ["OrganisationSetup", "Refinement", "Reports", "TrackerIntegration", "WorkItems"];

        private static IReadOnlyList<string> TheFiveTheFirstRuleLists => ["Access", "FlowMetrics", "Forecasting", "Licensing", "Promises"];

        // The dispatcher runs the handlers of an event in the order the composition root registers them, across contexts, so
        // they stay in the composition root's one ordered block rather than moving into each context's module.
        private static Dictionary<string, IReadOnlyList<string>> RegisteredByTheHandlerBlock => new(StringComparer.Ordinal)
        {
            ["OrganisationSetup"] = [],
            ["Refinement"] = [],
            ["Reports"] = [],
            ["TrackerIntegration"] = [],
            ["WorkItems"] =
            [
                $"{TheModulesRoot}.WorkItems.Application.Policies.ConnectionFetchShapeChangedUpdateTriggerHandler",
                $"{TheModulesRoot}.WorkItems.Application.Policies.FeatureBlockedTransitionCaptureHandler",
                $"{TheModulesRoot}.WorkItems.Application.Policies.FeatureBlockedTransitionCloseHandler",
                $"{TheModulesRoot}.WorkItems.Application.Policies.PortfolioDeletedOrphanedFeatureCleanupHandler",
                $"{TheModulesRoot}.WorkItems.Application.Policies.TeamDeletedRefreshLogCleanupHandler",
                $"{TheModulesRoot}.WorkItems.Application.Policies.WorkItemBlockedTransitionCaptureHandler",
                $"{TheModulesRoot}.WorkItems.Application.Policies.WorkItemBlockedTransitionCloseHandler",
            ],
        };

        private static Dictionary<string, IReadOnlyList<string>> LookedUpByTheHandlerBlock => new(StringComparer.Ordinal)
        {
            ["OrganisationSetup"] = [],
            ["Refinement"] = [],
            ["Reports"] = [],
            ["TrackerIntegration"] = [],
            ["WorkItems"] = [],
        };

        // A service registered more than once is resolved in registration order, so a line whose place no call of the
        // context's module can keep stays in the composition root. Each of these contexts keeps its order from one call.
        private static Dictionary<string, IReadOnlyList<string>> RegisteredToKeepTheirOrder => new(StringComparer.Ordinal)
        {
            ["OrganisationSetup"] = [],
            ["Refinement"] = [],
            ["Reports"] = [],
            ["TrackerIntegration"] = [],
            ["WorkItems"] = [],
        };

        private static Dictionary<string, IReadOnlyList<string>> LookedUpToSetTheHostUp => new(StringComparer.Ordinal)
        {
            ["OrganisationSetup"] = [],
            ["Refinement"] = [],
            ["Reports"] = [],
            ["TrackerIntegration"] = [],
            ["WorkItems"] = [],
        };

        // A statement that registers one context's type as another context's service names two contexts; no context's own
        // module may name another context, so the statement stays here.
        private static Dictionary<string, IReadOnlyList<string>> RegisteredByCrossContextWiring => new(StringComparer.Ordinal)
        {
            ["OrganisationSetup"] = [],
            ["Refinement"] = [],
            ["Reports"] = [],
            ["TrackerIntegration"] = [$"{TheModulesRoot}.TrackerIntegration.Contracts.IDescriptionBlockMerger"],
            ["WorkItems"] = [],
        };

        private static Dictionary<string, IReadOnlyList<string>> LookedUpByCrossContextWiring => new(StringComparer.Ordinal)
        {
            ["OrganisationSetup"] = [],
            ["Refinement"] = [],
            ["Reports"] = [],
            ["TrackerIntegration"] = [],
            ["WorkItems"] = [],
        };

        private static IEnumerable<TestCaseData> EveryAllowedEntry
            => TheOtherFiveContexts.SelectMany(context =>
                MayRegister(context).Select(entry => new TestCaseData(context, Registered, entry))
                    .Concat(MayLookUp(context).Select(entry => new TestCaseData(context, LookedUp, entry))));

        private static IEnumerable<TestCaseData> OneTypeOfEachOtherContext
            =>
            [
                new TestCaseData("OrganisationSetup", typeof(Backend.Modules.OrganisationSetup.Application.OptionalFeatures.OptionalFeatureApplierRegistry).FullName),
                new TestCaseData("Refinement", typeof(Backend.Modules.Refinement.Application.RefinementList).FullName),
                new TestCaseData("Reports", typeof(Backend.Modules.Reports.Application.Metrics.ReportMetricCatalog).FullName),
                new TestCaseData("TrackerIntegration", typeof(Backend.Modules.TrackerIntegration.Application.ConnectionHealth.ConnectionHealthCadence).FullName),
                new TestCaseData("WorkItems", typeof(Backend.Modules.WorkItems.Application.Dependencies.FeatureDependencyRendering).FullName),
            ];

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [TestCaseSource(nameof(TheOtherFiveContexts))]
        public void EachOfTheOtherFiveContexts_RegistersThroughTheAddMethodOfItsOwnModule(string context)
        {
            var problem = WhenTheStartUpModuleIsLookedFor(ProductionModule.Value, TheModulesRoot, context);

            ThenTheModuleIsThere(problem, TheModulesRoot, context);
        }

        [TestCaseSource(nameof(TheOtherFiveContexts))]
        public void EachOfTheOtherFiveContextsModules_IsStartedByTheCompositionRootAlone(string context)
        {
            var namers = WhenTheTypesNamingTheModuleAreRead(ProductionModule.Value, TheModulesRoot, context, AnyNamespace);

            ThenOnlyTheCompositionRootNamesTheModule(namers, TheCompositionRoot, TheModulesRoot, context);
        }

        [TestCaseSource(nameof(TheOtherFiveContexts))]
        public void TheCompositionRoot_RegistersNoTypeOfTheOtherFiveContexts_ButItsHandlerBlockOrderKeptLinesHostSetUpAndCrossContextWiring(string context)
        {
            var verdict = WhenTheCompositionRootIsComparedWithTheLists(ProductionModule.Value, TheCompositionRoot, TheModulesRoot, context, MayRegister(context), MayLookUp(context));

            ThenTheCompositionRootRegistersAndLooksUpOnlyTheListed(verdict, TheCompositionRoot, context);
        }

        [TestCaseSource(nameof(EveryAllowedEntry))]
        public void AnAllowedEntryOfTheOtherFiveContexts_IsNamedOnceItIsTakenOffItsList(string context, string list, string entry)
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
        public void TheOtherFiveContextsAndTheFiveOfTheFirstRule_AreTheTenPackagedContexts()
        {
            var packaged = PackagedContexts.InTheAssembly(ProductionModule.Value);

            ThenBothRulesTogetherListEveryPackagedContextOnce(packaged);
        }

        [TestCaseSource(nameof(OneTypeOfEachOtherContext))]
        public void AMadeUpCompositionRootRegisteringATypeOfOneOfTheOtherFiveContexts_IsReportedNamingIt(string context, string registeredType)
        {
            var verdict = WhenTheCompositionRootIsComparedWithTheLists(TestModule.Value, ARootRegisteringATypeOfEachOtherContext, TheModulesRoot, context, [], []);

            ThenOnlyTheRegisteredTypeIsNamed(verdict, context, registeredType);
        }

        [Test]
        public void AMadeUpCompositionRootLookingUpAContextsService_IsReportedAsALookupOnly()
        {
            var verdict = WhenTheCompositionRootIsComparedWithTheLists(TestModule.Value, ARootLookingUpKappasService, TheMadeUpModulesRoot, "Kappa", [], []);

            ThenOnlyTheLookupIsNamed(verdict, $"{TheMadeUpKappa}.KappaService");
        }

        [Test]
        public void AMadeUpOrderKeptLine_PassesWhileListed_AndIsNamedOnceItIsTakenOffTheList()
        {
            var orderKept = new[] { $"{TheMadeUpKappa}.IKappaStep", $"{TheMadeUpKappa}.KappaFirstStep", $"{TheMadeUpKappa}.KappaSecondStep" };

            var listed = WhenTheCompositionRootIsComparedWithTheLists(TestModule.Value, ARootKeepingKappasStepsInOrder, TheMadeUpModulesRoot, "Kappa", orderKept, []);
            var takenOff = WhenTheCompositionRootIsComparedWithTheLists(TestModule.Value, ARootKeepingKappasStepsInOrder, TheMadeUpModulesRoot, "Kappa", [], []);

            ThenTheOrderKeptLineIsNamedOnlyOnceTakenOff(listed, takenOff, orderKept);
        }

        [Test]
        public void AMadeUpCompositionRootStartingAContextOnlyThroughItsModule_IsNotReported()
        {
            var problem = WhenTheStartUpModuleIsLookedFor(TestModule.Value, TheMadeUpModulesRoot, "Kappa");
            var namers = WhenTheTypesNamingTheModuleAreRead(TestModule.Value, TheMadeUpModulesRoot, "Kappa", TheMadeUpTypes);
            var verdict = WhenTheCompositionRootIsComparedWithTheLists(TestModule.Value, ARootStartingKappaThroughItsModule, TheMadeUpModulesRoot, "Kappa", [], []);

            ThenTheCompliantRootIsNotReported(problem, namers, verdict);
        }

        [Test]
        public void AnAllowedEntryAMadeUpCompositionRootDoesNotRegisterOrLookUp_IsReportedAsGone()
        {
            var goneRegistration = $"{TheMadeUpKappa}.AGoneHandler";
            var goneLookup = $"{TheMadeUpKappa}.AGoneBanner";

            var verdict = WhenTheCompositionRootIsComparedWithTheLists(TestModule.Value, ARootStartingKappaThroughItsModule, TheMadeUpModulesRoot, "Kappa", [goneRegistration], [goneLookup]);

            ThenOnlyTheGoneEntriesAreNamed(verdict, goneRegistration, goneLookup);
        }

        [Test]
        public void AMadeUpModuleOrOtherTypeStartingAContextsModule_IsReportedBesideTheCompositionRoot()
        {
            var namers = WhenTheTypesNamingTheModuleAreRead(TestModule.Value, TheMadeUpModulesRoot, "Kappa", TheMadeUpTypes);

            ThenTheOutsidersAreNamed(namers);
        }

        [TestCase("Mu", "no type")]
        [TestCase("Nu", "no public static AddNu")]
        [TestCase("Xi", "not a public static class")]
        public void AMadeUpContextWithoutAWellFormedStartUpModule_IsReported(string context, string expectedProblem)
        {
            var problem = WhenTheStartUpModuleIsLookedFor(TestModule.Value, TheMadeUpModulesRoot, context);

            ThenTheModuleProblemIsNamed(problem, context, expectedProblem);
        }

        private static IReadOnlyList<string> MayRegister(string context)
            => [.. RegisteredByTheHandlerBlock[context], .. EventTypesTheCompositionRootNames.Of(context), .. RegisteredToKeepTheirOrder[context], .. RegisteredByCrossContextWiring[context]];

        private static IReadOnlyList<string> MayLookUp(string context)
            => [.. LookedUpByTheHandlerBlock[context], .. LookedUpToSetTheHostUp[context], .. LookedUpByCrossContextWiring[context]];

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
                    $"How to fix: move the registration lines into Add{context} unchanged and in their order; only a handler of the ordered block, a line no call position can keep in order, or a statement naming two contexts stays, and is listed here.");
                Assert.That(verdict.UnlistedLookups, Is.Empty,
                    $"The composition root {compositionRoot} looks up these types of the {context} context: {Listed(verdict.UnlistedLookups)}. " +
                    "Why it matters: a lookup in the composition root is a dependency on the context's inside that its module does not own. " +
                    $"How to fix: move the line that looks it up behind a method of the {context} module called at the same point; only a forward of the handler block, a service the host needs to set itself up, or a statement naming two contexts stays, and is listed here.");
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
                    "Why it matters: the registration and lookup lists are separate, so taking an entry off one must change only what that list excuses. How to fix: compare registrations and lookups each with its own lists.");
                Assert.That(withoutTheEntry.GoneRegistrations.Concat(withoutTheEntry.GoneLookups), Is.EqualTo(full.GoneRegistrations.Concat(full.GoneLookups)),
                    $"With {entry} taken off the {list} list, what the check names as gone changed. Why it matters: taking one entry off must change nothing else. How to fix: compare each entry on its own.");
            }
        }

        private static void ThenBothRulesTogetherListEveryPackagedContextOnce(IReadOnlyList<string> packaged)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheOtherFiveContexts.Intersect(TheFiveTheFirstRuleLists, StringComparer.Ordinal), Is.Empty,
                    $"These contexts are listed by both composition-root rules: {Listed(TheOtherFiveContexts.Intersect(TheFiveTheFirstRuleLists, StringComparer.Ordinal))}. " +
                    "Why it matters: each context's start-up is judged against one set of lists, or two rules disagree about what may stay in the composition root. How to fix: list each context in one rule.");
                Assert.That(TheOtherFiveContexts.Concat(TheFiveTheFirstRuleLists).Order(StringComparer.Ordinal), Is.EqualTo(packaged.Order(StringComparer.Ordinal)),
                    $"The two composition-root rules list {Listed(TheOtherFiveContexts.Concat(TheFiveTheFirstRuleLists).Order(StringComparer.Ordinal))}; the contexts the application holds are {Listed(packaged.Order(StringComparer.Ordinal))}. " +
                    "Why it matters: a context neither rule lists can keep its registrations in the composition root unseen. How to fix: list every context the application holds in one of the two rules.");
            }
        }

        private static void ThenOnlyTheRegisteredTypeIsNamed(CompositionRootVerdict verdict, string context, string registeredType)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.UnlistedRegistrations, Is.EqualTo(new[] { registeredType }),
                    $"For the {context} context the check named {Listed(verdict.UnlistedRegistrations)} as registered by a made-up composition root that registers one type of each of the five contexts. Expected exactly {registeredType}. " +
                    $"Why it matters: a registration left in a composition root must be named under its own context, and never under another one whose name it does not carry. " +
                    "How to fix: read every registration of the composition root and match the context by its whole name.");
                Assert.That(verdict.UnlistedLookups.Concat(verdict.GoneRegistrations).Concat(verdict.GoneLookups), Is.Empty,
                    "The check named a lookup or a gone entry for a made-up composition root that only registers, with empty lists. Why it matters: a registration is not a lookup. How to fix: keep registrations and lookups apart.");
            }
        }

        private static void ThenOnlyTheLookupIsNamed(CompositionRootVerdict verdict, string lookedUpType)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.UnlistedLookups, Is.EqualTo(new[] { lookedUpType }),
                    $"The check named {Listed(verdict.UnlistedLookups)} as looked up by a made-up composition root that asks the container for one Kappa service. Expected exactly {lookedUpType}. " +
                    "Why it matters: a composition root that reaches into a context through a lookup must be named. How to fix: compare the lookups with the lookup lists.");
                Assert.That(verdict.UnlistedRegistrations.Concat(verdict.GoneRegistrations).Concat(verdict.GoneLookups), Is.Empty,
                    $"The check named registrations {Listed(verdict.UnlistedRegistrations)} or gone entries for a made-up composition root that only looks a service up. " +
                    "Why it matters: a lookup counted as a registration lets the registration lists excuse what the lookup lists must name. How to fix: keep registrations and lookups apart.");
            }
        }

        private static void ThenTheOrderKeptLineIsNamedOnlyOnceTakenOff(CompositionRootVerdict listed, CompositionRootVerdict takenOff, string[] orderKept)
        {
            ThenNothingIsNamed(listed, "a made-up composition root keeping two registrations of one Kappa service in its order, with them listed as kept for their order");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(takenOff.UnlistedRegistrations, Is.EqualTo(orderKept.Order(StringComparer.Ordinal)),
                    $"With the lines kept for their order taken off the list, the check named {Listed(takenOff.UnlistedRegistrations)} as registered and not listed. Expected {Listed(orderKept.Order(StringComparer.Ordinal))}. " +
                    "Why it matters: a line kept in the composition root for its order is an exception by name, so a line nobody listed must be named. " +
                    "How to fix: compare the order-kept entries with the registrations like every other entry.");
                Assert.That(takenOff.UnlistedLookups.Concat(takenOff.GoneRegistrations).Concat(takenOff.GoneLookups), Is.Empty,
                    "Taking the order-kept lines off the list named a lookup or a gone entry. Why it matters: taking entries off one list must change only what that list excuses. How to fix: compare each list on its own.");
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
                    "The check named what a made-up composition root starting Kappa only through its module registers or looks up. Why it matters: what the module registers is the module's. How to fix: read only the composition root and its nested types.");
            }
        }

        private static void ThenTheCompliantRootIsNotReported(string? problem, List<string> namers, CompositionRootVerdict verdict)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(problem, Is.Null, $"The made-up Kappa module was refused: {problem}. Why it matters: a well-formed module must pass, or the check refuses every context. How to fix: accept a public static class with a public static Add method extending the service collection.");
                Assert.That(namers, Does.Contain(ARootStartingKappaThroughItsModule),
                    $"The made-up composition root that calls AddKappa was not found among the types naming the Kappa module: {Listed(namers)}. " +
                    "Why it matters: a check that cannot see the call reports every module as never started. How to fix: read the calls of the module's methods from the IL, lambdas included.");
            }

            ThenNothingIsNamed(verdict, "a made-up composition root that starts Kappa only through its module and registers a type outside the contexts");
        }

        private static void ThenTheOutsidersAreNamed(List<string> namers)
        {
            string[] expected =
            [
                $"{TheMadeUpRoot}.Elsewhere.AnOutsiderStartingKappa",
                $"{TheMadeUpModulesRoot}.Lambda.LambdaModule",
                ARootStartingKappaThroughItsModule,
            ];

            Assert.That(namers, Is.EqualTo(expected.Order(StringComparer.Ordinal)),
                $"The check named {Listed(namers)} as naming the made-up Kappa module. Expected exactly the made-up composition root, a made-up Lambda module that starts Kappa inside its own Add method, and a made-up type elsewhere that starts Kappa, and not Kappa's own module nor the composition roots that register or look up Kappa's types themselves. " +
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

namespace Lighthouse.Backend.Tests.Architecture.OtherCompositionRootFixtures.Modules.Kappa
{
    public static class KappaModule
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddKappa(this Microsoft.Extensions.DependencyInjection.IServiceCollection services)
            => Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddScoped<Application.KappaService>(services);
    }
}

namespace Lighthouse.Backend.Tests.Architecture.OtherCompositionRootFixtures.Modules.Kappa.Application
{
    public interface IKappaStep
    {
        int Step(int days);
    }

    public sealed class KappaService
    {
        public int Serve(int days) => days + 21;
    }

    public sealed class KappaFirstStep : IKappaStep
    {
        public int Step(int days) => days + 22;
    }

    public sealed class KappaSecondStep : IKappaStep
    {
        public int Step(int days) => days + 23;
    }
}

namespace Lighthouse.Backend.Tests.Architecture.OtherCompositionRootFixtures.Modules.Lambda
{
    public static class LambdaModule
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddLambda(this Microsoft.Extensions.DependencyInjection.IServiceCollection services)
            => Kappa.KappaModule.AddKappa(services);
    }
}

namespace Lighthouse.Backend.Tests.Architecture.OtherCompositionRootFixtures.Modules.Mu.Application
{
    public sealed class MuService
    {
        public int Serve(int days) => days + 24;
    }
}

namespace Lighthouse.Backend.Tests.Architecture.OtherCompositionRootFixtures.Modules.Nu
{
    public static class NuModule
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection StartNu(this Microsoft.Extensions.DependencyInjection.IServiceCollection services)
            => Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddScoped<Application.NuService>(services);
    }
}

namespace Lighthouse.Backend.Tests.Architecture.OtherCompositionRootFixtures.Modules.Nu.Application
{
    public sealed class NuService
    {
        public int Serve(int days) => days + 25;
    }
}

namespace Lighthouse.Backend.Tests.Architecture.OtherCompositionRootFixtures.Modules.Xi
{
    public sealed class XiModule
    {
        public int Started { get; private set; }

        public Microsoft.Extensions.DependencyInjection.IServiceCollection AddXi(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
        {
            Started++;
            return Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddScoped<Application.XiService>(services);
        }
    }
}

namespace Lighthouse.Backend.Tests.Architecture.OtherCompositionRootFixtures.Modules.Xi.Application
{
    public sealed class XiService
    {
        public int Serve(int days) => days + 26;
    }
}

namespace Lighthouse.Backend.Tests.Architecture.OtherCompositionRootFixtures.Shared
{
    public sealed class AClockOutsideTheContexts
    {
        public int Tick(int days) => days + 27;
    }
}

namespace Lighthouse.Backend.Tests.Architecture.OtherCompositionRootFixtures.Roots
{
    using Lighthouse.Backend.Tests.Architecture.OtherCompositionRootFixtures.Modules.Kappa;
    using Lighthouse.Backend.Tests.Architecture.OtherCompositionRootFixtures.Modules.Kappa.Application;
    using Microsoft.Extensions.DependencyInjection;

    public static class ARootRegisteringATypeOfEachOtherContext
    {
        public static void Compose(IServiceCollection services)
        {
            services.AddScoped<Lighthouse.Backend.Modules.OrganisationSetup.Application.OptionalFeatures.OptionalFeatureApplierRegistry>();
            services.AddScoped<Lighthouse.Backend.Modules.Refinement.Application.RefinementList>();
            services.AddScoped<Lighthouse.Backend.Modules.Reports.Application.Metrics.ReportMetricCatalog>();
            services.AddScoped<Lighthouse.Backend.Modules.TrackerIntegration.Application.ConnectionHealth.ConnectionHealthCadence>();
            services.AddScoped<Lighthouse.Backend.Modules.WorkItems.Application.Dependencies.FeatureDependencyRendering>();
            services.AddSingleton<Shared.AClockOutsideTheContexts>();
        }
    }

    public static class ARootLookingUpKappasService
    {
        public static int Use(IServiceProvider provider) => provider.GetRequiredService<KappaService>().Serve(1);
    }

    public static class ARootKeepingKappasStepsInOrder
    {
        public static void Compose(IServiceCollection services)
        {
            services.AddScoped<IKappaStep, KappaFirstStep>();
            services.AddSingleton<Shared.AClockOutsideTheContexts>();
            services.AddScoped<IKappaStep, KappaSecondStep>();
        }
    }

    public static class ARootStartingKappaThroughItsModule
    {
        public static void Compose(IServiceCollection services)
        {
            services.AddKappa();
            services.AddSingleton<Shared.AClockOutsideTheContexts>();
        }
    }
}

namespace Lighthouse.Backend.Tests.Architecture.OtherCompositionRootFixtures.Elsewhere
{
    public static class AnOutsiderStartingKappa
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection Start(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
            => Modules.Kappa.KappaModule.AddKappa(services);
    }
}
