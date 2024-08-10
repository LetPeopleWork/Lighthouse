using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.History;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation
{
    public class FeatureHistoryService : IFeatureHistoryService
    {
        private readonly IRepository<FeatureHistoryEntry> repository;

        public FeatureHistoryService(IRepository<FeatureHistoryEntry> repository)
        {
            this.repository = repository;
        }

        public async Task ArchiveFeature(Feature feature)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var historyEntry = repository.GetByPredicate(fh => fh.FeatureId == feature.Id && fh.Snapshot == today);

            if (historyEntry == null)
            {
                AddNewFeatureHistoryEntry(feature);
            }
            else
            {
                UpdateExistingFeatureHistoryEntry(feature, historyEntry);
            }

            await repository.Save();
        }

        private void UpdateExistingFeatureHistoryEntry(Feature feature, FeatureHistoryEntry? historyEntry)
        {
            historyEntry.Update(feature);
            repository.Update(historyEntry);
        }

        private void AddNewFeatureHistoryEntry(Feature feature)
        {
            var historyEntry = new FeatureHistoryEntry(feature);
            repository.Add(historyEntry);
        }
    }
}
