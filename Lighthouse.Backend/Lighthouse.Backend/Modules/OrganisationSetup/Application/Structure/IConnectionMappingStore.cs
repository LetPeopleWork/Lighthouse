using Lighthouse.Backend.Modules.OrganisationSetup.Domain.Mapping;

namespace Lighthouse.Backend.Modules.OrganisationSetup.Application.Structure
{
    public interface IConnectionMappingStore
    {
        /// <returns>
        /// The stored mapping, or a new one with the create defaults, saved with the caller's unit of work.
        /// </returns>
        Task<ConnectionMapping> ForConnection(int connectionId, CancellationToken cancellationToken);

        Task<IReadOnlyList<ConnectionMapping>> All(CancellationToken cancellationToken);

        Task Save(ConnectionMapping mapping, CancellationToken cancellationToken);
    }
}
