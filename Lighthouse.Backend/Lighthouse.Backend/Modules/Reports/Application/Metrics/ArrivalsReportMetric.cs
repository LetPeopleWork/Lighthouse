using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Interfaces.Reports;

namespace Lighthouse.Backend.Modules.Reports.Application.Metrics
{
    /// <summary>
    /// The Work Items started in a window against those finished: the total started, the average started and finished per
    /// day, and the gap between the two averages (negative when more finished than started). Every day of a window has a
    /// count, so a window where nothing started or finished is a real value, never absent.
    /// </summary>
    public class ArrivalsReportMetric(IReportMetricSeries series) : WindowedReportMetric(series)
    {
        public const string MetricKey = "arrivals";

        private const string Total = "total";
        private const string PerDay = "perDay";
        private const string ThroughputPerDay = "throughputPerDay";
        private const string GapPerDay = "gapPerDay";

        public override string Key => MetricKey;

        protected override Dictionary<string, CapturedValue> ValuesOver(ReportOwner owner, ReportWindow window)
        {
            var started = Series.DailyArrivals(owner, window);
            var finished = Series.DailyThroughput(owner, window);
            var days = started.Count;
            var total = started.Sum();

            var startedPerDay = AveragePerDay(started);
            var finishedPerDay = AveragePerDay(finished);

            return new()
            {
                [Total] = CapturedValue.Shown(total, days),
                [PerDay] = CapturedValue.Shown(startedPerDay, days),
                [ThroughputPerDay] = CapturedValue.Shown(finishedPerDay, days),
                [GapPerDay] = CapturedValue.Shown(startedPerDay - finishedPerDay, days),
            };
        }
    }
}
