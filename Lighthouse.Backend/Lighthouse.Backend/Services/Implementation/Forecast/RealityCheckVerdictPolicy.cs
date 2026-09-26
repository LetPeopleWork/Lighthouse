using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.Services.Implementation.Forecast
{
    public static class RealityCheckVerdictPolicy
    {
        public static bool Held(int actualCompleted, int forecastValue) => actualCompleted >= forecastValue;

        // The forecast engine answers minus one for a level when its simulation holds nothing to read that level
        // from, so a negative value is the engine saying it cannot tell, never a number of items.
        public static bool HasAReadingAtEveryLevel(IReadOnlyList<RealityCheckForecastDto> forecast)
            => forecast.All(level => level.Value >= 0);

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

        public static RealityCheckSoundWindowDto SoundWindows(
            IReadOnlyList<int> sampledWindowDays, IReadOnlyList<RealityCheckCellDto> cells, int currentSettingDays)
        {
            var windowStates = sampledWindowDays
                .Select(windowDays => StateOf(cells.Where(cell => cell.SamplingWindowDays == windowDays)))
                .ToList();
            var soundWindowDays = sampledWindowDays
                .Where((_, index) => windowStates[index] == WindowState.HoldsUp)
                .ToList();

            return new RealityCheckSoundWindowDto(
                soundWindowDays,
                [],
                DeterminationOf(windowStates),
                currentSettingDays,
                true,
                soundWindowDays.Contains(currentSettingDays) ? CurrentSettingStanding.Inside : CurrentSettingStanding.Outside,
                null);
        }

        // Only falling short of the most cautious forecast counts against a window. The band's top edge is the
        // median forecast, so landing above it is what half of a well-judged forecast's checks do: a coin flip,
        // not a finding.
        private static WindowState StateOf(IEnumerable<RealityCheckCellDto> windowCells)
        {
            var heldAtMostCautiousLevel = windowCells
                .Where(cell => cell.Sufficiency.IsSufficient)
                .Select(cell => cell.LevelOutcomes!.MaxBy(outcome => outcome.ConfidenceLevel)!.Held)
                .ToList();

            if (heldAtMostCautiousLevel.Count == 0)
            {
                return WindowState.NotEvaluated;
            }

            var timesHeld = heldAtMostCautiousLevel.Count(held => held);

            return timesHeld * 2 > heldAtMostCautiousLevel.Count ? WindowState.HoldsUp : WindowState.DoesNotHoldUp;
        }

        private static Determination DeterminationOf(List<WindowState> windowStates)
        {
            if (windowStates.TrueForAll(state => state == WindowState.NotEvaluated))
            {
                return Determination.NotEnoughEvidence;
            }

            if (windowStates.TrueForAll(state => state == WindowState.HoldsUp))
            {
                return Determination.AllWindowsAlike;
            }

            return windowStates.Contains(WindowState.HoldsUp) ? Determination.SomeWindowsSound : Determination.NoWindowSound;
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

        private enum WindowState
        {
            HoldsUp,
            DoesNotHoldUp,
            NotEvaluated,
        }
    }
}
