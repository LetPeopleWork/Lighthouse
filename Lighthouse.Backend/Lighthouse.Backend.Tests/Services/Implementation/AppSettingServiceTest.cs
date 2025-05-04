using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Services.Implementation;
using Moq;
using Lighthouse.Backend.API.DTO;
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

            Assert.Multiple(() =>
            {
                Assert.That(settings.Interval, Is.EqualTo(60));
                Assert.That(settings.RefreshAfter, Is.EqualTo(360));
                Assert.That(settings.StartDelay, Is.EqualTo(1));
            });
        }

        [Test]
        public void GetTeamDataRefreshSettings_ReturnsCorrectSettings()
        {
            SetupRepositoryForKeys(AppSettingKeys.TeamDataRefreshInterval, "30", AppSettingKeys.TeamDataRefreshAfter, "180", AppSettingKeys.TeamDataRefreshStartDelay, "2");

            var service = CreateService();

            var settings = service.GetTeamDataRefreshSettings();

            Assert.Multiple(() =>
            {
                Assert.That(settings.Interval, Is.EqualTo(30));
                Assert.That(settings.RefreshAfter, Is.EqualTo(180));
                Assert.That(settings.StartDelay, Is.EqualTo(2));
            });
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
                AppSettingKeys.TeamSettingRelationCustomField, "Custom.RemoteParentID",
                AppSettingKeys.TeamSettingAutomaticallyAdjustFeatureWIP, "true",
                AppSettingKeys.TeamSettingTags, "tag1,tag2"
                );

            var service = CreateService();

            var settings = service.GetDefaultTeamSettings();

            Assert.Multiple(() =>
            {
                Assert.That(settings.Name, Is.EqualTo("MyTeam"));                

                Assert.That(settings.ThroughputHistory, Is.EqualTo(90));
                Assert.That(settings.UseFixedDatesForThroughput, Is.False);

                var today = DateTime.Today;
                Assert.That(settings.ThroughputHistoryStartDate, Is.EqualTo(today.AddDays(-90)));
                Assert.That(settings.ThroughputHistoryEndDate, Is.EqualTo(today));
                
                Assert.That(settings.FeatureWIP, Is.EqualTo(2));
                Assert.That(settings.WorkItemQuery, Is.EqualTo("[System.TeamProject] = \"MyProject\""));
                Assert.That(settings.RelationCustomField, Is.EqualTo("Custom.RemoteParentID"));
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
            });
        }

        [Test]
        public async Task UpdateDefaultTeamSettingsAsync_UpdatesCorrectlyAsync()
        {
            SetupRepositoryForKeys(
                AppSettingKeys.TeamSettingName, "MyTeam",
                AppSettingKeys.TeamSettingHistory, "90",
                AppSettingKeys.TeamSettingFeatureWIP, "2",
                AppSettingKeys.TeamSettingToDoStates, "New,Planned",
                AppSettingKeys.TeamSettingDoingStates, "In Progress,Committed",
                AppSettingKeys.TeamSettingDoneStates, "Closed,Done",
                AppSettingKeys.TeamSettingWorkItemQuery, "[System.TeamProject] = \"MyProject\"",
                AppSettingKeys.TeamSettingWorkItemTypes, "Product Backlog Item, Bug",
                AppSettingKeys.TeamSettingRelationCustomField, "Custom.RemoteParentID",
                AppSettingKeys.TeamSettingAutomaticallyAdjustFeatureWIP, "False",
                AppSettingKeys.TeamSettingTags, "tag1,tag2"
                );

            var service = CreateService();

            var newSettings = new TeamSettingDto
            {
                Name = "Other Team",
                ThroughputHistory = 190,
                FeatureWIP = 3,
                ToDoStates = ["Backlog"],
                DoingStates = ["Ongoing"],
                DoneStates = ["Over"],
                WorkItemQuery = "project = MyJiraProject",
                WorkItemTypes = ["Task", "Spike"],
                RelationCustomField = "CUSTOM_12039213",
                AutomaticallyAdjustFeatureWIP = true,
                Tags = ["tag3", "tag4"]
            };

            await service.UpdateDefaultTeamSettings(newSettings);

            VerifyUpdateCalled(AppSettingKeys.TeamSettingName, "Other Team");
            VerifyUpdateCalled(AppSettingKeys.TeamSettingHistory, "190");
            VerifyUpdateCalled(AppSettingKeys.TeamSettingFeatureWIP, "3");
            VerifyUpdateCalled(AppSettingKeys.TeamSettingToDoStates, "Backlog");
            VerifyUpdateCalled(AppSettingKeys.TeamSettingDoingStates, "Ongoing");
            VerifyUpdateCalled(AppSettingKeys.TeamSettingDoneStates, "Over");
            VerifyUpdateCalled(AppSettingKeys.TeamSettingWorkItemQuery, "project = MyJiraProject");
            VerifyUpdateCalled(AppSettingKeys.TeamSettingWorkItemTypes, "Task,Spike");
            VerifyUpdateCalled(AppSettingKeys.TeamSettingRelationCustomField, "CUSTOM_12039213");
            VerifyUpdateCalled(AppSettingKeys.TeamSettingAutomaticallyAdjustFeatureWIP, "True");
            VerifyUpdateCalled(AppSettingKeys.TeamSettingTags, "tag3,tag4");
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
                AppSettingKeys.ProjectSettingHistoricalFeaturesWorkItemQuery, "[System.TeamProject] = \"MyProject\"",
                AppSettingKeys.ProjectSettingSizeEstimateField, "Microsoft.VSTS.Scheduling.Size",
                AppSettingKeys.ProjectSettingsFeatureOwnerField, "System.AreaPath",
                AppSettingKeys.ProjectSettingTags, "tag1,tag2"
                );

            var service = CreateService();

            var settings = service.GetDefaultProjectSettings();

            Assert.Multiple(() =>
            {
                Assert.That(settings.Name, Is.EqualTo("My Project"));
                Assert.That(settings.WorkItemQuery, Is.EqualTo("[System.TeamProject] = \"MyProject\""));
                Assert.That(settings.UnparentedItemsQuery, Is.EqualTo("[System.TeamProject] = \"MyProject\""));
                Assert.That(settings.UsePercentileToCalculateDefaultAmountOfWorkItems, Is.True);
                Assert.That(settings.DefaultAmountOfWorkItemsPerFeature, Is.EqualTo(15));
                Assert.That(settings.DefaultWorkItemPercentile, Is.EqualTo(85));
                Assert.That(settings.HistoricalFeaturesWorkItemQuery, Is.EqualTo("[System.TeamProject] = \"MyProject\""));
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
            });
        }

        [Test]
        public async Task UpdateDefaultProjectSettingsAsync_UpdatesCorrectlyAsync()
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
                AppSettingKeys.ProjectSettingUsePercentileToCalculateDefaultAmountOfWorkItems, "False",
                AppSettingKeys.ProjectSettingDefaultAmountOfWorkItemsPerFeature, "10",
                AppSettingKeys.ProjectSettingDefaultWorkItemPercentile, "85",
                AppSettingKeys.ProjectSettingHistoricalFeaturesWorkItemQuery, "[System.TeamProject] = \"MyProject\"",
                AppSettingKeys.ProjectSettingSizeEstimateField, "Microsoft.VSTS.Scheduling.Size",
                AppSettingKeys.ProjectSettingsFeatureOwnerField, "System.Tags",
                AppSettingKeys.ProjectSettingTags, "tag1,tag2"
                );

            var service = CreateService();

            var newSettings = new ProjectSettingDto
            {
                Name = "Other Project",
                WorkItemQuery = "project = MyJiraProject",
                WorkItemTypes = ["Feature"],
                ToDoStates = ["Backlog"],
                DoingStates = ["Ongoing"],
                DoneStates = ["Over"],
                OverrideRealChildCountStates = ["Backlog,Proposed"],
                UnparentedItemsQuery = "project = MyJiraProject",
                UsePercentileToCalculateDefaultAmountOfWorkItems = true,
                DefaultAmountOfWorkItemsPerFeature = 22,
                DefaultWorkItemPercentile = 75,
                HistoricalFeaturesWorkItemQuery = "project = MyJiraProject",
                SizeEstimateField = "customfield_10037",
                FeatureOwnerField = "labels",
                Tags = ["tag3", "tag4"]
            };

            await service.UpdateDefaultProjectSettings(newSettings);

            VerifyUpdateCalled(AppSettingKeys.ProjectSettingName, "Other Project");
            VerifyUpdateCalled(AppSettingKeys.ProjectSettingWorkItemQuery, "project = MyJiraProject");
            VerifyUpdateCalled(AppSettingKeys.ProjectSettingWorkItemTypes, "Feature");
            VerifyUpdateCalled(AppSettingKeys.ProjectSettingToDoStates, "Backlog");
            VerifyUpdateCalled(AppSettingKeys.ProjectSettingDoingStates, "Ongoing");
            VerifyUpdateCalled(AppSettingKeys.ProjectSettingDoneStates, "Over");
            VerifyUpdateCalled(AppSettingKeys.ProjectSettingOverrideRealChildCountStates, "Backlog,Proposed");
            VerifyUpdateCalled(AppSettingKeys.ProjectSettingUnparentedWorkItemQuery, "project = MyJiraProject");
            VerifyUpdateCalled(AppSettingKeys.ProjectSettingUsePercentileToCalculateDefaultAmountOfWorkItems, "True");
            VerifyUpdateCalled(AppSettingKeys.ProjectSettingDefaultAmountOfWorkItemsPerFeature, "22");
            VerifyUpdateCalled(AppSettingKeys.ProjectSettingDefaultWorkItemPercentile, "75");
            VerifyUpdateCalled(AppSettingKeys.ProjectSettingHistoricalFeaturesWorkItemQuery, "project = MyJiraProject");
            VerifyUpdateCalled(AppSettingKeys.ProjectSettingSizeEstimateField, "customfield_10037");
            VerifyUpdateCalled(AppSettingKeys.ProjectSettingsFeatureOwnerField, "labels");
            VerifyUpdateCalled(AppSettingKeys.ProjectSettingTags, "tag3,tag4");
        }

        [Test]
        public void GetSettingByKey_KeyDoesNotExist_ThrowsException()
        {
            repositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<AppSetting, bool>>())).Returns((AppSetting)null);

            var service = CreateService();

            Assert.Throws<ArgumentNullException>(() => service.GetFeaturRefreshSettings());
        }

        [Test]
        public void GetCleanUpDataHistorySettings_ReturnsCorrectSettings()
        {
            SetupRepositoryForKeys(AppSettingKeys.CleanUpDataHistorySettingsMaxStorageTimeInDays, "42");

            var service = CreateService();

            var settings = service.GetDataRetentionSettings();

            Assert.That(settings.MaxStorageTimeInDays, Is.EqualTo(42));
        }

        [Test]
        public async Task UpdateCleanUpDataHistorySettings_UpdatesCorrectlyAsync()
        {
            SetupRepositoryForKeys(AppSettingKeys.CleanUpDataHistorySettingsMaxStorageTimeInDays, "60");

            var service = CreateService();

            var cleanUpDataHistorySettings = new DataRetentionSettings { MaxStorageTimeInDays = 42 };
            await service.UpdateDataRetentionSettings(cleanUpDataHistorySettings);

            VerifyUpdateCalled(AppSettingKeys.CleanUpDataHistorySettingsMaxStorageTimeInDays, "42");
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