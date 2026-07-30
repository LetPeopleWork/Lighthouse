using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class FeatureRepository : RepositoryBase<Feature>
    {
        public FeatureRepository(LighthouseAppContext context, ILogger<FeatureRepository> logger) : base(context, (context) => context.Features, logger)
        {
        }

        public override IEnumerable<Feature> GetAll()
        {
            var features = GetFeatures().ToList();

            return features.OrderBy(f => f, new FeatureComparer());
        }

        public override IQueryable<Feature> GetAllByPredicate(Expression<Func<Feature, bool>> predicate)
        {
            var features = GetFeatures().Where(predicate).AsEnumerable().OrderBy(f => f, new FeatureComparer());

            return features.AsQueryable();
        }

        public override Feature? GetById(int id)
        {
            return GetAll().SingleOrDefault(f => f.Id == id);
        }

        public override Feature? GetByPredicate(Func<Feature, bool> predicate)
        {
            return GetFeatures().AsEnumerable().SingleOrDefault(predicate);
        }

        private IQueryable<Feature> GetFeatures()
        {
            // Split queries are configured globally for every relational provider (DatabaseConfigurator), so S8733's Cartesian explosion cannot occur.
#pragma warning disable S8733
            return Context.Features
                .Include(f => f.Portfolios)
                .Include(f => f.FeatureWork).ThenInclude(rw => rw.Team)
                .Include(f => f.Forecasts).ThenInclude(f => f.SimulationResults);
#pragma warning restore S8733
        }
    }
}
