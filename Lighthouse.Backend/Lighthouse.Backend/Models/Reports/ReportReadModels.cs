using Lighthouse.Backend.Services.Interfaces.Reports;
using System.Text.Json.Serialization;

namespace Lighthouse.Backend.Models.Reports
{
    public enum ReportOutcomeStatus
    {
        Done,
        NotFound,
        Refused,
        Unsupported,
    }

    /// <summary>What a Reports port answers: the value, or no such owner or report, or a refusal named by one code of a closed set.</summary>
    public sealed record ReportOutcome<T>(ReportOutcomeStatus Status, T? Value, string? Code)
    {
        public static ReportOutcome<T> Done(T value) => new(ReportOutcomeStatus.Done, value, null);

        public static ReportOutcome<T> NotFound() => new(ReportOutcomeStatus.NotFound, default, null);

        public static ReportOutcome<T> Refused(string code) => new(ReportOutcomeStatus.Refused, default, code);

        public static ReportOutcome<T> Unsupported(string code) => new(ReportOutcomeStatus.Unsupported, default, code);
    }

    public sealed record NewReport(string TemplateKey, string Name, IReadOnlyList<string> ShownMetricKeys, NewThenAndNow? ThenAndNow);

    public sealed record NewThenAndNow(DateOnly ThenEndDate, int ThenLengthDays, int? NowLengthDays);

    public sealed record ReportTemplatesRead(DateOnly Today, DateOnly? EarliestStart, int MinimumLengthDays, IReadOnlyList<ReportTemplateRead> Templates);

    public sealed record ReportTemplateRead(string TemplateKey, IReadOnlyList<ReportTemplateMetricRead> Metrics);

    public sealed record ReportTemplateMetricRead(string Key);

    public sealed record ReportList(IReadOnlyList<ReportSummary> Reports, bool CreationBlockedByCap, int CommunityCap);

    public sealed record ReportSummary(int Id, string Name, string TemplateKey, ReportOwnerKind OwnerKind, int OwnerId, DateOnly CreatedOn)
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ThenAndNowSummary? ThenAndNow { get; init; }
    }

    public sealed record ThenAndNowSummary(bool ThenRebuildable);

    public sealed record ReportRead(int Id, string Name, string TemplateKey, ReportOwnerKind OwnerKind, int OwnerId, DateOnly CreatedOn)
    {
        public Guid ConcurrencyToken { get; init; }

        public IReadOnlyList<string> ShownMetricKeys { get; init; } = [];

        public ThenAndNowRead? ThenAndNow { get; init; }
    }

    public sealed record ThenAndNowRead(ReportWindow ThenWindow, ReportWindow NowWindow, bool ThenRebuildable, IReadOnlyDictionary<string, MetricRead> Metrics);

    /// <summary>One metric's two sides, each a value per member (p85, total, …).</summary>
    public sealed record MetricRead(IReadOnlyDictionary<string, CapturedValue> AtThen, IReadOnlyDictionary<string, CapturedValue> AtNow);
}
