using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.TeamData
{
    public interface ITeamDataService
    {
        Task UpdateTeamData(Team team);
    }
}
