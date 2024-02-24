using CMFTAspNet.Data;
using CMFTAspNet.Models;
using Microsoft.EntityFrameworkCore;

namespace CMFTAspNet.Services.Implementation.Repositories
{
    public class ProjectRepository : RepositoryBase<Project>
    {
        public ProjectRepository(CMFTAspNetContext context) : base(context, (context) => context.Projects)
        {
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
