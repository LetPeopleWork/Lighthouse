using Lighthouse.Backend.Data;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The Models types that belong to the platform lane and that nothing blocks live with the lane, under
    /// Lighthouse.Backend.Platform.UsageData, and nowhere under the Models or in a module. Each one is looked up by name among
    /// the top-level types of the compiled backend, and its catalogue row names the namespace it is found in.
    /// </summary>
    [TestFixture]
    public class PlatformOwnedModelsTypesLiveInPlatformArchUnitTest
    {
        // A property, not a constant: a rule check reads every string constant of the test assembly as a rule pattern.
        private static string TheLanesUsageDataNamespace => "Lighthouse.Backend.Platform.UsageData";

        private static readonly string[] TheUsageDataTypesThatLeftTheModels =
        [
            "UsageDataEmitPermit",
            "UsageDataEventReported",
            "UsageDataEventShapes",
            "UsageDataSuppressionReason",
        ];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(LighthouseAppContext).Assembly));

        [TestCaseSource(nameof(ThePlatformMovers))]
        public void ThePlatformType_IsNoLongerOnTheModelsCatalogueOrItsMoversList(string name, string expectedNamespace)
        {
            var entries = WhenTheCatalogueIsSearchedFor(name);

            ThenNoEntryNamesIt(name, expectedNamespace, entries);
        }

        [TestCaseSource(nameof(ThePlatformMovers))]
        public void ThePlatformType_IsFoundOnlyInTheLanesNamespaceOfItsArea(string name, string expectedNamespace)
        {
            var places = WhenTheTypeIsLookedUpAmongTheTopLevelTypes(ProductionModule.Value, name);

            ThenItIsFoundOnlyIn(name, expectedNamespace, places);
        }

        private static IEnumerable<TestCaseData> ThePlatformMovers()
            => TheUsageDataTypesThatLeftTheModels.Select(name => new TestCaseData(name, TheLanesUsageDataNamespace).SetArgDisplayNames(name));

        private static List<string> WhenTheCatalogueIsSearchedFor(string name)
            => [.. KernelCatalogue.Rows.Where(row => row.Type.EndsWith("." + name, StringComparison.Ordinal) || row.Type == name).Select(row => $"row {row.Type}"),
                .. KernelCatalogue.Movers.Where(mover => mover.Name == name).Select(mover => $"mover {mover.OldFullName}")];

        private static List<string> WhenTheTypeIsLookedUpAmongTheTopLevelTypes(ModuleDefinition module, string name)
            => [.. module.Types.Where(type => type.Name == name && type.DeclaringType is null).Select(type => type.Namespace).Order(StringComparer.Ordinal)];

        private static void ThenItIsFoundOnlyIn(string name, string expectedNamespace, List<string> places)
        {
            Assert.That(
                places,
                Is.EqualTo(new List<string> { expectedNamespace }),
                $"{name} must be found once, in {expectedNamespace}; it was found in [{string.Join(", ", places)}]. "
                + "A platform-owned type belongs to the platform lane: move its whole file to Platform/<Area> with only the namespace line changed, "
                + "and leave no copy under the Models or in a module.");
        }

        private static void ThenNoEntryNamesIt(string name, string expectedNamespace, List<string> entries)
        {
            Assert.That(
                entries,
                Is.Empty,
                $"{name} lives in {expectedNamespace}, so it is no longer a Models type, but the catalogue still lists [{string.Join(", ", entries)}]. "
                + "Delete its row and its movers entry from KernelCatalogue: the catalogue lists only what is still under the Models.");
        }
    }
}
