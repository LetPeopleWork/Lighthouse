namespace Lighthouse.Backend.Models.Events
{
    public record WorkItemBecameStale(int WorkItemId, int ThresholdDays) : IDomainEvent;
}
