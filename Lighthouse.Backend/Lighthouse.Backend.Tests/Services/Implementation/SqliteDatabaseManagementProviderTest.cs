using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Implementation.DatabaseManagement;
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
        public void CreateBackup_ThrowsWhenDatabaseFileNotFound()
        {
            var backupDir = Path.Combine(testDbDirectory, "backup");
            Directory.CreateDirectory(backupDir);

            Assert.ThrowsAsync<FileNotFoundException>(async () =>
                await subject.CreateBackup(backupDir));
        }

        [Test]
        public async Task RestoreBackup_ReplacesDatabaseFile()
        {
            await File.WriteAllBytesAsync(testDbPath, "original"u8.ToArray());

            var restoreDir = Path.Combine(testDbDirectory, "restore");
            Directory.CreateDirectory(restoreDir);
            await File.WriteAllBytesAsync(Path.Combine(restoreDir, "LighthouseAppContext.db"), "restored"u8.ToArray());

            await subject.RestoreBackup(restoreDir);

            var restoredContent = await File.ReadAllBytesAsync(testDbPath);
            Assert.That(restoredContent, Is.EqualTo("restored"u8.ToArray()));
        }

        [Test]
        public async Task RestoreBackup_RestoresWalAndShmFiles_WhenPresent()
        {
            await File.WriteAllBytesAsync(testDbPath, "original"u8.ToArray());

            var restoreDir = Path.Combine(testDbDirectory, "restore");
            Directory.CreateDirectory(restoreDir);
            await File.WriteAllBytesAsync(Path.Combine(restoreDir, "LighthouseAppContext.db"), "newdb"u8.ToArray());
            await File.WriteAllBytesAsync(Path.Combine(restoreDir, "LighthouseAppContext.db-wal"), "newwal"u8.ToArray());
            await File.WriteAllBytesAsync(Path.Combine(restoreDir, "LighthouseAppContext.db-shm"), "newshm"u8.ToArray());

            await subject.RestoreBackup(restoreDir);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await File.ReadAllBytesAsync(testDbPath + "-wal"), Is.EqualTo("newwal"u8.ToArray()));
                Assert.That(await File.ReadAllBytesAsync(testDbPath + "-shm"), Is.EqualTo("newshm"u8.ToArray()));
            }
        }

        [Test]
        public async Task RestoreBackup_RemovesExistingWalAndShm_WhenNotInBackup()
        {
            await File.WriteAllBytesAsync(testDbPath, "original"u8.ToArray());
            await File.WriteAllBytesAsync(testDbPath + "-wal", "old-wal"u8.ToArray());
            await File.WriteAllBytesAsync(testDbPath + "-shm", "old-shm"u8.ToArray());

            var restoreDir = Path.Combine(testDbDirectory, "restore");
            Directory.CreateDirectory(restoreDir);
            await File.WriteAllBytesAsync(Path.Combine(restoreDir, "LighthouseAppContext.db"), "newdb"u8.ToArray());

            await subject.RestoreBackup(restoreDir);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(File.Exists(testDbPath + "-wal"), Is.False);
                Assert.That(File.Exists(testDbPath + "-shm"), Is.False);
            }
        }

        [Test]
        public void RestoreBackup_ThrowsWhenBackupDatabaseFileNotFound()
        {
            var restoreDir = Path.Combine(testDbDirectory, "restore");
            Directory.CreateDirectory(restoreDir);

            Assert.ThrowsAsync<FileNotFoundException>(async () =>
                await subject.RestoreBackup(restoreDir));
        }

        [Test]
        public async Task ClearDatabase_DeletesDatabaseFile()
        {
            await File.WriteAllBytesAsync(testDbPath, "data"u8.ToArray());

            await subject.ClearDatabase();

            Assert.That(File.Exists(testDbPath), Is.False);
        }

        [Test]
        public async Task ClearDatabase_DeletesWalAndShmFiles()
        {
            await File.WriteAllBytesAsync(testDbPath, "data"u8.ToArray());
            await File.WriteAllBytesAsync(testDbPath + "-wal", "wal"u8.ToArray());
            await File.WriteAllBytesAsync(testDbPath + "-shm", "shm"u8.ToArray());

            await subject.ClearDatabase();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(File.Exists(testDbPath), Is.False);
                Assert.That(File.Exists(testDbPath + "-wal"), Is.False);
                Assert.That(File.Exists(testDbPath + "-shm"), Is.False);
            }
        }

        [Test]
        public async Task ClearDatabase_NoOpWhenDatabaseFileDoesNotExist()
        {
            await subject.ClearDatabase();

            Assert.That(File.Exists(testDbPath), Is.False);
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
