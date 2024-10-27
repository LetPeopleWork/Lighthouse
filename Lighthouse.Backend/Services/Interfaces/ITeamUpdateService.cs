using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface ITeamUpdateService
    {
        Task UpdateTeam(Team team);
    }
}
