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

        /// <summary>How long the run took to read the Features and throughput it forecasts from, and to fingerprint them.</summary>
        public long GatherMs { get; init; }

        /// <summary>How long the simulation took; zero when the run found nothing to compute.</summary>
        public long SimulateMs { get; init; }

        /// <summary>How long it took to save the new forecasts; zero when the run found nothing to compute.</summary>
        public long SaveMs { get; init; }

        /// <summary>
        /// The timings say how long a run took, not what it did, so two runs that did the same thing are equal
        /// however long each one took.
        /// </summary>
        public virtual bool Equals(ForecastRunOutcome? other) => other is not null && EqualityContract == other.EqualityContract;

        public override int GetHashCode() => EqualityContract.GetHashCode();

        /// <param name="RunMark">What the run put in place of the recorded inputs before it saved, or null when somebody changed them first.</param>
        /// <param name="FreshFingerprint">The inputs this run computed from, or null when they could not be told apart.</param>
        public sealed record Computed(string? RunMark, string? FreshFingerprint) : ForecastRunOutcome;

        /// <param name="FeatureIds">The Features whose stored forecasts still hold.</param>
        public sealed record ConfirmedUnchanged(IReadOnlyCollection<int> FeatureIds) : ForecastRunOutcome;
    }
}
