using Lighthouse.Backend.Models.Events;

namespace Lighthouse.Backend.Modules.OrganisationSetup.Contracts
{
    public record ConnectionFetchShapeChanged(int ConnectionId, IReadOnlyList<int> TeamIds, IReadOnlyList<int> PortfolioIds) : IDomainEvent;
}
