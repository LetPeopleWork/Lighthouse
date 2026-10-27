using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/reports")]
    [Route("api/latest/reports")]
    [ApiController]
    public class ReportsController(IReportQueries queries, IReportOwnerLookup owners, IRbacAdministrationService rbacAdministrationService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<ReportsAcrossOwners>> GetReports()
        {
            var everyOwner = owners.All();
            var teamIds = everyOwner.Where(row => row.Owner.Kind == ReportOwnerKind.Team).Select(row => row.Owner.Id).ToArray();
            var portfolioIds = everyOwner.Where(row => row.Owner.Kind == ReportOwnerKind.Portfolio).Select(row => row.Owner.Id).ToArray();

            var readableTeamIds = await rbacAdministrationService.GetReadableTeamIdsAsync(User, teamIds, HttpContext.RequestAborted);
            var readablePortfolioIds = await rbacAdministrationService.GetReadablePortfolioIdsAsync(User, portfolioIds, HttpContext.RequestAborted);

            IReadOnlyCollection<ReportOwner> readable = [.. readableTeamIds.Select(ReportOwner.Team), .. readablePortfolioIds.Select(ReportOwner.Portfolio)];

            return Ok(queries.ListAcross(readable).Value);
        }
    }
}
