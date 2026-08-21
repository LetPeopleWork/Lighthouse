using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class DeliveryRepository(LighthouseAppContext context, ILogger<DeliveryRepository> logger)
        : RepositoryBase<Delivery>(context, (context) => context.Deliveries, logger), IDeliveryRepository
    {
        public override Delivery? GetById(int id)
        {
            return GetAllDeliveriesWithIncludes()
                    .SingleOrDefault(x => x.Id == id);
        }
        
        public override IEnumerable<Delivery> GetAll()
        {
            logger.LogDebug("Get All Deliveries");

            return GetAllDeliveriesWithIncludes()
                    .ToList();
        }

        public IEnumerable<Delivery> GetByPortfolioAsync(int portfolioId)
        {
            return GetAllDeliveriesWithIncludes()
                    .Where(x => x.PortfolioId == portfolioId)
                    .ToList();
        }

        public Delivery? GetByIdForUpdate(int id)
        {
            return Context.Deliveries
                    .Include(d => d.Features)
                    .SingleOrDefault(x => x.Id == id);
        }

        public List<Feature> GetFeaturesByIds(IEnumerable<int> featureIds)
        {
            var idList = featureIds.ToList();
            return Context.Features
                    .Include(f => f.Portfolios)
                    .Where(f => idList.Contains(f.Id))
                    .ToList();
        }

        public int? GetPortfolioId(int deliveryId)
        {
            return Context.Deliveries
                .Where(d => d.Id == deliveryId)
                .Select(d => (int?)d.PortfolioId)
                .FirstOrDefault();
        }

        private IQueryable<Delivery> GetAllDeliveriesWithIncludes()
        {
            // Split queries are configured globally for every relational provider (DatabaseConfigurator), so S8733's Cartesian explosion cannot occur.
#pragma warning disable S8733
            return Context.Deliveries
                    .Include(d => d.Portfolio.Teams)
                    .Include(d => d.Features).ThenInclude(f => f.Forecasts).ThenInclude(f => f.SimulationResults)
                    .Include(d => d.Features).ThenInclude(f => f.FeatureWork).ThenInclude(fw => fw.Team);
#pragma warning restore S8733
        }
    }
}