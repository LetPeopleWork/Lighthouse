using Lighthouse.Backend.Models.Events;

namespace Lighthouse.Backend.Modules.WorkItems.Contracts
{
    public record TeamDataRefreshed(int TeamId) : IDomainEvent;
}
