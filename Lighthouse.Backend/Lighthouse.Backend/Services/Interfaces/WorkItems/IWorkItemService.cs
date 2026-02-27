using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.WorkItems
{
    public interface IWorkItemService
    {
        Task UpdateFeaturesForPortfolio(Portfolio portfolio);

        Task UpdateWorkItemsForTeam(Team team);
    }
}
