namespace Lighthouse.Backend.Models.Forecast
{
    internal static class FeatureForecastWindow
    {
        private const int CalendarHeadroomDays = 14;
        private const int BlackoutDensityMultiplier = 2;
        private const int WorstCasePercentile = 95;

        public static DateTime EndFor(DateTime windowStart, IEnumerable<Feature> features)
        {
            var worstCaseWorkingDays = features
                .Select(feature => WorstCaseWorkingDaysOf(feature.Forecast))
                .DefaultIfEmpty(0)
                .Max();

            return EndFor(windowStart, worstCaseWorkingDays);
        }

        public static DateTime EndFor(DateTime windowStart, WhenForecast? forecast)
        {
            return EndFor(windowStart, WorstCaseWorkingDaysOf(forecast));
        }

        public static DateTime EndFor(DateTime windowStart, int workingDays)
        {
            var calendarSpan = (workingDays * BlackoutDensityMultiplier) + CalendarHeadroomDays;

            return windowStart.AddDays(calendarSpan);
        }

        // A percentile beyond the horizon has no day of its own, but it lies further out than any that
        // does. Sized as no days, the window would miss the blackouts of the earlier percentiles.
        private static int WorstCaseWorkingDaysOf(WhenForecast? forecast)
        {
            if (forecast is null)
            {
                return 0;
            }

            var reading = forecast.GetPercentile(WorstCasePercentile);

            return reading.IsBeyondHorizon ? ForecastHorizon.Days : reading.Day ?? 0;
        }
    }
}
