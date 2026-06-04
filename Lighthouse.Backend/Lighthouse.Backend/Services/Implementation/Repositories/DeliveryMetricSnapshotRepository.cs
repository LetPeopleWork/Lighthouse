using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class DeliveryMetricSnapshotRepository(LighthouseAppContext context, ILogger<DeliveryMetricSnapshotRepository> logger)
        : RepositoryBase<DeliveryMetricSnapshot>(context, (lighthouseAppContext) => lighthouseAppContext.DeliveryMetricSnapshots, logger), IDeliveryMetricSnapshotRepository
    {
        public DeliveryMetricSnapshot GetOrCreateForDay(int deliveryId, DateTime recordedAt)
        {
            var day = recordedAt.Date;
            var nextDay = day.AddDays(1);

            var existing = Context.DeliveryMetricSnapshots
                .Where(snapshot => snapshot.DeliveryId == deliveryId && snapshot.RecordedAt >= day && snapshot.RecordedAt < nextDay)
                .OrderBy(snapshot => snapshot.RecordedAt)
                .FirstOrDefault();

            if (existing != null)
            {
                return existing;
            }

            var snapshot = new DeliveryMetricSnapshot { DeliveryId = deliveryId, RecordedAt = day };
            Add(snapshot);
            return snapshot;
        }

        public IEnumerable<DeliveryMetricSnapshot> GetByDelivery(int deliveryId)
        {
            return Context.DeliveryMetricSnapshots
                .Where(snapshot => snapshot.DeliveryId == deliveryId)
                .OrderBy(snapshot => snapshot.RecordedAt)
                .ToList();
        }

        public IReadOnlyDictionary<int, int> GetSnapshotCountsByDelivery(IEnumerable<int> deliveryIds)
        {
            var ids = deliveryIds.Distinct().ToList();

            return Context.DeliveryMetricSnapshots
                .Where(snapshot => ids.Contains(snapshot.DeliveryId))
                .GroupBy(snapshot => snapshot.DeliveryId)
                .Select(group => new { DeliveryId = group.Key, Count = group.Count() })
                .ToDictionary(entry => entry.DeliveryId, entry => entry.Count);
        }
    }
}
