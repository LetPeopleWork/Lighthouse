using Lighthouse.Backend.Data;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The Models types that belong to a context and that nothing blocks live in that context's Domain, under
    /// Lighthouse.Backend.Modules.&lt;Owner&gt;.Domain, and nowhere under the Models or in another module. Each one is looked up by
    /// name among the top-level types of the compiled backend, and no catalogue row or movers entry names it any more. The unit
    /// tests that sat beside a moved type sit under Tests/Modules/&lt;Owner&gt;/Domain. That a moved type keeps its public members
    /// is shown by the build and by the consumer suites, which stay unchanged and only change their using lines.
    /// </summary>
    [TestFixture]
    public class ContextOwnedModelsTypesLiveInTheirModuleArchUnitTest
    {
        private static readonly (string Name, string Owner)[] TheContextTypesThatLeftTheModels =
        [
            ("AdditionalFieldInUse", "OrganisationSetup"),
            ("AppSettingKeys", "OrganisationSetup"),
            ("ConnectionFieldDefault", "OrganisationSetup"),
            ("ConnectionFieldDefaults", "OrganisationSetup"),
            ("CurrentUserProfileStatus", "Access"),
            ("DefaultHeldFor", "OrganisationSetup"),
            ("DefaultNamingAField", "OrganisationSetup"),
            ("DeliverySourceBindability", "TrackerIntegration"),
            ("DeliverySourceOption", "TrackerIntegration"),
            ("DeliverySourceProject", "TrackerIntegration"),
            ("EmbedSessionTokenRedemption", "Access"),
            ("EstimateNormalizationStatus", "FlowMetrics"),
            ("FeatureMoveVerdict", "Access"),
            ("FeatureOrderKey", "OrganisationSetup"),
            ("ForecastHorizon", "Forecasting"),
            ("ManualRankComparer", "OrganisationSetup"),
            ("OptionalFeatureKeys", "OrganisationSetup"),
            ("OwnerNamingFields", "OrganisationSetup"),
            ("OwnerTerms", "OrganisationSetup"),
            ("OwnersWhoseFetchMoved", "OrganisationSetup"),
            ("PortfolioSize", "OrganisationSetup"),
            ("RbacGuardRequirement", "Access"),
            ("RbacOperationErrorCodes", "Access"),
            ("RbacOperationResult", "Access"),
            ("RemovedAdditionalField", "OrganisationSetup"),
            ("ReportOwnerGoneException", "Reports"),
            ("ReportRefusalCodes", "Reports"),
            ("RuleSetSemantics", "WorkItems"),
            ("StaleReason", "WorkItems"),
            ("StalenessCandidate", "WorkItems"),
            ("StalenessPolicy", "WorkItems"),
            ("StalenessVerdict", "WorkItems"),
            ("StateAsOf", "FlowMetrics"),
            ("SyncOutcome", "WorkItems"),
            ("ThroughputFilterOverride", "FlowMetrics"),
            ("WriteBackValueSources", "Forecasting"),
        ];

        private static readonly (string Name, string Owner)[] TheUnitTestsThatMovedWithTheirType =
        [
            ("AdditionalFieldInUseTest", "OrganisationSetup"),
            ("DeliverySourceBindabilityTest", "TrackerIntegration"),
            ("ManualRankComparerTest", "OrganisationSetup"),
            ("OwnersWhoseFetchMovedTest", "OrganisationSetup"),
            ("PortfolioSizeTest", "OrganisationSetup"),
            ("StalenessPolicyTest", "WorkItems"),
            ("ThroughputFilterOverrideTest", "FlowMetrics"),
        ];

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(LighthouseAppContext).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(ContextOwnedModelsTypesLiveInTheirModuleArchUnitTest).Assembly));

        [TestCaseSource(nameof(TheContextMovers))]
        public void TheContextType_IsNoLongerOnTheModelsCatalogueOrItsMoversList(string name, string expectedNamespace)
        {
            var entries = WhenTheCatalogueIsSearchedFor(name);

            ThenNoEntryNamesIt(name, expectedNamespace, entries);
        }

        [TestCaseSource(nameof(TheContextMovers))]
        public void TheContextType_IsFoundOnlyInTheDomainOfItsOwner(string name, string expectedNamespace)
        {
            var places = WhenTheTypeIsLookedUpAmongTheTopLevelTypes(ProductionModule.Value, name);

            ThenItIsFoundOnlyIn(name, expectedNamespace, places);
        }

        [TestCaseSource(nameof(TheMovedUnitTests))]
        public void TheUnitTestOfAMovedType_IsFoundOnlyBesideItsTypeInTheOwnersDomainTests(string name, string expectedNamespace)
        {
            var places = WhenTheTypeIsLookedUpAmongTheTopLevelTypes(TestModule.Value, name);

            ThenTheTestIsFoundOnlyIn(name, expectedNamespace, places);
        }

        private static string TheDomainOf(string owner) => $"Lighthouse.Backend.Modules.{owner}.Domain";

        private static string TheDomainTestsOf(string owner) => $"Lighthouse.Backend.Tests.Modules.{owner}.Domain";

        private static IEnumerable<TestCaseData> TheContextMovers()
            => TheContextTypesThatLeftTheModels.Select(mover => new TestCaseData(mover.Name, TheDomainOf(mover.Owner)).SetArgDisplayNames(mover.Name));

        private static IEnumerable<TestCaseData> TheMovedUnitTests()
            => TheUnitTestsThatMovedWithTheirType.Select(moved => new TestCaseData(moved.Name, TheDomainTestsOf(moved.Owner)).SetArgDisplayNames(moved.Name));

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
                + "A context-owned type belongs to its owner's Domain: move its whole file with git mv, change only the namespace line and the using lines it needs, "
                + "and leave no copy under the Models or in another module.");
        }

        private static void ThenTheTestIsFoundOnlyIn(string name, string expectedNamespace, List<string> places)
        {
            Assert.That(
                places,
                Is.EqualTo(new List<string> { expectedNamespace }),
                $"{name} must be found once, in {expectedNamespace}; it was found in [{string.Join(", ", places)}]. "
                + "A unit test sits beside the type it tests: move its file with git mv to the matching Tests/Modules folder, "
                + "make its namespace equal to the folder, and leave none under Tests/Models.");
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
