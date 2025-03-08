using Lighthouse.Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lighthouse.Backend.Data
{
    public class LighthouseAppContextFactory : IDesignTimeDbContextFactory<LighthouseAppContext>
    {
        public LighthouseAppContext CreateDbContext(string[] args)
        {
            string provider = Environment.GetEnvironmentVariable("Database__Provider") ?? "sqlite";
            string connectionString = Environment.GetEnvironmentVariable("Database__ConnectionString") ??
                "Data Source=lighthouse.db";

            var optionsBuilder = new DbContextOptionsBuilder<LighthouseAppContext>();

            if (provider.Equals("postgres", StringComparison.CurrentCultureIgnoreCase))
            {
                optionsBuilder.UseNpgsql(connectionString, npgsql =>
                {
                    npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    npgsql.MigrationsAssembly("Lighthouse.Migrations.Postgres");
                });
            }
            else
            {
                optionsBuilder.UseSqlite(connectionString, options =>
                {
                    options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    options.MigrationsAssembly("Lighthouse.Migrations.Sqlite");
                });
            }

            return new LighthouseAppContext(
                optionsBuilder.Options,
                new DesignTimeCryptoService(),
                NullLogger<LighthouseAppContext>.Instance);
        }
    }

    public class DesignTimeCryptoService : ICryptoService
    {
        public string Encrypt(string plainText) => plainText;

        public string Decrypt(string cipherText) => cipherText;
    }
}