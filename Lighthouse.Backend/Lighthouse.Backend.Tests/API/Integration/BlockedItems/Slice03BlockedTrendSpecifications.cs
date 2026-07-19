using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
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

        protected void GivenBlockedCountSnapshots(SeededTeam team, List<(DateOnly Date, int Count)> snapshots)
        {
            using var scope = Factory.Services.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IBlockedCountSnapshotRepository>();
            foreach (var (date, count) in snapshots)
            {
                repo.Add(new BlockedCountSnapshot
                {
                    OwnerId = team.TeamId,
                    OwnerType = OwnerType.Team,
                    RecordedAt = date,
                    BlockedCount = count,
                });
            }

            repo.Save().GetAwaiter().GetResult();
        }

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
            using (Assert.EnterMultipleScope())
            {
                Assert.That(series.ValueKind, Is.EqualTo(JsonValueKind.Array), response.Body);
                Assert.That(series.GetArrayLength(), Is.Zero,
                    $"With no snapshots the series must be empty (empty state = 'builds forward from today'), never a fabricated flat-zero line. Body: {response.Body}");
            }
        }

        private void ThenTheTrendInterpolatesMissingDaysFromTheLastKnownCount((HttpStatusCode Status, string Body) response)
        {
            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.OK),
                $"The blocked-over-time endpoint must serve the interpolated daily series. Body: {response.Body}");
            var points = ParseSeries(response.Body)
                .EnumerateArray()
                .Select(e => (Date: e.GetProperty("recordedAt").GetString(), Count: e.GetProperty("blockedCount").GetInt32()))
                .ToList();
            var countByDate = points.ToDictionary(p => p.Date!, p => p.Count);

            var windowStart = DateOnly.FromDateTime(SyncDay.AddDays(-21)).ToString("yyyy-MM-dd");
            var gapDayBeforeSnapshot = DateOnly.FromDateTime(SyncDay.AddDays(-15)).ToString("yyyy-MM-dd");
            var realSnapshotDay = DateOnly.FromDateTime(SyncDay.AddDays(-14)).ToString("yyyy-MM-dd");
            var windowEnd = DateOnly.FromDateTime(SyncDay).ToString("yyyy-MM-dd");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(points, Has.Count.EqualTo(22),
                    $"The window spans 22 days; every day must appear exactly once. Body: {response.Body}");
                Assert.That(countByDate[windowStart], Is.EqualTo(2),
                    "The first window day carries the pre-window snapshot (count 2) forward — never a fabricated zero.");
                Assert.That(countByDate[gapDayBeforeSnapshot], Is.EqualTo(2),
                    "Days before the next real snapshot keep the last known count.");
                Assert.That(countByDate[realSnapshotDay], Is.EqualTo(6),
                    "The day with a real snapshot reports the recorded count.");
                Assert.That(countByDate[windowEnd], Is.EqualTo(6),
                    "The final day carries the latest recorded count forward through the window end.");
            }
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
