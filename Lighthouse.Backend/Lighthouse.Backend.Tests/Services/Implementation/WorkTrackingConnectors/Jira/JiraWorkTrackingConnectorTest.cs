using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    [Category("Integration")]
    public class JiraWorkTrackingConnectorTest
    {
        private Mock<ILexoRankService> lexoRankServiceMock;

        [SetUp]
        public void Setup()
        {
            lexoRankServiceMock = new Mock<ILexoRankService>();
        }

        [Test]
        public async Task GetWorkItemsForTeam_GetsAllItemsThatMatchQuery()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = PROJ AND labels = ExistingLabel");

            team.ResetUpdateTime();

            var matchingItems = await subject.GetWorkItemsForTeam(team);

            Assert.That(matchingItems.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task GetWorkItemsForTeam_OrCaseInWorkItemQuery_HandlesCorrectly()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = PROJ OR project = DUMMY");

            team.UseFixedDatesForThroughput = true;
            team.ThroughputHistoryStartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            team.ThroughputHistoryEndDate = new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc);

            var closedItems = await subject.GetWorkItemsForTeam(team);

            Assert.That(closedItems.Count, Is.EqualTo(18));
        }

        [Test]
        [TestCase("PROJ-18", "")]
        [TestCase("PROJ-15", "PROJ-8")]
        public async Task GetWorkItemsForTeam_SetsParentRelationCorrect(string issueKey, string expectedParentReference)
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = PROJ AND issueKey = {issueKey}");

            var workItems = await subject.GetWorkItemsForTeam(team);
            var workItem = workItems.Single(wi => wi.ReferenceId == issueKey);

            Assert.That(workItem.ParentReferenceId, Is.EqualTo(expectedParentReference));
        }

        [Test]
        public async Task GetWorkItemsForTeam_UseParentOverride_SetsParentRelationCorrect()
        {
            var subject = CreateSubject();
            var team = CreateTeam("project = LGHTHSDMO AND labels = NoProperParentLink AND issuekey = LGHTHSDMO-1726");
            team.AdditionalRelatedField = "cf[10038]";

            var workItems = await subject.GetWorkItemsForTeam(team);
            var workItem = workItems.Single(wi => wi.ReferenceId == "LGHTHSDMO-1726");

            Assert.That(workItem.ParentReferenceId, Is.EqualTo("LGHTHSDMO-1724"));
        }

        [Test]
        public async Task GetWorkItemsForTeam_QueryContainsAmpersand_EscapesCorrectly()
        {
            var subject = CreateSubject();
            var team = CreateTeam("project = LGHTHSDMO");

            team.ToDoStates.Clear();
            team.DoneStates.Clear();
            team.DoingStates.Clear();
            team.DoingStates.Add("QA&Testing");

            var workItems = await subject.GetWorkItemsForTeam(team);

            Assert.That(workItems.ToList(), Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetFeaturesForProject_LabelDoesNotExist_ReturnsNoItems()
        {
            var subject = CreateSubject();
            var project = CreateProject($"project = PROJ AND labels = \"NotExistingLabel\"");

            project.WorkItemTypes.Clear();
            project.WorkItemTypes.Add("Story");

            var itemsByTag = await subject.GetFeaturesForProject(project);

            Assert.That(itemsByTag, Is.Empty);
        }

        [Test]
        public async Task GetFeaturesForProject_ReturnsCorrectAmountOfItems()
        {
            var subject = CreateSubject();
            var project = CreateProject($"project = \"LGHTHSDMO\" AND labels = \"Phoenix\"");

            project.WorkItemTypes.Clear();
            project.WorkItemTypes.Add("Epic");

            var itemsByTag = await subject.GetFeaturesForProject(project);

            Assert.That(itemsByTag, Has.Count.EqualTo(4));
        }


        [Test]
        public async Task GetFeaturesForProject_TagExists_ReturnsCorrectNumberOfItems()
        {
            var subject = CreateSubject();
            var project = CreateProject($"project = PROJ AND labels = \"ExistingLabel\"");

            project.WorkItemTypes.Clear();
            project.WorkItemTypes.Add("Story");

            var itemsByTag = await subject.GetFeaturesForProject(project);

            Assert.That(itemsByTag, Has.Count.EqualTo(2));
        }

        [Test]
        public async Task GetFeaturesForProject_TagExists_WorkItemTypeDoesNotMatch_ReturnsNoItems()
        {
            var subject = CreateSubject();
            var project = CreateProject($"project = PROJ AND labels = \"ExistingLabel\"");

            project.WorkItemTypes.Clear();
            project.WorkItemTypes.Add("Bug");

            var itemsByTag = await subject.GetFeaturesForProject(project);

            Assert.That(itemsByTag, Is.Empty);
        }

        [Test]
        public async Task GetFeaturesForProject_ItemIsClosed_ReturnsItem()
        {
            var subject = CreateSubject();
            var project = CreateProject($"project = PROJ AND labels = \"LabelOfItemThatIsClosed\"");

            project.WorkItemTypes.Clear();
            project.WorkItemTypes.Add("Story");

            var actualItems = await subject.GetFeaturesForProject(project);

            Assert.That(actualItems, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetFeaturesForProject_ReturnsCorrectFeatureProperties()
        {
            var subject = CreateSubject();
            var project = CreateProject($"project = PROJ AND issueKey = PROJ-18");

            project.WorkItemTypes.Clear();
            project.WorkItemTypes.Add("Story");

            var features = await subject.GetFeaturesForProject(project);
            var feature = features.Single(f => f.ReferenceId == "PROJ-18");

            Assert.Multiple(() =>
            {
                Assert.That(feature.Name, Is.EqualTo("Test 32523"));
                Assert.That(feature.Order, Is.EqualTo("0|i00037:9"));
                Assert.That(feature.State, Is.EqualTo("In Progress"));
                Assert.That(feature.StateCategory, Is.EqualTo(StateCategories.Doing));
                Assert.That(feature.Url, Is.EqualTo("https://letpeoplework.atlassian.net/browse/PROJ-18"));
            });
        }

        [Test]
        public async Task GetFeaturesForProject_ReturnsCorrectStartedDateBasedOnStateMapping()
        {
            var subject = CreateSubject();
            var project = CreateProject($"project = PROJ AND issueKey = PROJ-21");

            var features = await subject.GetFeaturesForProject(project);
            var feature = features.Single(f => f.ReferenceId == "PROJ-21");

            Assert.Multiple(() =>
            {
                Assert.That(feature.StartedDate.HasValue, Is.True);
                Assert.That(feature.StartedDate?.Date, Is.EqualTo(new DateTime(2025, 4, 5, 0, 0, 0, DateTimeKind.Utc)));

                Assert.That(feature.ClosedDate.HasValue, Is.False);
            });
        }

        [Test]
        public async Task GetFeaturesForProject_ReturnsCorrectClosedDateBasedOnStateMapping()
        {
            var subject = CreateSubject();
            var project = CreateProject($"project = PROJ AND issueKey = PROJ-21");
            project.DoingStates.Remove("In Progress");
            project.DoneStates.Add("In Progress");

            var features = await subject.GetFeaturesForProject(project);
            var feature = features.Single(f => f.ReferenceId == "PROJ-21");

            Assert.Multiple(() =>
            {
                Assert.That(feature.ClosedDate.HasValue, Is.True);
                Assert.That(feature.ClosedDate?.Date, Is.EqualTo(new DateTime(2025, 4, 5, 0, 0, 0, DateTimeKind.Utc)));
            });
        }

        [Test]
        public async Task GetFeaturesForProject_ClosedDateButNoStartedDate_SetsStartedDateToClosedDate()
        {
            var subject = CreateSubject();
            var project = CreateProject($"project = PROJ AND issueKey = PROJ-21");
            project.DoingStates.Clear();
            project.DoneStates.Clear();
            project.DoneStates.Add("In Progress");

            var features = await subject.GetFeaturesForProject(project);
            var feature = features.Single(f => f.ReferenceId == "PROJ-21");

            Assert.Multiple(() =>
            {
                Assert.That(feature.StartedDate, Is.EqualTo(feature.ClosedDate));
            });
        }

        [Test]
        [TestCase("", "LGHTHSDMO-9", 0)]
        [TestCase("MambooJamboo", "LGHTHSDMO-9", 0)]
        [TestCase("customfield_10037", "LGHTHSDMO-9", 12)]
        [TestCase("customfield_10037", "LGHTHSDMO-1724", 0)]
        [TestCase("customfield_10037", "LGHTHSDMO-8", 2)]
        public async Task GetFeaturesForProject_ReadsEstimatedSizeCorrect(string fieldName, string issueKey, int expectedEstimatedSize)
        {
            var subject = CreateSubject();

            var project = CreateProject($"project = LGHTHSDMO AND issuekey = {issueKey}");
            project.SizeEstimateField = fieldName;

            var features = await subject.GetFeaturesForProject(project);
            var feature = features.Single(f => f.ReferenceId == issueKey);

            Assert.That(feature.EstimatedSize, Is.EqualTo(expectedEstimatedSize));
        }

        [Test]
        [TestCase("", "LGHTHSDMO-9", "")]
        [TestCase("MambooJamboo", "LGHTHSDMO-9", "")]
        [TestCase("customfield_10037", "LGHTHSDMO-9", "12.0")]
        [TestCase("fixVersions", "LGHTHSDMO-9", "Elixir Project")]
        [TestCase("labels", "LGHTHSDMO-5", "Phoenix")]
        [TestCase("labels", "LGHTHSDMO-5", "RebelRevolt")]
        public async Task GetFeaturesForProject_ReadsFeatureOwnerFieldCorrect(string fieldName, string issueKey, string expectedFeatureOwnerFieldValue)
        {
            var subject = CreateSubject();

            var project = CreateProject($"project = LGHTHSDMO AND issuekey = {issueKey}");
            project.FeatureOwnerField = fieldName;

            var features = await subject.GetFeaturesForProject(project);
            var feature = features.Single(f => f.ReferenceId == issueKey);
            
            Assert.That(feature.OwningTeam, Contains.Substring(expectedFeatureOwnerFieldValue));
        }

        [Test]
        public async Task GetWorkItemsIdsForTeamWithAdditionalQuery_IncludesClosedItems()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = PROJ");
            team.WorkItemTypes.Clear();
            team.WorkItemTypes.AddRange(["Story", "Bug"]);

            var totalItems = await subject.GetWorkItemsIdsForTeamWithAdditionalQuery(team, "labels = \"LabelOfItemThatIsClosed\"");

            Assert.That(totalItems, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetWorkItemsIdsForTeamWithAdditionalQuery_IgnoresItemsOfNotMatchingWorkItemType()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = PROJ");
            team.WorkItemTypes.Clear();
            team.WorkItemTypes.AddRange(["Bug"]);

            var totalItems = await subject.GetWorkItemsIdsForTeamWithAdditionalQuery(team, "labels = \"ExistingLabel\"");

            Assert.That(totalItems, Has.Count.EqualTo(0));
        }

        [Test]
        public async Task GetWorkItemsIdsForTeamWithAdditionalQuery_IncludesItemsThatMatchBothTeamAndCustomQuery()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = \"LGHTHSDMO\" AND labels = \"Lagunitas\"");
            team.WorkItemTypes.Clear();
            team.WorkItemTypes.AddRange(["Story", "Bug"]);

            var totalItems = await subject.GetWorkItemsIdsForTeamWithAdditionalQuery(team, "project = \"LGHTHSDMO\" AND labels = \"Oberon\"");

            Assert.That(totalItems, Has.Count.EqualTo(2));
        }

        [Test]
        public async Task GetWorkItemsIdsForTeamWithAdditionalQuery_IncludesToDoAndDoneItems()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = \"LGHTHSDMO\" AND labels = \"Lagunitas\"");
            team.WorkItemTypes.Clear();
            team.WorkItemTypes.AddRange(["Story", "Bug"]);

            var totalItems = await subject.GetWorkItemsIdsForTeamWithAdditionalQuery(team, "project = \"LGHTHSDMO\" AND fixVersion = \"Elixir Project\"");

            Assert.That(totalItems, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetWorkItemsIdsForTeamWithAdditionalQuery_IgnoresItemsThatMatchCustomBotNotTeamTeamQuery()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = \"LGHTHSDMO\" AND labels = \"RebelRevolt\"");
            team.WorkItemTypes.Clear();
            team.WorkItemTypes.AddRange(["Story", "Bug"]);

            var totalItems = await subject.GetWorkItemsIdsForTeamWithAdditionalQuery(team, "project = \"LGHTHSDMO\" AND labels = \"Oberon\"");

            Assert.That(totalItems, Has.Count.EqualTo(0));
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
            var username = Environment.GetEnvironmentVariable("JiraLighthouseIntegrationTestUsername") ?? "atlassian.pushchair@huser-berta.com";
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
        [TestCase("https://letpeoplework.atlassian.net", "Yah-yah-yah, Coco Jamboo, yah-yah-yeh", "atlassian.pushchair@huser-berta.com")]
        [TestCase("https://letpeoplework.atlassian.net", "", "atlassian.pushchair@huser-berta.com")]
        [TestCase("https://letpeoplework.atlassian.net", "PATPATPAT", "")]
        [TestCase("", "PATPATPAT", "atlassian.pushchair@huser-berta.com")]
        [TestCase("https://not.valid", "PATPATPAT", "atlassian.pushchair@huser-berta.com")]
        [TestCase("asdfasdfasdfasdf", "PATPATPAT", "atlassian.pushchair@huser-berta.com")]
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
        [TestCase("project = LGHTHSDMO AND issueKey = LGHTHSDMO-11", true)]
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
        public async Task GetChildItemsForFeaturesInProject_GivenCorrectQuery_ReturnsCorrectNumberOfItems()
        {
            var subject = CreateSubject();
            var team = CreateTeam("project = LGHTHSDMO");
            var project = CreateProject("project = LGHTHSDMO", team);

            project.Features.Add(new Feature(team, 10));

            project.HistoricalFeaturesWorkItemQuery = "project = LGHTHSDMO";

            var childItems = await subject.GetHistoricalFeatureSize(project);

            Assert.That(childItems.Values, Is.EquivalentTo(new List<int> { 8, 7, 7, 5, 9, 8, 8, 11, 6, 8 }));
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
            var username = Environment.GetEnvironmentVariable("JiraLighthouseIntegrationTestUsername") ?? "atlassian.pushchair@huser-berta.com";
            var apiToken = Environment.GetEnvironmentVariable("JiraLighthouseIntegrationTestToken") ?? throw new NotSupportedException("Can run test only if Environment Variable 'JiraLighthouseIntegrationTestToken' is set!");

            var connectionSetting = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.Jira, Name = "Test Setting" };
            connectionSetting.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Url, Value = organizationUrl, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Username, Value = username, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.ApiToken, Value = apiToken, IsSecret = true },
                ]);

            return connectionSetting;
        }

        private JiraWorkTrackingConnector CreateSubject()
        {
            return new JiraWorkTrackingConnector(lexoRankServiceMock.Object, new IssueFactory(lexoRankServiceMock.Object, Mock.Of<ILogger<IssueFactory>>()), Mock.Of<ILogger<JiraWorkTrackingConnector>>(), new FakeCryptoService());
        }
    }
}
