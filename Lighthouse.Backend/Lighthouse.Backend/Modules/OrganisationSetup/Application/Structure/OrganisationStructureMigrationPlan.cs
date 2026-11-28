using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.OrganisationSetup.Domain.Mapping;
using Lighthouse.Backend.Modules.OrganisationSetup.Domain.Structure;

namespace Lighthouse.Backend.Modules.OrganisationSetup.Application.Structure
{
    /// <summary>
    /// What the first upgrade writes, worked out from plain values only: the two levels named from Terminology, and
    /// for each connection that has units, the types per level its units there read.
    /// </summary>
    public sealed class OrganisationStructureMigrationPlan
    {
        internal OrganisationStructureMigrationPlan(IReadOnlyList<PlannedLevel> levels, IReadOnlyList<PlannedMapping> mappings)
        {
            Levels = levels;
            Mappings = mappings;
        }

        public IReadOnlyList<PlannedLevel> Levels { get; }

        public IReadOnlyList<PlannedMapping> Mappings { get; }

        internal OrganisationStructure TheStructure()
            => new([.. Levels.Select(level => new Level(level.Id, level.Ordinal, level.UnitName, level.ItemName))]);

        internal IEnumerable<ConnectionMapping> TheConnectionMappings()
            => Mappings
                .GroupBy(mapping => mapping.ConnectionId)
                .Select(connection => ConnectionMapping.From(connection.Key, connection.ToDictionary(mapping => mapping.Level, mapping => mapping.Types)));

        public static OrganisationStructureMigrationPlan From(
            IReadOnlyList<Team> teams,
            IReadOnlyList<Portfolio> portfolios,
            IReadOnlyList<WorkTrackingSystemConnection> connections,
            TerminologyWords words)
        {
            var levels = words.SeedTheStructure().Levels
                .Select(level => new PlannedLevel(level.Id, level.Ordinal, level.UnitName, level.ItemName))
                .ToList();

            var mappings = connections
                .Where(connection => OnConnection(teams, connection).Count != 0 || OnConnection(portfolios, connection).Count != 0)
                .SelectMany(connection => new[]
                {
                    MappingOf(connection, LevelId.Team, OnConnection(teams, connection)),
                    MappingOf(connection, LevelId.Portfolio, OnConnection(portfolios, connection)),
                })
                .ToList();

            return new OrganisationStructureMigrationPlan(levels, mappings);
        }

        private static List<WorkTrackingSystemOptionsOwner> OnConnection(IEnumerable<WorkTrackingSystemOptionsOwner> units, WorkTrackingSystemConnection connection)
            => [.. units.Where(unit => unit.WorkTrackingSystemConnectionId == connection.Id).OrderBy(unit => unit.Id)];

        private static PlannedMapping MappingOf(WorkTrackingSystemConnection connection, LevelId level, List<WorkTrackingSystemOptionsOwner> unitsByNumber)
        {
            var types = unitsByNumber.Count > 0
                ? TypesReadBy(unitsByNumber)
                : ConnectionMapping.WithCreateDefaults(connection).TypesOn(level);

            return new PlannedMapping(connection.Id, level, types);
        }

        // Each type is listed once whatever the case, in the order it first appears going through the units by
        // number, spelled as most units spell it; a tie goes to the spelling the lowest-numbered unit uses.
        private static List<string> TypesReadBy(List<WorkTrackingSystemOptionsOwner> unitsByNumber)
        {
            var spellingsInOrder = unitsByNumber
                .SelectMany(unit => unit.WorkItemTypes.Distinct(StringComparer.Ordinal))
                .ToList();

            return [.. spellingsInOrder
                .GroupBy(spelling => spelling, StringComparer.OrdinalIgnoreCase)
                .Select(sameType => sameType
                    .GroupBy(spelling => spelling, StringComparer.Ordinal)
                    .Select(spelling => (Spelling: spelling.Key, Units: spelling.Count(), FirstUse: spellingsInOrder.IndexOf(spelling.Key)))
                    .OrderByDescending(spelling => spelling.Units)
                    .ThenBy(spelling => spelling.FirstUse)
                    .First()
                    .Spelling)];
        }
    }
}
