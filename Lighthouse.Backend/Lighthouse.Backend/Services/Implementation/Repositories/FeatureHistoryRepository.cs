using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.History;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class FeatureHistoryRepository : RepositoryBase<FeatureHistoryEntry>
    {
        public FeatureHistoryRepository(LighthouseAppContext context, ILogger<FeatureHistoryRepository> logger) : base(context, (context) => context.FeatureHistory, logger)
        {
        }

        public override IEnumerable<FeatureHistoryEntry> GetAll()
        {
            return GetFeatureHistoryWithInlcudes();
        }

        public override FeatureHistoryEntry? GetByPredicate(Func<FeatureHistoryEntry, bool> predicate)
        {
            return GetFeatureHistoryWithInlcudes()
                .SingleOrDefault(predicate);
        }

        public override IQueryable<FeatureHistoryEntry> GetAllByPredicate(Expression<Func<FeatureHistoryEntry, bool>> predicate)
        {
            return GetFeatureHistoryWithInlcudes()
                .Where(predicate);
        }

        private IQueryable<FeatureHistoryEntry> GetFeatureHistoryWithInlcudes()
        {
            return Context.FeatureHistory
                .Include(f => f.FeatureWork)
                .Include(f => f.Forecasts).ThenInclude(f => f.SimulationResults);
        }
    }
}
