using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;
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
        public async Task GetFeatureRefreshSettings_ReturnsSettings()
        {
            var settings = new RefreshSettings();
            appSettingServiceMock.Setup(x => x.GetFeatureRefreshSettings()).Returns(settings);

            var subject = CreateSubject();

            var result = await subject.GetFeatureRefreshSettings(CancellationToken.None);

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

            var result = await subject.UpdateFeatureRefreshSettings(refreshSettings, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                appSettingServiceMock.Verify(x => x.UpdateFeatureRefreshSettings(refreshSettings), Times.Once);
                Assert.That(result, Is.InstanceOf<OkResult>());
            }
        }

        [Test]
        public async Task GetTeamDataRefreshSettings_ReturnsSettings()
        {
            var settings = new RefreshSettings();
            appSettingServiceMock.Setup(x => x.GetTeamDataRefreshSettings()).Returns(settings);

            var subject = CreateSubject();

            var result = await subject.GetTeamDataRefreshSettings(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
                Assert.That(okResult.Value, Is.EqualTo(settings));
            }
        }

        [Test]
        public async Task UpdateTeamDataRefreshSettings_UpdatesSettings()
        {
            var refreshSettings = new RefreshSettings();

            var subject = CreateSubject();

            var result = await subject.UpdateTeamDataRefreshSettings(refreshSettings, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                appSettingServiceMock.Verify(x => x.UpdateTeamDataRefreshSettings(refreshSettings), Times.Once);
                Assert.That(result, Is.InstanceOf<OkResult>());
            }
        }

        private AppSettingsController CreateSubject()
        {
            return new AppSettingsController(appSettingServiceMock.Object);
        }

        [Test]
        public void Controller_HasSystemAdminRbacGuardAttribute()
        {
            var attribute = typeof(AppSettingsController)
                .GetCustomAttributes(typeof(RbacGuardAttribute), inherit: true)
                .Cast<RbacGuardAttribute>()
                .SingleOrDefault();

            Assert.That(attribute, Is.Not.Null);
            Assert.That(attribute!.Requirement, Is.EqualTo(RbacGuardRequirement.SystemAdmin));
        }
    }
}
