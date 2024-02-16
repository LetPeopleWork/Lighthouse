namespace CMFTAspNet.Models.Teams
{
    public class Team
    {
        public Team(string teamName, int featureWIP)
        {
            TeamName = teamName;
            FeatureWIP = featureWIP;
            Throughput = new Throughput([1]);
            TeamConfiguration = new DefaultTeamConfiguration();
        }

        public Guid ID { get; } = Guid.NewGuid();

        public ITeamConfiguration TeamConfiguration { get; private set; }

        public string TeamName { get; set; }

        public int FeatureWIP { get; set;  }

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
