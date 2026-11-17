using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Interfaces.DatabaseManagement;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using System.Data.Common;

namespace Lighthouse.Backend.Services.Implementation.DatabaseManagement
{
    public class SqliteDatabaseManagementProvider : IDatabaseManagementProvider
    {
        // The same wait every application connection gets, so writing a restore waits for a busy writer as long as the app does.
        private const string WaitForABusyWriter = "PRAGMA busy_timeout=10000;";

        private readonly string databasePath;
        private readonly string liveConnectionString;
        private readonly ILogger<SqliteDatabaseManagementProvider> logger;

        public SqliteDatabaseManagementProvider(
            IOptions<DatabaseConfiguration> dbConfig,
            ILogger<SqliteDatabaseManagementProvider> logger)
        {
            var connectionStringBuilder = new SqliteConnectionStringBuilder(dbConfig.Value.ConnectionString);
            databasePath = connectionStringBuilder.DataSource;
            liveConnectionString = dbConfig.Value.ConnectionString;
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

        public Task<IStagedBackup> StageBackup(string backupContentPath)
        {
            return Task.FromResult<IStagedBackup>(new ExtractedDatabaseFile(DatabaseFileIn(backupContentPath)));
        }

        public Task RestoreBackup(string backupContentPath)
        {
            var backupDbFile = DatabaseFileIn(backupContentPath);

            logger.LogInformation("Restoring SQLite database from {Source} to {Destination}", backupDbFile, databasePath);

            // Opening the staged copy applies a -wal file that was backed up beside it, so its newest rows come along.
            using var backup = OpenUnpooled(new SqliteConnectionStringBuilder { DataSource = backupDbFile }.ToString());
            CopyPagesIntoLiveDatabase(backup);

            return Task.CompletedTask;
        }

        public Task ClearDatabase()
        {
            logger.LogInformation("Clearing SQLite database at {Path}", databasePath);

            using var emptyDatabase = OpenUnpooled("Data Source=:memory:");
            CopyPagesIntoLiveDatabase(emptyDatabase);

            return Task.CompletedTask;
        }

        public void RecycleConnection()
        {
            SqliteConnection.ClearAllPools();
        }

        // A running application holds the live file open, and Windows and macOS refuse to replace or delete an open
        // file. Copying pages through SQLite's online backup writes into the open database instead: every connection
        // already open on it reads the new content, and the live file keeps its write-ahead-log mode.
        private void CopyPagesIntoLiveDatabase(SqliteConnection source)
        {
            var directory = Path.GetDirectoryName(databasePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var live = OpenUnpooled(liveConnectionString);
            using (var command = live.CreateCommand())
            {
                command.CommandText = WaitForABusyWriter;
                command.ExecuteNonQuery();
            }

            source.BackupDatabase(live);
        }

        private static string DatabaseFileIn(string backupContentPath)
            => Directory.GetFiles(backupContentPath, "*.db").FirstOrDefault() ?? throw new FileNotFoundException("Backup does not contain a .db file");

        private static SqliteConnection OpenUnpooled(string connectionString)
        {
            var connection = new SqliteConnection(new SqliteConnectionStringBuilder(connectionString) { Pooling = false }.ToString());
            connection.Open();
            return connection;
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

        // The extracted file already sits in a temporary directory the caller deletes, so disposing removes
        // nothing. Unpooled, so no pooled handle keeps the file open and stops that directory from being deleted.
        private sealed class ExtractedDatabaseFile(string databaseFile) : IStagedBackup
        {
            public DbConnection OpenConnection()
                => new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = databaseFile, Pooling = false }.ToString());

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
