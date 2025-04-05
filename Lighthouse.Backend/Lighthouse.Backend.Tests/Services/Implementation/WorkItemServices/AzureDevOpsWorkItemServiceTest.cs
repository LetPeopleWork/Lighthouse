using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkItemServices;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestHelpers;
using Lighthouse.Backend.WorkTracking;
using Lighthouse.Backend.WorkTracking.AzureDevOps;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkItemServices
{
    [Category("Integration")]
    public class AzureDevOpsWorkItemServiceTest
    {
        [Test]
        public async Task GetChangedWorkItemsSinceLastTeamUpdate_NoUpdateDone_GetsAllItemsThatMatchQuery()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [System.Title] CONTAINS 'Unparented' AND [System.State] != 'Closed'");

            team.ResetUpdateTime();

            var matchingItems = await subject.GetChangedWorkItemsSinceLastTeamUpdate(team);

            Assert.That(matchingItems.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task GetChangedWorkItemsSinceLastTeamUpdate_UpdateDone_NoChangedItems_DoesNotFindNewItems()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [System.Title] CONTAINS 'Unparented' AND [System.State] != 'Closed'");

            team.TeamUpdateTime = DateTime.UtcNow.AddDays(+1);

            var matchingItems = await subject.GetChangedWorkItemsSinceLastTeamUpdate(team);

            Assert.That(matchingItems.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task GetChangedWorkItemsSinceLastTeamUpdate_UpdateDone_ReturnsOnlyChangedItems()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [System.Title] CONTAINS 'Unparented' AND [System.State] != 'Closed'");

            team.TeamUpdateTime = new DateTime(2024, 10, 17, 0, 0, 0, DateTimeKind.Utc);

            var matchingItems = await subject.GetChangedWorkItemsSinceLastTeamUpdate(team);

            Assert.That(matchingItems.Count, Is.EqualTo(1));
        }

        [Test]
        [TestCase("377", "", null)]
        [TestCase("365", "371", null)]
        [TestCase("375", "279", "Custom.RemoteFeatureID")]
        public async Task GetChangedWorkItemsSinceLastTeamUpdate_SetsParentRelationCorrect(string workItemId, string expectedParentReference, string? parentOverrideField)
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Id}] = '{workItemId}'");
            team.AdditionalRelatedField = parentOverrideField;

            var workItems = await subject.GetChangedWorkItemsSinceLastTeamUpdate(team);
            var workItem = workItems.Single(wi => wi.ReferenceId == workItemId);

            Assert.That(workItem.ParentReferenceId, Is.EqualTo(expectedParentReference));
        }

        [Test]
        public async Task GetChangedWorkItemsSinceLastTeamUpdate_OrCaseInWorkItemQuery_HandlesCorrectly()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' OR [{AzureDevOpsFieldNames.TeamProject}] = 'DummyProject'");
            team.DoneStates.Clear();
            team.DoneStates.Add("Closed");
            team.UseFixedDatesForThroughput = true;
            team.ThroughputHistoryStartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            team.ThroughputHistoryEndDate = new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc);

            var result = await subject.GetChangedWorkItemsSinceLastTeamUpdate(team);

            Assert.That(result.Count, Is.EqualTo(17));
        }

        [Test]
        public async Task GetFeaturesForProject_TagDoesNotExist_ReturnsNoItems()
        {
            var subject = CreateSubject();
            var project = CreateProject($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Tags}] CONTAINS 'NotExistingTag'");

            var itemsByTag = await subject.GetFeaturesForProject(project);

            Assert.That(itemsByTag, Is.Empty);
        }

        [Test]
        public async Task GetFeaturesForProject_TagExists_ReturnsCorrectNumberOfItems()
        {
            var subject = CreateSubject();
            var project = CreateProject($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Tags}] CONTAINS 'Release1'");

            var itemsByTag = await subject.GetFeaturesForProject(project);

            Assert.That(itemsByTag, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetFeaturesForProject_TagExists_WorkItemTypeDoesNotMatch_ReturnsNoItems()
        {
            var subject = CreateSubject();
            var project = CreateProject($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Tags}] CONTAINS 'Release1'");

            project.WorkItemTypes.Clear();
            project.WorkItemTypes.Add("Bug");

            var itemsByTag = await subject.GetFeaturesForProject(project);

            Assert.That(itemsByTag, Is.Empty);
        }

        [Test]
        public async Task GetFeaturesForProject_AreaPathDoesNotExist_ReturnsNoItems()
        {
            var subject = CreateSubject();
            var project = CreateProject($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.AreaPath}] UNDER 'CMFTTestTeamProject\\NotExistingAreaPath'");

            var itemsByTag = await subject.GetFeaturesForProject(project);

            Assert.That(itemsByTag, Is.Empty);
        }

        [Test]
        public async Task GetFeaturesForProject_AreaPathExists_ReturnsCorrectNumberOfItems()
        {
            var subject = CreateSubject();
            var project = CreateProject($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.AreaPath}] UNDER 'CMFTTestTeamProject\\SomeReleeaseThatIsUsingAreaPaths'");

            project.WorkItemTypes.Clear();
            project.WorkItemTypes.Add("User Story");

            var itemsByTag = await subject.GetFeaturesForProject(project);

            Assert.That(itemsByTag, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetFeaturesForProject_AreaPathExists_WorkItemTypeDoesNotMatch_ReturnsNoItems()
        {
            var subject = CreateSubject();
            var project = CreateProject($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.AreaPath}] UNDER 'CMFTTestTeamProject\\SomeReleeaseThatIsUsingAreaPaths'");

            project.WorkItemTypes.Clear();
            project.WorkItemTypes.Add("Bug");

            var itemsByTag = await subject.GetFeaturesForProject(project);

            Assert.That(itemsByTag, Is.Empty);
        }

        [Test]
        public async Task GetFeaturesForProjectByTag_ItemIsOpen_ReturnsItem()
        {
            var subject = CreateSubject();
            var project = CreateProject($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Tags}] CONTAINS 'Release1'");

            project.WorkItemTypes.Clear();
            project.WorkItemTypes.Add("User Story");

            var actualItems = await subject.GetFeaturesForProject(project);

            Assert.That(actualItems, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetFeaturesForProjectByTag_ItemIsClosed_ReturnsItem()
        {
            var subject = CreateSubject();
            var project = CreateProject($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Tags}] CONTAINS 'PreviousRelease'");

            project.WorkItemTypes.Clear();
            project.WorkItemTypes.Add("User Story");

            var actualItems = await subject.GetFeaturesForProject(project);

            Assert.That(actualItems, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetFeaturesForProjectByAreaPath_ItemIsOpen_ReturnsItem()
        {
            var subject = CreateSubject();
            var project = CreateProject($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.AreaPath}] UNDER 'CMFTTestTeamProject\\SomeReleeaseThatIsUsingAreaPaths'");

            project.WorkItemTypes.Clear();
            project.WorkItemTypes.Add("User Story");

            var actualItems = await subject.GetFeaturesForProject(project);

            Assert.That(actualItems, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetFeaturesForProjectByAreaPath_ItemIsClosed_ReturnsItem()
        {
            var subject = CreateSubject();
            var project = CreateProject($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.AreaPath}] UNDER 'CMFTTestTeamProject\\PreviousReleaseAreaPath'");

            project.WorkItemTypes.Clear();
            project.WorkItemTypes.Add("User Story");

            var actualItems = await subject.GetFeaturesForProject(project);

            Assert.That(actualItems, Has.Count.EqualTo(2));
        }

        [Test]
        public async Task GetFeaturesForProject_ReturnsCorrectFeatureProperties()
        {
            var subject = CreateSubject();
            var project = CreateProject($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Id}] = '366'");
            project.WorkItemTypes.Clear();
            project.WorkItemTypes.Add("User Story");

            var features = await subject.GetFeaturesForProject(project);
            var feature = features.Single(f => f.ReferenceId == "366");

            Assert.Multiple(() =>
            {
                Assert.That(feature.Name, Is.EqualTo("Test Test Test"));
                Assert.That(feature.Order, Is.EqualTo("1999821120"));
                Assert.That(feature.State, Is.EqualTo("Resolved"));
                Assert.That(feature.StateCategory, Is.EqualTo(StateCategories.Doing));
                Assert.That(feature.Url, Is.EqualTo("https://dev.azure.com/huserben/e7b3c1df-8d70-4943-98a7-ef00c7a0c523/_workitems/edit/366"));
            });
        }

        [Test]
        public async Task GetFeaturesForProject_ReturnsCorrectStartedDateBasedOnStateMapping()
        {
            var subject = CreateSubject();
            var project = CreateProject($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Id}] = '370'");

            var features = await subject.GetFeaturesForProject(project);
            var feature = features.Single(f => f.ReferenceId == "370");

            Assert.Multiple(() =>
            {
                Assert.That(feature.StartedDate.HasValue, Is.True);
                Assert.That(feature.StartedDate?.Date, Is.EqualTo(new DateTime(2024, 2, 16, 0, 0, 0, DateTimeKind.Utc)));

                Assert.That(feature.ClosedDate.HasValue, Is.False);
            });
        }

        [Test]
        public async Task GetFeaturesForProject_ReturnsCorrectClosedDateBasedOnStateMapping()
        {
            var subject = CreateSubject();
            var project = CreateProject($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Id}] = '370'");
            project.DoneStates.Add("Resolved");

            var features = await subject.GetFeaturesForProject(project);
            var feature = features.Single(f => f.ReferenceId == "370");

            Assert.Multiple(() =>
            {
                Assert.That(feature.ClosedDate.HasValue, Is.True);
                Assert.That(feature.ClosedDate?.Date, Is.EqualTo(new DateTime(2024, 2, 16, 0, 0, 0, DateTimeKind.Utc)));
            });
        }

        [Test]
        public async Task GetFeaturesForProject_ClosedDateButNoStartedDate_SetsStartedDateToClosedDate()
        {
            var subject = CreateSubject();
            var project = CreateProject($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Id}] = '370'");
            project.DoingStates.Remove("Resolved");
            project.DoneStates.Add("Resolved");

            var features = await subject.GetFeaturesForProject(project);
            var feature = features.Single(f => f.ReferenceId == "370");

            Assert.Multiple(() =>
            {
                Assert.That(feature.StartedDate, Is.EqualTo(feature.ClosedDate));
            });
        }

        [Test]
        [TestCase("", "370", 0)]
        [TestCase("MambooJamboo", "370", 0)]
        [TestCase("Microsoft.VSTS.Scheduling.Size", "370", 12)]
        [TestCase("Microsoft.VSTS.Scheduling.Size", "380", 0)]
        [TestCase("Microsoft.VSTS.Scheduling.Size", "371", 2)]
        public async Task GetFeaturesForProject_ReadsEstimatedSizeCorrect(string fieldName, string workItemId, int expectedEstimatedSize)
        {
            var subject = CreateSubject();

            var project = CreateProject($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Id}] = '{workItemId}'");
            project.SizeEstimateField = fieldName;

            var features = await subject.GetFeaturesForProject(project);
            var feature = features.Single(f => f.ReferenceId == workItemId);

            Assert.That(feature.EstimatedSize, Is.EqualTo(expectedEstimatedSize));
        }

        [Test]
        [TestCase("", "370", "")]
        [TestCase("MambooJamboo", "370", "")]
        [TestCase("Microsoft.VSTS.Scheduling.Size", "370", "12")]
        [TestCase("System.AreaPath", "370", "CMFTTestTeamProject")]
        [TestCase("System.Tags", "370", "Release1")]
        public async Task GetFeaturesForProject_ReadsFeatureOwnerFieldCorrect(string fieldName, string workItemId, string expectedFeatureOwnerFieldValue)
        {
            var subject = CreateSubject();

            var project = CreateProject($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Id}] = '{workItemId}'");
            project.FeatureOwnerField = fieldName;

            var features = await subject.GetFeaturesForProject(project);
            var feature = features.Single(f => f.ReferenceId == workItemId);

            Assert.That(feature.OwningTeam, Is.EqualTo(expectedFeatureOwnerFieldValue));
        }

        [Test]
        public async Task GetWorkItemsIdsForTeamWithAdditionalQuery_IncludesClosedItems()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.AreaPath}] UNDER 'CMFTTestTeamProject\\PreviousReleaseAreaPath'");

            var totalItems = await subject.GetWorkItemsIdsForTeamWithAdditionalQuery(team, "[System.Tags] CONTAINS 'ThroughputIgnore'");
            
            Assert.That(totalItems, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetWorkItemsIdsForTeamWithAdditionalQuery_IgnoresItemsOfNotMatchingWorkItemType()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.AreaPath}] UNDER 'CMFTTestTeamProject\\PreviousReleaseAreaPath'");
            team.WorkItemTypes.Clear();
            team.WorkItemTypes.Add("Bug");

            var totalItems = await subject.GetWorkItemsIdsForTeamWithAdditionalQuery(team, "[System.Tags] CONTAINS 'Release1'");

            Assert.That(totalItems, Has.Count.EqualTo(0));
        }

        [Test]
        [TestCase(RelativeOrder.Above)]
        [TestCase(RelativeOrder.Below)]
        public void GetAdjacentOrderIndex_NoFeaturesPassed_Returns0(RelativeOrder relativeOrder)
        {
            var subject = CreateSubject();

            var order = subject.GetAdjacentOrderIndex([], relativeOrder);

            Assert.That(order, Is.EqualTo("0"));
        }

        [Test]
        [TestCase(new[] { "1" }, RelativeOrder.Above, "2")]
        [TestCase(new[] { "2" }, RelativeOrder.Above, "3")]
        [TestCase(new[] { "1", "2" }, RelativeOrder.Above, "3")]
        [TestCase(new[] { "2", "1" }, RelativeOrder.Above, "3")]
        [TestCase(new[] { "2", "3", "1" }, RelativeOrder.Above, "4")]
        [TestCase(new[] { "2", "1", "test" }, RelativeOrder.Above, "3")]
        [TestCase(new[] { "1" }, RelativeOrder.Below, "0")]
        [TestCase(new[] { "2" }, RelativeOrder.Below, "1")]
        [TestCase(new[] { "1", "2" }, RelativeOrder.Below, "0")]
        [TestCase(new[] { "2", "1" }, RelativeOrder.Below, "0")]
        [TestCase(new[] { "2", "1", "3" }, RelativeOrder.Below, "0")]
        [TestCase(new[] { "2", "1", "test" }, RelativeOrder.Below, "0")]
        public void GetAdjacentOrderIndex_ReturnsCorrectOrder(string[] existingItemsOrder, RelativeOrder relativeOrder, string expectedResult)
        {
            var subject = CreateSubject();

            var order = subject.GetAdjacentOrderIndex(existingItemsOrder, relativeOrder);

            Assert.That(order, Is.EqualTo(expectedResult));
        }

        [Test]
        public async Task ValidateConnection_GivenValidSettings_ReturnsTrue()
        {
            var subject = CreateSubject();

            var organizationUrl = "https://dev.azure.com/huserben";
            var personalAccessToken = Environment.GetEnvironmentVariable("AzureDevOpsLighthouseIntegrationTestToken") ?? throw new NotSupportedException("Can run test only if Environment Variable 'AzureDevOpsLighthouseIntegrationTestToken' is set!");

            var connectionSetting = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.AzureDevOps, Name = "Test Setting" };
            connectionSetting.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.Url, Value = organizationUrl, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken, Value = personalAccessToken, IsSecret = true },
                ]);

            var isValid = await subject.ValidateConnection(connectionSetting);

            Assert.That(isValid, Is.True);
        }

        [Test]
        [TestCase("https://dev.azure.com/huserben", "Yah-yah-yah, Coco Jamboo, yah-yah-yeh")]
        [TestCase("https://dev.azure.com/huserben", "")]
        [TestCase("", "PATPATPAT")]
        [TestCase("https://not.valid", "PATPATPAT")]
        [TestCase("asdfasdfasdfasdf", "PATPATPAT")]
        public async Task ValidateConnection_GivenInvalidSettings_ReturnsFalse(string organizationUrl, string personalAccessToken)
        {
            var subject = CreateSubject();

            var connectionSetting = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.AzureDevOps, Name = "Test Setting" };
            connectionSetting.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.Url, Value = organizationUrl, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken, Value = personalAccessToken, IsSecret = true },
                ]);

            var isValid = await subject.ValidateConnection(connectionSetting);

            Assert.That(isValid, Is.False);
        }

        [Test]
        [TestCase("[System.TeamProject] = 'CMFTTestTeamProject'", true)]
        [TestCase("[System.TeamProject] = 'CMFTTestTeamProject' AND [System.Tags] CONTAINS 'NotExistingTag'", false)]
        [TestCase("[System.TeamProject] = 'SomethingThatDoesNotExist'", false)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S6561:Avoid using \"DateTime.Now\" for benchmarking or timing operations", Justification = "Not used for benchmarking here")]
        public async Task ValidateTeamSettings_ValidConnectionSettings_ReturnsTrueIfTeamHasThroughput(string query, bool expectedValue)
        {
            var history = (DateTime.Now - new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Days;

            var team = CreateTeam(query);
            team.ThroughputHistory = history;

            var subject = CreateSubject();

            var isValid = await subject.ValidateTeamSettings(team);

            Assert.That(isValid, Is.EqualTo(expectedValue));
        }

        [Test]
        public async Task ValidateTeamSettings_InvalidConnectionSettings_ReturnsFalse()
        {
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");
            var subject = CreateSubject();

            var connectionSetting = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.AzureDevOps, Name = "Test Setting" };
            connectionSetting.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.Url, Value = "https://dev.azure.com/huserben", IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken, Value = "dsakjflasdkjflasdkfjlaskdjflskdjfa", IsSecret = true },
                ]);

            team.WorkTrackingSystemConnection = connectionSetting;

            var isValid = await subject.ValidateTeamSettings(team);

            Assert.That(isValid, Is.False);
        }

        [Test]
        [TestCase("[System.TeamProject] = 'CMFTTestTeamProject'", true)]
        [TestCase("[System.TeamProject] = 'CMFTTestTeamProject' AND [System.Tags] CONTAINS 'NotExistingTag'", false)]
        [TestCase("[System.TeamProject] = 'SomethingThatDoesNotExist'", false)]
        public async Task ValidateProjectSettings_ValidConnectionSettings_ReturnsTrueIfFeaturesAreFound(string query, bool expectedValue)
        {
            var team = CreateTeam("[System.TeamProject] = 'CMFTTestTeamProject'");
            var project = CreateProject(query, team);

            var subject = CreateSubject();

            var isValid = await subject.ValidateProjectSettings(project);

            Assert.That(isValid, Is.EqualTo(expectedValue));
        }

        [Test]
        public async Task ValidateProjectSettings_InvalidConnectionSettings_ReturnsFalse()
        {
            _ = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");
            var project = CreateProject("[System.TeamProject] = 'CMFTTestTeamProject'");
            var subject = CreateSubject();

            var connectionSetting = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.AzureDevOps, Name = "Test Setting" };
            connectionSetting.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.Url, Value = "https://dev.azure.com/huserben", IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken, Value = "dsakjflasdkjflasdkfjlaskdjflskdjfa", IsSecret = true },
                ]);

            project.WorkTrackingSystemConnection = connectionSetting;

            var isValid = await subject.ValidateProjectSettings(project);

            Assert.That(isValid, Is.False);
        }

        [Test]
        public async Task GetChildItemsForFeaturesInProject_GivenCorrectQuery_ReturnsCorrectNumberOfItems()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");

            var project = CreateProject($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'", team);

            project.Features.Add(new Feature(team, 10));

            project.HistoricalFeaturesWorkItemQuery = $"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'";

            var childItems = await subject.GetHistoricalFeatureSize(project);

            Assert.That(new List<int> { 1, 3, 3 }, Is.EquivalentTo(childItems.Values));
        }

        private Team CreateTeam(string query)
        {
            var team = new Team
            {
                Name = "TestTeam",
                WorkItemQuery = query
            };

            var workTrackingSystemConnection = CreateWorkTrackingSystemConnection();
            team.WorkTrackingSystemConnection = workTrackingSystemConnection;

            return team;
        }

        private Project CreateProject(string query, params Team[] teams)
        {
            var project = new Project
            {
                Name = "TestProject",
                WorkItemQuery = query,
            };

            project.WorkItemTypes.Clear();
            project.WorkItemTypes.Add("Feature");

            var workTrackingSystemConnection = CreateWorkTrackingSystemConnection();
            project.WorkTrackingSystemConnection = workTrackingSystemConnection;

            project.UpdateTeams(teams);

            return project;
        }

        private WorkTrackingSystemConnection CreateWorkTrackingSystemConnection()
        {
            var organizationUrl = "https://dev.azure.com/huserben";
            var personalAccessToken = Environment.GetEnvironmentVariable("AzureDevOpsLighthouseIntegrationTestToken") ?? throw new NotSupportedException("Can run test only if Environment Variable 'AzureDevOpsLighthouseIntegrationTestToken' is set!");

            var connectionSetting = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.AzureDevOps, Name = "Test Setting" };
            connectionSetting.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.Url, Value = organizationUrl, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken, Value = personalAccessToken, IsSecret = true },
                ]);

            return connectionSetting;
        }

        private static AzureDevOpsWorkItemService CreateSubject()
        {
            return new AzureDevOpsWorkItemService(Mock.Of<ILogger<AzureDevOpsWorkItemService>>(), new FakeCryptoService());
        }
    }
}