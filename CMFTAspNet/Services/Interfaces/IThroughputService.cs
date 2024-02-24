using CMFTAspNet.Models;

namespace CMFTAspNet.Services.Interfaces
{
    public interface IThroughputService
    {
        Task UpdateThroughput(Team team);
    }
}
