using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.OrganisationSetup.Application.Structure;
using Lighthouse.Backend.Modules.OrganisationSetup.Domain.Structure;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Modules.OrganisationSetup.Application
{
    /// <summary>
    /// Terminology, with the eight words that name the levels read from and written to the levels once the upgrade
    /// has built them. Their old rows are still written, so a previous release rolled back to shows the same words.
    /// </summary>
    /// <remarks>
    /// The structure store is optional only because some compositions register Terminology without the levels; there
    /// the eight words come from their rows, as before the upgrade.
    /// </remarks>
    public class TerminologyService(IRepository<TerminologyEntry> repository, IOrganisationStructureStore? structureStore = null) : ITerminologyService
    {
        public IEnumerable<TerminologyEntry> GetAll()
        {
            var entries = repository.GetAll().OrderBy(e => e.Id).ToList();
            var structure = LoadTheStructure();
            var words = structure is null ? null : TerminologyWords.Of(structure);

            return [.. entries.Select(entry => TerminologyWords.NamesALevel(entry.Key) ? AnsweredFrom(entry, words) : entry)];
        }

        public async Task UpdateTerminology(IEnumerable<TerminologyEntry> terminology)
        {
            var structure = structureStore is null ? null : await structureStore.Load(CancellationToken.None);
            var words = structure is null ? null : TerminologyWords.Of(structure);

            foreach (var entry in terminology)
            {
                var existing = repository.GetByPredicate(e => e.Key == entry.Key);
                if (existing != null)
                {
                    existing.Value = entry.Value;
                    words = words?.With(existing);
                }

                // We do not support adding new entries through this method.
            }

            if (structure is not null && words is not null)
            {
                var refused = words.RenameTheLevelsOf(structure).Where(outcome => outcome is not RenameOutcome.Renamed).ToList();
                if (refused.Count > 0)
                {
                    throw new InvalidOperationException(
                        $"Renaming the levels from Terminology was refused: {string.Join(", ", refused)}. An empty word falls back to its default, "
                        + "so this means a Terminology row has no default word; start the application again so the Terminology seeder restores it.");
                }

                await structureStore!.Save(structure, CancellationToken.None);
            }

            await repository.Save();
        }

        // GetAll is synchronous on its port; the read is one small query on the request's own context.
        private OrganisationStructure? LoadTheStructure()
            => structureStore?.Load(CancellationToken.None).GetAwaiter().GetResult();

        // An empty Value means "use the default word", and screens read it that way, so it is answered empty as
        // before; the level holds the word in use either way.
        private static TerminologyEntry AnsweredFrom(TerminologyEntry entry, TerminologyWords? words) => new()
        {
            Id = entry.Id,
            Key = entry.Key,
            Description = entry.Description,
            DefaultValue = entry.DefaultValue,
            Value = string.IsNullOrWhiteSpace(entry.Value) ? entry.Value : words?.WordFor(entry.Key) ?? entry.Value,
        };
    }
}
