using CMFTAspNet.WorkTracking;

namespace CMFTAspNet.Models.Teams
{
    public class Team
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string ProjectName { get; set; }

        public WorkTrackingSystems WorkTrackingSystem { get; set; }

        public Dictionary<string, string> WorkTrackingSystemOptions { get; set; } = new Dictionary<string, string>();

        public List<string> AreaPaths { get; set; } = new List<string>();

        public List<string> WorkItemTypes { get; set; } = new List<string> { "User Story", "Bug" };

        public List<string> IgnoredTags { get; set; } = new List<string>();

        public List<string> AdditionalRelatedFields { get; set; } = new List<string>();

        public int FeatureWIP { get; set; } = 1;

        public int[] RawThroughput { get; set; } = [1];

        public int ThroughputHistory { get; set; } = 30;

        public Throughput Throughput => new Throughput(RawThroughput);
    }
}
