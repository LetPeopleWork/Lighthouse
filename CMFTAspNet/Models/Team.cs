namespace CMFTAspNet.Models
{
    public class Team
    {
        public Team(int featureWIP, Throughput throughput)
        {
            FeatureWIP = featureWIP;
            Throughput = throughput;
        }

        public Guid ID { get; } = Guid.NewGuid();

        public int FeatureWIP { get; }

        public Throughput Throughput { get; }
    }
}
