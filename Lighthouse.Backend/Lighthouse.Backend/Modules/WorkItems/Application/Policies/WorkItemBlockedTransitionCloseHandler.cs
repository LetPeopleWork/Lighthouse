using Lighthouse.Backend.Modules.WorkItems.Contracts;
using Lighthouse.Backend.Platform.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Modules.WorkItems.Application.Policies
{
    public class WorkItemBlockedTransitionCloseHandler(
        IWorkItemBlockedTransitionRepository transitionRepository,
        TimeProvider timeProvider,
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

            openTransition.LeftAt = timeProvider.GetUtcNow().UtcDateTime;
            transitionRepository.Update(openTransition);
            await transitionRepository.Save();
        }
    }
}
