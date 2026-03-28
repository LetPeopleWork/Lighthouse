using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Implementation.DatabaseManagement;
using Lighthouse.Backend.Services.Interfaces.DatabaseManagement;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Diagnostics;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    [TestFixture]
    public class PostgresDatabaseManagementProviderTest
    {
        private Mock<IOptions<DatabaseConfiguration>> optionsMock;
        private Mock<ICommandRunner> commandRunnerMock;
        private Mock<ILogger<PostgresDatabaseManagementProvider>> loggerMock;
        private PostgresDatabaseManagementProvider subject;

        private const string TestConnectionString = "Host=localhost;Port=5432;Database=lighthouse;Username=admin;Password=secret";

        [SetUp]
        public void SetUp()
        {
            optionsMock = new Mock<IOptions<DatabaseConfiguration>>();
            optionsMock.Setup(o => o.Value).Returns(new DatabaseConfiguration
            {
                Provider = "PostgreSQL",
                ConnectionString = TestConnectionString,
            });

            commandRunnerMock = new Mock<ICommandRunner>();
            loggerMock = new Mock<ILogger<PostgresDatabaseManagementProvider>>();

            subject = new PostgresDatabaseManagementProvider(optionsMock.Object, commandRunnerMock.Object, loggerMock.Object);
        }

        [Test]
        public void ProviderName_ReturnsLowercasePostgresql()
        {
            Assert.That(subject.ProviderName, Is.EqualTo("postgresql"));
        }

        [Test]
        public void IsToolingAvailable_BothToolsAvailable_ReturnsTrue()
        {
            commandRunnerMock.Setup(c => c.IsToolAvailable("pg_dump")).Returns(true);
            commandRunnerMock.Setup(c => c.IsToolAvailable("pg_restore")).Returns(true);
            commandRunnerMock.Setup(c => c.IsToolAvailable("psql")).Returns(true);

            Assert.That(subject.IsToolingAvailable(), Is.True);
        }

        [Test]
        public void IsToolingAvailable_PsqlMissing_ReturnsFalse()
        {
            commandRunnerMock.Setup(c => c.IsToolAvailable("pg_dump")).Returns(true);
            commandRunnerMock.Setup(c => c.IsToolAvailable("pg_restore")).Returns(true);
            commandRunnerMock.Setup(c => c.IsToolAvailable("psql")).Returns(false);

            Assert.That(subject.IsToolingAvailable(), Is.False);
        }

        [Test]
        public void IsToolingAvailable_PgDumpMissing_ReturnsFalse()
        {
            commandRunnerMock.Setup(c => c.IsToolAvailable("pg_dump")).Returns(false);
            commandRunnerMock.Setup(c => c.IsToolAvailable("pg_restore")).Returns(true);

            Assert.That(subject.IsToolingAvailable(), Is.False);
        }

        [Test]
        public void IsToolingAvailable_PgRestoreMissing_ReturnsFalse()
        {
            commandRunnerMock.Setup(c => c.IsToolAvailable("pg_dump")).Returns(true);
            commandRunnerMock.Setup(c => c.IsToolAvailable("pg_restore")).Returns(false);

            Assert.That(subject.IsToolingAvailable(), Is.False);
        }

        [Test]
        public void GetToolingGuidanceMessage_ToolsMissing_ReturnsGuidance()
        {
            commandRunnerMock.Setup(c => c.IsToolAvailable("pg_dump")).Returns(false);
            commandRunnerMock.Setup(c => c.IsToolAvailable("pg_restore")).Returns(false);

            Assert.That(subject.GetToolingGuidanceMessage(), Does.Contain("pg_dump").And.Contain("pg_restore"));
        }

        [Test]
        public void GetToolingGuidanceMessage_ToolsAvailable_ReturnsNull()
        {
            commandRunnerMock.Setup(c => c.IsToolAvailable("pg_dump")).Returns(true);
            commandRunnerMock.Setup(c => c.IsToolAvailable("pg_restore")).Returns(true);
            commandRunnerMock.Setup(c => c.IsToolAvailable("psql")).Returns(true);

            Assert.That(subject.GetToolingGuidanceMessage(), Is.Null);
        }

        [Test]
        public void GetToolingGuidanceUrl_ToolsAvailable_ReturnsNull()
        {
            commandRunnerMock.Setup(c => c.IsToolAvailable("pg_dump")).Returns(true);
            commandRunnerMock.Setup(c => c.IsToolAvailable("pg_restore")).Returns(true);
            commandRunnerMock.Setup(c => c.IsToolAvailable("psql")).Returns(true);

            Assert.That(subject.GetToolingGuidanceUrl(), Is.Null);
        }

        [Test]
        public void GetToolingGuidanceUrl_ToolsMissing_ReturnsPostgresDownloadUrl()
        {
            commandRunnerMock.Setup(c => c.IsToolAvailable("pg_dump")).Returns(false);

            Assert.That(subject.GetToolingGuidanceUrl(), Does.Contain("postgresql.org"));
        }

        [Test]
        public void GetServerVersion_ReturnsVersion_WhenPgDumpAvailable()
        {
            commandRunnerMock.Setup(c => c.IsToolAvailable("pg_dump")).Returns(true);
            commandRunnerMock.Setup(c => c.RunAsync(It.Is<ProcessStartInfo>(p => p.FileName == "pg_dump" && p.Arguments == "--version"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CommandResult(0, "pg_dump (PostgreSQL) 16.2", ""));

            var version = subject.GetServerVersion();

            Assert.That(version, Is.EqualTo("pg_dump (PostgreSQL) 16.2"));
        }

        [Test]
        public void GetServerVersion_ReturnsNull_WhenPgDumpNotAvailable()
        {
            commandRunnerMock.Setup(c => c.IsToolAvailable("pg_dump")).Returns(false);

            Assert.That(subject.GetServerVersion(), Is.Null);
        }

        [Test]
        public async Task CreateBackup_RunsPgDumpWithCorrectArguments()
        {
            commandRunnerMock.Setup(c => c.RunAsync(It.IsAny<ProcessStartInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CommandResult(0, "", ""));

            var backupDir = Path.Combine(Path.GetTempPath(), $"test-pg-backup-{Guid.NewGuid():N}");
            Directory.CreateDirectory(backupDir);

            try
            {
                await subject.CreateBackup(backupDir);

                commandRunnerMock.Verify(c => c.RunAsync(
                    It.Is<ProcessStartInfo>(p =>
                        p.FileName == "pg_dump" &&
                        p.Arguments.Contains("--format=custom") &&
                        p.Arguments.Contains($"--file=") &&
                        p.Arguments.Contains("lighthouse.pgdump")),
                    It.IsAny<CancellationToken>()), Times.Once);
            }
            finally
            {
                if (Directory.Exists(backupDir))
                    Directory.Delete(backupDir, true);
            }
        }

        [Test]
        public async Task CreateBackup_SetsPasswordViaEnvironmentVariable()
        {
            commandRunnerMock.Setup(c => c.RunAsync(It.IsAny<ProcessStartInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CommandResult(0, "", ""));

            var backupDir = Path.Combine(Path.GetTempPath(), $"test-pg-backup-{Guid.NewGuid():N}");
            Directory.CreateDirectory(backupDir);

            try
            {
                await subject.CreateBackup(backupDir);

                commandRunnerMock.Verify(c => c.RunAsync(
                    It.Is<ProcessStartInfo>(p =>
                        p.Environment.ContainsKey("PGPASSWORD") &&
                        p.Environment["PGPASSWORD"] == "secret"),
                    It.IsAny<CancellationToken>()), Times.Once);
            }
            finally
            {
                if (Directory.Exists(backupDir))
                    Directory.Delete(backupDir, true);
            }
        }

        [Test]
        public async Task CreateBackup_ReturnsFilePath()
        {
            commandRunnerMock.Setup(c => c.RunAsync(It.IsAny<ProcessStartInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CommandResult(0, "", ""));

            var backupDir = Path.Combine(Path.GetTempPath(), $"test-pg-backup-{Guid.NewGuid():N}");
            Directory.CreateDirectory(backupDir);

            try
            {
                var result = await subject.CreateBackup(backupDir);

                Assert.That(result, Is.EqualTo(Path.Combine(backupDir, "lighthouse.pgdump")));
            }
            finally
            {
                if (Directory.Exists(backupDir))
                    Directory.Delete(backupDir, true);
            }
        }

        [Test]
        public void CreateBackup_PgDumpFails_ThrowsWithStderr()
        {
            commandRunnerMock.Setup(c => c.RunAsync(It.IsAny<ProcessStartInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CommandResult(1, "", "pg_dump: connection to server failed"));

            var backupDir = Path.Combine(Path.GetTempPath(), $"test-pg-backup-{Guid.NewGuid():N}");
            Directory.CreateDirectory(backupDir);

            try
            {
                var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await subject.CreateBackup(backupDir));

                Assert.That(ex!.Message, Does.Contain("pg_dump failed"));
            }
            finally
            {
                if (Directory.Exists(backupDir))
                    Directory.Delete(backupDir, true);
            }
        }

        [Test]
        public async Task RestoreBackup_RunsPgRestoreWithCorrectArguments()
        {
            var restoreDir = Path.Combine(Path.GetTempPath(), $"test-pg-restore-{Guid.NewGuid():N}");
            Directory.CreateDirectory(restoreDir);
            await File.WriteAllBytesAsync(Path.Combine(restoreDir, "lighthouse.pgdump"), "data"u8.ToArray());

            commandRunnerMock.Setup(c => c.RunAsync(It.IsAny<ProcessStartInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CommandResult(0, "", ""));

            try
            {
                await subject.RestoreBackup(restoreDir);

                // Verify the psql drop/create happens first (2 calls)
                commandRunnerMock.Verify(c => c.RunAsync(
                    It.Is<ProcessStartInfo>(p => p.FileName == "psql"),
                    It.IsAny<CancellationToken>()),
                    Times.Exactly(2));

                // Verify pg_restore arguments (removed --clean and --if-exists)
                commandRunnerMock.Verify(c => c.RunAsync(
                    It.Is<ProcessStartInfo>(p =>
                        p.FileName == "pg_restore" &&
                        p.Arguments.Contains("--host=") &&
                        p.Arguments.Contains("--username=") &&
                        p.Arguments.Contains("--dbname=lighthouse")),
                    It.IsAny<CancellationToken>()), Times.Once);
            }
            finally
            {
                if (Directory.Exists(restoreDir))
                    Directory.Delete(restoreDir, true);
            }
        }

        [Test]
        public async Task RestoreBackup_SetsPasswordViaEnvironmentVariable()
        {
            var restoreDir = Path.Combine(Path.GetTempPath(), $"test-pg-restore-{Guid.NewGuid():N}");
            Directory.CreateDirectory(restoreDir);
            await File.WriteAllBytesAsync(Path.Combine(restoreDir, "lighthouse.pgdump"), "data"u8.ToArray());

            commandRunnerMock.Setup(c => c.RunAsync(It.IsAny<ProcessStartInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CommandResult(0, "", ""));

            try
            {
                await subject.RestoreBackup(restoreDir);

                // Verify PGPASSWORD is set for the 2 psql calls and 1 pg_restore call
                commandRunnerMock.Verify(c => c.RunAsync(
                    It.Is<ProcessStartInfo>(p =>
                        p.Environment.ContainsKey("PGPASSWORD") &&
                        p.Environment["PGPASSWORD"] == "secret"),
                    It.IsAny<CancellationToken>()), Times.Exactly(3));
            }
            finally
            {
                if (Directory.Exists(restoreDir))
                    Directory.Delete(restoreDir, true);
            }
        }

        [Test]
        public void RestoreBackup_PgDumpFileMissing_ThrowsFileNotFound()
        {
            var restoreDir = Path.Combine(Path.GetTempPath(), $"test-pg-restore-{Guid.NewGuid():N}");
            Directory.CreateDirectory(restoreDir);

            try
            {
                Assert.ThrowsAsync<FileNotFoundException>(async () =>
                    await subject.RestoreBackup(restoreDir));
            }
            finally
            {
                if (Directory.Exists(restoreDir))
                    Directory.Delete(restoreDir, true);
            }
        }

        [Test]
        public async Task RestoreBackup_PgRestoreFails_ThrowsWithStderr()
        {
            var restoreDir = Path.Combine(Path.GetTempPath(), $"test-pg-restore-{Guid.NewGuid():N}");
            Directory.CreateDirectory(restoreDir);
            await File.WriteAllBytesAsync(Path.Combine(restoreDir, "lighthouse.pgdump"), "data"u8.ToArray());

            // Setup: psql calls succeed (0), but pg_restore fails (1)
            commandRunnerMock.Setup(c => c.RunAsync(It.Is<ProcessStartInfo>(p => p.FileName == "psql"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CommandResult(0, "", ""));

            commandRunnerMock.Setup(c => c.RunAsync(It.Is<ProcessStartInfo>(p => p.FileName == "pg_restore"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CommandResult(1, "", "pg_restore: error: could not connect"));

            try
            {
                var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await subject.RestoreBackup(restoreDir));

                Assert.That(ex!.Message, Does.Contain("pg_restore failed"));
            }
            finally
            {
                if (Directory.Exists(restoreDir))
                    Directory.Delete(restoreDir, true);
            }
        }

        [Test]
        public async Task ClearDatabase_RunsPsqlDropAndRecreate()
        {
            commandRunnerMock.Setup(c => c.RunAsync(It.IsAny<ProcessStartInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CommandResult(0, "", ""));

            await subject.ClearDatabase();

            // Should connect to 'postgres' maintenance database and drop/recreate the target database
            commandRunnerMock.Verify(c => c.RunAsync(
                It.Is<ProcessStartInfo>(p =>
                    p.FileName == "psql" &&
                    p.Arguments.Contains("postgres") &&
                    p.Arguments.Contains("-c")),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Test]
        public async Task ClearDatabase_SetsPasswordViaEnvironmentVariable()
        {
            commandRunnerMock.Setup(c => c.RunAsync(It.IsAny<ProcessStartInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CommandResult(0, "", ""));

            await subject.ClearDatabase();

            commandRunnerMock.Verify(c => c.RunAsync(
                It.Is<ProcessStartInfo>(p =>
                    p.Environment.ContainsKey("PGPASSWORD") &&
                    p.Environment["PGPASSWORD"] == "secret"),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Test]
        public void ClearDatabase_DropFails_ThrowsWithStderr()
        {
            commandRunnerMock.Setup(c => c.RunAsync(It.IsAny<ProcessStartInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CommandResult(1, "", "ERROR: database is being accessed by other users"));

            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await subject.ClearDatabase());

            Assert.That(ex!.Message, Does.Contain("Failed to drop"));
        }

        [Test]
        public async Task CreateBackup_PassesHostAndPortFromConnectionString()
        {
            commandRunnerMock.Setup(c => c.RunAsync(It.IsAny<ProcessStartInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CommandResult(0, "", ""));

            var backupDir = Path.Combine(Path.GetTempPath(), $"test-pg-backup-{Guid.NewGuid():N}");
            Directory.CreateDirectory(backupDir);

            try
            {
                await subject.CreateBackup(backupDir);

                commandRunnerMock.Verify(c => c.RunAsync(
                    It.Is<ProcessStartInfo>(p =>
                        p.Arguments.Contains("--host=localhost") &&
                        p.Arguments.Contains("--port=5432") &&
                        p.Arguments.Contains("--username=admin")),
                    It.IsAny<CancellationToken>()), Times.Once);
            }
            finally
            {
                if (Directory.Exists(backupDir))
                    Directory.Delete(backupDir, true);
            }
        }

        [Test]
        public async Task CreateBackup_DoesNotLogPassword()
        {
            commandRunnerMock.Setup(c => c.RunAsync(It.IsAny<ProcessStartInfo>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CommandResult(0, "", ""));

            var backupDir = Path.Combine(Path.GetTempPath(), $"test-pg-backup-{Guid.NewGuid():N}");
            Directory.CreateDirectory(backupDir);

            try
            {
                await subject.CreateBackup(backupDir);

                // Verify arguments do not contain the password
                commandRunnerMock.Verify(c => c.RunAsync(
                    It.Is<ProcessStartInfo>(p =>
                        !p.Arguments.Contains("secret")),
                    It.IsAny<CancellationToken>()), Times.Once);
            }
            finally
            {
                if (Directory.Exists(backupDir))
                    Directory.Delete(backupDir, true);
            }
        }
    }
}
