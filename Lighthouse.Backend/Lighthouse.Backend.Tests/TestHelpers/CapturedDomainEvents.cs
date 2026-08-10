using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// The signals a refresh raised, in the order it raised them. Several of this epic's promises are
    /// about a signal that no handler persists — staleness is the case that bites — so the event bus is
    /// where they are observable. <see cref="DomainEventDispatcher"/> resolves handlers from a fresh
    /// scope per publication, which is why the store is shared and the handler is a thin recorder.
    /// </summary>
    public sealed class CapturedDomainEvents
    {
        private readonly List<IDomainEvent> raised = [];
        private readonly Lock gate = new();

        public void Record(IDomainEvent domainEvent)
        {
            lock (gate)
            {
                raised.Add(domainEvent);
            }
        }

        public void Clear()
        {
            lock (gate)
            {
                raised.Clear();
            }
        }

        public List<TEvent> Of<TEvent>() where TEvent : IDomainEvent
        {
            lock (gate)
            {
                return [.. raised.OfType<TEvent>()];
            }
        }
    }

    /// <summary>
    /// Records one event type into <see cref="CapturedDomainEvents"/> and does nothing else. Registered
    /// alongside the production handlers, so it never displaces one.
    /// </summary>
    public sealed class CapturingDomainEventHandler<TEvent>(CapturedDomainEvents captured)
        : IDomainEventHandler<TEvent> where TEvent : IDomainEvent
    {
        public Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken)
        {
            captured.Record(domainEvent);
            return Task.CompletedTask;
        }
    }
}
