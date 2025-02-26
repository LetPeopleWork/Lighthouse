using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkItemServices;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestHelpers;
using Lighthouse.Backend.WorkTracking;
using Lighthouse.Backend.WorkTracking.AzureDevOps;
using Lighthouse.Backend.WorkTracking.Jira;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkItemServices
{
    [Category("Integration")]
    public class JiraWorkItemServiceTest
    {
        private Mock<ILexoRankService> lexoRankServiceMock;

        [SetUp]
        public void Setup()
        {
            lexoRankServiceMock = new Mock<ILexoRankService>();
        }

        [Test]
        public async Task GetClosedWorkItemsForTeam_FullHistory_DynamicThroughput_TestProject_ReturnsCorrectAmountOfItems()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = PROJ");

            var history = (DateTime.Now - new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Days;
            team.UseFixedDatesForThroughput = false;
            team.ThroughputHistory = history;

            var closedItems = await subject.GetThroughputForTeam(team);

            Assert.That(closedItems.Count, Is.EqualTo(team.GetThroughputSettings().NumberOfDays));
            Assert.That(closedItems.Sum(), Is.EqualTo(6));
        }

        [Test]
        public async Task GetClosedWorkItemsForTeam_FullHistory_FixedThroughput_TestProject_ReturnsCorrectAmountOfItems()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = PROJ");

            team.UseFixedDatesForThroughput = true;
            team.ThroughputHistoryStartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            team.ThroughputHistoryEndDate = DateTime.Now;

            var closedItems = await subject.GetThroughputForTeam(team);

            Assert.That(closedItems.Count, Is.EqualTo(team.GetThroughputSettings().NumberOfDays));
            Assert.That(closedItems.Sum(), Is.EqualTo(6));
        }

        [Test]
        public async Task GetWorkItemsByLabel_LabelDoesNotExist_ReturnsNoItems()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = PROJ AND labels = \"NotExistingLabel\"");

            var itemsByTag = await subject.GetOpenWorkItems(["Story"], team);

            Assert.That(itemsByTag, Is.Empty);
        }

        [Test]
        public async Task GetWorkItemsByLabel_ReturnsCorrectAmountOfItems()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = \"LGHTHSDMO\" AND labels = \"Phoenix\"");

            var itemsByTag = await subject.GetOpenWorkItems(["Epic"], team);

            Assert.That(itemsByTag, Has.Count.EqualTo(3));
        }


        [Test]
        public async Task GetWorkItemsByTag_TagExists_ReturnsCorrectNumberOfItems()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = PROJ AND labels = \"ExistingLabel\"");

            var itemsByTag = await subject.GetOpenWorkItems(["Story"], team);

            Assert.That(itemsByTag, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetWorkItemsByTag_TagExists_WorkItemTypeDoesNotMatch_ReturnsNoItems()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = PROJ AND labels = \"ExistingLabel\"");

            var itemsByTag = await subject.GetOpenWorkItems(["Bug"], team);

            Assert.That(itemsByTag, Is.Empty);
        }

        [Test]
        public async Task GetRelatedItems_ItemIdIsParent_FindsRelation()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = PROJ");

            var (remainingItems, totalItems) = await subject.GetRelatedWorkItems("PROJ-9", team);
            Assert.Multiple(() =>
            {
                Assert.That(remainingItems, Is.EqualTo(1));
                Assert.That(totalItems, Is.EqualTo(4));
            });
        }

        [Test]
        public async Task GetRelatedItems_ItemIdIsParent_HasOpenAndClosedItems_ReturnsCorrectNumber()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = LGHTHSDMO");

            var (remainingItems, totalItems) = await subject.GetRelatedWorkItems("LGHTHSDMO-9", team);
            Assert.Multiple(() =>
            {
                Assert.That(remainingItems, Is.EqualTo(8));
                Assert.That(totalItems, Is.EqualTo(11));
            });
        }

        [Test]
        [TestCase("project = LGHTHSDMO", 7)]
        [TestCase("project = LGHTHSDMO and key = LGHTHSDMO-1116", 1)]
        [TestCase("project = LGHTHSDMO and issuetype = Story", 7)]
        [TestCase("project = LGHTHSDMO and labels IN (Phoenix)", 1)]
        [TestCase("project = LGHTHSDMO and labels IN (Phoenix, RebelRevolt)", 3)]
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
            var team = CreateTeam("project = LGHTHSDMO AND labels = NoProperParentLink");
            team.AdditionalRelatedField = "cf[10038]";

            var featuresInProgress = (await subject.GetFeaturesInProgressForTeam(team)).ToList();

            Assert.That(featuresInProgress, Has.Count.EqualTo(1));
        }

        [Test]
        [TestCase(new string[] { "Story" }, 2, 3)]
        [TestCase(new string[] { "Task" }, 2, 3)]
        [TestCase(new string[] { "Bug" }, 1, 2)]
        [TestCase(new string[] { "Story", "Bug" }, 3, 5)]
        [TestCase(new string[] { "Task", "Bug" }, 3, 5)]
        [TestCase(new string[] { "Story", "Task" }, 4, 6)]
        [TestCase(new string[] { "Story", "Task", "Bug" }, 5, 8)]
        public async Task GetRelatedItems_HasDifferentTypes_ReturnsCorrectNumber(string[] workItemTypes, int expectedRemainingItems, int expectedTotalItems)
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = LGHTHSDMO AND labels IN (IntegrationTest)");
            team.WorkItemTypes.Clear();

            foreach ( var workItemType in workItemTypes)
            {
                team.WorkItemTypes.Add(workItemType);
            }

            var (remainingItems, totalItems) = await subject.GetRelatedWorkItems("LGHTHSDMO-1041", team);

            Assert.Multiple(() =>
            {
                Assert.That(remainingItems, Is.EqualTo(expectedRemainingItems));
                Assert.That(totalItems, Is.EqualTo(expectedTotalItems));
            });
        }

        [Test]
        public async Task GetRelatedItems_ItemIdIsParent_OnlyClosedItems_ReturnsCorrectNumber()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = PROJ");

            var (remainingItems, totalItems) = await subject.GetRelatedWorkItems("PROJ-21", team);
            Assert.Multiple(() =>
            {
                Assert.That(remainingItems, Is.EqualTo(0));
                Assert.That(totalItems, Is.EqualTo(1));
            });
        }

        [Test]
        public async Task GetRelatedItems_ItemIdIsChild_DoesNotFindRelation()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = PROJ");

            var (remainingItems, totalItems) = await subject.GetRelatedWorkItems("PROJ-6", team);
            Assert.Multiple(() =>
            {
                Assert.That(remainingItems, Is.EqualTo(0));
                Assert.That(totalItems, Is.EqualTo(0));
            });
        }

        [Test]
        public async Task GetRelatedItems_ItemIdIsRemoteRelated_FindsRelation()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = LGHTHSDMO");
            team.AdditionalRelatedField = "cf[10038]";

            var (relatedItems, totalItems) = await subject.GetRelatedWorkItems("LGHTHSDMO-1724", team);

            Assert.Multiple(() =>
            {
                Assert.That(relatedItems, Is.EqualTo(2));
                Assert.That(totalItems, Is.EqualTo(3));
            });
        }

        [Test]
        public async Task GetOpenWorkItemsByTag_ItemIsClosed_ReturnsEmpty()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = PROJ AND labels = \"LabelOfItemThatIsClosed\"");

            var actualItems = await subject.GetOpenWorkItems(["Story"], team);

            Assert.That(actualItems, Has.Count.EqualTo(0));
        }

        [Test]
        public async Task GetWorkItemDetails_ReturnsTitleAndStackRank()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = PROJ");

            var (name, rank, url, state) = await subject.GetWorkItemDetails("PROJ-18", team);

            Assert.Multiple(() =>
            {
                Assert.That(name, Is.EqualTo("Test 32523"));
                Assert.That(rank, Is.EqualTo("0|i00037:9"));
                Assert.That(state, Is.EqualTo("In Progress"));
                Assert.That(url, Is.EqualTo("https://letpeoplework.atlassian.net/browse/PROJ-18"));
            });
        }

        [Test]
        public async Task GetOpenWorkItemsByQuery_IgnoresClosedItems()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = PROJ");

            var (remainingItems, totalItems) = await subject.GetWorkItemsByQuery(["Story", "Bug"], team, "labels = \"LabelOfItemThatIsClosed\"");
            
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
            var team = CreateTeam($"project = PROJ");
            var (remainingItems, _) = await subject.GetWorkItemsByQuery(["Bug"], team, "labels = \"ExistingLabel\"");

            Assert.That(remainingItems, Has.Count.EqualTo(0));
        }

        [Test]
        public async Task GetOpenWorkItemsByQuery_IncludesItemsThatMatchBothTeamAndCustomQuery()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = \"LGHTHSDMO\" AND labels = \"Lagunitas\"");

            var (remainingItems, _) = await subject.GetWorkItemsByQuery(["Story", "Bug"], team, "project = \"LGHTHSDMO\" AND labels = \"Oberon\"");

            Assert.That(remainingItems, Has.Count.EqualTo(2));
        }

        [Test]
        public async Task GetOpenWorkItemsByQuery_IncludesToDoAndDoneItems()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = \"LGHTHSDMO\" AND labels = \"Lagunitas\"");

            var (remainingItems, totalItems) = await subject.GetWorkItemsByQuery(["Story", "Bug"], team, "project = \"LGHTHSDMO\" AND fixVersion = \"Elixir Project\"");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(remainingItems, Has.Count.EqualTo(1));
                Assert.That(totalItems, Has.Count.EqualTo(1));
            }
        }

        [Test]
        public async Task GetOpenWorkItemsByQuery_IgnoresItemsThatMatchCustomBotNotTeamTeamQuery()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = \"LGHTHSDMO\" AND labels = \"RebelRevolt\"");

            var (remainingItems, _) = await subject.GetWorkItemsByQuery(["Story", "Bug"], team, "project = \"LGHTHSDMO\" AND labels = \"Oberon\"");

            Assert.That(remainingItems, Has.Count.EqualTo(0));
        }

        [Test]
        public async Task IsRelatedToFeature_ItemHasNoRelation_ReturnsFalse()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = PROJ");

            var isRelated = await subject.IsRelatedToFeature("PROJ-18", ["PROJ-8"], team);

            Assert.That(isRelated, Is.False);
        }

        [Test]
        public async Task IsRelatedToFeature_ItemIsChild_ReturnsTrue()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = PROJ");

            var isRelated = await subject.IsRelatedToFeature("PROJ-15", ["PROJ-8"], team);

            Assert.That(isRelated, Is.True);
        }

        [Test]
        public async Task IsRelatedToFeature_ItemIsChild_MultipleFeatures_ReturnsTrue()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = PROJ");

            var isRelated = await subject.IsRelatedToFeature("PROJ-15", ["PROJ-9", "PROJ-8"], team);

            Assert.That(isRelated, Is.True);
        }

        [Test]
        [TestCase(RelativeOrder.Above)]
        [TestCase(RelativeOrder.Below)]
        public void GetAdjacentOrderIndex_NoFeaturesPassed_ReturnsDefault(RelativeOrder relativeOrder)
        {
            var subject = CreateSubject();
            var expectedOrder = "00000|";

            lexoRankServiceMock.Setup(x => x.Default).Returns(expectedOrder);

            var order = subject.GetAdjacentOrderIndex([], relativeOrder);

            Assert.That(order, Is.EqualTo(expectedOrder));
        }

        [Test]
        public void GetAdjacentOrderIndex_OrderIsAbove_ReturnsHigherOrder()
        {
            var subject = CreateSubject();
            var itemsOrder = new[] { "0|i000v3:", "0|i001v3:", "0|i000v2:"};
            var expectedOrder = "0|i001v5:";

            lexoRankServiceMock.Setup(x => x.GetHigherPriority("0|i001v3:")).Returns(expectedOrder);

            var order = subject.GetAdjacentOrderIndex(itemsOrder, RelativeOrder.Above);

            Assert.That(order, Is.EqualTo(expectedOrder));
        }

        [Test]
        public void GetAdjacentOrderIndex_OrderIsBelow_ReturnsLowestOrder()
        {
            var subject = CreateSubject();
            var itemsOrder = new[] { "0|i000v3:", "0|i001v3:", "0|i000v2:"};
            var expectedOrder = "0|i000v1:";

            lexoRankServiceMock.Setup(x => x.GetLowerPriority("0|i000v2:")).Returns(expectedOrder);

            var order = subject.GetAdjacentOrderIndex(itemsOrder, RelativeOrder.Below);

            Assert.That(order, Is.EqualTo(expectedOrder));
        }

        [Test]
        public async Task ValidateConnection_GivenValidSettings_ReturnsTrue()
        {
            var subject = CreateSubject();

            var organizationUrl = "https://letpeoplework.atlassian.net";
            var username = "benjhuser@gmail.com";
            var apiToken = Environment.GetEnvironmentVariable("JiraLighthouseIntegrationTestToken") ?? throw new NotSupportedException("Can run test only if Environment Variable 'JiraLighthouseIntegrationTestToken' is set!");

            var connectionSetting = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.Jira, Name = "Test Setting" };
            connectionSetting.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Url, Value = organizationUrl, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Username, Value = username, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.ApiToken, Value = apiToken, IsSecret = true },
                ]);

            var isValid = await subject.ValidateConnection(connectionSetting);

            Assert.That(isValid, Is.True);
        }

        [Test]
        [TestCase("https://letpeoplework.atlassian.net", "Yah-yah-yah, Coco Jamboo, yah-yah-yeh", "benjhuser@gmail.com")]
        [TestCase("https://letpeoplework.atlassian.net", "", "benjhuser@gmail.com")]
        [TestCase("https://letpeoplework.atlassian.net", "PATPATPAT", "")]
        [TestCase("", "PATPATPAT", "benjhuser@gmail.com")]
        [TestCase("https://not.valid", "PATPATPAT", "benjhuser@gmail.com")]
        [TestCase("asdfasdfasdfasdf", "PATPATPAT", "benjhuser@gmail.com")]
        public async Task ValidateConnection_GivenInvalidSettings_ReturnsFalse(string organizationUrl, string apiToken, string username)
        {
            var subject = CreateSubject();

            var connectionSetting = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.Jira, Name = "Test Setting" };
            connectionSetting.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Url, Value = organizationUrl, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Username, Value = username, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.ApiToken, Value = apiToken, IsSecret = true },
                ]);

            var isValid = await subject.ValidateConnection(connectionSetting);

            Assert.That(isValid, Is.False);
        }

        [Test]
        [TestCase("project = LGHTHSDMO", true)]
        [TestCase("project = LGHTHSDMO AND labels = 'NotExisting'", false)]
        [TestCase("project = SomethingThatDoesNotExist", false)]
        public async Task ValidateTeamSettings_ValidConnectionSettings_ReturnsTrueIfTeamHasThroughput(string query, bool expectedValue)
        {
            var team = CreateTeam(query);

            var subject = CreateSubject();

            var isValid = await subject.ValidateTeamSettings(team);

            Assert.That(isValid, Is.EqualTo(expectedValue));
        }

        [Test]
        public async Task ValidateTeamSettings_InvalidConnectionSettings_ReturnsFalse()
        {
            var subject = CreateSubject();
            var team = CreateTeam("project = LGHTHSDMO");

            var connectionSetting = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.Jira, Name = "Test Setting" };
            connectionSetting.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Url, Value = "https://letpeoplework.atlassian.net", IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Username, Value = "Benji", IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.ApiToken, Value = "JennifferAniston", IsSecret = true },
                ]);

            team.WorkTrackingSystemConnection = connectionSetting;

            var isValid = await subject.ValidateTeamSettings(team);

            Assert.That(isValid, Is.False);
        }

        [Test]
        [TestCase("project = LGHTHSDMO", true)]
        [TestCase("project = LGHTHSDMO AND labels = 'NotExisting'", false)]
        [TestCase("project = SomethingThatDoesNotExist", false)]
        public async Task ValidateProjectSettings_ValidConnectionSettings_ReturnsTrueIfFeaturesAreFound(string query, bool expectedValue)
        {
            var team = CreateTeam("project = LGHTHSDMO");
            var project = CreateProject(query, team);

            var subject = CreateSubject();

            var isValid = await subject.ValidateProjectSettings(project);

            Assert.That(isValid, Is.EqualTo(expectedValue));
        }

        [Test]
        public async Task ValidateProjectSettings_InvalidConnectionSettings_ReturnsFalse()
        {
            var project = CreateProject("project = LGHTHSDMO");
            var subject = CreateSubject();

            var connectionSetting = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.AzureDevOps, Name = "Test Setting" };
            connectionSetting.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Url, Value = "https://letpeoplework.atlassian.net", IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Username, Value = "Benji", IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.ApiToken, Value = "JennifferAniston", IsSecret = true },
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

            var project = CreateProject("project = LGHTHSDMO");
            project.SizeEstimateField = fieldName;

            var estimatedSize = await subject.GetEstimatedSizeForItem("LGHTHSDMO-9", project);

            Assert.That(estimatedSize, Is.EqualTo(0));
        }

        [Test]
        [TestCase("LGHTHSDMO-9", 12)]
        [TestCase("LGHTHSDMO-10", 0)]
        [TestCase("LGHTHSDMO-8", 2)]
        public async Task GetEstimatedSizeForItem_GivenExistingField_ReturnsCorrectValue(string referenceId, int expectedSize)
        {
            var subject = CreateSubject();

            var project = CreateProject("project = LGHTHSDMO");
            project.SizeEstimateField = "customfield_10037";

            var estimatedSize = await subject.GetEstimatedSizeForItem(referenceId, project);

            Assert.That(estimatedSize, Is.EqualTo(expectedSize));
        }

        [Test]
        [TestCase("")]
        [TestCase("MambooJamboo")]
        public async Task GetFeatureOwnerByField_FeatureOwnerFieldDoesNotExist_ReturnsEmptyString(string fieldName)
        {
            var subject = CreateSubject();

            var project = CreateProject("project = LGHTHSDMO");
            project.FeatureOwnerField = fieldName;

            var featureOwnerFieldContent = await subject.GetFeatureOwnerByField("LGHTHSDMO-9", project);

            Assert.That(featureOwnerFieldContent, Is.Empty);
        }

        [Test]
        [TestCase("LGHTHSDMO-9", "customfield_10037", "12.0")]
        [TestCase("LGHTHSDMO-9", "fixVersions", "Elixir Project")]
        [TestCase("LGHTHSDMO-1393", "labels", "Brownies")]
        [TestCase("LGHTHSDMO-5", "labels", "Phoenix")]
        [TestCase("LGHTHSDMO-5", "labels", "RebelRevolt")]
        public async Task GetFeatureOwnerByField_GivenExistingField_ReturnsCorrectValue(string referenceId, string fieldName, string expectedContent)
        {
            var subject = CreateSubject();

            var project = CreateProject("project = LGHTHSDMO");
            project.FeatureOwnerField = fieldName;

            var featureOwnerFieldContent = await subject.GetFeatureOwnerByField(referenceId, project);

            Assert.That(featureOwnerFieldContent, Contains.Substring(expectedContent));
        }

        [Test]
        public async Task GetChildItemsForFeaturesInProject_GivenCorrectQuery_ReturnsCorrectNumberOfItems()
        {
            var subject = CreateSubject();
            var team = CreateTeam("project = LGHTHSDMO");
            var project = CreateProject("project = LGHTHSDMO", team);

            project.Features.Add(new Feature(team, 10));

            project.HistoricalFeaturesWorkItemQuery = "project = LGHTHSDMO";

            var childItems = await subject.GetChildItemsForFeaturesInProject(project);

            Assert.That(childItems, Is.EquivalentTo(new List<int> { 8, 7, 7, 5, 9, 8, 8, 11, 6, 8 }));
        }

        private Team CreateTeam(string query)
        {
            var team = new Team
            {
                Name = "TestTeam",
                WorkItemQuery = query
            };

            team.WorkItemTypes.Clear();
            team.WorkItemTypes.Add("Story");
            team.WorkItemTypes.Add("Bug");
            team.WorkItemTypes.Add("Task");

            team.DoneStates.Clear();
            team.DoneStates.Add("Done");

            team.DoingStates.Clear();
            team.DoingStates.Add("In Progress");

            team.ToDoStates.Clear();
            team.ToDoStates.Add("To Do");

            var connectionSetting = CreateWorkTrackingSystemConnection();
            team.WorkTrackingSystemConnection = connectionSetting;

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
            project.WorkItemTypes.Add("Epic");

            project.DoneStates.Clear();
            project.DoneStates.Add("Done");

            project.DoingStates.Clear();
            project.DoingStates.Add("In Progress");

            project.ToDoStates.Clear();
            project.ToDoStates.Add("To Do");

            project.UpdateTeams(teams);

            var workTrackingSystemConnection = CreateWorkTrackingSystemConnection();
            project.WorkTrackingSystemConnection = workTrackingSystemConnection;

            return project;
        }

        private WorkTrackingSystemConnection CreateWorkTrackingSystemConnection()
        {
            var organizationUrl = "https://letpeoplework.atlassian.net";
            var username = "benjhuser@gmail.com";
            var apiToken = Environment.GetEnvironmentVariable("JiraLighthouseIntegrationTestToken") ?? throw new NotSupportedException("Can run test only if Environment Variable 'JiraLighthouseIntegrationTestToken' is set!");

            var connectionSetting = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.Jira, Name = "Test Setting" };
            connectionSetting.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Url, Value = organizationUrl, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Username, Value = username, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.ApiToken, Value = apiToken, IsSecret = true },
                ]);

            return connectionSetting;
        }

        private JiraWorkItemService CreateSubject()
        {
            return new JiraWorkItemService(lexoRankServiceMock.Object, new IssueFactory(lexoRankServiceMock.Object, Mock.Of<ILogger<IssueFactory>>()), Mock.Of<ILogger<JiraWorkItemService>>(), new FakeCryptoService());
        }
    }
}
