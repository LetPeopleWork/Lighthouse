namespace Lighthouse.Backend.Models.Metrics
{
    public class RunChartData
    {
        public RunChartData() : this(new Dictionary<int, List<WorkItemBase>>())
        {
        }

        public RunChartData(Dictionary<int, List<WorkItemBase>> workItemsPerUnitOfTime)
        {
            WorkItemsPerUnitOfTime = workItemsPerUnitOfTime;
        }

        private int[] ValuePerUnitOfTime => WorkItemsPerUnitOfTime.Keys.Select(k => WorkItemsPerUnitOfTime[k].Count).ToArray();

        public Dictionary<int, List<WorkItemBase>> WorkItemsPerUnitOfTime { get; }

        public int History => WorkItemsPerUnitOfTime.Count;

        public int Total => ValuePerUnitOfTime.Sum();

        public int GetCountOnDay(int day)
        {
            return WorkItemsPerUnitOfTime[day].Count;
        }
    }
}
