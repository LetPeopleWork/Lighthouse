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
        [TestCase(new[] { "Closed" }, 5)]
        [TestCase(new[] { "Closed", "Resolved" }, 6)]
        [TestCase(new[] { "Closed", "Resolved", "Active" }, 10)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S6561:Avoid using \"DateTime.Now\" for benchmarking or timing operations", Justification = "Not used for benchmarking here")]
        public async Task GetClosedWorkItemsForTeam_FullHistory_DynamicThroughout_TestProject_ReturnsCorrectAmountOfItems(string[] doneStates, int expectedItems)
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [System.Tags] NOT CONTAINS 'ThroughputIgnore'");
            team.DoneStates.Clear();
            team.DoneStates.AddRange(doneStates);

            var history = (DateTime.Now - new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Days;
            team.UseFixedDatesForThroughput = false;
            team.ThroughputHistory = history;

            var closedItems = await subject.GetThroughputForTeam(team);

            Assert.That(closedItems.Count, Is.EqualTo(team.GetThroughputSettings().NumberOfDays));
            Assert.That(closedItems.Sum(), Is.EqualTo(expectedItems));
        }
        [Test]
        [TestCase(new[] { "Closed" }, 5)]
        [TestCase(new[] { "Closed", "Resolved" }, 6)]
        [TestCase(new[] { "Closed", "Resolved", "Active" }, 10)]
        public async Task GetClosedWorkItemsForTeam_FullHistory_FixedThroughout_TestProject_ReturnsCorrectAmountOfItems(string[] doneStates, int expectedItems)
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [System.Tags] NOT CONTAINS 'ThroughputIgnore'");

            team.UseFixedDatesForThroughput = true;
            team.ThroughputHistoryStartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            team.ThroughputHistoryEndDate = DateTime.UtcNow;

            team.DoneStates.Clear();
            team.DoneStates.AddRange(doneStates);

            var closedItems = await subject.GetThroughputForTeam(team);

            Assert.That(closedItems.Count, Is.EqualTo(team.GetThroughputSettings().NumberOfDays));
            Assert.That(closedItems.Sum(), Is.EqualTo(expectedItems));
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
        public async Task GetRelatedItems_ItemIdIsParent_FindsRelation()
        {

            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");

            var (remainingItems, totalItems) = await subject.GetRelatedWorkItems("370", team);

            Assert.Multiple(() =>
            {
                Assert.That(remainingItems, Is.EqualTo(2));
                Assert.That(totalItems, Is.EqualTo(3));
            });
        }

        [Test]
        public async Task GetRelatedItems_ItemIdIsParent_AllWorkIsDone_ReturnsCorrectNumberOfRemainingAndTotalItems()
        {

            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");

            var (remainingItems, totalItems) = await subject.GetRelatedWorkItems("380", team);

            Assert.Multiple(() =>
            {
                Assert.That(remainingItems, Is.EqualTo(0));
                Assert.That(totalItems, Is.EqualTo(1));
            });
        }

        [Test]
        public async Task GetRelatedItems_ItemIdIsPartiallyMatching_DoesNotFindRelation()
        {

            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");

            var (relatedItems, _) = await subject.GetRelatedWorkItems("37", team);

            Assert.That(relatedItems, Is.EqualTo(0));
        }

        [Test]
        public async Task GetRelatedItems_ItemIdIsChild_DoesNotFindRelation()
        {

            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");
            team.WorkItemTypes.Add("Feature");

            var (relatedItems, _) = await subject.GetRelatedWorkItems("366", team);

            Assert.That(relatedItems, Is.EqualTo(0));
        }

        [Test]
        public async Task GetRelatedItems_ItemIdIsRemoteRelated_FindsRelation()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");
            team.AdditionalRelatedField = "Custom.RemoteFeatureID";

            var (relatedItems, _) = await subject.GetRelatedWorkItems("279", team);

            Assert.That(relatedItems, Is.EqualTo(1));
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
        public async Task GetWorkItemDetails_ReturnsTitleAndStackRank()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");

            var (name, rank, url, state, _, _) = await subject.GetWorkItemDetails("366", team);

            Assert.Multiple(() =>
            {
                Assert.That(name, Is.EqualTo("Test Test Test"));
                Assert.That(rank, Is.EqualTo("1999821120"));
                Assert.That(state, Is.EqualTo("Resolved"));
                Assert.That(url, Is.EqualTo("https://dev.azure.com/huserben/e7b3c1df-8d70-4943-98a7-ef00c7a0c523/_workitems/edit/366"));
            });
        }

        [Test]
        public async Task GetWorkItemDetails_ReturnsCorrectStartedDateBasedOnStateMapping()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");

            var (_, _, _, _, startedDate, closedDate) = await subject.GetWorkItemDetails("375", team);

            Assert.Multiple(() =>
            {
                Assert.That(startedDate.HasValue, Is.True);
                Assert.That(startedDate?.Date, Is.EqualTo(new DateTime(2025, 2, 26, 0, 0, 0, DateTimeKind.Utc)));

                Assert.That(closedDate.HasValue, Is.False);
            });
        }

        [Test]
        public async Task GetWorkItemDetails_ReturnsCorrectClosedDateBasedOnStateMapping()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");
            team.DoneStates.Add("Active");

            var (_, _, _, _, _, closedDate) = await subject.GetWorkItemDetails("375", team);

            Assert.Multiple(() =>
            {
                Assert.That(closedDate.HasValue, Is.True);
                Assert.That(closedDate?.Date, Is.EqualTo(new DateTime(2025, 2, 26, 0, 0, 0, DateTimeKind.Utc)));
            });
        }

        [Test]
        public async Task GetWorkItemDetails_ClosedDateButNoStartedDate_SetsStartedDateToClosedDate()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");
            team.DoingStates.Remove("Active");
            team.DoneStates.Add("Active");

            var (_, _, _, _, startedDate, closedDate) = await subject.GetWorkItemDetails("375", team);

            Assert.Multiple(() =>
            {
                Assert.That(startedDate, Is.EqualTo(closedDate));
            });
        }

        [Test]
        public async Task GetOpenWorkItemsByQuery_IgnoresClosedItems()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.AreaPath}] UNDER 'CMFTTestTeamProject\\PreviousReleaseAreaPath'");

            var (remainingItems, totalItems) = await subject.GetWorkItemsByQuery(["User Story", "Bug"], team, "[System.Tags] CONTAINS 'ThroughputIgnore'");

            Assert.Multiple(() =>
            {
                Assert.That(remainingItems, Has.Count.EqualTo(0));
                Assert.That(totalItems, Has.Count.EqualTo(1));
            });
        }

        [Test]
        public async Task GetOpenWorkItemsByQuery_IgnoresItemsOfNotMatchingWorkItemType()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.AreaPath}] UNDER 'CMFTTestTeamProject\\PreviousReleaseAreaPath'");

            var (remainingItems, _) = await subject.GetWorkItemsByQuery(["Bug"], team, "[System.Tags] CONTAINS 'Release1'");

            Assert.That(remainingItems, Has.Count.EqualTo(0));
        }

        [Test]
        public async Task IsRelatedToFeature_ItemHasNoRelation_ReturnsFalse()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");

            var isRelated = await subject.IsRelatedToFeature("365", ["370"], team);

            Assert.That(isRelated, Is.False);
        }

        [Test]
        public async Task IsRelatedToFeature_ItemIsChild_ReturnsTrue()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");

            var isRelated = await subject.IsRelatedToFeature("365", ["371"], team);

            Assert.That(isRelated, Is.True);
        }

        [Test]
        public async Task IsRelatedToFeature_ItemIsChild_MultipleFeatures_ReturnsTrue()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");

            var isRelated = await subject.IsRelatedToFeature("365", ["370", "371"], team);

            Assert.That(isRelated, Is.True);
        }

        [Test]
        public async Task IsRelatedToFeature_ItemIsRemoteRelatedViaCustomField_ReturnsTrue()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");
            team.AdditionalRelatedField = "Custom.RemoteFeatureID";

            var isRelated = await subject.IsRelatedToFeature("375", ["279"], team);

            Assert.That(isRelated, Is.True);
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
        [TestCase("")]
        [TestCase("MambooJamboo")]
        public async Task GetEstimatedSizeForItem_EstimateSizeFieldNotExists_Returns0(string fieldName)
        {
            var subject = CreateSubject();

            var project = CreateProject($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");
            project.SizeEstimateField = fieldName;

            var estimatedSize = await subject.GetEstimatedSizeForItem("370", project);

            Assert.That(estimatedSize, Is.EqualTo(0));
        }

        [Test]
        [TestCase("370", 12)]
        [TestCase("380", 0)]
        [TestCase("371", 2)]
        public async Task GetEstimatedSizeForItem_GivenExistingField_ReturnsCorrectValue(string referenceId, int expectedSize)
        {
            var subject = CreateSubject();

            var project = CreateProject($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");
            project.SizeEstimateField = "Microsoft.VSTS.Scheduling.Size";

            var estimatedSize = await subject.GetEstimatedSizeForItem(referenceId, project);

            Assert.That(estimatedSize, Is.EqualTo(expectedSize));
        }

        [Test]
        [TestCase("")]
        [TestCase("MambooJamboo")]
        public async Task GetFeatureOwnerByField_FeatureOwnerFieldDoesNotExist_ReturnsEmptyString(string fieldName)
        {
            var subject = CreateSubject();

            var project = CreateProject($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");
            project.FeatureOwnerField = fieldName;

            var featureOwnerFieldContent = await subject.GetFeatureOwnerByField("370", project);

            Assert.That(featureOwnerFieldContent, Is.Empty);
        }

        [Test]
        [TestCase("370", "Microsoft.VSTS.Scheduling.Size", "12")]
        [TestCase("377", "System.AreaPath", "CMFTTestTeamProject\\SomeReleeaseThatIsUsingAreaPaths")]
        [TestCase("370", "System.Tags", "Release1")]
        public async Task GetFeatureOwnerByField_GivenExistingField_ReturnsCorrectValue(string referenceId, string fieldName, string expectedValue)
        {
            var subject = CreateSubject();

            var project = CreateProject($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");
            project.FeatureOwnerField = fieldName;

            var estimatedSize = await subject.GetFeatureOwnerByField(referenceId, project);

            Assert.That(estimatedSize, Is.EqualTo(expectedValue));
        }

        [Test]
        public async Task GetChildItemsForFeaturesInProject_GivenCorrectQuery_ReturnsCorrectNumberOfItems()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");

            var project = CreateProject($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'", team);

            project.Features.Add(new Feature(team, 10));

            project.HistoricalFeaturesWorkItemQuery = $"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'";

            var childItems = await subject.GetChildItemsForFeaturesInProject(project);

            Assert.That(new List<int> { 1, 3, 3 }, Is.EquivalentTo(childItems));
        }

        [Test]
        [TestCase("[System.TeamProject] = 'CMFTTestTeamProject'", 3)]
        [TestCase("[System.TeamProject] = 'CMFTTestTeamProject' AND [System.WorkItemType] = 'Bug'", 1)]
        [TestCase("[System.TeamProject] = 'CMFTTestTeamProject' AND [System.ID] = '377'", 1)]
        public async Task GetFeaturesInProgressForTeam_ReturnsCorrectAmount(string teamQuery, int expectedFeaturesInProgress)
        {
            var subject = CreateSubject();
            var team = CreateTeam(teamQuery);

            var featuresInProgress = (await subject.GetFeaturesInProgressForTeam(team)).ToList();

            Assert.That(featuresInProgress, Has.Count.EqualTo(expectedFeaturesInProgress));
        }

        [Test]
        public async Task GetFeaturesInProgressForTeam_FeatureLinkedViaCustomField_ReturnsCorrectAmount()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");
            team.AdditionalRelatedField = "Custom.RemoteFeatureID";

            var featuresInProgress = (await subject.GetFeaturesInProgressForTeam(team)).ToList();

            Assert.That(featuresInProgress, Has.Count.EqualTo(2));
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
