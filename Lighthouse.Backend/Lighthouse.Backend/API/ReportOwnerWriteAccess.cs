using System.Security.Claims;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Reports;

namespace Lighthouse.Backend.API
{
    /// <summary>
    /// The caller's right to change an owner's reports, bound to one request. It takes no cancellation on purpose:
    /// a run carries on when the browser stops listening, so what was asked for still gets made.
    /// </summary>
    public sealed class ReportOwnerWriteAccess(IRbacAdministrationService rbacAdministrationService, ClaimsPrincipal caller) : IReportOwnerWriteAccess
    {
        public Task<bool> MayWriteAsync(ReportOwner owner)
            => owner.Kind switch
            {
                ReportOwnerKind.Team => rbacAdministrationService.CanWriteTeamAsync(caller, owner.Id),
                ReportOwnerKind.Portfolio => rbacAdministrationService.CanWritePortfolioAsync(caller, owner.Id),
                _ => throw new ArgumentOutOfRangeException(nameof(owner), owner.Kind, "A report owner is a Team or a Portfolio."),
            };
    }
}
