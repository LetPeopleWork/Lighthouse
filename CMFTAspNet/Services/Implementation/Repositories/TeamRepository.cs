using CMFTAspNet.Data;
using CMFTAspNet.Models;
using Microsoft.EntityFrameworkCore;

namespace CMFTAspNet.Services.Implementation.Repositories
{
    public class TeamRepository : RepositoryBase<Team>
    {
        public TeamRepository(CMFTAspNetContext context) : base(context, (context) => context.Teams)
        {
        }

        public override Team? GetById(int id)
        {
            return Context.Teams.Include(x => x.WorkTrackingSystemOptions).SingleOrDefault(t => t.Id == id);
        }
    }
}
