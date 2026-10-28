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
            if (request.ToNewReport() is not NewReport newReport)
            {
                return ValidationProblem(NewReportRequest.ThenAndNowIncomplete);
            }

            var created = await commands.Create(owner, newReport);
            if (created is { Status: ReportOutcomeStatus.Done, Value: ReportRead read })
            {
                return Created($"{Request.Path}/{read.Id}", read);
            }

            return Refusal(created, owner);
        }

        protected async Task<ActionResult<ReportRead>> Edit(ReportOwner owner, int reportId, ReportEditRequest request)
        {
            if (request.ConcurrencyToken is not Guid concurrencyToken)
            {
                return ValidationProblem("An edit needs the concurrency token of the report it was made on.");
            }

            var edit = new ReportEdit(request.Name ?? string.Empty, request.ShownMetricKeys ?? [], concurrencyToken, request.ThenAndNow?.NowLengthDays);

            var edited = await commands.Edit(owner, reportId, edit);
            return edited is { Status: ReportOutcomeStatus.Done, Value: ReportRead read } ? Ok(read) : Refusal(edited, owner);
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

            var status = StatusOf(outcome);
            var problem = new ProblemDetails { Status = status, Title = "The report request was refused." };
            problem.Extensions["code"] = outcome.Code;
            if (owner is not null && outcome.Code is ReportRefusalCodes.ThenBeforeCutoff or ReportRefusalCodes.NowBeforeCutoff)
            {
                problem.Extensions["earliestStart"] = queries.Templates(owner).Value?.EarliestStart;
            }

            return new ObjectResult(problem) { StatusCode = status };
        }

        private static int StatusOf<T>(ReportOutcome<T> outcome)
        {
            if (outcome.Code == ReportRefusalCodes.ReportCapReached)
            {
                return StatusCodes.Status403Forbidden;
            }

            return outcome.Status == ReportOutcomeStatus.Unsupported ? StatusCodes.Status422UnprocessableEntity : StatusCodes.Status400BadRequest;
        }
    }
}
