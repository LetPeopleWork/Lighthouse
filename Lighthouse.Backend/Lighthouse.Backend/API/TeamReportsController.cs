using Lighthouse.Backend.API.DTO.Reports;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/teams/{teamId:int}/reports")]
    [Route("api/latest/teams/{teamId:int}/reports")]
    [ApiController]
    [RbacGuard(RbacGuardRequirement.TeamRead, ScopeIdRouteKey = "teamId")]
    public class TeamReportsController(IReportQueries queries, IReportCommands commands) : ReportsControllerBase(queries, commands)
    {
        [HttpGet("templates")]
        public ActionResult<ReportTemplatesRead> GetTemplates(int teamId) => TemplatesOf(ReportOwner.Team(teamId));

        [HttpGet]
        public ActionResult<ReportList> GetReports(int teamId) => ReportsOf(ReportOwner.Team(teamId));

        [HttpGet("{reportId:int}")]
        public ActionResult<ReportRead> GetReport(int teamId, int reportId) => ReportOf(ReportOwner.Team(teamId), reportId);

        [HttpPost]
        [RbacGuard(RbacGuardRequirement.TeamWrite, ScopeIdRouteKey = "teamId")]
        public Task<ActionResult<ReportRead>> CreateReport(int teamId, [FromBody] NewReportRequest request) => Create(ReportOwner.Team(teamId), request);

        [HttpDelete("{reportId:int}")]
        [RbacGuard(RbacGuardRequirement.TeamWrite, ScopeIdRouteKey = "teamId")]
        public Task<IActionResult> DeleteReport(int teamId, int reportId) => Delete(ReportOwner.Team(teamId), reportId);
    }
}
