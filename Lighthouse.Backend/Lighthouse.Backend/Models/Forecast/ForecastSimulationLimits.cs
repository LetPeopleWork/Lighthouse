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

        // SCAFFOLD: not implemented yet. -1 is what Parallel.For reads as "no limit", today's behaviour; the
        // acceptance tests for keeping pages answering expect a bound here.
        public int MostRunsAtOnce { get; init; } = -1;
    }
}
