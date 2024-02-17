using CMFTAspNet.Models.Connections;
using CMFTAspNet.Models.Teams;
using CMFTAspNet.Services.Implementation.AzureDevOps;

namespace CMFTAspNet.Tests.Services.Implementation.AzureDevOps
{
    [Category("Integration")]
    public class AzureDevOpsWorkItemServiceTest
    {
        [Test]
        public async Task GetClosedWorkItemsForTeam_FullHistory_TestProject_ReturnsCorrectAmountOfItems()
        {
            var subject = new AzureDevOpsWorkItemService();
            var teamConfiguration = CreateTeamConfiguration();

            var closedItems = await subject.GetClosedWorkItemsForTeam(720, teamConfiguration);

            Assert.That(closedItems.Count, Is.EqualTo(720));
            Assert.That(closedItems.Sum(), Is.EqualTo(2));
        }

        [Test]
        public async Task GetWorkItemsByTag_TagDoesNotExist_ReturnsNoItems()
        {
            var subject = new AzureDevOpsWorkItemService();
            var teamConfiguration = CreateTeamConfiguration();

            var itemsByTag = await subject.GetWorkItemsByTag(["Feature"], "NotExistingTag", teamConfiguration);

            Assert.That(itemsByTag, Is.Empty);
        }

        [Test]
        public async Task GetWorkItemsByTag_TagExists_ReturnsCorrectNumberOfItems()
        {
            var subject = new AzureDevOpsWorkItemService();
            var teamConfiguration = CreateTeamConfiguration();

            var itemsByTag = await subject.GetWorkItemsByTag(["Feature"], "Release1", teamConfiguration);

            Assert.That(itemsByTag, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetWorkItemsByTag_TagExists_WorkItemTypeDoesNotMatch_ReturnsNoItems()
        {
            var subject = new AzureDevOpsWorkItemService();
            var teamConfiguration = CreateTeamConfiguration();

            var itemsByTag = await subject.GetWorkItemsByTag(["Bug"], "Release1", teamConfiguration);

            Assert.That(itemsByTag, Is.Empty);
        }

        [Test]
        public async Task GetWorkItemsByAreaPath_AreaPathDoesNotExist_ReturnsNoItems()
        {
            var subject = new AzureDevOpsWorkItemService();
            var teamConfiguration = CreateTeamConfiguration();

            var itemsByTag = await subject.GetWorkItemsByAreaPath(["Feature"], "NotExistingAreaPath", teamConfiguration);

            Assert.That(itemsByTag, Is.Empty);
        }

        [Test]
        public async Task GetWorkItemsByAreaPath_AreaPathExists_ReturnsCorrectNumberOfItems()
        {
            var subject = new AzureDevOpsWorkItemService();
            var teamConfiguration = CreateTeamConfiguration();

            var itemsByTag = await subject.GetWorkItemsByAreaPath(["Feature"], "CMFTTestTeamProject\\SomeReleeaseThatIsUsingAreaPaths", teamConfiguration);

            Assert.That(itemsByTag, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetWorkItemsByAreaPath_AreaPathExists_WorkItemTypeDoesNotMatch_ReturnsNoItems()
        {
            var subject = new AzureDevOpsWorkItemService();
            var teamConfiguration = CreateTeamConfiguration();

            var itemsByTag = await subject.GetWorkItemsByAreaPath(["Bug"], "CMFTTestTeamProject\\SomeReleeaseThatIsUsingAreaPaths", teamConfiguration);

            Assert.That(itemsByTag, Is.Empty);
        }

        [Test]
        public async Task GetRelatedItems_ItemIdIsParent_FindsRelation()
        {

            var subject = new AzureDevOpsWorkItemService();
            var teamConfiguration = CreateTeamConfiguration();

            var relatedItems = await subject.GetRemainingRelatedWorkItems(370, teamConfiguration);

            Assert.That(relatedItems, Is.EqualTo(2));
        }

        [Test]
        public async Task GetRelatedItems_ItemIdIsChild_DoesNotFindRelation()
        {

            var subject = new AzureDevOpsWorkItemService();
            var teamConfiguration = CreateTeamConfiguration();
            teamConfiguration.WorkItemTypes.Add("Feature");

            var relatedItems = await subject.GetRemainingRelatedWorkItems(366, teamConfiguration);

            Assert.That(relatedItems, Is.EqualTo(0));
        }

        [Test]
        public async Task GetRelatedItems_ItemIdIsRemoteRelated_FindsRelation()
        {
            var subject = new AzureDevOpsWorkItemService();
            var teamConfiguration = CreateTeamConfiguration();
            teamConfiguration.AdditionalRelatedFields.Add("Custom.RemoteFeatureID");

            var relatedItems = await subject.GetRemainingRelatedWorkItems(279, teamConfiguration);

            Assert.That(relatedItems, Is.EqualTo(1));
        }

        private AzureDevOpsTeamConfiguration CreateTeamConfiguration()
        {
            var azureDevOpsConfig = new AzureDevOpsConfiguration
            {
                Url = "https://dev.azure.com/huserben",
                PersonalAccessToken = Environment.GetEnvironmentVariable("AzureDevOpsCMFTIntegrationTestToken") ?? throw new NotSupportedException("Can run test only if Environment Variable 'AzureDevOpsCMFTIntegrationTestToken' is set!")
            };

            var teamConfig = new AzureDevOpsTeamConfiguration
            {
                AzureDevOpsConfiguration = azureDevOpsConfig,
                TeamProject = "CMFTTestTeamProject"
            };

            teamConfig.AreaPaths.Add("CMFTTestTeamProject");
            teamConfig.IgnoredTags.Add("ThroughputIgnore");

            return teamConfig;
        }
    }
}
