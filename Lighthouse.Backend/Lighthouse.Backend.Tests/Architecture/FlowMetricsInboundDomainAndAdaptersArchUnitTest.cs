using Lighthouse.Backend.FlowMetricsInboundFixtures.AsyncOnlyCallerOfTheAdapters;
using Lighthouse.Backend.FlowMetricsInboundFixtures.AsyncOnlyCallerOfTheDomain;
using Lighthouse.Backend.FlowMetricsInboundFixtures.CompositionRoot;
using Lighthouse.Backend.FlowMetricsInboundFixtures.FormerCallers;
using Lighthouse.Backend.FlowMetricsInboundFixtures.ListedCallers;
using Lighthouse.Backend.FlowMetricsInboundFixtures.NewcomerToTheAdapters;
using Lighthouse.Backend.FlowMetricsInboundFixtures.NewcomerToTheDomain;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.FlowMetricsFixtures.InboundRules.Adapters.Persistence;
using Lighthouse.Backend.Modules.FlowMetricsFixtures.InboundRules.Domain;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Code outside the Flow metrics module reaches the module's Domain only from the listed types, and its Adapters
    /// only from the composition root that registers them. Both are read from the compiled production assembly and
    /// compared with their list in both directions. The same rules run on made-up types: an outside type that is not
    /// on the list, one that reaches the layer only inside an async method, and a listed type that no longer reaches
    /// it must each fail and be named, and a set where only the allowed type reaches the layer must pass.
    /// </summary>
    [TestFixture]
    public class FlowMetricsInboundDomainAndAdaptersArchUnitTest
    {
        private const string FixtureModuleNamespaces = @"^Lighthouse\.Backend\.Modules\.FlowMetricsFixtures\.InboundRules(\.|$)";

        private const string FixtureDomain = @"^Lighthouse\.Backend\.Modules\.FlowMetricsFixtures\.InboundRules\.Domain(\.|$)";

        private const string FixtureAdapters = @"^Lighthouse\.Backend\.Modules\.FlowMetricsFixtures\.InboundRules\.Adapters(\.|$)";

        private const string ListedCallers = "ListedCallers";

        private const string CompositionRoot = "CompositionRoot";

        private const string ApplicationCallers = "ApplicationCallers";

        private const string FormerCallers = "FormerCallers";

        // Every outside type that reached the module's Domain when the module was cut out. The list only shrinks.
        private static readonly string[] OutsideTypesReachingTheDomain =
        [
            "Lighthouse.Backend.API.PortfolioController",
            "Lighthouse.Backend.API.PortfoliosController",
            "Lighthouse.Backend.API.TeamController",
            "Lighthouse.Backend.API.TeamsController",
            "Lighthouse.Backend.Services.Implementation.Reports.Metrics.DailySeriesSummary",
            "Lighthouse.Backend.Services.Implementation.WorkItems.WorkItemService",
        ];

        // The allowed outside type, the type it reaches, and a type in the module's Application that reaches the
        // layer too and is not outside, together with an outside type that reaches only the Application.
        private static readonly string[] AllowedOutside = [ListedCallers, CompositionRoot, ApplicationCallers];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        private static readonly Lazy<ModuleDefinition> FixtureModule = new(() => IlDependencies.ReadModule(typeof(FlowMetricsInboundDomainAndAdaptersArchUnitTest).Assembly));

        public enum ReachedLayer
        {
            Domain,
            Adapters,
        }

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, FixtureModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void TheModuleDomain_IsReachedFromOutsideOnlyByTheListedTypes()
        {
            ModuleRules.DomainIsReachedFromOutsideOnlyBy(ProductionModule.Value, OutsideTypesReachingTheDomain).Check();
        }

        [Test]
        public void TheModuleAdapters_AreReachedFromOutsideOnlyByTheCompositionRoot()
        {
            ModuleRules.AdaptersAreReachedFromOutsideOnlyByTheCompositionRoot(ProductionModule.Value).Check();
        }

        [TestCase(ReachedLayer.Domain)]
        [TestCase(ReachedLayer.Adapters)]
        public void OnlyTheAllowedOutsideTypeReachingTheLayer_PassesTheRule(ReachedLayer layer)
        {
            var verdict = TheRuleOverMadeUpTypes(layer, AllowedOutside, AllowedType(layer));

            ThenTheRuleSawOnlyTheAllowedTypeAndPasses(verdict, AllowedType(layer), layer);
        }

        [TestCase(ReachedLayer.Domain)]
        [TestCase(ReachedLayer.Adapters)]
        public void AnOutsideTypeNotOnTheList_FailsTheRuleAndIsNamed(ReachedLayer layer)
        {
            var verdict = TheRuleOverMadeUpTypes(layer, [.. AllowedOutside, NewcomerNamespace(layer)], AllowedType(layer));

            ThenTheRuleFailsNamingTheNewcomer(verdict, Newcomer(layer), ReachedType(layer), layer);
        }

        [TestCase(ReachedLayer.Domain)]
        [TestCase(ReachedLayer.Adapters)]
        public void AnOutsideTypeReachingTheLayerOnlyInsideAnAsyncMethod_FailsTheRuleAndIsNamed(ReachedLayer layer)
        {
            var verdict = TheRuleOverMadeUpTypes(layer, [.. AllowedOutside, AsyncOnlyCallerNamespace(layer)], AllowedType(layer));

            ThenTheRuleFailsNamingTheNewcomer(verdict, AsyncOnlyCaller(layer), ReachedType(layer), layer);
        }

        [TestCase(ReachedLayer.Domain)]
        [TestCase(ReachedLayer.Adapters)]
        public void AListedTypeThatNoLongerReachesTheLayer_FailsTheRuleAndIsNamedForDeletion(ReachedLayer layer)
        {
            var verdict = TheRuleOverMadeUpTypesListingAFormerCaller(layer);

            ThenTheRuleFailsNamingTheStaleEntryForDeletion(verdict, typeof(AFormerCaller).FullName!, layer);
        }

        private static InboundVerdict TheRuleOverMadeUpTypes(ReachedLayer layer, IReadOnlyCollection<string> outsideNamespaces, string allowed) => layer switch
        {
            ReachedLayer.Domain => ModuleRules.DomainIsReachedFromOutsideOnlyBy(FixtureModule.Value, [allowed], FixtureModuleNamespaces, FixtureDomain, OutsideFixtures(outsideNamespaces)),
            ReachedLayer.Adapters => ModuleRules.AdaptersAreReachedFromOutsideOnlyByTheCompositionRoot(FixtureModule.Value, allowed, FixtureModuleNamespaces, FixtureAdapters, OutsideFixtures(outsideNamespaces)),
            _ => throw new ArgumentOutOfRangeException(nameof(layer)),
        };

        // The Domain list keeps its caller and also names a type that stopped reaching the Domain; the composition
        // root named is one that registers nothing any more, while the real one is left out of the outside types.
        private static InboundVerdict TheRuleOverMadeUpTypesListingAFormerCaller(ReachedLayer layer) => layer switch
        {
            ReachedLayer.Domain => ModuleRules.DomainIsReachedFromOutsideOnlyBy(
                FixtureModule.Value,
                [typeof(AListedDomainCaller).FullName!, typeof(AFormerCaller).FullName!],
                FixtureModuleNamespaces,
                FixtureDomain,
                OutsideFixtures([.. AllowedOutside, FormerCallers])),
            ReachedLayer.Adapters => ModuleRules.AdaptersAreReachedFromOutsideOnlyByTheCompositionRoot(
                FixtureModule.Value,
                typeof(AFormerCaller).FullName!,
                FixtureModuleNamespaces,
                FixtureAdapters,
                OutsideFixtures([ListedCallers, ApplicationCallers, FormerCallers])),
            _ => throw new ArgumentOutOfRangeException(nameof(layer)),
        };

        // The made-up module is always among the types read, so a type inside it that reaches the layer is present
        // and must not count as outside.
        private static string OutsideFixtures(IEnumerable<string> outsideNamespaces)
            => $@"^Lighthouse\.Backend\.(Modules\.FlowMetricsFixtures\.InboundRules|FlowMetricsInboundFixtures\.({string.Join('|', outsideNamespaces)}))(\.|$)";

        private static string AllowedType(ReachedLayer layer) => layer == ReachedLayer.Domain ? typeof(AListedDomainCaller).FullName! : typeof(ACompositionRoot).FullName!;

        private static string ReachedType(ReachedLayer layer) => layer == ReachedLayer.Domain ? typeof(ADomainCalculation).FullName! : typeof(AnAdapter).FullName!;

        private static string NewcomerNamespace(ReachedLayer layer) => layer == ReachedLayer.Domain ? "NewcomerToTheDomain" : "NewcomerToTheAdapters";

        private static string Newcomer(ReachedLayer layer) => layer == ReachedLayer.Domain ? typeof(ANewcomerReachingTheDomain).FullName! : typeof(ANewcomerReachingTheAdapters).FullName!;

        private static string AsyncOnlyCallerNamespace(ReachedLayer layer) => layer == ReachedLayer.Domain ? "AsyncOnlyCallerOfTheDomain" : "AsyncOnlyCallerOfTheAdapters";

        private static string AsyncOnlyCaller(ReachedLayer layer) => layer == ReachedLayer.Domain ? typeof(ACallerOfTheDomainOnlyInsideAnAsyncMethod).FullName! : typeof(ACallerOfTheAdaptersOnlyInsideAnAsyncMethod).FullName!;

        private static void ThenTheRuleSawOnlyTheAllowedTypeAndPasses(InboundVerdict verdict, string allowed, ReachedLayer layer)
        {
            Assert.That(
                verdict.Dependents.Select(dependent => dependent.TypeName),
                Is.EqualTo(new[] { allowed }),
                $"Expected the rule to find exactly {allowed} reaching the made-up module's {layer}. Why it matters: a rule that " +
                "misses the allowed caller proves nothing by passing, and one that also counts a type inside the module, or an " +
                "outside type that reaches only the module's Application, flags code that is allowed. How to fix: report only " +
                $"types outside the module's namespaces that reference a type in its {layer}.");

            Assert.DoesNotThrow(
                verdict.Check,
                $"The {layer} rule failed although only the allowed outside type reached the layer. Why it matters: a rule that " +
                "fails on compliant code gets switched off. How to fix: compare what was found with the list, nothing more.");
        }

        private static void ThenTheRuleFailsNamingTheNewcomer(InboundVerdict verdict, string newcomer, string reached, ReachedLayer layer)
        {
            var failure = Assert.Throws<AssertionException>(
                verdict.Check,
                $"The {layer} rule passed with {newcomer} reaching the layer from outside without being on the list. Why it " +
                "matters: a new direct dependency on the module's insides would go in unseen, async bodies included. How to fix: " +
                $"report every type outside the module that references a type in its {layer} and is not on the list.");

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
                    "The allowed type still reaches the layer, so the failure must not ask to delete it from the list.");
            }
        }

        private static void ThenTheRuleFailsNamingTheStaleEntryForDeletion(InboundVerdict verdict, string stale, ReachedLayer layer)
        {
            var failure = Assert.Throws<AssertionException>(
                verdict.Check,
                $"The {layer} rule passed with {stale} on its list although it no longer reaches the layer. Why it matters: a " +
                "list that keeps a gone entry lets that dependency come back unseen. How to fix: compare the list with what was " +
                "found in both directions.");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    failure!.Message,
                    Does.Contain($"On the list, but no longer offending: {stale}. Good - delete it"),
                    $"The failure must name {stale}, and only it, with 'delete', so the reader knows which list entry to remove.");
                Assert.That(
                    failure.Message,
                    Does.Not.Contain("Not on the list"),
                    "Every type that reaches the layer is on the list, so the failure must not name a newcomer.");
            }
        }
    }
}

namespace Lighthouse.Backend.Modules.FlowMetricsFixtures.InboundRules.Domain
{
    internal static class ADomainCalculation
    {
        public static int Doubled(int days) => days * 2;
    }
}

namespace Lighthouse.Backend.Modules.FlowMetricsFixtures.InboundRules.Application
{
    internal static class AnApplicationService
    {
        public static int Answer(int days) => global::Lighthouse.Backend.Modules.FlowMetricsFixtures.InboundRules.Domain.ADomainCalculation.Doubled(days)
            + new global::Lighthouse.Backend.Modules.FlowMetricsFixtures.InboundRules.Adapters.Persistence.AnAdapter().Stored;
    }
}

namespace Lighthouse.Backend.Modules.FlowMetricsFixtures.InboundRules.Adapters.Persistence
{
    internal sealed class AnAdapter
    {
        public int Stored => global::Lighthouse.Backend.Modules.FlowMetricsFixtures.InboundRules.Domain.ADomainCalculation.Doubled(21);
    }
}

namespace Lighthouse.Backend.FlowMetricsInboundFixtures.ListedCallers
{
    internal static class AListedDomainCaller
    {
        public static int Answer() => global::Lighthouse.Backend.Modules.FlowMetricsFixtures.InboundRules.Domain.ADomainCalculation.Doubled(21);
    }
}

namespace Lighthouse.Backend.FlowMetricsInboundFixtures.CompositionRoot
{
    internal static class ACompositionRoot
    {
        public static object Register() => new global::Lighthouse.Backend.Modules.FlowMetricsFixtures.InboundRules.Adapters.Persistence.AnAdapter();
    }
}

namespace Lighthouse.Backend.FlowMetricsInboundFixtures.ApplicationCallers
{
    internal static class ACallerOfTheApplicationOnly
    {
        public static int Answer() => global::Lighthouse.Backend.Modules.FlowMetricsFixtures.InboundRules.Application.AnApplicationService.Answer(21);
    }
}

namespace Lighthouse.Backend.FlowMetricsInboundFixtures.FormerCallers
{
    internal static class AFormerCaller
    {
        public static int Answer() => Math.Max(21, 42);
    }
}

namespace Lighthouse.Backend.FlowMetricsInboundFixtures.NewcomerToTheDomain
{
    internal static class ANewcomerReachingTheDomain
    {
        public static int Answer() => global::Lighthouse.Backend.Modules.FlowMetricsFixtures.InboundRules.Domain.ADomainCalculation.Doubled(21);
    }
}

namespace Lighthouse.Backend.FlowMetricsInboundFixtures.NewcomerToTheAdapters
{
    internal static class ANewcomerReachingTheAdapters
    {
        public static int Answer() => new global::Lighthouse.Backend.Modules.FlowMetricsFixtures.InboundRules.Adapters.Persistence.AnAdapter().Stored;
    }
}

namespace Lighthouse.Backend.FlowMetricsInboundFixtures.AsyncOnlyCallerOfTheDomain
{
    internal sealed class ACallerOfTheDomainOnlyInsideAnAsyncMethod
    {
        public async Task<int> AnswerAsync()
        {
            await Task.Yield();
            return global::Lighthouse.Backend.Modules.FlowMetricsFixtures.InboundRules.Domain.ADomainCalculation.Doubled(21);
        }
    }
}

namespace Lighthouse.Backend.FlowMetricsInboundFixtures.AsyncOnlyCallerOfTheAdapters
{
    internal sealed class ACallerOfTheAdaptersOnlyInsideAnAsyncMethod
    {
        public async Task<int> AnswerAsync()
        {
            await Task.Yield();
            return new global::Lighthouse.Backend.Modules.FlowMetricsFixtures.InboundRules.Adapters.Persistence.AnAdapter().Stored;
        }
    }
}
