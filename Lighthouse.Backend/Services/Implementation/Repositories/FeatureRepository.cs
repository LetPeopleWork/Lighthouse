using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class FeatureRepository : RepositoryBase<Feature>
    {
        public FeatureRepository(LighthouseAppContext context, ILogger<FeatureRepository> logger) : base(context, (context) => context.Features, logger)
        {            
        }

        public override IEnumerable<Feature> GetAll()
        {
            return Context.Features
                .Include(f => f.Project)
                    .ThenInclude(p => p.Milestones)
                .Include(f => f.RemainingWork).ThenInclude(rw => rw.Team)
                .Include(f => f.Forecast).ThenInclude(f => f.SimulationResults);
        }
    }
}
