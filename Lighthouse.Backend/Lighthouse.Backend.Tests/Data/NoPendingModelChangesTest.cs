using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests
{
    /// <summary>
    /// The model in code and the latest migration snapshot of each provider must agree. A property or
    /// index added without a migration (or a CLR default that changes on every model build) shows up here
    /// instead of as an instance that never finishes starting. Nothing is connected to: EF compares the
    /// model with the snapshot in the migrations assembly.
    /// </summary>
    [TestFixture]
    public class NoPendingModelChangesTest
    {
        [Test]
        public void TheSqliteMigrationsDescribeTheCurrentModel()
        {
            var options = new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseSqlite(
                    "Data Source=not-opened.db",
                    sqlite => sqlite.MigrationsAssembly("Lighthouse.Migrations.Sqlite"))
                .Options;

            using var context = CreateContext(options);

            Assert.That(
                context.Database.HasPendingModelChanges(),
                Is.False,
                "The model changed without a migration for SQLite. Generate one with Create-Migration.ps1.");
        }

        [Test]
        public void ThePostgresMigrationsDescribeTheCurrentModel()
        {
            var options = new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseNpgsql(
                    "Host=not-opened;Database=not-opened",
                    npgsql => npgsql.MigrationsAssembly("Lighthouse.Migrations.Postgres"))
                .Options;

            using var context = CreateContext(options);

            Assert.That(
                context.Database.HasPendingModelChanges(),
                Is.False,
                "The model changed without a migration for PostgreSQL. Generate one with Create-Migration.ps1.");
        }

        private static LighthouseAppContext CreateContext(DbContextOptions<LighthouseAppContext> options)
            => new(options, Mock.Of<ICryptoService>(), Mock.Of<ILogger<LighthouseAppContext>>());
    }
}
