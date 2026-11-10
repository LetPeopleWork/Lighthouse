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

        public IReadOnlyList<WorkItem> DoneItemsOfTeam(int teamId)
            => ItemsWithTeam(i => i.TeamId == teamId && i.StateCategory == StateCategories.Done).ToList();

        public IReadOnlyList<WorkItem> DoingOrDoneItemsOfTeam(int teamId)
            => ItemsWithTeam(i => i.TeamId == teamId && (i.StateCategory == StateCategories.Doing || i.StateCategory == StateCategories.Done)).ToList();

        public IReadOnlyList<WorkItem> ToDoOrDoingItemsOfTeam(int teamId)
            => ItemsWithTeam(i => i.TeamId == teamId && (i.StateCategory == StateCategories.ToDo || i.StateCategory == StateCategories.Doing)).ToList();

        public int CountToDoOrDoingItemsOfTeam(int teamId)
            => ItemsWithTeam(i => i.TeamId == teamId && (i.StateCategory == StateCategories.Doing || i.StateCategory == StateCategories.ToDo)).Count();

        public IReadOnlyList<WorkItem> ItemsOfTeam(int teamId)
            => ItemsWithTeam(item => item.TeamId == teamId).ToList();

        private IQueryable<WorkItem> ItemsWithTeam(Expression<Func<WorkItem, bool>> predicate)
            => Context.WorkItems
                .Include(wi => wi.Team)
                .Where(predicate);

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
