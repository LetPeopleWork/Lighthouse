using Lighthouse.Backend.Models.Events;

namespace Lighthouse.Backend.Modules.OrganisationSetup.Contracts
{
    public record PortfolioDeleted(int PortfolioId) : IDomainEvent;
}
