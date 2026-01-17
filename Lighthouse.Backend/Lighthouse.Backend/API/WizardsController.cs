using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
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
        IAzureDevOpsWorkTrackingConnector azureDevOpsWorkTrackingConnector,
        IRepository<WorkTrackingSystemConnection> workTrackingSystemRepo)
    : ControllerBase
    {
        [HttpGet("{workTrackingSystemConnectionId:int}/boards")]
        public async Task<ActionResult<IEnumerable<Board>>> GetBoards(int workTrackingSystemConnectionId)
        {
            return await this.GetEntityByIdAnExecuteAction(workTrackingSystemRepo, workTrackingSystemConnectionId, async workTrackingSystemConnection =>
            {
                var boardInformationProvider =
                    GetBoardInformationProviderForWorkTrackingSystem(workTrackingSystemConnection);
                return await boardInformationProvider.GetBoards(workTrackingSystemConnection);
            });
        }

        [HttpGet("{workTrackingSystemConnectionId:int}/boards/{boardId:int}")]
        public async Task<ActionResult<BoardInformation>> GetBoardInformation(int workTrackingSystemConnectionId,
            int boardId)
        {
            return await this.GetEntityByIdAnExecuteAction(workTrackingSystemRepo, workTrackingSystemConnectionId,
                async workTrackingSystemConnection =>
                {
                    var boardInformationProvider =
                        GetBoardInformationProviderForWorkTrackingSystem(workTrackingSystemConnection);
                    return await boardInformationProvider.GetBoardInformation(workTrackingSystemConnection, boardId);
                });
        }

        private IBoardInformationProvider GetBoardInformationProviderForWorkTrackingSystem(
            WorkTrackingSystemConnection workTrackingSystemConnection)
        {
            return workTrackingSystemConnection.WorkTrackingSystem switch
            {
                WorkTrackingSystems.AzureDevOps => azureDevOpsWorkTrackingConnector,
                WorkTrackingSystems.Jira => jiraWorkTrackingConnector,
                _ => throw new NotImplementedException(
                    "Work Tracking System Type {Type} does not support Board Information!")
            };
        }
    }
}