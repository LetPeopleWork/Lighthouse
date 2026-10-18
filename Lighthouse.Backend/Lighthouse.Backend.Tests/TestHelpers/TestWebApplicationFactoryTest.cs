using Lighthouse.Backend.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    public class TestWebApplicationFactoryTest
    {
        [Test]
        public void AFreshDatabaseNeverReusesThePathOfAnEarlierOne()
        {
            using var factory = new TestWebApplicationFactory<Program>();

            var first = PathOfCurrentDatabase(factory);
            factory.UseFreshDatabase();
            var second = PathOfCurrentDatabase(factory);

            Assert.That(second, Is.Not.EqualTo(first));
        }

        // The product switches its database file to write-ahead logging when it opens it, and that setting
        // is stored in the file itself, so a product database still in its original mode after a test host
        // has handed out a database context is one the product's own setup never opened. Startup may still
        // read the file to look for stored secrets; that read leaves the mode alone.
        [Test]
        public void UsingTheTestDatabaseNeverOpensTheProductDatabase()
        {
            var productDirectory = Path.Combine(Path.GetTempPath(), $"lighthouse-product-db-{Guid.NewGuid():N}");
            var productConnectionString = $"Data Source={Path.Combine(productDirectory, "LighthouseAppContext.db")};Pooling=False";

            try
            {
                Directory.CreateDirectory(productDirectory);
                Execute(productConnectionString, "CREATE TABLE Untouched (Id INTEGER)");

                using (var root = new TestWebApplicationFactory<Program>())
                using (var factory = root.WithWebHostBuilder(builder =>
                    builder.UseSetting("Database:ConnectionString", productConnectionString)))
                using (var scope = factory.Services.CreateScope())
                {
                    scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureCreated();
                }

                Assert.That(Execute(productConnectionString, "PRAGMA journal_mode"), Is.EqualTo("delete"));
            }
            finally
            {
                Directory.Delete(productDirectory, recursive: true);
            }
        }

        [Test]
        public void TheTestDatabaseSplitsQueriesLikeTheProductDoes()
        {
            using var factory = new TestWebApplicationFactory<Program>();
            using var scope = factory.Services.CreateScope();

            var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<LighthouseAppContext>>();

            var relationalOptions = options.Extensions.OfType<RelationalOptionsExtension>().Single();
            Assert.That(relationalOptions.QuerySplittingBehavior, Is.EqualTo(QuerySplittingBehavior.SplitQuery));
        }

        private static string PathOfCurrentDatabase(TestWebApplicationFactory<Program> factory)
        {
            using var scope = factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.GetDbConnection().DataSource;
        }

        private static string? Execute(string connectionString, string sql)
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            return command.ExecuteScalar()?.ToString();
        }
    }
}
