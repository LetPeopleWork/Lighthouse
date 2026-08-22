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

        /// <summary>
        /// The one place a retired Delivery is filtered out for the background writers. There is
        /// deliberately no global query filter doing this instead: the Portfolio screen has to keep
        /// listing retired Deliveries in a section of their own, and a filter that hid them
        /// everywhere would empty that section without anyone asking it to.
        /// </summary>
        public RecordableDeliveries GetRecordableByPortfolio(int portfolioId)
        {
            return new RecordableDeliveries(
                [.. GetAllDeliveriesWithIncludes().Where(x => x.PortfolioId == portfolioId && x.ArchivedOn == null)]);
        }

        /// <summary>
        /// Saves what a background pass worked out, and lets go of any Delivery somebody changed while
        /// that pass was running instead of writing over them. The pass is holding copies it read
        /// before those changes happened, so its version of the Delivery is the older one. Returns
        /// false when something was let go of.
        ///
        /// The refused change has to be dropped from the session, and dropping it means detaching the
        /// Delivery, not refreshing it. Refreshing it re-reads the version number the database refused
        /// the write against, which leaves the same change pending against a version that now matches
        /// - so the next save in this session writes it through, and the refusal is undone by the
        /// attempt to recover from it. Leaving it pending instead means the next save is refused too,
        /// and that refusal reaches a caller with no idea what a Delivery is.
        /// </summary>
        public async Task<bool> TrySaveRecomputedDeliveries()
        {
            try
            {
                await Save();
                return true;
            }
            catch (DbUpdateConcurrencyException exception) when (EveryConflictIsADelivery(exception))
            {
                logger.LogInformation(
                    exception,
                    "Letting go of {DeliveryCount} Deliveries that were changed while this refresh was running",
                    exception.Entries.Count);

                foreach (var entry in exception.Entries)
                {
                    entry.State = EntityState.Detached;
                }

                return false;
            }
        }

        private static bool EveryConflictIsADelivery(DbUpdateConcurrencyException exception)
        {
            return exception.Entries.Count > 0 && exception.Entries.All(entry => entry.Entity is Delivery);
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

        public bool IsArchived(int deliveryId)
        {
            return Context.Deliveries
                .Any(d => d.Id == deliveryId && d.ArchivedOn != null);
        }

        /// <summary>
        /// One row per Delivery, so archiving a Delivery that was already archived once overwrites the
        /// pin rather than adding a second one nobody could choose between.
        /// </summary>
        public DeliveryClosureRecord GetOrCreateClosureRecord(int deliveryId)
        {
            var existing = Context.DeliveryClosureRecords
                .FirstOrDefault(record => record.DeliveryId == deliveryId);

            if (existing != null)
            {
                return existing;
            }

            var closureRecord = new DeliveryClosureRecord { DeliveryId = deliveryId };
            Context.DeliveryClosureRecords.Add(closureRecord);
            return closureRecord;
        }

        public IReadOnlyDictionary<int, DeliveryClosureRecord> GetClosureRecordsByDelivery(IEnumerable<int> deliveryIds)
        {
            var ids = deliveryIds.Distinct().ToList();

            return Context.DeliveryClosureRecords
                .Where(record => ids.Contains(record.DeliveryId))
                .ToDictionary(record => record.DeliveryId);
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