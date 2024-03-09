using Lighthouse.Data;
using Lighthouse.Models;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Services.Implementation.Repositories
{
    public class FeatureRepository : RepositoryBase<Feature>
    {
        public FeatureRepository(LighthouseAppContext context) : base(context, (context) => context.Features)
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
