using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Interfaces.Reports;

namespace Lighthouse.Backend.Services.Implementation.Reports.Metrics
{
    /// <summary>
    /// The Metrics tab's cycle-time percentiles over a window. Then keeps the 85th alone; Now shows the 85th,
    /// 70th and 50th. Each rests on the finished items with a cycle time in its window.
    /// </summary>
    public class CycleTimeReportMetric(IReportMetricSeries series) : IReportMetric
    {
        public const string MetricKey = "cycle-time";

        public const string NoFinishedItems = "no-finished-items";

        private const string P85 = "p85";
        private const string P70 = "p70";
        private const string P50 = "p50";

        public string Key => MetricKey;

        public bool AppliesTo(ReportOwnerKind ownerKind) => true;

        public JsonObject CaptureThen(ReportOwner owner, ReportWindow thenWindow)
        {
            var percentiles = series.CycleTimePercentiles(owner, thenWindow);
            return new JsonObject
            {
                [P85] = JsonValueOf(ValueOf(percentiles.P85, percentiles.FinishedItems)),
            };
        }

        public MetricRead Read(JsonObject? capturedThen, ReportOwner owner, ReportWindow nowWindow)
        {
            var atThen = new Dictionary<string, CapturedValue>();
            if (capturedThen?[P85] is JsonNode frozen && frozen.Deserialize<CapturedValue>(ThenAndNowPayload.JsonOptions) is CapturedValue thenP85)
            {
                atThen[P85] = thenP85;
            }

            var now = series.CycleTimePercentiles(owner, nowWindow);
            var atNow = new Dictionary<string, CapturedValue>
            {
                [P85] = ValueOf(now.P85, now.FinishedItems),
                [P70] = ValueOf(now.P70, now.FinishedItems),
                [P50] = ValueOf(now.P50, now.FinishedItems),
            };

            return new MetricRead(atThen, atNow);
        }

        private static CapturedValue ValueOf(int percentile, int finishedItems)
            => finishedItems > 0 ? CapturedValue.Shown(percentile, finishedItems) : CapturedValue.Absent(NoFinishedItems);

        private static JsonNode? JsonValueOf(CapturedValue value) => JsonSerializer.SerializeToNode(value, ThenAndNowPayload.JsonOptions);
    }
}
