using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;

namespace Lighthouse.Backend.Tests.Services.Implementation.Repositories
{
    public class AppSettingRepositoryTest() : IntegrationTestBase(new TestWebApplicationFactory<Program>())
    {
        [Test]
        [TestCase(AppSettingKeys.TeamDataRefreshInterval, "60")]
        [TestCase(AppSettingKeys.TeamDataRefreshAfter, "180")]
        [TestCase(AppSettingKeys.TeamDataRefreshStartDelay, "10")]
        [TestCase(AppSettingKeys.FeaturesRefreshInterval, "60")]
        [TestCase(AppSettingKeys.FeaturesRefreshAfter, "180")]
        [TestCase(AppSettingKeys.FeaturesRefreshStartDelay, "15")]
        public void AddsDefaultAppSettingsIfMissing(string key, string expectedValue)
        {
            var subject = CreateSubject();
            
            var appSetting = subject.GetByPredicate(s => s.Key == key);

            Assert.That(appSetting.Value, Is.EqualTo(expectedValue));
        }

        [TearDown]
        protected override void TearDown()
        {
            ResetSeedingFlag();
            base.TearDown();
        }

        private static void ResetSeedingFlag()
        {
            var type = typeof(AppSettingRepository);
            var field = type.GetField("hasSeeded", BindingFlags.NonPublic | BindingFlags.Static);

            if (field != null)
            {
                field.SetValue(null, false);
            }
        }

        private AppSettingRepository CreateSubject()
        {
            return new AppSettingRepository(DatabaseContext, Mock.Of<ILogger<AppSettingRepository>>());
        }
    }
}
