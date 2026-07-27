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

        public List<Portfolio> Portfolios { get; } = [];

        public List<WorkItem> WorkItems { get; } = [];

        /// <param name="today">
        /// Bug #5567: the instance's calendar day, supplied by the caller. Entities are
        /// EF-materialised and get no constructor injection, so the day arrives as a parameter -
        /// the same shape the blackout periods already use.
        /// </param>
        public ThroughputSettings GetThroughputSettings(DateOnly today)
        {
            var todayAtMidnightUtc = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
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
