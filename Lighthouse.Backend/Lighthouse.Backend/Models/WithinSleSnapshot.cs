namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// One day of one trailing window for one owner: how many Work Items closed in the window, how many of them
    /// finished within the SLE, and the SLE in force that day. Counts are stored rather than a percentage so the
    /// share is always worked out by the same rule that judges the Within SLE widget.
    /// </summary>
    public class WithinSleSnapshot : IEntity
    {
        public int Id { get; set; }

        public int OwnerId { get; set; }

        public OwnerType OwnerType { get; set; }

        public DateOnly RecordedAt { get; set; }

        public int Horizon { get; set; }

        public int ClosedCount { get; set; }

        public int WithinCount { get; set; }

        public int SlePercentile { get; set; }

        public int SleDays { get; set; }

        public WithinSleSnapshotOrigin Origin { get; set; }
    }
}
