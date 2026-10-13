using System.Text.Json;
using System.Text.Json.Nodes;

namespace Lighthouse.Backend.Models.Reports
{
    /// <summary>
    /// What a Then &amp; Now report keeps on its row. Members are only ever added and every one is optional: a member
    /// a report predates is simply absent, and members a later version wrote are left alone when this one reads.
    /// </summary>
    public sealed record ThenAndNowPayload
    {
        public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public ReportWindow? ThenWindow { get; init; }

        public int? NowLengthDays { get; init; }

        public Dictionary<string, JsonObject>? Captures { get; init; }

        public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

        public static ThenAndNowPayload? FromJson(string json) => JsonSerializer.Deserialize<ThenAndNowPayload>(json, JsonOptions);
    }

    /// <summary>Inclusive calendar days in the instance's zone: both ends count.</summary>
    public sealed record ReportWindow(DateOnly Start, DateOnly End, int LengthDays)
    {
        public static ReportWindow EndingOn(DateOnly end, int lengthDays) => new(end.AddDays(-(lengthDays - 1)), end, lengthDays);
    }

    /// <summary>A value with what it rests on, or no value and the reason there is none - never a 0 standing in for nothing.</summary>
    public sealed record CapturedValue(double? Value, int? SampleSize, string? AbsentReason)
    {
        public static CapturedValue Shown(double value, int sampleSize) => new(value, sampleSize, null);

        public static CapturedValue Absent(string reason) => new(null, null, reason);
    }
}
