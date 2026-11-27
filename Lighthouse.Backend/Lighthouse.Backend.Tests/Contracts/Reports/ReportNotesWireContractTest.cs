using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Modules.Reports.Application;
using Lighthouse.Backend.Modules.Reports.Domain;
using Lighthouse.Backend.Platform.Seeding;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Lighthouse.Backend.Modules.Licensing.Application;
using Lighthouse.Backend.Modules.Licensing.Contracts;

namespace Lighthouse.Backend.Tests.Contracts.Reports
{
    /// <summary>
    /// What a caller with read access only is told on the wire about the notes of a report: the notes are read, and
    /// a direct request to add, change or delete one is turned away with the note left as it was. Who may write is
    /// the route's rule, so these scenarios start a host with sign-in on; what a note is made of is asserted at the port.
    /// </summary>
    public class ReportNotesWireContractTest : WireContractTest
    {
        private const string TheNote = "Training took place";

        protected override WebApplicationFactory<Program> AdjustTheHost(TestWebApplicationFactory<Program> unchanged)
            => TestWebApplicationFactory<Program>.WithTestAuthentication(unchanged)
            .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                services.RemoveAll<ILicenseService>();
                services.AddScoped(_ => PortDoubles.PremiumLicence());
            }));

        [TestCase("team")]
        [TestCase("portfolio")]
        public async Task A_caller_with_read_access_only_reads_the_notes_of_a_report(string ownerKind)
        {
            var report = await ReportWithANote(ownerKind);

            using var response = await ReaderOf(report).GetAsync(NotesRoute(report));
            var body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"WHAT: GET {NotesRoute(report)} as a reader answered {(int)response.StatusCode}. WHY: every reader of the report sees its notes. HOW: guard the read with read access only.");
                Assert.That(JsonElement.Parse(body).EnumerateArray().Select(note => note.GetProperty("text").GetString()), Is.EqualTo(new[] { TheNote }), $"WHAT: the reader was answered {body}. WHY: the reader gets the context the notes give. HOW: list the report's notes for a reader.");
            }
        }

        [TestCase("team")]
        [TestCase("portfolio")]
        public async Task A_caller_with_read_access_only_is_refused_a_new_note(string ownerKind)
        {
            var report = await ReportWithANote(ownerKind);

            using var asReader = await ReaderOf(report).PostAsJsonAsync(NotesRoute(report), new { text = "Team grew to 9" });
            var afterTheReader = await TextsOfTheNotes(report);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(asReader.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"WHAT: a reader adding a note was answered {(int)asReader.StatusCode} on {NotesRoute(report)}. WHY: adding a note takes write access on the report's owner. HOW: guard the POST with write access on the owner named in the route.");
                Assert.That(afterTheReader, Is.EqualTo(new[] { TheNote }), "WHAT: the notes changed after a refused request. WHY: a refused request changes nothing. HOW: refuse before writing.");
            }
        }

        [TestCase("team")]
        [TestCase("portfolio")]
        public async Task A_caller_with_read_access_only_cannot_change_or_delete_a_note_by_a_direct_request(string ownerKind)
        {
            var report = await ReportWithANote(ownerKind);
            var theNote = (await NotesOf(report))[0].GetProperty("id").GetInt32();

            using var change = await ReaderOf(report).PutAsJsonAsync($"{NotesRoute(report)}/{theNote}", new { text = "Rewritten" });
            using var delete = await ReaderOf(report).DeleteAsync($"{NotesRoute(report)}/{theNote}");
            var afterwards = await TextsOfTheNotes(report);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(change.IsSuccessStatusCode, Is.False, $"WHAT: a reader changing note {theNote} was answered {(int)change.StatusCode}. WHY: a reader may not change notes. HOW: refuse the request.");
                Assert.That(delete.IsSuccessStatusCode, Is.False, $"WHAT: a reader deleting note {theNote} was answered {(int)delete.StatusCode}. WHY: a reader may not delete notes. HOW: refuse the request.");
                Assert.That(afterwards, Is.EqualTo(new[] { TheNote }), "WHAT: the note is not as it was written. WHY: refused requests change nothing. HOW: refuse before writing.");
            }
        }

        [TestCase("team")]
        [TestCase("portfolio")]
        public async Task A_caller_with_read_access_only_is_forbidden_to_change_or_delete_a_note(string ownerKind)
        {
            var report = await ReportWithANote(ownerKind);
            var theNote = (await NotesOf(report))[0].GetProperty("id").GetInt32();

            using var change = await ReaderOf(report).PutAsJsonAsync($"{NotesRoute(report)}/{theNote}", new { text = "Rewritten" });
            using var delete = await ReaderOf(report).DeleteAsync($"{NotesRoute(report)}/{theNote}");
            var afterwards = await TextsOfTheNotes(report);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(change.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"WHAT: a reader changing note {theNote} was answered {(int)change.StatusCode}. WHY: changing a note takes write access on the report's owner. HOW: guard the PUT with write access on the owner named in the route.");
                Assert.That(delete.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"WHAT: a reader deleting note {theNote} was answered {(int)delete.StatusCode}. WHY: deleting a note takes write access on the report's owner. HOW: guard the DELETE with write access on the owner named in the route.");
                Assert.That(afterwards, Is.EqualTo(new[] { TheNote }), "WHAT: the note is not as it was written. WHY: refused requests change nothing. HOW: refuse before writing.");
            }
        }

        [TestCase("team")]
        [TestCase("portfolio")]
        public async Task A_writer_who_did_not_sign_a_note_is_forbidden_to_change_or_delete_it_and_is_told_it_is_not_theirs(string ownerKind)
        {
            var report = await ReportWithANote(ownerKind, signedBy: "Ann");
            var theNote = (await NotesOf(report))[0].GetProperty("id").GetInt32();

            using var change = await WriterOf(report).PutAsJsonAsync($"{NotesRoute(report)}/{theNote}", new { text = "Rewritten" });
            var changeBody = await change.Content.ReadAsStringAsync();
            using var delete = await WriterOf(report).DeleteAsync($"{NotesRoute(report)}/{theNote}");
            var deleteBody = await delete.Content.ReadAsStringAsync();
            var afterwards = await TextsOfTheNotes(report);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(change.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"WHAT: a writer changing a note somebody else signed was answered {(int)change.StatusCode}. WHY: a signed note stays its author's. HOW: answer 403 when the note is not the caller's.");
                Assert.That(changeBody, Does.Contain(ReportRefusalCodes.NoteNotYours), "WHAT: the refusal does not name why. WHY: the client matches on the code. HOW: answer the problem code note-not-yours.");
                Assert.That(delete.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden), $"WHAT: a writer deleting a note somebody else signed was answered {(int)delete.StatusCode}. WHY: a signed note stays its author's. HOW: answer 403 when the note is not the caller's.");
                Assert.That(deleteBody, Does.Contain(ReportRefusalCodes.NoteNotYours));
                Assert.That(afterwards, Is.EqualTo(new[] { TheNote }), "WHAT: the note is not as it was written. WHY: refused requests change nothing. HOW: refuse before writing.");
            }
        }

        [TestCase("team")]
        [TestCase("portfolio")]
        public async Task A_writer_adds_a_note_is_told_why_an_empty_one_or_one_on_a_missing_report_is_refused_and_deletes_their_own(string ownerKind)
        {
            var report = await ReportWithANote(ownerKind);
            var writer = WriterOf(report);

            using var added = await writer.PostAsJsonAsync(NotesRoute(report), new { text = "Team grew to 9" });
            var addedId = JsonElement.Parse(await added.Content.ReadAsStringAsync()).GetProperty("id").GetInt32();
            using var empty = await writer.PostAsJsonAsync(NotesRoute(report), new { text = "" });
            var problem = JsonElement.Parse(await empty.Content.ReadAsStringAsync());
            var missingReport = report with { ReportId = report.ReportId + 1000 };
            using var onAMissingReport = await writer.PostAsJsonAsync(NotesRoute(missingReport), new { text = "Team grew to 9" });
            using var deleted = await writer.DeleteAsync($"{NotesRoute(report)}/{addedId}");
            var afterwards = await TextsOfTheNotes(report);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(added.StatusCode, Is.EqualTo(HttpStatusCode.Created), $"WHAT: a writer adding a note was answered {(int)added.StatusCode}. WHY: a new note is created. HOW: answer 201.");
                Assert.That(added.Headers.Location?.ToString(), Is.EqualTo(NotesRoute(report)), "WHAT: the answer to a new note names no place. WHY: a created resource says where it lives. HOW: answer the notes route as the location.");
                Assert.That(empty.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), $"WHAT: an empty note was answered {(int)empty.StatusCode}. WHY: what is written is wrong, not who writes it. HOW: answer 400 for a text refusal.");
                Assert.That(problem.GetProperty("code").GetString(), Is.EqualTo(ReportRefusalCodes.NoteTextMissing), $"WHAT: the refusal was {problem}. WHY: the client matches on the code to say why. HOW: answer the problem with a code property.");
                Assert.That(problem.GetProperty("status").GetInt32(), Is.EqualTo(400), "WHAT: the problem does not carry its status. WHY: a problem says its own status. HOW: set the status on the problem.");
                Assert.That(onAMissingReport.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), $"WHAT: a note on a report that is not there was answered {(int)onAMissingReport.StatusCode}. WHY: there is nothing to add it to. HOW: answer 404.");
                Assert.That(deleted.StatusCode, Is.EqualTo(HttpStatusCode.NoContent), $"WHAT: the author deleting their note was answered {(int)deleted.StatusCode}. WHY: the author may delete it. HOW: answer 204.");
                Assert.That(afterwards, Is.EqualTo(new[] { TheNote }), "WHAT: the notes are not as they were before the writer's own note. WHY: the empty note was refused and the added one deleted. HOW: save only accepted notes.");
            }
        }

        private sealed record ReportAt(string OwnerKind, int OwnerId, int ReportId);

        private static string NotesRoute(ReportAt report)
            => $"/api/latest/{report.OwnerKind}s/{report.OwnerId}/reports/{report.ReportId}/notes";

        private HttpClient ReaderOf(ReportAt report)
            => report.OwnerKind == "team" ? Client.AsTeamViewer(report.OwnerId) : Client.AsPortfolioViewer(report.OwnerId);

        private HttpClient WriterOf(ReportAt report)
            => report.OwnerKind == "team" ? Client.AsTeamAdmin(report.OwnerId) : Client.AsPortfolioAdmin(report.OwnerId);

        private async Task<IReadOnlyList<JsonElement>> NotesOf(ReportAt report)
        {
            using var response = await WriterOf(report).GetAsync(NotesRoute(report));
            return JsonElement.Parse(await response.Content.ReadAsStringAsync()).EnumerateArray().ToList();
        }

        private async Task<string?[]> TextsOfTheNotes(ReportAt report)
            => (await NotesOf(report)).Select(note => note.GetProperty("text").GetString()).ToArray();

        private async Task<ReportAt> ReportWithANote(string ownerKind, string? signedBy = null)
        {
            using var scope = NewScope();
            foreach (var seeder in scope.ServiceProvider.GetServices<ISeeder>())
            {
                await seeder.Seed();
            }

            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var connection = new WorkTrackingSystemConnection { Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.Jira };
            ReportOwner owner;
            if (ownerKind == "team")
            {
                var team = new Team { Name = "Lightspeed", WorkTrackingSystemConnection = connection, DataRetrievalValue = "project = LIGHTSPEED" };
                context.Teams.Add(team);
                context.SaveChanges();
                owner = ReportOwner.Team(team.Id);
            }
            else
            {
                var portfolio = new Portfolio { Name = "Apollo", WorkTrackingSystemConnection = connection, DataRetrievalValue = "project = APOLLO" };
                context.Portfolios.Add(portfolio);
                context.SaveChanges();
                owner = ReportOwner.Portfolio(portfolio.Id);
            }

            var created = await scope.ServiceProvider.GetRequiredService<IReportCommands>().Create(owner, new NewReport(
                "then-and-now", "Started against finished", ["arrivals"], new NewThenAndNow(new DateOnly(2026, 7, 31), 30, 30)));
            Assert.That(created.Status, Is.EqualTo(ReportOutcomeStatus.Done), $"the report was not created: {created.Code}");

            var reportId = created.Value!.Id;
            ReportNoteCaller author = new ReportNoteCaller.Anonymous();
            if (signedBy is not null)
            {
                var profile = new UserProfile { Subject = signedBy.ToLowerInvariant(), SubjectClaimType = "sub", DisplayName = signedBy, CreatedAt = DateTime.UtcNow, LastSeenAt = DateTime.UtcNow };
                context.UserProfiles.Add(profile);
                context.SaveChanges();
                author = new ReportNoteCaller.Signed(profile.Id, signedBy);
            }

            var added = await scope.ServiceProvider.GetRequiredService<IReportNoteCommands>().Add(owner, reportId, author, TheNote, DateTime.UtcNow);
            Assert.That(added.Status, Is.EqualTo(ReportOutcomeStatus.Done), $"the note was not saved: {added.Code}");

            return new ReportAt(ownerKind, owner.Id, reportId);
        }
    }
}
