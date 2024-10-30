using Lighthouse.Backend.API;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    public class LogsControllerTest
    {
        private Mock<ILogConfiguration> logConfigurationMock;

        [SetUp]
        public void Setup()
        {
            logConfigurationMock = new Mock<ILogConfiguration>();
        }

        [Test]
        public void GetLogLevel_ReturnsCurrentLogLevel()
        {
            var logLevel = "Warning";
            logConfigurationMock.SetupGet(x => x.CurrentLogLevel).Returns(logLevel);

            var subject = CreateSubject();

            var response = subject.GetLogLevel();

            Assert.Multiple(() =>
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = response.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                var actualLogLevel = okResult.Value as string;
                Assert.That(actualLogLevel, Is.EqualTo(logLevel));
            });            
        }

        [Test]
        public void SetLogLevel_ChangesCurrentLogLevel()
        {
            var expectedLogLevel = "Warning";

            var subject = CreateSubject();

            var response = subject.SetLogLevel(new LogsController.LogLevelDto { Level = expectedLogLevel });

            Assert.Multiple(() =>
            {
                Assert.That(response, Is.InstanceOf<OkResult>());

                var okResult = response as OkResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                logConfigurationMock.Verify(x => x.SetLogLevel(expectedLogLevel));
            });
        }

        [Test]        
        public void GetSupportedLogLevel_SupportsReturnsAvailableLogLevels()
        {
            var expectedLogLevels = new[] { "Level 1", "Level 2", "Level 3" };
            logConfigurationMock.SetupGet(x => x.SupportedLogLevels).Returns(expectedLogLevels);

            var subject = CreateSubject();

            var response = subject.GetSupportedLogLevels();

            Assert.Multiple(() =>
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = response.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                var supportedLogLevels = okResult.Value as string[];
                Assert.That(supportedLogLevels, Is.EquivalentTo(expectedLogLevels));
            });
        }

        [Test]
        public void GetLogs_ReturnsLogsFromLogConfiguration()
        {
            var expectedLogs = @"
This is ten percent luck, twenty percent skill
Fifteen percent concentrated power of will
Five percent pleasure, fifty percent pain
And a hundred percent reason to remember the name (Mike!)
";
            logConfigurationMock.Setup(x => x.GetLogs()).Returns(expectedLogs);

            var subject = CreateSubject();

            var response = subject.GetLogs();

            Assert.Multiple(() =>
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = response.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));

                var logs = okResult.Value as string;
                Assert.That(logs, Is.EqualTo(expectedLogs));
            });
        }


        private LogsController CreateSubject()
        {
            return new LogsController(logConfigurationMock.Object);
        }
    }
}
