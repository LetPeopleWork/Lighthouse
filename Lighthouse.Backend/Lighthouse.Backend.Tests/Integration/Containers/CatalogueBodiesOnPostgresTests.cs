using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Tests.API.Integration.FastWithManyFeatures;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    /// <summary>
    /// The pages that name and count Portfolios and Teams answer with the very bodies approved on SQLite, when the
    /// instance lives on PostgreSQL: the Portfolios list, the Teams list, one Team, one Portfolio and its settings.
    /// </summary>
    [TestFixture]
    public class CatalogueBodiesOnPostgresTests : FastWithManyFeaturesAcceptanceTest
    {
        private const string AConcurrencyToken = "concurrencyToken";

        private SharedPostgresContainer? postgres;
        private WebApplicationFactory<Program>? hostOnSqlite;
        private WebApplicationFactory<Program>? hostOnPostgres;

        // The base class built its host in its own one-time set-up, which has run by now. The fixture keeps that
        // host for the base's per-test reset and builds one more, on a Postgres database, for the tests to use.
        [OneTimeSetUp]
        public async Task BuildTheHostOnPostgres()
        {
            postgres = await SharedPostgresContainer.StartAsync();
            var connectionString = await postgres.ConnectionStringToANewCreatedDatabaseAsync();

            hostOnSqlite = Factory;
            hostOnPostgres = Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<LighthouseAppContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<LighthouseAppContext>>();
                services.AddDbContext<LighthouseAppContext>(options => options.UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    npgsql.MigrationsAssembly("Lighthouse.Migrations.Postgres");
                }));
            }));
        }

        [OneTimeTearDown]
        public async Task StopPostgres()
        {
            hostOnPostgres?.Dispose();

            if (postgres is not null)
            {
                await postgres.DisposeAsync();
            }
        }

        [SetUp]
        public async Task MoveTheInstanceToPostgres()
        {
            Client.Dispose();
            Factory = hostOnPostgres!;
            Factory.ForgetTheCachedMetrics();
            Client = Factory.CreateClient().AsSystemAdmin();

            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.MigrateAsync();

            foreach (var seeder in scope.ServiceProvider.GetServices<ISeeder>())
            {
                await seeder.Seed();
            }
        }

        // The base class's own per-test reset and clean-up work on the host it built.
        [TearDown]
        public void HandTheBaseItsOwnHostBack() => Factory = hostOnSqlite!;

        [TestCase("/api/latest/portfolios", "portfolios")]
        [TestCase("/api/latest/teams", "teams")]
        [TestCase("/api/latest/teams/{Atlas}", "team-atlas")]
        [TestCase("/api/latest/portfolios/{Platform}", "portfolio-northwind-platform")]
        [TestCase("/api/latest/portfolios/{Platform}/settings", "portfolio-northwind-platform-settings")]
        public async Task Every_body_equals_the_one_approved_on_Sqlite(string route, string approvedName)
        {
            var northwind = GivenTheNorthwindInstance();
            var theRoute = route
                .Replace("{Atlas}", northwind.Atlas.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
                .Replace("{Platform}", northwind.Platform.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);

            var body = await TheBodyOf(theRoute);

            ApprovedBodies.Verify(body, ApprovedBodiesFolder, $"slice02-{approvedName}", AConcurrencyToken);
        }
    }
}
