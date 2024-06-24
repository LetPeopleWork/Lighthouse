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

        private IEnumerable<Project> GetAllProjectsWithIncludes()
        {
            return Context.Projects
                .Include(r => r.Features).ThenInclude(f => f.RemainingWork).ThenInclude(rw => rw.Team).ThenInclude(t => t.WorkTrackingSystemOptions)
                .Include(f => f.Features).ThenInclude(f => f.Forecast).ThenInclude(f => f.SimulationResults)
                .Include(p => p.Milestones)
                .Include(p => p.WorkTrackingSystemOptions);
        }
    }
}
