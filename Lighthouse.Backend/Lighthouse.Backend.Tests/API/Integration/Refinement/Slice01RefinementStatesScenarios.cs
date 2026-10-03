using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// A Team admin names which of the Team's mapped states mean refinement, and from then on the Team says
    /// it has refinement states, which is what turns its Refinement tab on for every reader. Only To Do and
    /// Doing states can be chosen. A chosen state that a save takes out of To Do and Doing leaves the
    /// refinement states in that same save; otherwise a save that says nothing about refinement leaves the
    /// choice alone. Saving only the refinement section never makes the Team throw away the Work Items it
    /// already holds.
    ///
    /// Driving ports: the Team settings write and read, the Team read, and start-up seeding for the word
    /// "Refinement". Step definitions live in Slice01RefinementStatesSpecifications.cs.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5510-5881-refinement")]
    [Category("slice-01")]
    public partial class Slice01RefinementStatesTest : RefinementAcceptanceTest
    {
        // @driving_port @real-io @us-01 @slice-01 @kpi-OUT-5510-K1-refinement-set-up @contract-shape:bounded-change
        [Test]
        public async Task A_Team_admin_names_the_refinement_states_and_the_Team_says_it_has_them()
        {
            var gravity = GivenTeamGravityWithNoRefinementStates();
            TheCallerAdministersTheTeam(gravity);

            var save = await WhenTheAdminChoosesRefinementStates(gravity, Backlog, Analysing, Next);

            await ThenTheSaveIsAccepted(save);
            var settings = await ReadTheTeamSettings(gravity);
            var team = await ReadTheTeam(gravity);
            using (Assert.EnterMultipleScope())
            {
                ThenTheChosenStatesAre(settings, Backlog, Analysing, Next);
                ThenTheTeamSaysItHasRefinementStates(team);
            }
        }

        // @driving_port @real-io @us-01 @slice-01 @boundary @contract-shape:pure-function
        [Test]
        public async Task A_Team_nobody_has_set_up_says_it_has_no_refinement_states()
        {
            var gravity = GivenTeamGravityWithNoRefinementStates();
            TheCallerOnlyReadsTheTeam(gravity);

            var team = await ReadTheTeam(gravity);
            var settings = await ReadTheTeamSettings(gravity);

            using (Assert.EnterMultipleScope())
            {
                ThenTheTeamSaysItHasNoRefinementStates(team);
                ThenNoStateIsChosen(settings);
            }
        }

        // @driving_port @real-io @us-01 @slice-01 @contract-shape:pure-function
        [Test]
        public async Task Every_reader_of_the_Team_learns_that_it_has_refinement_states()
        {
            var gravity = await GivenGravitysAdminHasChosen(Backlog, Analysing, Next);

            TheCallerOnlyReadsTheTeam(gravity);
            var team = await ReadTheTeam(gravity);

            ThenTheTeamSaysItHasRefinementStates(team);
        }

        // @driving_port @real-io @us-01 @slice-01 @contract-shape:bounded-change
        [Test]
        public async Task A_state_the_Team_maps_under_a_name_of_its_own_can_be_chosen_by_that_name()
        {
            var team = GivenATeamThatMapsAnalysingAndGroomingAsRefining();
            TheCallerAdministersTheTeam(team);

            var save = await WhenTheAdminChoosesRefinementStates(team, Refining);

            await ThenTheSaveIsAccepted(save);
            ThenTheChosenStatesAre(await ReadTheTeamSettings(team), Refining);
        }

        // @driving_port @real-io @us-01 @slice-01 @boundary @contract-shape:bounded-change
        // States match whatever their case, so the same state sent twice in different case is one choice.
        [Test]
        public async Task A_state_chosen_twice_in_different_case_is_kept_once()
        {
            var gravity = GivenTeamGravityWithNoRefinementStates();
            TheCallerAdministersTheTeam(gravity);

            var save = await WhenTheAdminChoosesRefinementStates(gravity, Backlog, "backlog", Next);

            await ThenTheSaveIsAccepted(save);
            ThenTheChosenStatesAre(await ReadTheTeamSettings(gravity), Backlog, Next);
        }

        // @driving_port @real-io @us-01 @slice-01 @error @contract-shape:unbounded-preservation
        // A Done state is finished work and an unmapped state never reaches the Team, so neither can mean refinement.
        [TestCase(Done)]
        [TestCase(Icebox)]
        public async Task A_state_that_is_neither_To_Do_nor_Doing_is_refused_and_nothing_is_saved(string notARefinementCandidate)
        {
            var gravity = await GivenGravitysAdminHasChosen(Backlog);

            var save = await WhenTheAdminChoosesRefinementStates(gravity, Backlog, notARefinementCandidate);

            await ThenTheSaveIsRefusedNaming(save, notARefinementCandidate);
            ThenTheChosenStatesAre(await ReadTheTeamSettings(gravity), Backlog);
        }

        // @driving_port @real-io @us-01 @slice-01 @error @contract-shape:unbounded-preservation
        [Test]
        public async Task Only_a_Team_admin_can_change_the_refinement_states()
        {
            var gravity = await GivenGravitysAdminHasChosen(Backlog);

            TheCallerOnlyReadsTheTeam(gravity);
            var save = await WhenTheReaderTriesToChooseRefinementStates(gravity, Next);

            TheCallerAdministersTheTeam(gravity);
            var settings = await ReadTheTeamSettings(gravity);
            using (Assert.EnterMultipleScope())
            {
                ThenTheChangeIsForbidden(save);
                ThenTheChosenStatesAre(settings, Backlog);
            }
        }

        // @driving_port @real-io @us-01 @slice-01 @boundary @contract-shape:unbounded-preservation
        // An older client, or any form that does not know the section, must not wipe what the admin chose.
        [Test]
        public async Task A_save_that_says_nothing_about_refinement_leaves_the_chosen_states_as_they_were()
        {
            var gravity = await GivenGravitysAdminHasChosen(Backlog, Analysing, Next);

            var save = await WhenTheTeamSettingsAreSavedWithoutTheRefinementSection(gravity);

            await ThenTheSaveIsAccepted(save);
            ThenTheChosenStatesAre(await ReadTheTeamSettings(gravity), Backlog, Analysing, Next);
        }

        // @driving_port @real-io @us-01 @slice-01 @boundary @contract-shape:bounded-change
        [Test]
        public async Task Clearing_every_refinement_state_turns_the_Team_back_to_having_none()
        {
            var gravity = await GivenGravitysAdminHasChosen(Backlog, Analysing);

            var save = await WhenTheAdminChoosesRefinementStates(gravity);

            await ThenTheSaveIsAccepted(save);
            var settings = await ReadTheTeamSettings(gravity);
            var team = await ReadTheTeam(gravity);
            using (Assert.EnterMultipleScope())
            {
                ThenNoStateIsChosen(settings);
                ThenTheTeamSaysItHasNoRefinementStates(team);
            }
        }

        // @driving_port @real-io @us-01 @slice-01 @contract-shape:bounded-change
        // Only an admin editing To Do, Doing or the mappings can take a state out of those lists, so the removal
        // happens in the save they make, in front of them.
        [Test]
        public async Task A_chosen_state_that_stops_being_To_Do_or_Doing_is_removed_from_the_refinement_states()
        {
            var gravity = await GivenGravitysAdminHasChosen(Backlog, Analysing, Next);

            var save = await WhenTheAdminStopsMappingAnalysing(gravity);

            await ThenTheSaveIsAccepted(save);
            var settings = await ReadTheTeamSettings(gravity);
            var team = await ReadTheTeam(gravity);
            using (Assert.EnterMultipleScope())
            {
                ThenTheChosenStatesAre(settings, Backlog, Next);
                ThenTheTeamSaysItHasRefinementStates(team);
            }
        }

        // @driving_port @real-io @us-01 @slice-01 @boundary @contract-shape:bounded-change
        [Test]
        public async Task Removing_the_only_chosen_state_from_To_Do_and_Doing_turns_the_Team_back_to_having_none()
        {
            var gravity = await GivenGravitysAdminHasChosen(Analysing);

            var save = await WhenTheAdminStopsMappingAnalysing(gravity);

            await ThenTheSaveIsAccepted(save);
            var settings = await ReadTheTeamSettings(gravity);
            var team = await ReadTheTeam(gravity);
            using (Assert.EnterMultipleScope())
            {
                ThenNoStateIsChosen(settings);
                ThenTheTeamSaysItHasNoRefinementStates(team);
            }
        }

        // @driving_port @real-io @us-01 @slice-01 @error @contract-shape:bounded-change
        // A form opened before the change still sends the state it showed as chosen. Refusing that would block
        // the very save that takes the state out of Doing.
        [Test]
        public async Task A_stale_form_that_sends_a_removed_state_again_is_accepted_and_the_state_stays_removed()
        {
            var gravity = await GivenGravitysAdminHasChosen(Backlog, Analysing, Next);

            var save = await WhenTheAdminStopsMappingAnalysingWhileStillChoosingIt(gravity);

            await ThenTheSaveIsAccepted(save);
            ThenTheChosenStatesAre(await ReadTheTeamSettings(gravity), Backlog, Next);
        }

        // @driving_port @real-io @us-01 @slice-01 @error @contract-shape:unbounded-preservation
        // A state the Team does map, but newly ticked, still has to be To Do or Doing at the moment it is chosen.
        [Test]
        public async Task A_newly_chosen_state_that_is_no_longer_mapped_is_refused()
        {
            var gravity = await GivenGravitysChosenAnalysingStoppedBeingMappedWithOnlyBacklogChosen();

            var save = await WhenTheAdminChoosesRefinementStates(gravity, Backlog, Analysing);

            await ThenTheSaveIsRefusedNaming(save, Analysing);
            ThenTheChosenStatesAre(await ReadTheTeamSettings(gravity), Backlog);
        }

        // @driving_port @real-io @us-01 @slice-01 @contract-shape:bounded-change
        // A Team's Work Items are thrown away and fetched again only when the work they describe changes;
        // naming refinement states describes nothing new about that work.
        [Test]
        public async Task Choosing_refinement_states_keeps_every_Work_Item_the_Team_already_holds()
        {
            var gravity = GivenTeamGravityHoldingWorkItemsInEveryState();
            var heldBefore = WorkItemsStoredFor(gravity);
            TheCallerAdministersTheTeam(gravity);

            var save = await WhenTheAdminChoosesRefinementStates(gravity, Backlog, Analysing, Next);

            await ThenTheSaveIsAccepted(save);
            var settings = await ReadTheTeamSettings(gravity);
            using (Assert.EnterMultipleScope())
            {
                ThenTheChosenStatesAre(settings, Backlog, Analysing, Next);
                ThenTheTeamStillHoldsItsWorkItems(gravity, heldBefore);
            }
        }

        // @driving_port @real-io @us-02 @slice-01 @contract-shape:pure-function
        // The tab, its heading and its tooltip all say the Team's own word; the word has to exist to be renamed.
        [Test]
        public async Task Refinement_is_a_word_every_instance_can_rename()
        {
            TheCallerHasNoRoleOnTheTeam();

            var terminology = await ReadTheTerminology();

            ThenTheTerminologyOffersRefinementInBothForms(terminology);
        }
    }
}
