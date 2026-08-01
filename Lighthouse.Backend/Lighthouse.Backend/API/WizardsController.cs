using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Boards;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/[controller]")]
    [Route("api/latest/[controller]")]
    [ApiController]
    [RbacGuard(RbacGuardRequirement.SystemAdmin)]
    public class WizardsController(
        IJiraWorkTrackingConnector jiraWorkTrackingConnector,
        IAzureDevOpsWorkTrackingConnector azureDevOpsWorkTrackingConnector,
        ILinearWorkTrackingConnector linearWorkTrackingConnector,
        IServiceNowWorkTrackingConnector serviceNowWorkTrackingConnector,
        IRepository<WorkTrackingSystemConnection> workTrackingSystemRepo)
    : ControllerBase
    {
        [HttpGet("{workTrackingSystemConnectionId:int}/boards")]
        public Task<ActionResult<IEnumerable<Board>>> GetBoards(int workTrackingSystemConnectionId)
        {
            return AnsweringARefusalWithItsReason<IEnumerable<Board>>(
                workTrackingSystemConnectionId,
                (boardInformationProvider, connection) => boardInformationProvider.GetBoards(connection));
        }

        [HttpGet("{workTrackingSystemConnectionId:int}/boards/{boardId}")]
        public Task<ActionResult<BoardInformation>> GetBoardInformation(int workTrackingSystemConnectionId,
            string boardId)
        {
            return AnsweringARefusalWithItsReason(
                workTrackingSystemConnectionId,
                (boardInformationProvider, connection) => boardInformationProvider.GetBoardInformation(connection, boardId));
        }

        // ADR-126 decision 1. A connector that refused a read already said why, so the reason travels
        // to the administrator instead of being replaced by "Failed to load boards. Please try again."
        // Catching the abstract type is what keeps the driving side from naming any one connector.
        private async Task<ActionResult<TResult>> AnsweringARefusalWithItsReason<TResult>(
            int workTrackingSystemConnectionId,
            Func<IBoardInformationProvider, WorkTrackingSystemConnection, Task<TResult>> read)
        {
            try
            {
                return await this.GetEntityByIdAnExecuteAction(workTrackingSystemRepo, workTrackingSystemConnectionId,
                    workTrackingSystemConnection => read(
                        GetBoardInformationProviderForWorkTrackingSystem(workTrackingSystemConnection),
                        workTrackingSystemConnection));
            }
            catch (WorkTrackingReadException refusal)
            {
                return BadRequest(refusal.Verdict);
            }
        }

        private IBoardInformationProvider GetBoardInformationProviderForWorkTrackingSystem(
            WorkTrackingSystemConnection workTrackingSystemConnection)
        {
            return workTrackingSystemConnection.WorkTrackingSystem switch
            {
                WorkTrackingSystems.AzureDevOps => azureDevOpsWorkTrackingConnector,
                WorkTrackingSystems.Jira => jiraWorkTrackingConnector,
                WorkTrackingSystems.Linear => linearWorkTrackingConnector,
                WorkTrackingSystems.ServiceNow => serviceNowWorkTrackingConnector,
                _ => throw new NotImplementedException(
                    "Work Tracking System Type {Type} does not support Board Information!")
            };
        }
    }
}