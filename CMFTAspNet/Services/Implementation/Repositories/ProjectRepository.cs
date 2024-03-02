using CMFTAspNet.Models;
using Microsoft.EntityFrameworkCore;

namespace CMFTAspNet.Services.Implementation.Repositories
{
    public class ProjectRepository : RepositoryBase<Project>
    {
        public ProjectRepository(AppContext context) : base(context, (context) => context.Projects)
        {
        }

        public override IEnumerable<Project> GetAll()
        {
            return GetAllProjectsWithIncludes()
                .ToList();
        }

        public override Project? GetById(int id)
        {
            return GetAllProjectsWithIncludes()
                    .SingleOrDefault(x => x.Id == id);
        }

        private IEnumerable<Project> GetAllProjectsWithIncludes()
        {
            return Context.Projects
                .Include(r => r.Features).ThenInclude(f => f.RemainingWork)
                .Include(f => f.Features).ThenInclude(f => f.Forecast).ThenInclude(f => f.SimulationResults)
                .Include(f => f.InvolvedTeams)
                    .ThenInclude(t => t.Team.WorkTrackingSystemOptions)
                .Include(p => p.Milestones);
        }
    }
}
