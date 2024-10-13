using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkItemServices;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestHelpers;
using Lighthouse.Backend.WorkTracking;
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
        public async Task GetClosedWorkItemsForTeam_FullHistory_TestProject_ReturnsCorrectAmountOfItems()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = PROJ");

            var closedItems = await subject.GetClosedWorkItems(720, team);

            Assert.That(closedItems.Count, Is.EqualTo(720));
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

            var (name, rank, url) = await subject.GetWorkItemDetails("PROJ-18", team);

            Assert.Multiple(() =>
            {
                Assert.That(name, Is.EqualTo("Test 32523"));
                Assert.That(rank, Is.EqualTo("0|i00037:9"));
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

            var (remainingItems, totalItems) = await subject.GetWorkItemsByQuery(["Bug"], team, "labels = \"ExistingLabel\"");

            Assert.That(remainingItems, Has.Count.EqualTo(0));
        }

        [Test]
        public async Task GetOpenWorkItemsByQuery_IncludesItemsThatMatchBothTeamAndCustomQuery()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = \"LGHTHSDMO\" AND labels = \"Lagunitas\"");

            var (remainingItems, totalItems) = await subject.GetWorkItemsByQuery(["Story", "Bug"], team, "project = \"LGHTHSDMO\" AND labels = \"Oberon\"");

            Assert.That(remainingItems, Has.Count.EqualTo(2));
        }

        [Test]
        public async Task GetOpenWorkItemsByQuery_IgnoresItemsThatMatchCustomBotNotTeamTeamQuery()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = \"LGHTHSDMO\" AND labels = \"RebelRevolt\"");

            var (remainingItems, totalItems) = await subject.GetWorkItemsByQuery(["Story", "Bug"], team, "project = \"LGHTHSDMO\" AND labels = \"Oberon\"");

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

            var order = subject.GetAdjacentOrderIndex(Enumerable.Empty<string>(), relativeOrder);

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

            var connectionSetting = CreateWorkTrackingSystemConnection();
            team.WorkTrackingSystemConnection = connectionSetting;

            return team;
        }

        private Project CreateProject(string query)
        {
            var project = new Project
            {
                Name = "TestProject",
                WorkItemQuery = query,
            };

            project.WorkItemTypes.Clear();
            project.WorkItemTypes.Add("Epic");

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
