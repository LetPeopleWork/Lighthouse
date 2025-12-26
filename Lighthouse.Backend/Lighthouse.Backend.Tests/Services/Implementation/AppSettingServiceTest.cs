using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Implementation;
using Moq;
using Lighthouse.Backend.Services.Interfaces.Repositories;

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

            using (Assert.EnterMultipleScope())
            {
                Assert.That(settings.Interval, Is.EqualTo(60));
                Assert.That(settings.RefreshAfter, Is.EqualTo(360));
                Assert.That(settings.StartDelay, Is.EqualTo(1));
            }
        }

        [Test]
        public void GetTeamDataRefreshSettings_ReturnsCorrectSettings()
        {
            SetupRepositoryForKeys(AppSettingKeys.TeamDataRefreshInterval, "30", AppSettingKeys.TeamDataRefreshAfter, "180", AppSettingKeys.TeamDataRefreshStartDelay, "2");

            var service = CreateService();

            var settings = service.GetTeamDataRefreshSettings();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(settings.Interval, Is.EqualTo(30));
                Assert.That(settings.RefreshAfter, Is.EqualTo(180));
                Assert.That(settings.StartDelay, Is.EqualTo(2));
            }
        }

        [Test]
        public async Task UpdateFeatureRefreshSettings_UpdatesCorrectlyAsync()
        {
            SetupRepositoryForKeys(AppSettingKeys.FeaturesRefreshInterval, "60", AppSettingKeys.FeaturesRefreshAfter, "360", AppSettingKeys.FeaturesRefreshStartDelay, "1");

            var service = CreateService();

            var newSettings = new RefreshSettings { Interval = 70, RefreshAfter = 370, StartDelay = 10 };
            await service.UpdateFeatureRefreshSettings(newSettings);

            VerifyUpdateCalled(AppSettingKeys.FeaturesRefreshInterval, "70");
            VerifyUpdateCalled(AppSettingKeys.FeaturesRefreshAfter, "370");
            VerifyUpdateCalled(AppSettingKeys.FeaturesRefreshStartDelay, "10");
        }

        [Test]
        public async Task UpdateTeamDataRefreshSettings_UpdatesCorrectlyAsync()
        {
            SetupRepositoryForKeys(AppSettingKeys.TeamDataRefreshInterval, "30", AppSettingKeys.TeamDataRefreshAfter, "180", AppSettingKeys.TeamDataRefreshStartDelay, "2");

            var service = CreateService();

            var newSettings = new RefreshSettings { Interval = 35, RefreshAfter = 190, StartDelay = 3 };
            await service.UpdateTeamDataRefreshSettings(newSettings);

            VerifyUpdateCalled(AppSettingKeys.TeamDataRefreshInterval, "35");
            VerifyUpdateCalled(AppSettingKeys.TeamDataRefreshAfter, "190");
            VerifyUpdateCalled(AppSettingKeys.TeamDataRefreshStartDelay, "3");
        }

        [Test]
        public void GetDefaultTeamSettings_ReturnsCorrectSettings()
        {
            SetupRepositoryForKeys(
                AppSettingKeys.TeamSettingName, "MyTeam",
                AppSettingKeys.TeamSettingHistory, "90",
                AppSettingKeys.TeamSettingFeatureWIP, "2",
                AppSettingKeys.TeamSettingWorkItemQuery, "[System.TeamProject] = \"MyProject\"",
                AppSettingKeys.TeamSettingWorkItemTypes, "Product Backlog Item, Bug",
                AppSettingKeys.TeamSettingToDoStates, "New,Planned",
                AppSettingKeys.TeamSettingDoingStates, "In Progress,Committed",
                AppSettingKeys.TeamSettingDoneStates, "Closed,Done",
                AppSettingKeys.TeamSettingParentOverrideField, "Custom.RemoteParentID",
                AppSettingKeys.TeamSettingAutomaticallyAdjustFeatureWIP, "true",
                AppSettingKeys.TeamSettingTags, "tag1,tag2",
                AppSettingKeys.TeamSettingSLEProbability, "88",
                AppSettingKeys.TeamSettingSLERange, "10",
                AppSettingKeys.TeamSettingBlockedStates, "Blocked,On Hold",
                AppSettingKeys.TeamSettingBlockedTags, "tag1,tag2"
                );

            var service = CreateService();

            var settings = service.GetDefaultTeamSettings();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(settings.Name, Is.EqualTo("MyTeam"));

                Assert.That(settings.ThroughputHistory, Is.EqualTo(90));
                Assert.That(settings.UseFixedDatesForThroughput, Is.False);

                var today = DateTime.Today;
                Assert.That(settings.ThroughputHistoryStartDate, Is.EqualTo(today.AddDays(-90)));
                Assert.That(settings.ThroughputHistoryEndDate, Is.EqualTo(today));

                Assert.That(settings.FeatureWIP, Is.EqualTo(2));
                Assert.That(settings.WorkItemQuery, Is.EqualTo("[System.TeamProject] = \"MyProject\""));
                Assert.That(settings.ParentOverrideField, Is.EqualTo("Custom.RemoteParentID"));
                Assert.That(settings.AutomaticallyAdjustFeatureWIP, Is.True);

                Assert.That(settings.WorkItemTypes, Has.Count.EqualTo(2));
                Assert.That(settings.WorkItemTypes, Does.Contain("Product Backlog Item"));
                Assert.That(settings.WorkItemTypes, Does.Contain("Bug"));

                Assert.That(settings.ToDoStates, Has.Count.EqualTo(2));
                Assert.That(settings.ToDoStates, Does.Contain("New"));
                Assert.That(settings.ToDoStates, Does.Contain("Planned"));

                Assert.That(settings.DoingStates, Has.Count.EqualTo(2));
                Assert.That(settings.DoingStates, Does.Contain("In Progress"));
                Assert.That(settings.DoingStates, Does.Contain("Committed"));

                Assert.That(settings.DoneStates, Has.Count.EqualTo(2));
                Assert.That(settings.DoneStates, Does.Contain("Done"));
                Assert.That(settings.DoneStates, Does.Contain("Closed"));

                Assert.That(settings.Tags, Has.Count.EqualTo(2));
                Assert.That(settings.Tags, Does.Contain("tag1"));
                Assert.That(settings.Tags, Does.Contain("tag2"));

                Assert.That(settings.ServiceLevelExpectationProbability, Is.EqualTo(88));
                Assert.That(settings.ServiceLevelExpectationRange, Is.EqualTo(10));

                Assert.That(settings.BlockedStates, Has.Count.EqualTo(2));
                Assert.That(settings.BlockedStates, Does.Contain("Blocked"));
                Assert.That(settings.BlockedStates, Does.Contain("On Hold"));
                Assert.That(settings.BlockedTags, Has.Count.EqualTo(2));
                Assert.That(settings.BlockedTags, Does.Contain("tag1"));
                Assert.That(settings.BlockedTags, Does.Contain("tag2"));
                Assert.That(settings.DoneItemsCutoffDays, Is.EqualTo(180));
            }
        }

        [Test]
        public void GetDefaultProjectSettings_ReturnsCorrectSettings()
        {
            SetupRepositoryForKeys(
                AppSettingKeys.ProjectSettingName, "My Project",
                AppSettingKeys.ProjectSettingWorkItemQuery, "[System.TeamProject] = \"MyProject\"",
                AppSettingKeys.ProjectSettingWorkItemTypes, "Epic",
                AppSettingKeys.ProjectSettingToDoStates, "New,Planned",
                AppSettingKeys.ProjectSettingDoingStates, "In Progress,Committed",
                AppSettingKeys.ProjectSettingDoneStates, "Closed,Done",
                AppSettingKeys.ProjectSettingOverrideRealChildCountStates, "New,Proposed",
                AppSettingKeys.ProjectSettingUnparentedWorkItemQuery, "[System.TeamProject] = \"MyProject\"",
                AppSettingKeys.ProjectSettingUsePercentileToCalculateDefaultAmountOfWorkItems, "True",
                AppSettingKeys.ProjectSettingDefaultAmountOfWorkItemsPerFeature, "15",
                AppSettingKeys.ProjectSettingDefaultWorkItemPercentile, "85",
                AppSettingKeys.ProjectSettingPercentileHistoryInDays, "55",
                AppSettingKeys.ProjectSettingSizeEstimateField, "Microsoft.VSTS.Scheduling.Size",
                AppSettingKeys.ProjectSettingsFeatureOwnerField, "System.AreaPath",
                AppSettingKeys.ProjectSettingTags, "tag1,tag2",
                AppSettingKeys.ProjectSettingSLEProbability, "88",
                AppSettingKeys.ProjectSettingSLERange, "10",
                AppSettingKeys.ProjectSettingParentOverrideField, "customfield_10923123",
                AppSettingKeys.ProjectSettingBlockedStates, "Blocked,On Hold",
                AppSettingKeys.ProjectSettingBlockedTags, "tag1,tag2"
                );

            var service = CreateService();

            var settings = service.GetDefaultProjectSettings();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(settings.Name, Is.EqualTo("My Project"));
                Assert.That(settings.WorkItemQuery, Is.EqualTo("[System.TeamProject] = \"MyProject\""));
                Assert.That(settings.UnparentedItemsQuery, Is.EqualTo("[System.TeamProject] = \"MyProject\""));
                Assert.That(settings.UsePercentileToCalculateDefaultAmountOfWorkItems, Is.True);
                Assert.That(settings.DefaultAmountOfWorkItemsPerFeature, Is.EqualTo(15));
                Assert.That(settings.DefaultWorkItemPercentile, Is.EqualTo(85));
                Assert.That(settings.PercentileHistoryInDays, Is.EqualTo(55));
                Assert.That(settings.SizeEstimateField, Is.EqualTo("Microsoft.VSTS.Scheduling.Size"));
                Assert.That(settings.FeatureOwnerField, Is.EqualTo("System.AreaPath"));

                Assert.That(settings.WorkItemTypes, Has.Count.EqualTo(1));
                Assert.That(settings.WorkItemTypes, Does.Contain("Epic"));

                Assert.That(settings.ToDoStates, Has.Count.EqualTo(2));
                Assert.That(settings.ToDoStates, Does.Contain("New"));
                Assert.That(settings.ToDoStates, Does.Contain("Planned"));

                Assert.That(settings.DoingStates, Has.Count.EqualTo(2));
                Assert.That(settings.DoingStates, Does.Contain("In Progress"));
                Assert.That(settings.DoingStates, Does.Contain("Committed"));

                Assert.That(settings.DoneStates, Has.Count.EqualTo(2));
                Assert.That(settings.DoneStates, Does.Contain("Done"));
                Assert.That(settings.DoneStates, Does.Contain("Closed"));

                Assert.That(settings.OverrideRealChildCountStates, Has.Count.EqualTo(2));
                Assert.That(settings.OverrideRealChildCountStates, Does.Contain("New"));
                Assert.That(settings.OverrideRealChildCountStates, Does.Contain("Proposed"));

                Assert.That(settings.Tags, Has.Count.EqualTo(2));
                Assert.That(settings.Tags, Does.Contain("tag1"));
                Assert.That(settings.Tags, Does.Contain("tag2"));

                Assert.That(settings.ServiceLevelExpectationProbability, Is.EqualTo(88));
                Assert.That(settings.ServiceLevelExpectationRange, Is.EqualTo(10));

                Assert.That(settings.ParentOverrideField, Is.EqualTo("customfield_10923123"));

                Assert.That(settings.BlockedStates, Has.Count.EqualTo(2));
                Assert.That(settings.BlockedStates, Does.Contain("Blocked"));
                Assert.That(settings.BlockedStates, Does.Contain("On Hold"));

                Assert.That(settings.BlockedTags, Has.Count.EqualTo(2));
                Assert.That(settings.BlockedTags, Does.Contain("tag1"));
                Assert.That(settings.BlockedTags, Does.Contain("tag2"));

                Assert.That(settings.DoneItemsCutoffDays, Is.EqualTo(365));
            }
        }

        [Test]
        public void GetSettingByKey_KeyDoesNotExist_ThrowsException()
        {
            repositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<AppSetting, bool>>())).Returns((AppSetting)null);

            var service = CreateService();

            Assert.Throws<ArgumentNullException>(() => service.GetFeaturRefreshSettings());
        }

        [Test]
        public void GetWorkTrackingSystemSettings_ReturnsCorrectSettings()
        {
            SetupRepositoryForKeys(
                AppSettingKeys.WorkTrackingSystemSettingsOverrideRequestTimeout, "True",
                AppSettingKeys.WorkTrackingSystemSettingsRequestTimeoutInSeconds, "300");

            var service = CreateService();

            var settings = service.GetWorkTrackingSystemSettings();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(settings.OverrideRequestTimeout, Is.True);
                Assert.That(settings.RequestTimeoutInSeconds, Is.EqualTo(300));
            }
        }

        [Test]
        public async Task UpdateWorkTrackingSystemSettings_UpdatesCorrectlyAsync()
        {
            SetupRepositoryForKeys(
                AppSettingKeys.WorkTrackingSystemSettingsOverrideRequestTimeout, "False",
                AppSettingKeys.WorkTrackingSystemSettingsRequestTimeoutInSeconds, "120");

            var service = CreateService();

            var workTrackingSystemSettings = new WorkTrackingSystemSettings { OverrideRequestTimeout = true, RequestTimeoutInSeconds = 300 };
            await service.UpdateWorkTrackingSystemSettings(workTrackingSystemSettings);

            VerifyUpdateCalled(AppSettingKeys.WorkTrackingSystemSettingsOverrideRequestTimeout, "True");
            VerifyUpdateCalled(AppSettingKeys.WorkTrackingSystemSettingsRequestTimeoutInSeconds, "300");
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