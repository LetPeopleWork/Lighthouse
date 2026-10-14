using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Interfaces.Reports;

namespace Lighthouse.Backend.Services.Implementation.Reports.Metrics
{
    /// <summary>
    /// The Metrics tab's cycle-time percentiles over a window: the 85th, 70th and 50th. Each rests on the finished
    /// items with a cycle time in its window. A report frozen before Then kept all three simply has no Then value for
    /// the ones it lacks.
    /// </summary>
    public class CycleTimeReportMetric(IReportMetricSeries series) : WindowedReportMetric(series)
    {
        public const string MetricKey = "cycle-time";

        public const string NoFinishedItems = "no-finished-items";

        private const string P85 = "p85";
        private const string P70 = "p70";
        private const string P50 = "p50";

        public override string Key => MetricKey;

        protected override Dictionary<string, CapturedValue> ValuesOver(ReportOwner owner, ReportWindow window)
        {
            var percentiles = Series.CycleTimePercentiles(owner, window);

            return new()
            {
                [P85] = ValueOf(percentiles.P85, percentiles.FinishedItems),
                [P70] = ValueOf(percentiles.P70, percentiles.FinishedItems),
                [P50] = ValueOf(percentiles.P50, percentiles.FinishedItems),
            };
        }

        private static CapturedValue ValueOf(int percentile, int finishedItems)
            => finishedItems > 0 ? CapturedValue.Shown(percentile, finishedItems) : CapturedValue.Absent(NoFinishedItems);
    }
}
