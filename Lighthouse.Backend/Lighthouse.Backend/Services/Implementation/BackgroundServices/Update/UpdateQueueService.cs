namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    using Lighthouse.Backend.Services.Implementation.DatabaseManagement;
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
        private readonly ConcurrentDictionary<UpdateKey, TaskCompletionSource<bool>> awaiters = new();
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly DatabaseMaintenanceGate maintenanceGate;

        public UpdateQueueService(
            ILogger<UpdateQueueService> logger,
            IHubContext<UpdateNotificationHub> hubContext,
            ConcurrentDictionary<UpdateKey, UpdateStatus> updateStatuses,
            IServiceScopeFactory serviceScopeFactory,
            DatabaseMaintenanceGate maintenanceGate)
        {
            this.logger = logger;
            this.hubContext = hubContext;
            this.updateStatuses = updateStatuses;
            this.serviceScopeFactory = serviceScopeFactory;
            this.maintenanceGate = maintenanceGate;

            StartProcessingQueue();
        }

        public void EnqueueUpdate(UpdateType updateType, int id, Func<IServiceProvider, Task> updateTask)
        {
            var updateKey = new UpdateKey(updateType, id);

            if (maintenanceGate.ActiveOperationId != null)
            {
                logger.LogInformation("Update for {UpdateType} with ID {Id} skipped because a database {OperationType} operation is active.", updateType, id, maintenanceGate.ActiveOperationType);
                return;
            }

            var updateStatus = new UpdateStatus { UpdateType = updateType, Id = id, Status = UpdateProgress.Queued };
            if (!updateStatuses.TryAdd(updateKey, updateStatus))
            {
                logger.LogInformation("Update for {UpdateType} with ID {Id} is already queued or being processed.", updateType, id);
                return;
            }

            logger.LogInformation("Queuing Update for {UpdateType} with ID {Id}.", updateType, id);

            _ = NotifyListeners(updateKey, updateStatus);

            queue.Writer.TryWrite(ExecuteUpdateAsync(updateType, id, updateTask, updateKey, updateStatus));
        }

        public Task EnqueueAndAwaitAsync(UpdateType updateType, int id, Func<IServiceProvider, Task> updateTask, CancellationToken cancellationToken = default)
        {
            var updateKey = new UpdateKey(updateType, id);

            if (maintenanceGate.ActiveOperationId != null)
            {
                logger.LogInformation("Update for {UpdateType} with ID {Id} skipped because a database {OperationType} operation is active.", updateType, id, maintenanceGate.ActiveOperationType);
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var updateStatus = new UpdateStatus { UpdateType = updateType, Id = id, Status = UpdateProgress.Queued };

            if (!updateStatuses.TryAdd(updateKey, updateStatus))
            {
                logger.LogInformation("Update for {UpdateType} with ID {Id} is already queued; awaiting the in-flight completion.", updateType, id);
                if (awaiters.TryGetValue(updateKey, out var existing))
                {
                    return RegisterCancellation(existing.Task, cancellationToken);
                }

                return Task.CompletedTask;
            }

            awaiters[updateKey] = tcs;

            logger.LogInformation("Queuing Update for {UpdateType} with ID {Id}.", updateType, id);

            _ = NotifyListeners(updateKey, updateStatus);

            queue.Writer.TryWrite(ExecuteAwaitableUpdateAsync(updateType, id, updateTask, updateKey, updateStatus, tcs));

            return RegisterCancellation(tcs.Task, cancellationToken);
        }

        private static Task RegisterCancellation(Task task, CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled)
            {
                return task;
            }

            var observer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var registration = cancellationToken.Register(() => observer.TrySetCanceled(cancellationToken));

            _ = task.ContinueWith(t =>
            {
                registration.Dispose();
                if (t.IsFaulted)
                {
                    observer.TrySetException(t.Exception!.InnerExceptions);
                }
                else if (t.IsCanceled)
                {
                    observer.TrySetCanceled();
                }
                else
                {
                    observer.TrySetResult(true);
                }
            }, TaskScheduler.Default);

            return observer.Task;
        }

        private Func<Task> ExecuteUpdateAsync(UpdateType updateType, int id, Func<IServiceProvider, Task> updateTask, UpdateKey updateKey, UpdateStatus updateStatus)
        {
            return async () =>
            {
                updateStatus.Status = UpdateProgress.InProgress;

                try
                {
                    await ExecuteUpdateTask(updateTask);
                    updateStatus.Status = UpdateProgress.Completed;
                }
                catch (Exception ex)
                {
                    updateStatus.Status = UpdateProgress.Failed;
                    logger.LogError(ex, "Error processing update task for {UpdateType} with ID {Id}", updateType, id);
                }

                updateStatuses.TryRemove(updateKey, out _);
                await NotifyListeners(updateKey, updateStatus);
            };
        }

        private Func<Task> ExecuteAwaitableUpdateAsync(UpdateType updateType, int id, Func<IServiceProvider, Task> updateTask, UpdateKey updateKey, UpdateStatus updateStatus, TaskCompletionSource<bool> tcs)
        {
            return async () =>
            {
                updateStatus.Status = UpdateProgress.InProgress;

                try
                {
                    await ExecuteUpdateTask(updateTask);
                    updateStatus.Status = UpdateProgress.Completed;
                    tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    updateStatus.Status = UpdateProgress.Failed;
                    logger.LogError(ex, "Error processing update task for {UpdateType} with ID {Id}", updateType, id);
                    tcs.TrySetException(ex);
                }
                finally
                {
                    awaiters.TryRemove(updateKey, out _);
                    updateStatuses.TryRemove(updateKey, out _);
                    await NotifyListeners(updateKey, updateStatus);
                }
            };
        }

        private async Task ExecuteUpdateTask(Func<IServiceProvider, Task> updateTask)
        {
            using (var scope = serviceScopeFactory.CreateScope())
            {
                await updateTask(scope.ServiceProvider);
            }
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

        private async Task NotifyListeners(UpdateKey updateKey, UpdateStatus status)
        {
            await hubContext.Clients.Group(updateKey.ToString()).SendAsync(updateKey.ToString(), status);

            await hubContext.Clients.Group("GlobalUpdates").SendAsync("GlobalUpdateNotification");
        }
    }
}
