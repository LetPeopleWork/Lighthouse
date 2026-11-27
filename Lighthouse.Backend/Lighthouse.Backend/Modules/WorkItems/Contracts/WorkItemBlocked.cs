using Lighthouse.Backend.Models.Events;

namespace Lighthouse.Backend.Modules.WorkItems.Contracts
{
    public record WorkItemBlocked(int WorkItemId, string Reason) : IDomainEvent;
}
