using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Implementation.Refinement;
using Lighthouse.Backend.Services.Interfaces.Refinement;

namespace Lighthouse.Backend.Tests.Services.Implementation.Refinement
{
    [TestFixture]
    public class RefinementResolutionTest
    {
        private const string Jonas = "jonas-browser";

        private const string Ana = "ana-browser";

        private const string OtherAna = "other-ana-browser";

        private const string Priya = "priya-browser";

        private static readonly DiscussionRules NoDiscussion = new() { No = null, YesIf = null };

        [TestCaseSource(nameof(Logs))]
        public void Each_voter_counts_once_with_their_latest_answer(SizingLogEntry[] log, string? readerKey, RowVotes expected)
        {
            var votes = RefinementResolution.VotesOn(log, readerKey);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(votes, Is.EqualTo(expected));
                Assert.That(votes.Split.Yes + votes.Split.YesBut + votes.Split.No, Is.EqualTo(votes.VoteCount),
                    "every current vote sits in exactly one part of the split");
            }
        }

        private static IEnumerable<TestCaseData> Logs()
        {
            yield return new TestCaseData(Array.Empty<SizingLogEntry>(), Jonas, new RowVotes(0, null, new VoteSplit(0, 0, 0)))
                .SetName("An empty log has no votes and an empty split");

            yield return new TestCaseData(
                    new[] { VoteBy(Jonas, 1, SizingAnswer.Yes), VoteBy(Jonas, 2, SizingAnswer.YesBut) },
                    Jonas,
                    new RowVotes(1, SizingAnswer.YesBut, new VoteSplit(0, 1, 0)))
                .SetName("A changed mind replaces the earlier answer");

            yield return new TestCaseData(
                    new[] { VoteBy(Jonas, 7, SizingAnswer.No), VoteBy(Jonas, 3, SizingAnswer.Yes) },
                    Jonas,
                    new RowVotes(1, SizingAnswer.No, new VoteSplit(0, 0, 1)))
                .SetName("The latest entry wins whatever order the log arrives in");

            yield return new TestCaseData(
                    new[] { VoteBy(Jonas, 1, SizingAnswer.Yes), VoteBy(Ana, 2, SizingAnswer.No) },
                    Ana,
                    new RowVotes(2, SizingAnswer.No, new VoteSplit(1, 0, 1)))
                .SetName("Each reader sees only their own answer as theirs");

            yield return new TestCaseData(
                    new[] { VoteBy(Ana, 1, SizingAnswer.Yes, "Ana Lima"), VoteBy(OtherAna, 2, SizingAnswer.Yes, "Ana Lima") },
                    Priya,
                    new RowVotes(2, null, new VoteSplit(2, 0, 0)))
                .SetName("Two keys declaring one name are two voters");

            yield return new TestCaseData(
                    new[] { VoteBy(Jonas, 1, SizingAnswer.Yes), CommentBy(Ana, 2) },
                    null,
                    new RowVotes(1, null, new VoteSplit(1, 0, 0)))
                .SetName("A comment is not a vote");

            yield return new TestCaseData(
                    new[] { VoteBy(Jonas, 1, SizingAnswer.Yes), CommentBy(Jonas, 2) },
                    Jonas,
                    new RowVotes(1, SizingAnswer.Yes, new VoteSplit(1, 0, 0)))
                .SetName("A comment after a vote leaves the vote standing");

            yield return new TestCaseData(
                    new[] { VoteBy(Jonas, 1, SizingAnswer.Yes), RevocationBy(Jonas, 2), VoteBy(Ana, 3, SizingAnswer.Yes) },
                    Jonas,
                    new RowVotes(1, null, new VoteSplit(1, 0, 0)))
                .SetName("A vote taken back no longer counts");
        }

        [TestCase(0, 0, 0, 3, 3, RowReadiness.MoreYesNeeded, 3, TestName = "Nobody has voted, so every Yes is missing")]
        [TestCase(1, 0, 0, 3, 3, RowReadiness.MoreYesNeeded, 2, TestName = "One Yes of three leaves two missing")]
        [TestCase(2, 0, 1, 3, 3, RowReadiness.MoreYesNeeded, 1, TestName = "A No does not count as a Yes")]
        [TestCase(0, 0, 3, 3, 3, RowReadiness.MoreYesNeeded, 3, TestName = "Only No votes leave every Yes missing")]
        [TestCase(2, 1, 0, 3, 3, RowReadiness.Ready, null, TestName = "A Yes-if counts as a Yes")]
        [TestCase(0, 3, 0, 3, 3, RowReadiness.Ready, null, TestName = "Yes-if votes alone can make a Work Item Ready")]
        [TestCase(3, 0, 0, 3, 3, RowReadiness.Ready, null, TestName = "Exactly the Yes votes asked for are enough")]
        [TestCase(4, 0, 1, 3, 3, RowReadiness.Ready, null, TestName = "More Yes votes than asked for are still Ready")]
        [TestCase(0, 0, 1, 1, 1, RowReadiness.MoreYesNeeded, 1, TestName = "A single Yes asked for and only a No given")]
        [TestCase(1, 0, 0, 1, 1, RowReadiness.Ready, null, TestName = "A single Yes asked for and given")]
        [TestCase(2, 0, 0, 5, 5, RowReadiness.MoreYesNeeded, 3, TestName = "The missing count follows the Team's own minimum")]
        [TestCase(1, 0, 0, 2, 4, RowReadiness.MoreYesNeeded, 1, TestName = "A Yes shortfall is named before a voter shortfall")]
        [TestCase(2, 0, 0, 2, 3, RowReadiness.MoreVotersNeeded, 1, TestName = "Enough Yes votes from too few voters name the missing voters")]
        [TestCase(1, 1, 0, 2, 5, RowReadiness.MoreVotersNeeded, 3, TestName = "The missing voters follow the Team's own minimum")]
        [TestCase(2, 0, 1, 2, 3, RowReadiness.Ready, null, TestName = "A No counts as a voter")]
        [TestCase(1, 1, 1, 2, 3, RowReadiness.Ready, null, TestName = "Enough Yes votes from enough voters are Ready")]
        public void Enough_Yes_votes_make_a_Work_Item_Ready_and_a_shortfall_is_named(
            int yes, int yesBut, int no, int minYes, int minVoters, RowReadiness readiness, int? missingVotes)
        {
            var standing = RefinementResolution.StandingOf(
                new VoteSplit(yes, yesBut, no),
                new ReadinessSetting { MinYes = minYes, MinVoters = minVoters, DiscussWhen = NoDiscussion });

            Assert.That(standing, Is.EqualTo(new RowStanding(readiness, missingVotes)));
        }

        [TestCase(3, 0, 1, RowReadiness.NeedsDiscussion, TestName = "By default one No sends the Work Item to discussion")]
        [TestCase(1, 2, 0, RowReadiness.NeedsDiscussion, TestName = "By default two Yes-if votes send the Work Item to discussion")]
        [TestCase(2, 1, 0, RowReadiness.Ready, TestName = "By default one Yes-if alone does not send the Work Item to discussion")]
        [TestCase(3, 0, 0, RowReadiness.Ready, TestName = "By default Yes votes alone are Ready")]
        public void A_Team_that_never_set_the_discussion_rules_discusses_one_No_or_two_Yes_if_votes(
            int yes, int yesBut, int no, RowReadiness readiness)
        {
            var standing = RefinementResolution.StandingOf(new VoteSplit(yes, yesBut, no), new ReadinessSetting());

            Assert.That(standing, Is.EqualTo(new RowStanding(readiness, null)));
        }

        [TestCase(3, 0, 1, 1, null, RowReadiness.NeedsDiscussion, TestName = "A No at the No threshold sends the Work Item to discussion")]
        [TestCase(5, 0, 1, 1, null, RowReadiness.NeedsDiscussion, TestName = "A discussion beats any number of Yes votes")]
        [TestCase(0, 0, 1, 1, null, RowReadiness.NeedsDiscussion, TestName = "A discussion is named before missing Yes votes")]
        [TestCase(3, 0, 1, 2, null, RowReadiness.Ready, TestName = "One No below a No threshold of two does not send it")]
        [TestCase(3, 0, 2, 2, null, RowReadiness.NeedsDiscussion, TestName = "Two No votes reach a No threshold of two")]
        [TestCase(2, 1, 0, null, 1, RowReadiness.NeedsDiscussion, TestName = "A Yes-if at a Yes-if threshold of one sends the Work Item to discussion")]
        [TestCase(3, 1, 0, null, 2, RowReadiness.Ready, TestName = "One Yes-if below a Yes-if threshold of two does not send it")]
        [TestCase(1, 2, 0, null, 2, RowReadiness.NeedsDiscussion, TestName = "Two Yes-if votes reach a Yes-if threshold of two")]
        [TestCase(3, 1, 1, 2, 2, RowReadiness.Ready, TestName = "A No and a Yes-if do not add up across the two rules")]
        [TestCase(3, 0, 1, null, 2, RowReadiness.Ready, TestName = "With the No rule off a No sends nothing to discussion")]
        [TestCase(1, 2, 0, 1, null, RowReadiness.Ready, TestName = "With the Yes-if rule off Yes-if votes send nothing to discussion")]
        [TestCase(2, 2, 2, null, null, RowReadiness.Ready, TestName = "With both rules off only the Yes votes and voters decide")]
        public void Either_discussion_rule_on_its_own_sends_a_Work_Item_to_discussion(
            int yes, int yesBut, int no, int? noThreshold, int? yesIfThreshold, RowReadiness readiness)
        {
            var standing = RefinementResolution.StandingOf(
                new VoteSplit(yes, yesBut, no),
                new ReadinessSetting { MinYes = 3, MinVoters = 3, DiscussWhen = new DiscussionRules { No = noThreshold, YesIf = yesIfThreshold } });

            Assert.That(standing, Is.EqualTo(new RowStanding(readiness, null)));
        }

        [Test]
        public void A_No_taken_back_by_a_later_Yes_no_longer_sends_the_Work_Item_to_discussion()
        {
            var log = new[]
            {
                VoteBy(Jonas, 1, SizingAnswer.Yes),
                VoteBy(Priya, 2, SizingAnswer.Yes),
                VoteBy(Ana, 3, SizingAnswer.No),
                VoteBy(Ana, 4, SizingAnswer.Yes),
            };

            var standing = RefinementResolution.StandingOf(
                RefinementResolution.VotesOn(log, null).Split,
                new ReadinessSetting());

            Assert.That(standing, Is.EqualTo(RowStanding.Ready));
        }

        private static SizingLogEntry VoteBy(string voterKey, int id, SizingAnswer answer, string displayName = "Voter")
            => Entry(voterKey, id, SizingEntryKind.Vote, answer, displayName);

        private static SizingLogEntry CommentBy(string voterKey, int id)
            => Entry(voterKey, id, SizingEntryKind.Comment, null, "Voter");

        private static SizingLogEntry RevocationBy(string voterKey, int id)
            => Entry(voterKey, id, SizingEntryKind.Revocation, null, "Voter");

        private static SizingLogEntry Entry(string voterKey, int id, SizingEntryKind kind, SizingAnswer? answer, string displayName)
            => new()
            {
                Id = id,
                TeamId = 1,
                WorkItemReferenceId = "GR-051",
                Kind = kind,
                Answer = answer,
                VoterKey = voterKey,
                VoterDisplayName = displayName,
                RecordedAt = new DateTime(2026, 10, 4, 9, 0, 0, DateTimeKind.Utc),
                Channel = SizingChannel.Web,
                YardstickSource = YardstickSource.Sle,
            };
    }
}
