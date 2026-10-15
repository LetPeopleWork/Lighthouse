namespace Lighthouse.Backend.Models.Events
{
    public record ConnectionFetchShapeChanged(int ConnectionId, IReadOnlyList<int> TeamIds, IReadOnlyList<int> PortfolioIds) : IDomainEvent;
}
