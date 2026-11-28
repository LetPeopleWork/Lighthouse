using Lighthouse.Backend.Data;
using Lighthouse.Backend.Modules.OrganisationSetup.Application.Structure;
using Lighthouse.Backend.Modules.OrganisationSetup.Domain.Mapping;
using Lighthouse.Backend.Modules.OrganisationSetup.Domain.Structure;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Modules.OrganisationSetup.Adapters.Persistence
{
    public sealed class ConnectionMappingStore(LighthouseAppContext context) : IConnectionMappingStore
    {
        public async Task<ConnectionMapping> ForConnection(int connectionId, CancellationToken cancellationToken)
        {
            var stored = await context.ConnectionMappings
                .Include(mapping => mapping.Levels)
                .SingleOrDefaultAsync(mapping => mapping.ConnectionId == connectionId, cancellationToken);
            if (stored is not null)
            {
                return ToMapping(stored);
            }

            var connection = await context.WorkTrackingSystemConnections.AsNoTracking()
                .SingleOrDefaultAsync(candidate => candidate.Id == connectionId, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Connection {connectionId} does not exist, so it has no item types to map. Ask for the mapping of a stored connection; "
                    + "the connections are listed under Settings, Connections.");

            var defaults = ConnectionMapping.WithCreateDefaults(connection);
            Stage(defaults, null);
            return defaults;
        }

        public async Task<IReadOnlyList<ConnectionMapping>> All(CancellationToken cancellationToken)
        {
            var stored = await context.ConnectionMappings.AsNoTracking()
                .Include(mapping => mapping.Levels)
                .OrderBy(mapping => mapping.ConnectionId)
                .ToListAsync(cancellationToken);

            return [.. stored.Select(ToMapping)];
        }

        public async Task Save(ConnectionMapping mapping, CancellationToken cancellationToken)
        {
            var stored = await context.ConnectionMappings
                .Include(record => record.Levels)
                .SingleOrDefaultAsync(record => record.ConnectionId == mapping.ConnectionId, cancellationToken);

            Stage(mapping, stored);
            await context.SaveChangesAsync(cancellationToken);
        }

        private void Stage(ConnectionMapping mapping, ConnectionMappingRecord? stored)
        {
            if (stored is null)
            {
                stored = new ConnectionMappingRecord { ConnectionId = mapping.ConnectionId };
                context.ConnectionMappings.Add(stored);
            }

            stored.Levels.RemoveAll(level => !mapping.MappedLevels.Contains(new LevelId(level.LevelKey)));

            foreach (var level in mapping.MappedLevels)
            {
                var record = stored.Levels.Find(candidate => candidate.LevelKey == level.Key);
                if (record is null)
                {
                    record = new ConnectionLevelMappingRecord { ConnectionId = mapping.ConnectionId, LevelKey = level.Key };
                    stored.Levels.Add(record);
                }

                record.ItemTypes = [.. mapping.TypesOn(level)];
            }
        }

        private static ConnectionMapping ToMapping(ConnectionMappingRecord stored)
            => ConnectionMapping.From(stored.ConnectionId, stored.Levels.ToDictionary(
                level => new LevelId(level.LevelKey),
                level => (IReadOnlyList<string>)level.ItemTypes));
    }
}
