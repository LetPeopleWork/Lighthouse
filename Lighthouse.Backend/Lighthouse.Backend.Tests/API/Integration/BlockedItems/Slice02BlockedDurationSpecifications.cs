using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Tests.TestHelpers;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.BlockedItems
{
    /// <summary>
    /// DISTILL step definitions (Specifications) for Slice 02 — Blocked-time capture / per-item duration.
    /// Backend-observable contract: the team WIP read surface exposes a per-item blocked-duration
    /// (blockedSince) for items that are blocked per the slice-01 rule set. The "blocked Nd" badge copy
    /// and the "Approximate — based on sync cadence" tooltip are FE (Vitest) concerns and are covered
    /// in DELIVER; here we drive the backend read field only.
    /// </summary>
    public partial class Slice02BlockedDurationTest : BlockedItemsAcceptanceTest
    {
        // --- Given ---

        private SeededTeam GivenATeamThatTreatsAStateAsBlocked(string blockedState)
            => SeedTeam(blockedStates: [blockedState]);

        private void GivenABlockedItemObservedDaysAgo(SeededTeam team, string referenceId, string blockedState, int daysAgo)
            => SeedWorkItem(team.TeamId, referenceId, state: blockedState, currentStateEnteredAt: SyncDay.AddDays(-daysAgo));

        private void GivenAnItemThatIsNotBlocked(SeededTeam team, string referenceId)
            => SeedWorkItem(team.TeamId, referenceId, state: "In Progress", currentStateEnteredAt: SyncDay.AddDays(-5));

        private void GivenABlockedItemFirstObservedThisSync(SeededTeam team, string referenceId, string blockedState)
            => SeedWorkItem(team.TeamId, referenceId, state: blockedState, currentStateEnteredAt: null);

        // --- When ---

        private async Task<JsonElement> WhenPriyaOpensTheTeamView(SeededTeam team, string referenceId)
        {
            Client.AsTeamAdmin(team.TeamId);
            var (status, body) = await GetTeamWip(team.TeamId);
            Assert.That(status, Is.EqualTo(HttpStatusCode.OK), body);
            return WorkItemByReference(body, referenceId);
        }

        // --- Then ---

        private static void ThenTheItemExposesABlockedDuration(JsonElement item)
        {
            Assert.Multiple(() =>
            {
                Assert.That(item.TryGetProperty("blockedSince", out var blockedSince), Is.True,
                    $"A blocked item's read surface must expose blockedSince (per-item blocked duration). Item: {item}");
                Assert.That(blockedSince.ValueKind, Is.EqualTo(JsonValueKind.String),
                    $"blockedSince must carry the enter-blocked timestamp for a captured spell. Item: {item}");
            });
        }

        private static void ThenTheItemExposesNoBlockedDuration(JsonElement item)
        {
            Assert.Multiple(() =>
            {
                Assert.That(item.TryGetProperty("blockedSince", out var blockedSince), Is.True,
                    $"blockedSince must be present on the contract even when null (first-observation / not blocked). Item: {item}");
                Assert.That(blockedSince.ValueKind, Is.EqualTo(JsonValueKind.Null),
                    $"An item with no open blocked spell must surface blockedSince as null, not a fabricated value. Item: {item}");
            });
        }
    }
}
