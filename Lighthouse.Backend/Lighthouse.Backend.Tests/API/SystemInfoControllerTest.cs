using Lighthouse.Backend.API;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    public class SystemInfoControllerTest
    {
        private Mock<ISystemInfoService> systemInfoServiceMock;

        [SetUp]
        public void Setup()
        {
            systemInfoServiceMock = new Mock<ISystemInfoService>();
        }

        [Test]
        public void GetSystemInfo_ReturnsSystemInfoFromService()
        {
            var expectedSystemInfo = new SystemInfo(
                Os: "Linux 5.15.0",
                Runtime: ".NET 10.0.0",
                Architecture: "X64",
                ProcessId: 12345,
                DatabaseProvider: "sqlite",
                DatabaseConnection: "/data/lighthouse.db",
                LogPath: "/var/log/lighthouse");

            systemInfoServiceMock.Setup(x => x.GetSystemInfo()).Returns(expectedSystemInfo);

            var subject = CreateSubject();

            var response = subject.GetSystemInfo();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = response.Result as OkObjectResult;
                Assert.That(okResult!.StatusCode, Is.EqualTo(200));

                var actual = okResult.Value as SystemInfo;
                Assert.That(actual, Is.EqualTo(expectedSystemInfo));
            }
        }

        [Test]
        public void GetSystemInfo_WithoutLogPath_ReturnsSystemInfoWithNullLogPath()
        {
            var expectedSystemInfo = new SystemInfo(
                Os: "Windows 11",
                Runtime: ".NET 10.0.0",
                Architecture: "X64",
                ProcessId: 99,
                DatabaseProvider: "postgresql",
                DatabaseConnection: "Host=myhost;Port=5432;Database=mydb",
                LogPath: null);

            systemInfoServiceMock.Setup(x => x.GetSystemInfo()).Returns(expectedSystemInfo);

            var subject = CreateSubject();

            var response = subject.GetSystemInfo();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = response.Result as OkObjectResult;
                var actual = okResult!.Value as SystemInfo;
                Assert.That(actual!.LogPath, Is.Null);
            }
        }

        private SystemInfoController CreateSubject()
        {
            return new SystemInfoController(systemInfoServiceMock.Object);
        }
    }
}
