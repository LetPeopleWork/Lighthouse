namespace CMFTAspNet.Models.Teams
{
    public class Team
    {
        public Team(int featureWIP)
        {
            FeatureWIP = featureWIP;
            Throughput = new Throughput([1]);
            TeamConfiguration = new DefaultTeamConfiguration();
        }

        public Guid ID { get; } = Guid.NewGuid();

        public ITeamConfiguration TeamConfiguration { get; private set; }

        public int FeatureWIP { get; }

        public Throughput Throughput { get; private set; }

        public void UpdateTeamConfiguration(ITeamConfiguration newTeamConfiguration)
        {
            TeamConfiguration = newTeamConfiguration;
        }

        public void UpdateThroughput(Throughput throughput)
        {
            Throughput = throughput;
        }
    }
}
