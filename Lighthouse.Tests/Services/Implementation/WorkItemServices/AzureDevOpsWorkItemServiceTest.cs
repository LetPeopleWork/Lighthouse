using Lighthouse.Models;
using Lighthouse.Services.Implementation.WorkItemServices;
using Lighthouse.Services.Interfaces;
using Lighthouse.WorkTracking;
using Lighthouse.WorkTracking.AzureDevOps;

namespace Lighthouse.Tests.Services.Implementation.WorkItemServices
{
    [Category("Integration")]
    public class AzureDevOpsWorkItemServiceTest
    {
        [Test]
        public async Task GetClosedWorkItemsForTeam_FullHistory_TestProject_ReturnsCorrectAmountOfItems()
        {
            var subject = new AzureDevOpsWorkItemService();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [System.Tags] NOT CONTAINS 'ThroughputIgnore'");

            var closedItems = await subject.GetClosedWorkItems(720, team);

            Assert.That(closedItems.Count, Is.EqualTo(720));
            Assert.That(closedItems.Sum(), Is.EqualTo(5));
        }

        [Test]
        public async Task GetWorkItemsByTag_TagDoesNotExist_ReturnsNoItems()
        {
            var subject = new AzureDevOpsWorkItemService();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Tags}] CONTAINS 'NotExistingTag'");

            var itemsByTag = await subject.GetOpenWorkItems(["Feature"], team);

            Assert.That(itemsByTag, Is.Empty);
        }

        [Test]
        public async Task GetWorkItemsByTag_TagExists_ReturnsCorrectNumberOfItems()
        {
            var subject = new AzureDevOpsWorkItemService();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Tags}] CONTAINS 'Release1'");

            var itemsByTag = await subject.GetOpenWorkItems(["Feature"], team);

            Assert.That(itemsByTag, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetWorkItemsByTag_TagExists_WorkItemTypeDoesNotMatch_ReturnsNoItems()
        {
            var subject = new AzureDevOpsWorkItemService();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Tags}] CONTAINS 'Release1'");

            var itemsByTag = await subject.GetOpenWorkItems(["Bug"], team);

            Assert.That(itemsByTag, Is.Empty);
        }

        [Test]
        public async Task GetWorkItemsByAreaPath_AreaPathDoesNotExist_ReturnsNoItems()
        {
            var subject = new AzureDevOpsWorkItemService();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.AreaPath}] UNDER 'CMFTTestTeamProject\\NotExistingAreaPath'");

            var itemsByTag = await subject.GetOpenWorkItems(["Feature"], team);

            Assert.That(itemsByTag, Is.Empty);
        }

        [Test]
        public async Task GetWorkItemsByAreaPath_AreaPathExists_ReturnsCorrectNumberOfItems()
        {
            var subject = new AzureDevOpsWorkItemService();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.AreaPath}] UNDER 'CMFTTestTeamProject\\SomeReleeaseThatIsUsingAreaPaths'");

            var itemsByTag = await subject.GetOpenWorkItems(["User Story"], team);

            Assert.That(itemsByTag, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetWorkItemsByAreaPath_AreaPathExists_WorkItemTypeDoesNotMatch_ReturnsNoItems()
        {
            var subject = new AzureDevOpsWorkItemService();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.AreaPath}] UNDER 'CMFTTestTeamProject\\SomeReleeaseThatIsUsingAreaPaths'");

            var itemsByTag = await subject.GetOpenWorkItems(["Bug"], team);

            Assert.That(itemsByTag, Is.Empty);
        }

        [Test]
        public async Task GetRelatedItems_ItemIdIsParent_FindsRelation()
        {

            var subject = new AzureDevOpsWorkItemService();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");

            var relatedItems = await subject.GetRemainingRelatedWorkItems("370", team);

            Assert.That(relatedItems, Is.EqualTo(2));
        }

        [Test]
        public async Task GetRelatedItems_ItemIdIsPartiallyMatching_DoesNotFindRelation()
        {

            var subject = new AzureDevOpsWorkItemService();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");

            var relatedItems = await subject.GetRemainingRelatedWorkItems("37", team);

            Assert.That(relatedItems, Is.EqualTo(0));
        }

        [Test]
        public async Task GetRelatedItems_ItemIdIsChild_DoesNotFindRelation()
        {

            var subject = new AzureDevOpsWorkItemService();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");
            team.WorkItemTypes.Add("Feature");

            var relatedItems = await subject.GetRemainingRelatedWorkItems("366", team);

            Assert.That(relatedItems, Is.EqualTo(0));
        }

        [Test]
        public async Task GetRelatedItems_ItemIdIsRemoteRelated_FindsRelation()
        {
            var subject = new AzureDevOpsWorkItemService();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");
            team.AdditionalRelatedField = "Custom.RemoteFeatureID";

            var relatedItems = await subject.GetRemainingRelatedWorkItems("279", team);

            Assert.That(relatedItems, Is.EqualTo(1));
        }

        [Test]
        public async Task GetOpenWorkItemsByTag_ItemIsOpen_ReturnsItem()
        {
            var subject = new AzureDevOpsWorkItemService();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Tags}] CONTAINS 'Release1'");

            var actualItems = await subject.GetOpenWorkItems(["User Story"], team);

            Assert.That(actualItems, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetOpenWorkItemsByTag_ItemIsClosed_ReturnsEmpty()
        {
            var subject = new AzureDevOpsWorkItemService();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Tags}] CONTAINS 'PreviousRelease'");

            var actualItems = await subject.GetOpenWorkItems(["User Story"], team);

            Assert.That(actualItems, Has.Count.EqualTo(0));
        }

        [Test]
        public async Task GetOpenWorkItemsByAreaPath_ItemIsOpen_ReturnsItem()
        {
            var subject = new AzureDevOpsWorkItemService();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.AreaPath}] UNDER 'CMFTTestTeamProject\\SomeReleeaseThatIsUsingAreaPaths'");

            var actualItems = await subject.GetOpenWorkItems(["User Story"], team);

            Assert.That(actualItems, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetOpenWorkItemsByAreaPath_ItemIsClosed_ReturnsEmpty()
        {
            var subject = new AzureDevOpsWorkItemService();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.AreaPath}] UNDER 'CMFTTestTeamProject\\PreviousReleaseAreaPath'");

            var actualItems = await subject.GetOpenWorkItems(["User Story"], team);

            Assert.That(actualItems, Has.Count.EqualTo(0));
        }

        [Test]
        public async Task GetWorkItemDetails_ReturnsTitleAndStackRank()
        {
            var subject = new AzureDevOpsWorkItemService();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");

            var (name, rank) = await subject.GetWorkItemDetails("366", team);

            Assert.Multiple(() =>
            {
                Assert.That(name, Is.EqualTo("Test Test Test"));
                Assert.That(rank, Is.EqualTo("1999821120"));
            });
        }

        [Test]
        public async Task GetOpenWorkItemsByQuery_IgnoresClosedItems()
        {
            var subject = new AzureDevOpsWorkItemService();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.AreaPath}] UNDER 'CMFTTestTeamProject\\PreviousReleaseAreaPath'");

            var workItems = await subject.GetOpenWorkItemsByQuery(["User Story", "Bug"], team, "[System.Tags] CONTAINS 'ThroughputIgnore'");

            Assert.That(workItems, Has.Count.EqualTo(0));
        }

        [Test]
        public async Task GetOpenWorkItemsByQuery_IgnoresItemsOfNotMatchingWorkItemType()
        {
            var subject = new AzureDevOpsWorkItemService();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.AreaPath}] UNDER 'CMFTTestTeamProject\\PreviousReleaseAreaPath'");

            var workItems = await subject.GetOpenWorkItemsByQuery(["Bug"], team, "[System.Tags] CONTAINS 'Release1'");

            Assert.That(workItems, Has.Count.EqualTo(0));
        }

        [Test]
        public async Task IsRelatedToFeature_ItemHasNoRelation_ReturnsFalse()
        {
            var subject = new AzureDevOpsWorkItemService();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");

            var isRelated = await subject.IsRelatedToFeature("365", ["370"], team);

            Assert.That(isRelated, Is.False);
        }

        [Test]
        public async Task IsRelatedToFeature_ItemIsChild_ReturnsTrue()
        {
            var subject = new AzureDevOpsWorkItemService();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");

            var isRelated = await subject.IsRelatedToFeature("365", ["371"], team);

            Assert.That(isRelated, Is.True);
        }

        [Test]
        public async Task IsRelatedToFeature_ItemIsChild_MultipleFeatures_ReturnsTrue()
        {
            var subject = new AzureDevOpsWorkItemService();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");

            var isRelated = await subject.IsRelatedToFeature("365", ["370", "371"], team);

            Assert.That(isRelated, Is.True);
        }

        [Test]
        public async Task IsRelatedToFeature_ItemIsRemoteRelatedViaCustomField_ReturnsTrue()
        {
            var subject = new AzureDevOpsWorkItemService();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");
            team.AdditionalRelatedField = "Custom.RemoteFeatureID";

            var isRelated = await subject.IsRelatedToFeature("375", ["279"], team);

            Assert.That(isRelated, Is.True);
        }

        [Test]
        [TestCase("371", true)]
        [TestCase("380", true)]
        [TestCase("379", false)]
        [TestCase("374", false)]
        public async Task ItemHasChildren_ReturnsTrueIfThereAreChildrenIndependentOfTheirState(string featureReferenceId, bool expectedValue)
        {
            var subject = new AzureDevOpsWorkItemService();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");

            var result = await subject.ItemHasChildren(featureReferenceId, team);

            Assert.That(result, Is.EqualTo(expectedValue));
        }

        [Test]
        [TestCase(RelativeOrder.Above)]
        [TestCase(RelativeOrder.Below)]
        public void GetAdjacentOrderIndex_NoFeaturesPassed_Returns0(RelativeOrder relativeOrder)
        {
            var subject = new AzureDevOpsWorkItemService();

            var order = subject.GetAdjacentOrderIndex(Enumerable.Empty<string>(), relativeOrder);

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
            var subject = new AzureDevOpsWorkItemService();

            var order = subject.GetAdjacentOrderIndex(existingItemsOrder, relativeOrder);

            Assert.That(order, Is.EqualTo(expectedResult));
        }

        private Team CreateTeam(string query)
        {
            var team = new Team
            {
                Name = "TestTeam",
                WorkItemQuery = query
            };

            var organizationUrl = "https://dev.azure.com/huserben";
            var personalAccessToken = Environment.GetEnvironmentVariable("AzureDevOpsLighthouseIntegrationTestToken") ?? throw new NotSupportedException("Can run test only if Environment Variable 'AzureDevOpsLighthouseIntegrationTestToken' is set!");

            team.WorkTrackingSystemOptions.Add(new WorkTrackingSystemOption<Team>(AzureDevOpsWorkTrackingOptionNames.Url, organizationUrl, false));
            team.WorkTrackingSystemOptions.Add(new WorkTrackingSystemOption<Team>(AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken, personalAccessToken, true));

            return team;
        }
    }
}
