using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Interfaces.Forecast;

namespace Lighthouse.Backend.Services.Implementation.Forecast
{
    /// <summary>
    /// How a forecast simulates the work: where its random numbers come from, and how far and how often it
    /// runs. The two always travel together, so a forecast takes them as one.
    /// </summary>
    public sealed record ForecastSimulation(IDrawStreamFactory Draws, ForecastSimulationLimits Limits);
}
