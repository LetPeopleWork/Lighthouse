using System.Linq.Expressions;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class WorkItemRepository(LighthouseAppContext context, ILogger<WorkItemRepository> logger)
        : RepositoryBase<WorkItem>(context, (lighthouseAppContext) => lighthouseAppContext.WorkItems, logger), IWorkItemRepository
    {
        public override IEnumerable<WorkItem> GetAll()
        {
            return Context.WorkItems
                .Include(wi => wi.Team);
        }       
        
        public override IQueryable<WorkItem> GetAllByPredicate(Expression<Func<WorkItem, bool>> predicate)
        {
            var features = Context.WorkItems
                .Include(wi => wi.Team)
                    .Where(predicate);

            return features.AsQueryable();
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
