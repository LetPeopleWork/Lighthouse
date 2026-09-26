using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.Services.Implementation.Forecast
{
    public static class RealityCheckVerdictPolicy
    {
        public static bool Held(int actualCompleted, int forecastValue) => actualCompleted >= forecastValue;

        public static CellOutcome Outcome(int actualCompleted, IReadOnlyList<RealityCheckForecastDto> forecast)
        {
            var mostConfidentValue = forecast.MaxBy(level => level.Probability)!.Value;
            var leastConfidentValue = forecast.MinBy(level => level.Probability)!.Value;

            if (!Held(actualCompleted, mostConfidentValue))
            {
                return CellOutcome.OverForecast;
            }

            return actualCompleted > leastConfidentValue ? CellOutcome.UnderForecast : CellOutcome.WithinBand;
        }
    }
}
