using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Tests.API.Integration.Reports;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    /// <summary>
    /// The owner deleted between being looked up and its report being saved, on PostgreSQL: the report's foreign key
    /// refuses the save there as it does on SQLite, and the bulk run must answer that owner alone and go on. A
    /// provider without foreign keys would save the orphan and prove nothing.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6350-bulk-reports")]
    [Category("slice-02a")]
    public class BulkReportOwnerDeletedMidRunOnPostgresTests : BulkReportsAcceptanceTest
    {
        private SharedPostgresContainer? postgres;

        [OneTimeTearDown]
        public async Task StopPostgres()
        {
            if (postgres is not null)
            {
                await postgres.DisposeAsync();
            }
        }

        [SetUp]
        public async Task MoveTheInstanceToPostgres()
        {
            postgres ??= await SharedPostgresContainer.StartAsync();
            var connectionString = await postgres.ConnectionStringToANewCreatedDatabaseAsync();

            Client.Dispose();
            Factory = Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<LighthouseAppContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<LighthouseAppContext>>();
                services.AddDbContext<LighthouseAppContext>(options => options.UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    npgsql.MigrationsAssembly("Lighthouse.Migrations.Postgres");
                }));
            }));
            Client = Factory.CreateClient().AsSystemAdmin();

            using var scope = Factory.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.MigrateAsync();

            foreach (var seeder in scope.ServiceProvider.GetServices<ISeeder>())
            {
                await seeder.Seed();
            }
        }

        // @driving_port @real-io @adapter-integration @us-02a @slice-02a @error @contract-shape:bounded-change
        [Test]
        [Ignore(PendingEpic6350.Slice02a)]
        public async Task An_owner_deleted_between_lookup_and_save_fails_only_its_own_row_on_Postgres()
        {
            TheInstanceHasAPremiumLicence();
            var gravity = ATeam("Gravity");
            var zenith = ATeam("Zenith");
            TheCallerIsAdminOf(gravity, zenith);
            TheOwnerIsDeletedBetweenLookupAndSave(gravity);

            using var run = await CreateForEach(TheQuartersComparison, gravity, zenith);
            var outcomes = await TheOutcomesOf(run);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcomes.Select(outcome => (outcome.Status, outcome.Code)),
                    Is.EqualTo(new[] { (NotCreated, RunReason.NotSaved), (Created, (string?)null) }));
                Assert.That(StoredReports(), Is.EqualTo(1));
            }
        }
    }
}
