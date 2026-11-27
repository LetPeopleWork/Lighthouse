using System.Reflection;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.Forecasting.Contracts;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The forecast, reality-check and filter-rule services, their ports, the forecast lane and the forecast updater's
    /// port live in the module's Application, and neither Flow metrics list names a Forecasting type: Forecasting calls Flow
    /// metrics by right, and Flow metrics asks Forecasting only through ports it owns. The types are named as
    /// strings and read from the compiled production assembly, so this class compiles before and after the move, and a
    /// type that has not moved yet is a failure that names it.
    /// </summary>
    [TestFixture]
    public class ForecastingApplicationLayoutArchUnitTest
    {
        private const string ForecastingApplicationNamespace = "Lighthouse.Backend.Modules.Forecasting.Application";

        private const string ForecastingContractsFolder = "Modules/Forecasting/Contracts";

        private const string ForecastingApplicationFolder = "Modules/Forecasting/Application";

        // Each file declares one top-level type named like the file; the fingerprint file also holds the record of the inputs.
        private static readonly string[] ApplicationTypes =
        [
            "ForecastService",
            "ForecastInputFingerprint",
            "ForecastInputs",
            "IForecastRealityCheckService",
            "ForecastRealityCheckService",
            "ForecastFilterRuleService",
            "ForecastLane",
            "IForecastUpdater",
        ];

        private static readonly string[] PublishedTypes =
        [
            "IForecastService",
            "IForecastFilterRuleService",
            "ForecastRunOutcome",
        ];

        private static readonly string[] ApplicationFiles =
        [
            "ForecastService.cs",
            "ForecastInputFingerprint.cs",
            "IForecastRealityCheckService.cs",
            "ForecastRealityCheckService.cs",
            "ForecastFilterRuleService.cs",
            "ForecastLane.cs",
            "IForecastUpdater.cs",
        ];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        [OneTimeTearDown]
        public void DisposeModules()
        {
            if (ProductionModule.IsValueCreated)
            {
                ProductionModule.Value.Dispose();
            }
        }

        [Test]
        public void TheForecastServicesTheirPortsAndTheLane_LiveInTheModuleApplication_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, ForecastingApplicationNamespace, ApplicationTypes).Check();
        }

        [Test]
        public void TheForecastServicesTheirPortsAndTheLaneFiles_SitInTheModuleApplicationFolder_AndNowhereElse()
        {
            ThenEachFileSitsOnlyIn(ForecastingApplicationFolder, ApplicationFiles);
        }

        [Test]
        public void TheForecastingPublishedTypes_LiveInTheModuleContracts_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, typeof(IForecastService).Namespace!, PublishedTypes).Check();
        }

        [Test]
        public void TheForecastingPublishedTypeFiles_SitInTheModuleContractsFolder_AndNowhereElse()
        {
            ThenEachFileSitsOnlyIn(ForecastingContractsFolder, [.. PublishedTypes.Select(name => $"{name}.cs")]);
        }

        [Test]
        public void TheFlowMetricsInboundList_NamesNoForecastingType()
        {
            var list = ListOf(typeof(FlowMetricsInboundApplicationArchUnitTest), "OtherOutsideTypesReachingTheApplication");

            ThenTheListNamesNoForecastingType(
                list,
                "inbound",
                "Forecasting builds on Flow metrics by design, so its calls into Flow metrics' Application are legal by namespace",
                "take the entry off the list; the rule already lets the Forecasting module call Flow metrics' Application");
        }

        [Test]
        public void TheFlowMetricsOutboundList_NamesNoForecastingType()
        {
            var list = ListOf(typeof(FlowMetricsApplicationOutboundArchUnitTest), "ForecastingEdgesOfTheApplication");

            ThenTheListNamesNoForecastingType(
                list,
                "outbound",
                "Flow metrics asks Forecasting only through ports it owns, so no edge from its Application into Forecasting is allowed",
                "ask through IThroughputFilter, IForecastDataSufficiency or IHowManyForecaster and take the entry off the list");
        }

        private static IReadOnlyList<string> ListOf(Type ruleClass, string fieldName)
        {
            var field = ruleClass.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(
                field,
                Is.Not.Null,
                $"{ruleClass.Name} has no list called {fieldName}. Why it matters: the Flow metrics lists are the baseline of what may reach Forecasting, " +
                "and this check reads them. How to fix: keep the list under that name, or tell the oracle's owner it was renamed.");

            return (string[])field!.GetValue(null)!;
        }

        private static void ThenTheListNamesNoForecastingType(IReadOnlyList<string> list, string direction, string whyItMatters, string howToFix)
        {
            var forecastingEntries = list.Where(entry => entry.Contains("Forecasting", StringComparison.Ordinal)).ToList();

            Assert.That(
                forecastingEntries,
                Is.Empty,
                $"The {direction} Flow metrics list still names Forecasting types: [{string.Join(", ", forecastingEntries)}]. Why it matters: " +
                $"{whyItMatters}. How to fix: {howToFix}.");
        }

        private static void ThenEachFileSitsOnlyIn(string folder, IReadOnlyCollection<string> fileNames)
        {
            var paths = ProductionSource.Files().Select(file => file.RelativePath).ToList();

            var misplaced = fileNames
                .SelectMany(fileName => paths.Where(path => Path.GetFileName(path) == fileName && path != $"{folder}/{fileName}"))
                .ToList();
            var missing = fileNames.Where(fileName => !paths.Contains($"{folder}/{fileName}", StringComparer.Ordinal)).ToList();

            Assert.That(
                missing.Concat(misplaced),
                Is.Empty,
                $"Expected each of {string.Join(", ", fileNames)} to sit in the production project's {folder} folder and nowhere else; " +
                $"missing there: [{string.Join(", ", missing)}], found elsewhere: [{string.Join(", ", misplaced)}]. Why it matters: a " +
                "module shows its layers only when the folder matches the namespace. How to fix: move the file with git mv into " +
                $"{folder}, leaving no copy behind.");
        }
    }
}
