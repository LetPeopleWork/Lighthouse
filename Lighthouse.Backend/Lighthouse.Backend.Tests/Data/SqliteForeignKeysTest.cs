using Lighthouse.Backend.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests
{
    [TestFixture]
    public class SqliteForeignKeysTest
    {
        // Deleting a forecast relies on the database to delete its histogram rows, which only happens
        // while foreign keys are enforced. A connection string that switches them off must not win.
        [TestCase("")]
        [TestCase(";Foreign Keys=False")]
        public void AddDbContext_ForSqlite_EnforcesForeignKeysWhateverTheConnectionStringSays(string extraConnectionStringPart)
        {
            var databasePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"{Guid.NewGuid()}.db");
            var builder = WebApplication.CreateBuilder();
            builder.Services.Configure<DatabaseConfiguration>(config =>
            {
                config.Provider = "sqlite";
                config.ConnectionString = $"Data Source={databasePath};Pooling=False{extraConnectionStringPart}";
            });

            DatabaseConfigurator.AddDbContext(builder);

            using var serviceProvider = builder.Services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<DbContextOptions<LighthouseAppContext>>();
            var connection = options.Extensions.OfType<RelationalOptionsExtension>().Single().Connection!;

            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA foreign_keys;";

            Assert.That(Convert.ToInt64(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture), Is.EqualTo(1));
            connection.Dispose();
        }
    }
}
