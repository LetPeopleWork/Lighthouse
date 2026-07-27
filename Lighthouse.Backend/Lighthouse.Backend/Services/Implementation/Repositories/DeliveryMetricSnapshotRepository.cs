using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class DeliveryMetricSnapshotRepository(LighthouseAppContext context, ILogger<DeliveryMetricSnapshotRepository> logger)
        : RepositoryBase<DeliveryMetricSnapshot>(context, (lighthouseAppContext) => lighthouseAppContext.DeliveryMetricSnapshots, logger), IDeliveryMetricSnapshotRepository
    {
        /// <summary>
        /// The caller owns the anchor decision: the day travels in as a <see cref="DateOnly"/> and
        /// matching is equality on the persisted day key (Bug #5567).
        /// </summary>
        public DeliveryMetricSnapshot GetOrCreateForDay(int deliveryId, DateOnly day)
        {
            var existing = Context.DeliveryMetricSnapshots
                .FirstOrDefault(snapshot => snapshot.DeliveryId == deliveryId && snapshot.RecordedDay == day);

            if (existing != null)
            {
                return existing;
            }

            var snapshot = new DeliveryMetricSnapshot
            {
                DeliveryId = deliveryId,
                RecordedDay = day,

                // Expand phase: the legacy column keeps being written so a rollback still reads right.
                RecordedAt = InstanceCalendar.AsUtcMidnight(day),
            };
            Add(snapshot);
            return snapshot;
        }

        public IEnumerable<DeliveryMetricSnapshot> GetByDelivery(int deliveryId)
        {
            return Context.DeliveryMetricSnapshots
                .Where(snapshot => snapshot.DeliveryId == deliveryId)
                .OrderBy(snapshot => snapshot.RecordedDay)
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
