using Lighthouse.Backend.Models.Events;

namespace Lighthouse.Backend.Services.Interfaces.DomainEvents
{
    public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
    {
        Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
    }
}
