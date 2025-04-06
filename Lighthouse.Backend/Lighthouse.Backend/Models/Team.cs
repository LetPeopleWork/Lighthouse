namespace Lighthouse.Backend.Models
{
    public class Team : WorkTrackingSystemOptionsOwner
    {
        public string Name { get; set; }

        public override List<string> WorkItemTypes { get; set; } = new List<string> { "User Story", "Bug" };

        public int FeatureWIP { get; set; } = 1;

        public bool AutomaticallyAdjustFeatureWIP { get; set; }

        public string? AdditionalRelatedField { get; set; } = string.Empty;

        public DateTime TeamUpdateTime { get; set; } = DateTime.MinValue;

        public bool UseFixedDatesForThroughput { get; set; } = false;

        public DateTime? ThroughputHistoryStartDate { get; set; }

        public DateTime? ThroughputHistoryEndDate { get; set; }

        public int ThroughputHistory { get; set; } = 30;

        public List<Project> Projects { get; } = [];

        public List<WorkItem> WorkItems { get; } = [];

        public void ResetUpdateTime()
        {
            TeamUpdateTime = DateTime.MinValue.ToUniversalTime();
        }

        public void RefreshUpdateTime()
        {
            TeamUpdateTime = DateTime.UtcNow;
        }

        public ThroughputSettings GetThroughputSettings()
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-(ThroughputHistory - 1));
            var endDate = DateTime.UtcNow.Date;
            var numberOfDays = ThroughputHistory;

            if (UseFixedDatesForThroughput)
            {
                startDate = ThroughputHistoryStartDate ?? startDate;
                endDate = ThroughputHistoryEndDate ?? endDate;
                numberOfDays = (endDate - startDate).Days + 1;
            }

            return new ThroughputSettings(startDate, endDate, numberOfDays);
        }
    }
}
