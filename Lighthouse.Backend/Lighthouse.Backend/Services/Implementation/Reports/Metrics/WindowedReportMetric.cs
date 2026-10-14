using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Interfaces.Reports;

namespace Lighthouse.Backend.Services.Implementation.Reports.Metrics
{
    /// <summary>
    /// A metric made of named values computed over a window: frozen for Then when the report is created, and computed
    /// again for Now on every read. A value the report never froze is left out rather than read as a zero, so an older
    /// report shows it as not captured.
    /// </summary>
    public abstract class WindowedReportMetric(IReportMetricSeries series) : IReportMetric
    {
        protected IReportMetricSeries Series { get; } = series;

        public abstract string Key { get; }

        public virtual bool AppliesTo(ReportOwnerKind ownerKind) => true;

        public JsonObject CaptureThen(ReportOwner owner, ReportWindow thenWindow)
            => new(ValuesOver(owner, thenWindow).Select(value => KeyValuePair.Create(value.Key, JsonSerializer.SerializeToNode(value.Value, ThenAndNowPayload.JsonOptions))));

        public MetricRead Read(JsonObject? capturedThen, ReportOwner owner, ReportWindow nowWindow)
        {
            var now = ValuesOver(owner, nowWindow);
            return new(FrozenIn(capturedThen, now), now);
        }

        protected abstract Dictionary<string, CapturedValue> ValuesOver(ReportOwner owner, ReportWindow window);

        private static Dictionary<string, CapturedValue> FrozenIn(JsonObject? captured, Dictionary<string, CapturedValue> now)
            => now.Keys
                .Select(member => (Member: member, Frozen: captured?[member]?.Deserialize<CapturedValue>(ThenAndNowPayload.JsonOptions)))
                .Where(frozen => frozen.Frozen is not null)
                .ToDictionary(frozen => frozen.Member, frozen => frozen.Frozen!);
    }
}
