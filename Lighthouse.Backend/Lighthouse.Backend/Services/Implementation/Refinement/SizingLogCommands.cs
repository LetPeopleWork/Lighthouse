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
            => Append(
                teamId,
                workItemReference,
                new Said(SizingEntryKind.Vote, vote.Answer, vote.Comment, vote.Channel),
                voter,
                (team, entry) => MadeReady(team, entry) ? VoteOutcome.RecordedAndMadeReady : VoteOutcome.Recorded);

        public VoteOutcome Comment(int teamId, string workItemReference, SizingComment comment, Voter voter)
            => Append(
                teamId,
                workItemReference,
                new Said(SizingEntryKind.Comment, null, comment.Comment, comment.Channel),
                voter,
                (_, _) => VoteOutcome.Recorded);

        private VoteOutcome Append(int teamId, string workItemReference, Said said, Voter voter, Func<Team, SizingLogEntry, VoteOutcome> recorded)
        {
            var team = teamRepository.GetById(teamId);
            if (team is null)
            {
                return VoteOutcome.TeamNotFound;
            }

            if (!refinementList.For(team).Any(item => string.Equals(item.ReferenceId, workItemReference, StringComparison.Ordinal)))
            {
                // A Work Item leaving refinement between reading the tab and writing to it is routine, not a fault.
                SizingRefusal.Log(logger, LogLevel.Information, SizingRefusal.WorkItemNotInRefinement, team.Id, said.Channel);
                return VoteOutcome.WorkItemNotInRefinement;
            }

            // The yardstick is kept with every entry: the SLE may change later, and a vote only means
            // something against the number it was cast against.
            var yardstick = yardstickResolver.For(team);

            var entry = new SizingLogEntry
            {
                TeamId = team.Id,
                WorkItemReferenceId = workItemReference,
                Kind = said.Kind,
                Answer = said.Answer,
                Comment = string.IsNullOrWhiteSpace(said.Comment) ? null : said.Comment,
                VoterKey = voter.Key,
                VoterProfileId = voter.ProfileId,
                VoterDisplayName = voter.DisplayName,
                RecordedAt = clock.Now.UtcDateTime,
                Channel = said.Channel,
                YardstickDays = yardstick.Days,
                YardstickSource = yardstick.Source,
                YardstickProbability = yardstick.Probability,
            };
            sizingLog.Append(entry);

            return recorded(team, entry);
        }

        private bool MadeReady(Team team, SizingLogEntry entry)
        {
            var log = sizingLog.ReadForTeam(team.Id, [entry.WorkItemReferenceId]).ToList();
            return RefinementResolution.MadeReady(log, entry, team.RefinementSettings?.Readiness ?? new ReadinessSetting());
        }

        private sealed record Said(SizingEntryKind Kind, SizingAnswer? Answer, string? Comment, SizingChannel Channel);
    }
}
