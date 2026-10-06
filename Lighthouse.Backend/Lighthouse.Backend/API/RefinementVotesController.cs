using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Implementation.Refinement;
using Lighthouse.Backend.Services.Interfaces.Auth;
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
        ICurrentUserProfileService currentUserProfileService,
        ILogger<RefinementVotesController> logger) : ControllerBase
    {
        [HttpPost("votes")]
        public async Task<ActionResult<RefinementRowDto>> CastVote(
            int teamId,
            string workItemId,
            [FromBody] SizingVoteDto vote,
            [FromHeader(Name = RefinementController.VoterKeyHeader)] string? voterKey,
            CancellationToken cancellationToken)
        {
            if (vote.Answer is not { } answer || vote.Channel is not { } channel)
            {
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "A vote needs an answer and the channel it was cast from.");
            }

            var resolution = await VoterOf(vote.VoterName, voterKey, cancellationToken);
            if (resolution.Voter is not { } voter)
            {
                return RefusedWithoutAVoter(resolution.Refusal, teamId, channel);
            }

            var workItemReference = WorkItemRouteReference.From(workItemId);
            var outcome = sizingLogCommands.Vote(teamId, workItemReference, new SizingVote(answer, channel, vote.Comment), voter);

            return Answered(outcome, teamId, workItemReference, voterKey);
        }

        [HttpPost("comments")]
        public async Task<ActionResult<RefinementRowDto>> AddComment(
            int teamId,
            string workItemId,
            [FromBody] SizingCommentDto comment,
            [FromHeader(Name = RefinementController.VoterKeyHeader)] string? voterKey,
            CancellationToken cancellationToken)
        {
            if (comment.Comment is not { } text || comment.Channel is not { } channel)
            {
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "A comment needs its text and the channel it was sent from.");
            }

            var resolution = await VoterOf(comment.VoterName, voterKey, cancellationToken);
            if (resolution.Voter is not { } voter)
            {
                return RefusedWithoutAVoter(resolution.Refusal, teamId, channel);
            }

            var workItemReference = WorkItemRouteReference.From(workItemId);
            var outcome = sizingLogCommands.Comment(teamId, workItemReference, new SizingComment(text, channel), voter);

            return Answered(outcome, teamId, workItemReference, voterKey);
        }

        /// <summary>
        /// Taking back needs only the voter's key, never a name: the take-back is recorded under the name the vote
        /// was cast with. Only the web page takes a vote back so far, and the request has no body to name a channel.
        /// </summary>
        [HttpDelete("votes/mine")]
        public ActionResult<RefinementRowDto> TakeBackVote(
            int teamId,
            string workItemId,
            [FromHeader(Name = RefinementController.VoterKeyHeader)] string? voterKey)
        {
            const SizingChannel channel = SizingChannel.Web;
            if (voterIdentityResolver.ReaderKeyFrom(voterKey) is not { } key)
            {
                var refusal = voterIdentityResolver.Kind == VoterIdentityKind.Account ? VoterRefusal.NeedsAPerson : VoterRefusal.KeyRequired;
                return RefusedWithoutAVoter(refusal, teamId, channel);
            }

            var workItemReference = WorkItemRouteReference.From(workItemId);
            var outcome = sizingLogCommands.TakeBack(teamId, workItemReference, channel, key);

            return Answered(outcome, teamId, workItemReference, voterKey);
        }

        private Task<VoterResolution> VoterOf(string? declaredName, string? voterKey, CancellationToken cancellationToken)
            => voterIdentityResolver.ForWriteAsync(
                declaredName,
                voterKey,
                () => currentUserProfileService.GetOrCreateFromPrincipalAsync(User, cancellationToken));

        private ActionResult<RefinementRowDto> Answered(SizingOutcome outcome, int teamId, string workItemReference, string? voterKey)
            => outcome switch
            {
                SizingOutcome.Recorded => RowAsItNowStands(teamId, workItemReference, voterKey, madeReady: false),
                SizingOutcome.RecordedAndMadeReady => RowAsItNowStands(teamId, workItemReference, voterKey, madeReady: true),
                SizingOutcome.NothingTakenBack => RowAsItNowStands(teamId, workItemReference, voterKey, madeReady: false),
                SizingOutcome.TeamNotFound => NotFound(),
                SizingOutcome.WorkItemNotInRefinement => Refused(StatusCodes.Status409Conflict, "That Work Item is not in refinement.", SizingRefusal.WorkItemNotInRefinement),
                SizingOutcome.CommentMissing => Refused(StatusCodes.Status400BadRequest, "A comment needs some text.", SizingRefusal.CommentRequired),
                SizingOutcome.CommentTooLong => Refused(StatusCodes.Status400BadRequest, $"A comment is at most {SizingLogEntry.LongestComment} characters.", SizingRefusal.CommentTooLong),
                _ => throw new System.Diagnostics.UnreachableException($"No such sizing outcome: {outcome}"),
            };

        // A missing name or key is routine (a browser that lost its key, a blank name) and the caller can put it
        // right, so the answer names it. A credential no person stands behind is for an administrator to fix.
        private ObjectResult RefusedWithoutAVoter(VoterRefusal? refusal, int teamId, SizingChannel channel)
        {
            var (level, reason, title, namesTheReason) = refusal switch
            {
                VoterRefusal.NameRequired => (LogLevel.Information, SizingRefusal.VoterNameRequired, "A vote or comment needs the name of whoever sends it.", true),
                VoterRefusal.KeyRequired => (LogLevel.Information, SizingRefusal.VoterKeyRequired, "A vote or comment needs the key the sender's browser keeps.", true),
                VoterRefusal.NameTooLong => (LogLevel.Information, SizingRefusal.VoterNameTooLong, $"A name is at most {VoterIdentityResolver.LongestVoterName} characters.", false),
                VoterRefusal.NeedsAPerson => (LogLevel.Warning, SizingRefusal.VoteNeedsAPerson, "A vote or comment needs a person to send it.", false),
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

        private ActionResult<RefinementRowDto> RowAsItNowStands(int teamId, string workItemId, string? voterKey, bool madeReady)
        {
            var row = refinementViewQuery.ForTeam(teamId, voterKey)?.WorkItems
                .FirstOrDefault(candidate => string.Equals(candidate.WorkItem.ReferenceId, workItemId, StringComparison.Ordinal));

            return row is null ? NotFound() : Ok(new VotedRowDto(row, madeReady));
        }
    }
}
