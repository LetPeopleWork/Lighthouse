using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Interfaces.Repositories
{
    public interface IDeliveryRepository : IRepository<Delivery>
    {
        IEnumerable<Delivery> GetByPortfolioAsync(int portfolioId);

        RecordableDeliveries GetRecordableByPortfolio(int portfolioId);

        Task<bool> TrySaveRecomputedDeliveries();

        Delivery? GetByIdForUpdate(int id);

        List<Feature> GetFeaturesByIds(IEnumerable<int> featureIds);

        int? GetPortfolioId(int deliveryId);

        DeliveryClosureRecord GetOrCreateClosureRecord(int deliveryId);

        IReadOnlyDictionary<int, DeliveryClosureRecord> GetClosureRecordsByDelivery(IEnumerable<int> deliveryIds);
    }
}