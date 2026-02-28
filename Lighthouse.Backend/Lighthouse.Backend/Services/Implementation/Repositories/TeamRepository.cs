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
                .Include(x => x.WorkTrackingSystemConnection)
                    .ThenInclude(wtsc => wtsc.AdditionalFieldDefinitions)
                .Include(x => x.WorkTrackingSystemConnection)
                    .ThenInclude(wtsc => wtsc.WriteBackMappingDefinitions)
                .Include(x => x.Portfolios)
                    .ThenInclude(p => p.Features)
                        .ThenInclude(f => f.FeatureWork)
                            .ThenInclude(rw => rw.Team);
        }

        public override Team? GetById(int id)
        {
            logger.LogDebug("Getting Team by Id. Id {Id}", id);
            return GetAll()
                .SingleOrDefault(t => t.Id == id);
        }

        public override void Remove(int id)
        {
            logger.LogInformation("Removing Team with {Id} and associated FeatureWork", id);

            var featureWorkToRemove = Context.Set<FeatureWork>()
                .Where(fw => fw.TeamId == id)
                .ToList();

            if (featureWorkToRemove.Count > 0)
            {
                logger.LogInformation("Removing {Count} FeatureWork entries for Team {Id}", featureWorkToRemove.Count, id);
                Context.Set<FeatureWork>().RemoveRange(featureWorkToRemove);
            }

            var teamToRemove = Context.Teams.Find(id);
            if (teamToRemove != null)
            {
                Context.Teams.Remove(teamToRemove);
            }
        }
    }
}
