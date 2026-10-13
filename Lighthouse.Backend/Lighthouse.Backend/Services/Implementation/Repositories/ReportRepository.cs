using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class ReportRepository(LighthouseAppContext context) : IReportReader, IReportWriter
    {
        public Report? GetForOwner(ReportOwner owner, int reportId)
            => OwnedBy(owner).SingleOrDefault(report => report.Id == reportId);

        public IReadOnlyList<Report> ListForOwner(ReportOwner owner)
            => [.. OwnedBy(owner).OrderBy(report => report.Id)];

        public void Add(Report report) => context.Reports.Add(report);

        public Task Save() => context.SaveChangesAsync();

        private IQueryable<Report> OwnedBy(ReportOwner owner)
            => owner.Kind == ReportOwnerKind.Team
                ? context.Reports.AsNoTracking().Where(report => report.TeamId == owner.Id)
                : context.Reports.AsNoTracking().Where(report => report.PortfolioId == owner.Id);
    }
}
