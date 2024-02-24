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
            return Context.Projects
                .Include(r => r.Features).ThenInclude(f => f.RemainingWork)
                .Include(f => f.Features).ThenInclude(f => f.Forecast).ThenInclude(f => f.SimulationResults)
                .Include(f => f.InvolvedTeams)
                .ToList();
        }

        public override Project? GetById(int id)
        {
            return Context.Projects
                    .Include(x => x.Features).ThenInclude(x => x.RemainingWork)
                    .Include(x => x.InvolvedTeams).ThenInclude(t => t.WorkTrackingSystemOptions)
                    .SingleOrDefault(x => x.Id == id);
        }
    }
}
