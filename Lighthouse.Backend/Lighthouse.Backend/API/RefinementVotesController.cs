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
        private const string WorkItemNotInRefinementCode = "work-item-not-in-refinement";

        private const string VoterNameRequiredCode = "voter-name-required";

        private const string VoterKeyRequiredCode = "voter-key-required";

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

            var outcome = sizingLogCommands.Vote(teamId, workItemId, new SizingVote(answer, channel, vote.Comment), voter);

            return outcome switch
            {
                VoteOutcome.Recorded => RowAsItNowStands(teamId, workItemId, voterKey),
                VoteOutcome.TeamNotFound => NotFound(),
                VoteOutcome.WorkItemNotInRefinement => Refused(StatusCodes.Status409Conflict, "That Work Item is not in refinement.", WorkItemNotInRefinementCode),
                _ => throw new System.Diagnostics.UnreachableException($"No such vote outcome: {outcome}"),
            };
        }

        // A refused write is routine (a browser that lost its key, a blank name), so the line says why, for which
        // Team and from where, and never who: the declared name and the key are exactly what it must not carry.
        private ObjectResult RefusedWithoutAVoter(VoterRefusal? refusal, int teamId, SizingChannel channel)
        {
            var (level, reason) = refusal switch
            {
                VoterRefusal.NameRequired => (LogLevel.Information, VoterNameRequiredCode),
                VoterRefusal.KeyRequired => (LogLevel.Information, VoterKeyRequiredCode),
                VoterRefusal.NameTooLong => (LogLevel.Information, "voter-name-too-long"),
                VoterRefusal.NeedsAPerson => (LogLevel.Warning, "vote-needs-a-person"),
                _ => throw new System.Diagnostics.UnreachableException($"No such voter refusal: {refusal}"),
            };
            logger.Log(level, "Sizing entry refused ({Reason}) for Team {TeamId} from {Channel}", reason, teamId, channel);

            return refusal switch
            {
                VoterRefusal.NameRequired => Refused(StatusCodes.Status400BadRequest, "A vote needs the voter's name.", VoterNameRequiredCode),
                VoterRefusal.KeyRequired => Refused(StatusCodes.Status400BadRequest, "A vote needs the key the voter's browser keeps.", VoterKeyRequiredCode),
                VoterRefusal.NameTooLong => Problem(statusCode: StatusCodes.Status400BadRequest, title: $"A voter's name is at most {VoterIdentityResolver.LongestVoterName} characters."),
                _ => Problem(statusCode: StatusCodes.Status400BadRequest, title: "A vote needs a person to cast it."),
            };
        }

        private ObjectResult Refused(int statusCode, string title, string code)
        {
            var problem = ProblemDetailsFactory.CreateProblemDetails(HttpContext, statusCode: statusCode, title: title);
            problem.Extensions["code"] = code;
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
