using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.Models.Reports;

namespace Lighthouse.Backend.Services.Implementation.Reports.Metrics
{
    /// <summary>
    /// How a metric's values are frozen in a report and read back. A member the report never froze is left out
    /// rather than read as a zero, so an older report shows it as not captured.
    /// </summary>
    internal static class CapturedMembers
    {
        public static JsonObject ToJson(Dictionary<string, CapturedValue> values)
            => new(values.Select(value => KeyValuePair.Create(value.Key, JsonSerializer.SerializeToNode(value.Value, ThenAndNowPayload.JsonOptions))));

        public static Dictionary<string, CapturedValue> FromJson(JsonObject? captured, string[] members)
            => members
                .Select(member => (Member: member, Frozen: captured?[member]?.Deserialize<CapturedValue>(ThenAndNowPayload.JsonOptions)))
                .Where(frozen => frozen.Frozen is not null)
                .ToDictionary(frozen => frozen.Member, frozen => frozen.Frozen!);
    }
}
