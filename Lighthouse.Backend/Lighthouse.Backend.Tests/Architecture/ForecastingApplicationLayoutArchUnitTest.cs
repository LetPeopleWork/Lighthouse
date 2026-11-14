using System.Reflection;
using Lighthouse.Backend.Models;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The forecast, reality-check and filter-rule services, their ports, the forecast lane and the forecast updater's
    /// port live in the module's Application, and the two Flow metrics rule lists name them there. The types are named as
    /// strings and read from the compiled production assembly, so this class compiles before and after the move, and a
    /// type that has not moved yet is a failure that names it.
    /// </summary>
    [TestFixture]
    public class ForecastingApplicationLayoutArchUnitTest
    {
        private const string ForecastingApplicationNamespace = "Lighthouse.Backend.Modules.Forecasting.Application";

        private const string ForecastingApplicationFolder = "Modules/Forecasting/Application";

        // Each file declares one top-level type named like the file; the fingerprint file also holds the record of the inputs.
        private static readonly string[] ApplicationTypes =
        [
            "IForecastService",
            "ForecastService",
            "ForecastInputFingerprint",
            "ForecastInputs",
            "IForecastRealityCheckService",
            "ForecastRealityCheckService",
            "IForecastFilterRuleService",
            "ForecastFilterRuleService",
            "ForecastLane",
            "IForecastUpdater",
        ];

        private static readonly string[] ApplicationFiles =
        [
            "IForecastService.cs",
            "ForecastService.cs",
            "ForecastInputFingerprint.cs",
            "IForecastRealityCheckService.cs",
            "ForecastRealityCheckService.cs",
            "IForecastFilterRuleService.cs",
            "ForecastFilterRuleService.cs",
            "ForecastLane.cs",
            "IForecastUpdater.cs",
        ];

        // What the Flow metrics lists must name, written out: the outside types that reach the module's Application, and the
        // Forecasting types the module's Application reaches.
        private static readonly string[] InboundListMustName =
        [
            "Lighthouse.Backend.Modules.Forecasting.Application.ForecastInputFingerprint",
            "Lighthouse.Backend.Modules.Forecasting.Application.ForecastInputs",
            "Lighthouse.Backend.Modules.Forecasting.Application.ForecastRealityCheckService",
            "Lighthouse.Backend.Modules.Forecasting.Application.ForecastService",
            "Lighthouse.Backend.Modules.Forecasting.Application.ForecastService+GatheredInputs",
        ];

        private static readonly string[] OutboundListMustName =
        [
            "Lighthouse.Backend.Modules.FlowMetrics.Application.BaseMetricsService -> Lighthouse.Backend.Modules.Forecasting.Application.IForecastService",
            "Lighthouse.Backend.Modules.FlowMetrics.Application.TeamMetricsService -> Lighthouse.Backend.Modules.Forecasting.Application.IForecastFilterRuleService",
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
        public void TheFlowMetricsInboundList_NamesTheForecastTypesInTheModuleApplication()
        {
            var list = ListOf(typeof(FlowMetricsInboundApplicationArchUnitTest), "OtherOutsideTypesReachingTheApplication");

            ThenTheListNamesTheMovedTypesOnlyInTheirNewPlace(list, InboundListMustName, "inbound");
        }

        [Test]
        public void TheFlowMetricsOutboundList_NamesTheForecastTypesInTheModuleApplication()
        {
            var list = ListOf(typeof(FlowMetricsApplicationOutboundArchUnitTest), "ForecastingEdgesOfTheApplication");

            ThenTheListNamesTheMovedTypesOnlyInTheirNewPlace(list, OutboundListMustName, "outbound");
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

        private static void ThenTheListNamesTheMovedTypesOnlyInTheirNewPlace(IReadOnlyList<string> list, IReadOnlyCollection<string> mustName, string direction)
        {
            var formerNames = ApplicationTypes
                .SelectMany(name => list.Where(entry => entry.Contains($"Services.") && EntryNamesType(entry, name)))
                .Distinct()
                .ToList();
            var missing = mustName.Where(entry => !list.Contains(entry, StringComparer.Ordinal)).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    formerNames,
                    Is.Empty,
                    $"The {direction} Flow metrics list still names moved types under a Services namespace: [{string.Join(", ", formerNames)}]. Why it matters: " +
                    "the list is the baseline the rule compares in both directions, so a stale name fails the rule and hides which type the edge is about. " +
                    "How to fix: rewrite each entry to the type's full name in Lighthouse.Backend.Modules.Forecasting.Application.");

                Assert.That(
                    missing,
                    Is.Empty,
                    $"The {direction} Flow metrics list does not name: [{string.Join(", ", missing)}]. Why it matters: a missing entry makes the rule report " +
                    "an edge that is allowed. How to fix: add the entry with the type's full name in Lighthouse.Backend.Modules.Forecasting.Application.");
            }
        }

        private static bool EntryNamesType(string entry, string simpleName)
        {
            var target = entry.Contains(" -> ", StringComparison.Ordinal) ? entry[(entry.IndexOf(" -> ", StringComparison.Ordinal) + 4)..] : entry;
            var typePart = target[(target.LastIndexOf('.') + 1)..];

            return typePart == simpleName || typePart.StartsWith($"{simpleName}+", StringComparison.Ordinal);
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
