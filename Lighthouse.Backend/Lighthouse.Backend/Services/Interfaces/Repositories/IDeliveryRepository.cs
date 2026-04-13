using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Interfaces.Repositories
{
    public interface IDeliveryRepository : IRepository<Delivery>
    {
        IEnumerable<Delivery> GetByPortfolioAsync(int portfolioId);

        Delivery? GetByIdForUpdate(int id);

        List<Feature> GetFeaturesByIds(IEnumerable<int> featureIds);
    }
}