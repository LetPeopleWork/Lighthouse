using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface IWorkItemRepository : IRepository<WorkItem>
    {
        void RemoveWorkItemsForTeam(int teamId);
    }
}
