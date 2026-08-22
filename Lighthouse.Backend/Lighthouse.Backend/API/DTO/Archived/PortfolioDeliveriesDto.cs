namespace Lighthouse.Backend.API.DTO.Archived
{
    /// <summary>
    /// A Portfolio's Deliveries, with the retired ones kept apart from the ones still running. The
    /// two are separated here rather than by a flag on each row because they are answers to
    /// different questions and are worked out from different sources - the live ones from the
    /// Features as they stand now, the retired ones from what was written down when they closed.
    /// </summary>
    public class PortfolioDeliveriesDto
    {
        public List<DeliveryWithLikelihoodDto> Active { get; set; } = [];

        public List<ArchivedDeliveryDto> Archived { get; set; } = [];
    }
}
