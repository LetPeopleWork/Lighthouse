using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Implementation.Refinement;
using Lighthouse.Backend.Services.Interfaces.Refinement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
    [EnableRateLimiting(RateLimitingConfiguration.RefinementContributionPolicy)]
    public class RefinementVotesController(
        ISizingLogCommands sizingLogCommands,
        IRefinementViewQuery refinementViewQuery,
        VoterIdentityResolver voterIdentityResolver,
        ILogger<RefinementVotesController> logger) : ControllerBase
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

            var resolution = voterIdentityResolver.ForWrite(vote.VoterName, voterKey);
            if (resolution.Voter is not { } voter)
            {
                return RefusedWithoutAVoter(resolution.Refusal, teamId, channel);
            }

            var workItemReference = WorkItemReferenceFrom(workItemId);
            var outcome = sizingLogCommands.Vote(teamId, workItemReference, new SizingVote(answer, channel, vote.Comment), voter);

            return outcome switch
            {
                VoteOutcome.Recorded => RowAsItNowStands(teamId, workItemReference, voterKey),
                VoteOutcome.TeamNotFound => NotFound(),
                VoteOutcome.WorkItemNotInRefinement => Refused(StatusCodes.Status409Conflict, "That Work Item is not in refinement.", SizingRefusal.WorkItemNotInRefinement),
                _ => throw new System.Diagnostics.UnreachableException($"No such vote outcome: {outcome}"),
            };
        }

        // The server has already unescaped every part of the path except an escaped slash, which it leaves
        // as it came so that it cannot pass for a path separator. A reference holding a slash therefore
        // arrives with "%2F" in it. Only that escape is restored: unescaping everything a second time would
        // turn a reference that merely contains "%25" into a different one.
        private static string WorkItemReferenceFrom(string routeValue)
            => routeValue.Replace("%2F", "/", StringComparison.OrdinalIgnoreCase);

        // A missing name or key is routine (a browser that lost its key, a blank name) and the caller can put it
        // right, so the answer names it. A credential no person stands behind is for an administrator to fix.
        private ObjectResult RefusedWithoutAVoter(VoterRefusal? refusal, int teamId, SizingChannel channel)
        {
            var (level, reason, title, namesTheReason) = refusal switch
            {
                VoterRefusal.NameRequired => (LogLevel.Information, SizingRefusal.VoterNameRequired, "A vote needs the voter's name.", true),
                VoterRefusal.KeyRequired => (LogLevel.Information, SizingRefusal.VoterKeyRequired, "A vote needs the key the voter's browser keeps.", true),
                VoterRefusal.NameTooLong => (LogLevel.Information, SizingRefusal.VoterNameTooLong, $"A voter's name is at most {VoterIdentityResolver.LongestVoterName} characters.", false),
                VoterRefusal.NeedsAPerson => (LogLevel.Warning, SizingRefusal.VoteNeedsAPerson, "A vote needs a person to cast it.", false),
                _ => throw new System.Diagnostics.UnreachableException($"No such voter refusal: {refusal}"),
            };
            SizingRefusal.Log(logger, level, reason, teamId, channel);

            return Refused(StatusCodes.Status400BadRequest, title, namesTheReason ? reason : null);
        }

        private ObjectResult Refused(int statusCode, string title, string? code)
        {
            var problem = ProblemDetailsFactory.CreateProblemDetails(HttpContext, statusCode: statusCode, title: title);
            if (code is not null)
            {
                problem.Extensions["code"] = code;
            }

            return new ObjectResult(problem) { StatusCode = statusCode };
        }

        private ActionResult<RefinementRowDto> RowAsItNowStands(int teamId, string workItemId, string? voterKey)
        {
            var row = refinementViewQuery.ForTeam(teamId, voterKey)?.WorkItems
                .FirstOrDefault(candidate => string.Equals(candidate.WorkItem.ReferenceId, workItemId, StringComparison.Ordinal));

            return row is null ? NotFound() : Ok(new RefinementRowDto(row));
        }
    }
}
