using System.Text.Json.Nodes;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Interfaces.Reports;

namespace Lighthouse.Backend.Services.Implementation.Reports.Metrics
{
    /// <summary>
    /// The Metrics tab's throughput over a window: the total finished and the average per day, frozen for Then and
    /// computed for Now. Every day of a window has a count, so a window where nothing finished is a real 0, never absent.
    /// </summary>
    public class ThroughputReportMetric(IReportMetricSeries series) : IReportMetric
    {
        public const string MetricKey = "throughput";

        private const string Total = "total";
        private const string PerDay = "perDay";

        private static readonly string[] Members = [Total, PerDay];

        public string Key => MetricKey;

        public bool AppliesTo(ReportOwnerKind ownerKind) => true;

        public JsonObject CaptureThen(ReportOwner owner, ReportWindow thenWindow)
            => CapturedMembers.ToJson(ValuesOf(owner, thenWindow));

        public MetricRead Read(JsonObject? capturedThen, ReportOwner owner, ReportWindow nowWindow)
            => new(CapturedMembers.FromJson(capturedThen, Members), ValuesOf(owner, nowWindow));

        private Dictionary<string, CapturedValue> ValuesOf(ReportOwner owner, ReportWindow window)
        {
            var total = series.DailyThroughput(owner, window).Sum();
            var days = window.LengthDays;

            return new()
            {
                [Total] = CapturedValue.Shown(total, days),
                [PerDay] = CapturedValue.Shown((double)total / days, days),
            };
        }
    }
}
