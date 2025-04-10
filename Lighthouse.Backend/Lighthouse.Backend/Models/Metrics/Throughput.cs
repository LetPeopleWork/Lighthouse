namespace Lighthouse.Backend.Models.Metrics
{
    public class Throughput
    {
        public Throughput() : this([])
        {
        }

        public Throughput(int[] throughputPerUnitOfTime)
        {
            ThroughputPerUnitOfTime = throughputPerUnitOfTime;
        }

        public int[] ThroughputPerUnitOfTime { get; set; }

        public int History => ThroughputPerUnitOfTime.Length;

        public int TotalThroughput => ThroughputPerUnitOfTime.Sum();

        public int GetThroughputOnDay(int day)
        {
            return ThroughputPerUnitOfTime[day];
        }
    }
}
