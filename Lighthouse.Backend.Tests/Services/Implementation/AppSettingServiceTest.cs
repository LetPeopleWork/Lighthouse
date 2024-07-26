using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Implementation;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class AppSettingServiceTests
    {
        private Mock<IRepository<AppSetting>> repositoryMock;

        [SetUp]
        public void Setup()
        {
            repositoryMock = new Mock<IRepository<AppSetting>>();
        }

        [Test]
        public void GetFeatureRefreshSettings_ReturnsCorrectSettings()
        {
            SetupRepositoryForKeys(AppSettingKeys.FeaturesRefreshInterval, "60", AppSettingKeys.FeaturesRefreshAfter, "360", AppSettingKeys.FeaturesRefreshStartDelay, "1");

            var service = CreateService();

            var settings = service.GetFeaturRefreshSettings();

            Assert.Multiple(() =>
            {
                Assert.That(settings.Interval, Is.EqualTo(60));
                Assert.That(settings.RefreshAfter, Is.EqualTo(360));
                Assert.That(settings.StartDelay, Is.EqualTo(1));
            });
        }

        [Test]
        public void GetForecastRefreshSettings_ReturnsCorrectSettings()
        {
            SetupRepositoryForKeys(AppSettingKeys.ForecastRefreshInterval, "20", AppSettingKeys.ForecastRefreshAfter, "120", AppSettingKeys.ForecastRefreshStartDelay, "3");

            var service = CreateService();

            var settings = service.GetForecastRefreshSettings();

            Assert.Multiple(() =>
            {
                Assert.That(settings.Interval, Is.EqualTo(20));
                Assert.That(settings.RefreshAfter, Is.EqualTo(120));
                Assert.That(settings.StartDelay, Is.EqualTo(3));
            });
        }

        [Test]
        public void GetThroughputRefreshSettings_ReturnsCorrectSettings()
        {
            SetupRepositoryForKeys(AppSettingKeys.ThroughputRefreshInterval, "30", AppSettingKeys.ThroughputRefreshAfter, "180", AppSettingKeys.ThroughputRefreshStartDelay, "2");

            var service = CreateService();

            var settings = service.GetThroughputRefreshSettings();

            Assert.Multiple(() =>
            {
                Assert.That(settings.Interval, Is.EqualTo(30));
                Assert.That(settings.RefreshAfter, Is.EqualTo(180));
                Assert.That(settings.StartDelay, Is.EqualTo(2));
            });
        }

        [Test]
        public void UpdateFeatureRefreshSettings_UpdatesCorrectly()
        {
            SetupRepositoryForKeys(AppSettingKeys.FeaturesRefreshInterval, "60", AppSettingKeys.FeaturesRefreshAfter, "360", AppSettingKeys.FeaturesRefreshStartDelay, "1");

            var service = CreateService();

            var newSettings = new RefreshSettings { Interval = 70, RefreshAfter = 370, StartDelay = 10 };
            service.UpdateFeatureRefreshSettings(newSettings);

            VerifyUpdateCalled(AppSettingKeys.FeaturesRefreshInterval, "70");
            VerifyUpdateCalled(AppSettingKeys.FeaturesRefreshAfter, "370");
            VerifyUpdateCalled(AppSettingKeys.FeaturesRefreshStartDelay, "10");
        }

        [Test]
        public void UpdateForecastRefreshSettings_UpdatesCorrectly()
        {
            SetupRepositoryForKeys(AppSettingKeys.ForecastRefreshInterval, "20", AppSettingKeys.ForecastRefreshAfter, "120", AppSettingKeys.ForecastRefreshStartDelay, "3");

            var service = CreateService();

            var newSettings = new RefreshSettings { Interval = 25, RefreshAfter = 130, StartDelay = 5 };
            service.UpdateForecastRefreshSettings(newSettings);

            VerifyUpdateCalled(AppSettingKeys.ForecastRefreshInterval, "25");
            VerifyUpdateCalled(AppSettingKeys.ForecastRefreshAfter, "130");
            VerifyUpdateCalled(AppSettingKeys.ForecastRefreshStartDelay, "5");
        }

        [Test]
        public void UpdateThroughputRefreshSettings_UpdatesCorrectly()
        {
            SetupRepositoryForKeys(AppSettingKeys.ThroughputRefreshInterval, "30", AppSettingKeys.ThroughputRefreshAfter, "180", AppSettingKeys.ThroughputRefreshStartDelay, "2");

            var service = CreateService();

            var newSettings = new RefreshSettings { Interval = 35, RefreshAfter = 190, StartDelay = 3 };
            service.UpdateThroughputRefreshSettings(newSettings);

            VerifyUpdateCalled(AppSettingKeys.ThroughputRefreshInterval, "35");
            VerifyUpdateCalled(AppSettingKeys.ThroughputRefreshAfter, "190");
            VerifyUpdateCalled(AppSettingKeys.ThroughputRefreshStartDelay, "3");
        }

        [Test]
        public void GetSettingByKey_KeyDoesNotExist_ThrowsException()
        {
            repositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<AppSetting, bool>>())).Returns((AppSetting)null);

            var service = CreateService();

            Assert.Throws<ArgumentNullException>(() => service.GetFeaturRefreshSettings());
        }

        private AppSettingService CreateService()
        {
            return new AppSettingService(repositoryMock.Object);
        }

        private void SetupRepositoryForKeys(params string[] keyValuePairs)
        {
            for (int i = 0; i < keyValuePairs.Length; i += 2)
            {
                var key = keyValuePairs[i];
                var value = keyValuePairs[i + 1];
                repositoryMock.Setup(x => x.GetByPredicate(It.Is<Func<AppSetting, bool>>(predicate => predicate(new AppSetting { Key = key })))).Returns(new AppSetting { Key = key, Value = value });
            }
        }

        private void VerifyUpdateCalled(string key, string value)
        {
            repositoryMock.Verify(x => x.Update(It.Is<AppSetting>(s => s.Key == key && s.Value == value)), Times.Once);
        }
    }
}