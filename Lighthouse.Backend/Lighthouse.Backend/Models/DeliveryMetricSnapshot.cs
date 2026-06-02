using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    public class DeliveryMetricSnapshot : IEntity
    {
        public int Id { get; set; }

        public int DeliveryId { get; set; }

        public DateTime RecordedAt { get; set; }

        public int TotalWork { get; set; }

        public int DoneWork { get; set; }

        public int RemainingWork { get; set; }

        public int? EstimatedTotalWork { get; set; }

        public int? ForecastHowMany { get; set; }

        public double? LikelihoodPercentage { get; set; }

        public string? WhenDistributionJson { get; set; }
    }
}
