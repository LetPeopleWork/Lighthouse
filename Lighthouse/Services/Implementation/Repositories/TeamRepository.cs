using Lighthouse.Data;
using Lighthouse.Models;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Services.Implementation.Repositories
{
    public class TeamRepository : RepositoryBase<Team>
    {
        public TeamRepository(LighthouseAppContext context) : base(context, (context) => context.Teams)
        {
        }

        public override IEnumerable<Team> GetAll()
        {
            return Context.Teams
                .Include(x => x.WorkTrackingSystemOptions);
        }

        public override Team? GetById(int id)
        {
            return Context.Teams.Include(x => x.WorkTrackingSystemOptions).SingleOrDefault(t => t.Id == id);
        }
    }
}
