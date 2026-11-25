using Lighthouse.Backend.Models.Reports;

namespace Lighthouse.Backend.Modules.Reports.Application
{
    public interface IReportBulkRemover
    {
        /// <summary>Removes each report from the owner the server finds for it, one after another in the order given, and answers every report.</summary>
        Task<IReadOnlyList<ReportRemovalOutcome>> RemoveEach(IReadOnlyList<int> reportIds, IReportOwnerWriteAccess writeAccess);
    }
}
