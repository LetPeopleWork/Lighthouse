using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// Votes make a Work Item Ready. A Team admin says how many Yes votes that takes (at least one; three
    /// unless changed), how many voters (never fewer than the Yes votes; three unless changed), and,
    /// optionally, how many No votes - or No and "Yes, if…" votes - send it to discussion instead. A
    /// "Yes, if…" counts as a Yes. Each row then says Ready, how many more Yes votes or voters it needs, or
    /// that it needs discussion, and the tab counts the Work Items the votes have made Ready. What is missing
    /// is named so the Team can raise it in its own rituals; nothing is pushed to anybody.
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
        public async Task A_Team_that_never_chose_readiness_needs_three_Yes_from_three_voters_and_has_no_veto()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            var settings = await WhenTheAdminOpensTheTeamSettings(gravity);

            ThenTheReadinessIs(settings, minYes: 3, minVoters: 3, vetoThreshold: null, vetoCounts: null);
        }

        // @driving_port @real-io @us-13 @slice-13 @contract-shape:bounded-change
        [Test]
        public async Task A_Team_admin_sets_readiness_and_it_reads_back()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            await TheAdminHasSetReadiness(gravity, minYes: 2, minVoters: 3, vetoThreshold: 1, VetoCounts.NoOrYesBut);

            ThenTheReadinessIs(await WhenTheAdminOpensTheTeamSettings(gravity), minYes: 2, minVoters: 3, vetoThreshold: 1, vetoCounts: nameof(VetoCounts.NoOrYesBut));
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

        // @driving_port @real-io @us-13 @slice-13 @boundary @contract-shape:bounded-change
        [Test]
        public async Task As_many_voters_as_Yes_votes_is_accepted()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            await TheAdminHasSetReadiness(gravity, minYes: 2, minVoters: 2);

            ThenTheReadinessIs(await WhenTheAdminOpensTheTeamSettings(gravity), minYes: 2, minVoters: 2, vetoThreshold: null, vetoCounts: null);
        }

        // @driving_port @real-io @us-13 @slice-13 @error @contract-shape:unbounded-preservation
        [Test]
        public async Task A_veto_of_zero_votes_is_refused_and_nothing_is_saved()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();

            using var refused = await WhenTheAdminSavesReadiness(gravity, minYes: 3, minVoters: 3, vetoThreshold: 0);

            await ThenTheSaveIsRefusedAndReadinessIsUnchanged(refused, gravity);
        }

        // @driving_port @real-io @us-13 @slice-13 @boundary @contract-shape:unbounded-preservation
        // An older settings form knows nothing about readiness; its saves must not reset what the admin chose.
        [Test]
        public async Task A_save_that_says_nothing_about_readiness_leaves_it_as_it_was()
        {
            var gravity = await GivenTheAdminChoseTwoYesFromTwoVoters();

            using var save = await WhenTheSettingsAreSavedWithoutReadiness(gravity);

            ThenTheReadinessIs(await WhenTheAdminOpensTheTeamSettings(gravity), minYes: 2, minVoters: 2, vetoThreshold: null, vetoCounts: null);
        }

        // @driving_port @real-io @us-13 @slice-13 @boundary @contract-shape:unbounded-preservation
        [Test]
        public async Task Changing_readiness_keeps_every_Work_Item_the_Team_holds()
        {
            var gravity = await GivenGravityRefinesAndNobodyHasVoted();
            var heldBefore = WorkItemsStoredFor(gravity);

            await TheAdminHasSetReadiness(gravity, minYes: 1, minVoters: 1, vetoThreshold: 2);

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
            var gravity = await GivenJonasAndMoSaidYesAndAnaSaidNoOn(ApiVersioning);

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
            var gravity = await GivenReadinessOfTwoYesFromThreeVotersAndTwoYesOn(AdvancedReporting);

            await WhenAnaVotes(gravity, AdvancedReporting, Answer.No);

            ThenTheRowIsReadyAndTheTabCounts(await WhenPriyaOpensTheRefinementTab(gravity), AdvancedReporting, readyByVotes: 1);
        }

        // @driving_port @real-io @us-13 @slice-13 @error @contract-shape:pure-function
        // A veto wins over any number of Yes votes: the doubt is what the meeting is for.
        [Test]
        [Ignore(PendingSlice13)]
        public async Task A_veto_sends_a_Work_Item_to_discussion_however_many_say_Yes()
        {
            var gravity = await GivenAVetoOfOneNoAndThreeYesAndAnasNoOn(ApiVersioning);

            var tab = await WhenPriyaOpensTheRefinementTab(gravity);

            ThenTheRowNeedsDiscussionAndIsNotCounted(tab, ApiVersioning);
        }

        // @driving_port @real-io @us-13 @slice-13 @boundary @contract-shape:pure-function
        [TestCase(VetoCounts.NoOrYesBut, NeedsDiscussion)]
        [TestCase(VetoCounts.No, Ready)]
        [Ignore(PendingSlice13)]
        public async Task A_veto_counts_Yes_but_only_when_the_Team_says_so(VetoCounts counts, string readiness)
        {
            var gravity = await GivenAVetoOfOneCounting(counts);
            await GivenJonasAndMoSaidYesAndAnaSaidYesButOn(gravity, AdvancedReporting);

            var tab = await WhenPriyaOpensTheRefinementTab(gravity);

            Assert.That(RowOf(tab, AdvancedReporting).Readiness, Is.EqualTo(readiness));
        }

        // @driving_port @real-io @us-13 @slice-13 @boundary @contract-shape:pure-function
        [Test]
        [Ignore(PendingSlice13)]
        public async Task A_veto_of_two_is_not_tripped_by_one_No()
        {
            var gravity = await GivenAVetoOfTwoNoAndThreeYesAndOneNoOn(ApiVersioning);

            var tab = await WhenPriyaOpensTheRefinementTab(gravity);

            ThenTheRowIsReadyAndTheTabCounts(tab, ApiVersioning, readyByVotes: 1);
        }

        // @driving_port @real-io @us-13 @slice-13 @contract-shape:bounded-change
        // Only the latest answer of each voter counts, for a veto too.
        [Test]
        [Ignore(PendingSlice13)]
        public async Task A_No_changed_to_Yes_lifts_the_veto()
        {
            var gravity = await GivenAVetoOfOneNoAndThreeYesAndAnasNoOn(ApiVersioning);

            await WhenAnaVotes(gravity, ApiVersioning, Answer.Yes);

            ThenTheRowIsReadyAndTheTabCounts(await WhenPriyaOpensTheRefinementTab(gravity), ApiVersioning, readyByVotes: 1);
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
