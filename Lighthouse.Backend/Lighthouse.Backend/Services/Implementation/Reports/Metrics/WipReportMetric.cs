using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Interfaces.Reports;

namespace Lighthouse.Backend.Services.Implementation.Reports.Metrics
{
    /// <summary>
    /// The Metrics tab's WIP over a window, summarised day by day. Every day has a count, so a window with nothing in
    /// progress reads as a real 0 on every line, never absent.
    /// </summary>
    public class WipReportMetric(IReportMetricSeries series) : WindowedReportMetric(series)
    {
        public const string MetricKey = "wip";

        public override string Key => MetricKey;

        protected override Dictionary<string, CapturedValue> ValuesOver(ReportOwner owner, ReportWindow window)
            => DailySeriesSummary.Of(Series.DailyWorkInProgress(owner, window));
    }
}
