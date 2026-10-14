using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Interfaces.Reports;

namespace Lighthouse.Backend.Services.Implementation.Reports.Metrics
{
    /// <summary>
    /// The Metrics tab's Total Work Item Age over a window, summarised day by day. Every day has a total, so a window
    /// with nothing in progress reads as a real 0 on every line, never absent.
    /// </summary>
    public class WorkItemAgeReportMetric(IReportMetricSeries series) : WindowedReportMetric(series)
    {
        public const string MetricKey = "work-item-age";

        public override string Key => MetricKey;

        protected override Dictionary<string, CapturedValue> ValuesOver(ReportOwner owner, ReportWindow window)
            => DailySeriesSummary.Of(Series.DailyTotalWorkItemAge(owner, window));
    }
}
