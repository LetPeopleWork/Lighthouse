using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Modules.Reports.Domain;

namespace Lighthouse.Backend.Modules.Reports.Application
{
    public sealed class ReportNoteQueries(IReportNoteStore store) : IReportNoteQueries
    {
        internal IReportNoteStore Store { get; } = store;

        public ReportOutcome<IReadOnlyList<ReportNoteRead>> List(ReportOwner owner, int reportId)
        {
            throw new NotImplementedException();
        }
    }
}
