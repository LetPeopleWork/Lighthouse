using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Tests.TestHelpers;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.BlockedItems
{
    /// <summary>
    /// DISTILL step definitions (Specifications) for Slice 03 — Blocked-items over-time trend.
    /// Backend-observable contract: a new forward-only read endpoint serves the daily in-scope blocked
    /// count per owner. The chart placement in the Flow Metrics area and the empty-state copy are FE
    /// (Vitest / Playwright) concerns covered in DELIVER; here we drive the read endpoint.
    /// </summary>
    public partial class Slice03BlockedTrendTest : BlockedItemsAcceptanceTest
    {
        // --- Given ---

        private SeededTeam GivenATeam()
            => SeedTeam(blockedStates: ["Blocked"]);

        // --- When ---

        private async Task<(HttpStatusCode Status, string Body)> WhenTheDeliveryLeadOpensTheBlockedTrend(SeededTeam team)
        {
            Client.AsTeamAdmin(team.TeamId);
            var start = SyncDay.AddDays(-21);
            var response = await Client.GetAsync(
                $"/api/latest/teams/{team.TeamId}/metrics/blockedCountHistory?startDate={start:O}&endDate={SyncDay:O}");
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        // --- Then ---

        private static void ThenTheTrendIsAvailable((HttpStatusCode Status, string Body) response)
        {
            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.OK),
                $"The blocked-over-time endpoint must serve the daily blocked-count series. Body: {response.Body}");
            var series = ParseSeries(response.Body);
            Assert.That(series.ValueKind, Is.EqualTo(JsonValueKind.Array),
                $"The blocked trend must be a daily series. Body: {response.Body}");
        }

        private static void ThenTheTrendShowsTheForwardOnlyEmptyState((HttpStatusCode Status, string Body) response)
        {
            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.OK),
                $"A team with no snapshots must still get an honest forward-only response, not an error. Body: {response.Body}");
            var series = ParseSeries(response.Body);
            Assert.Multiple(() =>
            {
                Assert.That(series.ValueKind, Is.EqualTo(JsonValueKind.Array), response.Body);
                Assert.That(series.GetArrayLength(), Is.Zero,
                    $"With no snapshots the series must be empty (empty state = 'builds forward from today'), never a fabricated flat-zero line. Body: {response.Body}");
            });
        }

        /// <summary>
        /// Parse the trend body as JSON, failing with a clean RED assertion (not a raw parse exception)
        /// when the endpoint is missing and the request falls through to the SPA HTML fallback.
        /// </summary>
        private static JsonElement ParseSeries(string body)
        {
            Assert.That(body.TrimStart(), Does.StartWith("["),
                $"The blockedCountHistory endpoint must return a JSON array, not HTML/other — the endpoint appears unimplemented. Body starts: {body[..Math.Min(60, body.Length)]}");
            using var document = JsonDocument.Parse(body);
            return document.RootElement.Clone();
        }
    }
}
