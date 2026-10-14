using Lighthouse.Backend.Models.Reports;

namespace Lighthouse.Backend.Services.Interfaces.Reports
{
    public interface IReportWriter
    {
        void Add(Report report);

        /// <summary>Marks the owner's report for removal on the next save; null when the owner holds no such report.</summary>
        Report? Remove(ReportOwner owner, int reportId);

        Task Save();
    }
}
