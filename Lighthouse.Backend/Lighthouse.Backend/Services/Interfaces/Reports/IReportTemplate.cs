using Lighthouse.Backend.Models.Reports;

namespace Lighthouse.Backend.Services.Interfaces.Reports
{
    /// <summary>The instance's day and how far back the owner keeps finished items: every window a report computes is bounded by these two.</summary>
    public sealed record ReportCalendar(DateOnly Today, int DoneItemsCutoffDays);

    public interface IReportTemplate
    {
        string Key { get; }

        IReadOnlyList<string> MetricKeysFor(ReportOwnerKind ownerKind);

        ReportOutcome<string> Freeze(ReportOwner owner, NewReport request, ReportCalendar calendar);

        /// <summary>The report's payload with the edit applied, leaving every member the edit may not change as it was stored.</summary>
        ReportOutcome<string> Edit(Report report, ReportEdit edit, ReportCalendar calendar);

        ReportRead Read(Report report, ReportCalendar calendar);

        ReportSummary Summarise(Report report, ReportCalendar calendar);
    }
}
