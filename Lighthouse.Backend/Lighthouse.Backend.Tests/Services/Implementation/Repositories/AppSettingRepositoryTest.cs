using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
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
        [TestCase(AppSettingKeys.TeamDataRefreshInterval, "60")]
        [TestCase(AppSettingKeys.TeamDataRefreshAfter, "180")]
        [TestCase(AppSettingKeys.TeamDataRefreshStartDelay, "10")]
        [TestCase(AppSettingKeys.FeaturesRefreshInterval, "60")]
        [TestCase(AppSettingKeys.FeaturesRefreshAfter, "180")]
        [TestCase(AppSettingKeys.FeaturesRefreshStartDelay, "15")]
        [TestCase(AppSettingKeys.TeamSettingName, "New Team")]
        [TestCase(AppSettingKeys.TeamSettingHistory, "30")]
        [TestCase(AppSettingKeys.TeamSettingFeatureWIP, "1")]
        [TestCase(AppSettingKeys.TeamSettingWorkItemQuery, "")]
        [TestCase(AppSettingKeys.TeamSettingWorkItemTypes, "User Story,Bug")]
        [TestCase(AppSettingKeys.TeamSettingParentOverrideField, "")]
        [TestCase(AppSettingKeys.ProjectSettingName, "New Project")]
        [TestCase(AppSettingKeys.ProjectSettingWorkItemQuery, "")]
        [TestCase(AppSettingKeys.ProjectSettingWorkItemTypes, "Epic")]
        [TestCase(AppSettingKeys.ProjectSettingUnparentedWorkItemQuery, "")]
        [TestCase(AppSettingKeys.ProjectSettingDefaultAmountOfWorkItemsPerFeature, "10")]
        [TestCase(AppSettingKeys.ProjectSettingSizeEstimateField, "")]
        [TestCase(AppSettingKeys.ProjectSettingsFeatureOwnerField, "")]
        [TestCase(AppSettingKeys.TeamSettingAutomaticallyAdjustFeatureWIP, "False")]
        [TestCase(AppSettingKeys.TeamSettingTags, "")]
        [TestCase(AppSettingKeys.ProjectSettingOverrideRealChildCountStates, "")]
        [TestCase(AppSettingKeys.ProjectSettingTags, "")]
        [TestCase(AppSettingKeys.TeamSettingSLEProbability, "0")]
        [TestCase(AppSettingKeys.TeamSettingSLERange, "0")]
        [TestCase(AppSettingKeys.ProjectSettingSLEProbability, "0")]
        [TestCase(AppSettingKeys.ProjectSettingSLERange, "0")]
        [TestCase(AppSettingKeys.WorkTrackingSystemSettingsOverrideRequestTimeout, "False")]
        [TestCase(AppSettingKeys.WorkTrackingSystemSettingsRequestTimeoutInSeconds, "100")]
        [TestCase(AppSettingKeys.ProjectSettingParentOverrideField, "")]
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
