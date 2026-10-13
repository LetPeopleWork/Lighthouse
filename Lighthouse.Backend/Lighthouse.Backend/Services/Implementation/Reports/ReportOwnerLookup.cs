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
            return found is null ? null : new ReportOwnerFacts(found.DoneItemsCutoffDays);
        }
    }
}
