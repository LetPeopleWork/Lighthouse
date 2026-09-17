using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Tests.TestHelpers;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.SleRisk
{
    /// <summary>
    /// DISTILL acceptance scenarios for Epic #4127 slice 03 / ADO Story #6014. Driving port:
    /// GET /api/latest/teams/{id}/metrics/sleRisk/zones.
    ///
    /// The column answers "what is this item's risk". These answer a different question the chart
    /// needs and no arrangement of the column's payload can supply: at which ages does the risk cross
    /// each level, including ages no item currently sits at.
    ///
    /// What the chart does with them - horizontal bands, their colours, and the control that turns
    /// them on - is a claim about a drawing and lives in WorkItemAgingChart.test.tsx.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-4127-sle-risk")]
    [Category("slice-03")]
    public class Slice03SleRiskZonesTest : SleRiskAcceptanceTest
    {
        private HttpResponseMessage response = null!;

        private string body = string.Empty;

        private int TheTeamUnderTest { get; set; }

        [TearDown]
        public void DisposeResponse() => response?.Dispose();

        // @walking_skeleton @driving_port @real-io @AC-03.2 — the whole slice in one scenario. The
        // chart needs four ages; the team's own history is what puts them where they are.
        [Test]
        public async Task A_team_with_a_target_is_told_where_the_odds_turn_against_an_item()
        {
            GivenATeamPromising(10);
            GivenTheTeamHasFinishedSeveralOfEach(3, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 8, 9, 11, 13, 15, 18, 22, 30);

            await WhenTheZonesAreAskedFor();

            ThenTheZonesBeginAt((25, 1), (50, 6), (75, 8), (100, 10));
        }

        // @driving_port @real-io @AC-03.2 — a boundary is only a boundary if the one above it is
        // higher up the chart. Out of order, the bands would overlap and paint over each other.
        [Test]
        public async Task The_bands_are_stacked_calmest_at_the_bottom()
        {
            GivenATeamPromising(10);
            GivenTheTeamHasFinishedSeveralOfEach(3, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 8, 9, 11, 13, 15, 18, 22, 30);

            await WhenTheZonesAreAskedFor();

            ThenEachBandStartsAboveTheOneBelowIt();
        }

        // @driving_port @real-io @error @AC-03.4 — no promise, nothing to be at risk of breaking, and
        // no geometry that could mean anything.
        [Test]
        public async Task A_team_that_never_published_a_target_has_no_bands_at_all()
        {
            GivenATeamPromising(0);
            GivenTheTeamHasFinishedSeveralOfEach(3, 2, 4, 6, 12, 14);

            await WhenTheZonesAreAskedFor();

            ThenThereAreNoBands();
        }

        // @driving_port @real-io @error — the band edges are where the evidence thins out fastest, so
        // this is the case that decides whether the chart tells the truth or decorates it.
        [Test]
        public async Task Bands_the_history_cannot_place_are_left_undrawn()
        {
            GivenATeamPromising(10);
            // Twelve items took two days; only three ever ran longer. Nothing can be said about an
            // age above two, so no band may begin there.
            GivenTheTeamHasFinishedSeveralOfEach(12, 2);
            GivenTheTeamHasFinished(11, 12, 13);

            await WhenTheZonesAreAskedFor();

            ThenNoBandBeginsLaterThan(2);
        }

        // @driving_port @real-io @AC-03.2 — two windows, one team, one after the other. The answer is
        // remembered between requests, so a memory that ignores which window was asked about hands
        // the chart the previous window's geometry and nothing on screen would say so.
        [Test]
        public async Task Two_windows_asked_one_after_the_other_get_their_own_bands()
        {
            GivenATeamPromising(10);
            GivenTheTeamHasFinishedSeveralOfEach(12, 2);
            GivenTheTeamHasFinished(11, 12, 13);

            await WhenTheZonesAreAskedFor();
            var first = TheBands();

            GivenTheTeamNowPromises(1);

            await WhenTheZonesAreAskedFor();

            Assert.That(TheBands(), Is.Not.EqualTo(first),
                $"A one-day target puts the odds against an item far sooner than a ten-day one. Body: {body}");
        }

        // @driving_port @real-io @error — a window that ends before it starts describes no period at
        // all, and an empty band list would read as "the odds never turn".
        [Test]
        public async Task A_window_that_ends_before_it_starts_is_refused()
        {
            GivenATeamPromising(10);
            GivenTheTeamHasFinishedSeveralOfEach(12, 2);

            await WhenTheZonesAreAskedForABackwardsWindow();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"Body: {body}");
        }

        // @driving_port @real-io — a window of one day is a question about one day, not a malformed
        // one. The guard that rejects a backwards range is one character away from rejecting this too.
        [Test]
        public async Task A_window_of_one_day_is_a_question_like_any_other()
        {
            GivenATeamPromising(10);
            GivenTheTeamHasFinishedSeveralOfEach(12, 2);

            await WhenTheZonesAreAskedForASingleDay();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Body: {body}");
        }

        // @driving_port @real-io @error @AC-01.6 — portfolios are out of this Epic for the same reason
        // the per-item read is: several targets, several histories, one chart.
        [Test]
        public async Task Portfolios_are_not_asked_this_question_either()
        {
            await WhenTheZonesAreAskedForAPortfolio();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
                $"A feature sits in several portfolios, each with its own target. Body: {body}");
        }

        // --- Given ---

        private void GivenATeamPromising(int rangeInDays) => TheTeamUnderTest = SeedTeamWithTarget(rangeInDays);

        private void GivenTheTeamHasFinished(params int[] cycleTimesInDays)
            => SeedFinishedItems(TheTeamUnderTest, cycleTimesInDays);

        private void GivenTheTeamNowPromises(int rangeInDays)
            => ChangeTheTargetOf(TheTeamUnderTest, rangeInDays);

        private void GivenTheTeamHasFinishedSeveralOfEach(int copies, params int[] cycleTimesInDays)
        {
            for (var copy = 0; copy < copies; copy++)
            {
                SeedFinishedItems(TheTeamUnderTest, cycleTimesInDays);
            }
        }

        // --- When ---

        private async Task WhenTheZonesAreAskedFor()
        {
            Client.AsTeamAdmin(TheTeamUnderTest);
            response = await Client.GetAsync(SleRiskZonesRoute(TheTeamUnderTest));
            body = await response.Content.ReadAsStringAsync();
        }

        private async Task WhenTheZonesAreAskedForASingleDay()
        {
            Client.AsTeamAdmin(TheTeamUnderTest);
            response = await Client.GetAsync(new Uri(
                $"/api/latest/teams/{TheTeamUnderTest}/metrics/sleRisk/zones"
                + $"?startDate={WindowEnd:yyyy-MM-dd}&endDate={WindowEnd:yyyy-MM-dd}",
                UriKind.Relative));
            body = await response.Content.ReadAsStringAsync();
        }

        private async Task WhenTheZonesAreAskedForABackwardsWindow()
        {
            Client.AsTeamAdmin(TheTeamUnderTest);
            response = await Client.GetAsync(new Uri(
                $"/api/latest/teams/{TheTeamUnderTest}/metrics/sleRisk/zones"
                + $"?startDate={WindowEnd:yyyy-MM-dd}&endDate={WindowStart:yyyy-MM-dd}",
                UriKind.Relative));
            body = await response.Content.ReadAsStringAsync();
        }

        private async Task WhenTheZonesAreAskedForAPortfolio()
        {
            Client.AsViewer();
            response = await Client.GetAsync(new Uri(
                $"/api/latest/portfolios/1/metrics/sleRisk/zones"
                + $"?startDate={WindowStart:yyyy-MM-dd}&endDate={WindowEnd:yyyy-MM-dd}",
                UriKind.Relative));
            body = await response.Content.ReadAsStringAsync();
        }

        // --- Then ---

        private void ThenTheZonesBeginAt(params (int Risk, int FromAge)[] expected)
        {
            var bands = TheBands();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(bands.Select(band => band.Risk), Is.EqualTo(expected.Select(e => e.Risk)),
                    $"Body: {body}");
                Assert.That(bands.Select(band => band.FromAge), Is.EqualTo(expected.Select(e => e.FromAge)),
                    $"Body: {body}");
            }
        }

        private void ThenEachBandStartsAboveTheOneBelowIt()
        {
            var ages = TheBands().Select(band => band.FromAge).ToList();

            Assert.That(ages, Is.Ordered, $"Body: {body}");
        }

        private void ThenThereAreNoBands()
        {
            Assert.That(TheBands(), Is.Empty, $"Body: {body}");
        }

        private void ThenNoBandBeginsLaterThan(int ageInDays)
        {
            Assert.That(TheBands().Select(band => band.FromAge), Has.All.LessThanOrEqualTo(ageInDays),
                $"Above the last age with enough evidence there is nothing to draw. Body: {body}");
        }

        private List<(int Risk, int FromAge)> TheBands()
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Body: {body}");

            using var document = JsonDocument.Parse(body);

            return [.. document.RootElement.EnumerateArray().Select(entry =>
                (entry.GetProperty("risk").GetInt32(), entry.GetProperty("fromAge").GetInt32()))];
        }
    }
}
