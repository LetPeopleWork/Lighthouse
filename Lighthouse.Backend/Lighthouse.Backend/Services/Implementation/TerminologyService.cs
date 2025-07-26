using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation
{
    public class TerminologyService : ITerminologyService
    {
        private readonly IRepository<TerminologyEntry> repository;

        public TerminologyService(IRepository<TerminologyEntry> repository)
        {
            this.repository = repository;
        }

        public IEnumerable<TerminologyEntry> GetAll()
        {
            var entries = repository.GetAll();
            return entries;
        }

        public async Task UpdateTerminology(IEnumerable<TerminologyEntry> terminology)
        {
            foreach (var entry in terminology)
            {
                var existing = repository.GetByPredicate(e => e.Key == entry.Key);
                if (existing != null)
                {
                    existing.DefaultValue = entry.DefaultValue;
                }

                // We do not support adding new entries through this method.
            }

            await repository.Save();
        }
    }
}
