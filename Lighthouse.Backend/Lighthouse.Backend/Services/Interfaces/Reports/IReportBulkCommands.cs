using Lighthouse.Backend.Models.Reports;

namespace Lighthouse.Backend.Services.Interfaces.Reports
{
    public interface IReportBulkCommands
    {
        /// <summary>The most Teams and Portfolios one bulk request may name; a longer request is refused whole.</summary>
        const int MostOwnersPerRun = 50;

        /// <summary>Creates the same report for each owner, one after another in the order given, and answers every owner.</summary>
        Task<IReadOnlyList<ReportOwnerOutcome>> CreateForEach(IReadOnlyList<ReportOwner> owners, NewReport request, IReportOwnerWriteAccess writeAccess);
    }
}
