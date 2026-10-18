namespace Lighthouse.Backend.Models.Forecast
{
    /// <param name="Trials">How many times a forecast simulates the work before reading percentiles off it.</param>
    /// <param name="MostDaysOneSimulatedRunMayCover">
    /// A last-resort ceiling. A run ends when the work is done, or when nothing is left that could be
    /// started; it is not meant to reach this and no data can make it. It exists so that a mistake in how
    /// runs end costs minutes of one background refresh rather than a thread that never comes back.
    /// </param>
    public sealed record ForecastSimulationLimits(int Trials, int MostDaysOneSimulatedRunMayCover)
    {
        public static ForecastSimulationLimits Default { get; } = new(10_000, 100_000);

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
