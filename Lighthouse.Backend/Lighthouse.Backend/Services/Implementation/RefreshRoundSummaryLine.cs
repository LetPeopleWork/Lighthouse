using System.Globalization;

namespace Lighthouse.Backend.Services.Implementation
{
    /// <summary>
    /// The one line an operator reads per refresh round. It is written by whichever execution leaves the
    /// round last, and that is usually the refresh itself but can be the update queue, when the last one out
    /// is work that was let go without running.
    /// </summary>
    public static class RefreshRoundSummaryLine
    {
        /// <summary>
        /// The reason and the forecast are appended whole or left out entirely - a cycle with nothing to
        /// explain would otherwise carry an empty <c>reason=</c> on every line, and this line exists precisely
        /// because the logs were too noisy to read.
        /// </summary>
        public static void Write(ILogger logger, RefreshRoundSummary summary)
        {
            var forecast = DescribeForecast(summary);

            if (summary.Outcome is not { } outcome)
            {
                logger.LogInformation(
                    "Update completed | {EntityType:l} '{EntityName:l}'{Forecast:l} | duration={DurationMs}ms | success={Success}",
                    summary.EntityType, summary.EntityName, forecast, summary.DurationMs, summary.Success);
                return;
            }

            var reason = outcome.Reason == null ? string.Empty : $" | reason={outcome.Reason}";

            logger.LogInformation(
                "Update completed | {EntityType:l} '{EntityName:l}' | mode={Mode} | scanned={RecordsScanned} | fetched={RecordsFetched} | duration={DurationMs}ms | success={Success}{Forecast:l}{Reason:l}",
                summary.EntityType, summary.EntityName, outcome.Mode, outcome.RecordsScanned, outcome.RecordsFetched, summary.DurationMs, summary.Success, forecast, reason);
        }

        private static string DescribeForecast(RefreshRoundSummary summary)
        {
            if (summary.ForecastDurationMs is not { } forecastMs)
            {
                return string.Empty;
            }

            if (summary.ForecastSucceeded)
            {
                return string.Create(CultureInfo.InvariantCulture, $" | forecast={forecastMs}ms");
            }

            return summary.ForecastCancelled
                ? string.Create(CultureInfo.InvariantCulture, $" | forecast=cancelled after {forecastMs}ms")
                : string.Create(CultureInfo.InvariantCulture, $" | forecast=failed after {forecastMs}ms");
        }
    }
}
