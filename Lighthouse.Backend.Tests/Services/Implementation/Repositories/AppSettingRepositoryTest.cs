using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Lighthouse.Backend.WorkTracking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Repositories
{
    public class AppSettingRepositoryTest : IntegrationTestBase
    {
        public AppSettingRepositoryTest() : base(new TestWebApplicationFactory<Program>())
        {
        }

        [Test]
        [TestCase(AppSettingKeys.ThroughputRefreshInterval, "60")]
        [TestCase(AppSettingKeys.ThroughputRefreshAfter, "180")]
        [TestCase(AppSettingKeys.ThroughputRefreshStartDelay, "1")]
        [TestCase(AppSettingKeys.FeaturesRefreshInterval, "60")]
        [TestCase(AppSettingKeys.FeaturesRefreshAfter, "180")]
        [TestCase(AppSettingKeys.FeaturesRefreshStartDelay, "2")]
        [TestCase(AppSettingKeys.ForecastRefreshInterval, "60")]
        [TestCase(AppSettingKeys.ForecastRefreshAfter, "180")]
        [TestCase(AppSettingKeys.ForecastRefreshStartDelay, "5")]
        [TestCase(AppSettingKeys.TeamSettingName, "New Team")]
        [TestCase(AppSettingKeys.TeamSettingHistory, "30")]
        [TestCase(AppSettingKeys.TeamSettingFeatureWIP, "1")]
        [TestCase(AppSettingKeys.TeamSettingWorkItemQuery, "")]
        [TestCase(AppSettingKeys.TeamSettingWorkItemTypes, "User Story,Bug")]
        [TestCase(AppSettingKeys.TeamSettingRelationCustomField, "")]
        [TestCase(AppSettingKeys.ProjectSettingName, "New Project")]
        [TestCase(AppSettingKeys.ProjectSettingWorkItemQuery, "")]
        [TestCase(AppSettingKeys.ProjectSettingWorkItemTypes, "Epic")]
        [TestCase(AppSettingKeys.ProjectSettingUnparentedWorkItemQuery, "")]
        [TestCase(AppSettingKeys.ProjectSettingDefaultAmountOfWorkItemsPerFeature, "10")]
        public void AddsDefaultAppSettingsIfMissing(string key, string expectedValue)
        {
            var subject = CreateSubject();
            
            var appSetting = subject.GetByPredicate(s => s.Key == key);

            Assert.That(appSetting.Value, Is.EqualTo(expectedValue));
        }

        [Test]
        public async Task UpdateDefaultSetting_DoesNotOverrideOnNextStart()
        {
            var subject = CreateSubject();

            var appSetting = subject.GetByPredicate(s => s.Key == AppSettingKeys.TeamSettingFeatureWIP) ?? throw new ArgumentException($"Could not find setting with key {AppSettingKeys.TeamSettingFeatureWIP}");

            appSetting.Value = "42";
            subject.Update(appSetting);
            await subject.Save();

            subject = CreateSubject();
            appSetting = subject.GetByPredicate(s => s.Key == AppSettingKeys.TeamSettingFeatureWIP);

            Assert.That(appSetting.Value, Is.EqualTo("42"));
        }

        [Test]
        public async Task StartRepo_DefaultSettingRemoved_AddsDefault()
        {
            var subject = CreateSubject();

            var appSetting = subject.GetByPredicate(s => s.Key == AppSettingKeys.TeamSettingFeatureWIP) ?? throw new ArgumentException($"Could not find setting with key {AppSettingKeys.TeamSettingFeatureWIP}");

            subject.Remove(appSetting);
            await subject.Save();

            subject = CreateSubject();
            appSetting = subject.GetByPredicate(s => s.Key == AppSettingKeys.TeamSettingFeatureWIP);

            Assert.That(appSetting.Value, Is.EqualTo("1"));
        }

        private AppSettingRepository CreateSubject()
        {
            return new AppSettingRepository(DatabaseContext, Mock.Of<ILogger<AppSettingRepository>>());
        }
    }
}
