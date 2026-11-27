using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.WorkItems.Contracts;
using Lighthouse.Backend.Platform.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Modules.WorkItems.Application.Policies
{
    public class WorkItemBlockedTransitionCaptureHandler(
        IWorkItemBlockedTransitionRepository transitionRepository,
        TimeProvider timeProvider,
        ILogger<WorkItemBlockedTransitionCaptureHandler> logger)
        : IDomainEventHandler<WorkItemBlocked>
    {
        public async Task HandleAsync(WorkItemBlocked domainEvent, CancellationToken cancellationToken)
        {
            var existingOpen = transitionRepository.GetByPredicate(
                t => t.WorkItemId == domainEvent.WorkItemId && t.LeftAt == null);

            if (existingOpen != null)
            {
                logger.LogInformation(
                    "WorkItem {WorkItemId} already has an open blocked transition; skipping capture",
                    domainEvent.WorkItemId);
                return;
            }

            var transition = new WorkItemBlockedTransition
            {
                WorkItemId = domainEvent.WorkItemId,
                EnteredAt = timeProvider.GetUtcNow().UtcDateTime,
                LeftAt = null,
            };

            transitionRepository.Add(transition);
            await transitionRepository.Save();
        }
    }
}
