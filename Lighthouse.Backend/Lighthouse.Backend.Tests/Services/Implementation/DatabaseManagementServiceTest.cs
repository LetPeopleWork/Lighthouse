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

        [SetUp]
        public void SetUp()
        {
            providerMock = new Mock<IDatabaseManagementProvider>();
            providerMock.Setup(p => p.ProviderName).Returns("sqlite");
            providerMock.Setup(p => p.IsToolingAvailable()).Returns(true);
            providerMock.Setup(p => p.RecycleConnection());

            serviceProviderMock = new Mock<IServiceProvider>();

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
                .Returns(Enumerable.Empty<ISeeder>());

            updateStatuses = new ConcurrentDictionary<UpdateKey, UpdateStatus>();
            gate = new DatabaseMaintenanceGate(updateStatuses);
            tracker = new DatabaseOperationTracker();

            var loggerMock = new Mock<ILogger<DatabaseManagementService>>();
            subject = new DatabaseManagementService(
                providerMock.Object,
                gate,
                tracker,
                loggerMock.Object,
                serviceProviderMock.Object);
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

            using var stream = CreateValidBackupStream("password123");
            var status = await subject.RestoreBackup(stream, "password123");

            Assert.That(status.OperationType, Is.EqualTo(DatabaseOperationType.Restore));
        }

        [Test]
        public async Task RestoreBackup_Success_SetsCompletedState()
        {
            providerMock.Setup(p => p.RestoreBackup(It.IsAny<string>())).Returns(Task.CompletedTask);

            using var stream = CreateValidBackupStream("password123");
            var status = await subject.RestoreBackup(stream, "password123");

            Assert.That(status.State, Is.EqualTo(DatabaseOperationState.Completed));
        }

        [Test]
        public async Task RestoreBackup_Success_RecyclesConnectionBeforeMigrating()
        {
            providerMock.Setup(p => p.RestoreBackup(It.IsAny<string>())).Returns(Task.CompletedTask);

            using var stream = CreateValidBackupStream("password123");
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

            using var stream = CreateValidBackupStream("password123");
            await subject.RestoreBackup(stream, "password123");

            Assert.That(gate.IsBlocked, Is.False);
        }

        [Test]
        public async Task RestoreBackup_ReleasesGateAfterFailure()
        {
            providerMock.Setup(p => p.RestoreBackup(It.IsAny<string>())).ThrowsAsync(new Exception("fail"));

            using var stream = CreateValidBackupStream("password123");
            await subject.RestoreBackup(stream, "password123");

            Assert.That(gate.IsBlocked, Is.False);
        }

        [Test]
        public async Task RestoreBackup_Failure_DoesNotRecycleConnection()
        {
            providerMock.Setup(p => p.RestoreBackup(It.IsAny<string>())).ThrowsAsync(new Exception("fail"));

            using var stream = CreateValidBackupStream("password123");
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

        private static MemoryStream CreateValidBackupStream(string password)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"test-backup-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            try
            {
                File.WriteAllText(Path.Combine(tempDir, "test.txt"), "test content");

                var zipPath = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid():N}.zip");
                System.IO.Compression.ZipFile.CreateFromDirectory(tempDir, zipPath);

                var salt = System.Text.Encoding.UTF8.GetBytes("LighthouseDbBackup");
                var key = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, System.Security.Cryptography.HashAlgorithmName.SHA256, 32);

                using var aes = System.Security.Cryptography.Aes.Create();
                aes.Key = key;
                aes.GenerateIV();

                var encryptedPath = zipPath + ".enc";
                using (var outputStream = File.Create(encryptedPath))
                {
                    outputStream.Write(aes.IV);
                    using var cryptoStream = new System.Security.Cryptography.CryptoStream(outputStream, aes.CreateEncryptor(), System.Security.Cryptography.CryptoStreamMode.Write);
                    using var inputStream = File.OpenRead(zipPath);
                    inputStream.CopyTo(cryptoStream);
                }

                var result = new MemoryStream(File.ReadAllBytes(encryptedPath));

                File.Delete(zipPath);
                File.Delete(encryptedPath);

                return result;
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}