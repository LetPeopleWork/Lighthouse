using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Boards;

namespace Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors
{
    public interface IJiraWorkTrackingConnector : IWorkTrackingConnector
    {
        Task<IEnumerable<Board>> GetBoards(WorkTrackingSystemConnection workTrackingSystemConnection);
        
        Task<BoardInformation> GetBoardInformation(WorkTrackingSystemConnection workTrackingSystemConnection, int boardId);
    }
}