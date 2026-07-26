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
    /// DELIVER step definitions (Specifications) for Slice 03b — the dashboard date range applies to both
    /// over-time series endpoints. Backend-observable contract (ADR-108 slice-03b amendment): both shipped
    /// read actions take an OPTIONAL <c>startDate</c>/<c>endDate</c> window on the recorded day, inclusive
    /// at both ends, on team and portfolio scope alike. Either bound may be omitted on its own; omitting
    /// both reproduces the shipped full-history response byte-for-byte, which is what keeps the parameters
    /// additive rather than breaking. An INVERTED window is rejected with 400 rather than answered with an
    /// empty 200 — the same reasoning the shipped unknown-metric-family guard uses, because the widget
    /// would otherwise report a caller error as an honest "no data recorded in the selected range".
    /// The endpoints stay read-only: a narrowed window plots fewer already-recorded days, it never
    /// triggers a recompute of the days outside it.
    /// </summary>
    public partial class Slice03bDateRangeTest : PercentilesOverTimeAcceptanceTest
    {
        private const int Horizon = 30;
        private const string ThroughputType = "Throughput";

        // --- Given ---

        private int GivenATeam() => SeedTeam();

        private int GivenAPortfolio() => SeedPortfolio();

        /// <summary>
        /// Five consecutive recorded days ending on the sync day, so a window can be placed strictly
        /// inside, on the boundaries, or entirely outside the recorded history.
        /// </summary>
        private List<DateOnly> GivenFiveConsecutiveRecordedDays(int ownerId, OwnerType ownerType)
        {
            var days = RecordedDays();

            foreach (var day in days)
            {
                SeedCycleTimePercentilesSnapshot(ownerId, ownerType, day, Horizon, 3, 5, 8, 13);
                SeedProcessBehaviorSnapshot(ownerId, ownerType, day);
            }

            return days;
        }

        private List<DateOnly> RecordedDays()
        {
            var firstDay = DateOnly.FromDateTime(SyncDay.Date).AddDays(-4);
            return [.. Enumerable.Range(0, 5).Select(firstDay.AddDays)];
        }

        private void SeedProcessBehaviorSnapshot(int ownerId, OwnerType ownerType, DateOnly recordedAt)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IProcessBehaviorSnapshotRepository>();
            repository.Add(new ProcessBehaviorSnapshot
            {
                OwnerId = ownerId,
                OwnerType = ownerType,
                RecordedAt = recordedAt,
                MetricType = ProcessBehaviorMetricType.Throughput,
                Unpl = 13,
                Average = 8,
                Lnpl = 3,
            });
            repository.Save().GetAwaiter().GetResult();
        }

        // --- When (read-side driving port, exercised over its real protocol) ---

        private async Task<(HttpStatusCode Status, string Body)> WhenTheTeamPercentilesSeriesIsRequested(int teamId, DateOnly? from, DateOnly? to)
        {
            Client.AsTeamAdmin(teamId);
            var response = await Client.GetAsync($"/api/latest/teams/{teamId}/metrics/percentiles-over-time?horizon={Horizon}{BuildWindowQuery(from, to)}");
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        private async Task<(HttpStatusCode Status, string Body)> WhenThePortfolioPercentilesSeriesIsRequested(int portfolioId, DateOnly? from, DateOnly? to)
        {
            Client.AsPortfolioAdmin(portfolioId);
            var response = await Client.GetAsync($"/api/latest/portfolios/{portfolioId}/metrics/percentiles-over-time?horizon={Horizon}{BuildWindowQuery(from, to)}");
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        private async Task<(HttpStatusCode Status, string Body)> WhenTheTeamProcessBehaviorSeriesIsRequested(int teamId, DateOnly? from, DateOnly? to)
        {
            Client.AsTeamAdmin(teamId);
            var response = await Client.GetAsync($"/api/latest/teams/{teamId}/metrics/process-behavior-over-time?type={ThroughputType}{BuildWindowQuery(from, to)}");
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        private async Task<(HttpStatusCode Status, string Body)> WhenThePortfolioProcessBehaviorSeriesIsRequested(int portfolioId, DateOnly? from, DateOnly? to)
        {
            Client.AsPortfolioAdmin(portfolioId);
            var response = await Client.GetAsync($"/api/latest/portfolios/{portfolioId}/metrics/process-behavior-over-time?type={ThroughputType}{BuildWindowQuery(from, to)}");
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// Only appends the bounds a caller actually supplied — an omitted bound must be genuinely absent
        /// from the query string, not sent as an empty or sentinel value, or the "additive" claim is a
        /// fiction.
        /// </summary>
        private static string BuildWindowQuery(DateOnly? from, DateOnly? to)
        {
            var query = string.Empty;

            if (from.HasValue)
            {
                query += $"&startDate={from.Value:yyyy-MM-dd}";
            }

            if (to.HasValue)
            {
                query += $"&endDate={to.Value:yyyy-MM-dd}";
            }

            return query;
        }

        // --- Then ---

        private static void ThenTheSeriesCoversExactlyTheseDays((HttpStatusCode Status, string Body) response, List<DateOnly> expected)
        {
            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.OK),
                $"A windowed series request must be served, not rejected. Body: {response.Body}");

            var actual = ReadRecordedDays(response.Body);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(actual, Has.Count.EqualTo(expected.Count),
                    $"The window must include every recorded day inside it and no day outside it. Body: {response.Body}");
                for (var i = 0; i < expected.Count; i++)
                {
                    Assert.That(actual[i], Is.EqualTo(expected[i].ToString("yyyy-MM-dd")),
                        $"Row {i} must be the expected recorded day, still ordered ascending. Body: {response.Body}");
                }
            }
        }

        private static void ThenTheSeriesIsEmpty((HttpStatusCode Status, string Body) response)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Status, Is.EqualTo(HttpStatusCode.OK),
                    $"A window with nothing recorded inside it is an honest empty series, not an error. Body: {response.Body}");
                Assert.That(ReadRecordedDays(response.Body), Is.Empty,
                    $"No recorded day lies inside the window, so nothing may be returned. Body: {response.Body}");
            }
        }

        private static void ThenTheWindowIsRejectedAsInverted((HttpStatusCode Status, string Body) response)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Status, Is.EqualTo(HttpStatusCode.BadRequest),
                    $"An inverted window must be rejected, never answered with an empty 200 the widget would report as an honest in-range emptiness. Body: {response.Body}");
                Assert.That(response.Body, Does.Contain("Start date must be before end date"),
                    $"The rejection must reuse the controllers' shipped start-before-end message. Body: {response.Body}");
            }
        }

        private static List<string> ReadRecordedDays(string body)
        {
            Assert.That(body.TrimStart(), Does.StartWith("["),
                $"The endpoint must return a JSON array, not HTML/other. Body starts: {body[..Math.Min(60, body.Length)]}");

            using var document = JsonDocument.Parse(body);
            return [.. document.RootElement
                .EnumerateArray()
                .Select(element => element.GetProperty("recordedAt").GetString() ?? string.Empty)];
        }
    }
}
