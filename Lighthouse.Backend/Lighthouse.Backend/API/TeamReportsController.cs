using Lighthouse.Backend.API.DTO.Reports;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Implementation.Reports;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    [Route("api/v1/teams/{teamId:int}/reports")]
    [Route("api/latest/teams/{teamId:int}/reports")]
    [ApiController]
    [RbacGuard(RbacGuardRequirement.TeamRead, ScopeIdRouteKey = "teamId")]
    public class TeamReportsController(IReportQueries queries, IReportCommands commands) : ControllerBase
    {
        [HttpGet("templates")]
        public ActionResult<ReportTemplatesRead> GetTemplates(int teamId) => Answer(queries.Templates(ReportOwner.Team(teamId)));

        [HttpGet]
        public ActionResult<ReportList> GetReports(int teamId) => Answer(queries.List(ReportOwner.Team(teamId)));

        [HttpGet("{reportId:int}")]
        public ActionResult<ReportRead> GetReport(int teamId, int reportId) => Answer(queries.Get(ReportOwner.Team(teamId), reportId));

        [HttpPost]
        [RbacGuard(RbacGuardRequirement.TeamWrite, ScopeIdRouteKey = "teamId")]
        public async Task<ActionResult<ReportRead>> CreateReport(int teamId, [FromBody] NewReportRequest request)
        {
            var thenAndNow = request.ThenAndNow is { ThenEndDate: DateOnly thenEndDate, ThenLengthDays: int thenLengthDays }
                ? new NewThenAndNow(thenEndDate, thenLengthDays, request.ThenAndNow.NowLengthDays)
                : null;

            if (thenAndNow is null && request.TemplateKey == ThenAndNowTemplate.TemplateKey)
            {
                return ValidationProblem("A report needs the day Then ends and how many days it spans.");
            }

            var newReport = new NewReport(request.TemplateKey ?? string.Empty, request.Name ?? string.Empty, request.ShownMetricKeys ?? [], thenAndNow);

            var created = await commands.Create(ReportOwner.Team(teamId), newReport);
            if (created is { Status: ReportOutcomeStatus.Done, Value: ReportRead read })
            {
                return Created($"{Request.Path}/{read.Id}", read);
            }

            return Refusal(created);
        }

        private ActionResult<T> Answer<T>(ReportOutcome<T> outcome)
            => outcome is { Status: ReportOutcomeStatus.Done, Value: T value } ? Ok(value) : Refusal(outcome);

        private ActionResult Refusal<T>(ReportOutcome<T> outcome)
        {
            if (outcome.Status == ReportOutcomeStatus.NotFound)
            {
                return NotFound();
            }

            var status = outcome.Status == ReportOutcomeStatus.Unsupported ? StatusCodes.Status422UnprocessableEntity : StatusCodes.Status400BadRequest;
            var problem = new ProblemDetails { Status = status, Title = "The report request was refused." };
            problem.Extensions["code"] = outcome.Code;

            return new ObjectResult(problem) { StatusCode = status };
        }
    }
}
