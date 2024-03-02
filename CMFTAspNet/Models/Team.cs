using CMFTAspNet.Services.Interfaces;

namespace CMFTAspNet.Models
{
    public class Team : WorkTrackingSystemOptionsOwner<Team>, IEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public List<string> WorkItemTypes { get; set; } = new List<string> { "User Story", "Bug" };

        public List<string> IgnoredTags { get; set; } = new List<string>();

        public List<string> AdditionalRelatedFields { get; set; } = new List<string>();

        public int FeatureWIP { get; set; } = 1;

        public DateTime ThroughputUpdateTime { get; set; }

        public int[] RawThroughput { get; set; } = [1];

        public int ThroughputHistory { get; set; } = 30;

        public Throughput Throughput => new Throughput(RawThroughput);

        public int TotalThroughput => RawThroughput.Sum();
        public void UpdateThroughput(int[] throughput)
        {
            RawThroughput = throughput;
            ThroughputUpdateTime = DateTime.Now;
        }
    }
}
