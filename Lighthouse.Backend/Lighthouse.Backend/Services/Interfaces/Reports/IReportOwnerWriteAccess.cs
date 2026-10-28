using Lighthouse.Backend.Models.Reports;

namespace Lighthouse.Backend.Services.Interfaces.Reports
{
    /// <summary>Whether the caller may change an owner's reports, asked at the moment a run reaches that owner.</summary>
    public interface IReportOwnerWriteAccess
    {
        Task<bool> MayWriteAsync(ReportOwner owner);
    }
}
