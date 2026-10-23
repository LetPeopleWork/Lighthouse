using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class FeatureRepository : RepositoryBase<Feature>, IFeatureRepository
    {
        private readonly IFeatureOrdering featureOrdering;

        public FeatureRepository(LighthouseAppContext context, IFeatureOrdering featureOrdering, ILogger<FeatureRepository> logger) : base(context, (context) => context.Features, logger)
        {
            this.featureOrdering = featureOrdering;
        }

        public override IEnumerable<Feature> GetAll()
        {
            return featureOrdering.Order(GetFeatures().ToList());
        }

        /// <summary>
        /// A plain update rather than a compare-and-set: a forecast rewritten by another run in the
        /// meantime is newer still, so reading it as current at this moment is true of it as well.
        /// </summary>
        public async Task ConfirmForecastsAsOf(IReadOnlyCollection<int> featureIds, DateTime confirmedAt, CancellationToken cancellationToken)
        {
            await Context.Set<WhenForecast>()
                .Where(forecast => featureIds.Contains(forecast.FeatureId) && forecast.CreationTime < confirmedAt)
                .ExecuteUpdateAsync(setters => setters.SetProperty(forecast => forecast.CreationTime, confirmedAt), cancellationToken);

            await Context.Set<StartForecast>()
                .Where(forecast => featureIds.Contains(forecast.FeatureId) && forecast.CreationTime < confirmedAt)
                .ExecuteUpdateAsync(setters => setters.SetProperty(forecast => forecast.CreationTime, confirmedAt), cancellationToken);
        }

        public override IQueryable<Feature> GetAllByPredicate(Expression<Func<Feature, bool>> predicate)
        {
            return featureOrdering.Order(GetFeatures().Where(predicate).AsEnumerable()).AsQueryable();
        }

        public override Feature? GetById(int id)
        {
            return GetAll().SingleOrDefault(f => f.Id == id);
        }

        public override Feature? GetByPredicate(Func<Feature, bool> predicate)
        {
            return GetFeatures().AsEnumerable().SingleOrDefault(predicate);
        }

        public IReadOnlyCollection<string> GetAllReferenceIds()
        {
            return Context.Features.Select(feature => feature.ReferenceId).ToList();
        }

        public StoredFeatures GetStoredFeatureCreatedDates()
            => new(Context.Features
                .Select(feature => new { feature.ReferenceId, feature.CreatedDate })
                .AsEnumerable()
                .GroupBy(feature => feature.ReferenceId, StringComparer.Ordinal)
                .ToDictionary(
                    sameId => sameId.Key,
                    sameId => sameId.Select(feature => feature.CreatedDate).Where(created => created != DateTime.MinValue).Min(),
                    StringComparer.Ordinal));

        private IQueryable<Feature> GetFeatures()
        {
            // Split queries are configured globally for every relational provider (DatabaseConfigurator), so S8733's Cartesian explosion cannot occur.
#pragma warning disable S8733
            return Context.Features
                .Include(f => f.Portfolios)
                .Include(f => f.FeatureWork).ThenInclude(rw => rw.Team)
                .Include(f => f.Forecasts).ThenInclude(f => f.SimulationResults)
                // Loaded beside Forecasts rather than left to lazy loading, which is off: a Feature read
                // through here without this reports no start dates at all, everywhere in the product,
                // while any test holding the entity in the change tracker still sees them.
                .Include(f => f.StartForecasts).ThenInclude(f => f.SimulationResults)
                .Include(f => f.DependsOnReferences);
#pragma warning restore S8733
        }
    }
}
