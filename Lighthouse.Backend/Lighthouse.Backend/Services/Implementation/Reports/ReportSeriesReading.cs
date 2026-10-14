using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Models.Reports;

namespace Lighthouse.Backend.Services.Implementation.Reports
{
    /// <summary>How every owner's series turns a report window into the days the metrics services read, and a run chart into one count per day.</summary>
    internal static class ReportSeriesReading
    {
        public static (DateTime Start, DateTime End) DaysOf(ReportWindow window)
            => (window.Start.ToDateTime(TimeOnly.MinValue), window.End.ToDateTime(TimeOnly.MinValue));

        public static int[] DailyCounts(RunChartData runChart)
            => [.. runChart.WorkItemsPerUnitOfTime.OrderBy(day => day.Key).Select(day => day.Value.Count)];
    }
}
