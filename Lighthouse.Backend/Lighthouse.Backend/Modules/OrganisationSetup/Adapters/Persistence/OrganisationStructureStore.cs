using Lighthouse.Backend.Data;
using Lighthouse.Backend.Modules.OrganisationSetup.Application.Structure;
using Lighthouse.Backend.Modules.OrganisationSetup.Domain.Structure;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Modules.OrganisationSetup.Adapters.Persistence
{
    public sealed class OrganisationStructureStore(LighthouseAppContext context) : IOrganisationStructureStore
    {
        public async Task<OrganisationStructure?> Load(CancellationToken cancellationToken)
        {
            var stored = await context.OrganisationStructures
                .AsNoTracking()
                .Include(structure => structure.Levels)
                .SingleOrDefaultAsync(structure => structure.Id == OrganisationStructureRecord.TheOnlyKey, cancellationToken);

            return stored is null
                ? null
                : new OrganisationStructure([.. stored.Levels.Select(level => new Level(
                    new LevelId(level.Key), level.Ordinal, new TermPair(level.UnitSingular, level.UnitPlural), new TermPair(level.ItemSingular, level.ItemPlural)))]);
        }

        public async Task Save(OrganisationStructure structure, CancellationToken cancellationToken)
        {
            var stored = await context.OrganisationStructures
                .Include(record => record.Levels)
                .SingleOrDefaultAsync(record => record.Id == OrganisationStructureRecord.TheOnlyKey, cancellationToken);

            if (stored is null)
            {
                stored = new OrganisationStructureRecord();
                context.OrganisationStructures.Add(stored);
            }
            else
            {
                stored.ConcurrencyToken = Guid.NewGuid();
            }

            foreach (var level in structure.Levels)
            {
                var record = stored.Levels.Find(candidate => candidate.Key == level.Id.Key);
                if (record is null)
                {
                    record = new LevelRecord { Key = level.Id.Key };
                    stored.Levels.Add(record);
                }

                record.Ordinal = level.Ordinal;
                record.UnitSingular = level.UnitName.Singular;
                record.UnitPlural = level.UnitName.Plural;
                record.ItemSingular = level.ItemName.Singular;
                record.ItemPlural = level.ItemName.Plural;
            }

            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
