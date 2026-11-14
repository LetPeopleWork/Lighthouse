using System.Reflection;
using System.Text.RegularExpressions;
using Lighthouse.Backend.FlowMetricsApplicationOutboundFixtures.Compliant.Application;
using Lighthouse.Backend.FlowMetricsApplicationOutboundFixtures.ForecastingOnlyInsideAnAsyncMethod.Application;
using Lighthouse.Backend.FlowMetricsApplicationOutboundFixtures.HoldingFeatureTransitions.Application;
using Lighthouse.Backend.FlowMetricsApplicationOutboundFixtures.ReadingWorkItemTransitionsOnlyInsideAnAsyncMethod.Application;
using Lighthouse.Backend.FlowMetricsApplicationOutboundFixtures.ReachingTheApi.Application;
using Lighthouse.Backend.FlowMetricsApplicationOutboundFixtures.ReachingTheDatabaseContext.Application;
using Lighthouse.Backend.FlowMetricsApplicationOutboundFixtures.ReachingTheModuleAdapters.Application.Policies;
using Lighthouse.Backend.FlowMetricsApplicationOutboundFixtures.SecondForecastingType.Application;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The Flow metrics module's Application depends on no EF Core, data layer, module adapter or API type, and
    /// reaches Forecasting only through the listed edges, read from the compiled production assembly and compared
    /// with the lists in both directions. The same two rules, with the same forbidden layers and the same Forecasting
    /// namespaces, run on made-up Application types: one holding or reading a transition DbSet, one reaching the
    /// database context, a module adapter or the API, a new Forecasting edge, an edge written only inside an async
    /// method, a listed edge that is gone, and an Application with no type at all must each fail and be named, and a
    /// compliant Application must pass. The hand-written scanner that once checked two services for transition
    /// DbSets is gone, because the first rule checks every Application type for every EF Core type.
    /// </summary>
    [TestFixture]
    public class FlowMetricsApplicationOutboundArchUnitTest
    {
        private const string FlowMetricsApplication = @"^Lighthouse\.Backend\.Modules\.FlowMetrics\.Application(\.|$)";

        private const string EntityFrameworkCore = @"^Microsoft\.EntityFrameworkCore(\.|$)";

        private const string FixtureNamespaces = "Lighthouse.Backend.FlowMetricsApplicationOutboundFixtures";

        private const string TheArchitectureTests = @"^Lighthouse\.Backend\.Tests\.Architecture$";

        private const string RemovedScannerTest = "MetricsServices_ReadTransitions_OnlyViaRepositoryPorts_NeverViaDbSetDirectly";

        private const string RawMethodBody = "System.Reflection.MethodBody";

        private const string AnyTransitionDbSet = "Microsoft.EntityFrameworkCore.DbSet`1";

        private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

        private static readonly string[] MetricsNameRulesThatStay =
        [
            "NoPerStateAggregationServiceExists_InProductionAssembly",
            "ComputeAgeInStatePercentiles_IsProtected_AndNoPublicPerStateAggregationMemberIsExposed",
        ];

        private static readonly string TheLayersBelowTheApplication =
            $"(?:{EntityFrameworkCore})|(?:{ApplicationCoreRules.TheData})|(?:{ModuleRules.FlowMetricsAdapters})|(?:{ApplicationCoreRules.TheApi})";

        // Every edge from the module's Application into Forecasting when the module was cut out. The list only shrinks.
        private static readonly string[] ForecastingEdgesOfTheApplication =
        [
            "Lighthouse.Backend.Modules.FlowMetrics.Application.BaseMetricsService -> Lighthouse.Backend.Services.Interfaces.Forecast.IForecastService",
            "Lighthouse.Backend.Modules.FlowMetrics.Application.TeamMetricsService -> Lighthouse.Backend.Services.Implementation.Forecast.ForecastDataSufficiencyPolicy",
            "Lighthouse.Backend.Modules.FlowMetrics.Application.TeamMetricsService -> Lighthouse.Backend.Services.Interfaces.Forecast.IForecastFilterRuleService",
        ];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(FlowMetricsApplicationOutboundArchUnitTest).Assembly));

        private static readonly Lazy<string> TheForecastingModule = new(ForecastingNamespacesOfTheModuleMap);

        private static string ForecastService => typeof(IForecastService).FullName!;

        private static string ForecastFilterRuleService => typeof(IForecastFilterRuleService).FullName!;

        private static string CompliantEdge => Edge(typeof(ACompliantApplicationService), ForecastService);

        private static string GoneEdge => Edge(typeof(AFormerForecastUser), ForecastService);

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void TheModuleApplication_DependsOnNoEntityFrameworkCoreDataAdaptersOrApiType()
        {
            ApplicationReachesNothingBelowIt(ProductionModule.Value, FlowMetricsApplication, []).Check();
        }

        [Test]
        public void TheModuleApplication_ReachesForecastingOnlyThroughTheListedEdges()
        {
            ApplicationReachesForecastingOnlyThrough(ProductionModule.Value, FlowMetricsApplication, ForecastingEdgesOfTheApplication).Check();
        }

        [Test]
        public void AnApplicationReachingOnlyItsOwnTypesTheModelsAndAListedForecastingEdge_PassesBothRules()
        {
            var below = TheRuleBelowOverMadeUpTypes(typeof(ACompliantApplicationService), []);
            var forecasting = TheForecastingRuleOverMadeUpTypes(typeof(ACompliantApplicationService), [CompliantEdge]);

            ThenTheRuleSawExactlyAndPasses(below, []);
            ThenTheRuleSawExactlyAndPasses(forecasting, [CompliantEdge]);
        }

        [TestCaseSource(nameof(ApplicationTypesReachingALayerBelow))]
        public void AnApplicationTypeReachingALayerBelowIt_FailsTheRuleAndIsNamed(Type offender, string reached)
        {
            var verdict = TheRuleBelowOverMadeUpTypes(offender, []);

            ThenTheRuleFailsNamingOnlyTheNewEdge(verdict, Edge(offender, reached));
        }

        [Test]
        public void AListedEdgeBelowTheApplicationThatIsGone_FailsTheRuleAndIsNamedForDeletion()
        {
            var goneEdgeBelow = Edge(typeof(AFormerForecastUser), AnyTransitionDbSet);

            var verdict = TheRuleBelowOverMadeUpTypes(typeof(ACompliantApplicationService), [goneEdgeBelow]);

            ThenTheRuleFailsNamingOnlyTheStaleEdgeForDeletion(verdict, goneEdgeBelow);
        }

        [Test]
        public void AListedTypeReachingASecondForecastingType_FailsTheRuleAndNamesOnlyTheNewEdge()
        {
            var verdict = TheForecastingRuleOverMadeUpTypes(typeof(AListedForecastUser), [Edge(typeof(AListedForecastUser), ForecastService)]);

            ThenTheRuleFailsNamingOnlyTheNewEdge(verdict, Edge(typeof(AListedForecastUser), ForecastFilterRuleService));
        }

        [Test]
        public void ATypeReachingForecastingOnlyInsideAnAsyncMethod_FailsTheRuleAndIsNamed()
        {
            var verdict = TheForecastingRuleOverMadeUpTypes(typeof(AForecastUserOnlyInsideAnAsyncMethod), []);

            ThenTheRuleFailsNamingOnlyTheNewEdge(verdict, Edge(typeof(AForecastUserOnlyInsideAnAsyncMethod), ForecastService));
        }

        [Test]
        public void AListedForecastingEdgeThatIsGone_FailsTheRuleAndIsNamedForDeletion()
        {
            var verdict = TheForecastingRuleOverMadeUpTypes(typeof(ACompliantApplicationService), [CompliantEdge, GoneEdge]);

            ThenTheRuleFailsNamingOnlyTheStaleEdgeForDeletion(verdict, GoneEdge);
        }

        [Test]
        public void AnApplicationThatHoldsNoType_FailsBothRules()
        {
            var nowhere = $@"^{Regex.Escape(FixtureNamespaces)}\.NothingLivesHere\.Application(\.|$)";

            var below = ApplicationReachesNothingBelowIt(TestModule.Value, nowhere, []);
            var forecasting = ApplicationReachesForecastingOnlyThrough(TestModule.Value, nowhere, []);

            ThenTheRuleFailsBecauseTheApplicationIsEmpty(below, nowhere);
            ThenTheRuleFailsBecauseTheApplicationIsEmpty(forecasting, nowhere);
        }

        [Test]
        public void TheHandWrittenScannerOfTransitionReads_IsGone_AndTheTwoNameRulesOfTheMetricsServicesStay()
        {
            var testsStillNamedLikeTheScanner = TestsInThisAssemblyNamed(RemovedScannerTest);
            var whatTheMetricsRulesReach = WhatTheMetricsRulesReachOfRawMethodBodiesOrEntityFrameworkCore();
            var nameRulesLeft = TestsOfTheMetricsRules(MetricsNameRulesThatStay);

            ThenTheScannerIsGoneAndTheNameRulesStay(testsStillNamedLikeTheScanner, whatTheMetricsRulesReach, nameRulesLeft);
        }

        private static IEnumerable<TestCaseData> ApplicationTypesReachingALayerBelow()
        {
            yield return new TestCaseData(typeof(AHolderOfFeatureTransitions), AnyTransitionDbSet)
                .SetName("AnApplicationTypeHoldingAFeatureTransitionDbSet_FailsTheRuleAndIsNamed");
            yield return new TestCaseData(typeof(AReaderOfWorkItemTransitionsOnlyInsideAnAsyncMethod), AnyTransitionDbSet)
                .SetName("AnApplicationTypeReadingAWorkItemTransitionDbSetOnlyInsideAnAsyncMethod_FailsTheRuleAndIsNamed");
            yield return new TestCaseData(typeof(AUserOfTheDatabaseContext), typeof(global::Lighthouse.Backend.Data.LighthouseAppContext).FullName!)
                .SetName("AnApplicationTypeReachingTheDatabaseContext_FailsTheRuleAndIsNamed");
            yield return new TestCaseData(typeof(AUserOfAModuleAdapter), typeof(global::Lighthouse.Backend.Modules.FlowMetrics.Adapters.Background.OverTimeHistoryFiller).FullName!)
                .SetName("AnApplicationPolicyReachingAModuleAdapter_FailsTheRuleAndIsNamed");
            yield return new TestCaseData(typeof(AUserOfAnApiType), typeof(global::Lighthouse.Backend.API.DTO.EntityReferenceDto).FullName!)
                .SetName("AnApplicationTypeReachingAnApiType_FailsTheRuleAndIsNamed");
        }

        /// <summary>
        /// The Application's edges into EF Core, the data layer, the module's Adapters and the API are exactly the
        /// listed ones, which for the module is none. An Application with no type fails, so a rule pointed at the
        /// wrong namespace cannot pass unseen.
        /// </summary>
        private static OutboundVerdict ApplicationReachesNothingBelowIt(ModuleDefinition module, string application, IReadOnlyCollection<string> listed)
            => OutboundEdges(
                module,
                application,
                TheLayersBelowTheApplication,
                listed,
                $"Types in namespaces matching \"{application}\" should not depend on any type in namespaces matching \"{TheLayersBelowTheApplication}\", " +
                "because the Application decides what the module answers and reads its records only through the repository ports " +
                "the module's Adapters implement. An Application type that holds a DbSet or the database context, uses an adapter " +
                "or an API type, ties the module's answers to how they are stored and shown, so neither can change on its own. " +
                "Read the records through a repository port in the Application, and return a Models type instead of an API one.");

        /// <summary>
        /// The Application's edges into Forecasting are exactly the listed ones: a new edge, also one from a listed type
        /// to a new Forecasting type, fails and is named, and a listed edge that is gone fails until it is deleted.
        /// </summary>
        private static OutboundVerdict ApplicationReachesForecastingOnlyThrough(ModuleDefinition module, string application, IReadOnlyCollection<string> listed)
            => OutboundEdges(
                module,
                application,
                TheForecastingModule.Value,
                listed,
                $"Types in namespaces matching \"{application}\" should reach types in namespaces matching \"{TheForecastingModule.Value}\" only " +
                "through the edges on the list, because Forecasting builds on the module's answers, and every further edge back " +
                "into Forecasting ties the two modules tighter, until neither can change on its own. Take what the module needs " +
                "from Forecasting as a value its caller hands in, or react to a domain event Forecasting publishes; the list only shrinks.");

        private static OutboundVerdict OutboundEdges(ModuleDefinition module, string application, string target, IReadOnlyCollection<string> listed, string rule)
            => new(
                IlDependencies.TypesIn(module, application),
                [.. IlDependencies.DependentsIn(module, application, type => IlDependencies.ResidesInNamespaceMatching(type, target))
                    .SelectMany(dependent => dependent.Targets.Select(reached => $"{dependent.TypeName} -> {reached}"))],
                listed,
                rule,
                application);

        private static OutboundVerdict TheRuleBelowOverMadeUpTypes(Type application, IReadOnlyCollection<string> listed)
            => ApplicationReachesNothingBelowIt(TestModule.Value, ApplicationOf(application), listed);

        private static OutboundVerdict TheForecastingRuleOverMadeUpTypes(Type application, IReadOnlyCollection<string> listed)
            => ApplicationReachesForecastingOnlyThrough(TestModule.Value, ApplicationOf(application), listed);

        // Each made-up Application lives in a namespace of its own, so a rule handed one sees only that case. The
        // Application is the namespace up to its Application part, so a type in one of its sub-namespaces counts too.
        private static string ApplicationOf(Type madeUpApplicationType)
        {
            var typeNamespace = madeUpApplicationType.Namespace!;
            var application = typeNamespace[..(typeNamespace.IndexOf(".Application", StringComparison.Ordinal) + ".Application".Length)];

            return $@"^{Regex.Escape(application)}(\.|$)";
        }

        private static string Edge(Type dependent, string reached) => $"{dependent.FullName} -> {reached}";

        // Read by name from the module map, so the rule matches Forecasting exactly as the map does and this file
        // compiles whatever the visibility of the map's pattern.
        private static string ForecastingNamespacesOfTheModuleMap()
        {
            var pattern = typeof(ModuleBoundariesArchUnitTest)
                .GetField("ForecastingPattern", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?
                .GetRawConstantValue() as string;

            Assert.That(
                pattern,
                Is.Not.Null.And.Not.Empty,
                "The module map no longer defines its Forecasting namespaces under the name ForecastingPattern. Why it matters: the " +
                "Forecasting rule must match Forecasting exactly as the module map does, or the two drift apart. How to fix: keep " +
                "one Forecasting pattern on the module map and let this rule read it.");

            return pattern!;
        }

        private static List<string> TestsInThisAssemblyNamed(string testName)
            => [.. typeof(FlowMetricsApplicationOutboundArchUnitTest).Assembly.GetTypes()
                .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                .Where(method => method.Name == testName)
                .Select(method => $"{method.DeclaringType!.FullName}.{method.Name}")];

        private static List<string> WhatTheMetricsRulesReachOfRawMethodBodiesOrEntityFrameworkCore()
            => [.. IlDependencies.DependentsIn(
                    TestModule.Value,
                    TheArchitectureTests,
                    type => type.FullName == RawMethodBody || IlDependencies.ResidesInNamespaceMatching(type, EntityFrameworkCore))
                .Where(dependent => dependent.TypeName == typeof(MetricsArchitectureTests).FullName)
                .SelectMany(dependent => dependent.Targets)];

        private static List<string> TestsOfTheMetricsRules(IEnumerable<string> testNames)
            => [.. testNames.Where(name => typeof(MetricsArchitectureTests).GetMethod(name)?.GetCustomAttribute<TestAttribute>() is not null)];

        private static void ThenTheRuleSawExactlyAndPasses(OutboundVerdict verdict, string[] expected)
        {
            Assert.That(
                verdict.Edges,
                Is.EquivalentTo(expected),
                $"Expected the rule over {verdict.Application} to find exactly [{string.Join(", ", expected)}]. Why it matters: a rule " +
                "that misses an edge proves nothing by passing, and one that also counts the Application's own types, the Models or " +
                "the base library flags code that is allowed. How to fix: report only edges from an Application type into the " +
                "namespaces the rule is about.");

            Assert.DoesNotThrow(
                verdict.Check,
                $"The rule over {verdict.Application} failed although its Application only reached what it may. Why it matters: a rule " +
                "that fails on compliant code gets switched off. How to fix: compare what was found with the list, nothing more.");
        }

        private static void ThenTheRuleFailsNamingOnlyTheNewEdge(OutboundVerdict verdict, string newEdge)
        {
            var failure = Assert.Throws<AssertionException>(
                verdict.Check,
                $"The rule passed with the edge {newEdge} not on its list. Why it matters: a new dependency of the module's " +
                "Application would go in unseen, async bodies included. How to fix: report every edge from an Application type " +
                "into the namespaces the rule is about that is not on the list.");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    failure!.Message,
                    Does.Contain($"Not on the list, and offending: {newEdge}. "),
                    $"The failure must name the edge {newEdge}, and only it, as not on the list, so the reader knows which dependency to undo.");
                Assert.That(
                    failure.Message,
                    Does.Not.Contain("no longer offending"),
                    "Every listed edge is still there, so the failure must not ask to delete one from the list.");
            }
        }

        private static void ThenTheRuleFailsNamingOnlyTheStaleEdgeForDeletion(OutboundVerdict verdict, string staleEdge)
        {
            var failure = Assert.Throws<AssertionException>(
                verdict.Check,
                $"The rule passed with {staleEdge} on its list although that edge is gone. Why it matters: a list that keeps a " +
                "gone edge lets that dependency come back unseen. How to fix: compare the list with what was found in both directions.");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    failure!.Message,
                    Does.Contain($"On the list, but no longer offending: {staleEdge}. Good - delete it"),
                    $"The failure must name {staleEdge}, and only it, with 'delete', so the reader knows which list entry to remove.");
                Assert.That(
                    failure.Message,
                    Does.Not.Contain("Not on the list"),
                    "Every edge found is on the list, so the failure must not name a new one.");
            }
        }

        private static void ThenTheRuleFailsBecauseTheApplicationIsEmpty(OutboundVerdict verdict, string application)
        {
            var failure = Assert.Throws<AssertionException>(
                verdict.Check,
                $"The rule passed over {application}, which holds no type. Why it matters: a rule pointed at a namespace that was " +
                "renamed or emptied checks nothing and stays green. How to fix: fail when the Application holds no type.");

            Assert.That(
                failure!.Message,
                Does.Contain($"No type lives in a namespace matching \"{application}\""),
                $"The failure must say that no type lives in {application}, so the reader knows the rule checked nothing.");
        }

        private static void ThenTheScannerIsGoneAndTheNameRulesStay(List<string> testsStillNamedLikeTheScanner, List<string> whatTheMetricsRulesReach, List<string> nameRulesLeft)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    testsStillNamedLikeTheScanner,
                    Is.Empty,
                    $"Expected no test named {RemovedScannerTest}, found [{string.Join(", ", testsStillNamedLikeTheScanner)}]. Why it " +
                    "matters: it checked two named services for transition DbSets by reading raw IL bytes by hand, and the rule that " +
                    "no Application type reaches any EF Core type now checks that for the whole Application. How to fix: delete " +
                    "that test.");
                Assert.That(
                    whatTheMetricsRulesReach,
                    Is.Empty,
                    $"Expected the Metrics rules to reach neither raw method bodies nor EF Core, found [{string.Join(", ", whatTheMetricsRulesReach)}]. " +
                    "Why it matters: those are the opcode scanner's helpers and its list of transition DbSets, and a scanner left " +
                    "behind without its test is dead code that looks like a guard. How to fix: delete the scanner's helpers and " +
                    "its DbSet list with the test.");
                Assert.That(
                    nameRulesLeft,
                    Is.EquivalentTo(MetricsNameRulesThatStay),
                    $"Expected the two name rules of the metrics services, [{string.Join(", ", MetricsNameRulesThatStay)}], to stay as tests, " +
                    $"found [{string.Join(", ", nameRulesLeft)}]. Why it matters: they guard the shared per-state helper, which no " +
                    "namespace rule checks. How to fix: delete only the scanner, not the name rules.");
            }
        }

        /// <summary>
        /// The edges a rule found from an Application into the namespaces it is about, as "Dependent -> Target",
        /// and the list of the allowed ones. <see cref="Check"/> fails when the Application holds no type, and
        /// otherwise compares the two both ways.
        /// </summary>
        private sealed record OutboundVerdict(IReadOnlyList<string> Examined, IReadOnlyList<string> Edges, IReadOnlyCollection<string> Listed, string Rule, string Application)
        {
            public void Check()
            {
                if (Examined.Count == 0)
                {
                    Assert.Fail(
                        $"{Rule}{Environment.NewLine}No type lives in a namespace matching \"{Application}\", so the rule checked nothing. " +
                        "A namespace that was renamed or emptied leaves the rule green while it guards nothing. Point the rule at " +
                        "the namespace where the module's Application lives now.");
                }

                InlineBaseline.AssertMatches(Edges, Listed, Rule);
            }
        }
    }
}

namespace Lighthouse.Backend.FlowMetricsApplicationOutboundFixtures.Compliant.Application
{
    /// <summary>
    /// Reaches what an Application may: a type of its own Application, a Models type, the base library, the
    /// service provider, and Forecasting through the one edge on the list.
    /// </summary>
    internal sealed class ACompliantApplicationService(IServiceProvider serviceProvider)
    {
        public bool CanForecast(FeatureStateTransition transition)
            => Policies.ACompliantPolicy.Applies(transition.ToState)
                && global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IForecastService>(serviceProvider) is not null;
    }

    internal static class AFormerForecastUser
    {
        public static int Answer() => Math.Max(21, 42);
    }
}

namespace Lighthouse.Backend.FlowMetricsApplicationOutboundFixtures.Compliant.Application.Policies
{
    internal static class ACompliantPolicy
    {
        public static bool Applies(string state) => state.Length > 0;
    }
}

namespace Lighthouse.Backend.FlowMetricsApplicationOutboundFixtures.HoldingFeatureTransitions.Application
{
    internal sealed class AHolderOfFeatureTransitions(global::Microsoft.EntityFrameworkCore.DbSet<FeatureStateTransition> transitions)
    {
        public bool HasAny() => transitions.Any();
    }
}

namespace Lighthouse.Backend.FlowMetricsApplicationOutboundFixtures.ReadingWorkItemTransitionsOnlyInsideAnAsyncMethod.Application
{
    internal sealed class AReaderOfWorkItemTransitionsOnlyInsideAnAsyncMethod
    {
        public async Task<int> CountAsync(object source)
        {
            await Task.Yield();
            var transitions = (global::Microsoft.EntityFrameworkCore.DbSet<WorkItemStateTransition>)source;
            return transitions.GetHashCode();
        }
    }
}

namespace Lighthouse.Backend.FlowMetricsApplicationOutboundFixtures.ReachingTheDatabaseContext.Application
{
    internal static class AUserOfTheDatabaseContext
    {
        public static string ContextName() => typeof(global::Lighthouse.Backend.Data.LighthouseAppContext).Name;
    }
}

namespace Lighthouse.Backend.FlowMetricsApplicationOutboundFixtures.ReachingTheModuleAdapters.Application.Policies
{
    internal static class AUserOfAModuleAdapter
    {
        public static string AdapterName() => typeof(global::Lighthouse.Backend.Modules.FlowMetrics.Adapters.Background.OverTimeHistoryFiller).Name;
    }
}

namespace Lighthouse.Backend.FlowMetricsApplicationOutboundFixtures.ReachingTheApi.Application
{
    internal static class AUserOfAnApiType
    {
        public static string Answer() => new global::Lighthouse.Backend.API.DTO.EntityReferenceDto(21, "Team").Name;
    }
}

namespace Lighthouse.Backend.FlowMetricsApplicationOutboundFixtures.SecondForecastingType.Application
{
    internal sealed class AListedForecastUser(IForecastService forecastService, IForecastFilterRuleService filterRuleService)
    {
        public bool IsReady() => forecastService is not null && filterRuleService is not null;
    }
}

namespace Lighthouse.Backend.FlowMetricsApplicationOutboundFixtures.ForecastingOnlyInsideAnAsyncMethod.Application
{
    internal sealed class AForecastUserOnlyInsideAnAsyncMethod(IServiceProvider serviceProvider)
    {
        public async Task<bool> IsReadyAsync()
        {
            await Task.Yield();
            return global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IForecastService>(serviceProvider) is not null;
        }
    }
}
