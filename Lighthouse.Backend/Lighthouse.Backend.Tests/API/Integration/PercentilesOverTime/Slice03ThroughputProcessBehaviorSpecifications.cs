using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// DELIVER step definitions (Specifications) for Slice 03 — Throughput process-behaviour limits over
    /// time. Backend-observable contract: a SECOND typed read action on the same two metrics controllers
    /// (ADR-108 — two typed endpoints, never one polymorphic envelope), serving
    /// <c>{recordedAt, unpl, average, lnpl}</c> rows ordered by date. The read is pure: it replays
    /// persisted rows and never recomputes a historical day, so an owner with nothing recorded gets an
    /// honest empty array rather than a fabricated or zero-padded chart. An unknown metric family is a
    /// 400, not an empty 200 — an empty 200 would be indistinguishable from "nothing recorded yet".
    /// </summary>
    public partial class Slice03ThroughputProcessBehaviorTest : PercentilesOverTimeAcceptanceTest
    {
        private const string ThroughputType = "Throughput";

        protected readonly record struct ProcessBehaviorPoint(DateOnly RecordedAt, int Unpl, int Average, int Lnpl);

        // --- Given ---

        private int GivenATeam() => SeedTeam();

        private int GivenAPortfolio() => SeedPortfolio();

        private int GivenAFreshTeamWithNoRecordedLimits() => SeedTeam();

        /// <summary>
        /// Builds a plausible ascending run of recorded days ending on the sync day. Seeded in reverse so
        /// the endpoint's ordering guarantee is proven rather than inherited from insertion order.
        /// </summary>
        private static List<ProcessBehaviorPoint> ARunOfRecordedDays(DateTime syncDay, int dayCount)
        {
            var points = new List<ProcessBehaviorPoint>();
            for (var index = 0; index < dayCount; index++)
            {
                var recordedAt = DateOnly.FromDateTime(syncDay.AddDays(-7 * (dayCount - 1 - index)));
                points.Add(new ProcessBehaviorPoint(recordedAt, Unpl: 12 + index, Average: 7 + index, Lnpl: 2 + index));
            }

            return points;
        }

        private void GivenPersistedThroughputLimits(int ownerId, OwnerType ownerType, List<ProcessBehaviorPoint> points)
        {
            foreach (var point in Enumerable.Reverse(points))
            {
                SeedProcessBehaviorSnapshot(new ProcessBehaviorSnapshot
                {
                    OwnerId = ownerId,
                    OwnerType = ownerType,
                    RecordedAt = point.RecordedAt,
                    MetricType = ProcessBehaviorMetricType.Throughput,
                    Unpl = point.Unpl,
                    Average = point.Average,
                    Lnpl = point.Lnpl,
                });
            }
        }

        private void GivenPersistedCycleTimePercentiles(int ownerId, OwnerType ownerType, int horizon, List<PercentilePoint> points)
        {
            foreach (var point in points)
            {
                SeedCycleTimePercentilesSnapshot(ownerId, ownerType, point.RecordedAt, horizon, point.P50, point.P70, point.P85, point.P95);
            }
        }

        private void SeedProcessBehaviorSnapshot(ProcessBehaviorSnapshot snapshot)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IProcessBehaviorSnapshotRepository>();
            repository.Add(snapshot);
            repository.Save().GetAwaiter().GetResult();
        }

        // --- When ---

        private Task<(HttpStatusCode Status, string Body)> WhenTheDeliveryLeadOpensTheTeamPbcOverTimeWidget(int teamId, string? type = ThroughputType)
            => GetTeamProcessBehaviorOverTime(teamId, type);

        private Task<(HttpStatusCode Status, string Body)> WhenTheDeliveryLeadOpensThePortfolioPbcOverTimeWidget(int portfolioId, string? type = ThroughputType)
            => GetPortfolioProcessBehaviorOverTime(portfolioId, type);

        private async Task<(HttpStatusCode Status, string Body)> GetTeamProcessBehaviorOverTime(int teamId, string? type)
        {
            Client.AsTeamAdmin(teamId);
            var response = await Client.GetAsync($"/api/latest/teams/{teamId}/metrics/process-behavior-over-time{BuildTypeQuery(type)}");
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        private async Task<(HttpStatusCode Status, string Body)> GetPortfolioProcessBehaviorOverTime(int portfolioId, string? type)
        {
            Client.AsPortfolioAdmin(portfolioId);
            var response = await Client.GetAsync($"/api/latest/portfolios/{portfolioId}/metrics/process-behavior-over-time{BuildTypeQuery(type)}");
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        private static string BuildTypeQuery(string? type) => type is null ? string.Empty : $"?type={type}";

        // --- Then ---

        private static void ThenTheDatedLimitTripleComesBackOrderedByDate((HttpStatusCode Status, string Body) response, List<ProcessBehaviorPoint> expected)
        {
            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.OK),
                $"The process-behavior-over-time endpoint must serve the Throughput limits series. Body: {response.Body}");

            var actual = ReadLimitSeries(response.Body);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(actual, Has.Count.EqualTo(expected.Count),
                    $"The series must contain exactly the persisted Throughput rows. Body: {response.Body}");
                for (var i = 0; i < expected.Count; i++)
                {
                    Assert.That(actual[i].RecordedAt, Is.EqualTo(expected[i].RecordedAt.ToString("yyyy-MM-dd")),
                        $"Row {i} must be ordered ascending by RecordedAt. Body: {response.Body}");
                    Assert.That(actual[i].Unpl, Is.EqualTo(expected[i].Unpl), $"Row {i} upper natural process limit. Body: {response.Body}");
                    Assert.That(actual[i].Average, Is.EqualTo(expected[i].Average), $"Row {i} average. Body: {response.Body}");
                    Assert.That(actual[i].Lnpl, Is.EqualTo(expected[i].Lnpl), $"Row {i} lower natural process limit. Body: {response.Body}");
                }
            }
        }

        private static void ThenTheWidgetGetsTheForwardOnlyEmptyState((HttpStatusCode Status, string Body) response)
        {
            var actual = ReadLimitSeries(response.Body);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Status, Is.EqualTo(HttpStatusCode.OK),
                    $"An owner with no recorded limits must still get an honest response, not an error. Body: {response.Body}");
                Assert.That(actual, Is.Empty,
                    $"With no recorded limits the series must be an empty array (forward-only empty state), never zero-padded or broken. Body: {response.Body}");
            }
        }

        private static void ThenTheRequestIsRejectedAsUnsupported((HttpStatusCode Status, string Body) response)
        {
            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.BadRequest),
                $"An unknown metric family must be rejected, never answered with an empty 200 that the widget would misread as 'nothing recorded yet'. Body: {response.Body}");
        }

        private static void ThenTheShippedPercentilesPayloadIsUnchanged((HttpStatusCode Status, string Body) response, List<PercentilePoint> expected)
        {
            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.OK),
                $"The shipped percentiles-over-time endpoint must keep answering its original contract. Body: {response.Body}");

            var actual = ReadPercentileSeries(response.Body);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(actual, Has.Count.EqualTo(expected.Count),
                    $"The shipped series must be unperturbed by the new endpoint. Body: {response.Body}");
                for (var i = 0; i < expected.Count; i++)
                {
                    Assert.That(actual[i].RecordedAt, Is.EqualTo(expected[i].RecordedAt.ToString("yyyy-MM-dd")), $"Row {i} date. Body: {response.Body}");
                    Assert.That(actual[i].MetricType, Is.EqualTo(nameof(MetricType.CycleTime)), $"Row {i} metric family. Body: {response.Body}");
                    Assert.That(actual[i].P50, Is.EqualTo(expected[i].P50), $"Row {i} p50. Body: {response.Body}");
                    Assert.That(actual[i].P70, Is.EqualTo(expected[i].P70), $"Row {i} p70. Body: {response.Body}");
                    Assert.That(actual[i].P85, Is.EqualTo(expected[i].P85), $"Row {i} p85. Body: {response.Body}");
                    Assert.That(actual[i].P95, Is.EqualTo(expected[i].P95), $"Row {i} p95. Body: {response.Body}");
                }
            }
        }

        /// <summary>
        /// Parse a series body as JSON, failing with a clean RED assertion (not a raw parse exception)
        /// when the request falls through to the SPA HTML fallback because the route does not exist.
        /// </summary>
        private static List<LimitRow> ReadLimitSeries(string body)
        {
            AssertIsJsonArray(body);

            using var document = JsonDocument.Parse(body);
            return document.RootElement
                .EnumerateArray()
                .Select(element => new LimitRow(
                    element.GetProperty("recordedAt").GetString() ?? string.Empty,
                    element.GetProperty("unpl").GetInt32(),
                    element.GetProperty("average").GetInt32(),
                    element.GetProperty("lnpl").GetInt32()))
                .ToList();
        }

        private static List<PercentileRow> ReadPercentileSeries(string body)
        {
            AssertIsJsonArray(body);

            using var document = JsonDocument.Parse(body);
            return document.RootElement
                .EnumerateArray()
                .Select(element => new PercentileRow(
                    element.GetProperty("recordedAt").GetString() ?? string.Empty,
                    element.GetProperty("metricType").GetString() ?? string.Empty,
                    element.GetProperty("p50").GetInt32(),
                    element.GetProperty("p70").GetInt32(),
                    element.GetProperty("p85").GetInt32(),
                    element.GetProperty("p95").GetInt32()))
                .ToList();
        }

        private static void AssertIsJsonArray(string body)
        {
            Assert.That(body.TrimStart(), Does.StartWith("["),
                $"The endpoint must return a JSON array, not HTML/other — it appears unimplemented. Body starts: {body[..Math.Min(60, body.Length)]}");
        }

        protected readonly record struct PercentilePoint(DateOnly RecordedAt, int P50, int P70, int P85, int P95);

        private readonly record struct LimitRow(string RecordedAt, int Unpl, int Average, int Lnpl);

        private readonly record struct PercentileRow(string RecordedAt, string MetricType, int P50, int P70, int P85, int P95);
    }
}
