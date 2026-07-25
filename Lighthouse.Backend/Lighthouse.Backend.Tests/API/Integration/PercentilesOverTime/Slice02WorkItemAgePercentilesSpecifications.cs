using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// DELIVER step definitions (Specifications) for Slice 02 — Work Item Age percentiles over time.
    /// Backend-observable contract: the SAME two read endpoints now select the metric family through an
    /// additive <c>metricType</c> query parameter (default CycleTime, so every slice-01-shaped request is
    /// unchanged on the wire). Work Item Age is measured as-of-today and has no horizon dimension: it is
    /// served under the horizon-less sentinel the shared recording pipeline persists, whatever horizon
    /// the caller happens to send. An owner with no age snapshots gets an honest empty array.
    /// </summary>
    public partial class Slice02WorkItemAgePercentilesTest : PercentilesOverTimeAcceptanceTest
    {
        protected readonly record struct AgePercentilePoint(DateOnly RecordedAt, int P50, int P70, int P85, int P95);

        // --- Given ---

        private int GivenATeam() => SeedTeam();

        private int GivenAPortfolio() => SeedPortfolio();

        private void GivenPersistedWorkItemAgePercentiles(int ownerId, OwnerType ownerType, List<AgePercentilePoint> points)
        {
            foreach (var point in points)
            {
                SeedPercentilesSnapshot(new PercentilesOverTimeSnapshot
                {
                    OwnerId = ownerId,
                    OwnerType = ownerType,
                    RecordedAt = point.RecordedAt,
                    MetricType = MetricType.WorkItemAge,
                    Horizon = PercentilesOverTimeSnapshot.NoHorizon,
                    P50 = point.P50,
                    P70 = point.P70,
                    P85 = point.P85,
                    P95 = point.P95,
                });
            }
        }

        private void GivenPersistedCycleTimePercentiles(int ownerId, OwnerType ownerType, int horizon, List<AgePercentilePoint> points)
        {
            foreach (var point in points)
            {
                SeedCycleTimePercentilesSnapshot(ownerId, ownerType, point.RecordedAt, horizon, point.P50, point.P70, point.P85, point.P95);
            }
        }

        private void GivenAnItemInProgressSinceDays(int teamId, string referenceId, int ageInDays)
            => SeedInProgressWorkItem(teamId, referenceId, ageInDays);

        // --- When ---

        private Task WhenTheTeamMetricsRefreshCompletes(int teamId) => TheTeamMetricsRefreshCompletes(teamId);

        private Task<(HttpStatusCode Status, string Body)> WhenTheFlowCoachReadsTheTeamAgeTrend(int teamId, int? horizon = null)
            => GetTeamPercentilesOverTime(teamId, MetricType.WorkItemAge, horizon);

        private Task<(HttpStatusCode Status, string Body)> WhenTheFlowCoachReadsThePortfolioAgeTrend(int portfolioId)
            => GetPortfolioPercentilesOverTime(portfolioId, MetricType.WorkItemAge, horizon: null);

        private Task<(HttpStatusCode Status, string Body)> WhenAClientOnTheSliceOneContractReadsTheTeamTrend(int teamId, int horizon)
            => GetTeamPercentilesOverTime(teamId, horizon);

        // --- Then ---

        private static void ThenTheDatedAgePercentilesTrendComesBackOrderedByDate((HttpStatusCode Status, string Body) response, List<AgePercentilePoint> expected)
            => AssertSeries(response, expected, MetricType.WorkItemAge);

        private static void ThenTheDatedCycleTimePercentilesTrendComesBackOrderedByDate((HttpStatusCode Status, string Body) response, List<AgePercentilePoint> expected)
            => AssertSeries(response, expected, MetricType.CycleTime);

        private static void AssertSeries((HttpStatusCode Status, string Body) response, List<AgePercentilePoint> expected, MetricType expectedMetricType)
        {
            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.OK),
                $"The percentiles-over-time endpoint must serve the {expectedMetricType} series. Body: {response.Body}");

            var actual = ReadSeries(response.Body);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(actual, Has.Count.EqualTo(expected.Count),
                    $"The series must contain exactly the persisted {expectedMetricType} rows. Body: {response.Body}");
                for (var i = 0; i < expected.Count; i++)
                {
                    var expectedDate = expected[i].RecordedAt.ToString("yyyy-MM-dd");
                    Assert.That(actual[i].RecordedAt, Is.EqualTo(expectedDate),
                        $"Row {i} must be ordered ascending by RecordedAt. Body: {response.Body}");
                    Assert.That(actual[i].MetricType, Is.EqualTo(expectedMetricType.ToString()),
                        $"Row {i} must report the {expectedMetricType} metric family. Body: {response.Body}");
                    Assert.That(actual[i].P50, Is.EqualTo(expected[i].P50), $"Row {i} p50. Body: {response.Body}");
                    Assert.That(actual[i].P70, Is.EqualTo(expected[i].P70), $"Row {i} p70. Body: {response.Body}");
                    Assert.That(actual[i].P85, Is.EqualTo(expected[i].P85), $"Row {i} p85. Body: {response.Body}");
                    Assert.That(actual[i].P95, Is.EqualTo(expected[i].P95), $"Row {i} p95. Body: {response.Body}");
                }
            }
        }

        private static void ThenTheSeriesIsEmpty((HttpStatusCode Status, string Body) response)
        {
            var actual = ReadSeries(response.Body);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Status, Is.EqualTo(HttpStatusCode.OK),
                    $"An owner with no age snapshots must still get an honest empty response, not an error. Body: {response.Body}");

                Assert.That(actual, Is.Empty,
                    $"With no age snapshots the series must be an empty array (honest empty-state), never zero-padded or broken. Body: {response.Body}");
            }
        }

        /// <summary>
        /// Parse the series body as JSON, failing with a clean RED assertion (not a raw parse exception)
        /// when the request falls through to the SPA HTML fallback.
        /// </summary>
        private static List<SeriesRow> ReadSeries(string body)
        {
            Assert.That(body.TrimStart(), Does.StartWith("["),
                $"The percentiles-over-time endpoint must return a JSON array, not HTML/other. Body starts: {body[..Math.Min(60, body.Length)]}");

            using var document = JsonDocument.Parse(body);
            return document.RootElement
                .EnumerateArray()
                .Select(element => new SeriesRow(
                    element.GetProperty("recordedAt").GetString() ?? string.Empty,
                    element.GetProperty("metricType").GetString() ?? string.Empty,
                    element.GetProperty("p50").GetInt32(),
                    element.GetProperty("p70").GetInt32(),
                    element.GetProperty("p85").GetInt32(),
                    element.GetProperty("p95").GetInt32()))
                .ToList();
        }

        private readonly record struct SeriesRow(string RecordedAt, string MetricType, int P50, int P70, int P85, int P95);
    }
}
