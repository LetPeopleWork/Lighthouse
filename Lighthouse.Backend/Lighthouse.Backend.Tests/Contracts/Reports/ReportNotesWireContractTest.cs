using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Modules.Reports.Application;
using Lighthouse.Backend.Modules.Reports.Domain;
using Lighthouse.Backend.Platform.Seeding;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

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
            => TestWebApplicationFactory<Program>.WithTestAuthentication(unchanged);

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

        private async Task<ReportAt> ReportWithANote(string ownerKind)
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
            var added = await scope.ServiceProvider.GetRequiredService<IReportNoteCommands>().Add(owner, reportId, new ReportNoteCaller.Anonymous(), TheNote, DateTime.UtcNow);
            Assert.That(added.Status, Is.EqualTo(ReportOutcomeStatus.Done), $"the note was not saved: {added.Code}");

            return new ReportAt(ownerKind, owner.Id, reportId);
        }
    }
}
