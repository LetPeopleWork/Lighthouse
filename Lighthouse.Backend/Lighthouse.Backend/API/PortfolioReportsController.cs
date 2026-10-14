using Lighthouse.Backend.API.DTO.Reports;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/portfolios/{portfolioId:int}/reports")]
    [Route("api/latest/portfolios/{portfolioId:int}/reports")]
    [ApiController]
    [RbacGuard(RbacGuardRequirement.PortfolioRead, ScopeIdRouteKey = "portfolioId")]
    public class PortfolioReportsController(IReportQueries queries, IReportCommands commands) : ReportsControllerBase(queries, commands)
    {
        [HttpGet("templates")]
        public ActionResult<ReportTemplatesRead> GetTemplates(int portfolioId) => TemplatesOf(ReportOwner.Portfolio(portfolioId));

        [HttpGet]
        public ActionResult<ReportList> GetReports(int portfolioId) => ReportsOf(ReportOwner.Portfolio(portfolioId));

        [HttpGet("{reportId:int}")]
        public ActionResult<ReportRead> GetReport(int portfolioId, int reportId) => ReportOf(ReportOwner.Portfolio(portfolioId), reportId);

        [HttpPost]
        [RbacGuard(RbacGuardRequirement.PortfolioWrite, ScopeIdRouteKey = "portfolioId")]
        public Task<ActionResult<ReportRead>> CreateReport(int portfolioId, [FromBody] NewReportRequest request) => Create(ReportOwner.Portfolio(portfolioId), request);

        [HttpPut("{reportId:int}")]
        [RbacGuard(RbacGuardRequirement.PortfolioWrite, ScopeIdRouteKey = "portfolioId")]
        public Task<ActionResult<ReportRead>> EditReport(int portfolioId, int reportId, [FromBody] ReportEditRequest request) => Edit(ReportOwner.Portfolio(portfolioId), reportId, request);

        [HttpDelete("{reportId:int}")]
        [RbacGuard(RbacGuardRequirement.PortfolioWrite, ScopeIdRouteKey = "portfolioId")]
        public Task<IActionResult> DeleteReport(int portfolioId, int reportId) => Delete(ReportOwner.Portfolio(portfolioId), reportId);
    }
}
