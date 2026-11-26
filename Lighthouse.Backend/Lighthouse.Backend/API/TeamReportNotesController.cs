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
    [Route("api/v1/teams/{teamId:int}/reports/{reportId:int}/notes")]
    [Route("api/latest/teams/{teamId:int}/reports/{reportId:int}/notes")]
    [ApiController]
    [RbacGuard(RbacGuardRequirement.TeamRead, ScopeIdRouteKey = "teamId")]
    public class TeamReportNotesController(
        IReportNoteQueries queries,
        IReportNoteCommands commands,
        ICurrentUserProfileService profiles,
        IAuthModeResolver authModes,
        ILighthouseClock clock) : ReportNotesControllerBase(queries, commands, profiles, authModes, clock)
    {
        [HttpGet]
        public ActionResult<IReadOnlyList<ReportNoteDto>> GetNotes(int teamId, int reportId) => NotesOf(ReportOwner.Team(teamId), reportId);

        [HttpPost]
        [RbacGuard(RbacGuardRequirement.TeamWrite, ScopeIdRouteKey = "teamId")]
        public Task<ActionResult<ReportNoteDto>> AddReportNote(int teamId, int reportId, [FromBody] ReportNoteRequest request) => AddNote(ReportOwner.Team(teamId), reportId, request);
    }
}
