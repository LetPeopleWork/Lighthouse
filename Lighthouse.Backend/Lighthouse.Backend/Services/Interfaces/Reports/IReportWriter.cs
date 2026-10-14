using Lighthouse.Backend.Models.Reports;

namespace Lighthouse.Backend.Services.Interfaces.Reports
{
    public interface IReportWriter
    {
        /// <summary>How many reports of the template the owner holds, so a create can be held to the cap before it adds one.</summary>
        int CountOfTemplate(ReportOwner owner, string templateKey);

        void Add(Report report);

        /// <summary>The owner's report, tracked so that changes to it are written on the next save; null when the owner holds no such report.</summary>
        Report? FindForEdit(ReportOwner owner, int reportId);

        /// <summary>Makes the next save fail with a concurrency conflict unless the stored report still carries the token the editor read.</summary>
        void ExpectUnchangedSince(Report report, Guid concurrencyToken);

        /// <summary>Marks the owner's report for removal on the next save; null when the owner holds no such report.</summary>
        Report? Remove(ReportOwner owner, int reportId);

        Task Save();
    }
}
