namespace Lighthouse.Backend.Models.Forecast
{
    /// <param name="Trials">How many times a forecast simulates the work before reading percentiles off it.</param>
    /// <param name="MostDaysOneSimulatedRunMayCover">
    /// How far one run is simulated before it stops. A run that has not finished the work by then counts as
    /// finishing beyond the horizon, so the product sets this to the horizon itself: simulating further
    /// costs time and changes no answer anyone is shown.
    /// </param>
    public sealed record ForecastSimulationLimits(int Trials, int MostDaysOneSimulatedRunMayCover)
    {
        public static ForecastSimulationLimits Default { get; } = new(10_000, ForecastHorizon.Days);

        /// <summary>
        /// The cores this instance was given. .NET reads it from the container's CPU quota; a test sets it so
        /// the number of runs allowed at once does not depend on the machine the test happens to run on.
        /// </summary>
        public int CoresSeen { get; init; } = Environment.ProcessorCount;

        /// <summary>
        /// How many simulated runs one forecast may have under way at once: half the cores, and never fewer
        /// than one. The other half stays free so that pages keep answering while a forecast runs. The bound
        /// belongs to each forecast on its own, so a manual forecast asked for during a background one gets
        /// its own half rather than queueing behind it.
        /// </summary>
        public int MostRunsAtOnce => Math.Max(1, CoresSeen / 2);
    }
}
