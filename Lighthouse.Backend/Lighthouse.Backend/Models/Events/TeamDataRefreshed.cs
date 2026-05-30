namespace Lighthouse.Backend.Models.Events
{
    public record TeamDataRefreshed(int TeamId) : IDomainEvent;
}
