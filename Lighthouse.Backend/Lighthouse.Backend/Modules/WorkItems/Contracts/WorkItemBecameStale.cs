using Lighthouse.Backend.Models.Events;

namespace Lighthouse.Backend.Modules.WorkItems.Contracts
{
    public record WorkItemBecameStale(int WorkItemId, int ThresholdDays) : IDomainEvent;
}
