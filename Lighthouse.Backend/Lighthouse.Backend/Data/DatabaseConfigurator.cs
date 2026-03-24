using Lighthouse.Backend.Services.Implementation.DatabaseManagement;
using Lighthouse.Backend.Services.Interfaces.DatabaseManagement;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Data
{
    public static class DatabaseConfigurator
    {
        public static void AddDatabaseConfiguration(WebApplicationBuilder builder)
        {
            // Configure database settings from appsettings.json
            builder.Services.Configure<DatabaseConfiguration>(
                builder.Configuration.GetSection("Database"));
        }

        public static void AddDbContext(WebApplicationBuilder builder)
        {
            builder.Services.AddDbContext<LighthouseAppContext>((provider, options) =>
            {
                var dbConfig = provider.GetRequiredService<IOptions<DatabaseConfiguration>>().Value;
                switch (dbConfig.Provider.ToLower())
                {
                    case "postgresql":
                    case "postgres":
                        options.UseNpgsql(dbConfig.ConnectionString,
                            npgsql =>
                            {
                                npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                                npgsql.MigrationsAssembly("Lighthouse.Migrations.Postgres");
                                npgsql.EnableRetryOnFailure(
                                    maxRetryCount: 3,
                                    maxRetryDelay: TimeSpan.FromSeconds(5),
                                    errorCodesToAdd: null);
                                npgsql.CommandTimeout(30);
                            });

                        // Log slow queries in development
                        if (builder.Environment.IsDevelopment())
                        {
                            options.EnableSensitiveDataLogging();
                            options.EnableDetailedErrors();
                        }
                        break;
                    case "sqlite":
                        // Ensure the directory for the SQLite database exists
                        var connectionStringBuilder = new SqliteConnectionStringBuilder(dbConfig.ConnectionString);
                        var dataSource = connectionStringBuilder.DataSource;
                        var directory = Path.GetDirectoryName(dataSource);
                        if (!string.IsNullOrEmpty(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        // Create and configure SQLite connection with WAL mode
                        var connection = new SqliteConnection(dbConfig.ConnectionString);
                        connection.Open();

                        // Enable WAL mode and other optimizations
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = @"
                    PRAGMA journal_mode=WAL;
                    PRAGMA busy_timeout=10000;
                    PRAGMA synchronous=NORMAL;
                ";
                            command.ExecuteNonQuery();
                        }

                        options.UseSqlite(connection,
                            sqliteOptions =>
                            {
                                sqliteOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                                sqliteOptions.MigrationsAssembly("Lighthouse.Migrations.Sqlite");
                            });
                        break;
                    default:
                        throw new NotSupportedException($"Database provider '{dbConfig.Provider}' is not supported.");
                }
            });
        }

        public static void ApplyMigrations(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<LighthouseAppContext>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("Migrating Database");
            context.Database.Migrate();
        }

        public static void SeedDatabase(IServiceProvider serviceProvider)
        {
            var seeders = serviceProvider.GetServices<ISeeder>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("Seeding Database with {SeederCount} seeders", seeders.Count());

            foreach (var seeder in seeders)
            {
                seeder.Seed().GetAwaiter().GetResult();
            }
        }

        public static void RegisterDatabaseManagementProvider(WebApplicationBuilder builder)
        {
            builder.Services.AddSingleton<IDatabaseManagementProvider>(sp =>
            {
                var dbConfig = sp.GetRequiredService<IOptions<DatabaseConfiguration>>().Value;
                switch (dbConfig.Provider.ToLowerInvariant())
                {
                    case "sqlite":
                        return new SqliteDatabaseManagementProvider(
                            sp.GetRequiredService<IOptions<DatabaseConfiguration>>(),
                            sp.GetRequiredService<ILogger<SqliteDatabaseManagementProvider>>());
                    case "postgresql":
                    case "postgres":
                        return new PostgresDatabaseManagementProvider(
                            sp.GetRequiredService<IOptions<DatabaseConfiguration>>(),
                            sp.GetRequiredService<ICommandRunner>(),
                            sp.GetRequiredService<ILogger<PostgresDatabaseManagementProvider>>());
                    default:
                        throw new NotSupportedException($"Database management provider '{dbConfig.Provider}' is not supported.");
                }
            });
        }
    }
}