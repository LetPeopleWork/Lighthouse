using Lighthouse.Backend.Models.Reports;

namespace Lighthouse.Backend.Services.Implementation.Reports.Metrics
{
    /// <summary>
    /// One value per day of a window, summarised as its average, median, min and max. Every value rests on every day
    /// of the window, so a day with nothing to count takes part as a real 0. The median is the Metrics tab's 50th
    /// percentile, so a report and the tab agree: the lower of the two middle days for an even number of days, and
    /// the day just below the middle for an odd number.
    /// </summary>
    public static class DailySeriesSummary
    {
        public const string Average = "average";
        public const string Median = "median";
        public const string Min = "min";
        public const string Max = "max";

        public static Dictionary<string, CapturedValue> Of(IReadOnlyList<int> daily)
        {
            var days = daily.Count;

            return new()
            {
                [Average] = CapturedValue.Shown(daily.Average(), days),
                [Median] = CapturedValue.Shown(PercentileCalculator.CalculatePercentile([.. daily], 50), days),
                [Min] = CapturedValue.Shown(daily.Min(), days),
                [Max] = CapturedValue.Shown(daily.Max(), days),
            };
        }
    }
}
