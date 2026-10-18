namespace Lighthouse.Backend.Models.Forecast
{
    /// <summary>
    /// One percentile read off a forecast. It falls on a day, or too few of the runs finished for it to
    /// have one and it lies beyond the horizon, or nothing was simulated at all and there is no answer.
    /// The last two used to share one number, which made a Feature nobody could finish read as due today.
    /// </summary>
    public readonly record struct ForecastPercentile
    {
        private ForecastPercentile(int? day, bool isBeyondHorizon)
        {
            Day = day;
            IsBeyondHorizon = isBeyondHorizon;
        }

        public static ForecastPercentile BeyondTheHorizon { get; } = new(null, true);

        public static ForecastPercentile NoAnswer { get; } = new(null, false);

        /// <summary>The day the percentile falls on, or nothing when it has no day.</summary>
        public int? Day { get; }

        public bool IsBeyondHorizon { get; }

        public static ForecastPercentile On(int day) => new(day, false);
    }
}
