using System.Text.RegularExpressions;
using Lighthouse.Backend.Data;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Flow metrics, Forecasting, Promises, Licensing and Access each start themselves up: each has a static module class at
    /// its root with one Add method for the service collection, only the composition root calls it, and the composition root
    /// itself registers or looks up no type of these contexts but the domain-event handlers it keeps in one ordered block (the
    /// dispatcher runs them in registration order across contexts) and the two services it looks up to set the host up. The
    /// lists are compared both ways. The same checks run over made-up contexts and composition roots, one made-up type per way a
    /// check can be broken, so a check that names nothing cannot pass.
    /// </summary>
    [TestFixture]
    public class CompositionRootNamesModulesOnlyThroughTheirModuleArchUnitTest
    {
        private const string TheCompositionRoot = "Lighthouse.Backend.Program";

        private static string TheModulesRoot => ContextContractsMembership.TheModulesRoot;

        private const string AnyNamespace = "^";

        private const string TheServiceCollection = "Microsoft.Extensions.DependencyInjection.IServiceCollection";

        private const string TheExtensionAttribute = "System.Runtime.CompilerServices.ExtensionAttribute";

        private const string TheMadeUpRoot = "Lighthouse.Backend.Tests.Architecture.CompositionRootFixtures";

        private const string TheMadeUpModulesRoot = TheMadeUpRoot + ".Modules";

        private const string TheMadeUpTypes = @"^Lighthouse\.Backend\.Tests\.Architecture\.CompositionRootFixtures(\.|$)";

        private const string ARootRegisteringAlphasTypesItself = TheMadeUpRoot + ".Roots.ARootRegisteringAlphasTypesItself";

        private const string ARootStartingAlphaThroughItsModule = TheMadeUpRoot + ".Roots.ARootStartingAlphaThroughItsModule";

        private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(LighthouseAppContext).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(CompositionRootNamesModulesOnlyThroughTheirModuleArchUnitTest).Assembly));

        private static IReadOnlyList<string> TheFiveContexts => ["Access", "FlowMetrics", "Forecasting", "Licensing", "Promises"];

        // The dispatcher runs the handlers of an event in the order the composition root registers them, across contexts, so
        // they stay in the composition root's one ordered block rather than moving into each context's module.
        private static Dictionary<string, IReadOnlyList<string>> TheDomainEventHandlerBlock => new(StringComparer.Ordinal)
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
                $"{TheModulesRoot}.FlowMetrics.Application.ReconstructionMemo",
            ],
            ["Forecasting"] =
            [
                $"{TheModulesRoot}.Forecasting.Application.Policies.FeatureOrderingPolicyChangedForecastTriggerHandler",
                $"{TheModulesRoot}.Forecasting.Application.Policies.FeatureRankChangedForecastTriggerHandler",
                $"{TheModulesRoot}.Forecasting.Application.Policies.TeamDataRefreshedForecastTriggerHandler",
                $"{TheModulesRoot}.Forecasting.Application.Policies.TeamDeletedForecastRetriggerHandler",
            ],
            ["Licensing"] = [],
            ["Promises"] =
            [
                $"{TheModulesRoot}.Promises.Application.Policies.DeliveryForecastPublishingHandler",
                $"{TheModulesRoot}.Promises.Application.Policies.DeliveryMetricSnapshotRecordingHandler",
            ],
        };

        // Looked up from the started host, not registered: the forecast thread count the start-up banner prints, and the writer
        // the sign-in event of the identity provider calls to keep the user's groups.
        private static Dictionary<string, IReadOnlyList<string>> LookedUpToSetTheHostUp => new(StringComparer.Ordinal)
        {
            ["Access"] = [$"{TheModulesRoot}.Access.Application.IOidcGroupSnapshotWriter"],
            ["FlowMetrics"] = [],
            ["Forecasting"] = [$"{TheModulesRoot}.Forecasting.Application.ForecastLane"],
            ["Licensing"] = [],
            ["Promises"] = [],
        };

        private static IEnumerable<TestCaseData> EveryAllowedEntry
            => TheFiveContexts.SelectMany(context => AllowedFor(context).Select(entry => new TestCaseData(context, entry)));

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
            var (unlisted, gone) = WhenTheRegistrationsAreComparedWithTheList(ProductionModule.Value, TheCompositionRoot, TheModulesRoot, context, AllowedFor(context));

            ThenTheCompositionRootRegistersOnlyTheListed(unlisted, gone, TheCompositionRoot, context);
        }

        [TestCaseSource(nameof(EveryAllowedEntry))]
        public void AnAllowedEntryOfTheCompositionRoot_IsNamedOnceItIsTakenOffTheList(string context, string entry)
        {
            var full = WhenTheRegistrationsAreComparedWithTheList(ProductionModule.Value, TheCompositionRoot, TheModulesRoot, context, AllowedFor(context));

            var withoutTheEntry = WhenTheRegistrationsAreComparedWithTheList(ProductionModule.Value, TheCompositionRoot, TheModulesRoot, context, [.. AllowedFor(context).Where(allowed => allowed != entry)]);

            ThenOnlyTheEntryTakenOffIsAddedToWhatIsNamed(entry, full, withoutTheEntry);
        }

        [Test]
        public void AMadeUpCompositionRootRegisteringAContextsTypes_IsReportedNamingEachType_ButNotATypeOfAContextWhoseNameBeginsAlike()
        {
            var (unlisted, gone) = WhenTheRegistrationsAreComparedWithTheList(TestModule.Value, ARootRegisteringAlphasTypesItself, TheMadeUpModulesRoot, "Alpha", []);

            ThenEveryWayOfRegisteringIsNamed(unlisted, gone);
        }

        [Test]
        public void AnAllowedEntryAMadeUpCompositionRootDoesNotRegister_IsReportedAsGone()
        {
            var goneEntry = $"{TheMadeUpModulesRoot}.Alpha.Application.AGoneHandler";

            var (unlisted, gone) = WhenTheRegistrationsAreComparedWithTheList(TestModule.Value, ARootStartingAlphaThroughItsModule, TheMadeUpModulesRoot, "Alpha", [goneEntry]);

            ThenOnlyTheGoneEntryIsNamed(unlisted, gone, goneEntry);
        }

        [Test]
        public void AMadeUpCompositionRootStartingAContextOnlyThroughItsModule_IsNotReported()
        {
            var problem = WhenTheStartUpModuleIsLookedFor(TestModule.Value, TheMadeUpModulesRoot, "Alpha");
            var namers = WhenTheTypesNamingTheModuleAreRead(TestModule.Value, TheMadeUpModulesRoot, "Alpha", TheMadeUpTypes);
            var (unlisted, gone) = WhenTheRegistrationsAreComparedWithTheList(TestModule.Value, ARootStartingAlphaThroughItsModule, TheMadeUpModulesRoot, "Alpha", []);

            ThenTheCompliantRootIsNotReported(problem, namers, unlisted, gone);
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

        private static IReadOnlyList<string> AllowedFor(string context) => [.. TheDomainEventHandlerBlock[context], .. LookedUpToSetTheHostUp[context]];

        private static string ContextPattern(string modulesRoot, string context) => $"^{Regex.Escape(modulesRoot)}\\.{context}(\\.|$)";

        private static string ModuleTypeOf(string modulesRoot, string context) => $"{modulesRoot}.{context}.{context}Module";

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

        private static (List<string> Unlisted, List<string> Gone) WhenTheRegistrationsAreComparedWithTheList(ModuleDefinition module, string compositionRoot, string modulesRoot, string context, IReadOnlyCollection<string> allowed)
        {
            var registered = CompositionRootRegistrations.Of(module, compositionRoot, ContextPattern(modulesRoot, context));

            return ([.. registered.Except(allowed, StringComparer.Ordinal).Order(StringComparer.Ordinal)], [.. allowed.Except(registered, StringComparer.Ordinal).Order(StringComparer.Ordinal)]);
        }

        private static void ThenTheModuleIsThere(string? problem, string modulesRoot, string context)
        {
            Assert.That(problem, Is.Null,
                $"The {context} context has no start-up module of its own: {problem}. " +
                $"Why it matters: until the context registers its own services, the composition root holds them, and a registration of the context can be dropped, doubled or reordered among hundreds of others without anyone owning it. " +
                $"How to fix: add public static class {ModuleTypeOf(modulesRoot, context)} with a public static Add{context}(this IServiceCollection services, ...) holding the context's registration lines, moved unchanged and in their order, and call it from the composition root.");
        }

        private static void ThenOnlyTheCompositionRootNamesTheModule(List<string> namers, string compositionRoot, string modulesRoot, string context)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(namers, Does.Contain(compositionRoot),
                    $"The composition root {compositionRoot} does not call {ModuleTypeOf(modulesRoot, context)}; the types naming it are [{string.Join(" | ", namers)}]. " +
                    $"Why it matters: a module nobody starts leaves its context's services out of the container, and the application fails when it first asks for one. " +
                    $"How to fix: call Add{context} from the composition root's registration method, at the position that keeps the order of every service registered more than once.");
                Assert.That(namers.Where(namer => namer != compositionRoot), Is.Empty,
                    $"These types name {ModuleTypeOf(modulesRoot, context)} besides the composition root: [{string.Join(" | ", namers.Where(namer => namer != compositionRoot))}]. " +
                    "Why it matters: a second caller registers the context twice or out of order, and a module starting another module hides one context's start-up inside another's. " +
                    $"How to fix: call Add{context} from the composition root only.");
            }
        }

        private static void ThenTheCompositionRootRegistersOnlyTheListed(List<string> unlisted, List<string> gone, string compositionRoot, string context)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(unlisted, Is.Empty,
                    $"The composition root {compositionRoot} still registers or looks up these types of the {context} context: [{string.Join(" | ", unlisted)}]. " +
                    $"Why it matters: the context's start-up is meant to live in its own module, so a registration left in the composition root is one its owner cannot see. " +
                    $"How to fix: move the registration lines into Add{context} unchanged and in their order; only a domain-event handler of the ordered block, or a service the host looks up to set itself up, stays and is listed here.");
                Assert.That(gone, Is.Empty,
                    $"These listed types of the {context} context are no longer registered or looked up by the composition root: [{string.Join(" | ", gone)}]. " +
                    "Why it matters: a list entry the code no longer needs lets the same registration come back unseen. How to fix: delete the entries from the list.");
            }
        }

        private static void ThenOnlyTheEntryTakenOffIsAddedToWhatIsNamed(string entry, (List<string> Unlisted, List<string> Gone) full, (List<string> Unlisted, List<string> Gone) withoutTheEntry)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(withoutTheEntry.Unlisted, Is.EqualTo(full.Unlisted.Append(entry).Order(StringComparer.Ordinal)),
                    $"With {entry} taken off the list, the check named [{string.Join(" | ", withoutTheEntry.Unlisted)}] as not listed. Expected what the full list leaves named, and {entry} besides. " +
                    "Why it matters: an entry the composition root does not register, or one the check never compares, lets the same registration come back unseen once it is deleted. " +
                    "How to fix: keep on the list only what the composition root registers or looks up, and compare the list with the code both ways.");
                Assert.That(withoutTheEntry.Gone, Is.EqualTo(full.Gone),
                    $"With {entry} taken off the list, the check named [{string.Join(" | ", withoutTheEntry.Gone)}] as gone, against [{string.Join(" | ", full.Gone)}] with the full list. " +
                    "Why it matters: taking one entry off must change nothing else. How to fix: compare each entry on its own.");
            }
        }

        private static void ThenEveryWayOfRegisteringIsNamed(List<string> unlisted, List<string> gone)
        {
            var alpha = $"{TheMadeUpModulesRoot}.Alpha.Application";
            string[] expected =
            [
                $"{alpha}.AlphaClock",
                $"{alpha}.AlphaJob",
                $"{alpha}.AlphaOptions",
                $"{alpha}.AlphaRecord",
                $"{alpha}.AlphaRecordReader",
                $"{alpha}.AlphaService",
                $"{alpha}.IAlphaReader`1",
                $"{alpha}.IAlphaService",
            ];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(unlisted, Is.EqualTo(expected),
                    $"The check named [{string.Join(" | ", unlisted)}] as registered by the made-up composition root. Expected exactly the Alpha types it registers with Add, TryAdd and Configure, " +
                    "the types a registered generic type is built from, and the type a registration's factory looks up inside its lambda, each by its full name, and not the type of AlphaPlus, a context whose name begins like Alpha's, nor a type outside the contexts. " +
                    "Why it matters: a way of registering the check does not read lets a registration stay in the composition root unseen, and a check that confuses two contexts reports the wrong owner. " +
                    "How to fix: read every generic argument of the service collection's Add, TryAdd and Configure calls and of GetService and GetRequiredService, in the composition root and its nested types, and match the context by its whole name.");
                Assert.That(gone, Is.Empty, $"The check named [{string.Join(" | ", gone)}] as gone, from an empty list.");
            }
        }

        private static void ThenOnlyTheGoneEntryIsNamed(List<string> unlisted, List<string> gone, string goneEntry)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(gone, Is.EqualTo(new[] { goneEntry }),
                    $"The check named [{string.Join(" | ", gone)}] as gone. Expected exactly the listed type the made-up composition root does not register. " +
                    "Why it matters: a list compared only one way keeps dead entries that hide a later registration. How to fix: compare the list both ways.");
                Assert.That(unlisted, Is.Empty,
                    $"The check named [{string.Join(" | ", unlisted)}] as registered by a made-up composition root that starts Alpha only through its module. " +
                    "Why it matters: what the module registers is the module's, not the composition root's. How to fix: read only the composition root and its nested types.");
            }
        }

        private static void ThenTheCompliantRootIsNotReported(string? problem, List<string> namers, List<string> unlisted, List<string> gone)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(problem, Is.Null, $"The made-up Alpha module was refused: {problem}. Why it matters: a well-formed module must pass, or the check refuses every context. How to fix: accept a public static class with a public static Add method extending the service collection.");
                Assert.That(namers, Does.Contain(ARootStartingAlphaThroughItsModule),
                    $"The made-up composition root that calls AddAlpha was not found among the types naming the Alpha module: [{string.Join(" | ", namers)}]. " +
                    "Why it matters: a check that cannot see the call reports every module as never started. How to fix: read the calls of the module's methods from the IL, lambdas included.");
                Assert.That(unlisted, Is.Empty,
                    $"The check named [{string.Join(" | ", unlisted)}] as registered by a made-up composition root that starts Alpha only through its module and registers a type outside the contexts. " +
                    "Why it matters: a compliant composition root must pass, or the list grows with entries nobody needs. How to fix: report only types of the context, registered by the composition root itself.");
                Assert.That(gone, Is.Empty, $"The check named [{string.Join(" | ", gone)}] as gone, from an empty list.");
            }
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
                $"The check named [{string.Join(" | ", namers)}] as naming the made-up Alpha module. Expected exactly the made-up composition root, a made-up Beta module that starts Alpha inside its own Add method, and a made-up type elsewhere that starts Alpha, and not Alpha's own module nor the composition root that registers Alpha's types itself. " +
                "Why it matters: a check that misses a second caller lets a context be started twice or by another context. How to fix: read every type that references the module, nested types included, and leave out the module itself.");
        }

        private static void ThenTheModuleProblemIsNamed(string? problem, string context, string expectedProblem)
        {
            Assert.That(problem, Does.Contain(expectedProblem).And.Contain(ModuleTypeOf(TheMadeUpModulesRoot, context)),
                $"The check said [{problem ?? "nothing"}] about the made-up {context} context. Expected it to name {ModuleTypeOf(TheMadeUpModulesRoot, context)} and say {expectedProblem}. " +
                "Why it matters: a context whose module is missing, is not a static class, or lacks its Add method extending the service collection cannot be started the way the others are. " +
                "How to fix: look the module up by its full name and check its shape and its Add method.");
        }
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
}

namespace Lighthouse.Backend.Tests.Architecture.CompositionRootFixtures.Modules.AlphaPlus.Application
{
    public sealed class AlphaPlusService
    {
        public int Serve(int days) => days + 5;
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
            services.AddScoped<AlphaPlusService>();
            services.AddSingleton<Shared.AClockOutsideTheContexts>();
        }
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
