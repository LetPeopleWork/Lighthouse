using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Interfaces.Refinement;

namespace Lighthouse.Backend.Services.Implementation.Refinement
{
    /// <summary>
    /// Resolves what a Work Item's sizing log currently says, from its entries alone: each voter counts once,
    /// with whatever they said last, and a vote taken back leaves them uncounted.
    /// </summary>
    public static class RefinementResolution
    {
        public static RowVotes VotesOn(IEnumerable<SizingLogEntry> entries, string? readerKey)
        {
            var current = CurrentVotes(entries);
            var myVote = current.FirstOrDefault(vote => readerKey is not null && vote.VoterKey == readerKey)?.Answer;

            return new RowVotes(current.Count, myVote, SplitOf(current));
        }

        /// <summary>A "Yes, if…" counts as a Yes; a No does not.</summary>
        public static RowStanding StandingOf(VoteSplit split, ReadinessSetting readiness)
        {
            var missingYes = readiness.MinYes - (split.Yes + split.YesBut);

            return missingYes > 0 ? new RowStanding(RowReadiness.MoreYesNeeded, missingYes) : RowStanding.Ready;
        }

        private static List<SizingLogEntry> CurrentVotes(IEnumerable<SizingLogEntry> entries)
            => [.. entries
                .Where(entry => entry.Kind != SizingEntryKind.Comment)
                .GroupBy(entry => entry.VoterKey, StringComparer.Ordinal)
                .Select(byVoter => byVoter.MaxBy(entry => entry.Id)!)
                .Where(latest => latest.Kind == SizingEntryKind.Vote)];

        private static VoteSplit SplitOf(List<SizingLogEntry> currentVotes)
            => new(
                currentVotes.Count(vote => vote.Answer == SizingAnswer.Yes),
                currentVotes.Count(vote => vote.Answer == SizingAnswer.YesBut),
                currentVotes.Count(vote => vote.Answer == SizingAnswer.No));
    }
}
