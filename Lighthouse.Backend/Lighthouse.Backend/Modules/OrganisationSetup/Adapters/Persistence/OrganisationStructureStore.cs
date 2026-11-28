using Lighthouse.Backend.Modules.OrganisationSetup.Application.Structure;
using Lighthouse.Backend.Modules.OrganisationSetup.Domain.Structure;

namespace Lighthouse.Backend.Modules.OrganisationSetup.Adapters.Persistence
{
    public sealed class OrganisationStructureStore : IOrganisationStructureStore
    {
        public Task<OrganisationStructure?> Load(CancellationToken cancellationToken)
            => throw new NotImplementedException("Loading the organisation structure is not built yet.");

        public Task Save(OrganisationStructure structure, CancellationToken cancellationToken)
            => throw new NotImplementedException($"Saving {structure} is not built yet.");
    }
}
