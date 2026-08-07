using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class FeatureRepository : RepositoryBase<Feature>
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
