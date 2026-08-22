using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Implementation
{
    /// <summary>
    /// What one refresh round has to tell an operator it did. A round can be more than one execution -
    /// a portfolio refresh and the forecast it asks for - and those executions together are one thing
    /// that happened, so they add up to one summary rather than one each.
    /// </summary>
    public sealed record RefreshRoundSummary(string EntityType, string EntityName, long DurationMs, bool Success)
    {
        /// <summary>
        /// What was fetched from the work tracking system, or null when this round never contacted it.
        /// Forecasting reads what an earlier refresh already downloaded, so a round that only forecast
        /// has no records to report, and saying it scanned nothing would read as if it had tried.
        /// </summary>
        public SyncOutcome? Outcome { get; init; }

        /// <summary>
        /// How long the forecast in this round took, or null when the round ran none. The time it cost
        /// is the whole of what a forecast adds to the line.
        /// </summary>
        public long? ForecastDurationMs { get; init; }

        public bool ForecastSucceeded { get; init; } = true;
    }
}
