using System.Net;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// On an instance without sign-in - every Community instance - anyone who can open the Refinement tab
    /// says Yes, "Yes, if…" or No on any Work Item in a refinement state, at any time. The voter is the
    /// name they declared plus the key their browser keeps, so two people with one name stay two voters
    /// and nobody can speak as somebody else's browser. Every vote is added to the Team's sizing log;
    /// changing your mind adds another entry and the latest one counts. The tab shows how many have voted
    /// and which answer is the reader's own.
    ///
    /// Driving ports: the vote write and the Refinement tab's read. Step definitions live in
    /// Slice11CastAVoteSpecifications.cs.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5510-5881-refinement")]
    [Category("slice-11")]
    public partial class Slice11CastAVoteTest : SizingVotesAcceptanceTest
    {
        // @driving_port @real-io @us-11 @slice-11 @kpi-OUT-5510-K4-votes-outside-the-meeting @contract-shape:bounded-change
        [Test]
        public async Task A_reader_votes_in_seconds_without_an_account()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            var answer = await WhenJonasVotes(gravity, ConfigurationManagement, Answer.Yes);

            await ThenTheVoteIsTakenAndTheRowCountsIt(answer, gravity, ConfigurationManagement, voteCount: 1, myVote: Answer.Yes);
        }

        // @driving_port @real-io @us-11 @slice-11 @contract-shape:bounded-change
        [Test]
        public async Task Changing_ones_mind_replaces_the_current_vote_and_still_counts_once()
        {
            var gravity = await GivenJonasHasVotedYesOnConfigurationManagement();

            using var answer = await WhenJonasVotes(gravity, ConfigurationManagement, Answer.YesBut);

            await ThenJonasSeesTheRow(gravity, ConfigurationManagement, voteCount: 1, myVote: Answer.YesBut);
        }

        // @driving_port @real-io @us-11 @slice-11 @contract-shape:bounded-change
        [Test]
        public async Task Every_voter_counts_once_and_sees_only_their_own_answer_as_theirs()
        {
            var gravity = await GivenJonasHasVotedYesOnConfigurationManagement();

            await WhenAnaVotesFromHerOwnBrowser(gravity, ConfigurationManagement, Answer.No);

            await ThenEachVoterSeesTwoVotesAndTheirOwnAnswer(gravity);
        }

        // @driving_port @real-io @us-11 @slice-11 @contract-shape:bounded-change
        [Test]
        public async Task Every_reader_sees_how_the_votes_split()
        {
            var gravity = await GivenJonasHasVotedYesOnConfigurationManagement();

            await WhenAnaVotesFromHerOwnBrowser(gravity, ConfigurationManagement, Answer.No);

            await ThenEveryReaderSeesTheSplit(gravity, ConfigurationManagement, new SplitReading(Yes: 1, YesBut: 0, No: 1));
        }

        // @driving_port @real-io @us-11 @slice-11 @boundary @contract-shape:bounded-change
        // A name is not an identity: two browsers declaring "Ana Lima" are two people.
        [Test]
        public async Task Two_people_who_declare_the_same_name_are_two_voters()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            await WhenTwoBrowsersBothCalledAnaLimaVoteYes(gravity, AdvancedReporting);

            await ThenJonasSeesTheRow(gravity, AdvancedReporting, voteCount: 2, myVote: null);
        }

        // @driving_port @real-io @us-11 @slice-11 @boundary @contract-shape:bounded-change
        // Votes are always open: whatever stage of refinement a Work Item is in, and wherever it sits in the list.
        [TestCase(UserActivityTracking)]
        [TestCase(AdvancedReporting)]
        [TestCase(LoadTesting)]
        public async Task Votes_are_open_on_every_Work_Item_in_refinement(string workItem)
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            using var answer = await WhenJonasVotes(gravity, workItem, Answer.No);

            await ThenJonasSeesTheRow(gravity, workItem, voteCount: 1, myVote: Answer.No);
        }

        // @driving_port @real-io @us-11 @slice-11 @error @contract-shape:unbounded-preservation
        // GR-040 is being implemented, not refined; GR-999 is a Work Item the tracker does not hold.
        [TestCase(BillingExport)]
        [TestCase("GR-999")]
        public async Task A_Work_Item_that_is_not_in_refinement_cannot_be_voted_on(string workItem)
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            using var answer = await WhenJonasVotes(gravity, workItem, Answer.Yes);

            await ThenTheVoteIsRefusedAndNothingIsCounted(answer, gravity, HttpStatusCode.Conflict, WorkItemNotInRefinement);
        }

        // @driving_port @real-io @us-11 @slice-11 @error @contract-shape:unbounded-preservation
        [TestCase(null)]
        [TestCase("   ")]
        [Ignore(PendingSlice11)]
        public async Task A_vote_without_a_name_is_refused_and_nothing_is_counted(string? declaredName)
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            using var answer = await WhenABrowserVotesHavingDeclared(gravity, declaredName);

            await ThenTheVoteIsRefusedAndNothingIsCounted(answer, gravity, HttpStatusCode.BadRequest, VoterNameRequired);
        }

        // @driving_port @real-io @us-11 @slice-11 @error @contract-shape:unbounded-preservation
        // A key shorter than thirty-two characters is as good as none: it could be guessed.
        [TestCase(null)]
        [TestCase("0123456789abcdef0123456789abcde")]
        [Ignore(PendingSlice11)]
        public async Task A_vote_without_a_voter_key_is_refused_and_nothing_is_counted(string? voterKey)
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            using var answer = await WhenJonasVotesFromABrowserKeeping(gravity, voterKey);

            await ThenTheVoteIsRefusedAndNothingIsCounted(answer, gravity, HttpStatusCode.BadRequest, VoterKeyRequired);
        }

        // @driving_port @real-io @us-11 @slice-11 @boundary @contract-shape:bounded-change
        [Test]
        [Ignore(PendingSlice11)]
        public async Task A_declared_name_of_one_hundred_characters_is_accepted()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            using var answer = await WhenABrowserVotesHavingDeclared(gravity, new string('a', 100));

            await ThenJonasSeesTheRow(gravity, ConfigurationManagement, voteCount: 1, myVote: null);
        }

        // @driving_port @real-io @us-11 @slice-11 @error @contract-shape:unbounded-preservation
        [Test]
        [Ignore(PendingSlice11)]
        public async Task A_declared_name_longer_than_one_hundred_characters_is_refused()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            using var answer = await WhenABrowserVotesHavingDeclared(gravity, new string('a', 101));

            await ThenTheVoteIsRefusedAndNothingIsCounted(answer, gravity, HttpStatusCode.BadRequest, expectedCode: null);
        }

        // @driving_port @real-io @us-11 @slice-11 @error @contract-shape:unbounded-preservation
        // There is no fourth answer: "can't tell yet" is a comment, not a vote.
        [TestCase("Maybe")]
        [TestCase("")]
        [TestCase(null)]
        [Ignore(PendingSlice11)]
        public async Task An_answer_other_than_Yes_Yes_but_or_No_is_refused(string? answer)
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            using var refused = await WhenJonasSendsTheAnswer(gravity, answer);

            await ThenTheVoteIsRefusedAndNothingIsCounted(refused, gravity, HttpStatusCode.BadRequest, expectedCode: null);
        }

        // @driving_port @real-io @us-11 @slice-11 @error @contract-shape:unbounded-preservation
        [TestCase("Email")]
        [TestCase(null)]
        [Ignore(PendingSlice11)]
        public async Task A_vote_that_does_not_say_where_it_was_cast_from_is_refused(string? channel)
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            using var refused = await WhenJonasVotesSayingItCameFrom(gravity, channel);

            await ThenTheVoteIsRefusedAndNothingIsCounted(refused, gravity, HttpStatusCode.BadRequest, expectedCode: null);
        }

        // @driving_port @real-io @us-11 @slice-11 @boundary @contract-shape:pure-function
        // The key is what makes a vote somebody's; an answer that echoed it would hand it to anyone watching.
        [Test]
        [Ignore(PendingSlice11)]
        public async Task The_voter_key_never_comes_back_in_any_answer()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            var whatCameBack = await WhenJonasVotesAndOpensTheTab(gravity);

            ThenNothingCarriesJonassKey(whatCameBack);
        }

        // @driving_port @real-io @us-11 @slice-11 @error @contract-shape:unbounded-preservation
        [Test]
        [Ignore(PendingSlice11)]
        public async Task A_vote_for_a_Team_that_does_not_exist_is_not_found()
        {
            var gravity = await GivenJonasHasVotedYesOnConfigurationManagement();

            using var answer = await WhenJonasVotesOnATeamThatDoesNotExist();

            await ThenTheVoteIsRefusedAndNothingIsCounted(answer, gravity, HttpStatusCode.NotFound, expectedCode: null, alreadyCounted: 1);
        }

        // @driving_port @real-io @us-11 @slice-11 @boundary @contract-shape:bounded-change
        // The log keeps every vote. A Work Item that leaves refinement is no longer listed; when it comes
        // back, so do its votes.
        [Test]
        public async Task Votes_outlive_a_Work_Item_leaving_refinement_and_count_again_when_it_returns()
        {
            var gravity = await GivenJonasHasVotedYesOnConfigurationManagement();

            await WhenTheAdminTakesBacklogOutOfRefinementAndPutsItBack(gravity);

            await ThenJonasSeesTheRow(gravity, ConfigurationManagement, voteCount: 1, myVote: Answer.Yes);
        }

        // @driving_port @real-io @us-11 @slice-11 @contract-shape:unbounded-preservation
        // A vote is a view, not an edit: the Team's settings and Work Items stay exactly as they were.
        [Test]
        [Ignore(PendingSlice11)]
        public async Task Voting_changes_nothing_about_the_Team_or_its_Work_Items()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();
            var before = await TheTeamAsItStands(gravity);

            await WhenJonasVotesOnEveryWorkItemInRefinement(gravity);

            await ThenTheTeamIsAsItWas(gravity, before);
        }

        // @driving_port @real-io @us-11 @slice-11 @error @contract-shape:unbounded-preservation
        // Thirty entries a minute is about ten times what a person needs; the thirty-first is held back.
        [Test]
        [Ignore(PendingSlice11)]
        public async Task A_voter_sending_more_than_thirty_entries_a_minute_is_told_to_slow_down()
        {
            var gravity = await GivenJonasHasVotedYesThirtyTimesWithinAMinute();

            using var thirtyFirst = await WhenJonasVotes(gravity, ConfigurationManagement, Answer.No);

            await ThenJonasIsToldToSlowDownAndHisYesStillCounts(thirtyFirst, gravity);
        }

        // @driving_port @real-io @us-11 @slice-11 @contract-shape:pure-function
        // The tab tells the browser how a voter is known here, so it knows to ask for a name.
        [Test]
        public async Task Without_sign_in_the_tab_says_a_voter_declares_their_name()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            var tab = await TheTabAsSeenBy(Jonas, gravity);

            Assert.That(VoterIdentityIn(tab), Is.EqualTo("SelfDeclared"));
        }
    }
}
