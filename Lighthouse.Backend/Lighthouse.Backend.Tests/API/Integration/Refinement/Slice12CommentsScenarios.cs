using System.Net;
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
    /// Driving ports: the vote and comment writes, the Work Item's log and the Refinement tab's read. Step
    /// definitions live in Slice12CommentsSpecifications.cs.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5510-5881-refinement")]
    [Category("slice-12")]
    public partial class Slice12CommentsTest : SizingVotesAcceptanceTest
    {
        // @driving_port @real-io @us-12 @slice-12 @contract-shape:bounded-change
        [Test]
        public async Task A_Yes_but_carries_its_condition_into_the_Work_Items_log()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            await WhenAnaSaysYesBut(gravity, AdvancedReporting, OnlyIfThePdfExportMovesOut);

            await ThenAnaReadsInTheLog(gravity, AdvancedReporting,
                new LogEntryReading("Vote", nameof(Answer.YesBut), OnlyIfThePdfExportMovesOut, AnaLima, nameof(Channel.Web), null, IsMine: true));
        }

        // @driving_port @real-io @us-12 @slice-12 @contract-shape:bounded-change
        [TestCase(Answer.Yes)]
        [TestCase(Answer.No)]
        public async Task Any_answer_may_carry_a_comment(Answer answer)
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            await WhenAnaVotesWithAComment(gravity, answer, NeedsTheDesignFirst);

            await ThenAnaReadsInTheLog(gravity, AdvancedReporting,
                new LogEntryReading("Vote", answer.ToString(), NeedsTheDesignFirst, AnaLima, nameof(Channel.Web), null, IsMine: true));
        }

        // @driving_port @real-io @us-12 @slice-12 @boundary @contract-shape:bounded-change
        [Test]
        public async Task A_vote_without_a_comment_carries_none()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            await WhenAnaVotesWithoutAComment(gravity);

            await ThenAnaReadsInTheLog(gravity, AdvancedReporting,
                new LogEntryReading("Vote", nameof(Answer.Yes), null, AnaLima, nameof(Channel.Web), null, IsMine: true));
        }

        // @driving_port @real-io @us-11 @us-12 @slice-12 @contract-shape:bounded-change
        // Changing your mind adds to the log; nothing is ever overwritten.
        [Test]
        public async Task A_changed_mind_keeps_both_votes_in_the_log_oldest_first()
        {
            var gravity = await GivenJonasVotedYesOnConfigurationManagement();

            await WhenJonasChangesHisMindTo(gravity, Answer.YesBut);

            await ThenJonasReadsTheAnswersInOrder(gravity, ConfigurationManagement, nameof(Answer.Yes), nameof(Answer.YesBut));
        }

        // @driving_port @real-io @us-12 @slice-12 @contract-shape:pure-function
        [Test]
        public async Task The_log_says_who_said_what_when_and_from_where_oldest_first()
        {
            var gravity = await GivenJonasVotedYesAnaVotedNoAndMoAskedAQuestionOnAdvancedReporting();

            var log = await WhenAnaOpensTheLogOf(gravity, AdvancedReporting);

            ThenTheLogReads(log,
                new LogEntryReading("Vote", nameof(Answer.Yes), null, JonasWeber, nameof(Channel.Web), null, IsMine: false),
                new LogEntryReading("Vote", nameof(Answer.No), null, AnaLima, nameof(Channel.Web), null, IsMine: true),
                new LogEntryReading("Comment", null, WhichApiVersion, MoOkafor, nameof(Channel.Web), null, IsMine: false));
        }

        // @driving_port @real-io @us-11 @us-12 @slice-12 @boundary @contract-shape:pure-function
        // Nobody has to vote to read the others: the split and every comment are there for anyone.
        [Test]
        public async Task Somebody_who_never_voted_reads_how_the_votes_split_and_every_comment()
        {
            var gravity = await GivenJonasVotedYesAnaVotedNoAndMoAskedAQuestionOnAdvancedReporting();

            var (row, log) = await WhenPriyaWhoNeverVotedLooksAt(gravity, AdvancedReporting);

            ThenPriyaReadsTheSplitAndTheLog(row, log, new SplitReading(Yes: 1, YesBut: 0, No: 1),
                new LogEntryReading("Vote", nameof(Answer.Yes), null, JonasWeber, nameof(Channel.Web), null, IsMine: false),
                new LogEntryReading("Vote", nameof(Answer.No), null, AnaLima, nameof(Channel.Web), null, IsMine: false),
                new LogEntryReading("Comment", null, WhichApiVersion, MoOkafor, nameof(Channel.Web), null, IsMine: false));
        }

        // @driving_port @real-io @us-12 @slice-12 @contract-shape:bounded-change
        [Test]
        public async Task A_question_without_a_vote_flags_the_Work_Item_and_counts_as_no_vote()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            await WhenJonasAsks(gravity, ApiVersioning, WhichApiVersion);

            await ThenTheRowReads(gravity, ApiVersioning, voteCount: 0, hasOpenQuestion: true, hasComments: true);
        }

        // @driving_port @real-io @us-12 @slice-12 @contract-shape:bounded-change
        [Test]
        public async Task The_asker_voting_closes_their_own_question()
        {
            var gravity = await GivenJonasAskedWhichApiVersionOnApiVersioning();

            await WhenTheyVote(Jonas, gravity, ApiVersioning, Answer.Yes);

            await ThenTheRowReads(gravity, ApiVersioning, voteCount: 1, hasOpenQuestion: false, hasComments: true);
        }

        // @driving_port @real-io @us-12 @slice-12 @boundary @contract-shape:bounded-change
        [Test]
        public async Task Somebody_elses_vote_does_not_close_the_question()
        {
            var gravity = await GivenJonasAskedWhichApiVersionOnApiVersioning();

            await WhenTheyVote(Ana, gravity, ApiVersioning, Answer.Yes);

            await ThenTheRowReads(gravity, ApiVersioning, voteCount: 1, hasOpenQuestion: true, hasComments: true);
        }

        // @driving_port @real-io @us-12 @slice-12 @boundary @contract-shape:bounded-change
        [Test]
        public async Task A_comment_from_somebody_who_has_voted_is_no_open_question_and_keeps_their_vote()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();
            await HasVoted(Ana, gravity, ApiVersioning, Answer.No);

            await WhenAnaComments(gravity, ApiVersioning, NeedsTheDesignFirst);

            await ThenAnaStillCountsWith(gravity, ApiVersioning, Answer.No);
        }

        // @driving_port @real-io @us-12 @us-13 @slice-12 @boundary @contract-shape:bounded-change
        [Test]
        public async Task A_question_counts_for_nothing_towards_readiness()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            await WhenJonasAsks(gravity, ApiVersioning, WhichApiVersion);

            await ThenTheReadinessOfTheRowIs(gravity, ApiVersioning, "MoreYesNeeded", missingVotes: 3);
        }

        // @driving_port @real-io @us-12 @slice-12 @error @contract-shape:unbounded-preservation
        [TestCase("")]
        [TestCase("   ")]
        public async Task An_empty_question_is_refused_and_nothing_is_recorded(string comment)
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            using var refused = await WhenJonasTriesToAsk(gravity, ApiVersioning, comment);

            await ThenTheCommentIsRefusedAndTheRowIsUntouched(refused, gravity, ApiVersioning, HttpStatusCode.BadRequest, CommentRequired);
        }

        // @driving_port @real-io @us-12 @slice-12 @boundary @contract-shape:bounded-change
        [Test]
        public async Task A_comment_of_two_thousand_characters_is_kept_whole()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();
            var twoThousand = new string('x', 2000);

            await WhenJonasVotesYesWithTheComment(gravity, twoThousand);

            await ThenJonasReadsHisComment(gravity, twoThousand);
        }

        // @driving_port @real-io @us-12 @slice-12 @error @contract-shape:unbounded-preservation
        [Test]
        public async Task A_comment_longer_than_two_thousand_characters_is_refused()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            using var refused = await WhenJonasTriesToAsk(gravity, ApiVersioning, new string('x', 2001));

            await ThenTheCommentIsRefusedAndTheRowIsUntouched(refused, gravity, ApiVersioning, HttpStatusCode.BadRequest, CommentTooLong);
        }

        // @driving_port @real-io @us-12 @slice-12 @boundary @contract-shape:bounded-change
        [Test]
        public async Task A_question_of_two_thousand_characters_is_kept_whole()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();
            var twoThousand = new string('x', 2000);

            await WhenJonasAsks(gravity, ConfigurationManagement, twoThousand);

            await ThenJonasReadsHisComment(gravity, twoThousand);
        }

        // @driving_port @real-io @us-12 @slice-12 @error @contract-shape:unbounded-preservation
        [Test]
        public async Task A_vote_whose_comment_is_longer_than_two_thousand_characters_is_refused()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            using var refused = await WhenJonasTriesToVoteYesWithTheComment(gravity, ApiVersioning, new string('x', 2001));

            await ThenTheCommentIsRefusedAndTheRowIsUntouched(refused, gravity, ApiVersioning, HttpStatusCode.BadRequest, CommentTooLong);
        }

        // @driving_port @real-io @us-12 @slice-12 @error @contract-shape:unbounded-preservation
        [Test]
        public async Task A_question_without_a_name_is_refused()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            using var refused = await Comments(ABrowserWithoutAName(), gravity, ApiVersioning, WhichApiVersion);

            await ThenTheCommentIsRefusedAndTheRowIsUntouched(refused, gravity, ApiVersioning, HttpStatusCode.BadRequest, VoterNameRequired);
        }

        // @driving_port @real-io @us-12 @slice-12 @error @contract-shape:unbounded-preservation
        [Test]
        public async Task A_question_on_a_Work_Item_that_is_not_in_refinement_is_refused()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            using var refused = await WhenJonasTriesToAsk(gravity, BillingExport, WhichApiVersion);

            await ThenTheCommentIsRefusedAndTheRowIsUntouched(refused, gravity, ApiVersioning, HttpStatusCode.Conflict, WorkItemNotInRefinement);
        }

        // @driving_port @real-io @us-12 @slice-12 @error @contract-shape:pure-function
        // The log opens for a listed Work Item first, so the refusal can only be about the Work Item.
        [Test]
        public async Task The_log_of_a_Work_Item_that_is_not_in_refinement_is_not_found()
        {
            var gravity = await GivenAnaVotedAndCanOpenTheLogOfAdvancedReporting();

            using var refused = await OpensTheLog(Ana, gravity, BillingExport);

            Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        // @driving_port @real-io @us-12 @slice-12 @boundary @contract-shape:bounded-change
        // Whatever somebody types is text: it is stored and handed back exactly, never interpreted.
        [Test]
        public async Task A_comment_comes_back_exactly_as_it_was_written()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            await WhenJonasVotesYesWithTheComment(gravity, MarkupAndQuotes);

            await ThenJonasReadsHisComment(gravity, MarkupAndQuotes);
        }
    }
}
