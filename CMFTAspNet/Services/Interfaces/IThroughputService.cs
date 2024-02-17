using CMFTAspNet.Models.Teams;

namespace CMFTAspNet.Services.Interfaces
{
    public interface IThroughputService
    {
        Task UpdateThroughput(int historyInDays, Team team);
    }
}
