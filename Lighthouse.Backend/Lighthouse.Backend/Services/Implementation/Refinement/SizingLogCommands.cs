using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Refinement;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.Refinement
{
    public sealed class SizingLogCommands(
        IRepository<Team> teamRepository,
        RefinementList refinementList,
        SleYardstickResolver yardstickResolver,
        ISizingLogRepository sizingLog,
        ILighthouseClock clock) : ISizingLogCommands
    {
        public VoteOutcome Vote(int teamId, string workItemReference, SizingVote vote, Voter voter)
        {
            var team = teamRepository.GetById(teamId);
            if (team is null)
            {
                return VoteOutcome.TeamNotFound;
            }

            if (!refinementList.For(team).Any(item => string.Equals(item.ReferenceId, workItemReference, StringComparison.Ordinal)))
            {
                return VoteOutcome.WorkItemNotInRefinement;
            }

            // The yardstick is kept with the vote: the SLE may change later, and a vote only means
            // something against the number it was cast against.
            var yardstick = yardstickResolver.For(team);

            sizingLog.Append(new SizingLogEntry
            {
                TeamId = team.Id,
                WorkItemReferenceId = workItemReference,
                Kind = SizingEntryKind.Vote,
                Answer = vote.Answer,
                Comment = vote.Comment,
                VoterKey = voter.Key,
                VoterProfileId = voter.ProfileId,
                VoterDisplayName = voter.DisplayName,
                RecordedAt = clock.Now.UtcDateTime,
                Channel = vote.Channel,
                YardstickDays = yardstick.Days,
                YardstickSource = yardstick.Source,
                YardstickProbability = yardstick.Probability,
            });

            return VoteOutcome.Recorded;
        }
    }
}
