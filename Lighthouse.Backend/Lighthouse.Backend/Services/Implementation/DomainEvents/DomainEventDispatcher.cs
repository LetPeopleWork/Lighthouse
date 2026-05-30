using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Services.Implementation.DomainEvents
{
    public class DomainEventDispatcher(IServiceScopeFactory serviceScopeFactory, ILogger<DomainEventDispatcher> logger) : IDomainEventDispatcher
    {
        public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) where TEvent : IDomainEvent
        {
            using var scope = serviceScopeFactory.CreateScope();
            var handlers = scope.ServiceProvider.GetServices<IDomainEventHandler<TEvent>>();

            foreach (var handler in handlers)
            {
                await InvokeHandlerSafely(handler, domainEvent, cancellationToken);
            }
        }

        private async Task InvokeHandlerSafely<TEvent>(IDomainEventHandler<TEvent> handler, TEvent domainEvent, CancellationToken cancellationToken) where TEvent : IDomainEvent
        {
            try
            {
                await handler.HandleAsync(domainEvent, cancellationToken);
            }
#pragma warning disable CA1031 // one failing handler must not abort the others or lose the committed fact; recovery is the next re-sync
            catch (Exception ex)
#pragma warning restore CA1031
            {
                logger.LogError(ex, "Handler {HandlerType} failed processing domain event {EventType}; continuing with remaining handlers", handler.GetType().Name, typeof(TEvent).Name);
            }
        }
    }
}
