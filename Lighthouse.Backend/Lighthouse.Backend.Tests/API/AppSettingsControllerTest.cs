using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    public class AppSettingsControllerTest
    {
        private Mock<IAppSettingService> appSettingServiceMock;

        [SetUp]
        public void Setup()
        {
            appSettingServiceMock = new Mock<IAppSettingService>();
        }

        [Test]
        public void GetFeatureRefreshSettings_ReturnsSettings()
        {
            var settings = new RefreshSettings();
            appSettingServiceMock.Setup(x => x.GetFeaturRefreshSettings()).Returns(settings);

            var subject = CreateSubject();

            var result = subject.GetFeatureRefreshSettings();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
                Assert.That(okResult.Value, Is.EqualTo(settings));
            }
        }

        [Test]
        public async Task UpdateFeatureRefreshSettings_UpdatesSettings()
        {
            var refreshSettings = new RefreshSettings();

            var subject = CreateSubject();

            var result = await subject.UpdateFeatureRefreshSettings(refreshSettings);

            using (Assert.EnterMultipleScope())
            {
                appSettingServiceMock.Verify(x => x.UpdateFeatureRefreshSettings(refreshSettings), Times.Once);
                Assert.That(result, Is.InstanceOf<OkResult>());
            };
        }

        [Test]
        public void GetTeamDataRefreshSettings_ReturnsSettings()
        {
            var settings = new RefreshSettings();
            appSettingServiceMock.Setup(x => x.GetTeamDataRefreshSettings()).Returns(settings);

            var subject = CreateSubject();

            var result = subject.GetTeamDataRefreshSettings();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
                Assert.That(okResult.Value, Is.EqualTo(settings));
            };
        }

        [Test]
        public async Task UpdateTeamDataRefreshSettings_UpdatesSettings()
        {
            var refreshSettings = new RefreshSettings();

            var subject = CreateSubject();

            var result = await subject.UpdateTeamDataRefreshSettings(refreshSettings);

            using (Assert.EnterMultipleScope())
            {
                appSettingServiceMock.Verify(x => x.UpdateTeamDataRefreshSettings(refreshSettings), Times.Once);
                Assert.That(result, Is.InstanceOf<OkResult>());
            };
        }

        [Test]
        public void GetDefaultTeamSettings_ReturnsSettings()
        {
            var settings = new TeamSettingDto();
            appSettingServiceMock.Setup(x => x.GetDefaultTeamSettings()).Returns(settings);

            var subject = CreateSubject();

            var result = subject.GetDefaultTeamSettings();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
                Assert.That(okResult.Value, Is.EqualTo(settings));
            };
        }

        [Test]
        public void GetDefaultProjectSettings_ReturnsSettings()
        {
            var settings = new ProjectSettingDto();
            appSettingServiceMock.Setup(x => x.GetDefaultProjectSettings()).Returns(settings);

            var subject = CreateSubject();

            var result = subject.GetDefaultProjectSettings();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
                Assert.That(okResult.Value, Is.EqualTo(settings));
            };
        }

        [Test]
        public void GetWorkTrackingSystemSettings_ReturnsSettings()
        {
            var settings = new WorkTrackingSystemSettings { OverrideRequestTimeout = true, RequestTimeoutInSeconds = 300 };
            appSettingServiceMock.Setup(x => x.GetWorkTrackingSystemSettings()).Returns(settings);

            var subject = CreateSubject();

            var result = subject.GetWorkTrackingSystemSettings();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
                Assert.That(okResult.Value, Is.EqualTo(settings));
            };
        }

        [Test]
        public async Task UpdateWorkTrackingSystemSettings_UpdatesSettings()
        {
            var settings = new WorkTrackingSystemSettings { OverrideRequestTimeout = true, RequestTimeoutInSeconds = 300 };

            var subject = CreateSubject();

            var result = await subject.UpdateWorkTrackingSystemSettings(settings);

            using (Assert.EnterMultipleScope())
            {
                appSettingServiceMock.Verify(x => x.UpdateWorkTrackingSystemSettings(settings), Times.Once);
                Assert.That(result, Is.InstanceOf<OkResult>());
            };
        }

        private AppSettingsController CreateSubject()
        {
            return new AppSettingsController(appSettingServiceMock.Object);
        }
    }
}
