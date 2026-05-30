namespace Lighthouse.Backend.Models.Events
{
    public record WorkItemTransitioned(int WorkItemId, string FromState, string ToState) : IDomainEvent;
}
