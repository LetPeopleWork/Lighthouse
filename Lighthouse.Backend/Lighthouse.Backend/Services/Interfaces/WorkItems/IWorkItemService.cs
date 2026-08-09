using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.WorkItems
{
    public interface IWorkItemService
    {
        Task<SyncOutcome> UpdateFeaturesForPortfolio(Portfolio portfolio);

        Task<SyncOutcome> UpdateWorkItemsForTeam(Team team);
    }
}
