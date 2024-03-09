using CMFTAspNet.Models;
using CMFTAspNet.Services.Implementation.WorkItemServices;
using CMFTAspNet.WorkTracking;
using CMFTAspNet.WorkTracking.Jira;

namespace CMFTAspNet.Tests.Services.Implementation.WorkItemServices
{
    [Category("Integration")]
    public class JiraWorkItemServiceTest
    {
        [Test]
        public async Task GetClosedWorkItemsForTeam_FullHistory_TestProject_ReturnsCorrectAmountOfItems()
        {
            var subject = new JiraWorkItemService();
            var team = CreateTeam($"project = PROJ");

            var closedItems = await subject.GetClosedWorkItems(720, team);

            Assert.That(closedItems.Count, Is.EqualTo(720));
            Assert.That(closedItems.Sum(), Is.EqualTo(5));
        }

        [Test]
        public async Task GetWorkItemsByLabel_LabelDoesNotExist_ReturnsNoItems()
        {
            var subject = new JiraWorkItemService();
            var team = CreateTeam($"project = PROJ AND labels = \"NotExistingLabel\"");

            var itemsByTag = await subject.GetOpenWorkItems(["Story"], team);

            Assert.That(itemsByTag, Is.Empty);
        }


        [Test]
        public async Task GetWorkItemsByTag_TagExists_ReturnsCorrectNumberOfItems()
        {
            var subject = new JiraWorkItemService();
            var team = CreateTeam($"project = PROJ AND labels = \"ExistingLabel\"");

            var itemsByTag = await subject.GetOpenWorkItems(["Story"], team);

            Assert.That(itemsByTag, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetWorkItemsByTag_TagExists_WorkItemTypeDoesNotMatch_ReturnsNoItems()
        {
            var subject = new JiraWorkItemService();
            var team = CreateTeam($"project = PROJ AND labels = \"ExistingLabel\"");

            var itemsByTag = await subject.GetOpenWorkItems(["Bug"], team);

            Assert.That(itemsByTag, Is.Empty);
        }

        [Test]
        public async Task GetRelatedItems_ItemIdIsParent_FindsRelation()
        {

            var subject = new JiraWorkItemService();
            var team = CreateTeam($"project = PROJ");

            var relatedItems = await subject.GetRemainingRelatedWorkItems("PROJ-9", team);

            Assert.That(relatedItems, Is.EqualTo(1));
        }

        [Test]
        public async Task GetRelatedItems_ItemIdIsChild_DoesNotFindRelation()
        {

            var subject = new JiraWorkItemService();
            var team = CreateTeam($"project = PROJ");

            var relatedItems = await subject.GetRemainingRelatedWorkItems("PROJ-6", team);

            Assert.That(relatedItems, Is.EqualTo(0));
        }

        [Test]
        public async Task GetOpenWorkItemsByTag_ItemIsClosed_ReturnsEmpty()
        {
            var subject = new JiraWorkItemService();
            var team = CreateTeam($"project = PROJ AND labels = \"LabelOfItemThatIsClosed\"");

            var actualItems = await subject.GetOpenWorkItems(["User Story"], team);

            Assert.That(actualItems, Has.Count.EqualTo(0));
        }

        [Test]
        public async Task GetWorkItemDetails_ReturnsTitleAndStackRank()
        {
            var subject = new JiraWorkItemService();
            var team = CreateTeam($"project = PROJ");

            var (name, rank) = await subject.GetWorkItemDetails("PROJ-18", team);

            Assert.Multiple(() =>
            {
                Assert.That(name, Is.EqualTo("Test 32523"));
                Assert.That(rank, Is.EqualTo(0));
            });
        }

        [Test]
        public async Task GetOpenWorkItemsByQuery_UsesSpecifiedQueryAndNotTeamQuery()
        {
            var subject = new JiraWorkItemService();
            var team = CreateTeam($"project = PROJ AND labels = \"LabelOfItemThatIsClosed\"");

            var workItems = await subject.GetOpenWorkItemsByQuery(["Story", "Bug"], team, "labels = \"ExistingLabel\"");

            Assert.Multiple(() =>
            {
                Assert.That(workItems, Has.Count.EqualTo(1));
                Assert.That(workItems.Single(), Is.EqualTo("PROJ-18"));
            });
        }

        [Test]
        public async Task GetOpenWorkItemsByQuery_IgnoresClosedItems()
        {
            var subject = new JiraWorkItemService();
            var team = CreateTeam($"project = PROJ");

            var workItems = await subject.GetOpenWorkItemsByQuery(["Story", "Bug"], team, "labels = \"LabelOfItemThatIsClosed\"");

            Assert.That(workItems, Has.Count.EqualTo(0));
        }

        [Test]
        public async Task GetOpenWorkItemsByQuery_IgnoresItemsOfNotMatchingWorkItemType()
        {
            var subject = new JiraWorkItemService();
            var team = CreateTeam($"project = PROJ");

            var workItems = await subject.GetOpenWorkItemsByQuery(["Bug"], team, "labels = \"ExistingLabel\"");

            Assert.That(workItems, Has.Count.EqualTo(0));
        }

        [Test]
        public async Task IsRelatedToFeature_ItemHasNoRelation_ReturnsFalse()
        {
            var subject = new JiraWorkItemService();
            var team = CreateTeam($"project = PROJ");

            var isRelated = await subject.IsRelatedToFeature("PROJ-18", ["PROJ-8"], team);

            Assert.That(isRelated, Is.False);
        }

        [Test]
        public async Task IsRelatedToFeature_ItemIsChild_ReturnsTrue()
        {
            var subject = new JiraWorkItemService();
            var team = CreateTeam($"project = PROJ");

            var isRelated = await subject.IsRelatedToFeature("PROJ-15", ["PROJ-8"], team);

            Assert.That(isRelated, Is.True);
        }

        [Test]
        public async Task IsRelatedToFeature_ItemIsChild_MultipleFeatures_ReturnsTrue()
        {
            var subject = new JiraWorkItemService();
            var team = CreateTeam($"project = PROJ");

            var isRelated = await subject.IsRelatedToFeature("PROJ-15", ["PROJ-9", "PROJ-8"], team);

            Assert.That(isRelated, Is.True);
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

            team.WorkTrackingSystemOptions.Add(new WorkTrackingSystemOption<Team>(JiraWorkTrackingOptionNames.Url, organizationUrl, false));
            team.WorkTrackingSystemOptions.Add(new WorkTrackingSystemOption<Team>(JiraWorkTrackingOptionNames.Username, username, false));
            team.WorkTrackingSystemOptions.Add(new WorkTrackingSystemOption<Team>(JiraWorkTrackingOptionNames.ApiToken, apiToken, true));

            return team;
        }
    }
}
