using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Modules.OrganisationSetup.Application.Structure
{
    public sealed class OrganisationStructureMigrationPlan
    {
        internal OrganisationStructureMigrationPlan(IReadOnlyList<PlannedLevel> levels, IReadOnlyList<PlannedMapping> mappings)
        {
            Levels = levels;
            Mappings = mappings;
        }

        public IReadOnlyList<PlannedLevel> Levels { get; }

        public IReadOnlyList<PlannedMapping> Mappings { get; }

        public static OrganisationStructureMigrationPlan From(
            IReadOnlyList<Team> teams,
            IReadOnlyList<Portfolio> portfolios,
            IReadOnlyList<WorkTrackingSystemConnection> connections,
            TerminologyWords words)
            => throw new NotImplementedException(
                $"Planning {teams.Count} Teams and {portfolios.Count} Portfolios on {connections.Count} connections with {words} is not built yet.");
    }
}
