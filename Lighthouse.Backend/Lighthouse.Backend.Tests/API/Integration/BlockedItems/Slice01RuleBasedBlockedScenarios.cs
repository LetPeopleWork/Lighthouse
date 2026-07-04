using NUnit.Framework;
using static Lighthouse.Backend.Tests.API.Integration.BlockedItems.BlockedItemsJson;

namespace Lighthouse.Backend.Tests.API.Integration.BlockedItems
{
    /// <summary>
    /// DISTILL acceptance scenarios (Epic 5074) — Slice 01: Rule-based blocked definition (FOUNDATION).
    /// Job: job-config-admin-define-blocked-rules. Persona: config-admin (Carlos). Driving ports: team
    /// settings PUT + team settings GET + team metrics WIP read (WorkItemDto.isBlocked).
    ///
    /// One scenario is the walking skeleton (@walking_skeleton, GREEN today via the existing blocked
    /// round-trip). Every other scenario is [Ignore]-pending: enable one at a time in DELIVER, drive it
    /// RED, implement, commit. See distill/red-classification.md for the per-scenario RED reason.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5074-blocked-items")]
    [Category("slice-01")]
    public partial class Slice01RuleBasedBlockedTest
    {
        // @walking_skeleton @driving_port @real-io @us-01
        [Test]
        [Category("walking_skeleton")]
        public async Task An_admin_saves_a_teams_blocked_definition_and_reads_it_back()
        {
            var team = GivenATeamReadyForConfiguration();

            var save = await WhenTheAdminDefinesBlockedByState(team, "Blocked");
            ThenTheDefinitionIsSaved(save);

            var settings = await WhenTheBlockedConfigurationIsRead(team);
            ThenTheBlockedDefinitionIncludes(settings, "Blocked");
        }

        // @driving_port @us-01 @migration
        [Test]
        public async Task Existing_blocked_config_is_preserved_as_equivalent_rules()
        {
            var team = GivenATeamWhoseBlockedConfigIs(states: ["Blocked"], tags: ["impediment"]);

            var settings = await WhenTheBlockedConfigurationIsRead(team);

            ThenTheMigratedRuleSetExpresses(settings, "workitem.state equals Blocked", "workitem.tags contains impediment");
        }

        // @driving_port @us-01
        [Test]
        public async Task A_custom_field_condition_makes_an_item_read_blocked()
        {
            var team = GivenATeamWithAFlaggedFieldAndOneFlaggedItem("PHX-204");

            var save = await WhenTheAdminSavesTheBlockedRuleSet(team, OrRuleSet(FieldIsNotEmpty(team.FlaggedFieldId)));
            ThenTheDefinitionIsSaved(save);

            await ThenTheItemReadsBlocked(team, "PHX-204");
        }

        // @driving_port @us-01 read-your-writes
        [Test]
        public async Task Saved_blocked_rules_persist_across_reload()
        {
            var team = GivenATeamReadyForConfiguration();

            var save = await WhenTheAdminSavesTheBlockedRuleSet(team, OrRuleSet(StateEquals("Blocked"), TagsContains("impediment")));
            ThenTheDefinitionIsSaved(save);

            var settings = await WhenTheBlockedConfigurationIsRead(team);
            ThenTheMigratedRuleSetExpresses(settings, "workitem.state equals Blocked", "workitem.tags contains impediment");
        }

        // @driving_port @us-01 @property (single definition drives every blocked signal)
        [Test]
        [Category("property")]
        public async Task An_item_matching_the_blocked_rules_reads_blocked_everywhere()
        {
            var team = GivenATeamReadyForConfiguration();
            GivenAnItemInState(team, "PHX-9", state: "On Hold");

            var save = await WhenTheAdminSavesTheBlockedRuleSet(team, OrRuleSet(StateEquals("On Hold")));
            ThenTheDefinitionIsSaved(save);

            await ThenTheItemReadsBlocked(team, "PHX-9");
        }

        // @edge @us-01 (empty rule set blocks nothing)
        [Test]
        public async Task A_team_with_no_blocked_config_blocks_nothing()
        {
            var team = GivenATeamReadyForConfiguration();
            GivenAnItemInState(team, "ATL-1", state: "In Progress");

            var settings = await WhenTheBlockedConfigurationIsRead(team);
            ThenTheMigratedRuleSetIsEmpty(settings);

            await ThenTheItemDoesNotReadBlocked(team, "ATL-1");
        }

        // @error @us-01 (schema limit)
        [Test]
        [Ignore("DISTILL pending — enable in DELIVER slice-01 (MaxRules validation on blocked rule set)")]
        public async Task A_blocked_rule_set_exceeding_the_maximum_conditions_is_rejected()
        {
            var team = GivenATeamReadyForConfiguration();

            var tooMany = Enumerable.Range(0, 21).Select(i => StateEquals($"State{i}")).ToArray();
            var save = await WhenTheAdminSavesTheBlockedRuleSet(team, OrRuleSet(tooMany));

            ThenTheSaveIsRejected(save);
        }

        // @error @us-01 (unknown field key)
        [Test]
        [Ignore("DISTILL pending — enable in DELIVER slice-01 (unknown-field validation on blocked rule set)")]
        public async Task A_blocked_rule_referencing_an_unknown_field_is_rejected()
        {
            var team = GivenATeamReadyForConfiguration();

            var save = await WhenTheAdminSavesTheBlockedRuleSet(team, OrRuleSet(UnknownField()));

            ThenTheSaveIsRejected(save);
        }

        // @error @us-01 @rbac (config-admin gate on the extended contract; passes today via existing guard)
        [Test]
        [Ignore("DISTILL pending — enable in DELIVER slice-01 (RBAC gate on blocked-rule write; GREEN-when-enabled, pre-existing TeamWrite guard)")]
        public async Task A_non_admin_cannot_change_the_blocked_definition()
        {
            var team = GivenATeamReadyForConfiguration();

            var save = await WhenANonAdminTriesToSaveTheBlockedRuleSet(team, OrRuleSet(StateEquals("Blocked")));

            ThenTheSaveIsForbidden(save);
        }
    }
}
