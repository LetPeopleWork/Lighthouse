using Lighthouse.Backend.Models.Reports;

namespace Lighthouse.Backend.Services.Interfaces.Reports
{
    public interface IReportWriter
    {
        void Add(Report report);

        Task Save();
    }
}
