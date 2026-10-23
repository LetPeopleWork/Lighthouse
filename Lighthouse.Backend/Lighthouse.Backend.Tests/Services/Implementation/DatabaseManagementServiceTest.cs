using Lighthouse.Backend.Services.Implementation.DatabaseManagement;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DatabaseManagement;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Lighthouse.Backend.Data;
using Microsoft.EntityFrameworkCore;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Time.Testing;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    [TestFixture]
    public class DatabaseManagementServiceTest
    {
        private Mock<IDatabaseManagementProvider> providerMock;
        private Mock<IServiceProvider> serviceProviderMock;
        private DatabaseMaintenanceGate gate;
        private DatabaseOperationTracker tracker;
        private ConcurrentDictionary<UpdateKey, UpdateStatus> updateStatuses;
        private DatabaseManagementService subject;
        private Mock<ISeeder> seederMock;

        private static readonly DateTimeOffset BackupInstant = new(2026, 7, 27, 23, 30, 0, TimeSpan.Zero);

        [SetUp]
        public void SetUp()
        {
            providerMock = new Mock<IDatabaseManagementProvider>();
            providerMock.Setup(p => p.ProviderName).Returns("sqlite");
            providerMock.Setup(p => p.IsToolingAvailable()).Returns(true);
            providerMock.Setup(p => p.RecycleConnection());

            serviceProviderMock = new Mock<IServiceProvider>();
            seederMock = new Mock<ISeeder>();
            seederMock.Setup(s => s.Seed()).Returns(Task.CompletedTask);

            // MigrateAndSeedDatabase creates a scope — wire up IServiceScope + IServiceScopeFactory
            var scopeMock = new Mock<IServiceScope>();
            var scopeFactoryMock = new Mock<IServiceScopeFactory>();
            scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);
            scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);
            serviceProviderMock
                .Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
                .Returns(scopeFactoryMock.Object);


            var optionsBuilder = new DbContextOptionsBuilder<LighthouseAppContext>();
            optionsBuilder.UseSqlite("Data Source=lighthouse.db", options =>
            {
                options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                options.MigrationsAssembly("Lighthouse.Migrations.Sqlite");
            });

            serviceProviderMock
                .Setup(sp => sp.GetService(typeof(LighthouseAppContext)))
                .Returns(new LighthouseAppContext(
                    optionsBuilder.Options, Mock.Of<ICryptoService>(), Mock.Of<ILogger<LighthouseAppContext>>()));

            serviceProviderMock
                .Setup(sp => sp.GetService(typeof(ILogger<Program>)))
                .Returns(Mock.Of<ILogger<Program>>());

            serviceProviderMock
                .Setup(sp => sp.GetService(typeof(IEnumerable<ISeeder>)))
                .Returns(new[] { seederMock.Object });

            updateStatuses = new ConcurrentDictionary<UpdateKey, UpdateStatus>();
            gate = new DatabaseMaintenanceGate(new InProcessUpdateStatusStore(updateStatuses, Clocks.SystemUtc));
            tracker = new DatabaseOperationTracker();

            var loggerMock = new Mock<ILogger<DatabaseManagementService>>();
            subject = new DatabaseManagementService(
                providerMock.Object,
                gate,
                tracker,
                loggerMock.Object,
                serviceProviderMock.Object,
                new FakeTimeProvider(BackupInstant));
        }

        [Test]
        public void GetCapabilityStatus_ReturnsProviderName()
        {
            var status = subject.GetCapabilityStatus();

            Assert.That(status.Provider, Is.EqualTo("sqlite"));
        }

        [Test]
        public void GetCapabilityStatus_NoActiveWork_IsNotBlocked()
        {
            var status = subject.GetCapabilityStatus();

            Assert.That(status.IsOperationBlocked, Is.False);
        }

        [Test]
        public void GetCapabilityStatus_BackgroundWorkActive_IsBlocked()
        {
            var key = new UpdateKey(UpdateType.Team, 1);
            updateStatuses[key] = new UpdateStatus { UpdateType = UpdateType.Team, Id = 1, Status = UpdateProgress.InProgress };

            var status = subject.GetCapabilityStatus();

            Assert.That(status.IsOperationBlocked, Is.True);
        }

        [Test]
        public void GetCapabilityStatus_ToolingAvailable_ReportsAvailable()
        {
            var status = subject.GetCapabilityStatus();

            Assert.That(status.IsToolingAvailable, Is.True);
        }

        [Test]
        public void GetCapabilityStatus_ToolingNotAvailable_ReportsNotAvailable()
        {
            providerMock.Setup(p => p.IsToolingAvailable()).Returns(false);
            providerMock.Setup(p => p.GetToolingGuidanceMessage()).Returns("Install pg_dump");
            providerMock.Setup(p => p.GetToolingGuidanceUrl()).Returns("https://docs.example.com/install");

            var status = subject.GetCapabilityStatus();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(status.IsToolingAvailable, Is.False);
                Assert.That(status.ToolingGuidanceMessage, Is.EqualTo("Install pg_dump"));
                Assert.That(status.ToolingGuidanceUrl, Is.EqualTo("https://docs.example.com/install"));
            }
        }

        [Test]
        public async Task CreateBackup_NoActiveWork_ReturnsCompletedStatus()
        {
            providerMock.Setup(p => p.CreateBackup(It.IsAny<string>())).ReturnsAsync("backup.db");

            var status = await subject.CreateBackup("password123");

            Assert.That(status.State, Is.EqualTo(DatabaseOperationState.Completed));
        }

        [Test]
        public async Task CreateBackup_NoActiveWork_SetsOperationTypeToBackup()
        {
            providerMock.Setup(p => p.CreateBackup(It.IsAny<string>())).ReturnsAsync("backup.db");

            var status = await subject.CreateBackup("password123");

            Assert.That(status.OperationType, Is.EqualTo(DatabaseOperationType.Backup));
        }

        [Test]
        public async Task CreateBackup_BackgroundWorkActive_ReturnsFailedWithBlockedReason()
        {
            var key = new UpdateKey(UpdateType.Team, 1);
            updateStatuses[key] = new UpdateStatus { UpdateType = UpdateType.Team, Id = 1, Status = UpdateProgress.InProgress };

            var status = await subject.CreateBackup("password123");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(status.State, Is.EqualTo(DatabaseOperationState.Failed));
                Assert.That(status.FailureReason, Is.Not.Null.And.Not.Empty);
            }
        }

        [Test]
        public async Task CreateBackup_ProviderThrows_ReturnsFailedStatus()
        {
            providerMock.Setup(p => p.CreateBackup(It.IsAny<string>())).ThrowsAsync(new InvalidOperationException("Disk full"));

            var status = await subject.CreateBackup("password123");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(status.State, Is.EqualTo(DatabaseOperationState.Failed));
                Assert.That(status.FailureReason, Does.Contain("Disk full"));
            }
        }

        [Test]
        public async Task CreateBackup_ReleasesGateAfterCompletion()
        {
            providerMock.Setup(p => p.CreateBackup(It.IsAny<string>())).ReturnsAsync("backup.db");

            await subject.CreateBackup("password123");

            Assert.That(gate.IsBlocked, Is.False);
        }

        [Test]
        public async Task CreateBackup_ReleasesGateAfterFailure()
        {
            providerMock.Setup(p => p.CreateBackup(It.IsAny<string>())).ThrowsAsync(new Exception("fail"));

            await subject.CreateBackup("password123");

            Assert.That(gate.IsBlocked, Is.False);
        }

        [Test]
        public async Task CreateBackup_Success_DoesNotRecycleConnection()
        {
            providerMock.Setup(p => p.CreateBackup(It.IsAny<string>())).ReturnsAsync("backup.db");

            await subject.CreateBackup("password123");

            providerMock.Verify(p => p.RecycleConnection(), Times.Never);
        }

        [Test]
        public async Task RestoreBackup_NoActiveWork_SetsOperationTypeToRestore()
        {
            providerMock.Setup(p => p.RestoreBackup(It.IsAny<string>())).Returns(Task.CompletedTask);

            using var stream = EncryptedBackupStream.Create("password123");
            var status = await subject.RestoreBackup(stream, "password123");

            Assert.That(status.OperationType, Is.EqualTo(DatabaseOperationType.Restore));
        }

        [Test]
        public async Task RestoreBackup_Success_SetsCompletedState()
        {
            providerMock.Setup(p => p.RestoreBackup(It.IsAny<string>())).Returns(Task.CompletedTask);

            using var stream = EncryptedBackupStream.Create("password123");
            var status = await subject.RestoreBackup(stream, "password123");

            Assert.That(status.State, Is.EqualTo(DatabaseOperationState.Completed));
        }

        [Test]
        public async Task RestoreBackup_Success_RecyclesConnectionBeforeMigrating()
        {
            providerMock.Setup(p => p.RestoreBackup(It.IsAny<string>())).Returns(Task.CompletedTask);

            using var stream = EncryptedBackupStream.Create("password123");
            await subject.RestoreBackup(stream, "password123");

            providerMock.Verify(p => p.RecycleConnection(), Times.Once);
        }

        [Test]
        public async Task RestoreBackup_BackgroundWorkActive_ReturnsFailedStatus()
        {
            var key = new UpdateKey(UpdateType.Team, 1);
            updateStatuses[key] = new UpdateStatus { UpdateType = UpdateType.Team, Id = 1, Status = UpdateProgress.InProgress };

            using var stream = new MemoryStream([1, 2, 3]);
            var status = await subject.RestoreBackup(stream, "password123");

            Assert.That(status.State, Is.EqualTo(DatabaseOperationState.Failed));
        }

        [Test]
        public async Task RestoreBackup_ReleasesGateAfterCompletion()
        {
            providerMock.Setup(p => p.RestoreBackup(It.IsAny<string>())).Returns(Task.CompletedTask);

            using var stream = EncryptedBackupStream.Create("password123");
            await subject.RestoreBackup(stream, "password123");

            Assert.That(gate.IsBlocked, Is.False);
        }

        [Test]
        public async Task RestoreBackup_ReleasesGateAfterFailure()
        {
            providerMock.Setup(p => p.RestoreBackup(It.IsAny<string>())).ThrowsAsync(new Exception("fail"));

            using var stream = EncryptedBackupStream.Create("password123");
            await subject.RestoreBackup(stream, "password123");

            Assert.That(gate.IsBlocked, Is.False);
        }

        [Test]
        public async Task RestoreBackup_Failure_DoesNotRecycleConnection()
        {
            providerMock.Setup(p => p.RestoreBackup(It.IsAny<string>())).ThrowsAsync(new Exception("fail"));

            using var stream = EncryptedBackupStream.Create("password123");
            await subject.RestoreBackup(stream, "password123");

            providerMock.Verify(p => p.RecycleConnection(), Times.Never);
        }

        [Test]
        public async Task ClearDatabase_NoActiveWork_SetsOperationTypeToClear()
        {
            providerMock.Setup(p => p.ClearDatabase()).Returns(Task.CompletedTask);

            var status = await subject.ClearDatabase();

            Assert.That(status.OperationType, Is.EqualTo(DatabaseOperationType.Clear));
        }

        [Test]
        public async Task ClearDatabase_Success_SetsCompletedState()
        {
            providerMock.Setup(p => p.ClearDatabase()).Returns(Task.CompletedTask);

            var status = await subject.ClearDatabase();

            Assert.That(status.State, Is.EqualTo(DatabaseOperationState.Completed));
        }

        [Test]
        public async Task ClearDatabase_Success_RecyclesConnectionBeforeMigrating()
        {
            providerMock.Setup(p => p.ClearDatabase()).Returns(Task.CompletedTask);

            await subject.ClearDatabase();

            providerMock.Verify(p => p.RecycleConnection(), Times.Once);
        }

        [Test]
        public async Task ClearDatabase_BackgroundWorkActive_ReturnsFailedStatus()
        {
            var key = new UpdateKey(UpdateType.Team, 1);
            updateStatuses[key] = new UpdateStatus { UpdateType = UpdateType.Team, Id = 1, Status = UpdateProgress.InProgress };

            var status = await subject.ClearDatabase();

            Assert.That(status.State, Is.EqualTo(DatabaseOperationState.Failed));
        }

        [Test]
        public async Task ClearDatabase_ReleasesGateAfterCompletion()
        {
            providerMock.Setup(p => p.ClearDatabase()).Returns(Task.CompletedTask);

            await subject.ClearDatabase();

            Assert.That(gate.IsBlocked, Is.False);
        }

        [Test]
        public async Task ClearDatabase_ReleasesGateAfterFailure()
        {
            providerMock.Setup(p => p.ClearDatabase()).ThrowsAsync(new Exception("fail"));

            await subject.ClearDatabase();

            Assert.That(gate.IsBlocked, Is.False);
        }

        [Test]
        public async Task ClearDatabase_Failure_DoesNotRecycleConnection()
        {
            providerMock.Setup(p => p.ClearDatabase()).ThrowsAsync(new Exception("fail"));

            await subject.ClearDatabase();

            providerMock.Verify(p => p.RecycleConnection(), Times.Never);
        }

        [Test]
        public void GetOperationStatus_ExistingOperation_ReturnsStatus()
        {
            providerMock.Setup(p => p.CreateBackup(It.IsAny<string>())).ReturnsAsync("backup.db");
            var createResult = subject.CreateBackup("password123").GetAwaiter().GetResult();

            var status = subject.GetOperationStatus(createResult.OperationId);

            Assert.That(status, Is.Not.Null);
        }

        [Test]
        public void GetOperationStatus_NonExistent_ReturnsNull()
        {
            var status = subject.GetOperationStatus("nonexistent");

            Assert.That(status, Is.Null);
        }

        private const string Password = "password123";

        private const string WrongPasswordReason = "Failed to decrypt the backup file. The password may be incorrect or the file may be corrupted.";

        private static readonly string[] EveryBackedUpFile = ["lighthouse.db", "nested/wal.log", "manifest.json"];

        private static readonly string[] ManifestKeysWithoutServerVersion = ["provider", "createdAt", "appVersion"];

        private const string NotABackupReason = "Failed to extract the backup archive. The file may be corrupted or not a valid backup.";

        [Test]
        public async Task CreateBackup_ThenRestoringIt_HandsTheProviderBackExactlyWhatItBackedUp()
        {
            providerMock.Setup(p => p.GetServerVersion()).Returns("16.2");

            var restored = await BackUpAndRestore();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(restored.Restore.State, Is.EqualTo(DatabaseOperationState.Completed));
                Assert.That(restored.Files.Keys, Is.EquivalentTo(EveryBackedUpFile));
                Assert.That(restored.Files["lighthouse.db"], Is.EqualTo("database bytes"));
                Assert.That(restored.Files["nested/wal.log"], Is.EqualTo("write-ahead log"));
            }
        }

        [Test]
        public async Task CreateBackup_WritesAManifestNamingTheProviderTheInstantTheVersionAndTheServer()
        {
            providerMock.Setup(p => p.GetServerVersion()).Returns("16.2");

            var restored = await BackUpAndRestore();
            var manifest = JsonSerializer.Deserialize<Dictionary<string, string>>(restored.Files["manifest.json"])!;

            Assert.That(manifest, Is.EquivalentTo(new Dictionary<string, string>
            {
                ["provider"] = "sqlite",
                ["createdAt"] = BackupInstant.UtcDateTime.ToString("O"),
                ["appVersion"] = typeof(DatabaseManagementService).Assembly.GetName().Version!.ToString(),
                ["serverVersion"] = "16.2",
            }));
        }

        [Test]
        public async Task CreateBackup_ProviderWithoutAServerVersion_LeavesItOutOfTheManifest()
        {
            providerMock.Setup(p => p.GetServerVersion()).Returns((string?)null);

            var restored = await BackUpAndRestore();
            var manifest = JsonSerializer.Deserialize<Dictionary<string, string?>>(restored.Files["manifest.json"])!;

            Assert.That(manifest.Keys, Is.EquivalentTo(ManifestKeysWithoutServerVersion));
        }

        [Test]
        public async Task BackupAndRestore_LeaveNoWorkingFilesBehind()
        {
            var restored = await BackUpAndRestore();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Directory.Exists(restored.BackupWorkingDirectory), Is.False, "backup working directory");
                Assert.That(File.Exists(restored.ArtifactPath + ".tmp"), Is.False, "unencrypted intermediate zip");
                Assert.That(Directory.Exists(restored.RestoreWorkingDirectory), Is.False, "restore working directory");
            }
        }

        [Test]
        public async Task BackupAndRestore_ReportThemselvesAsExecutingWhileTheProviderWorks()
        {
            var restored = await BackUpAndRestore();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(restored.StateWhileBackingUp, Is.EqualTo(DatabaseOperationState.Executing));
                Assert.That(restored.StateWhileRestoring, Is.EqualTo(DatabaseOperationState.Executing));
            }
        }

        [Test]
        public async Task ClearDatabase_ReportsItselfAsExecutingWhileTheProviderWorks()
        {
            DatabaseOperationState? stateWhileClearing = null;
            providerMock.Setup(p => p.ClearDatabase())
                .Callback(() => stateWhileClearing = subject.GetCapabilityStatus().ActiveOperation?.State)
                .Returns(Task.CompletedTask);

            await subject.ClearDatabase();

            Assert.That(stateWhileClearing, Is.EqualTo(DatabaseOperationState.Executing));
        }

        [Test]
        public async Task ClearDatabase_Success_SeedsTheFreshDatabase()
        {
            providerMock.Setup(p => p.ClearDatabase()).Returns(Task.CompletedTask);

            await subject.ClearDatabase();

            seederMock.Verify(s => s.Seed(), Times.Once);
        }

        [Test]
        public async Task RestoreBackup_ProviderThrows_ReportsTheFailureAndItsReason()
        {
            providerMock.Setup(p => p.RestoreBackup(It.IsAny<string>())).ThrowsAsync(new InvalidOperationException("Disk full"));

            using var stream = EncryptedBackupStream.Create(Password);
            var status = await subject.RestoreBackup(stream, Password);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(status.State, Is.EqualTo(DatabaseOperationState.Failed));
                Assert.That(status.FailureReason, Is.EqualTo("Disk full"));
            }
        }

        [Test]
        public async Task ClearDatabase_ProviderThrows_ReportsTheFailureAndItsReason()
        {
            providerMock.Setup(p => p.ClearDatabase()).ThrowsAsync(new InvalidOperationException("Disk full"));

            var status = await subject.ClearDatabase();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(status.State, Is.EqualTo(DatabaseOperationState.Failed));
                Assert.That(status.FailureReason, Is.EqualTo("Disk full"));
            }
        }

        [Test]
        public async Task RestoreBackup_WrongPassword_FailsWithoutTouchingTheDatabase()
        {
            using var stream = EncryptedBackupStream.Create(Password);

            var status = await subject.RestoreBackup(stream, "not the password");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(status.State, Is.EqualTo(DatabaseOperationState.Failed));
                // A wrong key usually breaks the padding, but now and then the padding survives and the zip is what fails.
                Assert.That(status.FailureReason, Is.AnyOf(WrongPasswordReason, NotABackupReason));
                providerMock.Verify(p => p.RestoreBackup(It.IsAny<string>()), Times.Never);
            }
        }

        [Test]
        public async Task RestoreBackup_EncryptedFileThatIsNotAZip_FailsAsNotAValidBackup()
        {
            using var stream = new MemoryStream(Encrypt(Encoding.UTF8.GetBytes("this is not a zip archive at all"), Password));

            var status = await subject.RestoreBackup(stream, Password);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(status.State, Is.EqualTo(DatabaseOperationState.Failed));
                Assert.That(status.FailureReason, Is.EqualTo(NotABackupReason));
                providerMock.Verify(p => p.RestoreBackup(It.IsAny<string>()), Times.Never);
            }
        }

        [Test]
        public async Task RestoreBackup_FileTooShortToCarryTheEncryptionHeader_FailsSayingSo()
        {
            using var stream = new MemoryStream([1, 2, 3]);

            var status = await subject.RestoreBackup(stream, Password);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(status.State, Is.EqualTo(DatabaseOperationState.Failed));
                Assert.That(status.FailureReason, Is.EqualTo("Invalid backup file: too short to contain encryption header."));
            }
        }

        [Test]
        public async Task RestoreBackup_ArchiveWithAnEntryEscapingTheTargetDirectory_IsRefused()
        {
            using var stream = new MemoryStream(Encrypt(ZipWithEntry("../escaped.txt"), Password));

            var status = await subject.RestoreBackup(stream, Password);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(status.State, Is.EqualTo(DatabaseOperationState.Failed));
                Assert.That(status.FailureReason, Is.EqualTo("Zip Slip detected: ../escaped.txt"));
                providerMock.Verify(p => p.RestoreBackup(It.IsAny<string>()), Times.Never);
            }
        }

        [Test]
        public void GetBackupArtifact_UnknownOperation_IsNotFound()
        {
            var thrown = Assert.Throws<FileNotFoundException>(() => subject.GetBackupArtifact("no-such-op"));

            Assert.That(thrown.Message, Is.EqualTo("Backup artifact for operation no-such-op not found."));
        }

        [Test]
        public async Task GetBackupArtifact_ArtifactGoneFromDisk_IsNotFound()
        {
            providerMock.Setup(p => p.CreateBackup(It.IsAny<string>())).ReturnsAsync("backup.db");
            var backup = await subject.CreateBackup(Password);
            string artifactPath;
            using (var artifact = (FileStream)subject.GetBackupArtifact(backup.OperationId))
            {
                artifactPath = artifact.Name;
            }

            File.Delete(artifactPath);

            var thrown = Assert.Throws<FileNotFoundException>(() => subject.GetBackupArtifact(backup.OperationId));
            Assert.That(thrown.Message, Is.EqualTo($"Backup artifact for operation {backup.OperationId} not found."));
        }

        private sealed record RoundTrip(
            DatabaseOperationStatus Restore,
            Dictionary<string, string> Files,
            string BackupWorkingDirectory,
            string RestoreWorkingDirectory,
            string ArtifactPath,
            DatabaseOperationState? StateWhileBackingUp,
            DatabaseOperationState? StateWhileRestoring);

        private async Task<RoundTrip> BackUpAndRestore()
        {
            var backupDirectory = string.Empty;
            DatabaseOperationState? stateWhileBackingUp = null;
            providerMock.Setup(p => p.CreateBackup(It.IsAny<string>()))
                .Returns<string>(async directory =>
                {
                    backupDirectory = directory;
                    stateWhileBackingUp = subject.GetCapabilityStatus().ActiveOperation?.State;
                    await File.WriteAllTextAsync(Path.Combine(directory, "lighthouse.db"), "database bytes");
                    Directory.CreateDirectory(Path.Combine(directory, "nested"));
                    await File.WriteAllTextAsync(Path.Combine(directory, "nested", "wal.log"), "write-ahead log");
                    Directory.CreateDirectory(Path.Combine(directory, "empty"));
                    return "lighthouse.db";
                });

            var files = new Dictionary<string, string>();
            var restoreDirectory = string.Empty;
            DatabaseOperationState? stateWhileRestoring = null;
            providerMock.Setup(p => p.RestoreBackup(It.IsAny<string>()))
                .Returns<string>(async directory =>
                {
                    restoreDirectory = Path.GetDirectoryName(directory)!;
                    stateWhileRestoring = subject.GetCapabilityStatus().ActiveOperation?.State;
                    foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
                    {
                        var relative = Path.GetRelativePath(directory, file).Replace(Path.DirectorySeparatorChar, '/');
                        files[relative] = await File.ReadAllTextAsync(file);
                    }
                });

            var backup = await subject.CreateBackup(Password);
            Assert.That(backup.State, Is.EqualTo(DatabaseOperationState.Completed), backup.FailureReason);

            using var copy = new MemoryStream();
            string artifactPath;
            using (var artifact = (FileStream)subject.GetBackupArtifact(backup.OperationId))
            {
                artifactPath = artifact.Name;
                await artifact.CopyToAsync(copy);
            }

            copy.Position = 0;
            var restore = await subject.RestoreBackup(copy, Password);

            return new RoundTrip(restore, files, backupDirectory, restoreDirectory, artifactPath, stateWhileBackingUp, stateWhileRestoring);
        }

        private static byte[] ZipWithEntry(string entryName)
        {
            using var buffer = new MemoryStream();
            using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
            {
                var entry = archive.CreateEntry(entryName);
                using var writer = new StreamWriter(entry.Open());
                writer.Write("payload");
            }

            return buffer.ToArray();
        }

        private static byte[] Encrypt(byte[] content, string password)
        {
            var key = Rfc2898DeriveBytes.Pbkdf2(password, Encoding.UTF8.GetBytes("LighthouseDbBackup"), 100_000, HashAlgorithmName.SHA256, 32);
            using var aes = Aes.Create();
            aes.Key = key;

            using var output = new MemoryStream();
            output.Write(aes.IV);
            using (var crypto = new CryptoStream(output, aes.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen: true))
            {
                crypto.Write(content);
            }

            return output.ToArray();
        }
    }
}