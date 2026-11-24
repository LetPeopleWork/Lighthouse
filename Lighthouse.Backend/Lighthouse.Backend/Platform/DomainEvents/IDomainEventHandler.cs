using Lighthouse.Backend.Models.Events;

namespace Lighthouse.Backend.Platform.DomainEvents
{
    public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
    {
        Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
    }
}
