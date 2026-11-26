using System.Data.Common;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Platform.Encryption;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.Integration.Containers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests
{
    // The schema comes from the real migrations and every note row is written as raw SQL, so nothing in EF can
    // stand between the test and what the database itself refuses or does.
    [TestFixture]
    public class ReportNotesTableConstraintsTest
    {
        private static readonly string[] TheColumns =
            ["Id", "ReportId", "Text", "CreatedAt", "LastEditedAt", "AuthorUserProfileId", "AuthorDisplayName"];

        private string templatePath = string.Empty;
        private string databasePath = string.Empty;
        private DbContextOptions<LighthouseAppContext> options = null!;

        // The migrations run once per fixture into a template file; each test works on its own copy of it.
        [OneTimeSetUp]
        public void MigrateTheTemplateOnce()
        {
            templatePath = Path.Combine(Path.GetTempPath(), $"report-notes-template-{Guid.NewGuid():N}.db");
            using var context = CreateContext(OptionsFor(templatePath));
            context.Database.Migrate();
            SqliteConnection.ClearAllPools();
        }

        [OneTimeTearDown]
        public void RemoveTheTemplate()
        {
            SqliteConnection.ClearAllPools();
            File.Delete(templatePath);
        }

        [SetUp]
        public void GiveTheTestACopyOfTheTemplate()
        {
            databasePath = Path.Combine(Path.GetTempPath(), $"report-notes-constraints-{Guid.NewGuid():N}.db");
            File.Copy(templatePath, databasePath);
            options = OptionsFor(databasePath);
        }

        [TearDown]
        public void TearDown()
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }

        private static DbContextOptions<LighthouseAppContext> OptionsFor(string path)
            => new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseSqlite($"Data Source={path};Foreign Keys=True;Pooling=False", sqlite => sqlite.MigrationsAssembly("Lighthouse.Migrations.Sqlite"))
                .Options;

        [Test]
        public async Task The_notes_table_holds_the_columns_a_note_is_made_of()
        {
            using var context = CreateContext(options);

            var columns = await context.Database
                .SqlQuery<string>($"SELECT name AS Value FROM pragma_table_info('ReportNotes')")
                .ToListAsync();

            Assert.That(columns, Is.EquivalentTo(TheColumns), "the ReportNotes table must exist with exactly these columns");
        }

        [Test]
        public async Task Deleting_a_report_deletes_its_notes_and_leaves_the_notes_of_other_reports()
        {
            using var context = CreateContext(options);
            var team = await SeedTeam(context);
            var deleted = await SeedReport(context, team.Id);
            var kept = await SeedReport(context, team.Id);
            await InsertNote(context, deleted, "Training took place", null);
            await InsertNote(context, deleted, "Team grew to 9", null);
            await InsertNote(context, kept, "Training took place", null);

            await context.Reports.Where(report => report.Id == deleted).ExecuteDeleteAsync();

            var deletedLeft = await NotesOf(context, deleted);
            var keptLeft = await NotesOf(context, kept);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(deletedLeft, Is.Zero, "the notes of a deleted report go with it");
                Assert.That(keptLeft, Is.EqualTo(1), "another report keeps its notes");
            }
        }

        [Test]
        public async Task Removing_the_author_keeps_the_note_and_its_name_but_clears_the_link_to_the_author()
        {
            using var context = CreateContext(options);
            var team = await SeedTeam(context);
            var report = await SeedReport(context, team.Id);
            var profile = new UserProfile { Subject = "ann", SubjectClaimType = "sub", DisplayName = "Ann", CreatedAt = DateTime.UtcNow, LastSeenAt = DateTime.UtcNow };
            context.UserProfiles.Add(profile);
            await context.SaveChangesAsync();
            await InsertNote(context, report, "Training took place", profile.Id, "Ann");

            await context.UserProfiles.Where(p => p.Id == profile.Id).ExecuteDeleteAsync();

            var author = await context.Database.SqlQuery<int?>($"SELECT \"AuthorUserProfileId\" AS \"Value\" FROM \"ReportNotes\"").SingleAsync();
            var name = await context.Database.SqlQuery<string>($"SELECT \"AuthorDisplayName\" AS \"Value\" FROM \"ReportNotes\"").SingleAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(author, Is.Null);
                Assert.That(name, Is.EqualTo("Ann"));
            }
        }

        // SQLite keeps whatever length it is given, so the bound on the text column is observed on PostgreSQL.
        [Test]
        [Category("Integration")]
        public async Task The_text_column_keeps_2000_characters_and_refuses_2001_on_PostgreSQL()
        {
            await using var postgres = await SharedPostgresContainer.StartAsync();
            var postgresOptions = new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseNpgsql(await postgres.ConnectionStringToANewCreatedDatabaseAsync(), npgsql => npgsql.MigrationsAssembly("Lighthouse.Migrations.Postgres"))
                .Options;
            await using var context = CreateContext(postgresOptions);
            await context.Database.MigrateAsync();
            var report = await SeedReport(context, (await SeedTeam(context)).Id);

            await InsertNote(context, report, new string('x', ReportNote.LongestText), null);
            var refused = await Assert.CatchAsync<DbException>(() => InsertNote(context, report, new string('x', ReportNote.LongestText + 1), null));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused?.Message, Does.Contain("too long"));
                Assert.That(await NotesOf(context, report), Is.EqualTo(1));
            }
        }

        private static LighthouseAppContext CreateContext(DbContextOptions<LighthouseAppContext> contextOptions)
            => new(contextOptions, Mock.Of<ICryptoService>(), Mock.Of<ILogger<LighthouseAppContext>>());

        private static Task<int> InsertNote(LighthouseAppContext context, int reportId, string text, int? authorId, string? authorName = null)
            => context.Database.ExecuteSqlAsync($$"""
                INSERT INTO "ReportNotes" ("ReportId", "Text", "CreatedAt", "AuthorUserProfileId", "AuthorDisplayName")
                VALUES ({{reportId}}, {{text}}, '2026-10-13 09:00:00', {{authorId}}, {{authorName}})
                """);

        private static Task<int> NotesOf(LighthouseAppContext context, int reportId)
            => context.Database.SqlQuery<int>($"SELECT COUNT(*) AS \"Value\" FROM \"ReportNotes\" WHERE \"ReportId\" = {reportId}").SingleAsync();

        private static async Task<int> SeedReport(LighthouseAppContext context, int teamId)
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

        private static async Task<Team> SeedTeam(LighthouseAppContext context)
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
