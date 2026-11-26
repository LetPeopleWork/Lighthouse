using Lighthouse.Backend.API.DTO.Reports;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Modules.Access.Adapters.Http;
using Lighthouse.Backend.Modules.Access.Application;
using Lighthouse.Backend.Modules.Reports.Application;
using Lighthouse.Backend.Platform.Host;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/portfolios/{portfolioId:int}/reports/{reportId:int}/notes")]
    [Route("api/latest/portfolios/{portfolioId:int}/reports/{reportId:int}/notes")]
    [ApiController]
    [RbacGuard(RbacGuardRequirement.PortfolioRead, ScopeIdRouteKey = "portfolioId")]
    public class PortfolioReportNotesController(
        IReportNoteQueries queries,
        IReportNoteCommands commands,
        ICurrentUserProfileService profiles,
        IAuthModeResolver authModes,
        ILighthouseClock clock) : ReportNotesControllerBase(queries, commands, profiles, authModes, clock)
    {
        [HttpGet]
        public ActionResult<IReadOnlyList<ReportNoteDto>> GetNotes(int portfolioId, int reportId) => NotesOf(ReportOwner.Portfolio(portfolioId), reportId);

        [HttpPost]
        [RbacGuard(RbacGuardRequirement.PortfolioWrite, ScopeIdRouteKey = "portfolioId")]
        public Task<ActionResult<ReportNoteDto>> AddReportNote(int portfolioId, int reportId, [FromBody] ReportNoteRequest request) => AddNote(ReportOwner.Portfolio(portfolioId), reportId, request);
    }
}
