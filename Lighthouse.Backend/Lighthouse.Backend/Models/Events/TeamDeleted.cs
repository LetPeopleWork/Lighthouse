namespace Lighthouse.Backend.Models.Events
{
    public record TeamDeleted(int TeamId, IReadOnlyList<int> AffectedPortfolioIds) : IDomainEvent;
}
