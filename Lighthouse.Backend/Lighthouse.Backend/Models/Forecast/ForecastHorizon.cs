namespace Lighthouse.Backend.Models.Forecast
{
    /// <summary>
    /// How far ahead a forecast looks. A percentile that fewer runs reached than it asks for falls beyond
    /// this, and is worded in these years rather than given a date nobody should plan on.
    /// </summary>
    public static class ForecastHorizon
    {
        public const int Years = 5;

        /// <summary>
        /// The horizon in simulated days, counting the most leap days any run of that many calendar years
        /// can hold, so the days never fall short of the years they are reported as.
        /// </summary>
        public const int Days = Years * 365 + (Years + 3) / 4;
    }
}
