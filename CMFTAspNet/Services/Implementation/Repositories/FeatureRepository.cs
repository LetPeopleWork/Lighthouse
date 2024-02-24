using CMFTAspNet.Models;
using Microsoft.EntityFrameworkCore;

namespace CMFTAspNet.Services.Implementation.Repositories
{
    public class FeatureRepository : RepositoryBase<Feature>
    {
        public FeatureRepository(AppContext context) : base(context, (context) => context.Features)
        {            
        }

        public override IEnumerable<Feature> GetAll()
        {
            return Context.Features
                .Include(f => f.Project)
                .Include(f => f.RemainingWork).ThenInclude(rw => rw.Team)
                .Include(f => f.Forecast).ThenInclude(f => f.SimulationResults);
        }
    }
}
