using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Interfaces.Reports;

namespace Lighthouse.Backend.Services.Implementation.Reports.Metrics
{
    /// <summary>
    /// The Metrics tab's cycle-time percentiles over a window: the 85th, 70th and 50th, frozen for Then and computed
    /// for Now. Each rests on the finished items with a cycle time in its window. A report frozen before Then kept all
    /// three simply has no Then value for the ones it lacks.
    /// </summary>
    public class CycleTimeReportMetric(IReportMetricSeries series) : IReportMetric
    {
        public const string MetricKey = "cycle-time";

        public const string NoFinishedItems = "no-finished-items";

        private const string P85 = "p85";
        private const string P70 = "p70";
        private const string P50 = "p50";

        private static readonly string[] Percentiles = [P85, P70, P50];

        public string Key => MetricKey;

        public bool AppliesTo(ReportOwnerKind ownerKind) => true;

        public JsonObject CaptureThen(ReportOwner owner, ReportWindow thenWindow)
            => new(ValuesOf(series.CycleTimePercentiles(owner, thenWindow))
                .Select(value => KeyValuePair.Create(value.Key, JsonSerializer.SerializeToNode(value.Value, ThenAndNowPayload.JsonOptions))));

        public MetricRead Read(JsonObject? capturedThen, ReportOwner owner, ReportWindow nowWindow)
        {
            var atThen = Percentiles
                .Select(member => (Member: member, Frozen: capturedThen?[member]?.Deserialize<CapturedValue>(ThenAndNowPayload.JsonOptions)))
                .Where(captured => captured.Frozen is not null)
                .ToDictionary(captured => captured.Member, captured => captured.Frozen!);

            return new MetricRead(atThen, ValuesOf(series.CycleTimePercentiles(owner, nowWindow)));
        }

        private static Dictionary<string, CapturedValue> ValuesOf(CycleTimePercentiles percentiles) => new()
        {
            [P85] = ValueOf(percentiles.P85, percentiles.FinishedItems),
            [P70] = ValueOf(percentiles.P70, percentiles.FinishedItems),
            [P50] = ValueOf(percentiles.P50, percentiles.FinishedItems),
        };

        private static CapturedValue ValueOf(int percentile, int finishedItems)
            => finishedItems > 0 ? CapturedValue.Shown(percentile, finishedItems) : CapturedValue.Absent(NoFinishedItems);
    }
}
