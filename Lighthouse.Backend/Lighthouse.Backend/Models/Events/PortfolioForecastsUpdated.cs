namespace Lighthouse.Backend.Models.Events
{
    public record PortfolioForecastsUpdated(int PortfolioId) : IDomainEvent;
}
