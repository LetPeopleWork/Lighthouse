using Lighthouse.Backend.Models.Events;

namespace Lighthouse.Backend.Modules.WorkItems.Contracts
{
    public record WorkItemUnblocked(int WorkItemId) : IDomainEvent;
}
