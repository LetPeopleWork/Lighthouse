namespace CMFTAspNet.Models
{
    public class Team : WorkTrackingSystemOptionsOwner<Team>
    {
        public string Name { get; set; }

        public List<string> WorkItemTypes { get; set; } = new List<string> { "User Story", "Bug" };

        public int FeatureWIP { get; set; } = 1;

        public string? AdditionalRelatedField { get; set; } = string.Empty;

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
