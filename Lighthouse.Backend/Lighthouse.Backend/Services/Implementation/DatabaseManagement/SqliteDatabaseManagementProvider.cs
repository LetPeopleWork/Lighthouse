using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Interfaces.DatabaseManagement;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace Lighthouse.Backend.Services.Implementation.DatabaseManagement
{
    public class SqliteDatabaseManagementProvider : IDatabaseManagementProvider
    {
        private readonly string databasePath;
        private readonly ILogger<SqliteDatabaseManagementProvider> logger;

        public SqliteDatabaseManagementProvider(
            IOptions<DatabaseConfiguration> dbConfig,
            ILogger<SqliteDatabaseManagementProvider> logger)
        {
            var connectionStringBuilder = new SqliteConnectionStringBuilder(dbConfig.Value.ConnectionString);
            databasePath = connectionStringBuilder.DataSource;
            this.logger = logger;
        }

        public string ProviderName => "sqlite";

        public bool IsToolingAvailable() => true;

        public string? GetToolingGuidanceMessage() => null;

        public string? GetToolingGuidanceUrl() => null;

        public string? GetServerVersion() => null;

        public async Task<string> CreateBackup(string destinationPath)
        {
            if (!File.Exists(databasePath))
            {
                throw new FileNotFoundException($"Database file not found: {databasePath}");
            }

            var fileName = Path.GetFileName(databasePath);
            var destinationFile = Path.Combine(destinationPath, fileName);

            logger.LogInformation("Creating SQLite backup from {Source} to {Destination}", databasePath, destinationFile);

            await CopyFileAsync(databasePath, destinationFile);

            // Copy WAL and SHM files if they exist
            await CopyCompanionFileIfExists(databasePath + "-wal", Path.Combine(destinationPath, fileName + "-wal"));
            await CopyCompanionFileIfExists(databasePath + "-shm", Path.Combine(destinationPath, fileName + "-shm"));

            return destinationFile;
        }

        public async Task RestoreBackup(string backupContentPath)
        {
            var backupDbFile = Directory.GetFiles(backupContentPath, "*.db").FirstOrDefault() ?? throw new FileNotFoundException("Backup does not contain a .db file");

            var backupBaseName = Path.GetFileName(backupDbFile);

            if (!File.Exists(backupDbFile))
            {
                throw new FileNotFoundException($"Backup does not contain expected database file: {backupBaseName}");
            }

            logger.LogInformation("Restoring SQLite database from {Source} to {Destination}", backupDbFile, databasePath);

            // Ensure target directory exists
            var directory = Path.GetDirectoryName(databasePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Replace main database file
            await CopyFileAsync(backupDbFile, databasePath);

            // Restore or remove WAL file
            await RestoreOrRemoveCompanionFile(
                Path.Combine(backupContentPath, backupBaseName + "-wal"),
                databasePath + "-wal");

            // Restore or remove SHM file
            await RestoreOrRemoveCompanionFile(
                Path.Combine(backupContentPath, backupBaseName + "-shm"),
                databasePath + "-shm");
        }

        public Task ClearDatabase()
        {
            logger.LogInformation("Clearing SQLite database at {Path}", databasePath);

            DeleteFileIfExists(databasePath);
            DeleteFileIfExists(databasePath + "-wal");
            DeleteFileIfExists(databasePath + "-shm");

            return Task.CompletedTask;
        }

        public void RecycleConnection()
        {
            SqliteConnection.ClearAllPools();
        }

        private static async Task CopyFileAsync(string source, string destination)
        {
            using var sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var destStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None);
            await sourceStream.CopyToAsync(destStream);
        }

        private static async Task CopyCompanionFileIfExists(string source, string destination)
        {
            if (File.Exists(source))
            {
                await CopyFileAsync(source, destination);
            }
        }

        private static async Task RestoreOrRemoveCompanionFile(string backupPath, string targetPath)
        {
            if (File.Exists(backupPath))
            {
                await CopyFileAsync(backupPath, targetPath);
            }
            else
            {
                DeleteFileIfExists(targetPath);
            }
        }

        private static void DeleteFileIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
