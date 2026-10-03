using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// Anyone who can read a Team opens its Refinement tab and sees every Work Item that sits in one of
    /// the Team's refinement states, in backlog order: the tracker's own rank, compared the way Feature
    /// order is compared, ties broken by id. Each row says what the Work Item is, where it lives in the
    /// tracker, its state and category; a Doing row also says how old it is, a To Do row does not. An
    /// empty refinement and a Team nobody set up are both stated, never answered as an error.
    ///
    /// Every scenario starts where slice 01 ends: the admin has chosen the refinement states through the
    /// Team settings write. Driving port: the Refinement tab's read. Step definitions live in
    /// Slice02RefinementListSpecifications.cs.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5510-5881-refinement")]
    [Category("slice-02")]
    public partial class Slice02RefinementListTest : RefinementAcceptanceTest
    {
        // @driving_port @real-io @us-02 @slice-02 @kpi-OUT-5510-K2-refinement-tab-weekly @contract-shape:pure-function
        [Test]
        public async Task The_coach_sees_every_Work_Item_in_refinement_in_backlog_order()
        {
            var gravity = await GivenGravityRefinesInBacklogAnalysingAndNextAndItsTrackerHoldsWorkInEveryState();
            TheCallerOnlyReadsTheTeam(gravity);

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheRowsAre(tab, "GR-058", "GR-059", "GR-051", "GR-073");
        }

        // @driving_port @real-io @us-02 @slice-02 @contract-shape:pure-function
        [Test]
        [Ignore(PendingSlice02)]
        public async Task Each_row_names_the_Work_Item_links_to_the_tracker_and_gives_its_state_and_category()
        {
            var gravity = await GivenGravityRefinesInBacklogAnalysingAndNextAndItsTrackerHoldsWorkInEveryState();
            TheCallerOnlyReadsTheTeam(gravity);

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            using (Assert.EnterMultipleScope())
            {
                ThenTheRowReads(tab, "GR-051", "Advanced reporting module", Analysing, DoingCategory);
                ThenTheRowReads(tab, "GR-073", "Configuration management", Backlog, ToDoCategory);
            }
        }

        // @driving_port @real-io @us-02 @slice-02 @boundary @contract-shape:pure-function
        // Work Item Age is defined for started work only. A To Do row says nothing about age rather than
        // printing a zero that reads as "brand new". Age counts the day work started, so GR-051, started
        // three days ago, is four days old.
        [Test]
        [Ignore(PendingSlice02)]
        public async Task A_Doing_row_carries_its_Work_Item_Age_and_a_To_Do_row_carries_none()
        {
            var gravity = await GivenGravityRefinesInBacklogAnalysingAndNextAndItsTrackerHoldsWorkInEveryState();
            TheCallerOnlyReadsTheTeam(gravity);

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            using (Assert.EnterMultipleScope())
            {
                ThenTheRowIsThisManyDaysOld(tab, "GR-051", 4);
                ThenTheRowCarriesNoAge(tab, "GR-073");
            }
        }

        // @driving_port @real-io @us-02 @slice-02 @boundary @contract-shape:pure-function
        [Test]
        [Ignore(PendingSlice02)]
        public async Task Work_Items_the_tracker_ranks_equally_are_listed_by_id()
        {
            var gravity = await GivenGravityRefinesInBacklogWithTwoWorkItemsRankedEqually();
            TheCallerOnlyReadsTheTeam(gravity);

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheRowsAre(tab, "GR-074", "GR-075");
        }

        // @driving_port @real-io @us-02 @slice-02 @boundary @contract-shape:pure-function
        // A tracker that ranks 9 above 10 must not see 10 first because "1" sorts before "9" as text.
        [Test]
        [Ignore(PendingSlice02)]
        public async Task Numeric_ranks_are_compared_as_numbers_not_as_text()
        {
            var gravity = await GivenGravityRefinesInBacklogWithWorkRankedNineAndTen();
            TheCallerOnlyReadsTheTeam(gravity);

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheRowsAre(tab, "GR-091", "GR-090");
        }

        // @driving_port @real-io @us-02 @slice-02 @contract-shape:pure-function
        // A Team that maps Analysing and Grooming together holds their Work Items under the mapping's name.
        [Test]
        [Ignore(PendingSlice02)]
        public async Task A_state_chosen_by_its_mapped_name_lists_the_Work_Items_held_under_that_name()
        {
            var orbit = await GivenOrbitRefinesInItsRefiningMappingAndHoldsWorkInBothMappedStates();
            TheCallerOnlyReadsTheTeam(orbit);

            var tab = await WhenTheCoachOpensTheRefinementTab(orbit);

            ThenTheRowsAre(tab, "OR-002", "OR-001");
        }

        // @driving_port @real-io @us-02 @slice-02 @error @contract-shape:pure-function
        [Test]
        public async Task An_empty_refinement_is_stated_not_answered_as_an_error()
        {
            var zenith = await GivenZenithRefinesInBacklogButHoldsNothingThere();
            TheCallerOnlyReadsTheTeam(zenith);

            var tab = await WhenTheCoachOpensTheRefinementTab(zenith);

            using (Assert.EnterMultipleScope())
            {
                ThenTheTabSaysTheTeamHasRefinementStates(tab, true);
                ThenNoWorkItemIsListed(tab);
            }
        }

        // @driving_port @real-io @us-02 @slice-02 @error @contract-shape:pure-function
        // The tab is disabled for such a Team, but an old link or a client can still ask.
        [Test]
        public async Task A_Team_nobody_set_up_answers_that_it_has_no_refinement_states_and_lists_nothing()
        {
            var zenith = GivenZenithWithNoRefinementStatesHoldingBacklogWork();
            TheCallerOnlyReadsTheTeam(zenith);

            var tab = await WhenTheCoachOpensTheRefinementTab(zenith);

            using (Assert.EnterMultipleScope())
            {
                ThenTheTabSaysTheTeamHasRefinementStates(tab, false);
                ThenNoWorkItemIsListed(tab);
            }
        }

        // @driving_port @real-io @us-02 @slice-02 @boundary @contract-shape:pure-function
        [Test]
        [Ignore(PendingSlice02)]
        public async Task Another_Teams_Work_Items_in_the_same_state_are_not_listed()
        {
            var gravity = await GivenGravityAndPulsarBothHoldWorkInBacklogAndOnlyGravityRefinesThere();
            TheCallerOnlyReadsTheTeam(gravity);

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheRowsAre(tab, "GR-073");
        }

        // @driving_port @real-io @us-01 @us-02 @slice-02 @error @contract-shape:pure-function
        // The section warns "its Work Items cannot appear"; this is the list keeping that promise.
        [Test]
        [Ignore(PendingSlice02)]
        public async Task Work_Items_in_a_chosen_state_that_is_no_longer_mapped_are_not_listed()
        {
            var gravity = await GivenGravitysChosenAnalysingStoppedBeingMappedWhileWorkSitsThere();
            TheCallerOnlyReadsTheTeam(gravity);

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheRowsAre(tab, "GR-073");
        }

        // @driving_port @real-io @us-02 @slice-02 @error @contract-shape:unbounded-preservation
        // Not even whether the Team exists is given away to somebody who cannot read it.
        [Test]
        public async Task Somebody_without_a_role_on_the_Team_is_told_the_tab_does_not_exist()
        {
            var gravity = await GivenGravityRefinesInBacklogAnalysingAndNextAndItsTrackerHoldsWorkInEveryState();
            await GivenAReaderOfTheTeamCanOpenItsRefinementTab(gravity);
            TheCallerHasNoRoleOnTheTeam();

            var answer = await WhenSomebodyAsksForTheRefinementTabOf(gravity.TeamId);

            ThenTheTabIsNotFound(answer);
        }

        // @driving_port @real-io @us-02 @slice-02 @error @contract-shape:unbounded-preservation
        [Test]
        public async Task Asking_for_the_Refinement_tab_of_a_Team_that_does_not_exist_is_not_found()
        {
            var gravity = await GivenGravityRefinesInBacklogAnalysingAndNextAndItsTrackerHoldsWorkInEveryState();
            TheCallerAdministersTheWholeInstance();
            await GivenTheRefinementTabOfAnExistingTeamOpens(gravity);

            var answer = await WhenSomebodyAsksForTheRefinementTabOf(TeamThatDoesNotExist);

            ThenTheTabIsNotFound(answer);
        }

        // @driving_port @real-io @us-02 @slice-02 @contract-shape:pure-function
        // Opening the tab is a read: it stores nothing, changes nothing and answers the same twice.
        [Test]
        public async Task Opening_the_tab_changes_nothing_about_the_Team_or_its_Work_Items()
        {
            var gravity = await GivenGravityRefinesInBacklogAnalysingAndNextAndItsTrackerHoldsWorkInEveryState();
            TheCallerAdministersTheTeam(gravity);
            var settingsBefore = await ReadTheTeamSettings(gravity);
            var heldBefore = WorkItemsStoredFor(gravity);

            var first = await WhenTheCoachOpensTheRefinementTab(gravity);
            var second = await WhenTheCoachOpensTheRefinementTab(gravity);

            var settingsAfter = await ReadTheTeamSettings(gravity);
            using (Assert.EnterMultipleScope())
            {
                ThenBothAnswersListTheSameRows(first, second);
                ThenTheSettingsAreUnchanged(settingsBefore, settingsAfter);
                ThenTheTeamStillHolds(gravity, heldBefore);
            }
        }

        // @driving_port @real-io @us-02 @slice-02 @boundary @kpi-OUT-5510-K2-refinement-tab-weekly @contract-shape:pure-function
        // The guardrail is a tab that renders within two seconds at 300 Work Items; the answer it renders
        // from must leave room for the rendering, so it is held to the same budget on its own.
        [Test]
        [Ignore(PendingSlice02)]
        public async Task Three_hundred_Work_Items_in_refinement_come_back_in_backlog_order_within_two_seconds()
        {
            var gravity = await GivenGravityHoldsThreeHundredWorkItemsInItsRefinementStates();
            TheCallerOnlyReadsTheTeam(gravity);
            await WhenTheCoachOpensTheRefinementTab(gravity);

            var (tab, elapsed) = await WhenTheCoachOpensTheRefinementTabAgainAndItIsTimed(gravity);

            using (Assert.EnterMultipleScope())
            {
                ThenThreeHundredRowsAreListedInRankOrder(tab);
                ThenTheAnswerTookLessThanTwoSeconds(elapsed);
            }
        }
    }
}
