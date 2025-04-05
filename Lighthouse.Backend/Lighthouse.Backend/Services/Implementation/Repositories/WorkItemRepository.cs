using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class WorkItemRepository : RepositoryBase<WorkItem>
    {
        public WorkItemRepository(LighthouseAppContext context, ILogger<WorkItemRepository> logger) : base(context, (context) => context.WorkItems, logger)
        {            
        }

        public override IEnumerable<WorkItem> GetAll()
        {
            return Context.WorkItems
                .Include(wi => wi.Team);
        }
    }
}
