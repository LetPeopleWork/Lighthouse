using Lighthouse.Backend.API.DTO.Reports;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Modules.Access.Application;
using Lighthouse.Backend.Modules.Access.Contracts;
using Lighthouse.Backend.Modules.Reports.Application;
using Lighthouse.Backend.Modules.Reports.Domain;
using Lighthouse.Backend.Platform.Host;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    public abstract class ReportNotesControllerBase(
        IReportNoteQueries queries,
        IReportNoteCommands commands,
        ICurrentUserProfileService profiles,
        IAuthModeResolver authModes,
        ILighthouseClock clock) : ControllerBase
    {
        protected IReportNoteQueries Queries { get; } = queries;

        protected IReportNoteCommands Commands { get; } = commands;

        protected ICurrentUserProfileService Profiles { get; } = profiles;

        protected IAuthModeResolver AuthModes { get; } = authModes;

        protected ILighthouseClock Clock { get; } = clock;

        public static ReportNoteCaller CallerOf(AuthMode mode, UserProfile? profile) => (mode, profile) switch
        {
            (AuthMode.Disabled, _) => new ReportNoteCaller.Anonymous(),
            (AuthMode.Enabled, UserProfile signedIn) => new ReportNoteCaller.Signed(signedIn.Id, signedIn.DisplayName),
            _ => new ReportNoteCaller.Unresolved(),
        };

        protected async Task<ActionResult<IReadOnlyList<ReportNoteDto>>> NotesOf(ReportOwner owner, int reportId)
        {
            var listed = Queries.List(owner, reportId, await CallerOfRequest());
            if (listed is { Status: ReportOutcomeStatus.Done, Value: IReadOnlyList<ReportNoteRead> notes })
            {
                return Ok(notes.Select(note => ReportNoteDto.From(note, Clock)).ToList());
            }

            return Refusal(listed);
        }

        protected async Task<ActionResult<ReportNoteDto>> AddNote(ReportOwner owner, int reportId, ReportNoteRequest request)
        {
            var caller = await CallerOfRequest();
            var added = await Commands.Add(owner, reportId, caller, request.Text, Clock.Now.UtcDateTime);
            if (added is { Status: ReportOutcomeStatus.Done, Value: ReportNoteRead note })
            {
                return Created($"{Request.Path}", ReportNoteDto.From(note, Clock));
            }

            return Refusal(added);
        }

        protected async Task<ActionResult<ReportNoteDto>> EditNote(ReportOwner owner, int reportId, int noteId, ReportNoteRequest request)
        {
            var caller = await CallerOfRequest();
            var changed = await Commands.Edit(owner, reportId, noteId, caller, request.Text, Clock.Now.UtcDateTime);
            if (changed is { Status: ReportOutcomeStatus.Done, Value: ReportNoteRead note })
            {
                return Ok(ReportNoteDto.From(note, Clock));
            }

            return Refusal(changed);
        }

        protected async Task<IActionResult> DeleteNote(ReportOwner owner, int reportId, int noteId)
        {
            var caller = await CallerOfRequest();
            var deleted = await Commands.Delete(owner, reportId, noteId, caller);
            if (deleted.Status == ReportOutcomeStatus.Done)
            {
                return NoContent();
            }

            return Refusal(deleted);
        }

        private async Task<ReportNoteCaller> CallerOfRequest()
        {
            var mode = AuthModes.Resolve().Mode;
            var profile = mode == AuthMode.Enabled
                ? await Profiles.GetOrCreateFromPrincipalAsync(User, HttpContext?.RequestAborted ?? default)
                : null;

            return CallerOf(mode, profile);
        }

        private ActionResult Refusal<T>(ReportOutcome<T> outcome)
        {
            if (outcome.Status == ReportOutcomeStatus.NotFound)
            {
                return NotFound();
            }

            var status = outcome.Code is ReportRefusalCodes.NoteAuthorUnknown or ReportRefusalCodes.NoteNotYours ? StatusCodes.Status403Forbidden : StatusCodes.Status400BadRequest;
            var problem = new ProblemDetails { Status = status, Title = "The report note request was refused." };
            problem.Extensions["code"] = outcome.Code;
            return new ObjectResult(problem) { StatusCode = status };
        }
    }
}
