using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.WorkItems
{
    public interface IWorkItemService
    {
        Task UpdateFeaturesForProject(Project project);

        Task UpdateWorkItemsForTeam(Team team);
    }
}
