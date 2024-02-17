using CMFTAspNet.Models.Connections;

namespace CMFTAspNet.Models.Teams
{
    public class AzureDevOpsTeamConfiguration : ITeamConfiguration
    {
        public AzureDevOpsTeamConfiguration()
        {
            WorkItemTypes = ["User Story", "Bug"];
        }

        public AzureDevOpsConfiguration AzureDevOpsConfiguration { get; set; }
        
        public string TeamProject { get; set; }

        public List<string> AreaPaths { get; } = new List<string>();

        public List<string> WorkItemTypes { get; }

        public List<string> IgnoredTags { get; } = new List<string>();

        public List<string> AdditionalRelatedFields { get; } = new List<string>();
    }
}
