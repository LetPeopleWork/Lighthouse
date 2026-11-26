using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Modules.Reports.Domain;

namespace Lighthouse.Backend.Modules.Reports.Application
{
    public sealed class ReportNoteCommands(IReportNoteStore store) : IReportNoteCommands
    {
        internal IReportNoteStore Store { get; } = store;

        public Task<ReportOutcome<ReportNoteRead>> Add(ReportOwner owner, int reportId, ReportNoteCaller caller, string? text, DateTime now)
        {
            throw new NotImplementedException();
        }
    }
}
