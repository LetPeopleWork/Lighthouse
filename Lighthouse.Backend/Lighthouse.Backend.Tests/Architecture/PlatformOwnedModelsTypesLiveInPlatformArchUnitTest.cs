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
        private const string PlatformOwner = "Platform";

        private const string TheLanesUsageDataNamespace = "Lighthouse.Backend.Platform.UsageData";

        private static readonly string[] TheUsageDataTypesThatLeftTheModels =
        [
            "UsageDataEmitPermit",
            "UsageDataEventReported",
            "UsageDataEventShapes",
            "UsageDataSuppressionReason",
        ];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(LighthouseAppContext).Assembly));

        [Test]
        public void ThePlatformTypesTheModelsStillHeld_AreTheFourUsageDataTypes()
        {
            var catalogued = WhenThePlatformMoversAreReadFromTheCatalogue();

            ThenTheyAreExactly(TheUsageDataTypesThatLeftTheModels, catalogued.Select(mover => mover.Name));
        }

        [TestCaseSource(nameof(ThePlatformMovers))]
        public void ThePlatformType_IsFoundOnlyInTheLanesNamespaceOfItsArea(string name, string expectedNamespace)
        {
            var places = WhenTheTypeIsLookedUpAmongTheTopLevelTypes(ProductionModule.Value, name);

            ThenItIsFoundOnlyIn(name, expectedNamespace, places);
        }

        [TestCaseSource(nameof(ThePlatformMovers))]
        public void ThePlatformTypesRow_NamesTheLanesNamespaceOfItsArea(string name, string expectedNamespace)
        {
            var row = WhenTheTypesCatalogueEntryIsRead(name);

            ThenTheEntryNames(name, expectedNamespace, row.NewNamespace);
        }

        private static IEnumerable<TestCaseData> ThePlatformMovers()
            => TheUsageDataTypesThatLeftTheModels.Select(name => new TestCaseData(name, TheLanesUsageDataNamespace).SetArgDisplayNames(name));

        private static List<MovingType> WhenThePlatformMoversAreReadFromTheCatalogue()
            => [.. KernelCatalogue.Movers.Where(mover => mover.Area == PlatformOwner)];

        private static MovingType WhenTheTypesCatalogueEntryIsRead(string name)
            => KernelCatalogue.Movers.Single(mover => mover.Name == name);

        private static List<string> WhenTheTypeIsLookedUpAmongTheTopLevelTypes(ModuleDefinition module, string name)
            => [.. module.Types.Where(type => type.Name == name).Select(type => type.Namespace).Order(StringComparer.Ordinal)];

        private static void ThenTheyAreExactly(IEnumerable<string> expected, IEnumerable<string> actual)
        {
            Assert.That(
                actual.Order(StringComparer.Ordinal).ToList(),
                Is.EqualTo(expected.Order(StringComparer.Ordinal).ToList()),
                "The platform-owned Models types the catalogue lists as free are not the four usage-data types. A platform type was added or dropped: "
                + "put it on KernelCatalogue.Movers with its owner and the namespace it moves to, and keep this list in step.");
        }

        private static void ThenItIsFoundOnlyIn(string name, string expectedNamespace, List<string> places)
        {
            Assert.That(
                places,
                Is.EqualTo(new List<string> { expectedNamespace }),
                $"{name} must be found once, in {expectedNamespace}; it was found in [{string.Join(", ", places)}]. "
                + "A platform-owned type belongs to the platform lane: move its whole file to Platform/<Area> with only the namespace line changed, "
                + "and leave no copy under the Models or in a module.");
        }

        private static void ThenTheEntryNames(string name, string expectedNamespace, string actualNamespace)
        {
            Assert.That(
                actualNamespace,
                Is.EqualTo(expectedNamespace),
                $"The catalogue row of {name} names {actualNamespace}, but a platform type lives in {expectedNamespace}. "
                + "Set the namespace it moves to on its KernelCatalogue.Movers entry.");
        }
    }
}
