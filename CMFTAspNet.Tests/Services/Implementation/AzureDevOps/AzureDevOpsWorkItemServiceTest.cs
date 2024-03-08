using CMFTAspNet.Models;
using CMFTAspNet.Services.Implementation.WorkItemServices;
using CMFTAspNet.WorkTracking;
using CMFTAspNet.WorkTracking.AzureDevOps;

namespace CMFTAspNet.Tests.Services.Implementation.WorkItemServices
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
            Assert.That(closedItems.Sum(), Is.EqualTo(4));
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
        public async Task GetNotClosedWorkItemsByTag_ItemIsOpen_ReturnsItem()
        {
            var subject = new AzureDevOpsWorkItemService();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Tags}] CONTAINS 'Release1'");

            var actualItems = await subject.GetOpenWorkItems(["User Story"], team);

            Assert.That(actualItems, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetNotClosedWorkItemsByTag_ItemIsClosed_ReturnsEmpty()
        {
            var subject = new AzureDevOpsWorkItemService();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Tags}] CONTAINS 'PreviousRelease'");

            var actualItems = await subject.GetOpenWorkItems(["User Story"], team);

            Assert.That(actualItems, Has.Count.EqualTo(0));
        }

        [Test]
        public async Task GetNotClosedWorkItemsByAreaPath_ItemIsOpen_ReturnsItem()
        {
            var subject = new AzureDevOpsWorkItemService();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.AreaPath}] UNDER 'CMFTTestTeamProject\\SomeReleeaseThatIsUsingAreaPaths'");

            var actualItems = await subject.GetOpenWorkItems(["User Story"], team);

            Assert.That(actualItems, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetNotClosedWorkItemsByAreaPath_ItemIsClosed_ReturnsEmpty()
        {
            var subject = new AzureDevOpsWorkItemService();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.AreaPath}] UNDER 'CMFTTestTeamProject\\PreviousReleaseAreaPath'");

            var actualItems = await subject.GetOpenWorkItems(["User Story"], team);

            Assert.That(actualItems, Has.Count.EqualTo(0));
        }

        private Team CreateTeam(string query)
        {
            var team = new Team
            {
                Name = "TestTeam",
                WorkItemQuery = query
            };

            var organizationUrl = "https://dev.azure.com/huserben";
            var personalAccessToken = Environment.GetEnvironmentVariable("AzureDevOpsCMFTIntegrationTestToken") ?? throw new NotSupportedException("Can run test only if Environment Variable 'AzureDevOpsCMFTIntegrationTestToken' is set!");
            
            team.WorkTrackingSystemOptions.Add(new WorkTrackingSystemOption<Team>(AzureDevOpsWorkTrackingOptionNames.Url, organizationUrl, false));
            team.WorkTrackingSystemOptions.Add(new WorkTrackingSystemOption<Team>(AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken, personalAccessToken, true));

            return team;
        }
    }
}
