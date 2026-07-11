using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using NUnit.Framework;
using static Lighthouse.Backend.Tests.API.Integration.BlockedItems.BlockedItemsJson;

namespace Lighthouse.Backend.Tests.API.Integration.BlockedItems
{
    /// <summary>
    /// DISTILL acceptance scenarios (Epic 5074) — Slice 05: Jira flagged via a predefined (system-owned)
    /// additional field. Job: job-config-admin-define-blocked-rules. Persona: config-admin (Carlos).
    /// Design authority: ADR-071 (amended 2026-07-11 — SPIKE WAIVED; auto-registration promoted to the
    /// IWorkTrackingConnector.GetPredefinedAdditionalFields port method). The five waived-SPIKE questions
    /// (reconcile merge-back, slot split, write-back compatibility + Reference immutability, single idempotent
    /// registration, FE DTO split) are pinned here as concrete tests.
    ///
    /// Driving ports: work-tracking-system-connection settings GET/PUT + team settings PUT + team metrics WIP
    /// read (WorkItemDto.isBlocked). One scenario is the walking skeleton (@walking_skeleton, GREEN today via
    /// the existing connection additional-field round-trip). Every other scenario is [Ignore]-pending: enable
    /// one at a time in DELIVER, drive it RED, implement, commit. See distill/red-classification.md for the
    /// per-scenario RED reason. The isPredefined DTO split (FE) and the synthetic-label removal (IssueFactory)
    /// are covered by the sibling Vitest spec and Slice05SyntheticLabelRemovalTests respectively.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5074-blocked-items")]
    [Category("slice-05")]
    public partial class Slice05PredefinedFieldTest
    {
        // @walking_skeleton @driving_port @real-io @us-05
        // GREEN today: proves the connection additional-field read/write round-trip wiring slice-05 extends.
        [Test]
        [Category("walking_skeleton")]
        public async Task A_connection_round_trips_its_additional_field_configuration()
        {
            var connectionId = GivenAJiraConnection(("Team", "customfield_10050"));

            var body = await WhenTheConnectionConfigurationIsRead(connectionId);
            var save = await WhenTheAdminSavesTheConnection(connectionId, AsConnectionPayload(body));
            ThenTheSaveSucceeds(save);

            var reread = await WhenTheConnectionConfigurationIsRead(connectionId);
            ThenTheServedConnectionStillCarriesTheUserField(reread, "customfield_10050");
        }

        // @driving_port @us-05 (AC1 — a flagged item reads blocked, with no synthetic "Flagged" label)
        [Test]
        public async Task A_flagged_item_reads_blocked_through_the_flagged_field_without_a_synthetic_label()
        {
            var team = GivenAJiraTeamWithAFlaggedFieldAndOneFlaggedItem("PHX-300");

            var save = await WhenTheAdminSavesTheBlockedRuleSet(team, OrRuleSet(FieldIsNotEmpty(team.FlaggedFieldId)));
            ThenTheSaveSucceeds(save);

            await ThenTheItemReadsBlockedWithoutASyntheticFlaggedLabel(team, "PHX-300");
        }

        // @driving_port @property @us-05 (AC4 — the flagged value flows through the generic id-keyed path
        // whether set or not; offered as a selectable rule field key)
        [TestCase(true)]
        [TestCase(false)]
        public async Task The_flagged_field_value_drives_blocked_through_the_generic_field_path(bool flagged)
        {
            var team = GivenAJiraTeamWithAFlaggedField();
            GivenAFlaggedItem(team, "PHX-301", flagged);

            var save = await WhenTheAdminSavesTheBlockedRuleSet(team, OrRuleSet(FieldIsNotEmpty(team.FlaggedFieldId)));
            ThenTheSaveSucceeds(save);

            await ThenTheItemBlockedStatusIs(team, "PHX-301", flagged);
        }

        // @error @edge @us-05 (AC2 — reconcile merge-back: a settings save that omits the predefined field
        // must NOT delete it; today UpdateAdditionalFieldDefinitions removes anything not in the incoming set)
        [Test]
        public async Task A_settings_save_that_omits_the_predefined_field_preserves_it()
        {
            var connectionId = GivenAJiraConnection(("Team", "customfield_10050"));

            var body = await WhenTheConnectionConfigurationIsRead(connectionId);
            var save = await WhenTheAdminSavesTheConnection(connectionId, WithoutPredefinedFields(AsConnectionPayload(body)));
            ThenTheSaveSucceeds(save);

            var reread = await WhenTheConnectionConfigurationIsRead(connectionId);
            ThenAPredefinedFlaggedFieldIsStillSurfaced(reread);
        }

        // @edge @us-05 (AC2 — slot split: a predefined field does not consume a user field slot;
        // SupportsAdditionalFields must count where !IsPredefined)
        [Test]
        public async Task A_predefined_field_does_not_consume_a_user_field_slot_on_a_non_premium_connection()
        {
            GivenTheConnectionIsOnANonPremiumPlan();
            var connectionId = GivenAJiraConnection();

            var body = await WhenTheConnectionConfigurationIsRead(connectionId);
            ThenExactlyOnePredefinedFlaggedFieldIsSurfaced(body);

            var save = await WhenTheAdminSavesTheConnection(connectionId, WithAnAddedUserField(AsConnectionPayload(body), "Team", "customfield_10050"));

            ThenTheSaveSucceeds(save);
        }

        // @property @us-05 (Port seam — only a Jira connection contributes the predefined flagged field;
        // ADO/Linear/Csv contribute none. Observed via the served connection since GetPredefinedAdditionalFields
        // does not exist on today's IWorkTrackingConnector.)
        [TestCase(WorkTrackingSystems.Jira, true)]
        [TestCase(WorkTrackingSystems.AzureDevOps, false)]
        [TestCase(WorkTrackingSystems.Linear, false)]
        [TestCase(WorkTrackingSystems.Csv, false)]
        public async Task Only_a_jira_connection_contributes_a_predefined_flagged_field(WorkTrackingSystems system, bool expectsPredefinedField)
        {
            var connectionId = SeedConnection(system);

            var body = await WhenTheConnectionConfigurationIsRead(connectionId);

            if (expectsPredefinedField)
            {
                ThenExactlyOnePredefinedFlaggedFieldIsSurfaced(body);
            }
            else
            {
                ThenNoPredefinedFieldIsSurfaced(body);
            }
        }

        // @edge @us-05 (Auto-registration idempotency — re-reading a Jira connection surfaces exactly one
        // predefined field, never a duplicate and never a =true sentinel)
        [Test]
        public async Task A_jira_connection_surfaces_exactly_one_predefined_field_stably()
        {
            var connectionId = GivenAJiraConnection();

            var first = await WhenTheConnectionConfigurationIsRead(connectionId);
            ThenExactlyOnePredefinedFlaggedFieldIsSurfaced(first);

            var second = await WhenTheConnectionConfigurationIsRead(connectionId);
            ThenExactlyOnePredefinedFlaggedFieldIsSurfaced(second);
        }

        // @error @us-05 (Write-back exclusion + Reference immutability — a predefined field is inbound-only:
        // its Reference cannot be changed by a settings save, and it is never persisted as a write-back target)
        [Test]
        public async Task A_predefined_field_is_inbound_only()
        {
            var connectionId = GivenAJiraConnection();

            var body = await WhenTheConnectionConfigurationIsRead(connectionId);
            ThenExactlyOnePredefinedFlaggedFieldIsSurfaced(body);
            var originalReference = PredefinedFields(body)[0]["reference"]!.GetValue<string>();

            var mutated = WithAWriteBackMappingTargetingTheFirstPredefinedField(
                WithAChangedReferenceOnEveryPredefinedField(AsConnectionPayload(body), "customfield_tampered"));
            var save = await WhenTheAdminSavesTheConnection(connectionId, mutated);
            ThenTheSaveSucceeds(save);

            var reread = await WhenTheConnectionConfigurationIsRead(connectionId);
            ThenTheConnectionsPredefinedFieldReferenceIs(reread, originalReference);
            ThenNoWriteBackMappingTargetsAPredefinedField(reread);
        }
    }
}
