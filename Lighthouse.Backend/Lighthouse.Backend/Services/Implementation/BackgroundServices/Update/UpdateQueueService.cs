namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    using Lighthouse.Backend.Services.Implementation.DatabaseManagement;
    using Lighthouse.Backend.Services.Interfaces.Update;
    using Microsoft.AspNetCore.SignalR;
    using System.Collections.Concurrent;
    using System.Threading.Channels;

    public class UpdateQueueService : IUpdateQueueService, IDisposable
    {
        private readonly Channel<Func<Task>> queue = Channel.CreateUnbounded<Func<Task>>();
        private readonly ILogger<UpdateQueueService> logger;
        private readonly IHubContext<UpdateNotificationHub> hubContext;
        private readonly IUpdateStatusStore statusStore;
        private readonly IUpdateExecutionLock executionLock;
        private readonly IUpdateCompletionNotifier completionNotifier;
        private readonly IDisposable completionSubscription;
        private readonly ConcurrentDictionary<UpdateKey, TaskCompletionSource<bool>> awaiters = new();
        private readonly ConcurrentDictionary<UpdateKey, Func<IServiceProvider, Task>> pendingReruns = new();
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly DatabaseMaintenanceGate maintenanceGate;
        private readonly Task processingTask;

        public UpdateQueueService(
            ILogger<UpdateQueueService> logger,
            IHubContext<UpdateNotificationHub> hubContext,
            IUpdateStatusStore statusStore,
            IUpdateExecutionLock executionLock,
            IUpdateCompletionNotifier completionNotifier,
            IServiceScopeFactory serviceScopeFactory,
            DatabaseMaintenanceGate maintenanceGate)
        {
            this.logger = logger;
            this.hubContext = hubContext;
            this.statusStore = statusStore;
            this.executionLock = executionLock;
            this.completionNotifier = completionNotifier;
            this.serviceScopeFactory = serviceScopeFactory;
            this.maintenanceGate = maintenanceGate;

            completionSubscription = completionNotifier.Subscribe(ReleaseAwaiter);
            processingTask = StartProcessingQueue();
        }

        private void ReleaseAwaiter(UpdateKey updateKey)
        {
            if (awaiters.TryRemove(updateKey, out var awaiter))
            {
                awaiter.TrySetResult(true);
            }
        }

        public async Task DrainAsync(CancellationToken cancellationToken = default)
        {
            queue.Writer.TryComplete();

            try
            {
                await processingTask.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException ex)
            {
                logger.LogWarning(ex, "Update queue drain exceeded the shutdown timeout; abandoning in-flight work.");
            }
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
            if (!statusStore.TryAdmit(updateKey, updateStatus))
            {
                // The in-flight run read its state before this trigger was raised, so it cannot reflect
                // whatever write caused it (blocked rules saved mid-refresh, for example). Dropping the
                // trigger loses that intent until the next periodic refresh; instead remember the newest
                // task and run it once when the in-flight run finishes. Repeated triggers collapse into
                // a single follow-up, because that follow-up already reads the newest state.
                pendingReruns[updateKey] = updateTask;
                logger.LogInformation("Update for {UpdateType} with ID {Id} is already queued or being processed - scheduling a single follow-up run.", updateType, id);
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

            if (!statusStore.TryAdmit(updateKey, updateStatus))
            {
                logger.LogInformation("Update for {UpdateType} with ID {Id} is already queued; awaiting the in-flight completion.", updateType, id);
                if (awaiters.TryGetValue(updateKey, out var existing))
                {
                    return RegisterCancellation(existing.Task, cancellationToken);
                }

                if (completionNotifier.IsDistributed)
                {
                    var crossPodAwaiter = awaiters.GetOrAdd(updateKey, _ => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously));
                    return RegisterCancellation(crossPodAwaiter.Task, cancellationToken);
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
                    observer.TrySetException(t.Exception.InnerExceptions);
                }
                else if (t.IsCanceled)
                {
                    observer.TrySetCanceled(cancellationToken);
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
                await using var executionScope = await executionLock.AcquireAsync(updateKey);

                statusStore.Advance(updateKey, UpdateProgress.InProgress);

                UpdateStatus terminalStatus;
                try
                {
                    await ExecuteUpdateTask(updateTask);
                    terminalStatus = statusStore.Advance(updateKey, UpdateProgress.Completed) ?? updateStatus;
                }
                catch (Exception ex)
                {
                    terminalStatus = statusStore.Advance(updateKey, UpdateProgress.Failed) ?? updateStatus;
                    logger.LogError(ex, "Error processing update task for {UpdateType} with ID {Id}", updateType, id);
                }

                if (TryScheduleRerun(updateType, id, updateKey, updateStatus))
                {
                    return;
                }

                statusStore.Remove(updateKey);

                // A trigger can land in the window between the check above and this removal: it saw the key
                // still admitted, so it parked a rerun instead of admitting its own. Re-check now that the
                // key is gone, otherwise that trigger would be lost after all.
                if (pendingReruns.TryRemove(updateKey, out var lateRerun))
                {
                    EnqueueUpdate(updateType, id, lateRerun);
                }

                await completionNotifier.PublishCompletionAsync(updateKey);
                await NotifyListeners(updateKey, terminalStatus);
            };
        }

        private bool TryScheduleRerun(UpdateType updateType, int id, UpdateKey updateKey, UpdateStatus updateStatus)
        {
            if (!pendingReruns.TryRemove(updateKey, out var rerun))
            {
                return false;
            }

            // Requeue before writing so the key never leaves the store: callers polling for "no active
            // work" must not observe idle between the run that just finished and its follow-up, or they
            // would read exactly the stale state the follow-up is about to correct.
            statusStore.Requeue(updateKey);

            if (queue.Writer.TryWrite(ExecuteUpdateAsync(updateType, id, rerun, updateKey, updateStatus)))
            {
                logger.LogInformation("Running the coalesced follow-up update for {UpdateType} with ID {Id}.", updateType, id);
                return true;
            }

            // The queue is closed (shutdown drain). Give the key back its terminal status so the caller
            // finishes it normally instead of leaving it admitted and permanently blocking re-admission.
            statusStore.Advance(updateKey, UpdateProgress.Completed);
            return false;
        }

        private Func<Task> ExecuteAwaitableUpdateAsync(UpdateType updateType, int id, Func<IServiceProvider, Task> updateTask, UpdateKey updateKey, UpdateStatus updateStatus, TaskCompletionSource<bool> tcs)
        {
            return async () =>
            {
                await using var executionScope = await executionLock.AcquireAsync(updateKey);

                statusStore.Advance(updateKey, UpdateProgress.InProgress);

                UpdateStatus terminalStatus = updateStatus;
                try
                {
                    await ExecuteUpdateTask(updateTask);
                    terminalStatus = statusStore.Advance(updateKey, UpdateProgress.Completed) ?? updateStatus;
                    tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    terminalStatus = statusStore.Advance(updateKey, UpdateProgress.Failed) ?? terminalStatus;
                    logger.LogError(ex, "Error processing update task for {UpdateType} with ID {Id}", updateType, id);
                    tcs.TrySetException(ex);
                }
                finally
                {
                    awaiters.TryRemove(updateKey, out _);
                    statusStore.Remove(updateKey);
                    await completionNotifier.PublishCompletionAsync(updateKey);
                    await NotifyListeners(updateKey, terminalStatus);
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

        private Task StartProcessingQueue()
        {
            return Task.Run(async () =>
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                completionSubscription.Dispose();
            }
        }
    }
}
