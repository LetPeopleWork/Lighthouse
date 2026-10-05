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
        ILighthouseClock clock,
        ILogger<SizingLogCommands> logger) : ISizingLogCommands
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
                // A Work Item leaving refinement between reading the tab and voting is routine, not a fault.
                SizingRefusal.Log(logger, LogLevel.Information, SizingRefusal.WorkItemNotInRefinement, team.Id, vote.Channel);
                return VoteOutcome.WorkItemNotInRefinement;
            }

            // The yardstick is kept with the vote: the SLE may change later, and a vote only means
            // something against the number it was cast against.
            var yardstick = yardstickResolver.For(team);

            var entry = new SizingLogEntry
            {
                TeamId = team.Id,
                WorkItemReferenceId = workItemReference,
                Kind = SizingEntryKind.Vote,
                Answer = vote.Answer,
                Comment = string.IsNullOrWhiteSpace(vote.Comment) ? null : vote.Comment,
                VoterKey = voter.Key,
                VoterProfileId = voter.ProfileId,
                VoterDisplayName = voter.DisplayName,
                RecordedAt = clock.Now.UtcDateTime,
                Channel = vote.Channel,
                YardstickDays = yardstick.Days,
                YardstickSource = yardstick.Source,
                YardstickProbability = yardstick.Probability,
            };
            sizingLog.Append(entry);

            return MadeReady(team, entry) ? VoteOutcome.RecordedAndMadeReady : VoteOutcome.Recorded;
        }

        private bool MadeReady(Team team, SizingLogEntry entry)
        {
            var log = sizingLog.ReadForTeam(team.Id, [entry.WorkItemReferenceId]).ToList();
            return RefinementResolution.MadeReady(log, entry, team.RefinementSettings?.Readiness ?? new ReadinessSetting());
        }
    }
}
