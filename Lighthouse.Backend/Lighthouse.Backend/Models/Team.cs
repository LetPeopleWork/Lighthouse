namespace Lighthouse.Backend.Models
{
    public class Team : WorkTrackingSystemOptionsOwner
    {
        public override List<string> WorkItemTypes { get; set; } = ["User Story", "Bug"];

        public int FeatureWIP { get; set; } = 1;

        public bool AutomaticallyAdjustFeatureWIP { get; set; }

        public bool UseFixedDatesForThroughput { get; set; } = false;

        public DateTime? ThroughputHistoryStartDate { get; set; }

        public DateTime? ThroughputHistoryEndDate { get; set; }

        public int ThroughputHistory { get; set; } = 30;

        public string? ForecastFilterRuleSetJson { get; set; }

        public override int DoneItemsCutoffDays { get; set; } = 365;

        public override int StalenessThresholdDays { get; set; }

        public override int BlockedStalenessThresholdDays { get; set; }

        public List<WorkItem> WorkItems { get; } = [];

        /// <param name="today">
        /// Bug #5567: entities are EF-materialised and get no constructor injection, so the
        /// instance's calendar day arrives as a parameter rather than from an injected clock.
        /// </param>
        public ThroughputSettings GetThroughputSettings(DateOnly today)
        {
            var todayAtMidnightUtc = InstanceCalendar.AsUtcMidnight(today);
            var startDate = todayAtMidnightUtc.AddDays(-(ThroughputHistory - 1));
            var endDate = todayAtMidnightUtc;
            var numberOfDays = ThroughputHistory;

            if (UseFixedDatesForThroughput)
            {
                startDate = ThroughputHistoryStartDate ?? startDate;
                endDate = ThroughputHistoryEndDate ?? endDate;
                numberOfDays = (endDate - startDate).Days + 1;
            }

            return new ThroughputSettings(DateTime.SpecifyKind(startDate, DateTimeKind.Utc), DateTime.SpecifyKind(endDate, DateTimeKind.Utc), numberOfDays);
        }
    }
}
