using Lighthouse.Backend.API.DTO.Reports;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Implementation.Reports;
using Lighthouse.Backend.Services.Interfaces.Reports;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    /// <summary>How a report request is answered, the same whichever owner's address it came through.</summary>
    public abstract class ReportsControllerBase(IReportQueries queries, IReportCommands commands) : ControllerBase
    {
        protected ActionResult<ReportTemplatesRead> TemplatesOf(ReportOwner owner) => Answer(queries.Templates(owner));

        protected ActionResult<ReportList> ReportsOf(ReportOwner owner) => Answer(queries.List(owner));

        protected ActionResult<ReportRead> ReportOf(ReportOwner owner, int reportId) => Answer(queries.Get(owner, reportId));

        protected async Task<ActionResult<ReportRead>> Create(ReportOwner owner, NewReportRequest request)
        {
            var thenAndNow = request.ThenAndNow is { ThenEndDate: DateOnly thenEndDate, ThenLengthDays: int thenLengthDays }
                ? new NewThenAndNow(thenEndDate, thenLengthDays, request.ThenAndNow.NowLengthDays)
                : null;

            if (thenAndNow is null && request.TemplateKey == ThenAndNowTemplate.TemplateKey)
            {
                return ValidationProblem("A report needs the day Then ends and how many days it spans.");
            }

            var newReport = new NewReport(request.TemplateKey ?? string.Empty, request.Name ?? string.Empty, request.ShownMetricKeys ?? [], thenAndNow);

            var created = await commands.Create(owner, newReport);
            if (created is { Status: ReportOutcomeStatus.Done, Value: ReportRead read })
            {
                return Created($"{Request.Path}/{read.Id}", read);
            }

            return Refusal(created, owner);
        }

        protected async Task<IActionResult> Delete(ReportOwner owner, int reportId)
            => await commands.Delete(owner, reportId) == ReportOutcomeStatus.Done ? NoContent() : NotFound();

        private ActionResult<T> Answer<T>(ReportOutcome<T> outcome)
            => outcome is { Status: ReportOutcomeStatus.Done, Value: T value } ? Ok(value) : Refusal(outcome);

        private ActionResult Refusal<T>(ReportOutcome<T> outcome, ReportOwner? owner = null)
        {
            if (outcome.Status == ReportOutcomeStatus.NotFound)
            {
                return NotFound();
            }

            var status = outcome.Status == ReportOutcomeStatus.Unsupported ? StatusCodes.Status422UnprocessableEntity : StatusCodes.Status400BadRequest;
            var problem = new ProblemDetails { Status = status, Title = "The report request was refused." };
            problem.Extensions["code"] = outcome.Code;
            if (owner is not null && outcome.Code is ReportRefusalCodes.ThenBeforeCutoff or ReportRefusalCodes.NowBeforeCutoff)
            {
                problem.Extensions["earliestStart"] = queries.Templates(owner).Value?.EarliestStart;
            }

            return new ObjectResult(problem) { StatusCode = status };
        }
    }
}
