using Lighthouse.Backend.Modules.OrganisationSetup.Contracts;

namespace Lighthouse.Backend.Modules.OrganisationSetup.Application.Structure
{
    public sealed class OrganisationStructureQuery : IOrganisationStructureQuery
    {
        public Task<StructureRead> Structure(CancellationToken cancellationToken)
            => Task.FromResult<StructureRead>(new StructureRead.NotMigrated());

        public Task<IReadOnlyList<ConnectionMappingView>> Mappings(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ConnectionMappingView>>([]);
    }
}
