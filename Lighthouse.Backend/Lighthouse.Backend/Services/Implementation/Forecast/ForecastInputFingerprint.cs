using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation.Forecast
{
    /// <summary>
    /// Everything one Portfolio forecast is about to hand the simulation, as the simulation would receive it.
    /// Two runs handed equal inputs compute the same forecast, randomness aside, so the second need not run.
    /// </summary>
    /// <param name="Today">The instance day the forecast counts from.</param>
    /// <param name="Limits">How many runs, how far each may go, and how many at once.</param>
    /// <param name="FeaturesInForecastOrder">The Features simulated, in the order their Teams work on them.</param>
    /// <param name="ThroughputByTeam">Per Team the forecast reads, the throughput and chip status it is handed.</param>
    /// <param name="Waits">What each Feature has to wait for before its work can begin.</param>
    public sealed record ForecastInputs(
        DateOnly Today,
        ForecastSimulationLimits Limits,
        IReadOnlyList<Feature> FeaturesInForecastOrder,
        IReadOnlyDictionary<int, ForecastThroughputStatus> ThroughputByTeam,
        ForecastWaits Waits);

    // __SCAFFOLD__: acceptance tests were written ahead of this; DELIVER replaces the member list and the body.
    public static class ForecastInputFingerprint
    {
        public static IReadOnlyList<string> RegisteredMembers { get; } = [];

        public static string Of(ForecastInputs inputs)
        {
            throw new NotImplementedException($"The fingerprint of {inputs.FeaturesInForecastOrder.Count} Features is not written yet");
        }
    }
}
