using System.Text.RegularExpressions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.ForecastingFixtures.MetricsAskThroughPorts.AskingThroughItsPort.FlowMetrics.Application;
using Lighthouse.Backend.Modules.ForecastingFixtures.MetricsAskThroughPorts.CallingAForecastingPolicy.FlowMetrics.Application;
using Lighthouse.Backend.Modules.ForecastingFixtures.MetricsAskThroughPorts.CallingAForecastingPolicy.Forecasting.Domain;
using Lighthouse.Backend.Modules.ForecastingFixtures.MetricsAskThroughPorts.HoldingAForecastingService.FlowMetrics.Application;
using Lighthouse.Backend.Modules.ForecastingFixtures.MetricsAskThroughPorts.HoldingAForecastingService.Forecasting.Application;
using Lighthouse.Backend.Modules.ForecastingFixtures.MetricsAskThroughPorts.PortInsideForecasting.Forecasting.Application;
using Lighthouse.Backend.Modules.ForecastingFixtures.MetricsAskThroughPorts.PortOnlyMetricsAnswers.FlowMetrics.Application;
using Lighthouse.Backend.Modules.ForecastingFixtures.MetricsAskThroughPorts.ResolvingAForecastingService.FlowMetrics.Application;
using Lighthouse.Backend.Modules.ForecastingFixtures.MetricsAskThroughPorts.ResolvingAForecastingService.Forecasting.Application;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Flow metrics asks Forecasting three questions: which of a Team's work items count for its throughput, whether
    /// the throughput is enough to forecast with, and how many items fit in a number of days. It asks them through three
    /// ports of its own, in its Application, and Forecasting answers them, so Forecasting can build on Flow metrics
    /// without Flow metrics depending on Forecasting in return. Read from the compiled production assembly: no type of
    /// the Flow metrics module reaches a Forecasting type, with no list of allowed edges, and each of the three ports is
    /// an interface in Flow metrics' Application that a Forecasting type implements. The second rule also proves the
    /// first looked at the real Forecasting: a Forecasting pattern that matched nothing would leave the ports unanswered.
    /// The same two rules run on made-up modules: a metrics type that holds, calls or resolves a Forecasting type fails
    /// and is named with what it reaches; a port declared in Forecasting, or one only Flow metrics answers, fails and is
    /// named; a metrics type that asks only through its own port passes both.
    /// </summary>
    /// <remarks>
    /// The IL reader does not see types used only in a catch clause, an event or a named attribute argument.
    /// </remarks>
    [TestFixture]
    public class FlowMetricsDoesNotReachForecastingArchUnitTest
    {
        private const string FlowMetricsApplication = "Lighthouse.Backend.Modules.FlowMetrics.Application";

        private const string MadeUpModules = "Lighthouse.Backend.Modules.ForecastingFixtures.MetricsAskThroughPorts";

        private const string ThroughputFilter = "IThroughputFilter";

        private const string DataSufficiency = "IForecastDataSufficiency";

        private const string HowManyForecaster = "IHowManyForecaster";

        private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

        private static readonly string[] TheQuestionsFlowMetricsAsks = [ThroughputFilter, DataSufficiency, HowManyForecaster];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(FlowMetricsDoesNotReachForecastingArchUnitTest).Assembly));

        private static TheModules TheRealModules => new(ModuleRules.FlowMetricsModule, ModuleBoundariesArchUnitTest.ForecastingPattern, typeof(global::Lighthouse.Backend.Modules.FlowMetrics.Contracts.ITeamMetricsService).Namespace!);

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void FlowMetrics_ReachesNoForecastingType()
        {
            var verdict = FlowMetricsReachesNoForecastingType(ProductionModule.Value, TheRealModules);

            verdict.Check();
        }

        [Test]
        public void TheThreeQuestionsFlowMetricsAsks_AreItsOwnPorts_AndForecastingAnswersEach()
        {
            var verdict = TheQuestionsAreFlowMetricsPortsThatForecastingAnswers(ProductionModule.Value, TheRealModules, TheQuestionsFlowMetricsAsks);

            verdict.Check();
        }

        [Test]
        public void AMetricsTypeAskingOnlyThroughItsOwnPort_PassesBothRules()
        {
            var modules = GivenTheMadeUpModulesOf(typeof(AMetricsServiceAskingThroughItsPort));

            var reach = FlowMetricsReachesNoForecastingType(TestModule.Value, modules);
            var ports = TheQuestionsAreFlowMetricsPortsThatForecastingAnswers(TestModule.Value, modules, [ThroughputFilter]);

            ThenTheReachRuleLookedAtAndPasses(reach, typeof(AMetricsServiceAskingThroughItsPort));
            ThenThePortRulePasses(ports);
        }

        [TestCaseSource(nameof(MetricsTypesReachingForecasting))]
        public void AMetricsTypeReachingForecasting_FailsAndIsNamedWithWhatItReaches(Type offender, Type reached)
        {
            var modules = GivenTheMadeUpModulesOf(offender);

            var reach = FlowMetricsReachesNoForecastingType(TestModule.Value, modules);

            ThenTheReachRuleFailsNamingOnly(reach, $"{offender.FullName} -> {reached.FullName}");
        }

        [Test]
        public void APortDeclaredInForecastingInsteadOfFlowMetrics_FailsAndIsNamedWithWhereItLives()
        {
            var modules = GivenTheMadeUpModulesOf(typeof(AForecastAnsweringItsOwnQuestion));

            var ports = TheQuestionsAreFlowMetricsPortsThatForecastingAnswers(TestModule.Value, modules, [HowManyForecaster]);

            ThenThePortRuleFailsNaming(
                ports,
                $"{modules.FlowMetricsApplication}.{HowManyForecaster} is not an interface there; a type of that name lives at {typeof(IHowManyForecaster).FullName}");
        }

        [Test]
        public void APortThatOnlyFlowMetricsAnswers_FailsAndIsNamedWithWhoAnswersIt()
        {
            var modules = GivenTheMadeUpModulesOf(typeof(ASufficiencyRuleCopiedIntoMetrics));

            var ports = TheQuestionsAreFlowMetricsPortsThatForecastingAnswers(TestModule.Value, modules, [DataSufficiency]);

            ThenThePortRuleFailsNaming(
                ports,
                $"{modules.FlowMetricsApplication}.{DataSufficiency} is answered by no type in namespaces matching \"{modules.Forecasting}\"; it is implemented by {typeof(ASufficiencyRuleCopiedIntoMetrics).FullName}");
        }

        private static IEnumerable<TestCaseData> MetricsTypesReachingForecasting()
        {
            yield return new TestCaseData(typeof(AMetricsServiceHoldingAForecastFilter), typeof(IForecastFilter))
                .SetName("AMetricsTypeTakingAForecastingServiceInItsConstructor_FailsAndIsNamedWithWhatItReaches");
            yield return new TestCaseData(typeof(AMetricsServiceCallingASufficiencyPolicy), typeof(ASufficiencyPolicy))
                .SetName("AMetricsTypeCallingAStaticForecastingPolicy_FailsAndIsNamedWithWhatItReaches");
            yield return new TestCaseData(typeof(AMetricsServiceResolvingAForecaster), typeof(IForecaster))
                .SetName("AMetricsTypeResolvingAForecastingServiceFromTheServiceProvider_FailsAndIsNamedWithWhatItReaches");
        }

        /// <summary>
        /// Every edge from a type of the Flow metrics module to a Forecasting type is an offender: there is no list,
        /// because the module asks Forecasting only through ports it owns. A module that holds no type fails too.
        /// </summary>
        private static OutboundVerdict FlowMetricsReachesNoForecastingType(ModuleDefinition module, TheModules modules)
            => ModuleRules.OutboundEdges(
                module,
                modules.FlowMetrics,
                modules.Forecasting,
                [],
                $"Types in namespaces matching \"{modules.FlowMetrics}\" should depend on no type in namespaces matching \"{modules.Forecasting}\", " +
                "because Forecasting builds on Flow metrics' answers, and one edge back from Flow metrics makes the two modules one " +
                "cycle that cannot change, be tested or move apart. Ask Forecasting through a port Flow metrics owns in its " +
                $"Application ({string.Join(", ", TheQuestionsFlowMetricsAsks)}), name only Models types in it, let Forecasting " +
                "implement it and the composition root register it; there is no list of allowed edges.");

        /// <summary>
        /// Each question is an interface in Flow metrics' Application, and at least one Forecasting type implements it.
        /// </summary>
        private static PortVerdict TheQuestionsAreFlowMetricsPortsThatForecastingAnswers(ModuleDefinition module, TheModules modules, IReadOnlyCollection<string> ports)
            => new(
                [.. ports.SelectMany(port => ProblemsOfThePort(module, modules, port))],
                $"Each question Flow metrics asks Forecasting should be an interface in namespace \"{modules.FlowMetricsApplication}\" that a type " +
                $"in namespaces matching \"{modules.Forecasting}\" implements, because Flow metrics owns the question in its own words and " +
                "Forecasting only answers it. A port declared in Forecasting makes Flow metrics depend on Forecasting again, and a port " +
                "no Forecasting type answers means the answer was copied into Flow metrics or never wired. Declare the interface in Flow " +
                "metrics' Application, and let the Forecasting type that owns the rule implement it.");

        private static IEnumerable<string> ProblemsOfThePort(ModuleDefinition module, TheModules modules, string port)
        {
            var fullName = $"{modules.FlowMetricsApplication}.{port}";
            var declared = module.GetType(fullName);

            if (declared is null || !declared.IsInterface)
            {
                var elsewhere = module.Types
                    .Where(type => type.Name == port && type.FullName != fullName)
                    .Select(type => type.FullName)
                    .Order(StringComparer.Ordinal)
                    .ToList();

                return [elsewhere.Count > 0
                    ? $"{fullName} is not an interface there; a type of that name lives at {string.Join(", ", elsewhere)}."
                    : $"{fullName} is not an interface there, and no type of that name exists."];
            }

            var implementers = module.Types
                .Where(type => type.Interfaces.Any(implemented => implemented.InterfaceType.FullName == fullName))
                .ToList();

            if (implementers.Any(type => Regex.IsMatch(type.Namespace, modules.Forecasting, RegexOptions.None, PatternTimeout)))
            {
                return [];
            }

            var answeredBy = implementers.Count > 0
                ? $"it is implemented by {string.Join(", ", implementers.Select(type => type.FullName).Order(StringComparer.Ordinal))}."
                : "no type implements it.";

            return [$"{fullName} is answered by no type in namespaces matching \"{modules.Forecasting}\"; {answeredBy}"];
        }

        // Each made-up case lives under a namespace of its own with a FlowMetrics and a Forecasting part, so a rule
        // handed one case sees only that case.
        private static TheModules GivenTheMadeUpModulesOf(Type madeUpType)
        {
            var typeNamespace = madeUpType.Namespace!;
            var madeUpCase = typeNamespace[..typeNamespace.IndexOf('.', MadeUpModules.Length + 1)];

            return new TheModules(
                $@"^{Regex.Escape(madeUpCase)}\.FlowMetrics(\.|$)",
                $@"^{Regex.Escape(madeUpCase)}\.Forecasting(\.|$)",
                $"{madeUpCase}.FlowMetrics.Application");
        }

        private static void ThenTheReachRuleLookedAtAndPasses(OutboundVerdict verdict, Type asker)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    verdict.Examined,
                    Does.Contain(asker.FullName),
                    $"Expected the rule to look at {asker.FullName}. Why it matters: a rule that does not see the metrics types " +
                    "passes without checking anything. How to fix: examine every type in the Flow metrics namespaces.");
                Assert.That(
                    verdict.Edges,
                    Is.Empty,
                    $"Expected no edge into Forecasting, found [{string.Join(", ", verdict.Edges)}]. Why it matters: a metrics type " +
                    "that asks through its own port, which a Forecasting type implements, does not depend on Forecasting. How to " +
                    "fix: report only edges whose target lives in the Forecasting namespaces, not types that implement a metrics port.");
            }

            Assert.DoesNotThrow(
                verdict.Check,
                "The rule failed although the metrics type asked only through its own port. Why it matters: a rule that fails on " +
                "compliant code gets switched off. How to fix: compare what was found with an empty list, nothing more.");
        }

        private static void ThenThePortRulePasses(PortVerdict verdict)
        {
            Assert.DoesNotThrow(
                verdict.Check,
                $"The port rule failed with [{string.Join(" ", verdict.Problems)}] although the port is an interface in the metrics " +
                "Application that a Forecasting type implements. Why it matters: a rule that fails on compliant code gets switched " +
                "off. How to fix: accept a port that lives in the metrics Application and has a Forecasting implementation.");
        }

        private static void ThenTheReachRuleFailsNamingOnly(OutboundVerdict verdict, string edge)
        {
            var failure = Assert.Throws<AssertionException>(
                verdict.Check,
                $"The rule passed although {edge} exists. Why it matters: Flow metrics would depend on Forecasting again, unseen, " +
                "and the two modules would form a cycle. How to fix: report every edge from a Flow metrics type into Forecasting, " +
                "against an empty list.");

            Assert.That(
                failure!.Message,
                Does.Contain($"Not on the list, and offending: {edge}. "),
                $"The failure must name {edge}, and only it, so the reader knows which metrics type to change and what it must " +
                "stop reaching.");
        }

        private static void ThenThePortRuleFailsNaming(PortVerdict verdict, string problem)
        {
            var failure = Assert.Throws<AssertionException>(
                verdict.Check,
                $"The port rule passed although {problem}. Why it matters: Flow metrics would no longer own the question it asks, or " +
                "Forecasting would not be the one answering it. How to fix: fail for a port outside the metrics Application or " +
                "without a Forecasting implementation.");

            Assert.That(
                failure!.Message,
                Does.Contain(problem),
                $"The failure must say \"{problem}\", so the reader knows which port to move or which Forecasting type must answer it.");
        }

        private sealed record TheModules(string FlowMetrics, string Forecasting, string FlowMetricsApplication);

        private sealed record PortVerdict(IReadOnlyList<string> Problems, string Rule)
        {
            public void Check()
            {
                if (Problems.Count > 0)
                {
                    Assert.Fail($"{string.Join(Environment.NewLine, Problems)}{Environment.NewLine}{Rule}");
                }
            }
        }
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.MetricsAskThroughPorts.AskingThroughItsPort.FlowMetrics.Application
{
    internal interface IThroughputFilter
    {
        IEnumerable<string> Filter(IEnumerable<string> items);
    }

    internal sealed class AMetricsServiceAskingThroughItsPort(IThroughputFilter throughputFilter)
    {
        public int CountFinished(IEnumerable<string> items) => throughputFilter.Filter(items).Count();
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.MetricsAskThroughPorts.AskingThroughItsPort.Forecasting.Application
{
    internal sealed class AFilterRuleAnsweringTheMetricsPort : global::Lighthouse.Backend.Modules.ForecastingFixtures.MetricsAskThroughPorts.AskingThroughItsPort.FlowMetrics.Application.IThroughputFilter
    {
        public IEnumerable<string> Filter(IEnumerable<string> items) => items.Where(item => item.Length > 0);
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.MetricsAskThroughPorts.HoldingAForecastingService.FlowMetrics.Application
{
    internal sealed class AMetricsServiceHoldingAForecastFilter(global::Lighthouse.Backend.Modules.ForecastingFixtures.MetricsAskThroughPorts.HoldingAForecastingService.Forecasting.Application.IForecastFilter forecastFilter)
    {
        public int CountFinished(IEnumerable<string> items) => forecastFilter.Filter(items).Count();
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.MetricsAskThroughPorts.HoldingAForecastingService.Forecasting.Application
{
    internal interface IForecastFilter
    {
        IEnumerable<string> Filter(IEnumerable<string> items);
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.MetricsAskThroughPorts.CallingAForecastingPolicy.FlowMetrics.Application
{
    internal static class AMetricsServiceCallingASufficiencyPolicy
    {
        public static bool CanForecast(int finishedDays) => global::Lighthouse.Backend.Modules.ForecastingFixtures.MetricsAskThroughPorts.CallingAForecastingPolicy.Forecasting.Domain.ASufficiencyPolicy.HasEnoughData(finishedDays);
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.MetricsAskThroughPorts.CallingAForecastingPolicy.Forecasting.Domain
{
    internal static class ASufficiencyPolicy
    {
        public static bool HasEnoughData(int finishedDays) => finishedDays >= 5;
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.MetricsAskThroughPorts.ResolvingAForecastingService.FlowMetrics.Application
{
    internal sealed class AMetricsServiceResolvingAForecaster(IServiceProvider serviceProvider)
    {
        private global::Lighthouse.Backend.Modules.ForecastingFixtures.MetricsAskThroughPorts.ResolvingAForecastingService.Forecasting.Application.IForecaster Forecaster
            => field ??= global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::Lighthouse.Backend.Modules.ForecastingFixtures.MetricsAskThroughPorts.ResolvingAForecastingService.Forecasting.Application.IForecaster>(serviceProvider);

        public int HowMany(int days) => Forecaster.HowMany(days);
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.MetricsAskThroughPorts.ResolvingAForecastingService.Forecasting.Application
{
    internal interface IForecaster
    {
        int HowMany(int days);
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.MetricsAskThroughPorts.PortInsideForecasting.FlowMetrics.Application
{
    internal static class AMetricsServiceUsingAPortForecastingDeclares
    {
        public static int HowMany(global::Lighthouse.Backend.Modules.ForecastingFixtures.MetricsAskThroughPorts.PortInsideForecasting.Forecasting.Application.IHowManyForecaster forecaster, int days)
            => forecaster.HowMany(days);
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.MetricsAskThroughPorts.PortInsideForecasting.Forecasting.Application
{
    internal interface IHowManyForecaster
    {
        int HowMany(int days);
    }

    internal sealed class AForecastAnsweringItsOwnQuestion : IHowManyForecaster
    {
        public int HowMany(int days) => days * 2;
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.MetricsAskThroughPorts.PortOnlyMetricsAnswers.FlowMetrics.Application
{
    internal interface IForecastDataSufficiency
    {
        bool HasEnoughData(int finishedDays);
    }

    internal sealed class ASufficiencyRuleCopiedIntoMetrics : IForecastDataSufficiency
    {
        public bool HasEnoughData(int finishedDays) => finishedDays >= 5;
    }
}

namespace Lighthouse.Backend.Modules.ForecastingFixtures.MetricsAskThroughPorts.PortOnlyMetricsAnswers.Forecasting.Application
{
    internal static class AForecastServiceThatAnswersSomethingElse
    {
        public static int HowMany(int days) => days;
    }
}
