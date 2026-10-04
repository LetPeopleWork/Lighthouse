using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Implementation.Refinement;
using Lighthouse.Backend.Services.Interfaces.Refinement;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    /// <summary>
    /// What a reader adds to the Refinement tab. Anyone who may read the Team may vote, so these writes are
    /// guarded by their own named requirement rather than by Team write, which would shut readers out.
    /// </summary>
    [Route("api/v1/teams/{teamId:int}/refinement/work-items/{workItemId}")]
    [Route("api/latest/teams/{teamId:int}/refinement/work-items/{workItemId}")]
    [ApiController]
    [RbacGuard(RbacGuardRequirement.TeamContribute, ScopeIdRouteKey = "teamId")]
    public class RefinementVotesController(
        ISizingLogCommands sizingLogCommands,
        IRefinementViewQuery refinementViewQuery,
        VoterIdentityResolver voterIdentityResolver) : ControllerBase
    {
        [HttpPost("votes")]
        public ActionResult<RefinementRowDto> CastVote(
            int teamId,
            string workItemId,
            [FromBody] SizingVoteDto vote,
            [FromHeader(Name = RefinementController.VoterKeyHeader)] string? voterKey)
        {
            if (vote.Answer is not { } answer || vote.Channel is not { } channel)
            {
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "A vote needs an answer and the channel it was cast from.");
            }

            var voter = voterIdentityResolver.ForWrite(vote.VoterName, voterKey);
            if (voter is null)
            {
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "A vote needs the voter's name and the key their browser keeps.");
            }

            var outcome = sizingLogCommands.Vote(teamId, workItemId, new SizingVote(answer, channel, vote.Comment), voter);

            return outcome switch
            {
                VoteOutcome.Recorded => RowAsItNowStands(teamId, workItemId, voterKey),
                VoteOutcome.TeamNotFound => NotFound(),
                VoteOutcome.WorkItemNotInRefinement => Problem(statusCode: StatusCodes.Status409Conflict, title: "That Work Item is not in refinement."),
                _ => throw new System.Diagnostics.UnreachableException($"No such vote outcome: {outcome}"),
            };
        }

        private ActionResult<RefinementRowDto> RowAsItNowStands(int teamId, string workItemId, string? voterKey)
        {
            var row = refinementViewQuery.ForTeam(teamId, voterKey)?.WorkItems
                .FirstOrDefault(candidate => string.Equals(candidate.WorkItem.ReferenceId, workItemId, StringComparison.Ordinal));

            return row is null ? NotFound() : Ok(new RefinementRowDto(row));
        }
    }
}
