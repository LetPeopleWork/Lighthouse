using Lighthouse.Backend.Models.Events;

namespace Lighthouse.Backend.Modules.OrganisationSetup.Contracts
{
    public record TeamDeleted(int TeamId, IReadOnlyList<int> AffectedPortfolioIds) : IDomainEvent;
}
