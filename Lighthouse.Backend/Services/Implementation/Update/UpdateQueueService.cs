namespace Lighthouse.Backend.Services.Implementation.Update
{
    using Lighthouse.Backend.Services.Interfaces.Update;
    using Microsoft.AspNetCore.SignalR;
    using System.Collections.Concurrent;
    using System.Threading.Channels;

    public class UpdateQueueService : IUpdateQueueService
    {
        private readonly Channel<Func<Task>> queue = Channel.CreateUnbounded<Func<Task>>();
        private readonly ILogger<UpdateQueueService> logger;
        private readonly IHubContext<UpdateNotificationHub> hubContext;
        private readonly ConcurrentDictionary<UpdateKey, UpdateStatus> updateStatuses;
        private readonly IServiceScopeFactory serviceScopeFactory;

        public UpdateQueueService(
            ILogger<UpdateQueueService> logger,
            IHubContext<UpdateNotificationHub> hubContext,
            ConcurrentDictionary<UpdateKey, UpdateStatus> updateStatuses,
            IServiceScopeFactory serviceScopeFactory)
        {
            this.logger = logger;
            this.hubContext = hubContext;
            this.updateStatuses = updateStatuses;
            this.serviceScopeFactory = serviceScopeFactory;

            StartProcessingQueue();
        }

        public void EnqueueUpdate(UpdateType updateType, int id, Func<IServiceProvider, Task> updateTask)
        {
            var updateKey = new UpdateKey(updateType, id);

            if (updateStatuses.ContainsKey(updateKey))
            {
                logger.LogInformation("Update for {UpdateType} with ID {Id} is already queued or being processed.", updateType, id);
                return;
            }

            logger.LogInformation("Queuing Update for {UpdateType} with ID {Id}.", updateType, id);
            var updateStatus = new UpdateStatus { UpdateType = updateType, Id = id, Status = UpdateProgress.Queued };
            updateStatuses[updateKey] = updateStatus;

            queue.Writer.TryWrite(async () =>
            {
                updateStatus.Status = UpdateProgress.InProgress;

                try
                {
                    using (var scope = serviceScopeFactory.CreateScope())
                    {
                        await updateTask(scope.ServiceProvider);
                    }

                    updateStatus.Status = UpdateProgress.Completed;
                }
                catch (Exception ex)
                {
                    updateStatus.Status = UpdateProgress.Completed;
                    logger.LogError(ex, "Error processing update task for {UpdateType} with ID {Id}", updateType, id);
                }

                updateStatuses.TryRemove(updateKey, out _);
                await hubContext.Clients.Group(updateKey.ToString()).SendAsync("UpdateStatusChanged", updateStatus);
            });

            hubContext.Clients.Group(updateKey.ToString()).SendAsync("UpdateStatusChanged", updateStatus);
        }

        private void StartProcessingQueue()
        {
            Task.Run(async () =>
            {
                await foreach (var updateTask in queue.Reader.ReadAllAsync())
                {
                    try
                    {
                        await updateTask();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing update task");
                    }
                }
            });
        }
    }

}
