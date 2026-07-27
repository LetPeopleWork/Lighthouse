using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.Repositories
{
    public interface IDeliveryMetricSnapshotRepository : IRepository<DeliveryMetricSnapshot>
    {
        DeliveryMetricSnapshot GetOrCreateForDay(int deliveryId, DateOnly day);

        IEnumerable<DeliveryMetricSnapshot> GetByDelivery(int deliveryId);

        IReadOnlyDictionary<int, int> GetSnapshotCountsByDelivery(IEnumerable<int> deliveryIds);
    }
}
