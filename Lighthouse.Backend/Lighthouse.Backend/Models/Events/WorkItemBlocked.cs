namespace Lighthouse.Backend.Models.Events
{
    public record WorkItemBlocked(int WorkItemId, string Reason) : IDomainEvent;
}
