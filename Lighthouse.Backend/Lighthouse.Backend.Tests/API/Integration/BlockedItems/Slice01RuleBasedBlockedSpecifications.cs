using System.Net;
using System.Text.Json;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Tests.TestHelpers;
using NUnit.Framework;
using static Lighthouse.Backend.Tests.API.Integration.BlockedItems.BlockedItemsJson;

namespace Lighthouse.Backend.Tests.API.Integration.BlockedItems
{
    /// <summary>
    /// DISTILL step definitions (Specifications) for Slice 01 — Rule-based blocked definition.
    /// Business-language Given/When/Then step methods; each delegates to the production driving ports
    /// on <see cref="BlockedItemsAcceptanceTest"/>. No business logic lives here (Mandate-12): the steps
    /// only arrange preconditions, invoke a port, and assert an observable outcome.
    /// </summary>
    public partial class Slice01RuleBasedBlockedTest : BlockedItemsAcceptanceTest
    {
        // --- Given ---

        private SeededTeam GivenATeamReadyForConfiguration()
            => SeedTeam();

        private SeededTeam GivenATeamWithAFlaggedFieldAndOneFlaggedItem(string flaggedItem)
        {
            var team = SeedTeam(withFlaggedAdditionalField: true);
            SeedWorkItem(
                team.TeamId,
                flaggedItem,
                state: "In Progress",
                tags: [],
                additionalFieldValues: new() { [team.FlaggedFieldId] = "Impediment" });
            return team;
        }

        private void GivenAnItemInState(SeededTeam team, string referenceId, string state, List<string>? tags = null)
            => SeedWorkItem(team.TeamId, referenceId, state, tags ?? []);

        // --- When ---

        private async Task<HttpResponseMessage> WhenTheAdminDefinesBlockedByState(SeededTeam team, params string[] blockedStates)
        {
            Client.AsTeamAdmin(team.TeamId);
            var ruleSetJson = OrRuleSet([.. blockedStates.Select(StateEquals)]);
            var payload = WithBlockedRuleSet(ToJsonObject(BuildTeamSettings(team)), ruleSetJson);
            return await PutTeamSettings(team.TeamId, payload);
        }

        private async Task<HttpResponseMessage> WhenTheAdminSavesTheBlockedRuleSet(SeededTeam team, string ruleSetJson)
        {
            Client.AsTeamAdmin(team.TeamId);
            var payload = WithBlockedRuleSet(ToJsonObject(BuildTeamSettings(team)), ruleSetJson);
            return await PutTeamSettings(team.TeamId, payload);
        }

        private async Task<HttpResponseMessage> WhenANonAdminTriesToSaveTheBlockedRuleSet(SeededTeam team, string ruleSetJson)
        {
            Client.AsViewer();
            var payload = WithBlockedRuleSet(ToJsonObject(BuildTeamSettings(team)), ruleSetJson);
            return await PutTeamSettings(team.TeamId, payload);
        }

        private async Task<string> WhenTheBlockedConfigurationIsRead(SeededTeam team)
        {
            Client.AsTeamAdmin(team.TeamId);
            var (status, body) = await GetTeamSettings(team.TeamId);
            Assert.That(status, Is.EqualTo(HttpStatusCode.OK), body);
            return body;
        }

        // --- Then ---

        private static void ThenTheDefinitionIsSaved(HttpResponseMessage response)
            => Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                response.Content.ReadAsStringAsync().GetAwaiter().GetResult());

        private static void ThenTheSaveIsRejected(HttpResponseMessage response)
            => Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
                response.Content.ReadAsStringAsync().GetAwaiter().GetResult());

        private static void ThenTheSaveIsForbidden(HttpResponseMessage response)
            => Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden),
                response.Content.ReadAsStringAsync().GetAwaiter().GetResult());

        private static void ThenTheBlockedDefinitionIncludes(string settingsBody, string expectedToken)
        {
            using var document = JsonDocument.Parse(settingsBody);
            var root = document.RootElement;

            // The observable is "the team's blocked DEFINITION round-trips" — asserted specifically on the
            // blocked-definition field (never the whole payload, which also mentions states elsewhere).
            // blockedRuleSetJson is the sole persisted definition surface (legacy BlockedStates/BlockedTags
            // columns have been dropped).
            Assert.That(root.TryGetProperty("blockedRuleSetJson", out var ruleSetProp) && ruleSetProp.ValueKind == JsonValueKind.String,
                $"Settings payload must expose blockedRuleSetJson. Body: {settingsBody}");

            var ruleSetJson = ruleSetProp.GetString() ?? string.Empty;
            Assert.That(ruleSetJson, Does.Contain(expectedToken),
                $"The saved blocked definition must be readable on reload via blockedRuleSetJson. " +
                $"blockedRuleSetJson: {ruleSetJson}. Body: {settingsBody}");
        }

        private static void ThenTheMigratedRuleSetExpresses(string settingsBody, params string[] expectedConditionTokens)
        {
            using var document = JsonDocument.Parse(settingsBody);
            Assert.That(document.RootElement.TryGetProperty("blockedRuleSetJson", out var ruleSetProp), Is.True,
                $"Settings payload must expose blockedRuleSetJson after migration. Body: {settingsBody}");

            var ruleSet = Parse(ruleSetProp.GetString());
            Assert.That(ruleSet, Is.Not.Null, settingsBody);
            var conditionKeys = ruleSet!.Conditions.Select(c => $"{c.FieldKey} {c.Operator} {c.Value}").ToList();
            foreach (var token in expectedConditionTokens)
            {
                Assert.That(conditionKeys, Has.Some.Contains(token),
                    $"Migrated blocked rule set must contain a condition matching '{token}'. Conditions: {string.Join(" | ", conditionKeys)}");
            }
        }

        private static void ThenTheMigratedRuleSetIsEmpty(string settingsBody)
        {
            using var document = JsonDocument.Parse(settingsBody);
            Assert.That(document.RootElement.TryGetProperty("blockedRuleSetJson", out var ruleSetProp), Is.True,
                $"Settings payload must expose blockedRuleSetJson after migration. Body: {settingsBody}");
            var ruleSet = Parse(ruleSetProp.GetString());
            Assert.That(ruleSet?.Conditions ?? [], Is.Empty,
                $"A team that never configured blocked states/tags must migrate to an empty rule set. Body: {settingsBody}");
        }

        private async Task ThenTheItemReadsBlocked(SeededTeam team, string referenceId)
        {
            var (status, body) = await GetTeamWip(team.TeamId);
            Assert.That(status, Is.EqualTo(HttpStatusCode.OK), body);
            var item = WorkItemByReference(body, referenceId);
            Assert.That(item.GetProperty("isBlocked").GetBoolean(), Is.True,
                $"Item {referenceId} must read as blocked via the single rule-based definition. Body: {body}");
        }

        private async Task ThenTheItemDoesNotReadBlocked(SeededTeam team, string referenceId)
        {
            var (status, body) = await GetTeamWip(team.TeamId);
            Assert.That(status, Is.EqualTo(HttpStatusCode.OK), body);
            var item = WorkItemByReference(body, referenceId);
            Assert.That(item.GetProperty("isBlocked").GetBoolean(), Is.False,
                $"Item {referenceId} must not read as blocked. Body: {body}");
        }
    }
}
