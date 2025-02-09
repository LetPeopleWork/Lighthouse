namespace Lighthouse.Backend.Models
{
    public class ThroughputSettings
    {
        public ThroughputSettings(DateTime startDate, DateTime endDate, int numberOfDays)
        {
            StartDate = startDate;
            EndDate = endDate;
            NumberOfDays = numberOfDays;
        }

        public DateTime StartDate { get; }

        public DateTime EndDate { get; }
        
        public int NumberOfDays { get; }
    }
}
