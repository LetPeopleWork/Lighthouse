using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    /// <summary>
    /// A forecast whose inputs had not changed is still part of the round, and the line says so rather than
    /// leaving the forecast out. The time stays on it, because how long a skip costs on a large instance is
    /// read off exactly this figure.
    /// </summary>
    [TestFixture]
    [Category("story-6354-skip-unchanged-forecasts")]
    [Category("slice-01")]
    public class RefreshRoundSummaryLineSkippedForecastTest
    {
        private const string Pending = "Story 6354: pending until DELIVER words the line for a skipped forecast";

        private static readonly SyncOutcome NothingFetched = new(SyncMode.Delta, 71, 0);

        [Test]
        [Ignore(Pending)]
        public void A_Team_round_whose_forecast_was_skipped_ends_with_the_skip_and_its_time()
        {
            var logger = new RecordingLogger<RefreshRoundSummaryLineSkippedForecastTest>();

            RefreshRoundSummaryLine.Write(logger, new RefreshRoundSummary("Team", "FSS", 16448, true)
            {
                Outcome = NothingFetched,
                ForecastDurationMs = 812,
                ForecastSkipped = true,
            });

            Assert.That(logger.Written(LogLevel.Information).Single(), Is.EqualTo(
                "Update completed | Team 'FSS' | mode=Delta | scanned=71 | fetched=0 | duration=16448ms | success=True | forecast=skipped after 812ms (inputs unchanged)"));
        }

        [Test]
        [Ignore(Pending)]
        public void A_round_that_only_forecast_names_the_skip_where_the_forecast_time_goes()
        {
            var logger = new RecordingLogger<RefreshRoundSummaryLineSkippedForecastTest>();

            RefreshRoundSummaryLine.Write(logger, new RefreshRoundSummary("Portfolio", "Payments Platform", 812, true)
            {
                ForecastDurationMs = 812,
                ForecastSkipped = true,
            });

            Assert.That(logger.Written(LogLevel.Information).Single(), Is.EqualTo(
                "Update completed | Portfolio 'Payments Platform' | forecast=skipped after 812ms (inputs unchanged) | duration=812ms | success=True"));
        }

        [Test]
        [Ignore(Pending)]
        public void A_skip_reported_into_a_refresh_round_reaches_the_line_as_a_skip()
        {
            var round = new WriteBackRound();
            round.ReportRefresh(new RefreshRoundSummary("Team", "FSS", 16448, true) { Outcome = NothingFetched });
            round.ReportForecast(new RefreshRoundSummary("Portfolio", "Payments Platform", 812, true)
            {
                ForecastDurationMs = 812,
                ForecastSkipped = true,
            });

            var summary = round.TakeSummary();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(summary!.ForecastSkipped, Is.True, "The round dropped the fact that its forecast was skipped.");
                Assert.That(summary.ForecastDurationMs, Is.EqualTo(812));
            }
        }

        /// <summary>A forecast that ran keeps the line it has always had.</summary>
        [Test]
        [Category("guard")]
        public void A_computed_forecast_still_reads_as_its_time_alone()
        {
            var logger = new RecordingLogger<RefreshRoundSummaryLineSkippedForecastTest>();

            RefreshRoundSummaryLine.Write(logger, new RefreshRoundSummary("Team", "FSS", 16448, true)
            {
                Outcome = NothingFetched,
                ForecastDurationMs = 269719,
            });

            Assert.That(logger.Written(LogLevel.Information).Single(), Is.EqualTo(
                "Update completed | Team 'FSS' | mode=Delta | scanned=71 | fetched=0 | duration=16448ms | success=True | forecast=269719ms"));
        }
    }
}
