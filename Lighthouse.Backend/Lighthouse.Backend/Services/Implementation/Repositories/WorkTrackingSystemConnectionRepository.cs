using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class WorkTrackingSystemConnectionRepository : RepositoryBase<WorkTrackingSystemConnection>
    {
        public WorkTrackingSystemConnectionRepository(LighthouseAppContext context, ILogger<WorkTrackingSystemConnectionRepository> logger) : base(context, (context) => context.WorkTrackingSystemConnections, logger)
        {
        }

        public override IEnumerable<WorkTrackingSystemConnection> GetAll()
        {
            return Context.WorkTrackingSystemConnections
                .Include(c => c.Options);
        }

        public override WorkTrackingSystemConnection? GetById(int id)
        {
            return GetAll().SingleOrDefault(t => t.Id == id);
        }
    }
}
