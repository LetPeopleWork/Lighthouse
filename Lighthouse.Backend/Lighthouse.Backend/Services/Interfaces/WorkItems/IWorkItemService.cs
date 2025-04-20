using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.WorkItems
{
    public interface IWorkItemService
    {
        Task UpdateWorkItemsForProject(Project project);
    }
}
