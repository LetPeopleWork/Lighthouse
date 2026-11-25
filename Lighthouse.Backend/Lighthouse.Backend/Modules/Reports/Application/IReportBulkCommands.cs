using Lighthouse.Backend.Models.Reports;

namespace Lighthouse.Backend.Modules.Reports.Application
{
    public interface IReportBulkCommands
    {
        /// <summary>Creates the same report for each owner, one after another in the order given, and answers every owner.</summary>
        Task<IReadOnlyList<ReportOwnerOutcome>> CreateForEach(IReadOnlyList<ReportOwner> owners, NewReport request, IReportOwnerWriteAccess writeAccess);
    }
}
