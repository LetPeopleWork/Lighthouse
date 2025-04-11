namespace Lighthouse.Backend.Models.Metrics
{
    public class RunChartData
    {
        public RunChartData() : this([])
        {
        }

        public RunChartData(int[] valuePerUnitOfTime)
        {
            ValuePerUnitOfTime = valuePerUnitOfTime;
        }

        public int[] ValuePerUnitOfTime { get; set; }

        public int History => ValuePerUnitOfTime.Length;

        public int Total => ValuePerUnitOfTime.Sum();

        public int GetValueOnDay(int day)
        {
            return ValuePerUnitOfTime[day];
        }
    }
}
