using CMFTAspNet.Models.Connections;
using CMFTAspNet.Models.Teams;
using CMFTAspNet.Services.AzureDevOps;
using Microsoft.TeamFoundation.Core.WebApi;

namespace CMFTAspNet.Tests.Services.AzureDevOps
{
    public class AzureDevOpsWorkItemServiceTest
    {
        [Test]
        [Category("Integration")]
        public async Task GetClosedWorkItemsForTeam_FullHistory_TestProject_ReturnsCorrectAmountOfItems()
        {
            var subject = new AzureDevOpsWorkItemService();
            var teamConfiguration = CreateTeamConfiguration();

            var closedItems = await subject.GetClosedWorkItemsForTeam(teamConfiguration, 720);

            Assert.That(closedItems.Count, Is.EqualTo(720));
            Assert.That(closedItems.Sum(), Is.EqualTo(2));
        }

        private AzureDevOpsTeamConfiguration CreateTeamConfiguration()
        {
            var azureDevOpsConfig = new AzureDevOpsConfiguration
            {
                Url = "https://dev.azure.com/huserben",
                PersonalAccessToken = Environment.GetEnvironmentVariable("AzureDevOpsCMFTIntegrationTestToken")
            };

            var teamConfig = new AzureDevOpsTeamConfiguration
            {
                AzureDevOpsConfiguration = azureDevOpsConfig,
                TeamProject = "CMFTTestTeamProject",
                AreaPaths = ["CMFTTestTeamProject"]
            };

            return teamConfig;
        }
    }
}
