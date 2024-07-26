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

            var relatedItems = await subject.GetRemainingRelatedWorkItems("PROJ-9", team);

            Assert.That(relatedItems, Is.EqualTo(1));
        }

        [Test]
        public async Task GetRelatedItems_ItemIdIsChild_DoesNotFindRelation()
        {

            var subject = CreateSubject();
            var team = CreateTeam($"project = PROJ");

            var relatedItems = await subject.GetRemainingRelatedWorkItems("PROJ-6", team);

            Assert.That(relatedItems, Is.EqualTo(0));
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

            var (name, rank) = await subject.GetWorkItemDetails("PROJ-18", team);

            Assert.Multiple(() =>
            {
                Assert.That(name, Is.EqualTo("Test 32523"));
                Assert.That(rank, Is.EqualTo("0|i00037:9"));
            });
        }

        [Test]
        public async Task GetOpenWorkItemsByQuery_IgnoresClosedItems()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = PROJ");

            var workItems = await subject.GetOpenWorkItemsByQuery(["Story", "Bug"], team, "labels = \"LabelOfItemThatIsClosed\"");

            Assert.That(workItems, Has.Count.EqualTo(0));
        }

        [Test]
        public async Task GetOpenWorkItemsByQuery_IgnoresItemsOfNotMatchingWorkItemType()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = PROJ");

            var workItems = await subject.GetOpenWorkItemsByQuery(["Bug"], team, "labels = \"ExistingLabel\"");

            Assert.That(workItems, Has.Count.EqualTo(0));
        }

        [Test]
        public async Task GetOpenWorkItemsByQuery_IncludesItemsThatMatchBothTeamAndCustomQuery()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = \"LGHTHSDMO\" AND labels = \"Lagunitas\"");

            var workItems = await subject.GetOpenWorkItemsByQuery(["Story", "Bug"], team, "project = \"LGHTHSDMO\" AND labels = \"Oberon\"");

            Assert.That(workItems, Has.Count.EqualTo(2));
        }

        [Test]
        public async Task GetOpenWorkItemsByQuery_IgnoresItemsThatMatchCustomBotNotTeamTeamQuery()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = \"LGHTHSDMO\" AND labels = \"RebelRevolt\"");

            var workItems = await subject.GetOpenWorkItemsByQuery(["Story", "Bug"], team, "project = \"LGHTHSDMO\" AND labels = \"Oberon\"");

            Assert.That(workItems, Has.Count.EqualTo(0));
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
        [TestCase("PROJ-21", true)]
        [TestCase("PROJ-10", true)]
        [TestCase("PROJ-20", false)]
        public async Task ItemHasChildren_ReturnsTrueIfThereAreChildrenIndependentOfTheirState(string epicReferenceID, bool expectedValue)
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = PROJ");

            var result = await subject.ItemHasChildren(epicReferenceID, team);

            Assert.That(result, Is.EqualTo(expectedValue));
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
        public async Task ValidateConnection_GivenInvalidSettings_ReturnsFalse()
        {
            var subject = CreateSubject();

            var apiToken = "Yah-yah-yah, Coco Jamboo, yah-yah-yeh";
            var organizationUrl = "https://letpeoplework.atlassian.net";
            var username = "benjhuser@gmail.com";

            var connectionSetting = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.Jira, Name = "Test Setting" };
            connectionSetting.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Url, Value = organizationUrl, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Username, Value = username, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.ApiToken, Value = apiToken, IsSecret = true },
                ]);

            var isValid = await subject.ValidateConnection(connectionSetting);

            Assert.That(isValid, Is.False);
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

            var organizationUrl = "https://letpeoplework.atlassian.net";
            var username = "benjhuser@gmail.com";
            var apiToken = Environment.GetEnvironmentVariable("JiraLighthouseIntegrationTestToken") ?? throw new NotSupportedException("Can run test only if Environment Variable 'JiraLighthouseIntegrationTestToken' is set!");

            var connectionSetting = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.Jira, Name = "Test Setting" };
            connectionSetting.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Url, Value = organizationUrl, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Username, Value = username, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.ApiToken, Value = apiToken, IsSecret = true },
                ]);

            team.WorkTrackingSystemConnection = connectionSetting;

            return team;
        }

        private JiraWorkItemService CreateSubject()
        {
            return new JiraWorkItemService(lexoRankServiceMock.Object, new IssueFactory(lexoRankServiceMock.Object, Mock.Of<ILogger<IssueFactory>>()), Mock.Of<ILogger<JiraWorkItemService>>(), new FakeCryptoService());
        }
    }
}
