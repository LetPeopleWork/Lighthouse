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

    public sealed record ReportsAcrossOwners(IReadOnlyList<ReportSummary> Reports);

    /// <summary>What creating a report for any of several owners needs, read once: the types on offer, the window bounds, and every owner the caller may read.</summary>
    public sealed record ReportCreationOptions(DateOnly Today, int MinimumLengthDays, IReadOnlyList<ReportCreationTemplate> Templates, IReadOnlyList<ReportCreationOwner> Owners);

    public sealed record ReportCreationTemplate(string TemplateKey, IReadOnlyDictionary<ReportOwnerKind, IReadOnlyList<string>> MetricKeysByOwnerKind);

    /// <summary>One owner a report may be created for, with the first day it still holds finished items for, or null when it keeps them all.</summary>
    public sealed record ReportCreationOwner(ReportOwnerKind OwnerKind, int OwnerId, string OwnerName, DateOnly? EarliestStart);

    public sealed record ReportSummary(int Id, string Name, string TemplateKey, ReportOwnerKind OwnerKind, int OwnerId, DateOnly CreatedOn)
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? OwnerName { get; init; }

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

    public sealed record ThenAndNowRead(ReportWindow ThenWindow, ReportWindow NowWindow, bool ThenRebuildable, IReadOnlyDictionary<string, MetricRead> Metrics)
    {
        /// <summary>Now always ends today, so this is how long ago Then ended; 0 when Then ends today.</summary>
        public int DaysSinceThenEnded => NowWindow.End.DayNumber - ThenWindow.End.DayNumber;
    }

    /// <summary>One metric's two sides, each a value per member (p85, total, …).</summary>
    public sealed record MetricRead(IReadOnlyDictionary<string, CapturedValue> AtThen, IReadOnlyDictionary<string, CapturedValue> AtNow);
}
