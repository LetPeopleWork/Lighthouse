using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static Lighthouse.Backend.Tests.API.Integration.BlockedItems.BlockedItemsJson;

namespace Lighthouse.Backend.Tests.API.Integration.BlockedItems
{
    /// <summary>
    /// DISTILL step definitions (Specifications) for Slice 05 — Jira flagged via a predefined (system-owned)
    /// additional field (ADR-071, amended 2026-07-11: SPIKE waived + GetPredefinedAdditionalFields connector
    /// port method). Business-language Given/When/Then step methods; each delegates to the production driving
    /// ports (team settings PUT/GET + team metrics WIP read on <see cref="BlockedItemsAcceptanceTest"/>, and
    /// the work-tracking-system-connection settings GET/PUT). No business logic lives in the steps (Mandate-12).
    ///
    /// Contract-compile note: the new observables — the additional-field DTO's <c>isPredefined</c> flag and the
    /// auto-registered predefined "Flagged" field — do NOT exist on today's types. So every predefined-specific
    /// step asserts on the served JSON (never on a not-yet-existing model/DTO member), keeping the suite
    /// black-box and COMPILING against today's contract while failing RED on the missing behaviour. This mirrors
    /// how Slice 04 drives blockedStalenessThresholdDays through raw JSON.
    /// </summary>
    public partial class Slice05PredefinedFieldTest : BlockedItemsAcceptanceTest
    {
        // --- Given (connection-level preconditions) ---

        private int SeedConnection(WorkTrackingSystems system, params (string DisplayName, string Reference)[] userFields)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Connection {Guid.NewGuid():N}",
                WorkTrackingSystem = system,
                AuthenticationMethodKey = AuthenticationMethodKeys.GetDefaultForSystem(system),
            };

            foreach (var (displayName, reference) in userFields)
            {
                connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition
                {
                    DisplayName = displayName,
                    Reference = reference,
                });
            }

            var repository = sp.GetRequiredService<IRepository<WorkTrackingSystemConnection>>();
            repository.Add(connection);
            repository.Save().GetAwaiter().GetResult();

            return connection.Id;
        }

        private int GivenAJiraConnection(params (string DisplayName, string Reference)[] userFields)
            => SeedConnection(WorkTrackingSystems.Jira, userFields);

        // --- Given (team-level preconditions, mirrors Slice 01) ---

        private SeededTeam GivenAJiraTeamWithAFlaggedFieldAndOneFlaggedItem(string flaggedItem)
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

        private SeededTeam GivenAJiraTeamWithAFlaggedField()
            => SeedTeam(withFlaggedAdditionalField: true);

        private void GivenAFlaggedItem(SeededTeam team, string referenceId, bool flagged)
            => SeedWorkItem(
                team.TeamId,
                referenceId,
                state: "In Progress",
                tags: [],
                additionalFieldValues: flagged ? new() { [team.FlaggedFieldId] = "Impediment" } : new());

        // --- When (connection settings driving port) ---

        private async Task<string> WhenTheConnectionConfigurationIsRead(int connectionId)
        {
            Client.AsSystemAdmin();
            var response = await Client.GetAsync($"/api/latest/worktrackingsystemconnections/{connectionId}");
            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
            return body;
        }

        private async Task<HttpResponseMessage> WhenTheAdminSavesTheConnection(int connectionId, JsonObject payload)
        {
            Client.AsSystemAdmin();
            return await Client.PutAsync(
                $"/api/latest/worktrackingsystemconnections/{connectionId}",
                new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json"));
        }

        // --- When (blocked rule set on the team settings driving port, mirrors Slice 01) ---

        private async Task<HttpResponseMessage> WhenTheAdminSavesTheBlockedRuleSet(SeededTeam team, string ruleSetJson)
        {
            Client.AsTeamAdmin(team.TeamId);
            var payload = WithBlockedRuleSet(ToJsonObject(BuildTeamSettings(team)), ruleSetJson);
            return await PutTeamSettings(team.TeamId, payload);
        }

        // --- Non-premium licence context (slot-limit split) ---

        private void GivenTheConnectionIsOnANonPremiumPlan()
            => LicenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(false);

        // --- Payload transforms (pure JSON, no business logic) ---

        private static JsonObject AsConnectionPayload(string body)
            => JsonNode.Parse(body)!.AsObject();

        private static JsonArray AdditionalFields(JsonObject payload)
            => payload["additionalFieldDefinitions"]?.AsArray() ?? new JsonArray();

        private static bool IsPredefinedField(JsonObject field)
        {
            foreach (var key in new[] { "isPredefined", "IsPredefined" })
            {
                if (field.TryGetPropertyValue(key, out var value) && value is JsonValue jsonValue
                    && jsonValue.TryGetValue<bool>(out var flag) && flag)
                {
                    return true;
                }
            }

            return false;
        }

        private static List<JsonObject> PredefinedFields(string body)
            => AdditionalFields(AsConnectionPayload(body))
                .OfType<JsonObject>()
                .Where(IsPredefinedField)
                .ToList();

        private static JsonObject WithoutPredefinedFields(JsonObject payload)
        {
            var kept = AdditionalFields(payload).OfType<JsonObject>().Where(f => !IsPredefinedField(f))
                .Select(f => JsonNode.Parse(f.ToJsonString())!).ToArray();
            payload["additionalFieldDefinitions"] = new JsonArray(kept);
            return payload;
        }

        private static JsonObject WithAnAddedUserField(JsonObject payload, string displayName, string reference)
        {
            AdditionalFields(payload).Add(new JsonObject
            {
                ["id"] = 0,
                ["displayName"] = displayName,
                ["reference"] = reference,
            });
            return payload;
        }

        private static JsonObject WithAChangedReferenceOnEveryPredefinedField(JsonObject payload, string newReference)
        {
            foreach (var field in AdditionalFields(payload).OfType<JsonObject>().Where(IsPredefinedField))
            {
                field["reference"] = newReference;
            }

            return payload;
        }

        private static JsonObject WithAWriteBackMappingTargetingTheFirstPredefinedField(JsonObject payload)
        {
            var predefined = AdditionalFields(payload).OfType<JsonObject>().FirstOrDefault(IsPredefinedField);
            if (predefined is null)
            {
                return payload;
            }

            var mappings = payload["writeBackMappingDefinitions"]?.AsArray() ?? new JsonArray();
            mappings.Add(new JsonObject
            {
                ["id"] = 0,
                ["valueSource"] = 0,
                ["appliesTo"] = 0,
                ["additionalFieldDefinitionId"] = predefined["id"]?.GetValue<int>() ?? 0,
                ["targetValueType"] = 0,
            });
            payload["writeBackMappingDefinitions"] = mappings;
            return payload;
        }

        // --- Then (connection settings observables) ---

        private static void ThenTheServedConnectionStillCarriesTheUserField(string body, string reference)
        {
            var references = AdditionalFields(AsConnectionPayload(body)).OfType<JsonObject>()
                .Select(f => f["reference"]?.GetValue<string>()).ToList();
            Assert.That(references, Has.Some.EqualTo(reference),
                $"The connection's user-configured additional field must round-trip across a settings save. " +
                $"Served references: [{string.Join(" | ", references)}]. Body: {body}");
        }

        private static void ThenExactlyOnePredefinedFlaggedFieldIsSurfaced(string body)
        {
            var predefined = PredefinedFields(body);
            Assert.That(predefined, Has.Count.EqualTo(1),
                $"A Jira connection must surface exactly one predefined (isPredefined=true) \"Flagged\" additional field " +
                $"(auto-registered, idempotent). Auto-registration is absent today, so none is served. " +
                $"Predefined display names: [{string.Join(" | ", predefined.Select(f => f["displayName"]?.GetValue<string>()))}]. Body: {body}");
        }

        private static void ThenAPredefinedFlaggedFieldIsStillSurfaced(string body)
            => Assert.That(PredefinedFields(body), Is.Not.Empty,
                $"A settings save that omits the predefined field must NOT delete it (reconcile merge-back). " +
                $"The predefined field must still be served after the save. Body: {body}");

        private static void ThenNoPredefinedFieldIsSurfaced(string body)
            => Assert.That(PredefinedFields(body), Is.Empty,
                $"A {nameof(WorkTrackingSystems)} connector that contributes no predefined fields must surface none. Body: {body}");

        private static void ThenTheConnectionsPredefinedFieldReferenceIs(string body, string expectedReference)
        {
            var predefined = PredefinedFields(body);
            Assert.That(predefined, Is.Not.Empty,
                $"expected a predefined field to be surfaced so its Reference immutability can be asserted. Body: {body}");
            Assert.That(predefined[0]["reference"]?.GetValue<string>(), Is.EqualTo(expectedReference),
                $"A predefined field's Reference is immutable after auto-registration — a settings save must not change it. Body: {body}");
        }

        private static void ThenNoWriteBackMappingTargetsAPredefinedField(string body)
        {
            var payload = AsConnectionPayload(body);
            var predefinedIds = AdditionalFields(payload).OfType<JsonObject>().Where(IsPredefinedField)
                .Select(f => f["id"]?.GetValue<int>() ?? 0).ToHashSet();
            var mappings = payload["writeBackMappingDefinitions"]?.AsArray() ?? new JsonArray();
            var offending = mappings.OfType<JsonObject>()
                .Select(m => m["additionalFieldDefinitionId"])
                .Where(id => id is not null && predefinedIds.Contains(id!.GetValue<int>()))
                .ToList();

            Assert.That(offending, Is.Empty,
                $"A predefined field is inbound-only — it must NOT be persisted as a write-back mapping target. Body: {body}");
        }

        private static void ThenTheSaveSucceeds(HttpResponseMessage response)
            => Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                response.Content.ReadAsStringAsync().GetAwaiter().GetResult());

        // --- Then (team WIP read observables, mirrors Slice 01) ---

        private async Task ThenTheItemReadsBlockedWithoutASyntheticFlaggedLabel(SeededTeam team, string referenceId)
        {
            var (status, body) = await GetTeamWip(team.TeamId);
            Assert.That(status, Is.EqualTo(HttpStatusCode.OK), body);
            var item = WorkItemByReference(body, referenceId);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(item.GetProperty("isBlocked").GetBoolean(), Is.True,
                    $"Item {referenceId} must read blocked via the predefined flagged field (generic id-keyed rule path). Body: {body}");
                Assert.That(Tags(item), Does.Not.Contain("Flagged"),
                    $"No synthetic \"Flagged\" label must be present on {referenceId}'s tags — the flag flows only through the predefined field. Body: {body}");
            }
        }

        private async Task ThenTheItemBlockedStatusIs(SeededTeam team, string referenceId, bool expectedBlocked)
        {
            var (status, body) = await GetTeamWip(team.TeamId);
            Assert.That(status, Is.EqualTo(HttpStatusCode.OK), body);
            var item = WorkItemByReference(body, referenceId);
            Assert.That(item.GetProperty("isBlocked").GetBoolean(), Is.EqualTo(expectedBlocked),
                $"Item {referenceId} flagged={expectedBlocked} must read isBlocked={expectedBlocked} via the generic id-keyed field path. Body: {body}");
        }

        private static IEnumerable<string> Tags(JsonElement item)
        {
            if (!item.TryGetProperty("tags", out var tags) || tags.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return tags.EnumerateArray().Select(t => t.GetString() ?? string.Empty).ToList();
        }
    }
}
