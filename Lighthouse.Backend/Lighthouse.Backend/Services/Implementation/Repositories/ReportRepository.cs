using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public class ReportRepository(LighthouseAppContext context) : IReportReader, IReportWriter
    {
        public Report? GetForOwner(ReportOwner owner, int reportId)
            => OwnedBy(owner).AsNoTracking().SingleOrDefault(report => report.Id == reportId);

        public IReadOnlyList<Report> ListForOwner(ReportOwner owner)
            => [.. OwnedBy(owner).AsNoTracking().OrderBy(report => report.Id)];

        public IReadOnlyList<Report> ListForOwners(IReadOnlyCollection<ReportOwner> owners)
        {
            var teamIds = owners.Where(owner => owner.Kind == ReportOwnerKind.Team).Select(owner => (int?)owner.Id).ToList();
            var portfolioIds = owners.Where(owner => owner.Kind == ReportOwnerKind.Portfolio).Select(owner => (int?)owner.Id).ToList();

            return [.. context.Reports.AsNoTracking()
                .Where(report => teamIds.Contains(report.TeamId) || portfolioIds.Contains(report.PortfolioId))
                .OrderBy(report => report.Id)];
        }

        public int CountOfTemplate(ReportOwner owner, string templateKey)
            => OwnedBy(owner).Count(report => report.TemplateKey == templateKey);

        public void Add(Report report) => context.Reports.Add(report);

        public Report? FindForEdit(ReportOwner owner, int reportId) => Tracked(owner, reportId);

        public void ExpectUnchangedSince(Report report, Guid concurrencyToken) => context.ApplyConcurrencyTokenForEdit(report, concurrencyToken);

        public Report? Remove(ReportOwner owner, int reportId)
        {
            var report = Tracked(owner, reportId);
            if (report is not null)
            {
                context.Reports.Remove(report);
            }

            return report;
        }

        public Task Save() => context.SaveChangesAsync();

        private Report? Tracked(ReportOwner owner, int reportId)
            => OwnedBy(owner).SingleOrDefault(report => report.Id == reportId);

        private IQueryable<Report> OwnedBy(ReportOwner owner)
            => owner.Kind == ReportOwnerKind.Team
                ? context.Reports.Where(report => report.TeamId == owner.Id)
                : context.Reports.Where(report => report.PortfolioId == owner.Id);
    }
}
