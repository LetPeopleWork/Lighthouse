using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Tests.API.Integration.FastWithManyFeatures;
using Lighthouse.Backend.Tests.TestHelpers;
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
    [Category("epic-6400-fast-with-many-features")]
    [Category("slice-02")]
    public class CatalogueBodiesOnPostgresTests : FastWithManyFeaturesAcceptanceTest
    {
        private const string AConcurrencyToken = "concurrencyToken";

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
