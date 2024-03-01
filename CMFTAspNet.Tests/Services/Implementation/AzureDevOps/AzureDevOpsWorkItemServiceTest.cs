using CMFTAspNet.Models;
using CMFTAspNet.Services.Implementation.AzureDevOps;
using CMFTAspNet.WorkTracking;
using CMFTAspNet.WorkTracking.AzureDevOps;

namespace CMFTAspNet.Tests.Services.Implementation.AzureDevOps
{
    [Category("Integration")]
    public class AzureDevOpsWorkItemServiceTest
    {
        [Test]
        public async Task GetClosedWorkItemsForTeam_FullHistory_TestProject_ReturnsCorrectAmountOfItems()
        {
            var subject = new AzureDevOpsWorkItemService();
            var teamConfiguration = CreateTeam();

            var closedItems = await subject.GetClosedWorkItemsForTeam(720, teamConfiguration);

            Assert.That(closedItems.Count, Is.EqualTo(720));
            Assert.That(closedItems.Sum(), Is.EqualTo(4));
        }

        [Test]
        public async Task GetWorkItemsByTag_TagDoesNotExist_ReturnsNoItems()
        {
            var subject = new AzureDevOpsWorkItemService();
            var teamConfiguration = CreateTeam();

            var itemsByTag = await subject.GetWorkItemsByTag(["Feature"], "NotExistingTag", teamConfiguration);

            Assert.That(itemsByTag, Is.Empty);
        }

        [Test]
        public async Task GetWorkItemsByTag_TagExists_ReturnsCorrectNumberOfItems()
        {
            var subject = new AzureDevOpsWorkItemService();
            var teamConfiguration = CreateTeam();

            var itemsByTag = await subject.GetWorkItemsByTag(["Feature"], "Release1", teamConfiguration);

            Assert.That(itemsByTag, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetWorkItemsByTag_TagExists_WorkItemTypeDoesNotMatch_ReturnsNoItems()
        {
            var subject = new AzureDevOpsWorkItemService();
            var teamConfiguration = CreateTeam();

            var itemsByTag = await subject.GetWorkItemsByTag(["Bug"], "Release1", teamConfiguration);

            Assert.That(itemsByTag, Is.Empty);
        }

        [Test]
        public async Task GetWorkItemsByAreaPath_AreaPathDoesNotExist_ReturnsNoItems()
        {
            var subject = new AzureDevOpsWorkItemService();
            var teamConfiguration = CreateTeam();

            var itemsByTag = await subject.GetWorkItemsByAreaPath(["Feature"], "NotExistingAreaPath", teamConfiguration);

            Assert.That(itemsByTag, Is.Empty);
        }

        [Test]
        public async Task GetWorkItemsByAreaPath_AreaPathExists_ReturnsCorrectNumberOfItems()
        {
            var subject = new AzureDevOpsWorkItemService();
            var teamConfiguration = CreateTeam();

            var itemsByTag = await subject.GetWorkItemsByAreaPath(["Feature"], "CMFTTestTeamProject\\SomeReleeaseThatIsUsingAreaPaths", teamConfiguration);

            Assert.That(itemsByTag, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetWorkItemsByAreaPath_AreaPathExists_WorkItemTypeDoesNotMatch_ReturnsNoItems()
        {
            var subject = new AzureDevOpsWorkItemService();
            var teamConfiguration = CreateTeam();

            var itemsByTag = await subject.GetWorkItemsByAreaPath(["Bug"], "CMFTTestTeamProject\\SomeReleeaseThatIsUsingAreaPaths", teamConfiguration);

            Assert.That(itemsByTag, Is.Empty);
        }

        [Test]
        public async Task GetRelatedItems_ItemIdIsParent_FindsRelation()
        {

            var subject = new AzureDevOpsWorkItemService();
            var teamConfiguration = CreateTeam();

            var relatedItems = await subject.GetRemainingRelatedWorkItems(370, teamConfiguration);

            Assert.That(relatedItems, Is.EqualTo(2));
        }

        [Test]
        public async Task GetRelatedItems_ItemIdIsChild_DoesNotFindRelation()
        {

            var subject = new AzureDevOpsWorkItemService();
            var teamConfiguration = CreateTeam();
            teamConfiguration.WorkItemTypes.Add("Feature");

            var relatedItems = await subject.GetRemainingRelatedWorkItems(366, teamConfiguration);

            Assert.That(relatedItems, Is.EqualTo(0));
        }

        [Test]
        public async Task GetRelatedItems_ItemIdIsRemoteRelated_FindsRelation()
        {
            var subject = new AzureDevOpsWorkItemService();
            var teamConfiguration = CreateTeam();
            teamConfiguration.AdditionalRelatedFields.Add("Custom.RemoteFeatureID");

            var relatedItems = await subject.GetRemainingRelatedWorkItems(279, teamConfiguration);

            Assert.That(relatedItems, Is.EqualTo(1));
        }

        [Test]
        public async Task GetNotClosedWorkItemsByTag_ItemIsOpen_ReturnsItem()
        {
            var subject = new AzureDevOpsWorkItemService();
            var teamConfiguration = CreateTeam();            

            var actualItems = await subject.GetNotClosedWorkItemsByTag(["User Story"], "Release1", teamConfiguration);

            Assert.That(actualItems, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetNotClosedWorkItemsByTag_ItemIsClosed_ReturnsEmpty()
        {
            var subject = new AzureDevOpsWorkItemService();
            var teamConfiguration = CreateTeam();            

            var actualItems = await subject.GetNotClosedWorkItemsByTag(["User Story"], "PreviousRelease", teamConfiguration);

            Assert.That(actualItems, Has.Count.EqualTo(0));
        }

        [Test]
        public async Task GetNotClosedWorkItemsByAreaPath_ItemIsOpen_ReturnsItem()
        {
            var subject = new AzureDevOpsWorkItemService();
            var teamConfiguration = CreateTeam();            

            var actualItems = await subject.GetNotClosedWorkItemsByAreaPath(["User Story"], "CMFTTestTeamProject\\SomeReleeaseThatIsUsingAreaPaths", teamConfiguration);

            Assert.That(actualItems, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetNotClosedWorkItemsByAreaPath_ItemIsClosed_ReturnsEmpty()
        {
            var subject = new AzureDevOpsWorkItemService();
            var teamConfiguration = CreateTeam();

            var actualItems = await subject.GetNotClosedWorkItemsByAreaPath(["User Story"], "CMFTTestTeamProject\\PreviousReleaseAreaPath", teamConfiguration);

            Assert.That(actualItems, Has.Count.EqualTo(0));
        }

        private Team CreateTeam()
        {
            var team = new Team
            {
                Name = "TestTeam",
            };

            var organizationUrl = "https://dev.azure.com/huserben";
            var personalAccessToken = Environment.GetEnvironmentVariable("AzureDevOpsCMFTIntegrationTestToken") ?? throw new NotSupportedException("Can run test only if Environment Variable 'AzureDevOpsCMFTIntegrationTestToken' is set!");
            
            team.WorkTrackingSystemOptions.Add(new WorkTrackingSystemOption(AzureDevOpsWorkTrackingOptionNames.AzureDevOpsUrl, organizationUrl, false));
            team.WorkTrackingSystemOptions.Add(new WorkTrackingSystemOption(AzureDevOpsWorkTrackingOptionNames.AzureDevOpsTeamProject, "CMFTTestTeamProject", false));
            team.WorkTrackingSystemOptions.Add(new WorkTrackingSystemOption(AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken, personalAccessToken, true));

            team.AreaPaths.Add("CMFTTestTeamProject");
            team.IgnoredTags.Add("ThroughputIgnore");

            return team;
        }
    }
}
