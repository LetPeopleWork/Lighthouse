using CMFTAspNet.Models.Connections;

namespace CMFTAspNet.Models.Teams
{
    public class AzureDevOpsTeamConfiguration : ITeamConfiguration
    {
        public AzureDevOpsConfiguration AzureDevOpsConfiguration { get; set; }
        
        public string TeamProject { get; set; }

        public string[] AreaPaths { get; set; }
    }
}
