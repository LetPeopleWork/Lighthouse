using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Interfaces.Reports;

namespace Lighthouse.Backend.Services.Implementation.Reports.Metrics
{
    /// <summary>
    /// The Metrics tab's throughput over a window: the total finished and the average per day. Every day of a window
    /// has a count, so a window where nothing finished is a real 0, never absent.
    /// </summary>
    public class ThroughputReportMetric(IReportMetricSeries series) : WindowedReportMetric(series)
    {
        public const string MetricKey = "throughput";

        private const string Total = "total";
        private const string PerDay = "perDay";

        public override string Key => MetricKey;

        protected override Dictionary<string, CapturedValue> ValuesOver(ReportOwner owner, ReportWindow window)
        {
            var total = Series.DailyThroughput(owner, window).Sum();
            var days = window.LengthDays;

            return new()
            {
                [Total] = CapturedValue.Shown(total, days),
                [PerDay] = CapturedValue.Shown((double)total / days, days),
            };
        }
    }
}
