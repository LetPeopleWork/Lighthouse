using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.Reports
{
    public class ReportOwnerLookup(IRepository<Team> teams, IRepository<Portfolio> portfolios) : IReportOwnerLookup
    {
        public ReportOwnerFacts? Find(ReportOwner owner)
        {
            WorkTrackingSystemOptionsOwner? found = owner.Kind == ReportOwnerKind.Team ? teams.GetById(owner.Id) : portfolios.GetById(owner.Id);
            return found is null ? null : new ReportOwnerFacts(found.Name, found.DoneItemsCutoffDays);
        }

        public IReadOnlyList<ReportOwnerRow> All()
        {
            var teamRows = teams.GetAllByPredicate(team => true)
                .Select(team => new { team.Id, team.Name, team.DoneItemsCutoffDays })
                .ToList()
                .Select(team => new ReportOwnerRow(ReportOwner.Team(team.Id), team.Name, team.DoneItemsCutoffDays));
            var portfolioRows = portfolios.GetAllByPredicate(portfolio => true)
                .Select(portfolio => new { portfolio.Id, portfolio.Name, portfolio.DoneItemsCutoffDays })
                .ToList()
                .Select(portfolio => new ReportOwnerRow(ReportOwner.Portfolio(portfolio.Id), portfolio.Name, portfolio.DoneItemsCutoffDays));

            return [.. teamRows, .. portfolioRows];
        }
    }
}
