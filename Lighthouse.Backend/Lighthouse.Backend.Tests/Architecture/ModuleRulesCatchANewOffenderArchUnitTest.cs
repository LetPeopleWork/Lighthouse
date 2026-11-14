using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using Lighthouse.Backend.ForecastingSharedRulesFixtures.AsyncOnlyCaller;
using Lighthouse.Backend.ForecastingSharedRulesFixtures.FormerCallers;
using Lighthouse.Backend.ForecastingSharedRulesFixtures.ListedCallers;
using Lighthouse.Backend.ForecastingSharedRulesFixtures.Newcomer;
using Lighthouse.Backend.Modules.ForecastingFixtures.SharedRules.Application;
using Lighthouse.Backend.Modules.ForecastingFixtures.SharedRules.Application.Policies;
using Lighthouse.Backend.Modules.ForecastingFixtures.SharedRulesNeighbour.Application;
using Lighthouse.Backend.Modules.ForecastingFixtures.SharedRulesOutbound.Compliant.Application;
using Lighthouse.Backend.Modules.ForecastingFixtures.SharedRulesOutbound.NeighbourOnlyInsideAnAsyncMethod.Application;
using Lighthouse.Backend.Modules.ForecastingFixtures.SharedRulesOutbound.SecondNeighbourType.Application;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The module rules are one shared class that any module can use with its own namespaces. Here the two Application
    /// rules that once lived inside the Flow metrics tests run over a made-up module that is not Flow metrics: who may
    /// reach its Application from outside, and which edges its Application may have into a neighbouring module. A
    /// newcomer, a caller or edge written only inside an async method, and a listed entry that is gone must each fail
    /// and be named, and a set of made-up types that only does what is allowed must pass. The Flow metrics rule tests
    /// themselves call the shared class and keep no rule of their own.
    /// </summary>
    [TestFixture]
    public class ModuleRulesCatchANewOffenderArchUnitTest
    {
        private const string SharedRulesTypeName = "Lighthouse.Backend.Tests.Architecture.ModuleRules";

        private const string FormerFlowMetricsRulesTypeName = "Lighthouse.Backend.Tests.Architecture.FlowMetricsModuleRules";

        private const string InboundRule = "ApplicationIsReachedFromOutsideOnlyBy";

        private const string OutboundRule = "OutboundEdges";

        private const string OutboundVerdictName = "OutboundVerdict";

        private const string TheArchitectureTests = @"^Lighthouse\.Backend\.Tests\.Architecture$";

        private const string MadeUpModule = @"^Lighthouse\.Backend\.Modules\.ForecastingFixtures\.SharedRules(\.|$)";

        private const string MadeUpApplication = @"^Lighthouse\.Backend\.Modules\.ForecastingFixtures\.SharedRules\.Application(\.|$)";

        private const string MadeUpLegalCallers = @"^Lighthouse\.Backend\.ForecastingSharedRulesFixtures\.Api(\.|$)";

        private const string MadeUpCompositionRoot = @"^Lighthouse\.Backend\.ForecastingSharedRulesFixtures\.CompositionRoot\.AForecastCompositionRoot(\+|$)";

        private const string MadeUpNeighbourModule = @"^Lighthouse\.Backend\.Modules\.ForecastingFixtures\.SharedRulesNeighbour(\.|$)";

        private const string MadeUpOutboundRuleText = "A made-up module's Application should reach its neighbour only through the listed edges.";

        private const string Api = "Api";

        private const string CompositionRoot = "CompositionRoot";

        private const string ListedCallers = "ListedCallers";

        private const string DomainCallers = "DomainCallers";

        private const string FormerCallers = "FormerCallers";

        private const string Newcomer = "Newcomer";

        private const string AsyncOnlyCaller = "AsyncOnlyCaller";

        private static readonly string[] CompliantOutside = [Api, CompositionRoot, ListedCallers, DomainCallers];

        private static readonly string[] FlowMetricsRuleTests =
        [
            typeof(FlowMetricsInboundApplicationArchUnitTest).FullName!,
            typeof(FlowMetricsApplicationOutboundArchUnitTest).FullName!,
            typeof(FlowMetricsInboundDomainAndAdaptersArchUnitTest).FullName!,
            typeof(FlowMetricsModuleLayoutArchUnitTest).FullName!,
        ];

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(ModuleRulesCatchANewOffenderArchUnitTest).Assembly));

        private static string ListedCaller => typeof(AListedForecastCaller).FullName!;

        private static string FormerCaller => typeof(AFormerForecastCaller).FullName!;

        private static string NeighbourService => typeof(ANeighbourService).FullName!;

        private static string CompliantEdge => Edge(typeof(ACompliantForecastService), NeighbourService);

        private static string GoneEdge => Edge(typeof(AFormerNeighbourUser), NeighbourService);

        [OneTimeTearDown]
        public void DisposeModule()
        {
            if (TestModule.IsValueCreated)
            {
                TestModule.Value.Dispose();
            }
        }

        [Test]
        public void AnotherModuleWhoseApplicationIsReachedOnlyByAllowedCallers_PassesTheSharedApplicationRule()
        {
            var verdict = WhenTheSharedApplicationRuleRunsOverTheMadeUpModule(CompliantOutside, [ListedCaller]);

            ThenTheRuleFoundExactlyAndPasses(verdict, [ListedCaller]);
        }

        [Test]
        public void AnOutsideCallerOfAnotherModulesApplicationNotOnTheList_FailsTheSharedApplicationRuleAndIsNamed()
        {
            var verdict = WhenTheSharedApplicationRuleRunsOverTheMadeUpModule([.. CompliantOutside, Newcomer], [ListedCaller]);

            ThenTheRuleFailsNamingTheNewcomer(verdict, typeof(ANewcomerReachingTheForecastPolicies).FullName!, typeof(AForecastPolicy).FullName!);
        }

        [Test]
        public void ACallerReachingAnotherModulesApplicationOnlyInsideAnAsyncMethod_FailsTheSharedApplicationRuleAndIsNamed()
        {
            var verdict = WhenTheSharedApplicationRuleRunsOverTheMadeUpModule([.. CompliantOutside, AsyncOnlyCaller], [ListedCaller]);

            ThenTheRuleFailsNamingTheNewcomer(verdict, typeof(ACallerOfTheForecastApplicationOnlyInsideAnAsyncMethod).FullName!, typeof(AForecastAnswerService).FullName!);
        }

        [Test]
        public void AListedCallerThatNoLongerReachesAnotherModulesApplication_FailsTheSharedApplicationRuleAndIsNamedForDeletion()
        {
            var verdict = WhenTheSharedApplicationRuleRunsOverTheMadeUpModule([.. CompliantOutside, FormerCallers], [ListedCaller, FormerCaller]);

            ThenTheRuleFailsNamingTheStaleEntryForDeletion(verdict, FormerCaller);
        }

        [Test]
        public void AnotherModulesApplicationReachingItsNeighbourOnlyThroughAListedEdge_PassesTheSharedEdgeRule()
        {
            var verdict = WhenTheSharedEdgeRuleRunsOverTheMadeUpApplicationOf(typeof(ACompliantForecastService), [CompliantEdge]);

            ThenTheRuleFoundExactlyAndPasses(verdict, [CompliantEdge]);
        }

        [Test]
        public void AnotherModulesListedTypeReachingASecondNeighbourType_FailsTheSharedEdgeRuleAndNamesTheNewEdge()
        {
            var verdict = WhenTheSharedEdgeRuleRunsOverTheMadeUpApplicationOf(typeof(AListedNeighbourUser), [Edge(typeof(AListedNeighbourUser), NeighbourService)]);

            ThenTheRuleFailsNamingTheNewcomer(verdict, Edge(typeof(AListedNeighbourUser), typeof(ASecondNeighbourService).FullName!), reached: null);
        }

        [Test]
        public void AnotherModulesApplicationReachingItsNeighbourOnlyInsideAnAsyncMethod_FailsTheSharedEdgeRuleAndIsNamed()
        {
            var verdict = WhenTheSharedEdgeRuleRunsOverTheMadeUpApplicationOf(typeof(ANeighbourUserOnlyInsideAnAsyncMethod), []);

            ThenTheRuleFailsNamingTheNewcomer(verdict, Edge(typeof(ANeighbourUserOnlyInsideAnAsyncMethod), NeighbourService), reached: null);
        }

        [Test]
        public void AListedEdgeOfAnotherModulesApplicationThatIsGone_FailsTheSharedEdgeRuleAndIsNamedForDeletion()
        {
            var verdict = WhenTheSharedEdgeRuleRunsOverTheMadeUpApplicationOf(typeof(ACompliantForecastService), [CompliantEdge, GoneEdge]);

            ThenTheRuleFailsNamingTheStaleEntryForDeletion(verdict, GoneEdge);
        }

        [Test]
        public void TheFlowMetricsRuleTests_CallTheSharedModuleRules_AndKeepNoRulesClassOrApplicationRuleOfTheirOwn()
        {
            var flowMetricsTestsCallingTheSharedRules = WhenTheFlowMetricsRuleTestsAreReadForWhatTheyCall();

            ThenEveryFlowMetricsRuleTestCallsTheSharedRules(flowMetricsTestsCallingTheSharedRules);
            ThenNoSecondRulesClassOrCopiedApplicationRuleIsLeft();
        }

        private static ObservedVerdict WhenTheSharedApplicationRuleRunsOverTheMadeUpModule(IReadOnlyCollection<string> outsideNamespaces, IReadOnlyCollection<string> listed)
        {
            var verdict = SharedRule(InboundRule, new Dictionary<string, object?>
            {
                ["module"] = TestModule.Value,
                ["listed"] = listed,
                ["moduleNamespaces"] = MadeUpModule,
                ["application"] = MadeUpApplication,
                ["legalCallerNamespaces"] = MadeUpLegalCallers,
                ["compositionRoot"] = MadeUpCompositionRoot,
                ["outside"] = OutsideFixtures(outsideNamespaces),
            });

            var inbound = verdict as InboundVerdict;

            Assert.That(
                inbound,
                Is.Not.Null,
                $"The shared rule {InboundRule} returned {verdict?.GetType().FullName ?? "nothing"} instead of an InboundVerdict. Why it " +
                "matters: callers compare the outside types it found with their list through that verdict. How to fix: return the " +
                "InboundVerdict the Flow metrics Application rule returned before it was moved.");

            return new ObservedVerdict([.. inbound!.Dependents.Select(dependent => dependent.TypeName)], inbound.Check);
        }

        private static ObservedVerdict WhenTheSharedEdgeRuleRunsOverTheMadeUpApplicationOf(Type madeUpApplicationType, IReadOnlyCollection<string> listed)
        {
            var verdict = SharedRule(OutboundRule, new Dictionary<string, object?>
            {
                ["module"] = TestModule.Value,
                ["application"] = ApplicationOf(madeUpApplicationType),
                ["target"] = MadeUpNeighbourModule,
                ["listed"] = listed,
                ["rule"] = MadeUpOutboundRuleText,
            });

            Assert.That(verdict, Is.Not.Null, $"The shared rule {OutboundRule} returned nothing. Why it matters: there is no verdict to check. How to fix: return its OutboundVerdict.");

            var edges = verdict!.GetType().GetProperty("Edges")?.GetValue(verdict) as IEnumerable<string>;
            var check = verdict.GetType().GetMethod("Check", Type.EmptyTypes);

            var notAnEdgeVerdict =
                $"The shared rule {OutboundRule} returned {verdict.GetType().FullName}, which has no Edges list or no Check. Why it matters: " +
                "callers read the edges it found and compare them with their list through that verdict. How to fix: return the " +
                "OutboundVerdict the Flow metrics edge rule returned before it was moved.";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(edges, Is.Not.Null, notAnEdgeVerdict);
                Assert.That(check, Is.Not.Null, notAnEdgeVerdict);
            }

            return new ObservedVerdict([.. edges!], () => Unwrapped(() => check!.Invoke(verdict, null)));
        }

        private static List<string> WhenTheFlowMetricsRuleTestsAreReadForWhatTheyCall()
            => [.. IlDependencies.DependentsIn(TestModule.Value, TheArchitectureTests, type => type.FullName == SharedRulesTypeName)
                .Select(dependent => dependent.TypeName)
                .Where(FlowMetricsRuleTests.Contains)];

        private static void ThenTheRuleFoundExactlyAndPasses(ObservedVerdict verdict, string[] expected)
        {
            Assert.That(
                verdict.Found,
                Is.EquivalentTo(expected),
                $"Expected the shared rule to find exactly [{string.Join(", ", expected)}] in the made-up module. Why it matters: a rule " +
                "that ignores the namespaces it is handed and keeps looking at Flow metrics finds nothing in another module and " +
                "proves nothing by passing. How to fix: let the rule read only the module, Application, callers and neighbour " +
                "namespaces passed to it.");

            Assert.DoesNotThrow(
                verdict.Check,
                "The shared rule failed although the made-up module only did what is allowed. Why it matters: a rule that fails on " +
                "compliant code gets switched off. How to fix: compare what was found with the list, nothing more.");
        }

        private static void ThenTheRuleFailsNamingTheNewcomer(ObservedVerdict verdict, string newcomer, string? reached)
        {
            var failure = Assert.Throws<AssertionException>(
                verdict.Check,
                $"The shared rule passed with {newcomer} not on its list. Why it matters: in a module other than Flow metrics, a new " +
                "dependency would go in unseen, async bodies included. How to fix: report everything the rule finds in the " +
                "namespaces it is handed that is not on the list.");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    failure!.Message,
                    Does.Contain($"Not on the list, and offending: {newcomer}. "),
                    $"The failure must name {newcomer}, and only it, as not on the list, so the reader knows what to change.");
                Assert.That(
                    failure.Message,
                    Does.Not.Contain("no longer offending"),
                    "Every listed entry is still there, so the failure must not ask to delete one from the list.");

                if (reached is not null)
                {
                    Assert.That(
                        failure.Message,
                        Does.Contain($"{newcomer} reaches {reached}"),
                        $"The failure must say that {newcomer} reaches {reached}, so the reader knows which dependency to undo.");
                }
            }
        }

        private static void ThenTheRuleFailsNamingTheStaleEntryForDeletion(ObservedVerdict verdict, string stale)
        {
            var failure = Assert.Throws<AssertionException>(
                verdict.Check,
                $"The shared rule passed with {stale} on its list although it is gone. Why it matters: a list that keeps a gone entry " +
                "lets that dependency come back unseen. How to fix: compare the list with what was found in both directions.");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    failure!.Message,
                    Does.Contain($"On the list, but no longer offending: {stale}. Good - delete it"),
                    $"The failure must name {stale}, and only it, with 'delete', so the reader knows which list entry to remove.");
                Assert.That(
                    failure.Message,
                    Does.Not.Contain("Not on the list"),
                    "Everything found is on the list, so the failure must not name a newcomer.");
            }
        }

        private static void ThenEveryFlowMetricsRuleTestCallsTheSharedRules(List<string> flowMetricsTestsCallingTheSharedRules)
        {
            Assert.That(
                flowMetricsTestsCallingTheSharedRules,
                Is.EquivalentTo(FlowMetricsRuleTests),
                $"Expected every Flow metrics rule test, [{string.Join(", ", FlowMetricsRuleTests)}], to call {SharedRulesTypeName}, found " +
                $"[{string.Join(", ", flowMetricsTestsCallingTheSharedRules)}]. Why it matters: Flow metrics' rules must catch their " +
                "offenders through the same shared rules the next module uses, or the two drift apart. How to fix: call the shared " +
                "rules from each of those tests with Flow metrics' own namespaces.");
        }

        private static void ThenNoSecondRulesClassOrCopiedApplicationRuleIsLeft()
        {
            var testAssembly = typeof(ModuleRulesCatchANewOffenderArchUnitTest).Assembly;
            var copiedRules = FlowMetricsRuleTests
                .Select(testAssembly.GetType)
                .OfType<Type>()
                .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(method => method.Name is InboundRule or OutboundRule)
                    .Select(method => $"{type.FullName}.{method.Name}")
                    .Concat(type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
                        .Where(nested => nested.Name == OutboundVerdictName)
                        .Select(nested => nested.FullName!)))
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    testAssembly.GetType(FormerFlowMetricsRulesTypeName),
                    Is.Null,
                    $"Expected no type {FormerFlowMetricsRulesTypeName}. Why it matters: a second rules class next to the shared one lets " +
                    "Flow metrics and the next module check different rules. How to fix: rename it to the shared module rules class.");
                Assert.That(
                    copiedRules,
                    Is.Empty,
                    $"Expected no Flow metrics rule test to keep its own {InboundRule}, {OutboundRule} or {OutboundVerdictName}, found " +
                    $"[{string.Join(", ", copiedRules)}]. Why it matters: a private copy is checked only for Flow metrics and drifts from " +
                    "the shared rule. How to fix: move it into the shared module rules class and call it from there.");
            }
        }

        private static object? SharedRule(string ruleName, IReadOnlyDictionary<string, object?> arguments)
        {
            var sharedRules = typeof(ModuleRulesCatchANewOffenderArchUnitTest).Assembly.GetType(SharedRulesTypeName);

            Assert.That(
                sharedRules,
                Is.Not.Null,
                $"Expected a shared rules class {SharedRulesTypeName} in the test project, found none. Why it matters: without one " +
                "class any module can use, each module copies the rules and the copies drift apart. How to fix: rename the Flow " +
                "metrics module rules to ModuleRules and move the two Application rules of the Flow metrics tests into it.");

            var rule = sharedRules!.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .SingleOrDefault(method => method.Name == ruleName);

            Assert.That(
                rule,
                Is.Not.Null,
                $"Expected {SharedRulesTypeName} to hold the rule {ruleName}, found none. Why it matters: the next module needs that rule " +
                $"with its own namespaces. How to fix: move {ruleName} out of its Flow metrics test into the shared class.");

            var parameters = rule!.GetParameters();
            var notTaken = arguments.Keys.Except(parameters.Select(parameter => parameter.Name!), StringComparer.Ordinal).ToList();
            var notHandedIn = parameters.Where(parameter => !arguments.ContainsKey(parameter.Name!) && !parameter.HasDefaultValue).Select(parameter => parameter.Name!).ToList();

            var wrongParameters =
                $"Expected {SharedRulesTypeName}.{ruleName} to take exactly [{string.Join(", ", arguments.Keys)}], but it does not take " +
                $"[{string.Join(", ", notTaken)}] and also needs [{string.Join(", ", notHandedIn)}]. Why it matters: a rule that does not " +
                "take the module's namespaces as parameters can only check Flow metrics. How to fix: keep the parameters the rule had " +
                "in its Flow metrics test.";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(notTaken, Is.Empty, wrongParameters);
                Assert.That(notHandedIn, Is.Empty, wrongParameters);
            }

            var values = parameters
                .Select(parameter => arguments.TryGetValue(parameter.Name!, out var value) ? value : parameter.DefaultValue)
                .ToArray();

            return Unwrapped(() => rule.Invoke(null, values));
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

        // The made-up module is always among the types read, so a type inside it that reaches the Application is
        // present and must not count as outside.
        private static string OutsideFixtures(IEnumerable<string> outsideNamespaces)
            => $@"^Lighthouse\.Backend\.(Modules\.ForecastingFixtures\.SharedRules|ForecastingSharedRulesFixtures\.({string.Join('|', outsideNamespaces)}))(\.|$)";

        // Each made-up Application lives in a namespace of its own, so the rule handed one sees only that case.
        private static string ApplicationOf(Type madeUpApplicationType)
        {
            var typeNamespace = madeUpApplicationType.Namespace!;
            var application = typeNamespace[..(typeNamespace.IndexOf(".Application", StringComparison.Ordinal) + ".Application".Length)];

            return $@"^{Regex.Escape(application)}(\.|$)";
        }

        private static string Edge(Type dependent, string reached) => $"{dependent.FullName} -> {reached}";

        /// <summary>What a shared rule found in the made-up module, and the check that compares it with the list.</summary>
        private sealed record ObservedVerdict(IReadOnlyList<string> Found, Action CheckAgainstTheList)
        {
            public void Check() => CheckAgainstTheList();
        }
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.SharedRules.Domain
{
    internal static class AForecastCalculation
    {
        public static int Percentile(int days) => days * 2;
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.SharedRules.Application
{
    internal static class AForecastAnswerService
    {
        public static int Answer(int days) => global::Lighthouse.Backend.Modules.ForecastingFixtures.SharedRules.Domain.AForecastCalculation.Percentile(days);
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.SharedRules.Application.Policies
{
    internal static class AForecastPolicy
    {
        public static int Applied(int days) => global::Lighthouse.Backend.Modules.ForecastingFixtures.SharedRules.Application.AForecastAnswerService.Answer(days);
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.SharedRules.Adapters.Background
{
    internal static class AForecastJob
    {
        public static int Run() => global::Lighthouse.Backend.Modules.ForecastingFixtures.SharedRules.Application.AForecastAnswerService.Answer(21);
    }
}

namespace Lighthouse.Backend.ForecastingSharedRulesFixtures.Api
{
    internal static class AForecastController
    {
        public static int Get() => global::Lighthouse.Backend.Modules.ForecastingFixtures.SharedRules.Application.AForecastAnswerService.Answer(21);
    }
}

namespace Lighthouse.Backend.ForecastingSharedRulesFixtures.CompositionRoot
{
    internal static class AForecastCompositionRoot
    {
        public static int Register() => global::Lighthouse.Backend.Modules.ForecastingFixtures.SharedRules.Application.AForecastAnswerService.Answer(21);

        internal static class Registrations
        {
            public static int Register() => global::Lighthouse.Backend.Modules.ForecastingFixtures.SharedRules.Application.Policies.AForecastPolicy.Applied(21);
        }
    }
}

namespace Lighthouse.Backend.ForecastingSharedRulesFixtures.ListedCallers
{
    internal static class AListedForecastCaller
    {
        public static int Answer() => global::Lighthouse.Backend.Modules.ForecastingFixtures.SharedRules.Application.AForecastAnswerService.Answer(21);
    }
}

namespace Lighthouse.Backend.ForecastingSharedRulesFixtures.DomainCallers
{
    internal static class ACallerOfTheForecastDomainOnly
    {
        public static int Answer() => global::Lighthouse.Backend.Modules.ForecastingFixtures.SharedRules.Domain.AForecastCalculation.Percentile(21);
    }
}

namespace Lighthouse.Backend.ForecastingSharedRulesFixtures.FormerCallers
{
    internal static class AFormerForecastCaller
    {
        public static int Answer() => Math.Max(21, 42);
    }
}

namespace Lighthouse.Backend.ForecastingSharedRulesFixtures.Newcomer
{
    internal static class ANewcomerReachingTheForecastPolicies
    {
        public static int Answer() => global::Lighthouse.Backend.Modules.ForecastingFixtures.SharedRules.Application.Policies.AForecastPolicy.Applied(21);
    }
}

namespace Lighthouse.Backend.ForecastingSharedRulesFixtures.AsyncOnlyCaller
{
    internal sealed class ACallerOfTheForecastApplicationOnlyInsideAnAsyncMethod
    {
        public async Task<int> AnswerAsync()
        {
            await Task.Yield();
            return global::Lighthouse.Backend.Modules.ForecastingFixtures.SharedRules.Application.AForecastAnswerService.Answer(21);
        }
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.SharedRulesNeighbour.Application
{
    internal static class ANeighbourService
    {
        public static int Answer() => 42;
    }

    internal static class ASecondNeighbourService
    {
        public static int Answer() => 21;
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.SharedRulesOutbound.Compliant.Application
{
    internal static class ACompliantForecastService
    {
        public static int Answer() => Policies.ACompliantForecastPolicy.Applied(ANeighbourService.Answer());
    }

    internal static class AFormerNeighbourUser
    {
        public static int Answer() => Math.Max(21, 42);
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.SharedRulesOutbound.Compliant.Application.Policies
{
    internal static class ACompliantForecastPolicy
    {
        public static int Applied(int days) => days + 1;
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.SharedRulesOutbound.SecondNeighbourType.Application
{
    internal static class AListedNeighbourUser
    {
        public static int Answer() => ANeighbourService.Answer() + ASecondNeighbourService.Answer();
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.SharedRulesOutbound.NeighbourOnlyInsideAnAsyncMethod.Application
{
    internal sealed class ANeighbourUserOnlyInsideAnAsyncMethod
    {
        public async Task<int> AnswerAsync()
        {
            await Task.Yield();
            return ANeighbourService.Answer();
        }
    }
}
