using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class DeliveryMetricSnapshotRepository(LighthouseAppContext context, ILogger<DeliveryMetricSnapshotRepository> logger)
        : RepositoryBase<DeliveryMetricSnapshot>(context, (lighthouseAppContext) => lighthouseAppContext.DeliveryMetricSnapshots, logger), IDeliveryMetricSnapshotRepository
    {
        /// <summary>
        /// The day travels in as a <see cref="DateOnly"/>: the caller owns the anchor decision, and
        /// no instant is reduced to a calendar day inside the repository any more (Bug #5567).
        /// Matching is EQUALITY on the persisted day key, backed by the unique
        /// (DeliveryId, RecordedDay) index, rather than a half-open range scan over instants.
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

                // Expand phase: the legacy instant column keeps being written at the day's midnight
                // so a rollback to the previous release still reads correct data.
                RecordedAt = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
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
