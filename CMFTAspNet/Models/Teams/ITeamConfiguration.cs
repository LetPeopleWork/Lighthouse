
namespace CMFTAspNet.Models.Teams
{
    public interface ITeamConfiguration
    {
        string TeamProject { get; set; }
        
        List<string> AreaPaths { get; }
        
        List<string> WorkItemTypes { get; }

        List<string> IgnoredTags { get; }
    }
}
