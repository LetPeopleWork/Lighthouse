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

        public static RealityCheckLevelCoverageDto Coverage(int confidenceLevel, int heldCount, int runsEvaluated)
        {
            var expectedHeldCount = runsEvaluated * confidenceLevel / 100.0;

            return new RealityCheckLevelCoverageDto(
                confidenceLevel, heldCount, expectedHeldCount, Reading(heldCount, runsEvaluated, expectedHeldCount));
        }

        // Never holding, or always holding, only says something when the level's own rate predicted at least
        // one whole check going the other way; below that, the extreme is what the level predicted.
        private static LevelReading Reading(int heldCount, int runsEvaluated, double expectedHeldCount)
        {
            if (runsEvaluated == 0)
            {
                return LevelReading.NotEvaluated;
            }

            if (heldCount == 0 && expectedHeldCount >= 1)
            {
                return LevelReading.NeverHeld;
            }

            if (heldCount == runsEvaluated && runsEvaluated - expectedHeldCount >= 1)
            {
                return LevelReading.AlwaysHeld;
            }

            return LevelReading.SometimesHeld;
        }
    }
}
