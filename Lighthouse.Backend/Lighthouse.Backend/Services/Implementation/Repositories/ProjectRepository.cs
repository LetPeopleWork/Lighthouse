using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class ProjectRepository : RepositoryBase<Project>
    {
        private readonly ILogger<ProjectRepository> logger;

        public ProjectRepository(LighthouseAppContext context, ILogger<ProjectRepository> logger) : base(context, (context) => context.Projects, logger)
        {
            this.logger = logger;
        }

        public override IEnumerable<Project> GetAll()
        {
            return GetAllProjectsWithIncludes()
                .ToList();
        }

        public override Project? GetById(int id)
        {
            logger.LogDebug("Get Project by Id. Id: {id}", id);

            return GetAllProjectsWithIncludes()
                    .SingleOrDefault(x => x.Id == id);
        }

        public override void Remove(int id)
        {
            logger.LogInformation("Removing Project with {id}", id);
            var itemToRemove = Context.Projects.Find(id);

            if (itemToRemove != null)
            {
                RemoveOrphanedFeatures(id, itemToRemove);

                Context.Projects.Remove(itemToRemove);
            }

        }

        private void RemoveOrphanedFeatures(int id, Project? itemToRemove)
        {
            var orphanedFeatures = new List<Feature>();
            foreach (var feature in itemToRemove.Features)
            {
                feature.Projects.Remove(itemToRemove);
                if (feature.Projects.Count == 0)
                {
                    logger.LogInformation("Feature {feature} ({id}) is not related to any project - removing.", feature.Name, id);
                    orphanedFeatures.Add(feature);
                }
            }

            Context.Features.RemoveRange(orphanedFeatures);
        }

        private IEnumerable<Project> GetAllProjectsWithIncludes()
        {
            return Context.Projects
                .Include(r => r.Features).ThenInclude(f => f.FeatureWork).ThenInclude(rw => rw.Team).ThenInclude(t => t.WorkTrackingSystemConnection).ThenInclude(wtsc => wtsc.Options)
                .Include(f => f.Features).ThenInclude(f => f.Forecasts).ThenInclude(f => f.SimulationResults)
                .Include(p => p.WorkTrackingSystemConnection).ThenInclude(wtsc => wtsc.Options)
                .Include(p => p.Teams)
                .Include(p => p.Milestones);
        }
    }
}
