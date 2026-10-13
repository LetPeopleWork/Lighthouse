using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Reports;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests
{
    // The owner rules live in the database, not only in the model: the schema comes from the real SQLite
    // migrations, and the rows that break the rules are written as raw SQL so nothing in EF can stop them first.
    [TestFixture]
    public class ReportsTableConstraintsTest
    {
        private string databasePath = string.Empty;
        private DbContextOptions<LighthouseAppContext> options = null!;

        [SetUp]
        public void SetUp()
        {
            databasePath = Path.Combine(Path.GetTempPath(), $"reports-constraints-{Guid.NewGuid():N}.db");

            options = new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseSqlite(
                    $"Data Source={databasePath};Foreign Keys=True",
                    sqlite => sqlite.MigrationsAssembly("Lighthouse.Migrations.Sqlite"))
                .Options;

            using var context = CreateContext();
            context.Database.Migrate();
        }

        [TearDown]
        public void TearDown()
        {
            SqliteConnection.ClearAllPools();
            File.Delete(databasePath);
        }

        [Test]
        public async Task A_Reports_row_with_no_owner_or_two_owners_is_refused_and_deleting_its_Team_removes_it()
        {
            using var context = CreateContext();
            var team = await SeedTeam(context);
            var portfolio = await SeedPortfolio(context);

            var report = new Report
            {
                TeamId = team.Id,
                TemplateKey = "then-and-now",
                Name = "Before the coaching started",
                CreatedAt = new DateTime(2026, 10, 13, 9, 0, 0, DateTimeKind.Utc),
                ShownMetricKeysJson = """["cycle-time"]""",
                TemplatePayloadJson = "{}",
                PayloadSchemaVersion = 1,
            };
            context.Reports.Add(report);
            await context.SaveChangesAsync();

            var noOwner = await Assert.ThrowsAsync<SqliteException>(() => InsertReportRow(context, teamId: null, portfolioId: null));
            var twoOwners = await Assert.ThrowsAsync<SqliteException>(() => InsertReportRow(context, team.Id, portfolio.Id));

            await context.Teams.Where(t => t.Id == team.Id).ExecuteDeleteAsync();
            var reportsLeft = await context.Reports.AsNoTracking().CountAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(noOwner?.Message, Does.Contain("CHECK constraint failed"));
                Assert.That(twoOwners?.Message, Does.Contain("CHECK constraint failed"));
                Assert.That(reportsLeft, Is.Zero);
            }
        }

        private LighthouseAppContext CreateContext()
        {
            return new LighthouseAppContext(options, Mock.Of<ICryptoService>(), Mock.Of<ILogger<LighthouseAppContext>>());
        }

        private static Task<int> InsertReportRow(LighthouseAppContext context, int? teamId, int? portfolioId)
        {
            return context.Database.ExecuteSqlInterpolatedAsync($$"""
                INSERT INTO "Reports" ("TeamId", "PortfolioId", "TemplateKey", "Name", "CreatedAt",
                                       "ShownMetricKeysJson", "TemplatePayloadJson", "PayloadSchemaVersion", "ConcurrencyToken")
                VALUES ({{teamId}}, {{portfolioId}}, 'then-and-now', 'Refused', '2026-10-13 09:00:00', '[]', '{}', 1, {{Guid.NewGuid()}})
                """);
        }

        private static async Task<Team> SeedTeam(LighthouseAppContext context)
        {
            var team = new Team
            {
                Name = "Lightspeed",
                WorkTrackingSystemConnectionId = (await SeedConnection(context, "Team Connection")).Id,
            };
            context.Teams.Add(team);
            await context.SaveChangesAsync();
            return team;
        }

        private static async Task<Portfolio> SeedPortfolio(LighthouseAppContext context)
        {
            var portfolio = new Portfolio
            {
                Name = "Ocean Explorer",
                WorkTrackingSystemConnectionId = (await SeedConnection(context, "Portfolio Connection")).Id,
            };
            context.Portfolios.Add(portfolio);
            await context.SaveChangesAsync();
            return portfolio;
        }

        private static async Task<WorkTrackingSystemConnection> SeedConnection(LighthouseAppContext context, string name)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = name,
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                AuthenticationMethodKey = "pat",
            };
            context.WorkTrackingSystemConnections.Add(connection);
            await context.SaveChangesAsync();
            return connection;
        }
    }
}
