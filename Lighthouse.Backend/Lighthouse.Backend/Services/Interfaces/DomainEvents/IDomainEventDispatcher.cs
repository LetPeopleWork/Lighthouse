using Lighthouse.Backend.Models.Events;

namespace Lighthouse.Backend.Services.Interfaces.DomainEvents
{
    /// <summary>
    /// Inbound application port for in-process domain-event publication (ADR-027 D1/D2). Publishing an event
    /// runs every registered <see cref="IDomainEventHandler{TEvent}"/> for that event type; a single handler
    /// that throws is logged and isolated so the remaining handlers still run and the already-committed fact is
    /// not lost — recovery of a failed reaction is the next scheduled re-sync, not an outbox.
    /// </summary>
    public interface IDomainEventDispatcher
    {
        Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) where TEvent : IDomainEvent;
    }
}
