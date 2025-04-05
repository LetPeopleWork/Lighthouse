using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class WorkItemRepository : RepositoryBase<WorkItem>, IWorkItemRepository
    {
        public WorkItemRepository(LighthouseAppContext context, ILogger<WorkItemRepository> logger) : base(context, (context) => context.WorkItems, logger)
        {
        }

        public override IEnumerable<WorkItem> GetAll()
        {
            return Context.WorkItems
                .Include(wi => wi.Team);
        }

        public void RemoveWorkItemsForTeam(int teamId)
        {
            var workItems = Context.WorkItems
                .Where(wi => wi.TeamId == teamId)
                .ToList();

            foreach (var workItem in workItems)
            {
                Context.WorkItems.Remove(workItem);
            }
        }
    }
}
