using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Implementation.DatabaseManagement;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    [TestFixture]
    public class SqliteDatabaseManagementProviderTest
    {
        private Mock<IOptions<DatabaseConfiguration>> optionsMock;
        private string testDbDirectory;
        
        private string testDbPath;

        private static readonly string[] OnlyTheBackupsPeriod = ["Backup period"];

        private static readonly string[] TheBackupsTwoPeriods = ["Backup period", "Newest backup period"];

        private readonly string testDbFileName = "TestLighthouse.db";

        private SqliteDatabaseManagementProvider subject;

        [SetUp]
        public void SetUp()
        {
            testDbDirectory = Path.Combine(Path.GetTempPath(), $"lighthouse-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(testDbDirectory);
            testDbPath = Path.Combine(testDbDirectory, testDbFileName);

            optionsMock = new Mock<IOptions<DatabaseConfiguration>>();
            optionsMock.Setup(o => o.Value).Returns(new DatabaseConfiguration
            {
                Provider = "Sqlite",
                ConnectionString = $"Data Source={testDbPath}",
            });

            var loggerMock = new Mock<ILogger<SqliteDatabaseManagementProvider>>();
            subject = new SqliteDatabaseManagementProvider(optionsMock.Object, loggerMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(testDbDirectory))
            {
                Directory.Delete(testDbDirectory, recursive: true);
            }
        }

        [Test]
        public void ProviderName_ReturnsLowercaseSqlite()
        {
            Assert.That(subject.ProviderName, Is.EqualTo("sqlite"));
        }

        [Test]
        public void IsToolingAvailable_ReturnsTrue()
        {
            Assert.That(subject.IsToolingAvailable(), Is.True);
        }

        [Test]
        public void GetToolingGuidanceMessage_ReturnsNull()
        {
            Assert.That(subject.GetToolingGuidanceMessage(), Is.Null);
        }

        [Test]
        public void GetToolingGuidanceUrl_ReturnsNull()
        {
            Assert.That(subject.GetToolingGuidanceUrl(), Is.Null);
        }

        [Test]
        public void GetServerVersion_ReturnsNull()
        {
            Assert.That(subject.GetServerVersion(), Is.Null);
        }

        [Test]
        public async Task CreateBackup_CopiesDatabaseFileToDestination()
        {
            var dbContent = "test database content"u8.ToArray();
            await File.WriteAllBytesAsync(testDbPath, dbContent);

            var backupDir = Path.Combine(testDbDirectory, "backup");
            Directory.CreateDirectory(backupDir);

            _ = await subject.CreateBackup(backupDir);

            var backedUpFile = Path.Combine(backupDir, testDbFileName);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(File.Exists(backedUpFile), Is.True);
                Assert.That(await File.ReadAllBytesAsync(backedUpFile), Is.EqualTo(dbContent));
            }
        }

        [Test]
        public async Task CreateBackup_ReturnsCopiedFilePath()
        {
            await File.WriteAllBytesAsync(testDbPath, "data"u8.ToArray());

            var backupDir = Path.Combine(testDbDirectory, "backup");
            Directory.CreateDirectory(backupDir);

            var result = await subject.CreateBackup(backupDir);

            Assert.That(result, Is.EqualTo(Path.Combine(backupDir, testDbFileName)));
        }

        [Test]
        public async Task CreateBackup_AlsoCopiesWalFile_WhenExists()
        {
            await File.WriteAllBytesAsync(testDbPath, "db"u8.ToArray());
            await File.WriteAllBytesAsync(testDbPath + "-wal", "wal-data"u8.ToArray());

            var backupDir = Path.Combine(testDbDirectory, "backup");
            Directory.CreateDirectory(backupDir);

            await subject.CreateBackup(backupDir);

            var walBackup = Path.Combine(backupDir, testDbFileName + "-wal");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(File.Exists(walBackup), Is.True);
                Assert.That(await File.ReadAllBytesAsync(walBackup), Is.EqualTo("wal-data"u8.ToArray()));
            }
        }

        [Test]
        public async Task CreateBackup_AlsoCopiesShmFile_WhenExists()
        {
            await File.WriteAllBytesAsync(testDbPath, "db"u8.ToArray());
            await File.WriteAllBytesAsync(testDbPath + "-shm", "shm-data"u8.ToArray());

            var backupDir = Path.Combine(testDbDirectory, "backup");
            Directory.CreateDirectory(backupDir);

            await subject.CreateBackup(backupDir);

            var shmBackup = Path.Combine(backupDir, testDbFileName + "-shm");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(File.Exists(shmBackup), Is.True);
                Assert.That(await File.ReadAllBytesAsync(shmBackup), Is.EqualTo("shm-data"u8.ToArray()));
            }
        }

        [Test]
        public async Task CreateBackup_ThrowsWhenDatabaseFileNotFound()
        {
            var backupDir = Path.Combine(testDbDirectory, "backup");
            Directory.CreateDirectory(backupDir);

            await Assert.ThrowsAsync<FileNotFoundException>(async () =>
                await subject.CreateBackup(backupDir));
        }

        [Test]
        public async Task RestoreBackup_LeavesTheLiveDatabaseHoldingTheBackupsRows()
        {
            WriteDatabase(testDbPath, "Live period");
            var restoreDir = BackupDirectoryHolding("Backup period");

            await subject.RestoreBackup(restoreDir);

            Assert.That(PeriodsIn(testDbPath), Is.EqualTo(OnlyTheBackupsPeriod),
                "After a restore the live database must hold exactly the backup's rows, and none of the rows it held before.");
        }

        [Test]
        public async Task RestoreBackup_IncludesRowsThatAreOnlyInTheBackupsWriteAheadLog()
        {
            WriteDatabase(testDbPath, "Live period");
            var restoreDir = LegacyBackupDirectoryWithItsNewestRowInTheLog("Backup period", "Newest backup period");

            await subject.RestoreBackup(restoreDir);

            Assert.That(PeriodsIn(testDbPath), Is.EqualTo(TheBackupsTwoPeriods),
                "A backup made while the database was in write-ahead-log mode keeps its newest rows in its -wal file. " +
                "The restore must apply that log, or the newest data of the backup is lost.");
        }

        [Test]
        public async Task RestoreBackup_DropsRowsTheLiveWriteAheadLogHeldThatTheBackupDoesNot()
        {
            using (var live = OpenInWriteAheadLogMode(testDbPath))
            {
                Execute(live, "CREATE TABLE Periods (Description TEXT NOT NULL); INSERT INTO Periods VALUES ('Live period in the log');");
                var restoreDir = BackupDirectoryHolding("Backup period");

                await subject.RestoreBackup(restoreDir);
            }

            Assert.That(PeriodsIn(testDbPath), Is.EqualTo(OnlyTheBackupsPeriod),
                "Rows the live database still held in its write-ahead log must not survive a restore of a backup that does not hold them.");
        }

        [Test]
        public async Task RestoreBackup_ThrowsWhenBackupDatabaseFileNotFound()
        {
            var restoreDir = Path.Combine(testDbDirectory, "restore");
            Directory.CreateDirectory(restoreDir);

            await Assert.ThrowsAsync<FileNotFoundException>(async () =>
                await subject.RestoreBackup(restoreDir));
        }

        [Test]
        public async Task ClearDatabase_LeavesAnEmptyDatabaseWithNoTables()
        {
            WriteDatabase(testDbPath, "Live period");

            await subject.ClearDatabase();

            Assert.That(TableCountIn(testDbPath), Is.Zero,
                "Clear Database must leave an empty database, so migrations build the schema afresh and seeding starts from nothing.");
        }

        [Test]
        public async Task ClearDatabase_OnAnInstallationWithNoDatabaseFileYet_LeavesAnEmptyDatabase()
        {
            Directory.Delete(testDbDirectory, recursive: true);

            await subject.ClearDatabase();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(File.Exists(testDbPath), Is.True,
                    "Clear Database on an installation that has no database file yet must create one, instead of failing because the file or its folder is missing.");
                Assert.That(TableCountIn(testDbPath), Is.Zero,
                    "Clear Database on an installation that has no database file yet must leave an empty database, so migrations build the schema afresh.");
            }
        }

        [Test]
        public async Task ClearDatabase_AlsoEmptiesRowsTheLiveWriteAheadLogHeld()
        {
            using (var live = OpenInWriteAheadLogMode(testDbPath))
            {
                Execute(live, "CREATE TABLE Periods (Description TEXT NOT NULL); INSERT INTO Periods VALUES ('Live period in the log');");

                await subject.ClearDatabase();
            }

            Assert.That(TableCountIn(testDbPath), Is.Zero,
                "Rows the live database still held in its write-ahead log must not survive Clear Database.");
        }

        private string BackupDirectoryHolding(params string[] periods)
        {
            var restoreDir = Path.Combine(testDbDirectory, "restore");
            Directory.CreateDirectory(restoreDir);
            WriteDatabase(Path.Combine(restoreDir, "LighthouseAppContext.db"), periods);
            return restoreDir;
        }

        // Copies a database and its -wal file while a connection still holds it open, the way a backup taken
        // from the running application did: the newest row is then in the copied log, not in the copied file.
        private string LegacyBackupDirectoryWithItsNewestRowInTheLog(string checkpointedPeriod, string periodOnlyInTheLog)
        {
            var sourceDir = Path.Combine(testDbDirectory, "running");
            Directory.CreateDirectory(sourceDir);
            var source = Path.Combine(sourceDir, "LighthouseAppContext.db");

            var restoreDir = Path.Combine(testDbDirectory, "restore");
            Directory.CreateDirectory(restoreDir);
            var backedUp = Path.Combine(restoreDir, "LighthouseAppContext.db");

            using (var running = OpenInWriteAheadLogMode(source))
            {
                Execute(running, "CREATE TABLE Periods (Description TEXT NOT NULL);");
                Insert(running, checkpointedPeriod);
                Execute(running, "PRAGMA wal_checkpoint(TRUNCATE);");
                Insert(running, periodOnlyInTheLog);

                File.Copy(source, backedUp);
                File.Copy(source + "-wal", backedUp + "-wal");
            }

            Assert.That(PeriodsInTheFileAlone(backedUp), Is.EqualTo(new[] { checkpointedPeriod }),
                "Set-up: the copied backup file was meant to lack its newest row, which only its -wal file holds.");

            return restoreDir;
        }

        private static void WriteDatabase(string path, params string[] periods)
        {
            using var connection = Open(path);
            Execute(connection, "CREATE TABLE Periods (Description TEXT NOT NULL);");
            foreach (var period in periods)
            {
                Insert(connection, period);
            }
        }

        private static SqliteConnection OpenInWriteAheadLogMode(string path)
        {
            var connection = Open(path);
            Execute(connection, "PRAGMA journal_mode=WAL; PRAGMA wal_autocheckpoint=0;");
            return connection;
        }

        private static List<string> PeriodsIn(string path)
        {
            using var connection = Open(path);
            return ReadPeriods(connection);
        }

        // Reads a copy of the main file with no -wal beside it, so only what the file itself holds is seen.
        private List<string> PeriodsInTheFileAlone(string path)
        {
            var fileAlone = Path.Combine(testDbDirectory, "file-alone.db");
            File.Copy(path, fileAlone);
            using var connection = Open(fileAlone);
            return ReadPeriods(connection);
        }

        private static long TableCountIn(string path)
        {
            using var connection = Open(path);
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table';";
            return (long)command.ExecuteScalar()!;
        }

        private static List<string> ReadPeriods(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Description FROM Periods ORDER BY Description;";
            using var reader = command.ExecuteReader();

            var periods = new List<string>();
            while (reader.Read())
            {
                periods.Add(reader.GetString(0));
            }

            return periods;
        }

        private static SqliteConnection Open(string path)
        {
            var connection = new SqliteConnection($"Data Source={path};Pooling=False");
            connection.Open();
            return connection;
        }

        private static void Insert(SqliteConnection connection, string period)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO Periods VALUES ($period);";
            command.Parameters.AddWithValue("$period", period);
            command.ExecuteNonQuery();
        }

        private static void Execute(SqliteConnection connection, string statements)
        {
            using var command = connection.CreateCommand();
            command.CommandText = statements;
            command.ExecuteNonQuery();
        }

        [Test]
        public async Task CreateBackup_DoesNotCopyWalOrShm_WhenNotPresent()
        {
            await File.WriteAllBytesAsync(testDbPath, "db-content"u8.ToArray());

            var backupDir = Path.Combine(testDbDirectory, "backup");
            Directory.CreateDirectory(backupDir);

            await subject.CreateBackup(backupDir);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(File.Exists(Path.Combine(backupDir, "LighthouseAppContext.db-wal")), Is.False);
                Assert.That(File.Exists(Path.Combine(backupDir, "LighthouseAppContext.db-shm")), Is.False);
            }
        }
    }
}
