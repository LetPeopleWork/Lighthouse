using Lighthouse.Backend.Modules.OrganisationSetup.Domain.Structure;

namespace Lighthouse.Backend.Modules.OrganisationSetup.Application.Structure
{
    public interface IOrganisationStructureStore
    {
        /// <returns>The stored structure, or null only before the first upgrade has run.</returns>
        Task<OrganisationStructure?> Load(CancellationToken cancellationToken);

        Task Save(OrganisationStructure structure, CancellationToken cancellationToken);
    }
}
