using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Services.Implementation.DomainEvents
{
    public class WorkItemBlockedTransitionCloseHandler(
        IWorkItemBlockedTransitionRepository transitionRepository,
        ILogger<WorkItemBlockedTransitionCloseHandler> logger)
        : IDomainEventHandler<WorkItemUnblocked>
    {
        public async Task HandleAsync(WorkItemUnblocked domainEvent, CancellationToken cancellationToken)
        {
            var openTransition = transitionRepository.GetByPredicate(
                t => t.WorkItemId == domainEvent.WorkItemId && t.LeftAt == null);

            if (openTransition == null)
            {
                logger.LogInformation(
                    "No open blocked transition found for WorkItem {WorkItemId}; skipping close",
                    domainEvent.WorkItemId);
                return;
            }

            openTransition.LeftAt = DateTime.UtcNow;
            transitionRepository.Update(openTransition);
            await transitionRepository.Save();
        }
    }
}
