using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class TerminologyRepository : RepositoryBase<TerminologyEntry>, IRepository<TerminologyEntry>
    {
        public TerminologyRepository(LighthouseAppContext context, ILogger<TerminologyRepository> logger) 
            : base(context, (LighthouseAppContext context) => context.TerminologyEntries, logger)
        {
            SeedTerminology();
        }

        private void SeedTerminology()
        {
            AddIfNotExists("workItem", "Work Item", "High level name of item that a team works on");
            AddIfNotExists("workItems", "Work Items", "Plural form of Work Item");

            SaveSync();
        }

        private void AddIfNotExists(string key, string defaultValue, string descrption)
        {
            var terminologyEntry = new TerminologyEntry
            {
                Key = key,
                DefaultValue = defaultValue,
                Description = descrption
            };

            var existingEntry = GetByPredicate((entry) => entry.Key == terminologyEntry.Key);
            if (existingEntry == null)
            {
                Add(terminologyEntry);
            }
        }
    }
}
