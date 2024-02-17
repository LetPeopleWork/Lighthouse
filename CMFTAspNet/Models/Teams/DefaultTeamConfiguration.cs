
namespace CMFTAspNet.Models.Teams
{
    public class DefaultTeamConfiguration : ITeamConfiguration
    {
        public string TeamProject { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public List<string> AreaPaths => throw new NotImplementedException();

        public List<string> WorkItemTypes => throw new NotImplementedException();

        public List<string> IgnoredTags => throw new NotImplementedException();
    }
}
