namespace Lighthouse.Backend.Models
{
    public class Team : WorkTrackingSystemOptionsOwner
    {
        public string Name { get; set; }

        public List<string> WorkItemTypes { get; set; } = new List<string> { "User Story", "Bug" };

        public int FeatureWIP { get; set; } = 1;

        public List<string> FeaturesInProgress { get; set; } = new List<string>();

        public string? AdditionalRelatedField { get; set; } = string.Empty;

        public DateTime TeamUpdateTime { get; set; }

        public int[] RawThroughput { get; set; } = [1];

        public int ThroughputHistory { get; set; } = 30;

        public Throughput Throughput => new Throughput(RawThroughput);

        public int TotalThroughput => RawThroughput.Sum();

        public void UpdateThroughput(int[] throughput)
        {
            RawThroughput = throughput;
            TeamUpdateTime = DateTime.UtcNow;
        }

        public void SetFeaturesInProgress(IEnumerable<string> featureReferences)
        {
            FeaturesInProgress.Clear();
            FeaturesInProgress.AddRange(featureReferences);
        }
    }
}
