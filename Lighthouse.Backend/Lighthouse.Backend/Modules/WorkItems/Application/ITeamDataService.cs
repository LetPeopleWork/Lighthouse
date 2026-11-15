using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Modules.WorkItems.Application
{
    public interface ITeamDataService
    {
        Task<SyncOutcome> UpdateTeamData(Team team);
    }
}
