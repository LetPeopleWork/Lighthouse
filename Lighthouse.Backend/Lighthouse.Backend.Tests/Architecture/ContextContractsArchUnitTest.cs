using Lighthouse.Backend.Data;
using Lighthouse.Backend.Modules.ContextContractsFixtures.Alpha.Adapters;
using Lighthouse.Backend.Modules.ContextContractsFixtures.Alpha.Application;
using Lighthouse.Backend.Modules.ContextContractsFixtures.AlphaPlus.Domain;
using Lighthouse.Backend.Modules.ContextContractsFixtures.Beta.Application;
using Lighthouse.Backend.Modules.ContextContractsFixtures.Beta.Contracts;
using Lighthouse.Backend.Modules.ContextContractsFixtures.Beta.Domain;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The packaged contexts reach each other only through what each publishes in its Contracts, read from the compiled code.
    /// The contexts are read from the assembly and must be exactly the ten named here; a type of one context reaches a type of
    /// another only in that context's Contracts or along a listed edge; and a type in a Contracts reaches only the kernel, the
    /// base library, Microsoft.Extensions and the Contracts of any context. Every list is compared both ways. The same checks
    /// run on made-up contexts, one made-up type per way a check can be broken, so a check that names nothing cannot pass.
    /// </summary>
    [TestFixture]
    public class ContextContractsArchUnitTest
    {
        private const string TheContractsLayer = "Contracts";

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(LighthouseAppContext).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(ContextContractsArchUnitTest).Assembly));

        private static readonly Lazy<OutboundVerdict> ProductionVerdict = new(() => WhenTheContextsAreChecked(ProductionModule.Value, TheModulesRoot, CrossContextEdgesOutsideContracts));

        private static string TheModulesRoot => ContextContractsMembership.TheModulesRoot;

        private static string TheMadeUpModuleRoot => "ContextContractsFixtures";

        private static string TheMadeUpContextsRoot => $"{TheModulesRoot}.{TheMadeUpModuleRoot}";

        private static IReadOnlyList<string> TheTenPackagedContexts =>
        [
            "Access",
            "FlowMetrics",
            "Forecasting",
            "Licensing",
            "OrganisationSetup",
            "Promises",
            "Refinement",
            "Reports",
            "TrackerIntegration",
            "WorkItems",
        ];

        // Each edge names its source and target under the modules root; the comment names the context that owns the target.
        private static IReadOnlyList<string> CrossContextEdgesOutsideContracts =>
        [
            Edge("FlowMetrics.Application.PortfolioMetricsService", "OrganisationSetup.Application.IAppSettingService"), // owner: Organisation set-up
            Edge("FlowMetrics.Application.TeamMetricsService", "OrganisationSetup.Application.IAppSettingService"), // owner: Organisation set-up
            Edge("FlowMetrics.Application.TeamMetricsService", "OrganisationSetup.Application.IBlackoutPeriodService"), // owner: Organisation set-up
            Edge("Forecasting.Application.ForecastRealityCheckService", "OrganisationSetup.Application.IBlackoutPeriodService"), // owner: Organisation set-up
            Edge("Forecasting.Application.WriteBack.WriteBackTriggerService", "OrganisationSetup.Application.IBlackoutPeriodService"), // owner: Organisation set-up
            Edge("OrganisationSetup.Application.Connections.ConnectionSaveService", "WorkItems.Domain.FetchFingerprint"), // owner: Work Items
            Edge("Promises.Application.DeliveryMetricValuesProjector", "OrganisationSetup.Application.IBlackoutPeriodService"), // owner: Organisation set-up
            Edge("Promises.Application.DeliveryRuleService", "WorkItems.Application.FeatureFieldProvider"), // owner: Work Items
            Edge("Promises.Application.DeliveryRuleService", "WorkItems.Application.RuleEvaluator`1"), // owner: Work Items
            Edge("Refinement.Application.RefinementCadenceCalendar", "OrganisationSetup.Domain.WeeklyRecurrence"), // owner: Organisation set-up
            Edge("Refinement.Application.RefinementCalendar", "OrganisationSetup.Application.IBlackoutPeriodService"), // owner: Organisation set-up
            Edge("Refinement.Application.RefinementNeedCalculator", "OrganisationSetup.Application.IBlackoutPeriodService"), // owner: Organisation set-up
            Edge("Refinement.Application.VoterIdentityResolver", "Access.Application.CurrentUserProfileService"), // owner: Access
            Edge("Reports.Application.Metrics.DailySeriesSummary", "FlowMetrics.Domain.PercentileCalculator"), // owner: Flow metrics
            Edge("TrackerIntegration.Application.ConnectionHealth.ConnectionHealthCadence", "OrganisationSetup.Application.IAppSettingService"), // owner: Organisation set-up
            Edge("WorkItems.Application.RefreshLogService", "OrganisationSetup.Application.IAppSettingService"), // owner: Organisation set-up
            Edge("WorkItems.Application.WorkItemService", "FlowMetrics.Domain.PercentileCalculator"), // owner: Flow metrics
            Edge("WorkItems.Application.WorkItemService", "OrganisationSetup.Application.IFeatureOrdering"), // owner: Organisation set-up
            Edge("WorkItems.Application.WorkItemService", "TrackerIntegration.Domain.ParentSourceSelector"), // owner: Tracker integration
        ];

        private static IReadOnlyList<string> ContractsEdgesBeyondTheKernelAndOtherContracts => [];

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void TheContextsReadFromTheCompiledCode_AreExactlyTheTenPackagedContexts()
        {
            var found = WhenTheContextsAreReadFrom(ProductionModule.Value);

            var (unknown, missing) = WhenComparedWithTheList(found, TheTenPackagedContexts);

            ThenTheContextsAreExactlyTheNamed(unknown, missing);
        }

        [Test]
        public void TheContextCheck_NamesAMadeUpModuleRootOfTheCompiledCode_AndANamedContextThatIsGone_ReadInOrderAndOnceEach()
        {
            var found = WhenTheContextsAreReadFrom(TestModule.Value);
            var named = found.Where(context => context != TheMadeUpModuleRoot).Append("AGoneContext").ToList();

            var (unknown, missing) = WhenComparedWithTheList(found, named);

            ThenOnlyTheMadeUpRootAndTheGoneContextAreNamed(found, unknown, missing);
        }

        [TestCaseSource(nameof(TheTenPackagedContexts))]
        public void TheContextCheck_NamesAPackagedContextOfTheCompiledCode_OnceItIsTakenOffTheList(string context)
        {
            var found = WhenTheContextsAreReadFrom(ProductionModule.Value);

            var (unknown, missing) = WhenComparedWithTheList(found, [.. TheTenPackagedContexts.Where(named => named != context)]);

            ThenOnlyTheEntryTakenOffIsNamed(context, unknown, missing, "context");
        }

        [Test]
        public void EveryEdgeFromOneContextIntoAnotherOutsideItsContracts_IsListed_AndEveryListedEdgeIsStillMade()
        {
            var verdict = ProductionVerdict.Value;

            ThenTheEdgesAreExactlyTheListed(verdict, NotFromAContracts(verdict.Edges, TheModulesRoot), CrossContextEdgesOutsideContracts);
        }

        [Test]
        public void TheSurfaceCheck_NamesEdgesIntoAnotherContextsDomain_AndAListedEdgeNoTypeMakes_ButNotEdgesIntoContractsTheKernelOrTheOwnContext()
        {
            var listed = new[] { Edge<AlphaListedReacher, IBetaService>(), $"{TheMadeUpContextsRoot}.Alpha.Application.AGoneType -> {TheMadeUpContextsRoot}.Beta.Domain.AGoneTarget" };

            var verdict = WhenTheContextsAreChecked(TestModule.Value, TheMadeUpContextsRoot, listed);
            var (unlisted, gone) = WhenComparedWithTheList(NotFromAContracts(verdict.Edges, TheMadeUpContextsRoot), listed);

            ThenOnlyTheEdgesIntoOtherDomainsAndTheGoneEdgeAreNamed(unlisted, gone);
        }

        [TestCaseSource(nameof(CrossContextEdgesOutsideContracts))]
        public void TheSurfaceCheck_NamesAListedEdgeOfTheCompiledCode_OnceItIsTakenOffTheList(string edge)
        {
            var verdict = ProductionVerdict.Value;

            var (unlisted, gone) = WhenComparedWithTheList(NotFromAContracts(verdict.Edges, TheModulesRoot), [.. CrossContextEdgesOutsideContracts.Where(listed => listed != edge)]);

            ThenOnlyTheEntryTakenOffIsNamed(edge, unlisted, gone, "edge");
        }

        [Test]
        public void NoContractsTypeReachesBeyondTheKernelTheBaseLibraryMicrosoftExtensionsAndTheContracts()
        {
            var verdict = ProductionVerdict.Value;

            ThenTheEdgesAreExactlyTheListed(verdict, FromAContracts(verdict.Edges, TheModulesRoot), ContractsEdgesBeyondTheKernelAndOtherContracts);
        }

        [Test]
        public void TheInwardCheck_NamesAContractsTypeReachingItsOwnApplication_ButNotOneReachingTheKernelTheBaseLibraryMicrosoftExtensionsOrAnotherContracts()
        {
            var verdict = WhenTheContextsAreChecked(TestModule.Value, TheMadeUpContextsRoot, []);

            var (unlisted, gone) = WhenComparedWithTheList(FromAContracts(verdict.Edges, TheMadeUpContextsRoot), []);

            ThenOnlyTheContractsTypeReachingItsOwnApplicationIsNamed(unlisted, gone);
        }

        private static string Edge(string source, string target) => $"{TheModulesRoot}.{source} -> {TheModulesRoot}.{target}";

        private static string Edge<TSource, TTarget>() => $"{typeof(TSource).FullName} -> {typeof(TTarget).FullName}";

        private static IReadOnlyList<string> WhenTheContextsAreReadFrom(ModuleDefinition module) => PackagedContexts.InTheAssembly(module);

        private static OutboundVerdict WhenTheContextsAreChecked(ModuleDefinition module, string modulesRoot, IReadOnlyCollection<string> listed)
            => ModuleRules.ReachesOtherContextsOnlyThroughContracts(module, listed, modulesRoot, TheContractsLayer, KernelRules.TheModels);

        private static (List<string> Unlisted, List<string> Gone) WhenComparedWithTheList(IEnumerable<string> found, IReadOnlyCollection<string> listed)
        {
            var foundOnce = found.ToList();

            return ([.. foundOnce.Except(listed, StringComparer.Ordinal).Order(StringComparer.Ordinal)], [.. listed.Except(foundOnce, StringComparer.Ordinal).Order(StringComparer.Ordinal)]);
        }

        private static List<string> FromAContracts(IEnumerable<string> edges, string modulesRoot)
            => [.. edges.Where(edge => SourceIsInAContracts(edge, modulesRoot))];

        private static List<string> NotFromAContracts(IEnumerable<string> edges, string modulesRoot)
            => [.. edges.Where(edge => !SourceIsInAContracts(edge, modulesRoot))];

        private static bool SourceIsInAContracts(string edge, string modulesRoot)
        {
            var source = edge[..edge.IndexOf(" -> ", StringComparison.Ordinal)];
            var parts = source[(modulesRoot.Length + 1)..].Split('+')[0].Split('.');

            return parts.Length > 2 && parts[1] == TheContractsLayer;
        }

        private static void ThenTheContextsAreExactlyTheNamed(List<string> unknown, List<string> missing)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(unknown, Is.Empty, $"These namespaces right under {TheModulesRoot} are not among the ten packaged contexts: {string.Join(", ", unknown)}. Why it matters: the rule that keeps contexts apart walks the contexts the code holds, so a new module root is a new context that nobody has decided on. How to fix: name it in the list of packaged contexts, or put its types into an existing context.");
                Assert.That(missing, Is.Empty, $"These packaged contexts were not found in the compiled code: {string.Join(", ", missing)}. Why it matters: a context the rules expect but cannot find is either gone or no longer read from the assembly, so its edges go unchecked. How to fix: read every namespace right under {TheModulesRoot} from the compiled code, or take a context that is really gone off the list.");
            }
        }

        private static void ThenOnlyTheMadeUpRootAndTheGoneContextAreNamed(IReadOnlyList<string> found, List<string> unknown, List<string> missing)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(unknown, Is.EqualTo(new[] { TheMadeUpModuleRoot }), $"The check named [{string.Join(" | ", unknown)}] as not named. Expected exactly the made-up module root {TheMadeUpModuleRoot}, read from the compiled test code as one name, not its contexts below it. Why it matters: contexts written down instead of read from the code, or read too deep, let a new module root pass unseen. How to fix: read the name right under {TheModulesRoot} of every type in the assembly.");
                Assert.That(missing, Is.EqualTo(new List<string> { "AGoneContext" }), $"The check named [{string.Join(" | ", missing)}] as gone. Expected exactly the named context the code does not hold. Why it matters: a list compared only one way keeps a context that is gone. How to fix: compare the list with the code both ways.");
                Assert.That(found, Is.EqualTo(found.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)), $"The contexts were read as [{string.Join(" | ", found)}]. Expected each once, in order. Why it matters: a repeated or shuffled reading makes the comparison's report hard to read and hides a duplicate root. How to fix: return the names once each, ordered.");
            }
        }

        private static void ThenOnlyTheEntryTakenOffIsNamed(string entry, List<string> unlisted, List<string> gone, string kind)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(unlisted, Is.EqualTo(new[] { entry }), $"With the {kind} {entry} taken off the list, the check named [{string.Join(" | ", unlisted)}] as not listed. Expected exactly that {kind}. Why it matters: a list entry the code does not need, or one the check never compares, lets the same {kind} come back unseen once it is deleted. How to fix: compare what the compiled code holds with the list both ways, and keep on the list only what the code holds.");
                Assert.That(gone, Is.Empty, $"With the {kind} {entry} taken off the list, the check named [{string.Join(" | ", gone)}] as listed but not in the compiled code. Why it matters: every other entry is in the code, so a gone entry here means the list holds something the code does not. How to fix: delete the gone entries.");
            }
        }

        private static void ThenTheEdgesAreExactlyTheListed(OutboundVerdict verdict, List<string> edges, IReadOnlyCollection<string> listed)
        {
            Assert.That(verdict.Examined, Is.Not.Empty, $"No type lives in a namespace under {TheModulesRoot}, so the rule checked nothing. Why it matters: a rule over no types passes while it guards nothing. How to fix: point the rule at the namespace that holds the packaged contexts.");

            InlineBaseline.AssertMatches(edges, listed, verdict.Rule);
        }

        private static void ThenOnlyTheEdgesIntoOtherDomainsAndTheGoneEdgeAreNamed(List<string> unlisted, List<string> gone)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(unlisted, Is.EqualTo(new[] { Edge<AlphaReachingAlphaPlusDomain, AlphaPlusCalculation>(), Edge<AlphaReachingBetasDomain, BetaCalculation>() }), $"The check named [{string.Join(" | ", unlisted)}] as unlisted. Expected exactly the made-up Alpha types reaching Beta's Domain and the Domain of AlphaPlus, a context whose name begins like Alpha's, and not the listed edge nor the edges into Beta's Contracts, the kernel, the base library or Alpha's own Domain. Why it matters: a check that misses an edge lets a context be reached past what it publishes, and one that names a legal edge makes the list grow. How to fix: compare every edge from one context into another, outside that context's Contracts, with the list.");
                Assert.That(gone, Is.EqualTo(new[] { $"{TheMadeUpContextsRoot}.Alpha.Application.AGoneType -> {TheMadeUpContextsRoot}.Beta.Domain.AGoneTarget" }), $"The check named [{string.Join(" | ", gone)}] as gone. Expected exactly the listed edge no type makes. Why it matters: a list compared only one way keeps dead entries that hide a later edge. How to fix: compare the list both ways.");
            }
        }

        private static void ThenOnlyTheContractsTypeReachingItsOwnApplicationIsNamed(List<string> unlisted, List<string> gone)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(unlisted, Is.EqualTo(new[] { Edge<BetaAnswerNamingItsOwnApplication, IBetaService>() }), $"The check named [{string.Join(" | ", unlisted)}] as reaching outward. Expected exactly the made-up Beta Contracts type reaching Beta's Application, and not its twin reaching the kernel, the base library, Microsoft.Extensions and Alpha's Contracts. Why it matters: a published type that drags in an Application type publishes that type too, and one refused for reaching another Contracts cannot be published at all. How to fix: refuse from a Contracts every target but the kernel, the base library, Microsoft.Extensions and any Contracts.");
                Assert.That(gone, Is.Empty, $"The check named [{string.Join(" | ", gone)}] as gone, from an empty list.");
            }
        }
    }
}

namespace Lighthouse.Backend.Modules.ContextContractsFixtures.Alpha.Contracts
{
    internal interface IAlphaFact
    {
        int Count();
    }
}

namespace Lighthouse.Backend.Modules.ContextContractsFixtures.Alpha.Domain
{
    internal sealed class AlphaCalculation
    {
        public int Add(int days) => days + 1;
    }
}

namespace Lighthouse.Backend.Modules.ContextContractsFixtures.Alpha.Application
{
    internal sealed class AlphaReachingBetasDomain(Beta.Domain.BetaCalculation calculation)
    {
        public int Run(int days) => calculation.Twice(days);
    }

    internal sealed class AlphaReachingAlphaPlusDomain(AlphaPlus.Domain.AlphaPlusCalculation calculation)
    {
        public int Run(int days) => calculation.Thrice(days);
    }

    internal sealed class AlphaReachingBetasContractsTheKernelAndItsOwnDomain(Beta.Contracts.IBetaQuestions questions, Domain.AlphaCalculation calculation, Lighthouse.Backend.Models.Team team)
    {
        public int Run(int days) => team is null ? 0 : questions.Ask(calculation.Add(days));
    }
}

namespace Lighthouse.Backend.Modules.ContextContractsFixtures.Alpha.Adapters
{
    internal sealed class AlphaListedReacher(Beta.Application.IBetaService service)
    {
        public int Run(int days) => service.Serve(days);
    }
}

namespace Lighthouse.Backend.Modules.ContextContractsFixtures.AlphaPlus.Domain
{
    internal sealed class AlphaPlusCalculation
    {
        public int Thrice(int days) => days * 3;
    }
}

namespace Lighthouse.Backend.Modules.ContextContractsFixtures.Beta.Application
{
    internal interface IBetaService
    {
        int Serve(int days);
    }
}

namespace Lighthouse.Backend.Modules.ContextContractsFixtures.Beta.Domain
{
    internal sealed class BetaCalculation
    {
        public int Twice(int days) => days * 2;
    }
}

namespace Lighthouse.Backend.Modules.ContextContractsFixtures.Beta.Contracts
{
    internal interface IBetaQuestions
    {
        int Ask(int days);
    }

    internal sealed class BetaAnswerNamingItsOwnApplication(Application.IBetaService service)
    {
        public int Answer(int days) => service.Serve(days);
    }

    internal sealed class BetaAnswerNamingTheKernelTheBaseLibraryExtensionsAndAlphasContracts(Alpha.Contracts.IAlphaFact fact, Microsoft.Extensions.Logging.ILogger logger, Lighthouse.Backend.Models.Team team)
    {
        public List<int> Answer() => logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug) && team is not null ? [fact.Count()] : [];
    }
}
