using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Lighthouse.Backend.Tests.Services
{
    public class SystemInfoServiceTest
    {
        private Mock<IConfiguration> configurationMock;
        private Mock<ILogConfiguration> logConfigurationMock;

        [SetUp]
        public void Setup()
        {
            configurationMock = new Mock<IConfiguration>();
            logConfigurationMock = new Mock<ILogConfiguration>();

            // Default: GetSection returns an empty section so GetValue<T> falls back to defaultValue
            var emptySection = new Mock<IConfigurationSection>();
            emptySection.Setup(s => s.Value).Returns((string?)null);
            configurationMock.Setup(c => c.GetSection(It.IsAny<string>())).Returns(emptySection.Object);
        }

        [Test]
        public void GetSystemInfo_ReturnsCorrectDatabaseProvider()
        {
            var expectedProvider = "sqlite";
            SetupDatabaseProvider(expectedProvider);

            var subject = CreateSubject();

            var result = subject.GetSystemInfo();

            Assert.That(result.DatabaseProvider, Is.EqualTo(expectedProvider));
        }

        [Test]
        public void GetSystemInfo_DatabaseProviderNotConfigured_ReturnsUnknown()
        {
            configurationMock.Setup(c => c.GetSection("Database:Provider")).Returns(new Mock<IConfigurationSection>().Object);

            var subject = CreateSubject();

            var result = subject.GetSystemInfo();

            Assert.That(result.DatabaseProvider, Is.EqualTo("Unknown"));
        }

        [Test]
        public void GetSystemInfo_ReturnsLogPathFromLogConfiguration()
        {
            var expectedLogPath = "/var/log/lighthouse";
            logConfigurationMock.Setup(x => x.LogPath).Returns(expectedLogPath);

            var subject = CreateSubject();

            var result = subject.GetSystemInfo();

            Assert.That(result.LogPath, Is.EqualTo(expectedLogPath));
        }

        [Test]
        public void GetSystemInfo_NoLogPath_ReturnsNullLogPath()
        {
            logConfigurationMock.Setup(x => x.LogPath).Returns((string?)null);

            var subject = CreateSubject();

            var result = subject.GetSystemInfo();

            Assert.That(result.LogPath, Is.Null);
        }

        [Test]
        public void GetSystemInfo_ReturnsNonEmptyOsDescription()
        {
            var subject = CreateSubject();

            var result = subject.GetSystemInfo();

            Assert.That(result.Os, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void GetSystemInfo_ReturnsNonEmptyRuntime()
        {
            var subject = CreateSubject();

            var result = subject.GetSystemInfo();

            Assert.That(result.Runtime, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void GetSystemInfo_ReturnsNonEmptyArchitecture()
        {
            var subject = CreateSubject();

            var result = subject.GetSystemInfo();

            Assert.That(result.Architecture, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void GetSystemInfo_ReturnsCurrentProcessId()
        {
            var subject = CreateSubject();

            var result = subject.GetSystemInfo();

            Assert.That(result.ProcessId, Is.EqualTo(Environment.ProcessId));
        }

        [Test]
        public void GetSystemInfo_SqliteProvider_ReturnsDatabaseFilePath()
        {
            SetupDatabaseProvider("sqlite");
            SetupConnectionString("Data Source=/data/lighthouse.db");

            var subject = CreateSubject();

            var result = subject.GetSystemInfo();

            Assert.That(result.DatabaseConnection, Is.EqualTo("/data/lighthouse.db"));
        }

        [Test]
        public void GetSystemInfo_PostgresProvider_ReturnsSafeConnectionInfo()
        {
            SetupDatabaseProvider("postgresql");
            SetupConnectionString("Host=myhost;Port=5432;Database=mydb;Username=admin;Password=secret123");

            var subject = CreateSubject();

            var result = subject.GetSystemInfo();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.DatabaseConnection, Does.Contain("Host=myhost"));
                Assert.That(result.DatabaseConnection, Does.Contain("Port=5432"));
                Assert.That(result.DatabaseConnection, Does.Contain("Database=mydb"));
                Assert.That(result.DatabaseConnection, Does.Not.Contain("Password"));
                Assert.That(result.DatabaseConnection, Does.Not.Contain("secret123"));
                Assert.That(result.DatabaseConnection, Does.Not.Contain("Username"));
                Assert.That(result.DatabaseConnection, Does.Not.Contain("admin"));
            }
        }

        [Test]
        public void GetSystemInfo_PostgresProviderShortAlias_ReturnsSafeConnectionInfo()
        {
            SetupDatabaseProvider("postgres");
            SetupConnectionString("Host=dbserver;Port=5432;Database=lighthouse;User Id=appuser;Pwd=REDACTED_IN_TEST");

            var subject = CreateSubject();

            var result = subject.GetSystemInfo();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.DatabaseConnection, Does.Contain("Host=dbserver"));
                Assert.That(result.DatabaseConnection, Does.Not.Contain("User Id"));
                Assert.That(result.DatabaseConnection, Does.Not.Contain("appuser"));
                Assert.That(result.DatabaseConnection, Does.Not.Contain("Pwd"));
                Assert.That(result.DatabaseConnection, Does.Not.Contain("topsecret"));
            }
        }

        [Test]
        public void GetSystemInfo_UnknownProvider_ReturnNullDatabaseConnection()
        {
            SetupDatabaseProvider("Unknown");
            SetupConnectionString("some-connection-string");

            var subject = CreateSubject();

            var result = subject.GetSystemInfo();

            Assert.That(result.DatabaseConnection, Is.Null);
        }

        [Test]
        public void GetSystemInfo_NoConnectionString_ReturnsNullDatabaseConnection()
        {
            SetupDatabaseProvider("sqlite");

            var subject = CreateSubject();

            var result = subject.GetSystemInfo();

            Assert.That(result.DatabaseConnection, Is.Null);
        }

        private void SetupDatabaseProvider(string provider)
        {
            var sectionMock = new Mock<IConfigurationSection>();
            sectionMock.Setup(s => s.Value).Returns(provider);
            configurationMock.Setup(c => c.GetSection("Database:Provider")).Returns(sectionMock.Object);
        }

        private void SetupConnectionString(string connectionString)
        {
            var sectionMock = new Mock<IConfigurationSection>();
            sectionMock.Setup(s => s.Value).Returns(connectionString);
            configurationMock.Setup(c => c.GetSection("Database:ConnectionString")).Returns(sectionMock.Object);
        }

        private SystemInfoService CreateSubject()
        {
            return new SystemInfoService(configurationMock.Object, logConfigurationMock.Object);
        }
    }
}
