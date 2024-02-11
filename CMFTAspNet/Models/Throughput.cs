namespace CMFTAspNet.Models
{
    public class Throughput
    {
        private readonly int[] throughputPerUnitOfTime;

        public Throughput(int[] throughputPerUnitOfTime) 
        {
            this.throughputPerUnitOfTime = throughputPerUnitOfTime;
        }

        public int History => throughputPerUnitOfTime.Length;

        public int GetThroughputOnDay(int day)
        {
            return throughputPerUnitOfTime[day];
        }
    }
}
