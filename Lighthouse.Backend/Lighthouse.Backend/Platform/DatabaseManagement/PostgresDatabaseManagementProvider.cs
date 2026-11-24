using Lighthouse.Backend.Data;
using Lighthouse.Backend.Platform.DatabaseManagement;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Data.Common;
using System.Diagnostics;

namespace Lighthouse.Backend.Platform.DatabaseManagement
{
    public class PostgresDatabaseManagementProvider : IDatabaseManagementProvider
    {
        private const string BackupFileName = "lighthouse.pgdump";
        private const string BackupToolName = "pg_dump";
        private const string RestoreToolName = "pg_restore";
        private const string PsqlToolName = "psql";
        private const string ScratchDatabaseSuffix = "_restore_check";
        private const string CredentialTables = "--table=WorkTrackingSystemConnectionOption --table=OAuthCredentials";
        private readonly string connectionString;
        private readonly string host;
        private readonly int port;
        private readonly string database;
        private readonly string username;
        private readonly string password;
        private readonly ICommandRunner commandRunner;
        private readonly ILogger<PostgresDatabaseManagementProvider> logger;

        public PostgresDatabaseManagementProvider(
            IOptions<DatabaseConfiguration> dbConfig,
            ICommandRunner commandRunner,
            ILogger<PostgresDatabaseManagementProvider> logger)
        {
            connectionString = dbConfig.Value.ConnectionString;
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            host = builder.Host ?? "localhost";
            port = builder.Port;
            database = builder.Database ?? "lighthouse";
            username = builder.Username ?? "postgres";
            password = builder.Password ?? string.Empty;

            this.commandRunner = commandRunner;
            this.logger = logger;
        }

        public string ProviderName => "postgresql";

        public bool IsToolingAvailable()
        {
            return commandRunner.IsToolAvailable(BackupToolName) && commandRunner.IsToolAvailable(RestoreToolName) && commandRunner.IsToolAvailable(PsqlToolName);
        }

        public string? GetToolingGuidanceMessage()
        {
            if (IsToolingAvailable())
            {
                return null;
            }

            return $"{BackupToolName} and {RestoreToolName} must be installed on the Lighthouse server to enable database backup and restore operations.";
        }

        public string? GetToolingGuidanceUrl()
        {
            if (IsToolingAvailable())
            {
                return null;
            }

            return "https://www.postgresql.org/download/";
        }

        public string? GetServerVersion()
        {
            if (!commandRunner.IsToolAvailable(BackupToolName))
            {
                return null;
            }

            try
            {
                var result = commandRunner.RunAsync(new ProcessStartInfo
                {
                    FileName = BackupToolName,
                    Arguments = "--version",
                }, CancellationToken.None).GetAwaiter().GetResult();

                return result.ExitCode == 0 ? result.StandardOutput.Trim() : null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<string> CreateBackup(string destinationPath)
        {
            var outputFile = Path.Combine(destinationPath, BackupFileName);

            logger.LogInformation("Creating PostgreSQL backup to {Destination}", outputFile);

            var startInfo = new ProcessStartInfo
            {
                FileName = BackupToolName,
                Arguments = $"--host={host} --port={port} --username={username} --format=custom --file={outputFile}  {database}",
            };
            startInfo.Environment["PGPASSWORD"] = password;

            var result = await commandRunner.RunAsync(startInfo);

            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException($"{BackupToolName} failed (exit code {result.ExitCode}): {result.StandardError}");
            }

            logger.LogInformation("PostgreSQL backup completed successfully");
            return outputFile;
        }

        // Only the two credential tables go into the scratch database: that is all the check reads, and it keeps the
        // check a fraction of a full restore. The scratch database is dropped again even when pg_restore fails.
        public async Task<IStagedBackup> StageBackup(string backupContentPath)
        {
            var dumpFile = DumpFileIn(backupContentPath);
            var scratchDatabase = database + ScratchDatabaseSuffix;

            logger.LogInformation("Staging the credential tables of {Source} in {ScratchDatabase}", dumpFile, scratchDatabase);

            await DropDatabase(scratchDatabase);
            await CreateDatabase(scratchDatabase);

            var staged = new ScratchDatabase(
                new NpgsqlConnectionStringBuilder(connectionString) { Database = scratchDatabase, Pooling = false }.ToString(),
                () => DropDatabase(scratchDatabase));

            var restored = false;
            try
            {
                await RunPgRestore(scratchDatabase, $"{CredentialTables} {dumpFile}");
                restored = true;
            }
            finally
            {
                if (!restored)
                {
                    await staged.DisposeAsync();
                }
            }

            return staged;
        }

        public async Task RestoreBackup(string backupContentPath)
        {
            var dumpFile = DumpFileIn(backupContentPath);

            await ClearDatabase();

            logger.LogInformation("Restoring PostgreSQL database from {Source}", dumpFile);

            await RunPgRestore(database, dumpFile);

            logger.LogInformation("PostgreSQL restore completed successfully");
        }

        private static string DumpFileIn(string backupContentPath)
        {
            var dumpFile = Path.Combine(backupContentPath, BackupFileName);
            if (!File.Exists(dumpFile))
            {
                throw new FileNotFoundException($"Backup does not contain expected dump file: {BackupFileName}");
            }

            return dumpFile;
        }

        private async Task RunPgRestore(string targetDatabase, string restoreArguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = RestoreToolName,
                Arguments = $"--host={host} --port={port} --username={username} --dbname={targetDatabase} {restoreArguments}",
            };
            startInfo.Environment["PGPASSWORD"] = password;

            var result = await commandRunner.RunAsync(startInfo);

            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException($"{RestoreToolName} failed (exit code {result.ExitCode}): {result.StandardError}");
            }
        }

        public async Task ClearDatabase()
        {
            logger.LogInformation("Clearing PostgreSQL database {Database}", database);

            await DropDatabase(database);
            await CreateDatabase(database);

            logger.LogInformation("PostgreSQL database {Database} cleared and recreated", database);
        }

        public DbConnection CreateConnection() => new NpgsqlConnection(connectionString);

        public void RecycleConnection()
        {
            NpgsqlConnection.ClearAllPools();
        }

        private Task DropDatabase(string name)
            => RunPsqlCommand($"DROP DATABASE IF EXISTS \"{name}\" WITH (FORCE);", "Failed to drop database");

        private Task CreateDatabase(string name)
            => RunPsqlCommand($"CREATE DATABASE \"{name}\" OWNER \"{username}\";", "Failed to create database");

        private async Task RunPsqlCommand(string sql, string errorPrefix)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = PsqlToolName,
                Arguments = $"--host={host} --port={port} --username={username} --dbname=postgres -c \"{sql}\"",
            };
            startInfo.Environment["PGPASSWORD"] = password;

            var result = await commandRunner.RunAsync(startInfo);

            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException($"{errorPrefix} (exit code {result.ExitCode}): {result.StandardError}");
            }
        }

        private sealed class ScratchDatabase(string scratchConnectionString, Func<Task> drop) : IStagedBackup
        {
            public DbConnection OpenConnection() => new NpgsqlConnection(scratchConnectionString);

            public async ValueTask DisposeAsync() => await drop();
        }
    }
}
