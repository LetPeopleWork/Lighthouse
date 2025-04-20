using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class TeamRepository : RepositoryBase<Team>
    {
        private readonly ILogger<TeamRepository> logger;

        public TeamRepository(LighthouseAppContext context, ILogger<TeamRepository> logger) : base(context, (context) => context.Teams, logger)
        {
            this.logger = logger;
        }

        public override IEnumerable<Team> GetAll()
        {
            return Context.Teams
                .Include(x => x.WorkTrackingSystemConnection)
                    .ThenInclude(wtsc => wtsc.Options)
                .Include(x => x.Projects)
                    .ThenInclude(p => p.Features)
                        .ThenInclude(f => f.FeatureWork)
                            .ThenInclude(rw => rw.Team);
        }

        public override Team? GetById(int id)
        {
            logger.LogDebug("Getting Team by Id. Id {id}", id);
            return GetAll()
                .SingleOrDefault(t => t.Id == id);
        }
    }
}
