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

            using (Assert.EnterMultipleScope())
            {
                Assert.That(config["Serilog:MinimumLevel:Default"], Is.EqualTo(logLevel));
                Assert.That(subject.CurrentLogLevel, Is.EqualTo(logLevel));
            };
        }

        [Test]
        public void CreateConfiguration_NoConfigValue_InitializesWithInformation()
        {
            var expectedLogLevel = "Information";

            var config = SetupConfiguration("");
            var subject = CreateSubject(config);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.CurrentLogLevel, Is.EqualTo(expectedLogLevel));
            };
        }

        [Test]
        public void SetLogLevel_ChangesCurrentLogLevel()
        {
            var logLevel = "Information";

            var config = SetupConfiguration("Warning");
            var subject = CreateSubject(config);

            subject.SetLogLevel(logLevel);

            using (Assert.EnterMultipleScope())
            {
                configFileUpdaterMock.Verify(x => x.UpdateConfigFile("Serilog:MinimumLevel:Default", logLevel));
                Assert.That(subject.CurrentLogLevel, Is.EqualTo(logLevel));
            };
        }

        [Test]
        public void SetLogLevel_InvalidLogLevel_DefaultsToInformation()
        {
            var expectedLogLevel = "Information";

            var config = SetupConfiguration("Warning");
            var subject = CreateSubject(config);

            subject.SetLogLevel("This is not the best log level in the world - this is just a tribute...");

            using (Assert.EnterMultipleScope())
            {
                configFileUpdaterMock.Verify(x => x.UpdateConfigFile("Serilog:MinimumLevel:Default", expectedLogLevel));
                Assert.That(subject.CurrentLogLevel, Is.EqualTo(expectedLogLevel));
            };
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

            Assert.That(supportedLogLevels, Does.Contain(logLevel.ToString()));
        }

        [Test]
        public void GetLogs_NoLogFiles_ReturnsLogsNotFound()
        {
            var config = SetupConfiguration("Warning");
            fileSystemMock
                .Setup(fs => fs.GetFiles(It.IsAny<string>(), "*.txt"))
                .Returns([]);

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

        // Bug #6020. Reading the whole file is what a Download is for. Following the log means asking
        // again every few seconds, and a Debug-level instance writes a file far too large to re-send
        // each time - so the caller can ask for the end of it instead.
        [Test]
        public void GetLogs_TailAsked_ReturnsOnlyTheEndOfTheFile()
        {
            var subject = ASubjectWhoseNewestLogHolds("first line\nsecond line\nthird line\n");

            var result = subject.GetLogs(tailBytes: "third line\n".Length);

            Assert.That(result, Is.EqualTo("third line\n"));
        }

        /// <summary>
        /// The end of a file is not the start of a line, and a byte offset can also land inside a
        /// multi-byte character. Both are cured the same way: begin after the first newline that the
        /// read picked up, so whatever fragment preceded it never reaches the reader.
        /// </summary>
        [Test]
        public void GetLogs_TailLandsInsideALine_BeginsAtTheNextWholeLine()
        {
            var subject = ASubjectWhoseNewestLogHolds("first line\nsecond line\nthird line\n");

            var result = subject.GetLogs(tailBytes: "cond line\nthird line\n".Length);

            Assert.That(result, Is.EqualTo("third line\n"));
        }

        [Test]
        public void GetLogs_TailBiggerThanTheFile_ReturnsTheWholeFile()
        {
            var wholeFile = "first line\nsecond line\n";
            var subject = ASubjectWhoseNewestLogHolds(wholeFile);

            var result = subject.GetLogs(tailBytes: 4096);

            Assert.That(result, Is.EqualTo(wholeFile));
        }

        [Test]
        public void GetLogs_NoTailAsked_StillReturnsTheWholeFile()
        {
            var wholeFile = "first line\nsecond line\n";
            var subject = ASubjectWhoseNewestLogHolds(wholeFile);

            var result = subject.GetLogs();

            Assert.That(result, Is.EqualTo(wholeFile));
        }

        private SerilogLogConfiguration ASubjectWhoseNewestLogHolds(string content)
        {
            var config = SetupConfiguration("Warning", "./logs/log-.txt");

            fileSystemMock
                .Setup(fs => fs.GetFiles(It.IsAny<string>(), "*.txt"))
                .Returns(["log-20240721.txt"]);

            fileSystemMock
                .Setup(fs => fs.OpenFile(It.IsAny<string>(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                .Returns(() => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content)));

            return CreateSubject(config);
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

        private IConfiguration SetupConfiguration(string logLevel, string logsFolder = "/logs")
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
