using System.Text.RegularExpressions;
using Lighthouse.Backend.Data;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Flow metrics and Forecasting publish what the other contexts may name in a Contracts namespace of their own. The
    /// published types are the entries of the measured map of what each context publishes, taken without the events. Each of
    /// them is found once in the compiled code, in the Contracts namespace of its own context; none is left in the namespace
    /// it had before; and no type of the production code or the tests outside Flow metrics and Forecasting names one by its
    /// old name.
    /// </summary>
    [TestFixture]
    public class FlowMetricsAndForecastingPublishTheirContractsArchUnitTest
    {
        private static readonly string[] ThePublishers = ["FlowMetrics", "Forecasting"];

        private static readonly string[] WhatTheOtherContextsReach =
        [
            "FlowMetrics:CycleTimeFeature",
            "FlowMetrics:CycleTimeWorkItem",
            "FlowMetrics:ForecastThroughputStatus",
            "FlowMetrics:IForecastDataSufficiency",
            "FlowMetrics:IHowManyForecaster",
            "FlowMetrics:IPortfolioMetricsService",
            "FlowMetrics:ITeamMetricsService",
            "FlowMetrics:IThroughputFilter",
            "FlowMetrics:OutlierVerdict",
            "FlowMetrics:UpperNaturalProcessLimit",
            "Forecasting:ForecastRunOutcome",
            "Forecasting:IForecastFilterRuleService",
            "Forecasting:IForecastService",
        ];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(LighthouseAppContext).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(FlowMetricsAndForecastingPublishTheirContractsArchUnitTest).Assembly));

        private static IReadOnlyList<MovingType> ThePublishedTypes { get; } =
        [
            .. ContextContractsMembership.Entries
                .Select(entry => entry.Type)
                .Where(type => ThePublishers.Contains(type.Area, StringComparer.Ordinal))
                .Where(type => !type.OldNamespace.EndsWith(".Models.Events", StringComparison.Ordinal)),
        ];

        [OneTimeTearDown]
        public void DisposeModules()
        {
            if (ProductionModule.IsValueCreated)
            {
                ProductionModule.Value.Dispose();
            }

            if (TestModule.IsValueCreated)
            {
                TestModule.Value.Dispose();
            }
        }

        [Test]
        public void EachContextPublishesTheTypesTheOthersReach_AndTheListOfThemIsNotEmpty()
        {
            var published = ThePublishedTypes
                .Select(type => $"{type.Area}:{type.Name}")
                .Order(StringComparer.Ordinal)
                .ToList();

            Assert.That(
                published,
                Is.EqualTo(WhatTheOtherContextsReach),
                $"The published types of Flow metrics and Forecasting were read as [{string.Join(" | ", published)}]. Expected the ten Flow metrics types and the three Forecasting types that the other contexts reach. Why it matters: an empty or shrunk list lets every check below pass without looking at anything. How to fix: keep the map of what each context publishes complete for these two contexts.");
        }

        [Test]
        public void EachPublishedTypeIsFoundOnceAndInTheContractsOfItsOwnContext()
        {
            var module = ProductionModule.Value;

            var misplaced = ThePublishedTypes
                .Select(type => (Type: type, Found: module.Types.Where(candidate => candidate.Name == type.Name).Select(candidate => candidate.FullName).Order(StringComparer.Ordinal).ToList()))
                .Where(each => !each.Found.SequenceEqual([each.Type.NewFullName]))
                .Select(each => $"{each.Type.Name} found as [{string.Join(", ", each.Found)}], expected only {each.Type.NewFullName}")
                .ToList();

            Assert.That(
                misplaced,
                Is.Empty,
                $"These published types are not found once, in the Contracts namespace of their own context: {string.Join("; ", misplaced)}. Why it matters: the other contexts may name a context only through its Contracts, and a type kept in its old place, kept in both places or put into another context's Contracts leaves them naming something that is not published. How to fix: move each type into Modules/<Context>/Contracts with the namespace of that folder, and leave no copy behind.");
        }

        [Test]
        public void NoPublishedTypeIsLeftInTheNamespaceItHadBefore()
        {
            var module = ProductionModule.Value;

            var left = ThePublishedTypes
                .Where(type => module.Types.Any(candidate => candidate.Namespace == type.OldNamespace && candidate.Name == type.Name))
                .Select(type => type.OldFullName)
                .ToList();

            Assert.That(
                left,
                Is.Empty,
                $"These published types still live in their old namespace: {string.Join(", ", left)}. Why it matters: a type in its old place is reachable without going through the Contracts of its context. How to fix: move the type into the Contracts namespace of its context.");
        }

        [Test]
        public void NoTypeOutsideFlowMetricsAndForecastingNamesAPublishedTypeByItsOldName()
        {
            var oldNames = ThePublishedTypes.Select(type => type.OldFullName).ToHashSet(StringComparer.Ordinal);

            var insideThePublishers = $@"^{Regex.Escape(ContextContractsMembership.TheModulesRoot)}\.({string.Join("|", ThePublishers)})(\.|$)";

            var production = IlDependencies.DependentsIn(ProductionModule.Value, "^", type => oldNames.Contains(type.FullName.Replace('/', '+')))
                .Where(dependent => !Regex.IsMatch(dependent.TypeName, insideThePublishers, RegexOptions.None, TimeSpan.FromSeconds(1)));

            var tests = IlDependencies.DependentsIn(TestModule.Value, "^", type => oldNames.Contains(type.FullName.Replace('/', '+')));

            var naming = production
                .Concat(tests)
                .Select(dependent => $"{dependent.TypeName} names {string.Join(", ", dependent.Targets)}")
                .Order(StringComparer.Ordinal)
                .ToList();

            Assert.That(
                naming,
                Is.Empty,
                $"These types name a published type of Flow metrics or Forecasting by its old name: {string.Join("; ", naming)}. Why it matters: the other contexts may reach Flow metrics and Forecasting only through their Contracts, so every use must follow the move. How to fix: change the using line, or the full name written in a string, to the Contracts namespace of the context.");
        }
    }
}
