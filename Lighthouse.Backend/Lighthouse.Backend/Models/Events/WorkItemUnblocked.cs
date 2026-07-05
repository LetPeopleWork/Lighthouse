namespace Lighthouse.Backend.Models.Events
{
    public record WorkItemUnblocked(int WorkItemId) : IDomainEvent;
}
