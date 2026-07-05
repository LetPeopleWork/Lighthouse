using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Tests.TestHelpers;
using NUnit.Framework;
using static Lighthouse.Backend.Tests.API.Integration.BlockedItems.BlockedItemsJson;

namespace Lighthouse.Backend.Tests.API.Integration.BlockedItems
{
    /// <summary>
    /// DISTILL step definitions (Specifications) for Slice 04 — Blocked → Stale linkage.
    /// Backend-observable contract: the new blockedStalenessThresholdDays settings field (twin of
    /// StalenessThresholdDays; 0 = disabled; config-admin gated; range-validated). The stale RENDERING —
    /// an item stale-once with a blocked-duration DRIVER plus a time-in-state CONTEXT reason (UC-1) — is
    /// computed by the FE deriveStaleness selector (ADR-070) and is covered by Vitest in DELIVER.
    /// </summary>
    public partial class Slice04BlockedStalenessTest : BlockedItemsAcceptanceTest
    {
        // --- Given ---

        private SeededTeam GivenATeam()
            => SeedTeam();

        // --- When ---

        private async Task<HttpResponseMessage> WhenTheAdminSetsTheBlockedStalenessThreshold(SeededTeam team, int days)
        {
            Client.AsTeamAdmin(team.TeamId);
            var payload = WithBlockedStalenessThreshold(ToJsonObject(BuildTeamSettings(team)), days);
            return await PutTeamSettings(team.TeamId, payload);
        }

        private async Task<HttpResponseMessage> WhenANonAdminSetsTheBlockedStalenessThreshold(SeededTeam team, int days)
        {
            Client.AsViewer();
            var payload = WithBlockedStalenessThreshold(ToJsonObject(BuildTeamSettings(team)), days);
            return await PutTeamSettings(team.TeamId, payload);
        }

        private async Task<string> WhenTheThresholdIsRead(SeededTeam team)
        {
            Client.AsTeamAdmin(team.TeamId);
            var (status, body) = await GetTeamSettings(team.TeamId);
            Assert.That(status, Is.EqualTo(HttpStatusCode.OK), body);
            return body;
        }

        // --- Then ---

        private static void ThenTheThresholdIs(string settingsBody, int expected)
        {
            using var document = JsonDocument.Parse(settingsBody);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(document.RootElement.TryGetProperty("blockedStalenessThresholdDays", out var prop), Is.True,
                    $"Settings payload must carry blockedStalenessThresholdDays (twin of stalenessThresholdDays). Body: {settingsBody}");
                Assert.That(prop.GetInt32(), Is.EqualTo(expected),
                    $"blockedStalenessThresholdDays must round-trip. Body: {settingsBody}");
            }
        }

        private static void ThenTheThresholdSaveSucceeds(HttpResponseMessage response)
            => Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                response.Content.ReadAsStringAsync().GetAwaiter().GetResult());

        private static void ThenTheThresholdSaveIsRejected(HttpResponseMessage response)
            => Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
                response.Content.ReadAsStringAsync().GetAwaiter().GetResult());

        private static void ThenTheThresholdSaveIsForbidden(HttpResponseMessage response)
            => Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden),
                response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
    }
}
