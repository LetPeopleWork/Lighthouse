using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// DELIVER step definitions (Specifications) for Slice 01 — Percentiles Over Time read path.
    /// Backend-observable contract: two typed read endpoints (team + portfolio) serve the persisted
    /// CT percentiles series for an owner + horizon, ordered by RecordedAt, side-effect-free (reads only
    /// the snapshot table — never a live recompute of historical days). Empty owner → honest empty array.
    /// </summary>
    public partial class Slice01PercentilesOverTimeTest : PercentilesOverTimeAcceptanceTest
    {
        protected readonly record struct PercentilePoint(DateOnly RecordedAt, int P50, int P70, int P85, int P95);

        // --- Given ---

        private int GivenATeam() => SeedTeam();

        private int GivenAPortfolio() => SeedPortfolio();

        private void GivenPersistedCycleTimePercentiles(int ownerId, OwnerType ownerType, int horizon, List<PercentilePoint> points)
        {
            foreach (var point in points)
            {
                SeedCycleTimePercentilesSnapshot(ownerId, ownerType, point.RecordedAt, horizon, point.P50, point.P70, point.P85, point.P95);
            }
        }

        // --- When ---

        private Task<(HttpStatusCode Status, string Body)> WhenTheFlowCoachReadsTheTeamTrend(int teamId, int horizon)
            => GetTeamPercentilesOverTime(teamId, horizon);

        private Task<(HttpStatusCode Status, string Body)> WhenTheFlowCoachReadsThePortfolioTrend(int portfolioId, int horizon)
            => GetPortfolioPercentilesOverTime(portfolioId, horizon);

        // --- Then ---

        private static void ThenTheDatedPercentilesTrendComesBackOrderedByDate((HttpStatusCode Status, string Body) response, List<PercentilePoint> expected)
        {
            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.OK),
                $"The percentiles-over-time endpoint must serve the dated CT series. Body: {response.Body}");

            var actual = ParseSeries(response.Body)
                .EnumerateArray()
                .Select(e => (
                    RecordedAt: e.GetProperty("recordedAt").GetString(),
                    MetricType: e.GetProperty("metricType").GetString(),
                    P50: e.GetProperty("p50").GetInt32(),
                    P70: e.GetProperty("p70").GetInt32(),
                    P85: e.GetProperty("p85").GetInt32(),
                    P95: e.GetProperty("p95").GetInt32()))
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(actual, Has.Count.EqualTo(expected.Count),
                    $"The series must contain exactly the persisted rows for the horizon. Body: {response.Body}");
                for (var i = 0; i < expected.Count; i++)
                {
                    var expectedDate = expected[i].RecordedAt.ToString("yyyy-MM-dd");
                    Assert.That(actual[i].RecordedAt, Is.EqualTo(expectedDate),
                        $"Row {i} must be ordered ascending by RecordedAt. Body: {response.Body}");
                    Assert.That(actual[i].MetricType, Is.EqualTo(nameof(MetricType.CycleTime)),
                        $"Row {i} must report the CycleTime metric type. Body: {response.Body}");
                    Assert.That(actual[i].P50, Is.EqualTo(expected[i].P50), $"Row {i} p50. Body: {response.Body}");
                    Assert.That(actual[i].P70, Is.EqualTo(expected[i].P70), $"Row {i} p70. Body: {response.Body}");
                    Assert.That(actual[i].P85, Is.EqualTo(expected[i].P85), $"Row {i} p85. Body: {response.Body}");
                    Assert.That(actual[i].P95, Is.EqualTo(expected[i].P95), $"Row {i} p95. Body: {response.Body}");
                }
            }
        }

        private static void ThenTheSeriesIsEmpty((HttpStatusCode Status, string Body) response)
        {
            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.OK),
                $"An owner with no snapshots must still get an honest empty response, not an error. Body: {response.Body}");
            var series = ParseSeries(response.Body);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(series.ValueKind, Is.EqualTo(JsonValueKind.Array), response.Body);
                Assert.That(series.GetArrayLength(), Is.Zero,
                    $"With no snapshots the series must be an empty array (honest empty-state), never zero-padded or broken. Body: {response.Body}");
            }
        }

        /// <summary>
        /// Parse the series body as JSON, failing with a clean RED assertion (not a raw parse exception)
        /// when the endpoint is missing and the request falls through to the SPA HTML fallback.
        /// </summary>
        private static JsonElement ParseSeries(string body)
        {
            Assert.That(body.TrimStart(), Does.StartWith("["),
                $"The percentiles-over-time endpoint must return a JSON array, not HTML/other — the endpoint appears unimplemented. Body starts: {body[..Math.Min(60, body.Length)]}");
            using var document = JsonDocument.Parse(body);
            return document.RootElement.Clone();
        }
    }
}
