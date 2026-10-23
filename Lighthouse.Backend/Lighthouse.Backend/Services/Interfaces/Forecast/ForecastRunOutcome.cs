namespace Lighthouse.Backend.Services.Interfaces.Forecast
{
    /// <summary>
    /// What a Portfolio's forecast run did. A run either simulated and saved new forecasts, or found the
    /// inputs identical to the ones its stored forecasts were computed from and left them standing.
    /// </summary>
    public abstract record ForecastRunOutcome
    {
        private ForecastRunOutcome()
        {
        }

        /// <param name="ReadFingerprint">The recorded inputs the run found when it started, if any.</param>
        /// <param name="FreshFingerprint">The inputs this run computed from, or null when they could not be told apart.</param>
        public sealed record Computed(string? ReadFingerprint, string? FreshFingerprint) : ForecastRunOutcome;

        /// <param name="FeatureIds">The Features whose stored forecasts still hold.</param>
        public sealed record ConfirmedUnchanged(IReadOnlyCollection<int> FeatureIds) : ForecastRunOutcome;
    }
}
