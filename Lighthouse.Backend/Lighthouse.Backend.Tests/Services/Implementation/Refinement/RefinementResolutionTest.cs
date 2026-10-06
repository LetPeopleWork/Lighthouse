using Lighthouse.Backend.Models;
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

        private static readonly int[] OpenAtOne = [1];

        private static readonly int[] OpenAtTwo = [2];

        private static readonly int[] OpenAtThree = [3];

        private static readonly int[] OpenAtNineAndFive = [9, 5];

        private static readonly DiscussionRules NoDiscussion = new() { No = null, YesIf = null };

        private static readonly string[] Nobody = [];

        private static readonly string[] OnlyJonas = ["Jonas Weber"];

        private static readonly string[] OnlyAna = ["Ana Lima"];

        private static readonly string[] AnaLimaTwice = ["Ana Lima", "Ana Lima"];

        private static readonly string[] AnaThenPriyaThenJonas = ["Ana Lima", "Priya Sharma", "Jonas Weber"];

        private static readonly string[] JonasThenPriya = ["Jonas Weber", "Priya Sharma"];

        private static readonly string[] AnaAsSheVoted = ["Ana"];

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

            yield return new TestCaseData(
                    new[] { VoteBy(Jonas, 1, SizingAnswer.Yes), RevocationBy(Jonas, 2), VoteBy(Jonas, 3, SizingAnswer.No) },
                    Jonas,
                    new RowVotes(1, SizingAnswer.No, new VoteSplit(0, 0, 1)))
                .SetName("Voting again after taking back counts again");

            yield return new TestCaseData(
                    new[] { VoteBy(Jonas, 1, SizingAnswer.Yes), RevocationBy(Jonas, 2), CommentBy(Jonas, 3) },
                    Jonas,
                    new RowVotes(0, null, new VoteSplit(0, 0, 0)))
                .SetName("A comment after taking back leaves the vote taken back");
        }

        [TestCaseSource(nameof(VoterLogs))]
        public void Each_answer_names_the_people_whose_current_vote_it_is_in_the_order_they_cast_it(
            SizingLogEntry[] log, string[] yes, string[] yesBut, string[] no)
        {
            var voters = RefinementResolution.VotersOn(log);
            var split = RefinementResolution.VotesOn(log, null).Split;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(voters.Yes, Is.EqualTo(yes), "Yes");
                Assert.That(voters.YesBut, Is.EqualTo(yesBut), "Yes, if");
                Assert.That(voters.No, Is.EqualTo(no), "No");
                Assert.That(voters.Split, Is.EqualTo(split), "the names under each answer and the split never disagree");
            }
        }

        private static IEnumerable<TestCaseData> VoterLogs()
        {
            yield return new TestCaseData(Array.Empty<SizingLogEntry>(), Nobody, Nobody, Nobody)
                .SetName("An empty log names nobody under any answer");

            yield return new TestCaseData(
                    new[] { VoteBy(Ana, 1, SizingAnswer.Yes, "Ana Lima"), VoteBy(OtherAna, 2, SizingAnswer.Yes, "Ana Lima") },
                    AnaLimaTwice, Nobody, Nobody)
                .SetName("Two voters giving the same name are named twice");

            yield return new TestCaseData(
                    new[] { VoteBy(Ana, 1, SizingAnswer.Yes, "Ana"), VoteBy(Ana, 2, SizingAnswer.Yes, "Ana Lima") },
                    OnlyAna, Nobody, Nobody)
                .SetName("A voter who renamed themselves is named once, as their current vote gives it");

            yield return new TestCaseData(
                    new[] { VoteBy(Ana, 1, SizingAnswer.No, "Ana"), CommentBy(Ana, 2, "Ana Lima") },
                    Nobody, Nobody, AnaAsSheVoted)
                .SetName("A comment under a new name does not rename the vote");

            yield return new TestCaseData(
                    new[] { VoteBy(Jonas, 1, SizingAnswer.Yes, "Jonas Weber"), VoteBy(Ana, 2, SizingAnswer.No, "Ana Lima"), RevocationBy(Jonas, 3) },
                    Nobody, Nobody, OnlyAna)
                .SetName("A vote taken back no longer names its voter");

            yield return new TestCaseData(
                    new[] { VoteBy(Jonas, 1, SizingAnswer.Yes, "Jonas Weber"), VoteBy(Ana, 2, SizingAnswer.Yes, "Ana Lima"), VoteBy(Jonas, 3, SizingAnswer.No, "Jonas Weber") },
                    OnlyAna, Nobody, OnlyJonas)
                .SetName("A changed mind moves the voter to their new answer");

            yield return new TestCaseData(
                    new[] { VoteBy(Jonas, 1, SizingAnswer.Yes, "Jonas Weber"), CommentBy(Ana, 2, "Ana Lima") },
                    OnlyJonas, Nobody, Nobody)
                .SetName("Somebody who only commented is named under no answer");

            yield return new TestCaseData(
                    new[]
                    {
                        VoteBy(Jonas, 1, SizingAnswer.Yes, "Jonas Weber"),
                        VoteBy(Ana, 2, SizingAnswer.Yes, "Ana Lima"),
                        VoteBy(Priya, 3, SizingAnswer.Yes, "Priya Sharma"),
                        VoteBy(Jonas, 4, SizingAnswer.Yes, "Jonas Weber"),
                    },
                    AnaThenPriyaThenJonas, Nobody, Nobody)
                .SetName("Voters are named in the order their current votes were cast");

            yield return new TestCaseData(
                    new[] { VoteBy(Priya, 9, SizingAnswer.YesBut, "Priya Sharma"), VoteBy(Jonas, 2, SizingAnswer.YesBut, "Jonas Weber") },
                    Nobody, JonasThenPriya, Nobody)
                .SetName("The order of names follows the log, not its arrival");
        }

        [TestCaseSource(nameof(Conversations))]
        public void A_question_stays_open_until_the_asker_votes(SizingLogEntry[] log, RowConversation expected)
        {
            var conversation = RefinementResolution.ConversationOn(log);

            Assert.That(conversation, Is.EqualTo(expected));
        }

        private static IEnumerable<TestCaseData> Conversations()
        {
            yield return new TestCaseData(Array.Empty<SizingLogEntry>(), new RowConversation(HasComments: false, HasOpenQuestion: false))
                .SetName("An empty log has neither comments nor a question");

            yield return new TestCaseData(
                    new[] { VoteBy(Jonas, 1, SizingAnswer.Yes) },
                    new RowConversation(HasComments: false, HasOpenQuestion: false))
                .SetName("A vote without a comment is no comment");

            yield return new TestCaseData(
                    new[] { VoteWithCommentBy(Jonas, 1, SizingAnswer.YesBut) },
                    new RowConversation(HasComments: true, HasOpenQuestion: false))
                .SetName("A vote's comment is a comment but no question");

            yield return new TestCaseData(
                    new[] { CommentBy(Jonas, 1) },
                    new RowConversation(HasComments: true, HasOpenQuestion: true))
                .SetName("A comment from somebody without a vote is an open question");

            yield return new TestCaseData(
                    new[] { CommentBy(Jonas, 1), VoteBy(Jonas, 2, SizingAnswer.Yes) },
                    new RowConversation(HasComments: true, HasOpenQuestion: false))
                .SetName("The asker voting closes their question");

            yield return new TestCaseData(
                    new[] { CommentBy(Jonas, 1), VoteBy(Ana, 2, SizingAnswer.Yes) },
                    new RowConversation(HasComments: true, HasOpenQuestion: true))
                .SetName("Somebody else's vote leaves the question open");

            yield return new TestCaseData(
                    new[] { VoteBy(Ana, 1, SizingAnswer.No), CommentBy(Ana, 2) },
                    new RowConversation(HasComments: true, HasOpenQuestion: false))
                .SetName("A comment after a vote is no question");

            yield return new TestCaseData(
                    new[] { CommentBy(Jonas, 1), VoteBy(Jonas, 2, SizingAnswer.Yes), CommentBy(Jonas, 3) },
                    new RowConversation(HasComments: true, HasOpenQuestion: false))
                .SetName("Commenting again while holding a vote opens no question");

            yield return new TestCaseData(
                    new[] { VoteBy(Jonas, 1, SizingAnswer.Yes), RevocationBy(Jonas, 2), CommentBy(Jonas, 3) },
                    new RowConversation(HasComments: true, HasOpenQuestion: true))
                .SetName("A comment after taking back a vote is an open question");

            yield return new TestCaseData(
                    new[] { CommentBy(Jonas, 3), VoteBy(Jonas, 7, SizingAnswer.Yes) },
                    new RowConversation(HasComments: true, HasOpenQuestion: false))
                .SetName("Whether a question is open follows the log's order, not its arrival");
        }

        [TestCaseSource(nameof(OpenQuestions))]
        public void Only_the_askers_latest_comment_is_the_open_question(SizingLogEntry[] log, int[] openQuestionIds)
        {
            var marked = RefinementResolution.OpenQuestionsIn(log);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(marked, Is.EquivalentTo(openQuestionIds));
                Assert.That(RefinementResolution.ConversationOn(log).HasOpenQuestion, Is.EqualTo(openQuestionIds.Length > 0),
                    "the row has an open question exactly when one of its entries is marked as one");
            }
        }

        private static IEnumerable<TestCaseData> OpenQuestions()
        {
            yield return new TestCaseData(Array.Empty<SizingLogEntry>(), Array.Empty<int>())
                .SetName("An empty log marks nothing");

            yield return new TestCaseData(new[] { CommentBy(Jonas, 1) }, OpenAtOne)
                .SetName("A comment from somebody without a vote is marked");

            yield return new TestCaseData(new[] { CommentBy(Jonas, 1), CommentBy(Jonas, 2) }, OpenAtTwo)
                .SetName("Only the asker's latest comment is marked");

            yield return new TestCaseData(new[] { CommentBy(Jonas, 1), VoteBy(Jonas, 2, SizingAnswer.Yes) }, Array.Empty<int>())
                .SetName("The asker's vote unmarks their comment");

            yield return new TestCaseData(new[] { VoteWithCommentBy(Jonas, 1, SizingAnswer.YesBut) }, Array.Empty<int>())
                .SetName("A vote's own comment is never marked");

            yield return new TestCaseData(
                    new[] { CommentBy(Jonas, 1), VoteBy(Jonas, 2, SizingAnswer.Yes), RevocationBy(Jonas, 3) },
                    Array.Empty<int>())
                .SetName("A question answered by a vote that is then taken back stays closed");

            yield return new TestCaseData(
                    new[] { VoteBy(Jonas, 1, SizingAnswer.Yes), CommentBy(Jonas, 2), RevocationBy(Jonas, 3) },
                    Array.Empty<int>())
                .SetName("A comment followed by taking back the vote is not marked");

            yield return new TestCaseData(
                    new[] { VoteBy(Jonas, 1, SizingAnswer.Yes), RevocationBy(Jonas, 2), CommentBy(Jonas, 3) },
                    OpenAtThree)
                .SetName("A comment after taking back a vote is marked");

            yield return new TestCaseData(
                    new[] { CommentBy(Ana, 1, "Ana Lima"), VoteBy(OtherAna, 2, SizingAnswer.Yes, "Ana Lima") },
                    OpenAtOne)
                .SetName("Another voter declaring the same name does not unmark the question");

            yield return new TestCaseData(
                    new[] { CommentBy(Ana, 1, "Ana"), VoteBy(Ana, 2, SizingAnswer.Yes, "Ana Lima") },
                    Array.Empty<int>())
                .SetName("A voter who renamed themselves still closes their own question");

            yield return new TestCaseData(
                    new[] { CommentBy(Ana, 1, "Ana"), CommentBy(Ana, 2, "Ana Lima") },
                    OpenAtTwo)
                .SetName("A voter who renamed themselves has one open question");

            yield return new TestCaseData(
                    new[] { CommentBy(Jonas, 9), CommentBy(Jonas, 4), CommentBy(Ana, 5), VoteBy(Priya, 6, SizingAnswer.No) },
                    OpenAtNineAndFive)
                .SetName("Each asker's latest comment is marked whatever order the log arrives in");
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

        [TestCaseSource(nameof(StepsToReady))]
        public void Only_the_entry_that_moves_a_Work_Item_to_Ready_made_it_Ready(SizingLogEntry[] log, int entryId, bool madeReady)
        {
            var twoYesFromTwoVoters = new ReadinessSetting { MinYes = 2, MinVoters = 2 };

            var made = RefinementResolution.MadeReady(log, log.Single(entry => entry.Id == entryId), twoYesFromTwoVoters);

            Assert.That(made, Is.EqualTo(madeReady));
        }

        private static IEnumerable<TestCaseData> StepsToReady()
        {
            yield return new TestCaseData(
                    new[] { VoteBy(Jonas, 1, SizingAnswer.Yes), VoteBy(Ana, 2, SizingAnswer.Yes) }, 2, true)
                .SetName("The second Yes of two made the Work Item Ready");

            yield return new TestCaseData(
                    new[] { VoteBy(Jonas, 1, SizingAnswer.Yes), VoteBy(Ana, 2, SizingAnswer.Yes) }, 1, false)
                .SetName("The first Yes of two left the Work Item short of Ready");

            yield return new TestCaseData(
                    new[] { VoteBy(Jonas, 1, SizingAnswer.Yes), VoteBy(Ana, 2, SizingAnswer.Yes), VoteBy(Priya, 3, SizingAnswer.Yes) }, 3, false)
                .SetName("A Yes on a Work Item that was already Ready did not make it Ready");

            yield return new TestCaseData(
                    new[] { VoteBy(Jonas, 1, SizingAnswer.Yes), VoteBy(Ana, 2, SizingAnswer.Yes), VoteBy(Priya, 3, SizingAnswer.No) }, 2, true)
                .SetName("The entry that made a Work Item Ready keeps that even after a later No takes it away");

            yield return new TestCaseData(
                    new[] { VoteBy(Jonas, 1, SizingAnswer.Yes), VoteBy(Ana, 2, SizingAnswer.Yes), VoteBy(Priya, 3, SizingAnswer.No) }, 3, false)
                .SetName("A No that sends a Ready Work Item to discussion did not make it Ready");
        }

        [TestCase(false, false, RefinementStage.Waiting, TestName = "A Work Item no rule matches is Waiting")]
        [TestCase(true, false, RefinementStage.Ready, TestName = "A Work Item only the Ready rule matches is Ready")]
        [TestCase(false, true, RefinementStage.BeingRefined, TestName = "A Work Item only the Being refined rule matches is being refined")]
        [TestCase(true, true, RefinementStage.Ready, TestName = "A Work Item both rules match is Ready")]
        public void The_Ready_rule_wins_and_whatever_no_rule_matches_is_Waiting(bool matchesReady, bool matchesBeingRefined, RefinementStage stage)
        {
            Assert.That(RefinementResolution.StageOf(matchesReady, matchesBeingRefined), Is.EqualTo(stage));
        }

        [TestCaseSource(nameof(StageAgainstVotes))]
        public void The_stage_and_the_votes_cast_disagree_only_about_being_Ready(RefinementStage? stage, int voteCount, RowReadiness readiness, bool disagree)
        {
            var votes = new RowVotes(voteCount, null, new VoteSplit(voteCount, 0, 0));

            Assert.That(RefinementResolution.SignalsDisagree(stage, votes, new RowStanding(readiness, null)), Is.EqualTo(disagree));
        }

        // Every stage against every reading of the votes, once with nobody voting and once with votes cast. The
        // expected answer is written out by hand for each pair so the table cannot agree with itself.
        private static IEnumerable<TestCaseData> StageAgainstVotes()
        {
            var disagreeOnceVoted = new (RefinementStage? Stage, RowReadiness Readiness, bool Disagree)[]
            {
                (null, RowReadiness.Ready, false),
                (null, RowReadiness.MoreYesNeeded, false),
                (null, RowReadiness.MoreVotersNeeded, false),
                (null, RowReadiness.NeedsDiscussion, false),
                (RefinementStage.Waiting, RowReadiness.Ready, true),
                (RefinementStage.Waiting, RowReadiness.MoreYesNeeded, false),
                (RefinementStage.Waiting, RowReadiness.MoreVotersNeeded, false),
                (RefinementStage.Waiting, RowReadiness.NeedsDiscussion, false),
                (RefinementStage.BeingRefined, RowReadiness.Ready, true),
                (RefinementStage.BeingRefined, RowReadiness.MoreYesNeeded, false),
                (RefinementStage.BeingRefined, RowReadiness.MoreVotersNeeded, false),
                (RefinementStage.BeingRefined, RowReadiness.NeedsDiscussion, false),
                (RefinementStage.Ready, RowReadiness.Ready, false),
                (RefinementStage.Ready, RowReadiness.MoreYesNeeded, true),
                (RefinementStage.Ready, RowReadiness.MoreVotersNeeded, true),
                (RefinementStage.Ready, RowReadiness.NeedsDiscussion, true),
            };

            foreach (var (stage, readiness, disagree) in disagreeOnceVoted)
            {
                var stageName = stage?.ToString() ?? "No";
                yield return new TestCaseData(stage, 0, readiness, false)
                    .SetName($"{stageName} stage, votes read {readiness}, nobody voted: no disagreement");
                yield return new TestCaseData(stage, 1, readiness, disagree)
                    .SetName($"{stageName} stage, votes read {readiness}, votes cast: disagree is {disagree}");
            }
        }

        [TestCase(false, 0, 0, 0, 0, ReadySource.Votes, TestName = "Without stage rules and no Ready votes nothing is Ready")]
        [TestCase(false, 0, 3, 0, 3, ReadySource.Votes, TestName = "Without stage rules every Work Item the votes call Ready counts")]
        [TestCase(true, 0, 0, 0, 0, ReadySource.Stages, TestName = "Stage rules that make nothing Ready count nothing")]
        [TestCase(true, 2, 0, 0, 2, ReadySource.Stages, TestName = "With stage rules every Work Item whose stage is Ready counts")]
        [TestCase(true, 0, 3, 0, 0, ReadySource.Stages, TestName = "With stage rules Ready votes alone count nothing")]
        [TestCase(true, 2, 3, 0, 2, ReadySource.Stages, TestName = "With stage rules the stages and the votes are never added together")]
        [TestCase(true, 1, 0, 1, 2, ReadySource.Stages, TestName = "A Work Item both signals call Ready counts once")]
        [TestCase(true, 0, 2, 2, 2, ReadySource.Stages, TestName = "With stage rules only the Ready stages count, whatever else the votes say")]
        public void The_ready_count_follows_the_votes_without_stage_rules_and_the_stages_with_them(
            bool stagesConfigured, int readyByStageOnly, int readyByVotesOnly, int readyByBoth, int readyCount, ReadySource source)
        {
            var rows = RowsWith(stagesConfigured, readyByStageOnly, readyByVotesOnly, readyByBoth);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(RefinementResolution.ReadyCountOf(stagesConfigured, rows), Is.EqualTo(readyCount));
                Assert.That(RefinementResolution.ReadySourceOf(stagesConfigured), Is.EqualTo(source));
                Assert.That(RefinementResolution.ReadyByVotesCountOf(rows), Is.EqualTo(readyByVotesOnly + readyByBoth), "the votes' own count never changes meaning");
            }
        }

        // Every table also carries one Work Item neither signal calls Ready, so a count of all rows shows up.
        private static List<RefinementRow> RowsWith(bool stagesConfigured, int readyByStageOnly, int readyByVotesOnly, int readyByBoth)
        {
            RefinementStage? StageWhen(bool ready)
            {
                if (!stagesConfigured)
                {
                    return null;
                }

                return ready ? RefinementStage.Ready : RefinementStage.Waiting;
            }

            var notReadyByVotes = new RowStanding(RowReadiness.MoreYesNeeded, 3);

            return
            [
                .. Enumerable.Range(0, readyByStageOnly).Select(_ => Row(StageWhen(true), notReadyByVotes)),
                .. Enumerable.Range(0, readyByVotesOnly).Select(_ => Row(StageWhen(false), RowStanding.Ready)),
                .. Enumerable.Range(0, readyByBoth).Select(_ => Row(StageWhen(true), RowStanding.Ready)),
                Row(stagesConfigured ? RefinementStage.BeingRefined : null, notReadyByVotes),
            ];
        }

        private static RefinementRow Row(RefinementStage? stage, RowStanding standing)
            => new(new WorkItem(), RowVotes.None, standing, stage);

        private static SizingLogEntry VoteBy(string voterKey, int id, SizingAnswer answer, string displayName = "Voter")
            => Entry(voterKey, id, SizingEntryKind.Vote, answer, displayName);

        private static SizingLogEntry CommentBy(string voterKey, int id, string displayName = "Voter")
            => Entry(voterKey, id, SizingEntryKind.Comment, null, displayName, "Which API version?");

        private static SizingLogEntry VoteWithCommentBy(string voterKey, int id, SizingAnswer answer)
            => Entry(voterKey, id, SizingEntryKind.Vote, answer, "Voter", "Only if the export moves out");

        private static SizingLogEntry RevocationBy(string voterKey, int id)
            => Entry(voterKey, id, SizingEntryKind.Revocation, null, "Voter");

        private static SizingLogEntry Entry(string voterKey, int id, SizingEntryKind kind, SizingAnswer? answer, string displayName, string? comment = null)
            => new()
            {
                Comment = comment,
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
