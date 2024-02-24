using CMFTAspNet.Data;
using CMFTAspNet.Models;
using Microsoft.EntityFrameworkCore;

namespace CMFTAspNet.Services.Implementation.Repositories
{
    public class ProjectRepository : RepositoryBase<Project>
    {
        public ProjectRepository(Data.AppContext context) : base(context, (context) => context.Projects)
        {
        }

        public override IEnumerable<Project> GetAll()
        {
            return Context.Projects
                .Include(r => r.Features)
                .ThenInclude(f => f.RemainingWork)
                .Include(f => f.Features)
                .ThenInclude(f => f.Forecast)
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
