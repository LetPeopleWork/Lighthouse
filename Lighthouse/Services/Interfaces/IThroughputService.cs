using Lighthouse.Models;

namespace Lighthouse.Services.Interfaces
{
    public interface IThroughputService
    {
        Task UpdateThroughput(Team team);
    }
}
