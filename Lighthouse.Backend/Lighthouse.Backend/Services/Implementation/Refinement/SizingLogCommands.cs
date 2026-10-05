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
        public SizingOutcome Vote(int teamId, string workItemReference, SizingVote vote, Voter voter)
            => Append(
                teamId,
                workItemReference,
                new Said(SizingEntryKind.Vote, vote.Answer, vote.Comment, vote.Channel),
                voter,
                (team, entry) => MadeReady(team, entry) ? SizingOutcome.RecordedAndMadeReady : SizingOutcome.Recorded);

        public SizingOutcome Comment(int teamId, string workItemReference, SizingComment comment, Voter voter)
            => Append(
                teamId,
                workItemReference,
                new Said(SizingEntryKind.Comment, null, comment.Comment, comment.Channel),
                voter,
                (_, _) => SizingOutcome.Recorded);

        private SizingOutcome Append(int teamId, string workItemReference, Said said, Voter voter, Func<Team, SizingLogEntry, SizingOutcome> recorded)
        {
            var team = teamRepository.GetById(teamId);
            if (team is null)
            {
                return SizingOutcome.TeamNotFound;
            }

            if (WhatIsWrongWith(said) is (var refusedAs, var reason))
            {
                SizingRefusal.Log(logger, LogLevel.Information, reason, team.Id, said.Channel);
                return refusedAs;
            }

            if (!refinementList.For(team).Any(item => string.Equals(item.ReferenceId, workItemReference, StringComparison.Ordinal)))
            {
                // A Work Item leaving refinement between reading the tab and writing to it is routine, not a fault.
                SizingRefusal.Log(logger, LogLevel.Information, SizingRefusal.WorkItemNotInRefinement, team.Id, said.Channel);
                return SizingOutcome.WorkItemNotInRefinement;
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

        // A vote stands on its answer, so its comment may be left blank; a comment on its own is only its text.
        private static (SizingOutcome RefusedAs, string Reason)? WhatIsWrongWith(Said said)
        {
            if (said.Comment?.Length > SizingLogEntry.LongestComment)
            {
                return (SizingOutcome.CommentTooLong, SizingRefusal.CommentTooLong);
            }

            if (said.Kind == SizingEntryKind.Comment && string.IsNullOrWhiteSpace(said.Comment))
            {
                return (SizingOutcome.CommentMissing, SizingRefusal.CommentRequired);
            }

            return null;
        }

        private bool MadeReady(Team team, SizingLogEntry entry)
        {
            var log = sizingLog.ReadForTeam(team.Id, [entry.WorkItemReferenceId]).ToList();
            return RefinementResolution.MadeReady(log, entry, team.RefinementSettings?.Readiness ?? new ReadinessSetting());
        }

        private sealed record Said(SizingEntryKind Kind, SizingAnswer? Answer, string? Comment, SizingChannel Channel);
    }
}
