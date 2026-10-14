using Lighthouse.Backend.Models.Reports;

namespace Lighthouse.Backend.Services.Interfaces.Reports
{
    public interface IReportWriter
    {
        /// <summary>How many reports of the template the owner holds, so a create can be held to the cap before it adds one.</summary>
        int CountOfTemplate(ReportOwner owner, string templateKey);

        void Add(Report report);

        /// <summary>Marks the owner's report for removal on the next save; null when the owner holds no such report.</summary>
        Report? Remove(ReportOwner owner, int reportId);

        Task Save();
    }
}
