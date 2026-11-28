namespace Lighthouse.Backend.Modules.OrganisationSetup.Contracts
{
    public interface IOrganisationStructureQuery
    {
        Task<StructureRead> Structure(CancellationToken cancellationToken);

        Task<IReadOnlyList<ConnectionMappingView>> Mappings(CancellationToken cancellationToken);
    }
}
