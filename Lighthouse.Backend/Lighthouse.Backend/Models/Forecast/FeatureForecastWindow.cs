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

            return EndAfter(windowStart, worstCaseWorkingDays);
        }

        public static DateTime EndFor(DateTime windowStart, ForecastBase? forecast)
        {
            return EndAfter(windowStart, WorstCaseWorkingDaysOf(forecast));
        }

        // A percentile beyond the horizon has no day of its own, but it lies further out than any that
        // does. Sized as no days, the window would miss the blackouts of the earlier percentiles.
        private static int WorstCaseWorkingDaysOf(ForecastBase? forecast)
        {
            if (forecast is null)
            {
                return 0;
            }

            var reading = forecast.GetPercentile(WorstCasePercentile);

            return reading.IsBeyondHorizon ? ForecastHorizon.Days : reading.Day ?? 0;
        }

        private static DateTime EndAfter(DateTime windowStart, int worstCaseWorkingDays)
        {
            var calendarSpan = (worstCaseWorkingDays * BlackoutDensityMultiplier) + CalendarHeadroomDays;

            return windowStart.AddDays(calendarSpan);
        }
    }
}
