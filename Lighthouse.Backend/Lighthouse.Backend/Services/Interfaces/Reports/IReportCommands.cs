using Lighthouse.Backend.Models.Reports;

namespace Lighthouse.Backend.Services.Interfaces.Reports
{
    /// <summary>What an editor may change on a report, with the concurrency token of the version they read.</summary>
    public sealed record ReportEdit(string Name, IReadOnlyList<string> ShownMetricKeys, Guid ConcurrencyToken, int? NowLengthDays);

    public interface IReportCommands
    {
        Task<ReportOutcome<ReportRead>> Create(ReportOwner owner, NewReport request);

        Task<ReportOutcome<ReportRead>> Edit(ReportOwner owner, int reportId, ReportEdit edit);

        Task<ReportOutcomeStatus> Delete(ReportOwner owner, int reportId);
    }
}
