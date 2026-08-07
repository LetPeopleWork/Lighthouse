using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Reflection;

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

        /// <summary>
        /// Epic 5375 moved the guard from the class onto each route, because the ordering-policy read is
        /// deliberately open — every feature list asks it to name its position column, so guarding it
        /// would leave everyone but an instance administrator reading the wrong heading. Every other
        /// route on this controller still requires SystemAdmin, and that is what this pins: a future
        /// route added without a guard fails here rather than shipping open.
        /// </summary>
        [Test]
        public void EverySettingsRouteExceptTheOrderingPolicyReadRequiresSystemAdmin()
        {
            var guardedRoutes = typeof(AppSettingsController)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(method => method.Name != nameof(AppSettingsController.GetFeatureOrdering));

            using (Assert.EnterMultipleScope())
            {
                foreach (var route in guardedRoutes)
                {
                    var guard = route
                        .GetCustomAttributes(typeof(RbacGuardAttribute), inherit: true)
                        .Cast<RbacGuardAttribute>()
                        .SingleOrDefault();

                    Assert.That(guard, Is.Not.Null, $"{route.Name} must be guarded.");
                    Assert.That(guard!.Requirement, Is.EqualTo(RbacGuardRequirement.SystemAdmin), $"{route.Name} must require SystemAdmin.");
                }

                Assert.That(
                    typeof(AppSettingsController).GetCustomAttributes(typeof(RbacGuardAttribute), inherit: true),
                    Is.Empty,
                    "A class-level guard would silently re-close the ordering-policy read.");

                Assert.That(
                    typeof(AppSettingsController)
                        .GetMethod(nameof(AppSettingsController.GetFeatureOrdering))!
                        .GetCustomAttributes(typeof(RbacGuardAttribute), inherit: true),
                    Is.Empty,
                    "Reading which ordering the instance uses is open to every viewer.");
            }
        }
    }
}
