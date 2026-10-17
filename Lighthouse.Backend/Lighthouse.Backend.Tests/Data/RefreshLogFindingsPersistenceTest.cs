using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests
{
    // The schema comes from the real SQLite migrations, so a row an older release wrote can be written the way
    // it wrote it: as raw SQL that knows nothing of the findings column.
    [TestFixture]
    public class RefreshLogFindingsPersistenceTest
    {
        private static readonly string[] BothDependencyPhrases = ["depends on", "is depended on by"];

        private static readonly RefreshFindingKind[] OnlyTheKindItKnows = [RefreshFindingKind.NamedLinkPointsBothWays];

        private string databasePath = string.Empty;
        private DbContextOptions<LighthouseAppContext> options = null!;

        [SetUp]
        public void SetUp()
        {
            databasePath = Path.Combine(Path.GetTempPath(), $"refresh-log-findings-{Guid.NewGuid():N}.db");

            options = new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseSqlite(
                    $"Data Source={databasePath}",
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
        public async Task A_refresh_rows_findings_read_back_exactly_as_they_were_written()
        {
            int rowId;

            using (var context = CreateContext())
            {
                var row = APortfolioRefresh();
                row.Findings =
                [
                    new RefreshFinding(RefreshFindingKind.NamedLinkPointsBothWays, RefreshFindingSetting.DependencyField, "Dependency", BothDependencyPhrases),
                ];
                context.RefreshLogs.Add(row);
                await context.SaveChangesAsync();
                rowId = row.Id;
            }

            using (var context = CreateContext())
            {
                var saved = await context.RefreshLogs.AsNoTracking().SingleAsync(row => row.Id == rowId);
                var theFinding = saved.Findings?.FirstOrDefault();

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(saved.Findings, Has.Count.EqualTo(1));
                    Assert.That(theFinding?.Kind, Is.EqualTo(RefreshFindingKind.NamedLinkPointsBothWays));
                    Assert.That(theFinding?.Setting, Is.EqualTo(RefreshFindingSetting.DependencyField));
                    Assert.That(theFinding?.Named, Is.EqualTo("Dependency"));
                    Assert.That(theFinding?.Seen, Is.EqualTo(BothDependencyPhrases));
                }
            }
        }

        [Test]
        public async Task A_finding_is_stored_by_the_name_of_its_kind_and_setting()
        {
            using (var context = CreateContext())
            {
                var row = APortfolioRefresh();
                row.Findings =
                [
                    new RefreshFinding(RefreshFindingKind.NamedLinkPointsBothWays, RefreshFindingSetting.DependencyField, "Dependency", BothDependencyPhrases),
                ];
                context.RefreshLogs.Add(row);
                await context.SaveChangesAsync();
            }

            using (var context = CreateContext())
            {
                var stored = await context.Database
                    .SqlQueryRaw<string>("SELECT \"Findings\" AS \"Value\" FROM \"RefreshLogs\"")
                    .SingleAsync();

                Assert.That(stored, Does.Contain("NamedLinkPointsBothWays").And.Contain("DependencyField"),
                    "Stored by name, a member added to either list later cannot relabel a finding already written.");
            }
        }

        [Test]
        public async Task A_refresh_row_written_before_findings_were_recorded_still_reads()
        {
            using (var context = CreateContext())
            {
                await context.Database.ExecuteSqlRawAsync(
                    """
                    INSERT INTO "RefreshLogs" ("Type", "EntityId", "EntityName", "ItemCount", "Mode", "RecordsScanned", "RecordsFetched",
                        "DurationMs", "ExecutedAt", "Success", "Cancelled", "RecordsWhoseLinksNamedMoreThanOneParent",
                        "RecordsPlacedFromLinksNamingMoreThanOneParent")
                    VALUES (1, 7, 'Payments Platform', 12, 0, 12, 12, 840, '2026-10-01 09:00:00', 1, 0, 0, 0)
                    """);
            }

            using (var context = CreateContext())
            {
                var saved = await context.RefreshLogs.AsNoTracking().SingleAsync();

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(saved.EntityName, Is.EqualTo("Payments Platform"));
                    Assert.That(saved.Findings, Is.Null);
                }
            }
        }

        [Test]
        public async Task A_finding_of_a_kind_this_release_no_longer_knows_is_left_out_and_the_rest_of_the_row_still_reads()
        {
            using (var context = CreateContext())
            {
                context.RefreshLogs.Add(APortfolioRefresh());
                await context.SaveChangesAsync();
                await context.Database.ExecuteSqlRawAsync(
                    """UPDATE "RefreshLogs" SET "Findings" = {0}""",
                    """[{"kind":"RelationTypeNotYetReadForDependencies","setting":"DependencyField","named":"Contoso.DependsOn-Forward","seen":[]},{"kind":"NamedLinkPointsBothWays","setting":"DependencyField","named":"Dependency","seen":["depends on","is depended on by"]}]""");
            }

            using (var context = CreateContext())
            {
                var saved = await context.RefreshLogs.AsNoTracking().SingleAsync();

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(saved.EntityName, Is.EqualTo("Payments Platform"));
                    Assert.That(saved.Findings?.Select(finding => finding.Kind), Is.EqualTo(OnlyTheKindItKnows));
                }
            }
        }

        private static RefreshLog APortfolioRefresh() => new()
        {
            Type = RefreshType.Portfolio,
            EntityId = 7,
            EntityName = "Payments Platform",
            ItemCount = 12,
            Mode = SyncMode.Full,
            RecordsScanned = 12,
            RecordsFetched = 12,
            DurationMs = 840,
            ExecutedAt = new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc),
            Success = true,
        };

        private LighthouseAppContext CreateContext()
            => new(options, Mock.Of<ICryptoService>(), Mock.Of<ILogger<LighthouseAppContext>>());
    }
}
