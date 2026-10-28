using Lighthouse.Backend.API.DTO.Reports;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Implementation.Licensing;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/reports")]
    [Route("api/latest/reports")]
    [ApiController]
    public class ReportsController(IReportQueries queries, IReportOwnerLookup owners, IReportBulkCommands bulkCommands, IRbacAdministrationService rbacAdministrationService) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<ReportsAcrossOwners>> GetReports()
            => Ok(queries.ListAcross(await ReadableOwners()).Value);

        [HttpGet("creation-options")]
        public async Task<ActionResult<ReportCreationOptions>> GetCreationOptions()
            => Ok(queries.CreationOptions(await ReadableOwners()).Value);

        private async Task<IReadOnlyCollection<ReportOwner>> ReadableOwners()
        {
            var everyOwner = owners.All();
            var teamIds = everyOwner.Where(row => row.Owner.Kind == ReportOwnerKind.Team).Select(row => row.Owner.Id).ToArray();
            var portfolioIds = everyOwner.Where(row => row.Owner.Kind == ReportOwnerKind.Portfolio).Select(row => row.Owner.Id).ToArray();

            var readableTeamIds = await rbacAdministrationService.GetReadableTeamIdsAsync(User, teamIds, HttpContext.RequestAborted);
            var readablePortfolioIds = await rbacAdministrationService.GetReadablePortfolioIdsAsync(User, portfolioIds, HttpContext.RequestAborted);

            return [.. readableTeamIds.Select(ReportOwner.Team), .. readablePortfolioIds.Select(ReportOwner.Portfolio)];
        }

        [HttpPost("bulk")]
        [LicenseGuard(RequirePremium = true)]
        public async Task<ActionResult<ReportBulkCreation>> CreateForEach([FromBody] NewReportsForOwnersRequest request)
        {
            if (request.ToNewReport() is not NewReport newReport)
            {
                return ValidationProblem(NewReportRequest.ThenAndNowIncomplete);
            }

            if (OwnersOf(request) is not List<ReportOwner> asked)
            {
                return ValidationProblem("Every owner needs its kind and its id.");
            }

            var outcomes = await bulkCommands.CreateForEach(asked, newReport, new ReportOwnerWriteAccess(rbacAdministrationService, User));
            return Ok(new ReportBulkCreation(outcomes));
        }

        private static List<ReportOwner>? OwnersOf(NewReportsForOwnersRequest request)
        {
            var asked = new List<ReportOwner>();
            foreach (var owner in request.Owners ?? [])
            {
                if (owner is not { OwnerKind: ReportOwnerKind kind, OwnerId: int id })
                {
                    return null;
                }

                asked.Add(new ReportOwner(kind, id));
            }

            return asked;
        }
    }
}
