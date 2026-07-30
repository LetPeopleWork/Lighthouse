using Lighthouse.Backend.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests
{
    [TestFixture]
    public class QuerySplittingBehaviorTest
    {
        // The repositories suppress S8733 (EF Cartesian explosion) because splitting is configured
        // globally here. If that ever stops being true, the suppressions hide a real defect.
        [TestCase("sqlite")]
        [TestCase("postgres")]
        [TestCase("postgresql")]
        public void AddDbContext_ForEverySupportedProvider_UsesSplitQueries(string provider)
        {
            var connectionString = provider == "sqlite"
                ? $"Data Source={Path.Combine(TestContext.CurrentContext.WorkDirectory, $"{Guid.NewGuid()}.db")}"
                : "Host=localhost;Database=lighthouse";

            var builder = WebApplication.CreateBuilder();
            builder.Services.Configure<DatabaseConfiguration>(config =>
            {
                config.Provider = provider;
                config.ConnectionString = connectionString;
            });

            DatabaseConfigurator.AddDbContext(builder);

            using var serviceProvider = builder.Services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<DbContextOptions<LighthouseAppContext>>();

            var relationalOptions = options.Extensions.OfType<RelationalOptionsExtension>().Single();
            Assert.That(relationalOptions.QuerySplittingBehavior, Is.EqualTo(QuerySplittingBehavior.SplitQuery));
        }

        [Test]
        public void CreateDbContext_ForDesignTimeFactory_UsesSplitQueries()
        {
            var context = new LighthouseAppContextFactory().CreateDbContext([]);

            using (context)
            {
                var relationalOptions = context.GetService<IDbContextOptions>().Extensions
                    .OfType<RelationalOptionsExtension>().Single();
                Assert.That(relationalOptions.QuerySplittingBehavior, Is.EqualTo(QuerySplittingBehavior.SplitQuery));
            }
        }
    }
}
