using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// Stages are optional and come only from rules. Under Settings → Refinement a Team admin may say which
    /// Work Items are Ready ("Ready when …") and which are being refined ("Being refined when …"), with the
    /// rule editor the Team already uses for blocked Work Items; whatever no rule matches is Waiting. A Team
    /// that sets no rule has no stages at all, and nothing about its tab changes: a Team with a single
    /// refinement state, or one that does not split its refinement, is as well served as one that does.
    ///
    /// The stage and the votes are two signals, shown side by side; neither overrides the other. Which one
    /// the ready count follows depends on the Team: without stage rules, the Work Items the votes say are
    /// Ready; with stage rules, the Work Items whose stage is Ready - the tracker is the source of truth. A
    /// row whose two signals disagree says so, so the Team can see where its tracker has drifted. Votes stay
    /// open on every row. Rules only ever look at Work Items already in refinement.
    ///
    /// Driving ports: the Team settings write and read, the vote write and the Refinement tab's read. Step
    /// definitions live in Slice03StageRulesSpecifications.cs.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5510-5881-refinement")]
    [Category("slice-03")]
    public partial class Slice03StageRulesTest : RefinementNeedAcceptanceTest
    {
        // --- The stage rules ---

        // @driving_port @real-io @us-03 @slice-03 @contract-shape:bounded-change
        [Test]
        public async Task A_Team_admin_sets_a_Ready_rule_and_a_Being_refined_rule_and_both_read_back()
        {
            var gravity = await GivenGravityRefinesWithoutStages();

            await TheAdminHasSetTheStageRules(gravity, ready: TagsContain(ReadyTag), beingRefined: TagsContain(AnalysingTag));

            ThenTheStageRulesAre(await ReadTheTeamSettings(gravity), ready: ReadyTag, beingRefined: AnalysingTag);
        }

        // @driving_port @real-io @us-03 @slice-03 @boundary @contract-shape:pure-function
        // Stages are optional, and a Team that uses none is told nothing new.
        [Test]
        public async Task A_Team_that_never_set_a_stage_rule_has_no_stages_at_all()
        {
            var gravity = await GivenGravityRefinesWithoutStages();

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheTabHasNoStages(tab);
        }

        // @driving_port @real-io @us-03 @us-13 @slice-03 @boundary @contract-shape:bounded-change
        [Test]
        [Ignore(PendingSlice03)]
        public async Task Without_stage_rules_the_ready_count_follows_the_votes()
        {
            var gravity = await GivenGravityRefinesWithoutStages();
            await GivenThreeVotersSaidYesOn(gravity, ConfigurationManagement);

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheReadyCountIs(tab, 1, FromVotes);
        }

        // @driving_port @real-io @us-03 @slice-03 @contract-shape:pure-function
        [Test]
        [Ignore(PendingSlice03)]
        public async Task With_stage_rules_the_ready_count_follows_the_stages()
        {
            var gravity = await GivenGravityTagsTwoWorkItemsReadyUnderItsReadyRule();

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheReadyCountIs(tab, 2, FromStages);
        }

        // @driving_port @real-io @us-03 @slice-03 @contract-shape:pure-function
        [Test]
        public async Task Every_Work_Item_the_rules_do_not_match_is_Waiting()
        {
            var gravity = await GivenGravityWithBothRules((UserActivityTracking, ReadyTag), (AdvancedReporting, AnalysingTag));

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheStagesAre(tab, (UserActivityTracking, ReadyStage), (AdvancedReporting, BeingRefined), (ConfigurationManagement, Waiting), (LoadTesting, Waiting));
        }

        // @driving_port @real-io @us-03 @slice-03 @boundary @contract-shape:pure-function
        [Test]
        public async Task When_both_rules_match_a_Work_Item_the_Ready_rule_wins()
        {
            var gravity = await GivenGravityWithBothRules((AdvancedReporting, ReadyTag), (AdvancedReporting, AnalysingTag));

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheStagesAre(tab, (AdvancedReporting, ReadyStage));
        }

        // --- Two signals side by side ---

        // @driving_port @real-io @us-03 @us-13 @slice-03 @boundary @contract-shape:bounded-change
        // With stages the tracker decides: three Yes votes say Ready, the stage still says Waiting.
        [Test]
        [Ignore(PendingSlice03)]
        public async Task With_stage_rules_votes_never_make_a_Work_Item_Ready()
        {
            var gravity = await GivenGravityTagsTwoWorkItemsReadyUnderItsReadyRule();
            await GivenThreeVotersSaidYesOn(gravity, ConfigurationManagement);

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheRowReads(tab, new StageRowReading(ConfigurationManagement, Waiting, "Ready", null, SignalsDisagree: true));
            ThenTheReadyCountIs(tab, 2, FromStages);
        }

        // @driving_port @real-io @us-03 @us-13 @slice-03 @boundary @contract-shape:bounded-change
        // One No sends a Work Item to discussion by default; its stage stays Ready, and so does the count.
        [Test]
        [Ignore(PendingSlice03)]
        public async Task With_stage_rules_votes_never_block_a_Ready_Work_Item()
        {
            var gravity = await GivenGravityTagsTwoWorkItemsReadyUnderItsReadyRule();
            await GivenJonasSaidNoOn(gravity, UserActivityTracking);

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheRowReads(tab, new StageRowReading(UserActivityTracking, ReadyStage, "NeedsDiscussion", null, SignalsDisagree: true));
            ThenTheReadyCountIs(tab, 2, FromStages);
        }

        // @driving_port @real-io @us-03 @slice-03 @boundary @contract-shape:pure-function
        // One Yes is cast and the votes still want two more, so the votes cast disagree with the Ready stage.
        [Test]
        [Ignore(PendingSlice03)]
        public async Task A_Ready_stage_the_votes_cast_do_not_back_yet_is_marked_as_disagreeing()
        {
            var gravity = await GivenGravityTagsTwoWorkItemsReadyUnderItsReadyRule();
            await GivenJonasSaidYesOn(gravity, UserActivityTracking);

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheRowReads(tab, new StageRowReading(UserActivityTracking, ReadyStage, "MoreYesNeeded", 2, SignalsDisagree: true));
        }

        // @driving_port @real-io @us-03 @slice-03 @boundary @contract-shape:pure-function
        // Nobody has voted, so the votes hold no opinion yet and there is nothing to disagree with.
        [Test]
        [Ignore(PendingSlice03)]
        public async Task A_Ready_stage_nobody_has_voted_on_shows_no_disagreement()
        {
            var gravity = await GivenGravityTagsTwoWorkItemsReadyUnderItsReadyRule();

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheRowReads(tab, new StageRowReading(UserActivityTracking, ReadyStage, "MoreYesNeeded", 3, SignalsDisagree: false));
            ThenTheReadyCountIs(tab, 2, FromStages);
        }

        // @driving_port @real-io @us-03 @slice-03 @boundary @contract-shape:bounded-change
        [Test]
        [Ignore(PendingSlice03)]
        public async Task A_Ready_stage_the_votes_also_call_Ready_shows_no_disagreement()
        {
            var gravity = await GivenGravityTagsTwoWorkItemsReadyUnderItsReadyRule();
            await GivenThreeVotersSaidYesOn(gravity, UserActivityTracking);

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheRowReads(tab, new StageRowReading(UserActivityTracking, ReadyStage, "Ready", null, SignalsDisagree: false));
        }

        // @driving_port @real-io @us-03 @slice-03 @boundary @contract-shape:pure-function
        [Test]
        [Ignore(PendingSlice03)]
        public async Task A_Waiting_Work_Item_the_votes_do_not_call_Ready_shows_no_disagreement()
        {
            var gravity = await GivenGravityTagsTwoWorkItemsReadyUnderItsReadyRule();

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheRowReads(tab, new StageRowReading(ConfigurationManagement, Waiting, "MoreYesNeeded", 3, SignalsDisagree: false));
        }

        // @driving_port @real-io @us-03 @us-11 @slice-03 @boundary @contract-shape:bounded-change
        [Test]
        [Ignore(PendingSlice03)]
        public async Task Votes_stay_open_on_a_Work_Item_whose_stage_is_Ready()
        {
            var gravity = await GivenGravityTagsTwoWorkItemsReadyUnderItsReadyRule();

            await GivenJonasSaidNoOn(gravity, UserActivityTracking);

            await ThenTheRowCarriesOneVoteAndItsReadyStage(gravity, UserActivityTracking);
        }

        // @driving_port @real-io @us-03 @slice-03 @error @contract-shape:bounded-change
        // Stage rules that make nothing Ready leave nothing Ready, however the votes went.
        [Test]
        [Ignore(PendingSlice03)]
        public async Task Stage_rules_that_match_nothing_Ready_leave_the_ready_count_at_zero()
        {
            var gravity = await GivenGravityWithItsReadyRuleTagging();
            await GivenThreeVotersSaidYesOn(gravity, ConfigurationManagement);

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheReadyCountIs(tab, 0, FromStages);
        }

        // @driving_port @real-io @us-03 @slice-03 @error @contract-shape:pure-function
        // A Team may split its refinement into Waiting and Being refined only; then nothing is Ready by stage.
        [Test]
        [Ignore(PendingSlice03)]
        public async Task A_Being_refined_rule_alone_still_counts_by_stage()
        {
            var gravity = await GivenGravityWithOnlyABeingRefinedRuleTagging((AdvancedReporting, AnalysingTag));
            await GivenThreeVotersSaidYesOn(gravity, ConfigurationManagement);

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheReadyCountIs(tab, 0, FromStages);
        }

        // @driving_port @real-io @us-03 @slice-03 @boundary @contract-shape:pure-function
        // A rule never pulls a Work Item into refinement from a state the Team does not refine in.
        [Test]
        public async Task Rules_only_ever_judge_Work_Items_already_in_refinement()
        {
            var gravity = await GivenGravityWithItsReadyRuleTagging((BillingExport, ReadyTag), (UserActivityTracking, ReadyTag));

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheWorkItemIsNotListed(tab, BillingExport);
            ThenTheReadyCountIs(tab, 1, FromStages);
        }

        // @driving_port @real-io @us-03 @slice-03 @contract-shape:pure-function
        // Team Pulsar refines in one state and marks ready Work Items with a tag.
        [Test]
        public async Task A_Team_with_a_single_refinement_state_splits_it_into_stages_by_rule()
        {
            var pulsar = await GivenPulsarRefinesInOneStateWithFourOfSixTaggedReady();

            var tab = await WhenTheCoachOpensTheRefinementTab(pulsar);

            ThenTheReadyCountIs(tab, 4, FromStages);
        }

        // --- Refusals and what a save leaves alone ---

        // @driving_port @real-io @us-03 @slice-03 @error @contract-shape:unbounded-preservation
        [Test]
        public async Task A_rule_on_a_field_Work_Items_do_not_have_is_refused_and_nothing_is_saved()
        {
            var gravity = await GivenGravityWithItsReadyRuleTagging((UserActivityTracking, ReadyTag));

            using var refused = await WhenTheAdminSavesTheReadyRule(gravity, ARuleOn("workitem.storypoints", "3"));

            await ThenTheSaveIsRefusedAndTheReadyRuleIsStill(refused, gravity, ReadyTag);
        }

        // @driving_port @real-io @us-03 @slice-03 @error @contract-shape:unbounded-preservation
        // The same limit the blocked-items rule editor has: twenty conditions.
        [Test]
        public async Task A_rule_with_more_conditions_than_the_rule_editor_allows_is_refused_and_nothing_is_saved()
        {
            var gravity = await GivenGravityWithItsReadyRuleTagging((UserActivityTracking, ReadyTag));

            using var refused = await WhenTheAdminSavesTheReadyRule(gravity, ARuleWithConditions(21));

            await ThenTheSaveIsRefusedAndTheReadyRuleIsStill(refused, gravity, ReadyTag);
        }

        // @driving_port @real-io @us-03 @slice-03 @boundary @contract-shape:bounded-change
        [Test]
        public async Task A_rule_of_twenty_conditions_is_accepted()
        {
            var gravity = await GivenGravityRefinesWithoutStages();

            using var save = await WhenTheAdminSavesTheReadyRule(gravity, ARuleWithConditions(20));

            await ThenTheSaveIsAcceptedWithAReadyRuleOn(save, gravity, "tag-1");
        }

        // @driving_port @real-io @us-03 @slice-03 @boundary @contract-shape:unbounded-preservation
        // An older settings form knows nothing about stage rules; its saves must not remove them.
        [TestCase(SaveShape.WithoutTheRefinementSection)]
        [TestCase(SaveShape.RefinementSectionWithoutTheMember)]
        public async Task A_save_that_says_nothing_about_stage_rules_keeps_them(SaveShape shape)
        {
            var gravity = await GivenGravityWithItsReadyRuleTagging((UserActivityTracking, ReadyTag));

            await WhenTheSettingsAreSaved(gravity, shape);

            ThenTheStageRulesAre(await ReadTheTeamSettings(gravity), ready: ReadyTag, beingRefined: null);
        }

        // @driving_port @real-io @us-03 @slice-03 @boundary @contract-shape:bounded-change
        [Test]
        [Ignore(PendingSlice03)]
        public async Task Turning_every_stage_rule_off_hands_the_ready_count_back_to_the_votes()
        {
            var gravity = await GivenGravityWithItsReadyRuleTagging((UserActivityTracking, ReadyTag));
            await GivenThreeVotersSaidYesOn(gravity, ConfigurationManagement);

            await TheAdminHasSetTheStageRules(gravity, ready: null, beingRefined: null);

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);
            ThenTheTabHasNoStages(tab);
            ThenTheReadyCountIs(tab, 1, FromVotes);
        }

        // @driving_port @real-io @us-03 @slice-03 @boundary @contract-shape:bounded-change
        // A stage rule is a refinement setting; it never makes the Team fetch its Work Items afresh.
        [Test]
        public async Task Setting_stage_rules_keeps_every_Work_Item_the_Team_holds()
        {
            var gravity = await GivenGravityRefinesWithoutStages();
            var before = WorkItemsStoredFor(gravity);

            await TheAdminHasSetTheStageRules(gravity, ready: TagsContain(ReadyTag), beingRefined: null);

            await ThenTheTeamStillHoldsItsWorkItemsAndTheReadyRule(gravity, before, ReadyTag);
        }
    }
}
