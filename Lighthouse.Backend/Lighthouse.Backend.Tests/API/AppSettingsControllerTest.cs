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

            Assert.Multiple(() =>
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
                Assert.That(okResult.Value, Is.EqualTo(settings));
            });
        }

        [Test]
        public async Task UpdateFeatureRefreshSettings_UpdatesSettings()
        {
            var refreshSettings = new RefreshSettings();

            var subject = CreateSubject();

            var result = await subject.UpdateFeatureRefreshSettings(refreshSettings);

            Assert.Multiple(() =>
            {
                appSettingServiceMock.Verify(x => x.UpdateFeatureRefreshSettings(refreshSettings), Times.Once);
                Assert.That(result, Is.InstanceOf<OkResult>());
            });
        }

        [Test]
        public void GetThroughputRefreshSettings_ReturnsSettings()
        {
            var settings = new RefreshSettings();
            appSettingServiceMock.Setup(x => x.GetThroughputRefreshSettings()).Returns(settings);

            var subject = CreateSubject();

            var result = subject.GetThroughputRefreshSettings();

            Assert.Multiple(() =>
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
                Assert.That(okResult.Value, Is.EqualTo(settings));
            });
        }

        [Test]
        public async Task UpdateThroughputRefreshSettings_UpdatesSettings()
        {
            var refreshSettings = new RefreshSettings();

            var subject = CreateSubject();

            var result = await subject.UpdateThroughputRefreshSettings(refreshSettings);

            Assert.Multiple(() =>
            {
                appSettingServiceMock.Verify(x => x.UpdateThroughputRefreshSettings(refreshSettings), Times.Once);
                Assert.That(result, Is.InstanceOf<OkResult>());
            });
        }

        [Test]
        public void GetDefaultTeamSettings_ReturnsSettings()
        {
            var settings = new TeamSettingDto();
            appSettingServiceMock.Setup(x => x.GetDefaultTeamSettings()).Returns(settings);

            var subject = CreateSubject();

            var result = subject.GetDefaultTeamSettings();

            Assert.Multiple(() =>
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
                Assert.That(okResult.Value, Is.EqualTo(settings));
            });
        }

        [Test]
        public async Task UpdateDefaultTeamSettings_UpdatesSettings()
        {
            var settings = new TeamSettingDto();

            var subject = CreateSubject();

            var result = await subject.UpdateDefaultTeamSettings(settings);

            Assert.Multiple(() =>
            {
                appSettingServiceMock.Verify(x => x.UpdateDefaultTeamSettings(settings), Times.Once);
                Assert.That(result, Is.InstanceOf<OkResult>());
            });
        }

        [Test]
        public void GetDefaultProjectSettings_ReturnsSettings()
        {
            var settings = new ProjectSettingDto();
            appSettingServiceMock.Setup(x => x.GetDefaultProjectSettings()).Returns(settings);

            var subject = CreateSubject();

            var result = subject.GetDefaultProjectSettings();

            Assert.Multiple(() =>
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
                Assert.That(okResult.Value, Is.EqualTo(settings));
            });
        }

        [Test]
        public async Task UpdateDefaultProjectSettings_UpdatesSettings()
        {
            var settings = new ProjectSettingDto();

            var subject = CreateSubject();

            var result = await subject.UpdateDefaultProjectSettings(settings);

            Assert.Multiple(() =>
            {
                appSettingServiceMock.Verify(x => x.UpdateDefaultProjectSettings(settings), Times.Once);
                Assert.That(result, Is.InstanceOf<OkResult>());
            });
        }

        [Test]
        public void GetDataRetentionSettings_ReturnsSettings()
        {
            var settings = new DataRetentionSettings { MaxStorageTimeInDays = 12 };
            appSettingServiceMock.Setup(x => x.GetDataRetentionSettings()).Returns(settings);

            var subject = CreateSubject();

            var result = subject.GetDataRetentionSettings();

            Assert.Multiple(() =>
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
                Assert.That(okResult.Value, Is.EqualTo(settings));
            });
        }

        [Test]
        public async Task UpdateDataRetentionSettings_UpdatesSettings()
        {
            var settings = new DataRetentionSettings { MaxStorageTimeInDays = 1337 };

            var subject = CreateSubject();

            var result = await subject.UpdateDataRetentionSettings(settings);

            Assert.Multiple(() =>
            {
                appSettingServiceMock.Verify(x => x.UpdateDataRetentionSettings(settings), Times.Once);
                Assert.That(result, Is.InstanceOf<OkResult>());
            });
        }

        private AppSettingsController CreateSubject()
        {
            return new AppSettingsController(appSettingServiceMock.Object);
        }
    }
}
