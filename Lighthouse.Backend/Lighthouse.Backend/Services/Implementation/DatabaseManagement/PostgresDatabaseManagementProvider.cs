using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Interfaces.DatabaseManagement;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Diagnostics;

namespace Lighthouse.Backend.Services.Implementation.DatabaseManagement
{
    public class PostgresDatabaseManagementProvider : IDatabaseManagementProvider
    {
        private const string BackupFileName = "lighthouse.pgdump";
        private const string BackupToolName = "pg_dump";
        private const string RestoreToolName = "pg_restore";
        private const string PsqlToolName = "psql";
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
            var builder = new NpgsqlConnectionStringBuilder(dbConfig.Value.ConnectionString);
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

        public async Task RestoreBackup(string backupContentPath)
        {
            var dumpFile = Path.Combine(backupContentPath, BackupFileName);
            if (!File.Exists(dumpFile))
            {
                throw new FileNotFoundException($"Backup does not contain expected dump file: {BackupFileName}");
            }

            logger.LogInformation("Restoring PostgreSQL database from {Source}", dumpFile);

            var startInfo = new ProcessStartInfo
            {
                FileName = RestoreToolName,
                Arguments = $"--host={host} --port={port} --username={username} --dbname={database} --clean --if-exists {dumpFile}",
            };
            startInfo.Environment["PGPASSWORD"] = password;

            var result = await commandRunner.RunAsync(startInfo);

            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException($"{RestoreToolName} failed (exit code {result.ExitCode}): {result.StandardError}");
            }

            logger.LogInformation("PostgreSQL restore completed successfully");
        }

        public async Task ClearDatabase()
        {
            logger.LogInformation("Clearing PostgreSQL database {Database}", database);

            await RunPsqlCommand($"DROP DATABASE IF EXISTS \"{database}\" WITH (FORCE);", "Failed to drop database");
            await RunPsqlCommand($"CREATE DATABASE \"{database}\" OWNER \"{username}\";", "Failed to create database");

            logger.LogInformation("PostgreSQL database {Database} cleared and recreated", database);
        }

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
    }
}
