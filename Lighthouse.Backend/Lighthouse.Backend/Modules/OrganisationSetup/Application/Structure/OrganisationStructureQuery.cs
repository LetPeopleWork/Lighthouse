using Lighthouse.Backend.Modules.OrganisationSetup.Contracts;

namespace Lighthouse.Backend.Modules.OrganisationSetup.Application.Structure
{
    public sealed class OrganisationStructureQuery(IOrganisationStructureStore structureStore, IConnectionMappingStore mappingStore) : IOrganisationStructureQuery
    {
        public async Task<StructureRead> Structure(CancellationToken cancellationToken)
        {
            var structure = await structureStore.Load(cancellationToken);
            if (structure is null)
            {
                return new StructureRead.NotMigrated();
            }

            return new StructureRead.Migrated(new OrganisationStructureView([.. structure.Levels
                .OrderBy(level => level.Ordinal)
                .Select(level => new LevelView(level.Id.Key, level.Ordinal, level.UnitName.Singular, level.UnitName.Plural, level.ItemName.Singular, level.ItemName.Plural))]));
        }

        public async Task<IReadOnlyList<ConnectionMappingView>> Mappings(CancellationToken cancellationToken)
        {
            var mappings = await mappingStore.All(cancellationToken);

            return [.. mappings.Select(mapping => new ConnectionMappingView(
                mapping.ConnectionId,
                mapping.MappedLevels.ToDictionary(level => level.Key, mapping.TypesOn, StringComparer.Ordinal),
                mapping.TypesOnBothLevels()))];
        }
    }
}
