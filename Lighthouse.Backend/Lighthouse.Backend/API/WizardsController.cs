using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Boards;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class WizardsController(
        IJiraWorkTrackingConnector jiraWorkTrackingConnector,
        IRepository<WorkTrackingSystemConnection> workTrackingSystemRepo)
    : ControllerBase
    {
        [HttpGet("jira/{workTrackingSystemConnectionId:int}/boards")]
        public async Task<ActionResult<IEnumerable<Board>>> GetJiraBoards(int workTrackingSystemConnectionId)
        {
            return await this.GetEntityByIdAnExecuteAction(workTrackingSystemRepo, workTrackingSystemConnectionId, async workTrackingSystemConnection => await jiraWorkTrackingConnector.GetBoards(workTrackingSystemConnection));
        }

        [HttpGet("jira/{workTrackingSystemConnectionId:int}/boards/{boardId:int}")]
        public async Task<ActionResult<BoardInformation>> GetJiraBoardInformation(int workTrackingSystemConnectionId,
            int boardId)
        {
            return await this.GetEntityByIdAnExecuteAction(workTrackingSystemRepo, workTrackingSystemConnectionId,
                async workTrackingSystemConnection => await  jiraWorkTrackingConnector.GetBoardInformation(workTrackingSystemConnection, boardId));
        }
    }
}