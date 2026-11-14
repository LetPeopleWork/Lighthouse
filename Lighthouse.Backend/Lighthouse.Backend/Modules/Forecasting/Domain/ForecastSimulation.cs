using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.Modules.Forecasting.Domain
{
    /// <summary>
    /// How a forecast simulates the work: where its random numbers come from, and how far and how often it
    /// runs. The two always travel together, so a forecast takes them as one.
    /// </summary>
    public sealed record ForecastSimulation(IDrawStreamFactory Draws, ForecastSimulationLimits Limits);
}
