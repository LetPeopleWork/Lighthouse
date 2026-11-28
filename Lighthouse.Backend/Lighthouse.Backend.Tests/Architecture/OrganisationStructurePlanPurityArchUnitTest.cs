using Lighthouse.Backend.Modules.OrganisationSetup.Adapters.Startup;
using Lighthouse.Backend.Modules.OrganisationSetup.Application.Structure;
using Lighthouse.Backend.Platform.Host;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The upgrade's planner is handed the units, the connections and the words, and returns what to write. Kept free
    /// of storage, clocks and I/O, every rule of it can be checked with plain values, and the start-up step stays the
    /// only place the upgrade reads or writes the database.
    /// </summary>
    [TestFixture]
    public class OrganisationStructurePlanPurityArchUnitTest
    {
        private const string StructureApplication = @"^Lighthouse\.Backend\.Modules\.OrganisationSetup\.Application\.Structure$";

        private const string StructureAndMappingDomain = @"^Lighthouse\.Backend\.Modules\.OrganisationSetup\.Domain\.(Structure|Mapping)$";

        private const string StorageClockOrIo =
            @"^Microsoft\.EntityFrameworkCore(\.|$)|^Lighthouse\.Backend\.Data(\.|$)"
            + @"|^Lighthouse\.Backend\.API(\.|$)|^Lighthouse\.Backend\.Modules\.[^.]+\.Adapters(\.|$)"
            + @"|^System\.IO(\.|$)|^System\.Net(\.|$)|^Microsoft\.Extensions\.Logging(\.|$)";

        private static readonly string[] TheClock = [typeof(ILighthouseClock).FullName!, "System.DateTime", "System.DateTimeOffset", "System.TimeProvider", "System.Random"];

        private static readonly string[] ThePlanner = [typeof(OrganisationStructureMigrationPlan).FullName!, typeof(PlannedLevel).FullName!, typeof(PlannedMapping).FullName!];

        private ModuleDefinition production = null!;

        [OneTimeSetUp]
        public void ReadTheProductionAssembly() => production = IlDependencies.ReadModule(typeof(OrganisationStructureMigrationPlan).Assembly);

        [OneTimeTearDown]
        public void Release() => production.Dispose();

        [Test]
        public void The_planner_depends_on_no_storage_clock_or_io_type()
        {
            var offenders = IlDependencies.DependentsIn(production, StructureApplication, IsStorageClockOrIo)
                .Where(dependent => ThePlanner.Contains(dependent.TypeName, StringComparer.Ordinal))
                .ToList();

            Assert.That(offenders, Is.Empty,
                $"The upgrade's planner reaches [{string.Join("; ", offenders.Select(offender => $"{offender.TypeName} -> {string.Join(", ", offender.Targets)}"))}]. "
                + "It must decide from the values it is handed alone: read and write in OrganisationStructureMigrator and pass the planner what it needs.");
        }

        [Test]
        public void The_levels_and_the_mappings_depend_on_no_storage_clock_or_io_type()
        {
            var offenders = IlDependencies.DependentsIn(production, StructureAndMappingDomain, IsStorageClockOrIo);

            Assert.That(offenders, Is.Empty,
                $"The levels or the connection mappings reach [{string.Join("; ", offenders.Select(offender => $"{offender.TypeName} -> {string.Join(", ", offender.Targets)}"))}]. "
                + "They decide from their own state alone: load and save them through the stores in Adapters.Persistence.");
        }

        [Test]
        public void The_rule_catches_a_type_that_reaches_storage()
        {
            var startUp = IlDependencies.DependentsIn(production, @"^Lighthouse\.Backend\.Modules\.OrganisationSetup\.Adapters\.Startup$", IsStorageClockOrIo)
                .Select(dependent => dependent.TypeName)
                .ToList();

            Assert.That(startUp, Does.Contain(typeof(OrganisationStructureMigrator).FullName),
                "The start-up step reads the database, yet the purity rule did not see it, so the rule could not catch a planner that does. Check the pattern it matches.");
        }

        private static bool IsStorageClockOrIo(TypeReference type)
            => IlDependencies.ResidesInNamespaceMatching(type, StorageClockOrIo) || TheClock.Contains(type.FullName, StringComparer.Ordinal);
    }
}
