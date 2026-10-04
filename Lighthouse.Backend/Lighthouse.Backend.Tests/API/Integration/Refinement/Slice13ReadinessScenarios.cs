using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// Votes make a Work Item Ready. A Team admin says how many Yes votes that takes (at least one; three
    /// unless changed), how many voters (never fewer than the Yes votes; three unless changed), and which
    /// votes send it to discussion instead: one rule for No votes (one unless changed) and one for "Yes, if…"
    /// votes (two unless changed), each of which can be turned off on its own. A "Yes, if…" counts as a Yes.
    /// Each row then says Ready, how many more Yes votes or voters it needs, or that it needs discussion, and
    /// the tab counts the Work Items the votes have made Ready. What is missing is named so the Team can raise
    /// it in its own rituals; nothing is pushed to anybody.
    ///
    /// Driving ports: the Team settings write and read, the vote write and the Refinement tab's read. Step
    /// definitions live in Slice13ReadinessSpecifications.cs.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5510-5881-refinement")]
    [Category("slice-13")]
    public partial class Slice13ReadinessTest : SizingVotesAcceptanceTest
    {
        // --- The readiness setting ---

        // @driving_port @real-io @us-13 @slice-13 @boundary @contract-shape:pure-function
        [Test]
        public async Task A_Team_that_never_chose_readiness_needs_three_Yes_from_three_voters_and_discusses_one_No_or_two_Yes_if_votes()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            var settings = await WhenTheAdminOpensTheTeamSettings(gravity);

            ThenTheReadinessIs(settings, minYes: 3, minVoters: 3, TheDefaultDiscussion);
        }

        // @driving_port @real-io @us-13 @slice-13 @contract-shape:bounded-change
        [Test]
        public async Task A_Team_admin_sets_readiness_and_it_reads_back()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            await TheAdminHasSetReadiness(gravity, minYes: 2, minVoters: 3, new DiscussWhen(No: 2, YesIf: 3));

            ThenTheReadinessIs(await WhenTheAdminOpensTheTeamSettings(gravity), minYes: 2, minVoters: 3, new DiscussWhen(No: 2, YesIf: 3));
        }

        // @driving_port @real-io @us-13 @slice-13 @boundary @contract-shape:bounded-change
        // Each rule has its own switch: turning one off leaves the other as the admin set it.
        [TestCase(null, 2)]
        [TestCase(1, null)]
        [TestCase(null, null)]
        public async Task Either_discussion_rule_is_turned_off_on_its_own_and_reads_back_off(int? no, int? yesIf)
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            await TheAdminHasSetReadiness(gravity, minYes: 3, minVoters: 3, new DiscussWhen(no, yesIf));

            ThenTheReadinessIs(await WhenTheAdminOpensTheTeamSettings(gravity), minYes: 3, minVoters: 3, new DiscussWhen(no, yesIf));
        }

        // @driving_port @real-io @us-13 @slice-13 @error @contract-shape:unbounded-preservation
        // Green always means somebody looked, so at least one Yes is needed.
        [TestCase(0)]
        [TestCase(-1)]
        public async Task Fewer_than_one_Yes_is_refused_and_nothing_is_saved(int minYes)
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            using var refused = await WhenTheAdminSavesReadiness(gravity, minYes, minVoters: 3);

            await ThenTheSaveIsRefusedAndReadinessIsUnchanged(refused, gravity);
        }

        // @driving_port @real-io @us-13 @slice-13 @error @contract-shape:unbounded-preservation
        // Three Yes votes already are three voters, so fewer voters than Yes votes could never bite.
        [Test]
        public async Task Fewer_voters_than_Yes_votes_is_refused_and_nothing_is_saved()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            using var refused = await WhenTheAdminSavesReadiness(gravity, minYes: 3, minVoters: 2);

            await ThenTheSaveIsRefusedAndReadinessIsUnchanged(refused, gravity);
        }

        // @driving_port @real-io @us-13 @slice-13 @error @contract-shape:unbounded-preservation
        // A save that names only one side of the voter rule is judged with the side already stored.
        [TestCase("minVoters", 2)]
        [TestCase("minYes", 4)]
        public async Task A_partial_save_that_would_leave_fewer_voters_than_Yes_votes_is_refused_and_nothing_is_saved(string field, int value)
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            using var refused = await WhenTheAdminSavesOnlyTheReadinessField(gravity, field, value);

            await ThenTheSaveIsRefusedAndReadinessIsUnchanged(refused, gravity);
        }

        // @driving_port @real-io @us-13 @slice-13 @boundary @contract-shape:bounded-change
        [Test]
        public async Task As_many_voters_as_Yes_votes_is_accepted()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            await TheAdminHasSetReadiness(gravity, minYes: 2, minVoters: 2);

            ThenTheReadinessIs(await WhenTheAdminOpensTheTeamSettings(gravity), minYes: 2, minVoters: 2, TheDefaultDiscussion);
        }

        // @driving_port @real-io @us-13 @slice-13 @error @contract-shape:unbounded-preservation
        // A rule that needs no votes would send every Work Item to discussion; turning the rule off is the way to silence it.
        [TestCase(0, 2)]
        [TestCase(1, 0)]
        public async Task A_discussion_rule_of_zero_votes_is_refused_and_nothing_is_saved(int no, int yesIf)
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            using var refused = await WhenTheAdminSavesReadiness(gravity, minYes: 3, minVoters: 3, new DiscussWhen(no, yesIf));

            await ThenTheSaveIsRefusedAndReadinessIsUnchanged(refused, gravity);
        }

        // @driving_port @real-io @us-13 @slice-13 @boundary @contract-shape:unbounded-preservation
        // An older settings form knows nothing about readiness; its saves must not reset what the admin chose.
        [Test]
        public async Task A_save_that_says_nothing_about_readiness_leaves_it_as_it_was()
        {
            var gravity = await GivenTheAdminChoseTwoYesFromTwoVotersAndNoDiscussionOnNo();

            using var save = await WhenTheSettingsAreSavedWithoutReadiness(gravity);

            ThenTheReadinessIs(await WhenTheAdminOpensTheTeamSettings(gravity), minYes: 2, minVoters: 2, new DiscussWhen(No: null, YesIf: 3));
        }

        // @driving_port @real-io @us-13 @slice-13 @boundary @contract-shape:unbounded-preservation
        [Test]
        public async Task A_readiness_save_that_leaves_out_the_discussion_rules_keeps_them()
        {
            var gravity = await GivenTheAdminChoseTwoYesFromTwoVotersAndNoDiscussionOnNo();

            await TheAdminHasSetReadiness(gravity, minYes: 1, minVoters: 1);

            ThenTheReadinessIs(await WhenTheAdminOpensTheTeamSettings(gravity), minYes: 1, minVoters: 1, new DiscussWhen(No: null, YesIf: 3));
        }

        // @driving_port @real-io @us-13 @slice-13 @boundary @contract-shape:unbounded-preservation
        [Test]
        public async Task A_save_of_only_the_discussion_rules_keeps_the_Yes_votes_and_voters()
        {
            var gravity = await GivenTheAdminChoseTwoYesFromTwoVotersAndNoDiscussionOnNo();

            using var save = await WhenTheAdminSavesOnlyTheDiscussionRules(gravity, new DiscussWhen(No: 3, YesIf: null));

            ThenTheReadinessIs(await WhenTheAdminOpensTheTeamSettings(gravity), minYes: 2, minVoters: 2, new DiscussWhen(No: 3, YesIf: null));
        }

        // @driving_port @real-io @us-13 @slice-13 @boundary @contract-shape:unbounded-preservation
        // A threshold left out of the save keeps the stored one; only a threshold sent as null turns its rule off.
        [Test]
        public async Task A_save_that_sends_only_the_No_rule_keeps_the_stored_Yes_if_rule()
        {
            var gravity = await GivenTheAdminChoseTwoYesFromTwoVotersAndNoDiscussionOnNo();

            using var save = await WhenTheAdminSavesOnlyTheDiscussionThreshold(gravity, "no", 2);

            ThenTheReadinessIs(await WhenTheAdminOpensTheTeamSettings(gravity), minYes: 2, minVoters: 2, new DiscussWhen(No: 2, YesIf: 3));
        }

        // @driving_port @real-io @us-13 @slice-13 @boundary @contract-shape:unbounded-preservation
        [Test]
        public async Task A_save_that_sends_only_the_Yes_if_rule_keeps_the_stored_No_rule()
        {
            var gravity = await GivenTheDiscussionRules(new DiscussWhen(No: 2, YesIf: null));

            using var save = await WhenTheAdminSavesOnlyTheDiscussionThreshold(gravity, "yesIf", 3);

            ThenTheReadinessIs(await WhenTheAdminOpensTheTeamSettings(gravity), minYes: 3, minVoters: 3, new DiscussWhen(No: 2, YesIf: 3));
        }

        // @driving_port @real-io @us-13 @slice-13 @boundary @contract-shape:unbounded-preservation
        [Test]
        public async Task Changing_readiness_keeps_every_Work_Item_the_Team_holds()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();
            var heldBefore = WorkItemsStoredFor(gravity);

            await TheAdminHasSetReadiness(gravity, minYes: 1, minVoters: 1, new DiscussWhen(No: 2, YesIf: null));

            ThenTheReadinessIsStoredAndTheWorkItemsKept(await WhenTheAdminOpensTheTeamSettings(gravity), gravity, heldBefore);
        }

        // --- What the votes make of each row ---

        // @driving_port @real-io @us-13 @slice-13 @kpi-OUT-5510-K5-ready-before-the-day @contract-shape:pure-function
        [Test]
        public async Task Enough_Yes_votes_make_a_Work_Item_Ready_and_the_tab_counts_it()
        {
            var gravity = await GivenJonasAndMoSaidYesAndAnaSaidYesButOn(ConfigurationManagement);

            var tab = await WhenPriyaOpensTheRefinementTab(gravity);

            ThenTheRowIsReadyAndTheTabCounts(tab, ConfigurationManagement, readyByVotes: 1);
        }

        // @driving_port @real-io @us-13 @slice-13 @contract-shape:pure-function
        [Test]
        public async Task The_missing_Yes_votes_are_named()
        {
            var gravity = await GivenOnlyJonasSaidYesOn(AdvancedReporting);

            var tab = await WhenPriyaOpensTheRefinementTab(gravity);

            ThenTheRowNeeds(tab, AdvancedReporting, MoreYesNeeded, missingVotes: 2);
        }

        // @driving_port @real-io @us-13 @slice-13 @boundary @contract-shape:pure-function
        [Test]
        public async Task A_Work_Item_nobody_has_voted_on_needs_every_Yes()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            var tab = await WhenPriyaOpensTheRefinementTab(gravity);

            ThenTheRowNeeds(tab, LoadTesting, MoreYesNeeded, missingVotes: 3);
        }

        // @driving_port @real-io @us-13 @slice-13 @boundary @contract-shape:pure-function
        [Test]
        public async Task A_No_does_not_count_towards_the_Yes_votes()
        {
            var gravity = await GivenNoDiscussionOnNoAndJonasAndMoSaidYesAndAnaSaidNoOn(ApiVersioning);

            var tab = await WhenPriyaOpensTheRefinementTab(gravity);

            ThenTheRowNeeds(tab, ApiVersioning, MoreYesNeeded, missingVotes: 1);
        }

        // @driving_port @real-io @us-13 @slice-13 @boundary @contract-shape:pure-function
        [Test]
        public async Task Enough_Yes_votes_from_too_few_voters_name_the_missing_voters()
        {
            var gravity = await GivenReadinessOfTwoYesFromThreeVotersAndTwoYesOn(AdvancedReporting);

            var tab = await WhenPriyaOpensTheRefinementTab(gravity);

            ThenTheRowNeeds(tab, AdvancedReporting, MoreVotersNeeded, missingVotes: 1);
        }

        // @driving_port @real-io @us-13 @slice-13 @contract-shape:bounded-change
        [Test]
        public async Task Any_answer_from_the_missing_voter_makes_the_Work_Item_Ready()
        {
            var gravity = await GivenReadinessOfTwoYesFromThreeVotersAndTwoYesOn(AdvancedReporting, NoDiscussionOnNo);

            await WhenAnaVotes(gravity, AdvancedReporting, Answer.No);

            ThenTheRowIsReadyAndTheTabCounts(await WhenPriyaOpensTheRefinementTab(gravity), AdvancedReporting, readyByVotes: 1);
        }

        // @driving_port @real-io @us-13 @slice-13 @error @contract-shape:pure-function
        // A discussion wins over any number of Yes votes: the doubt is what the meeting is for.
        [Test]
        public async Task By_default_one_No_sends_a_Work_Item_to_discussion_however_many_say_Yes()
        {
            var gravity = await GivenThreeYesAndAnasNoOn(ApiVersioning);

            var tab = await WhenPriyaOpensTheRefinementTab(gravity);

            ThenTheRowNeedsDiscussionAndIsNotCounted(tab, ApiVersioning);
        }

        // @driving_port @real-io @us-13 @slice-13 @boundary @contract-shape:pure-function
        // A single "Yes, if…" is a Yes with a caveat the Team can take along; a second one is worth a talk.
        [TestCase(1, Ready)]
        [TestCase(2, NeedsDiscussion)]
        public async Task By_default_the_second_Yes_if_sends_a_Work_Item_to_discussion(int yesIfVotes, string readiness)
        {
            var gravity = await GivenThreeVotersOfWhomSomeSaidYesIfOn(AdvancedReporting, yesIfVotes);

            var tab = await WhenPriyaOpensTheRefinementTab(gravity);

            Assert.That(RowOf(tab, AdvancedReporting).Readiness, Is.EqualTo(readiness));
        }

        // @driving_port @real-io @us-13 @slice-13 @boundary @contract-shape:pure-function
        // Jonas, Mo, Ana and Priya answer in that order; the readiness itself stays three Yes from three voters.
        [TestCase(null, 2, new[] { Answer.Yes, Answer.Yes, Answer.Yes, Answer.No }, Ready, TestName = "With the No rule off a No sends nothing to discussion")]
        [TestCase(null, 2, new[] { Answer.Yes, Answer.YesBut, Answer.YesBut }, NeedsDiscussion, TestName = "With the No rule off the Yes-if rule still sends to discussion")]
        [TestCase(1, null, new[] { Answer.Yes, Answer.YesBut, Answer.YesBut }, Ready, TestName = "With the Yes-if rule off Yes-if votes send nothing to discussion")]
        [TestCase(1, null, new[] { Answer.Yes, Answer.Yes, Answer.Yes, Answer.No }, NeedsDiscussion, TestName = "With the Yes-if rule off the No rule still sends to discussion")]
        [TestCase(null, null, new[] { Answer.YesBut, Answer.YesBut, Answer.YesBut, Answer.No }, Ready, TestName = "With both rules off only the Yes votes and voters decide")]
        [TestCase(2, 3, new[] { Answer.Yes, Answer.Yes, Answer.Yes, Answer.No }, Ready, TestName = "One No stays below a No rule of two")]
        [TestCase(2, 3, new[] { Answer.Yes, Answer.Yes, Answer.No, Answer.No }, NeedsDiscussion, TestName = "Two No votes reach a No rule of two")]
        [TestCase(2, 3, new[] { Answer.YesBut, Answer.YesBut, Answer.Yes }, Ready, TestName = "Two Yes-if votes stay below a Yes-if rule of three")]
        [TestCase(2, 3, new[] { Answer.YesBut, Answer.YesBut, Answer.YesBut }, NeedsDiscussion, TestName = "Three Yes-if votes reach a Yes-if rule of three")]
        [TestCase(2, 2, new[] { Answer.Yes, Answer.Yes, Answer.YesBut, Answer.No }, Ready, TestName = "A No and a Yes-if do not add up across the two rules")]
        public async Task Each_discussion_rule_sends_a_Work_Item_to_discussion_on_its_own(int? no, int? yesIf, Answer[] answers, string readiness)
        {
            var gravity = await GivenTheDiscussionRules(new DiscussWhen(no, yesIf));
            await GivenTheVotersAnsweredOn(gravity, AdvancedReporting, answers);

            var tab = await WhenPriyaOpensTheRefinementTab(gravity);

            Assert.That(RowOf(tab, AdvancedReporting).Readiness, Is.EqualTo(readiness));
        }

        // @driving_port @real-io @us-13 @slice-13 @contract-shape:bounded-change
        // Only the latest answer of each voter counts, for a discussion too.
        [Test]
        public async Task A_No_changed_to_Yes_lifts_the_discussion()
        {
            var gravity = await GivenThreeYesAndAnasNoOn(ApiVersioning);

            await WhenAnaVotes(gravity, ApiVersioning, Answer.Yes);

            ThenTheRowIsReadyAndTheTabCounts(await WhenPriyaOpensTheRefinementTab(gravity), ApiVersioning, readyByVotes: 1);
        }

        // @driving_port @real-io @us-13 @slice-13 @kpi-OUT-5510-K5-ready-before-the-day @contract-shape:bounded-change
        // Usage data counts a Work Item reaching Ready once, so only the vote that tipped it may say it did.
        [Test]
        public async Task Only_the_vote_that_makes_a_Work_Item_Ready_says_it_did()
        {
            var gravity = await GivenTheAdminChoseTwoYesFromTwoVotersAndNoDiscussionOnNo();
            await HasVoted(Jonas, gravity, AdvancedReporting, Answer.Yes);

            var tipping = await WhenTheVoteIsCast(Mo, gravity, AdvancedReporting, Answer.Yes);
            var following = await WhenTheVoteIsCast(Ana, gravity, AdvancedReporting, Answer.Yes);

            ThenOnlyTheFirstSaysItMadeTheWorkItemReady(tipping, following);
        }

        // @driving_port @real-io @us-13 @slice-13 @boundary @kpi-OUT-5510-K5-ready-before-the-day @contract-shape:unbounded-preservation
        [Test]
        public async Task A_vote_on_a_Work_Item_that_was_Ready_already_does_not_say_it_made_it_Ready()
        {
            var gravity = await GivenJonasAndMoSaidYesAndAnaSaidYesButOn(ConfigurationManagement);

            var answer = await WhenTheVoteIsCast(Priya, gravity, ConfigurationManagement, Answer.Yes);

            ThenTheVoteDidNotMakeItReady(answer);
        }

        // @driving_port @real-io @us-13 @slice-13 @boundary @kpi-OUT-5510-K5-ready-before-the-day @contract-shape:unbounded-preservation
        [Test]
        public async Task A_vote_that_leaves_a_Work_Item_short_does_not_say_it_made_it_Ready()
        {
            var gravity = await GivenOnlyJonasSaidYesOn(AdvancedReporting);

            var answer = await WhenTheVoteIsCast(Mo, gravity, AdvancedReporting, Answer.Yes);

            ThenTheVoteDidNotMakeItReady(answer);
        }

        // @driving_port @real-io @us-13 @slice-13 @contract-shape:bounded-change
        [Test]
        public async Task Lowering_readiness_makes_a_Work_Item_Ready_on_the_next_read()
        {
            var gravity = await GivenJonasAndMoSaidYesOn(AdvancedReporting);

            await TheAdminHasSetReadiness(gravity, minYes: 2, minVoters: 2);

            ThenTheRowIsReadyAndTheTabCounts(await WhenPriyaOpensTheRefinementTab(gravity), AdvancedReporting, readyByVotes: 1);
        }

        // @driving_port @real-io @us-13 @slice-13 @contract-shape:pure-function
        [Test]
        public async Task The_tab_counts_only_the_Work_Items_the_votes_made_Ready()
        {
            var gravity = await GivenTwoWorkItemsReadyByVotesAndOneShortOfIt();

            var tab = await WhenPriyaOpensTheRefinementTab(gravity);

            Assert.That(ReadyByVotesIn(tab), Is.EqualTo(2));
        }
    }
}
