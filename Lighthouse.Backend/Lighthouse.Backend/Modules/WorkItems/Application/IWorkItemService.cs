using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Modules.WorkItems.Application
{
    public interface IWorkItemService
    {
        Task<SyncOutcome> UpdateFeaturesForPortfolio(Portfolio portfolio);

        Task<SyncOutcome> UpdateWorkItemsForTeam(Team team);
    }
}
