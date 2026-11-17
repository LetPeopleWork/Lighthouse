using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Boards;

namespace Lighthouse.Backend.Modules.TrackerIntegration.Application
{
    public interface IBoardInformationProvider
    {
        Task<IEnumerable<Board>> GetBoards(WorkTrackingSystemConnection workTrackingSystemConnection);
        
        Task<BoardInformation> GetBoardInformation(WorkTrackingSystemConnection workTrackingSystemConnection, string boardId);
    }
}