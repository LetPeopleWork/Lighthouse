using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.Repositories
{
    public interface IWorkItemRepository : IRepository<WorkItem>
    {
        void RemoveWorkItemsForTeam(int teamId);
    }
}
