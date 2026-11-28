using Lighthouse.Backend.Modules.OrganisationSetup.Application.Structure;
using Lighthouse.Backend.Modules.OrganisationSetup.Domain.Mapping;

namespace Lighthouse.Backend.Modules.OrganisationSetup.Adapters.Persistence
{
    public sealed class ConnectionMappingStore : IConnectionMappingStore
    {
        public Task<ConnectionMapping> ForConnection(int connectionId, CancellationToken cancellationToken)
            => throw new NotImplementedException($"The mapping of connection {connectionId} is not built yet.");

        public Task<IReadOnlyList<ConnectionMapping>> All(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ConnectionMapping>>([]);

        public Task Save(ConnectionMapping mapping, CancellationToken cancellationToken)
            => throw new NotImplementedException($"Saving {mapping} is not built yet.");
    }
}
