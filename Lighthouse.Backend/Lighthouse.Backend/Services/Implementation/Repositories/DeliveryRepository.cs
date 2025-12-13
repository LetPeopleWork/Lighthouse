using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class DeliveryRepository : RepositoryBase<Delivery>, IDeliveryRepository
    {
        private readonly ILogger<DeliveryRepository> logger;

        public DeliveryRepository(LighthouseAppContext context, ILogger<DeliveryRepository> logger) 
            : base(context, (context) => context.Deliveries, logger)
        {
            this.logger = logger;
        }

        public override Delivery? GetById(int id)
        {
            logger.LogDebug("Get Delivery by Id. Id: {Id}", id);

            return GetAllDeliveriesWithIncludes()
                    .SingleOrDefault(x => x.Id == id);
        }

        public IEnumerable<Delivery> GetByPortfolioAsync(int portfolioId)
        {
            logger.LogDebug("Get Deliveries by Portfolio Id. PortfolioId: {PortfolioId}", portfolioId);

            return GetAllDeliveriesWithIncludes()
                    .Where(x => x.PortfolioId == portfolioId)
                    .ToList();
        }

        private IQueryable<Delivery> GetAllDeliveriesWithIncludes()
        {
            return Context.Deliveries
                    .Include(d => d.Portfolio)
                    .Include(d => d.Features);
        }
    }
}