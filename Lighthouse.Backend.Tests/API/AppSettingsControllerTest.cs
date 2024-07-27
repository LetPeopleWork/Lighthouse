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
        public async Task UpdateFeatureRefreshSettings_UpdatesSettingsAsync()
        {
            var refreshSettings = new RefreshSettings();

            var subject = CreateSubject();

            var result = await subject.UpdateFeatureRefreshSettingsAsync(refreshSettings);

            Assert.Multiple(() =>
            {
                appSettingServiceMock.Verify(x => x.UpdateFeatureRefreshSettingsAsync(refreshSettings), Times.Once);
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
        public async Task UpdateThroughputRefreshSettings_UpdatesSettingsAsync()
        {
            var refreshSettings = new RefreshSettings();

            var subject = CreateSubject();

            var result = await subject.UpdateThroughputRefreshSettingsAsync(refreshSettings);

            Assert.Multiple(() =>
            {
                appSettingServiceMock.Verify(x => x.UpdateThroughputRefreshSettingsAsync(refreshSettings), Times.Once);
                Assert.That(result, Is.InstanceOf<OkResult>());
            });
        }

        [Test]
        public void GetForecastRefreshSettings_ReturnsSettings()
        {
            var settings = new RefreshSettings();
            appSettingServiceMock.Setup(x => x.GetForecastRefreshSettings()).Returns(settings);

            var subject = CreateSubject();

            var result = subject.GetForecastRefreshSettings();

            Assert.Multiple(() =>
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
                Assert.That(okResult.Value, Is.EqualTo(settings));
            });
        }

        [Test]
        public async Task UpdateForecastRefreshSettings_UpdatesSettingsAsync()
        {
            var refreshSettings = new RefreshSettings();

            var subject = CreateSubject();

            var result = await subject.UpdateForecastRefreshSettingsAsync(refreshSettings);

            Assert.Multiple(() =>
            {
                appSettingServiceMock.Verify(x => x.UpdateForecastRefreshSettingsAsync(refreshSettings), Times.Once);
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
        public async Task UpdateDefaultTeamSettings_UpdatesSettingsAsync()
        {
            var settings = new TeamSettingDto();

            var subject = CreateSubject();

            var result = await subject.UpdateDefaultTeamSettingsAsync(settings);

            Assert.Multiple(() =>
            {
                appSettingServiceMock.Verify(x => x.UpdateDefaultTeamSettingsAsync(settings), Times.Once);
                Assert.That(result, Is.InstanceOf<OkResult>());
            });
        }

        private AppSettingsController CreateSubject()
        {
            return new AppSettingsController(appSettingServiceMock.Object);
        }
    }
}
