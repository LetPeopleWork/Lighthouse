using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.WorkItems
{
    public interface IWorkItemService
    {
        Task UpdateFeaturesForProject(Portfolio project);

        Task UpdateWorkItemsForTeam(Team team);
    }
}
