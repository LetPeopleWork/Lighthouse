using Lighthouse.Backend.Models.Events;

namespace Lighthouse.Backend.Modules.WorkItems.Contracts
{
    public record WorkItemTransitioned(int WorkItemId, string FromState, string ToState) : IDomainEvent;
}
