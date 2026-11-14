using Lighthouse.Backend.Models;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The forecast updater and the start-up seeder that forgets stale forecast inputs live in the module's background
    /// adapters, and the four forecast triggers live in its policies. The types are named as strings and read from the
    /// compiled production assembly, so this class compiles before and after the move, and a type that has not moved yet is
    /// a failure that names it. The order the handlers run in is pinned by DomainEventHandlerOrderPinTest, which must stay
    /// green as it is.
    /// </summary>
    [TestFixture]
    public class ForecastingAdaptersAndPoliciesLayoutArchUnitTest
    {
        private const string BackgroundNamespace = "Lighthouse.Backend.Modules.Forecasting.Adapters.Background";

        private const string BackgroundFolder = "Modules/Forecasting/Adapters/Background";

        private const string PoliciesNamespace = "Lighthouse.Backend.Modules.Forecasting.Application.Policies";

        private const string PoliciesFolder = "Modules/Forecasting/Application/Policies";

        // Each file declares one top-level type named like the file.
        private static readonly string[] BackgroundAdapters =
        [
            "ForecastUpdater",
            "ForecastInputsForgottenAtStartSeeder",
        ];

        private static readonly string[] Policies =
        [
            "TeamDataRefreshedForecastTriggerHandler",
            "TeamDeletedForecastRetriggerHandler",
            "FeatureRankChangedForecastTriggerHandler",
            "FeatureOrderingPolicyChangedForecastTriggerHandler",
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
        public void TheForecastUpdaterAndTheStartUpSeeder_LiveInTheModuleBackgroundAdapters_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, BackgroundNamespace, BackgroundAdapters).Check();
        }

        [Test]
        public void TheForecastUpdaterAndTheStartUpSeederFiles_SitInTheModuleBackgroundFolder_AndNowhereElse()
        {
            ThenEachFileSitsOnlyIn(BackgroundFolder, [.. BackgroundAdapters.Select(name => $"{name}.cs")]);
        }

        [Test]
        public void TheFourForecastTriggers_LiveInTheModulePolicies_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, PoliciesNamespace, Policies).Check();
        }

        [Test]
        public void TheFourForecastTriggerFiles_SitInTheModulePoliciesFolder_AndNowhereElse()
        {
            ThenEachFileSitsOnlyIn(PoliciesFolder, [.. Policies.Select(name => $"{name}.cs")]);
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
