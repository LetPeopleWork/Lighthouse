using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.Repositories
{
    public interface IDeliveryMetricSnapshotRepository : IRepository<DeliveryMetricSnapshot>
    {
        DeliveryMetricSnapshot GetOrCreateForDay(int deliveryId, DateTime recordedAt);

        IEnumerable<DeliveryMetricSnapshot> GetByDelivery(int deliveryId);
    }
}
