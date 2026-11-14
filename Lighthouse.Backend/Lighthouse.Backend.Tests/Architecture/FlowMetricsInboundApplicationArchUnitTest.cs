using Lighthouse.Backend.FlowMetricsApplicationInboundFixtures.AsyncOnlyCaller;
using Lighthouse.Backend.FlowMetricsApplicationInboundFixtures.CompositionRoot;
using Lighthouse.Backend.FlowMetricsApplicationInboundFixtures.FormerCallers;
using Lighthouse.Backend.FlowMetricsApplicationInboundFixtures.ListedCallers;
using Lighthouse.Backend.FlowMetricsApplicationInboundFixtures.Newcomer;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.FlowMetricsFixtures.ApplicationInbound.Application;
using Lighthouse.Backend.Modules.FlowMetricsFixtures.ApplicationInbound.Application.Policies;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Code outside the Flow metrics module reaches the module's Application only from the API, the composition root,
    /// Reports, Refinement and the listed types, read from the compiled production assembly and compared with the list
    /// in both directions. The same rule runs on made-up types: an outside type that is not on the list, one that
    /// reaches the Application only inside an async method, and a listed type that no longer reaches it must each fail
    /// and be named, and a set where only allowed callers reach the Application must pass.
    /// </summary>
    [TestFixture]
    public class FlowMetricsInboundApplicationArchUnitTest
    {
        private const string FlowMetricsApplication = @"^Lighthouse\.Backend\.Modules\.FlowMetrics\.Application(\.|$)";

        private const string TheCompositionRoot = @"^Lighthouse\.Backend\.Program(\+|$)";

        private const string FixtureModuleNamespaces = @"^Lighthouse\.Backend\.Modules\.FlowMetricsFixtures\.ApplicationInbound(\.|$)";

        private const string FixtureApplication = @"^Lighthouse\.Backend\.Modules\.FlowMetricsFixtures\.ApplicationInbound\.Application(\.|$)";

        private const string FixtureLegalCallerNamespaces = @"^Lighthouse\.Backend\.FlowMetricsApplicationInboundFixtures\.Api(\.|$)";

        private const string FixtureCompositionRoot = @"^Lighthouse\.Backend\.FlowMetricsApplicationInboundFixtures\.CompositionRoot\.ACompositionRoot(\+|$)";

        private const string Api = "Api";

        private const string CompositionRoot = "CompositionRoot";

        private const string ListedCallers = "ListedCallers";

        private const string DomainCallers = "DomainCallers";

        private const string FormerCallers = "FormerCallers";

        private const string Newcomer = "Newcomer";

        private const string AsyncOnlyCaller = "AsyncOnlyCaller";

        // The API, Reports and Refinement call the module's Application by design.
        private static readonly string TheLegalCallerNamespaces =
            $"(?:{ApplicationCoreRules.TheApi})|(?:{ModuleBoundariesArchUnitTest.ReportsPattern})|(?:{ModuleBoundariesArchUnitTest.RefinementPattern})";

        // Every other outside type that reached the module's Application when the module was cut out. The list only shrinks.
        private static readonly string[] OtherOutsideTypesReachingTheApplication =
        [
            "Lighthouse.Backend.Services.Implementation.DatabaseManagement.DatabaseMaintenanceGate",
            "Lighthouse.Backend.Services.Implementation.FeatureListActiveWork",
            "Lighthouse.Backend.Modules.Forecasting.Application.ForecastInputFingerprint",
            "Lighthouse.Backend.Modules.Forecasting.Application.ForecastInputs",
            "Lighthouse.Backend.Modules.Forecasting.Application.ForecastRealityCheckService",
            "Lighthouse.Backend.Modules.Forecasting.Application.ForecastService",
            "Lighthouse.Backend.Modules.Forecasting.Application.ForecastService+GatheredInputs",
            "Lighthouse.Backend.Services.Implementation.TeamData.TeamDataService",
            "Lighthouse.Backend.Services.Implementation.WorkItems.WorkItemService",
            "Lighthouse.Backend.Services.Implementation.WriteBackTriggerService",
        ];

        // A legal caller by namespace, the composition root with a hand-written nested type, a listed caller, and an
        // outside type that reaches only the module's Domain.
        private static readonly string[] CompliantOutside = [Api, CompositionRoot, ListedCallers, DomainCallers];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        private static readonly Lazy<ModuleDefinition> FixtureModule = new(() => IlDependencies.ReadModule(typeof(FlowMetricsInboundApplicationArchUnitTest).Assembly));

        private static string ListedCaller => typeof(AListedApplicationCaller).FullName!;

        private static string FormerCaller => typeof(AFormerApplicationCaller).FullName!;

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, FixtureModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void TheModuleApplication_IsReachedFromOutsideOnlyByTheApiTheCompositionRootReportsRefinementAndTheListedTypes()
        {
            ModuleRules.ApplicationIsReachedFromOutsideOnlyBy(
                ProductionModule.Value,
                OtherOutsideTypesReachingTheApplication,
                ModuleRules.FlowMetricsModule,
                FlowMetricsApplication,
                TheLegalCallerNamespaces,
                TheCompositionRoot,
                ModuleRules.AnyNamespace).Check();
        }

        [Test]
        public void OnlyAllowedCallersReachingTheApplication_PassTheRule()
        {
            var verdict = TheRuleOverMadeUpTypes(CompliantOutside, [ListedCaller, ListedCaller]);

            ThenTheRuleSawExactlyAndPasses(verdict, [ListedCaller]);
        }

        [Test]
        public void NoOutsideTypeReachingTheApplicationAndAnEmptyList_PassTheRule()
        {
            var verdict = TheRuleOverMadeUpTypes([DomainCallers, FormerCallers], []);

            ThenTheRuleSawExactlyAndPasses(verdict, []);
        }

        [Test]
        public void AnOutsideTypeNotOnTheList_FailsTheRuleAndIsNamed()
        {
            var verdict = TheRuleOverMadeUpTypes([.. CompliantOutside, Newcomer], [ListedCaller]);

            ThenTheRuleFailsNamingOnlyTheNewcomer(verdict, typeof(ANewcomerReachingThePolicies).FullName!, typeof(APolicy).FullName!);
        }

        [Test]
        public void AnOutsideTypeReachingTheApplicationOnlyInsideAnAsyncMethod_FailsTheRuleAndIsNamed()
        {
            var verdict = TheRuleOverMadeUpTypes([.. CompliantOutside, AsyncOnlyCaller], [ListedCaller]);

            ThenTheRuleFailsNamingOnlyTheNewcomer(verdict, typeof(ACallerOfTheApplicationOnlyInsideAnAsyncMethod).FullName!, typeof(AnApplicationService).FullName!);
        }

        [Test]
        public void AListedTypeThatNoLongerReachesTheApplication_FailsTheRuleAndIsNamedForDeletion()
        {
            var verdict = TheRuleOverMadeUpTypes([.. CompliantOutside, FormerCallers], [ListedCaller, FormerCaller]);

            ThenTheRuleFailsNamingOnlyTheStaleEntryForDeletion(verdict, FormerCaller);
        }

        [Test]
        public void ANewcomerAndAStaleEntryAtOnce_AreBothNamedInOneFailure()
        {
            var verdict = TheRuleOverMadeUpTypes([Api, CompositionRoot, Newcomer, FormerCallers], [FormerCaller]);

            ThenTheRuleFailsNamingTheNewcomerAndTheStaleEntry(verdict, typeof(ANewcomerReachingThePolicies).FullName!, FormerCaller);
        }

        private static InboundVerdict TheRuleOverMadeUpTypes(IReadOnlyCollection<string> outsideNamespaces, IReadOnlyCollection<string> listed)
            => ModuleRules.ApplicationIsReachedFromOutsideOnlyBy(
                FixtureModule.Value,
                listed,
                FixtureModuleNamespaces,
                FixtureApplication,
                FixtureLegalCallerNamespaces,
                FixtureCompositionRoot,
                OutsideFixtures(outsideNamespaces));

        // The made-up module is always among the types read, so a type inside it that reaches the Application is
        // present and must not count as outside.
        private static string OutsideFixtures(IEnumerable<string> outsideNamespaces)
            => $@"^Lighthouse\.Backend\.(Modules\.FlowMetricsFixtures\.ApplicationInbound|FlowMetricsApplicationInboundFixtures\.({string.Join('|', outsideNamespaces)}))(\.|$)";

        private static void ThenTheRuleSawExactlyAndPasses(InboundVerdict verdict, string[] expected)
        {
            Assert.That(
                verdict.Dependents.Select(dependent => dependent.TypeName),
                Is.EquivalentTo(expected),
                $"Expected the rule to find exactly [{string.Join(", ", expected)}] reaching the made-up module's Application. Why it " +
                "matters: a rule that misses a caller proves nothing by passing, and one that also counts a type inside the module, " +
                "a type in a legal caller's namespace, the composition root or its nested types, or an outside type that reaches " +
                "only the Domain, flags code that is allowed. How to fix: report only types outside the module, the legal callers " +
                "and the composition root that reference a type in its Application.");

            Assert.DoesNotThrow(
                verdict.Check,
                "The Application rule failed although only allowed callers reached it. Why it matters: a rule that fails on " +
                "compliant code gets switched off. How to fix: compare what was found with the list, nothing more.");
        }

        private static void ThenTheRuleFailsNamingOnlyTheNewcomer(InboundVerdict verdict, string newcomer, string reached)
        {
            var failure = Assert.Throws<AssertionException>(
                verdict.Check,
                $"The Application rule passed with {newcomer} reaching it from outside without being on the list. Why it matters: " +
                "a new caller of the module's Application would go in unseen, async bodies included. How to fix: report every " +
                "type outside the module and the legal callers that references a type in its Application and is not on the list.");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    failure!.Message,
                    Does.Contain($"Not on the list, and offending: {newcomer}. "),
                    $"The failure must name {newcomer}, and only it, as not on the list, so the reader knows which type to change.");
                Assert.That(
                    failure.Message,
                    Does.Contain($"{newcomer} reaches {reached}"),
                    $"The failure must say that {newcomer} reaches {reached}, so the reader knows which dependency to undo.");
                Assert.That(
                    failure.Message,
                    Does.Not.Contain("no longer offending"),
                    "The listed type still reaches the Application, so the failure must not ask to delete it from the list.");
            }
        }

        private static void ThenTheRuleFailsNamingOnlyTheStaleEntryForDeletion(InboundVerdict verdict, string stale)
        {
            var failure = Assert.Throws<AssertionException>(
                verdict.Check,
                $"The Application rule passed with {stale} on its list although it no longer reaches the Application. Why it " +
                "matters: a list that keeps a gone entry lets that dependency come back unseen. How to fix: compare the list " +
                "with what was found in both directions.");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    failure!.Message,
                    Does.Contain($"On the list, but no longer offending: {stale}. Good - delete it"),
                    $"The failure must name {stale}, and only it, with 'delete', so the reader knows which list entry to remove.");
                Assert.That(
                    failure.Message,
                    Does.Not.Contain("Not on the list"),
                    "Every outside type that reaches the Application is on the list, so the failure must not name a newcomer.");
            }
        }

        private static void ThenTheRuleFailsNamingTheNewcomerAndTheStaleEntry(InboundVerdict verdict, string newcomer, string stale)
        {
            var failure = Assert.Throws<AssertionException>(
                verdict.Check,
                $"The Application rule passed with {newcomer} reaching it unlisted and {stale} listed but gone. Why it matters: " +
                "either one alone hides a change to who calls the module. How to fix: compare the list with what was found in " +
                "both directions and report both differences.");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    failure!.Message,
                    Does.Contain($"Not on the list, and offending: {newcomer}. "),
                    $"The failure must name {newcomer} as not on the list.");
                Assert.That(
                    failure.Message,
                    Does.Contain($"On the list, but no longer offending: {stale}. Good - delete it"),
                    $"The same failure must also name {stale} with 'delete', so one run shows both list changes.");
            }
        }
    }
}

namespace Lighthouse.Backend.Modules.FlowMetricsFixtures.ApplicationInbound.Domain
{
    internal static class ADomainCalculation
    {
        public static int Doubled(int days) => days * 2;
    }
}

namespace Lighthouse.Backend.Modules.FlowMetricsFixtures.ApplicationInbound.Application
{
    internal static class AnApplicationService
    {
        public static int Answer(int days) => global::Lighthouse.Backend.Modules.FlowMetricsFixtures.ApplicationInbound.Domain.ADomainCalculation.Doubled(days);
    }
}

namespace Lighthouse.Backend.Modules.FlowMetricsFixtures.ApplicationInbound.Application.Policies
{
    internal static class APolicy
    {
        public static int Applied(int days) => global::Lighthouse.Backend.Modules.FlowMetricsFixtures.ApplicationInbound.Application.AnApplicationService.Answer(days);
    }
}

namespace Lighthouse.Backend.Modules.FlowMetricsFixtures.ApplicationInbound.Adapters.Background
{
    internal static class ABackgroundJob
    {
        public static int Run() => global::Lighthouse.Backend.Modules.FlowMetricsFixtures.ApplicationInbound.Application.AnApplicationService.Answer(21);
    }
}

namespace Lighthouse.Backend.FlowMetricsApplicationInboundFixtures.Api
{
    internal static class AController
    {
        public static int Get() => global::Lighthouse.Backend.Modules.FlowMetricsFixtures.ApplicationInbound.Application.AnApplicationService.Answer(21);
    }
}

namespace Lighthouse.Backend.FlowMetricsApplicationInboundFixtures.CompositionRoot
{
    internal static class ACompositionRoot
    {
        public static int Register() => global::Lighthouse.Backend.Modules.FlowMetricsFixtures.ApplicationInbound.Application.AnApplicationService.Answer(21);

        internal static class Registrations
        {
            public static int Register() => global::Lighthouse.Backend.Modules.FlowMetricsFixtures.ApplicationInbound.Application.Policies.APolicy.Applied(21);
        }
    }
}

namespace Lighthouse.Backend.FlowMetricsApplicationInboundFixtures.ListedCallers
{
    internal static class AListedApplicationCaller
    {
        public static int Answer() => global::Lighthouse.Backend.Modules.FlowMetricsFixtures.ApplicationInbound.Application.AnApplicationService.Answer(21);
    }
}

namespace Lighthouse.Backend.FlowMetricsApplicationInboundFixtures.DomainCallers
{
    internal static class ACallerOfTheDomainOnly
    {
        public static int Answer() => global::Lighthouse.Backend.Modules.FlowMetricsFixtures.ApplicationInbound.Domain.ADomainCalculation.Doubled(21);
    }
}

namespace Lighthouse.Backend.FlowMetricsApplicationInboundFixtures.FormerCallers
{
    internal static class AFormerApplicationCaller
    {
        public static int Answer() => Math.Max(21, 42);
    }
}

namespace Lighthouse.Backend.FlowMetricsApplicationInboundFixtures.Newcomer
{
    internal static class ANewcomerReachingThePolicies
    {
        public static int Answer() => global::Lighthouse.Backend.Modules.FlowMetricsFixtures.ApplicationInbound.Application.Policies.APolicy.Applied(21);
    }
}

namespace Lighthouse.Backend.FlowMetricsApplicationInboundFixtures.AsyncOnlyCaller
{
    internal sealed class ACallerOfTheApplicationOnlyInsideAnAsyncMethod
    {
        public async Task<int> AnswerAsync()
        {
            await Task.Yield();
            return global::Lighthouse.Backend.Modules.FlowMetricsFixtures.ApplicationInbound.Application.AnApplicationService.Answer(21);
        }
    }
}
