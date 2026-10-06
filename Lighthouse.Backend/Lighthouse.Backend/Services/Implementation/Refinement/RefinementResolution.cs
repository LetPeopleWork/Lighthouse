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

            return new RowVotes(current.Count, VoteBy(current, readerKey)?.Answer, SplitOf(current));
        }

        public static SizingLogEntry? CurrentVoteOf(IEnumerable<SizingLogEntry> entries, string voterKey)
            => VoteBy(CurrentVotes(entries), voterKey);

        /// <summary>Each voter's name under the answer of their current vote, as that vote gave it.</summary>
        public static VoterNames VotersOn(IEnumerable<SizingLogEntry> entries) => VotersOf(CurrentVotes(entries));

        /// <summary>A row has an open question exactly when one of its entries is one.</summary>
        public static RowConversation ConversationOn(IEnumerable<SizingLogEntry> entries)
        {
            var log = entries.ToList();

            return new RowConversation(log.Exists(SaysSomething), OpenQuestionsIn(log).Count > 0);
        }

        /// <summary>
        /// The entries that are open questions, by their id. A comment from somebody without a vote is a
        /// question, and it stays open until they vote: an answer from anybody else does not tell whether the
        /// asker is satisfied. Only somebody's latest entry can be their open question, so a vote taken back
        /// does not reopen what they asked before it. Voters are told apart by their key, never by the name
        /// they gave, which two people may share and one person may change.
        /// </summary>
        public static IReadOnlySet<int> OpenQuestionsIn(IEnumerable<SizingLogEntry> entries)
        {
            var log = entries.ToList();
            var voterKeys = CurrentVotes(log).Select(vote => vote.VoterKey).ToHashSet(StringComparer.Ordinal);

            return log
                .GroupBy(entry => entry.VoterKey, StringComparer.Ordinal)
                .Select(byVoter => byVoter.MaxBy(entry => entry.Id)!)
                .Where(latest => SaysSomething(latest) && !voterKeys.Contains(latest.VoterKey))
                .Select(latest => latest.Id)
                .ToHashSet();
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

        /// <summary>
        /// Whether this entry moved the Work Item to Ready, judged on the log as it stood just before the entry
        /// and just after it. Each step to Ready therefore belongs to exactly one entry, however many readers
        /// were looking at an older picture of the row when they voted.
        /// </summary>
        public static bool MadeReady(IReadOnlyCollection<SizingLogEntry> log, SizingLogEntry entry, ReadinessSetting readiness)
            => !IsReady(log.Where(earlier => earlier.Id < entry.Id), readiness)
                && IsReady(log.Where(upTo => upTo.Id <= entry.Id), readiness);

        /// <summary>
        /// A Work Item both stage rules match is Ready, because being ready is the further step; one neither
        /// rule matches is Waiting.
        /// </summary>
        public static RefinementStage StageOf(bool matchesReady, bool matchesBeingRefined)
            => (matchesReady, matchesBeingRefined) switch
            {
                (true, _) => RefinementStage.Ready,
                (false, true) => RefinementStage.BeingRefined,
                (false, false) => RefinementStage.Waiting,
            };

        /// <summary>
        /// Whether the stage and the votes cast tell a different story about the Work Item being Ready. Without
        /// a stage, or before anybody has voted, there is only one signal and so nothing to disagree with.
        /// </summary>
        public static bool SignalsDisagree(RefinementStage? stage, RowVotes votes, RowStanding standing)
            => stage is not null
                && votes.VoteCount > 0
                && (stage == RefinementStage.Ready) != (standing.Readiness == RowReadiness.Ready);

        /// <summary>
        /// A Team with stage rules trusts its tracker for what is Ready, whatever the votes say; a Team without
        /// them trusts the votes. The two signals are never added together.
        /// </summary>
        public static int ReadyCountOf(bool stagesConfigured, IEnumerable<RefinementRow> rows)
            => stagesConfigured
                ? rows.Count(row => row.Stage == RefinementStage.Ready)
                : ReadyByVotesCountOf(rows);

        public static int ReadyByVotesCountOf(IEnumerable<RefinementRow> rows)
            => rows.Count(row => row.Standing.Readiness == RowReadiness.Ready);

        public static ReadySource ReadySourceOf(bool stagesConfigured)
            => stagesConfigured ? ReadySource.Stages : ReadySource.Votes;

        private static bool IsReady(IEnumerable<SizingLogEntry> entries, ReadinessSetting readiness)
            => StandingOf(SplitOf(CurrentVotes(entries)), readiness).Readiness == RowReadiness.Ready;

        // A rule that is off has no threshold, and a comparison with a missing threshold is never true.
        private static bool NeedsDiscussion(VoteSplit split, DiscussionRules rules)
            => split.No >= rules.No || split.YesBut >= rules.YesIf;

        private static bool SaysSomething(SizingLogEntry entry)
            => entry.Kind == SizingEntryKind.Comment || entry.Comment is not null;

        private static List<SizingLogEntry> CurrentVotes(IEnumerable<SizingLogEntry> entries)
            => [.. entries
                .Where(entry => entry.Kind != SizingEntryKind.Comment)
                .GroupBy(entry => entry.VoterKey, StringComparer.Ordinal)
                .Select(byVoter => byVoter.MaxBy(entry => entry.Id)!)
                .Where(latest => latest.Kind == SizingEntryKind.Vote)];

        private static SizingLogEntry? VoteBy(List<SizingLogEntry> currentVotes, string? voterKey)
            => voterKey is null ? null : currentVotes.Find(vote => string.Equals(vote.VoterKey, voterKey, StringComparison.Ordinal));

        // The split counts the same names a reader sees under each answer, so the two can never disagree.
        private static VoteSplit SplitOf(List<SizingLogEntry> currentVotes) => VotersOf(currentVotes).Split;

        private static VoterNames VotersOf(List<SizingLogEntry> currentVotes)
        {
            var oldestFirst = currentVotes.OrderBy(vote => vote.Id).ToList();
            List<string> NamesSaying(SizingAnswer answer)
                => [.. oldestFirst.Where(vote => vote.Answer == answer).Select(vote => vote.VoterDisplayName)];

            return new VoterNames(NamesSaying(SizingAnswer.Yes), NamesSaying(SizingAnswer.YesBut), NamesSaying(SizingAnswer.No));
        }
    }
}
