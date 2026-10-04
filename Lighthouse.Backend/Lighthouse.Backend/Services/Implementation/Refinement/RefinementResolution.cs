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

        /// <summary>
        /// A "Yes, if…" counts as a Yes; a No does not, though every answer counts as a voter. A discussion
        /// rule that is met wins over everything else, because the doubt is what the meeting is for. Missing Yes
        /// votes are named before missing voters, because more Yes votes also bring more voters.
        /// </summary>
        public static RowStanding StandingOf(VoteSplit split, ReadinessSetting readiness)
        {
            if (NeedsDiscussion(split, readiness.DiscussWhen))
            {
                return new RowStanding(RowReadiness.NeedsDiscussion, null);
            }

            var yesVotes = split.Yes + split.YesBut;
            var missingYes = readiness.MinYes - yesVotes;
            if (missingYes > 0)
            {
                return new RowStanding(RowReadiness.MoreYesNeeded, missingYes);
            }

            var missingVoters = readiness.MinVoters - (yesVotes + split.No);
            return missingVoters > 0 ? new RowStanding(RowReadiness.MoreVotersNeeded, missingVoters) : RowStanding.Ready;
        }

        // A rule that is off has no threshold, and a comparison with a missing threshold is never true.
        private static bool NeedsDiscussion(VoteSplit split, DiscussionRules rules)
            => split.No >= rules.No || split.YesBut >= rules.YesIf;

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
