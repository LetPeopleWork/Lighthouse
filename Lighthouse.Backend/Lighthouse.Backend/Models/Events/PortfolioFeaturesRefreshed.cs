namespace Lighthouse.Backend.Models.Events
{
    public record PortfolioFeaturesRefreshed(int PortfolioId) : IDomainEvent;
}
