using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Interfaces.Refinement;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// A vote can say why: any answer may carry a comment, and a "Yes, if…" is where the condition goes.
    /// A comment can also stand alone - a question rather than a view. It counts for nothing, and it marks
    /// the Work Item as having an open question until the person who asked it votes. Every vote, comment
    /// and change of mind stays in the Work Item's log, oldest first, saying who, when and from where.
    ///
    /// The log is open to everybody who can open the tab, voted or not: a Product Owner who never votes
    /// still reads how the Team voted and why.
    ///
    /// Driven at the application-service port: votes and comments are written through the sizing log's
    /// commands for a voter established as the votes route establishes one, and the tab and a Work Item's log
    /// are read as the browser receives them, before they are written out as JSON. What a refused comment is
    /// answered with on the wire, the shape of the log and the log of a Work Item the tab does not list are
    /// pinned on the wire contract of the Refinement tab. Step definitions live in
    /// Slice12CommentsSpecifications.cs.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5510-5881-refinement")]
    [Category("slice-12")]
    public partial class Slice12CommentsTest : RefinementAtThePortTest
    {
        [Test]
        public void A_Yes_but_carries_its_condition_into_the_Work_Items_log()
        {
            var gravity = GivenGravityRefinesAndNobodyHasVoted();

            WhenAnaSaysYesBut(gravity, AdvancedReporting, OnlyIfThePdfExportMovesOut);

            var log = WhenAnaOpensTheLogOf(gravity, AdvancedReporting);

            ThenTheLogReads(log,
                new LogEntryReading("Vote", nameof(SizingAnswer.YesBut), OnlyIfThePdfExportMovesOut, AnaLima, nameof(SizingChannel.Web), null, IsMine: true));
        }

        [TestCase(SizingAnswer.Yes)]
        [TestCase(SizingAnswer.No)]
        public void Any_answer_may_carry_a_comment(SizingAnswer answer)
        {
            var gravity = GivenGravityRefinesAndNobodyHasVoted();

            WhenAnaVotesWithAComment(gravity, answer, NeedsTheDesignFirst);

            var log = WhenAnaOpensTheLogOf(gravity, AdvancedReporting);

            ThenTheLogReads(log,
                new LogEntryReading("Vote", answer.ToString(), NeedsTheDesignFirst, AnaLima, nameof(SizingChannel.Web), null, IsMine: true));
        }

        [Test]
        public void A_vote_without_a_comment_carries_none()
        {
            var gravity = GivenGravityRefinesAndNobodyHasVoted();

            WhenAnaVotesWithoutAComment(gravity);

            var log = WhenAnaOpensTheLogOf(gravity, AdvancedReporting);

            ThenTheLogReads(log,
                new LogEntryReading("Vote", nameof(SizingAnswer.Yes), null, AnaLima, nameof(SizingChannel.Web), null, IsMine: true));
        }

        // Changing your mind adds to the log; nothing is ever overwritten.
        [Test]
        public void A_changed_mind_keeps_both_votes_in_the_log_oldest_first()
        {
            var gravity = GivenJonasVotedYesOnConfigurationManagement();

            WhenJonasChangesHisMindTo(gravity, SizingAnswer.YesBut);

            ThenJonasReadsTheAnswersInOrder(gravity, ConfigurationManagement, nameof(SizingAnswer.Yes), nameof(SizingAnswer.YesBut));
        }

        [Test]
        public void The_log_says_who_said_what_when_and_from_where_oldest_first()
        {
            var gravity = GivenJonasVotedYesAnaVotedNoAndMoAskedAQuestionOnAdvancedReporting();

            var log = WhenAnaOpensTheLogOf(gravity, AdvancedReporting);

            ThenTheLogReads(log,
                new LogEntryReading("Vote", nameof(SizingAnswer.Yes), null, JonasWeber, nameof(SizingChannel.Web), null, IsMine: false),
                new LogEntryReading("Vote", nameof(SizingAnswer.No), null, AnaLima, nameof(SizingChannel.Web), null, IsMine: true),
                new LogEntryReading("Comment", null, WhichApiVersion, MoOkafor, nameof(SizingChannel.Web), null, IsMine: false));
        }

        // The server decides which entry is the open question, so every reader marks the same one.
        [Test]
        public void The_log_marks_the_question_still_waiting_for_its_askers_vote()
        {
            var gravity = GivenJonasVotedYesAnaVotedNoAndMoAskedAQuestionOnAdvancedReporting();

            var log = WhenAnaOpensTheLogOf(gravity, AdvancedReporting);

            ThenOnlyMosQuestionIsMarkedOpen(log);
        }

        // The names travel beside the split so a reader can see who stands behind each count; a changed mind moves
        // its voter, and each list keeps the order in which those current votes were cast.
        [Test]
        public void The_log_names_who_currently_votes_each_answer_in_the_order_they_voted()
        {
            var gravity = GivenJonasVotedYesAnaVotedNoAndMoAskedAQuestionOnAdvancedReporting();
            HasVoted(Mo, gravity, AdvancedReporting, SizingAnswer.Yes);

            WhenAnaChangesHerMindTo(gravity, AdvancedReporting, SizingAnswer.Yes);

            ThenPriyaReadsTheVoters(gravity, AdvancedReporting,
                new VotersReading(Yes: [JonasWeber, MoOkafor, AnaLima], YesBut: [], No: []));
        }

        // Nobody has to vote to read the others: the split and every comment are there for anyone.
        [Test]
        public void Somebody_who_never_voted_reads_how_the_votes_split_and_every_comment()
        {
            var gravity = GivenJonasVotedYesAnaVotedNoAndMoAskedAQuestionOnAdvancedReporting();

            var (row, log) = WhenPriyaWhoNeverVotedLooksAt(gravity, AdvancedReporting);

            ThenPriyaReadsTheSplitAndTheLog(row, log, new SplitReading(Yes: 1, YesBut: 0, No: 1),
                new LogEntryReading("Vote", nameof(SizingAnswer.Yes), null, JonasWeber, nameof(SizingChannel.Web), null, IsMine: false),
                new LogEntryReading("Vote", nameof(SizingAnswer.No), null, AnaLima, nameof(SizingChannel.Web), null, IsMine: false),
                new LogEntryReading("Comment", null, WhichApiVersion, MoOkafor, nameof(SizingChannel.Web), null, IsMine: false));
        }

        [Test]
        public void A_question_without_a_vote_flags_the_Work_Item_and_counts_as_no_vote()
        {
            var gravity = GivenGravityRefinesAndNobodyHasVoted();

            WhenJonasAsks(gravity, ApiVersioning, WhichApiVersion);

            ThenTheRowReads(gravity, ApiVersioning, voteCount: 0, hasOpenQuestion: true, hasComments: true);
        }

        [Test]
        public void The_asker_voting_closes_their_own_question()
        {
            var gravity = GivenJonasAskedWhichApiVersionOnApiVersioning();

            WhenTheyVote(Jonas, gravity, ApiVersioning, SizingAnswer.Yes);

            ThenTheRowReads(gravity, ApiVersioning, voteCount: 1, hasOpenQuestion: false, hasComments: true);
        }

        [Test]
        public void Somebody_elses_vote_does_not_close_the_question()
        {
            var gravity = GivenJonasAskedWhichApiVersionOnApiVersioning();

            WhenTheyVote(Ana, gravity, ApiVersioning, SizingAnswer.Yes);

            ThenTheRowReads(gravity, ApiVersioning, voteCount: 1, hasOpenQuestion: true, hasComments: true);
        }

        [Test]
        public void A_comment_from_somebody_who_has_voted_is_no_open_question_and_keeps_their_vote()
        {
            var gravity = GivenGravityRefinesAndNobodyHasVoted();
            HasVoted(Ana, gravity, ApiVersioning, SizingAnswer.No);

            WhenAnaComments(gravity, ApiVersioning, NeedsTheDesignFirst);

            ThenAnaStillCountsWith(gravity, ApiVersioning, SizingAnswer.No);
        }

        [Test]
        public void A_question_counts_for_nothing_towards_readiness()
        {
            var gravity = GivenGravityRefinesAndNobodyHasVoted();

            WhenJonasAsks(gravity, ApiVersioning, WhichApiVersion);

            ThenTheReadinessOfTheRowIs(gravity, ApiVersioning, "MoreYesNeeded", missingVotes: 3);
        }

        [TestCase("")]
        [TestCase("   ")]
        public void An_empty_question_is_refused_and_nothing_is_recorded(string comment)
        {
            var gravity = GivenGravityRefinesAndNobodyHasVoted();

            var refused = WhenJonasTriesToAsk(gravity, ApiVersioning, comment);

            ThenTheCommentIsRefusedAndTheRowIsUntouched(refused, gravity, ApiVersioning, SizingOutcome.CommentMissing);
        }

        [Test]
        public void A_comment_of_two_thousand_characters_is_kept_whole()
        {
            var gravity = GivenGravityRefinesAndNobodyHasVoted();
            var twoThousand = new string('x', 2000);

            WhenJonasVotesYesWithTheComment(gravity, twoThousand);

            ThenJonasReadsHisComment(gravity, twoThousand);
        }

        [Test]
        public void A_comment_longer_than_two_thousand_characters_is_refused()
        {
            var gravity = GivenGravityRefinesAndNobodyHasVoted();

            var refused = WhenJonasTriesToAsk(gravity, ApiVersioning, new string('x', 2001));

            ThenTheCommentIsRefusedAndTheRowIsUntouched(refused, gravity, ApiVersioning, SizingOutcome.CommentTooLong);
        }

        [Test]
        public void A_question_of_two_thousand_characters_is_kept_whole()
        {
            var gravity = GivenGravityRefinesAndNobodyHasVoted();
            var twoThousand = new string('x', 2000);

            WhenJonasAsks(gravity, ConfigurationManagement, twoThousand);

            ThenJonasReadsHisComment(gravity, twoThousand);
        }

        [Test]
        public void A_vote_whose_comment_is_longer_than_two_thousand_characters_is_refused()
        {
            var gravity = GivenGravityRefinesAndNobodyHasVoted();

            var refused = WhenJonasTriesToVoteYesWithTheComment(gravity, ApiVersioning, new string('x', 2001));

            ThenTheCommentIsRefusedAndTheRowIsUntouched(refused, gravity, ApiVersioning, SizingOutcome.CommentTooLong);
        }

        [Test]
        public void A_question_on_a_Work_Item_that_is_not_in_refinement_is_refused()
        {
            var gravity = GivenGravityRefinesAndNobodyHasVoted();

            var refused = WhenJonasTriesToAsk(gravity, BillingExport, WhichApiVersion);

            ThenTheCommentIsRefusedAndTheRowIsUntouched(refused, gravity, ApiVersioning, SizingOutcome.WorkItemNotInRefinement);
        }

        // Whatever somebody types is text: it is stored and handed back exactly, never interpreted.
        [Test]
        public void A_comment_comes_back_exactly_as_it_was_written()
        {
            var gravity = GivenGravityRefinesAndNobodyHasVoted();

            WhenJonasVotesYesWithTheComment(gravity, MarkupAndQuotes);

            ThenJonasReadsHisComment(gravity, MarkupAndQuotes);
        }
    }
}
