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

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    // SQLite keeps whatever length it is given, so the bound on the text column is observed on PostgreSQL.
    [TestFixture]
    public class ReportNotesTextBoundOnPostgresTest : IntegrationFixture
    {
        // SQLite keeps whatever length it is given, so the bound on the text column is observed on PostgreSQL.
        [Test]
        public async Task The_text_column_keeps_2000_characters_and_refuses_2001_on_PostgreSQL()
        {
            await using var postgres = await SharedPostgresContainer.StartAsync();
            var postgresOptions = new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseNpgsql(await postgres.ConnectionStringToANewCreatedDatabaseAsync(), npgsql => npgsql.MigrationsAssembly("Lighthouse.Migrations.Postgres"))
                .Options;
            await using var context = Lighthouse.Backend.Tests.ReportNoteRows.CreateContext(postgresOptions);
            await context.Database.MigrateAsync();
            var report = await Lighthouse.Backend.Tests.ReportNoteRows.SeedReport(context, (await Lighthouse.Backend.Tests.ReportNoteRows.SeedTeam(context)).Id);

            await Lighthouse.Backend.Tests.ReportNoteRows.InsertNote(context, report, new string('x', ReportNote.LongestText), null);
            var refused = await Assert.CatchAsync<DbException>(() => Lighthouse.Backend.Tests.ReportNoteRows.InsertNote(context, report, new string('x', ReportNote.LongestText + 1), null));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused?.Message, Does.Contain("too long"));
                Assert.That(await Lighthouse.Backend.Tests.ReportNoteRows.NotesOf(context, report), Is.EqualTo(1));
            }
        }
    }
}
