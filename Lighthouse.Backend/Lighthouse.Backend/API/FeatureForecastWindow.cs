using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API
{
    internal static class FeatureForecastWindow
    {
        private const int CalendarHeadroomDays = 14;
        private const int BlackoutDensityMultiplier = 2;
        private const int WorstCasePercentile = 95;

        public static DateTime EndFor(IEnumerable<Feature> features)
        {
            var worstCaseWorkingDays = features
                .Select(feature => feature.Forecast?.GetProbability(WorstCasePercentile) ?? 0)
                .DefaultIfEmpty(0)
                .Max();

            var calendarSpan = (worstCaseWorkingDays * BlackoutDensityMultiplier) + CalendarHeadroomDays;

            return DateTime.UtcNow.Date.AddDays(calendarSpan);
        }
    }
}
