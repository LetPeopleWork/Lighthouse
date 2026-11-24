namespace Lighthouse.Backend.Models.Events
{
    public record PortfolioDeleted(int PortfolioId) : IDomainEvent;
}
