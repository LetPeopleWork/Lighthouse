using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.OrganisationSetup.Application.Structure;
using Lighthouse.Backend.Modules.OrganisationSetup.Domain.Mapping;
using Lighthouse.Backend.Modules.OrganisationSetup.Domain.Structure;
using Lighthouse.Backend.Platform.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Modules.OrganisationSetup.Adapters.Startup
{
    /// <summary>
    /// Builds the two levels and every connection's type mapping on the first start after the upgrade, in one
    /// transaction that is checked before it commits. Later starts find the structure and leave it alone.
    /// </summary>
    public sealed class OrganisationStructureMigrator(
        LighthouseAppContext context,
        IOrganisationStructureStore structureStore,
        IConnectionMappingStore mappingStore,
        ILogger<OrganisationStructureMigrator> logger) : ISeeder
    {
        public async Task Seed()
        {
            if (await structureStore.Load(CancellationToken.None) is not null)
            {
                logger.LogDebug("The organisation structure exists already; nothing to upgrade");
                return;
            }

            if (context.ChangeTracker.HasChanges())
            {
                throw new InvalidOperationException(
                    "The upgrade to levels did not start: the shared database context holds changes an earlier start-up step staged and did not save, "
                    + "and the upgrade would either commit or discard them. Save them in the step that staged them before the organisation structure is built.");
            }

            // Postgres runs with a retrying execution strategy, which refuses a user-initiated transaction
            // unless the whole unit of work is handed to it.
            var strategy = context.Database.CreateExecutionStrategy();
            try
            {
                await strategy.ExecuteAsync(TheFirstUpgrade);
            }
            catch (DbUpdateException exception)
            {
                context.ChangeTracker.Clear();
                if (await structureStore.Load(CancellationToken.None) is null)
                {
                    throw;
                }

                logger.LogInformation(exception, "Another instance built the organisation structure first; using its levels and mappings");
            }
        }

        private async Task TheFirstUpgrade()
        {
            // The context was empty when the upgrade began, so this drops only what a failed earlier attempt of
            // this upgrade staged before the execution strategy retried it.
            context.ChangeTracker.Clear();
            await using var transaction = await context.Database.BeginTransactionAsync();

            var teams = await context.Teams.AsNoTracking().ToListAsync();
            var portfolios = await context.Portfolios.AsNoTracking().ToListAsync();
            var connections = await context.WorkTrackingSystemConnections.AsNoTracking().ToListAsync();
            var terminology = await context.TerminologyEntries.AsNoTracking().ToListAsync();

            var plan = OrganisationStructureMigrationPlan.From(teams, portfolios, connections, TerminologyWords.From(terminology));

            await structureStore.Save(plan.TheStructure(), CancellationToken.None);

            foreach (var mapping in plan.TheConnectionMappings())
            {
                await mappingStore.Save(mapping, CancellationToken.None);
            }

            var lost = TypesNoLevelLists([.. teams, .. portfolios], await mappingStore.All(CancellationToken.None));
            if (lost.Count > 0)
            {
                // Disposing the transaction without committing rolls every write of this upgrade back.
                throw new InvalidOperationException(
                    "The upgrade to levels stopped and changed nothing: its check that every unit keeps all its item types failed for "
                    + string.Join("; ", lost) + ". "
                    + "Each type a Team or Portfolio reads must be listed on its level of its connection. Restore the previous release to keep working "
                    + "and report this message with the units it names.");
            }

            await transaction.CommitAsync();
            logger.LogInformation("Built the organisation structure: {Levels} levels, {Connections} connection mappings",
                plan.Levels.Count, plan.Mappings.Select(mapping => mapping.ConnectionId).Distinct().Count());
        }

        private static List<string> TypesNoLevelLists(IEnumerable<WorkTrackingSystemOptionsOwner> units, IReadOnlyList<ConnectionMapping> mappings)
        {
            var byConnection = mappings.ToDictionary(mapping => mapping.ConnectionId);

            return [.. units.SelectMany(unit =>
            {
                var level = LevelId.Of(unit);
                var mapping = byConnection.GetValueOrDefault(unit.WorkTrackingSystemConnectionId)
                    ?? ConnectionMapping.From(unit.WorkTrackingSystemConnectionId, new Dictionary<LevelId, IReadOnlyList<string>>());
                return unit.WorkItemTypes
                    .Where(type => mapping.Resolve(level, [type]) is ResolveOutcome.TypeNotMapped)
                    .Select(type => $"{level.Key} {unit.Id} ({unit.Name}) reads {type}, which connection {unit.WorkTrackingSystemConnectionId} does not list on that level");
            })];
        }
    }
}
