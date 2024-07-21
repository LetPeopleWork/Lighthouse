using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Configuration;
using Moq;
using Serilog.Events;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class SerilogLogConfigurationTest
    {
        private Mock<IConfigFileUpdater> configFileUpdaterMock;
        private Mock<IFileSystemService> fileSystemMock;

        [SetUp]
        public void Setup()
        {
            configFileUpdaterMock = new Mock<IConfigFileUpdater>();
            fileSystemMock = new Mock<IFileSystemService>();
        }

        [Test]
        public void CreateConfiguration_InitializesWithValueFromConfig()
        {
            var logLevel = "Warning";

            var config = SetupConfiguration(logLevel);
            var subject = CreateSubject(config);

            Assert.Multiple(() =>
            {
                Assert.That(config["Serilog:MinimumLevel:Default"], Is.EqualTo(logLevel));
                Assert.That(subject.CurrentLogLevel, Is.EqualTo(logLevel));
            });
        }

        [Test]
        public void CreateConfiguration_NoConfigValue_InitializesWithInformation()
        {
            var expectedLogLevel = "Information";

            var config = SetupConfiguration("");
            var subject = CreateSubject(config);

            Assert.Multiple(() =>
            {
                Assert.That(subject.CurrentLogLevel, Is.EqualTo(expectedLogLevel));
            });
        }

        [Test]
        public void SetLogLevel_ChangesCurrentLogLevel()
        {
            var logLevel = "Information";

            var config = SetupConfiguration("Warning");
            var subject = CreateSubject(config);

            subject.SetLogLevel(logLevel);

            Assert.Multiple(() =>
            {
                configFileUpdaterMock.Verify(x => x.UpdateConfigFile("Serilog:MinimumLevel:Default", logLevel));
                Assert.That(subject.CurrentLogLevel, Is.EqualTo(logLevel));
            });
        }

        [Test]
        public void SetLogLevel_InvalidLogLevel_DefaultsToInformation()
        {
            var expectedLogLevel = "Information";

            var config = SetupConfiguration("Warning");
            var subject = CreateSubject(config);

            subject.SetLogLevel("This is not the best log level in the world - this is just a tribute...");

            Assert.Multiple(() =>
            {
                configFileUpdaterMock.Verify(x => x.UpdateConfigFile("Serilog:MinimumLevel:Default", expectedLogLevel));
                Assert.That(subject.CurrentLogLevel, Is.EqualTo(expectedLogLevel));
            });
        }

        [Test]
        [TestCase(LogEventLevel.Verbose)]
        [TestCase(LogEventLevel.Debug)]
        [TestCase(LogEventLevel.Information)]
        [TestCase(LogEventLevel.Warning)]
        [TestCase(LogEventLevel.Error)]
        [TestCase(LogEventLevel.Fatal)]
        public void GetSupportedLogLevel_SupportsAllSerilogLogLevels(LogEventLevel logLevel)
        {
            var config = SetupConfiguration("Warning");
            var subject = CreateSubject(config);

            var supportedLogLevels = subject.SupportedLogLevels;

            CollectionAssert.Contains(supportedLogLevels, logLevel.ToString());
        }

        [Test]
        public void GetLogs_NoLogFiles_ReturnsLogsNotFound()
        {
            var config = SetupConfiguration("Warning");
            fileSystemMock
                .Setup(fs => fs.GetFiles(It.IsAny<string>(), "*.txt"))
                .Returns(Array.Empty<string>());

            var subject = CreateSubject(config);

            var result = subject.GetLogs();

            Assert.That(result, Is.EqualTo("Logs not Found"));
        }

        [Test]
        public void GetLogs_WithLogFiles_ReturnsContentOfNewestFile()
        {
            var logFolder = "./logs/log-.txt";
            var logFilePath = "log-20240721.txt";
            var logFileContent = "Log content";

            var config = SetupConfiguration("Warning", logFolder);

            fileSystemMock
                .Setup(fs => fs.GetFiles(It.IsAny<string>(), "*.txt"))
                .Returns([logFilePath]);

            fileSystemMock
                .Setup(fs => fs.OpenFile(It.IsAny<string>(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                .Returns(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(logFileContent)));

            var subject = CreateSubject(config);

            var result = subject.GetLogs();

            Assert.That(result, Is.EqualTo(logFileContent));
        }

        [Test]
        public void GetLogs_FileOperationException_ReturnsLogsNotFound()
        {
            var config = SetupConfiguration("Warning");

            fileSystemMock
                .Setup(fs => fs.GetFiles(It.IsAny<string>(), "*.txt"))
                .Throws(new IOException());

            var subject = CreateSubject(config);

            var result = subject.GetLogs();

            Assert.That(result, Is.EqualTo("Logs not Found"));
        }

        private SerilogLogConfiguration CreateSubject(IConfiguration config)
        {
            return new SerilogLogConfiguration(config, configFileUpdaterMock.Object, fileSystemMock.Object);
        }

        private IConfiguration SetupConfiguration(string logLevel, string logsFolder = "")
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "Serilog:MinimumLevel:Default", logLevel },
                { "Serilog:WriteTo:0:Name", "File" },
                { "Serilog:WriteTo:0:Args:path", logsFolder },
            };

            return TestConfiguration.SetupTestConfiguration(inMemorySettings);
        }
    }
}
