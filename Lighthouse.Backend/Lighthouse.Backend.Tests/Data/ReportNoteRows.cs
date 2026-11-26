using System.Data.Common;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Platform.Encryption;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.Integration.Containers;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests
{
    internal static class ReportNoteRows
    {
        internal static LighthouseAppContext CreateContext(DbContextOptions<LighthouseAppContext> contextOptions)
            => new(contextOptions, Mock.Of<ICryptoService>(), Mock.Of<ILogger<LighthouseAppContext>>());

        internal static Task<int> InsertNote(LighthouseAppContext context, int reportId, string text, int? authorId, string? authorName = null)
            => context.Database.ExecuteSqlAsync($$"""
                INSERT INTO "ReportNotes" ("ReportId", "Text", "CreatedAt", "AuthorUserProfileId", "AuthorDisplayName")
                VALUES ({{reportId}}, {{text}}, '2026-10-13 09:00:00', {{authorId}}, {{authorName}})
                """);

        internal static Task<int> NotesOf(LighthouseAppContext context, int reportId)
            => context.Database.SqlQuery<int>($"SELECT COUNT(*) AS \"Value\" FROM \"ReportNotes\" WHERE \"ReportId\" = {reportId}").SingleAsync();

        internal static async Task<int> SeedReport(LighthouseAppContext context, int teamId)
        {
            var report = new Report
            {
                TeamId = teamId,
                TemplateKey = "then-and-now",
                Name = "Before the coaching started",
                CreatedAt = new DateTime(2026, 10, 13, 9, 0, 0, DateTimeKind.Utc),
                ShownMetricKeysJson = """["cycle-time"]""",
                TemplatePayloadJson = "{}",
                PayloadSchemaVersion = 1,
            };
            context.Reports.Add(report);
            await context.SaveChangesAsync();
            return report.Id;
        }

        internal static async Task<Team> SeedTeam(LighthouseAppContext context)
        {
            var connection = new WorkTrackingSystemConnection { Name = "Team Connection", WorkTrackingSystem = WorkTrackingSystems.Jira, AuthenticationMethodKey = "pat" };
            context.WorkTrackingSystemConnections.Add(connection);
            await context.SaveChangesAsync();
            var team = new Team { Name = "Lightspeed", WorkTrackingSystemConnectionId = connection.Id };
            context.Teams.Add(team);
            await context.SaveChangesAsync();
            return team;
        }
    }
}
