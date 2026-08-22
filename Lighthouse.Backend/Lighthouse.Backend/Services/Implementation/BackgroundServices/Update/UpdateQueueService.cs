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
        private readonly ConcurrentDictionary<UpdateKey, HeldUpdate> heldUpdates = new();
        private readonly AsyncLocal<WriteBackRound?> roundBeingHandedOver = new();
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly WriteBackRoundContext roundContext;
        private readonly DatabaseMaintenanceGate maintenanceGate;
        private readonly Task processingTask;

        public UpdateQueueService(
            ILogger<UpdateQueueService> logger,
            IHubContext<UpdateNotificationHub> hubContext,
            IUpdateStatusStore statusStore,
            IUpdateExecutionLock executionLock,
            IUpdateCompletionNotifier completionNotifier,
            IServiceScopeFactory serviceScopeFactory,
            DatabaseMaintenanceGate maintenanceGate,
            WriteBackRoundContext roundContext)
        {
            this.logger = logger;
            this.hubContext = hubContext;
            this.statusStore = statusStore;
            this.executionLock = executionLock;
            this.completionNotifier = completionNotifier;
            this.serviceScopeFactory = serviceScopeFactory;
            this.maintenanceGate = maintenanceGate;
            this.roundContext = roundContext;

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

            if (IsBlockedByDatabaseMaintenance(updateKey))
            {
                return;
            }

            var updateStatus = QueuedStatusFor(updateKey);
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

            logger.LogDebug("Queuing Update for {UpdateType} with ID {Id}.", updateType, id);

            _ = NotifyListeners(updateKey, updateStatus);

            var round = RoundForNewWork();

            if (!queue.Writer.TryWrite(() => RunUpdateAsync(updateKey, updateTask, updateStatus, round)))
            {
                AbandonUnqueuedWork(updateKey, round);
            }
        }

        public Task EnqueueAndAwaitAsync(UpdateType updateType, int id, Func<IServiceProvider, Task> updateTask, CancellationToken cancellationToken = default)
        {
            var updateKey = new UpdateKey(updateType, id);

            if (IsBlockedByDatabaseMaintenance(updateKey))
            {
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var updateStatus = QueuedStatusFor(updateKey);

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

            logger.LogDebug("Queuing Update for {UpdateType} with ID {Id}.", updateType, id);

            _ = NotifyListeners(updateKey, updateStatus);

            var round = RoundForNewWork();

            if (!queue.Writer.TryWrite(() => RunAwaitableUpdateAsync(updateKey, updateTask, updateStatus, tcs, round)))
            {
                AbandonUnqueuedWork(updateKey, round);
                awaiters.TryRemove(updateKey, out _);
                tcs.TrySetResult(false);
            }

            return RegisterCancellation(tcs.Task, cancellationToken);
        }

        private bool IsBlockedByDatabaseMaintenance(UpdateKey updateKey)
        {
            if (maintenanceGate.ActiveOperationId == null)
            {
                return false;
            }

            logger.LogInformation("Update for {UpdateType} with ID {Id} skipped because a database {OperationType} operation is active.", updateKey.UpdateType, updateKey.Id, maintenanceGate.ActiveOperationType);
            return true;
        }

        /// <summary>
        /// Hands back everything queuing this work claimed, for when the queue is already closed and the
        /// work will therefore never run. Left as it was, the key would stay marked as work in progress
        /// that nobody is doing: Lighthouse would keep reporting a refresh that never finishes, and
        /// anything parked until that key clears would stay parked for good.
        /// </summary>
        private void AbandonUnqueuedWork(UpdateKey updateKey, WriteBackRound round)
        {
            logger.LogInformation("Update for {UpdateType} with ID {Id} was not queued because the update queue is closing.", updateKey.UpdateType, updateKey.Id);

            statusStore.Remove(updateKey);
            round.Leave();
        }

        private static UpdateStatus QueuedStatusFor(UpdateKey updateKey)
        {
            return new UpdateStatus { UpdateType = updateKey.UpdateType, Id = updateKey.Id, Status = UpdateProgress.Queued };
        }

        /// <summary>
        /// Which refresh round work that starts now belongs to. Work an update execution asks for joins
        /// the round of the execution that asked, so a portfolio refresh and the forecast it triggers
        /// reach the work tracking system in one conversation rather than two. Work let go by a hold takes
        /// over the place that hold was keeping for it. Anything else opens a round of its own.
        /// </summary>
        private WriteBackRound RoundForNewWork()
        {
            if (roundBeingHandedOver.Value is { } handedOver)
            {
                roundBeingHandedOver.Value = null;
                return handedOver;
            }

            var runningRound = roundContext.Current;

            if (runningRound == null)
            {
                return new WriteBackRound();
            }

            runningRound.Join();
            return runningRound;
        }

        public void HoldUntilQueuedWorkClears(UpdateKey heldFor, IReadOnlyCollection<UpdateKey> waitingOn, Action onQueuedWorkCleared)
        {
            heldUpdates[heldFor] = new HeldUpdate(waitingOn, onQueuedWorkCleared, RoundForNewWork());

            // The work being waited on can finish between the caller looking at it and this line. Releases
            // only ever fire when something leaves the queue, so nothing would come along afterwards to let
            // this one out - check once more now that it is actually held.
            ReleaseClearedHolds();
        }

        public bool IsHeld(UpdateKey heldFor)
        {
            return heldUpdates.ContainsKey(heldFor);
        }

        private void ReleaseClearedHolds()
        {
            foreach (var heldFor in heldUpdates.Keys)
            {
                if (!heldUpdates.TryGetValue(heldFor, out var held) || statusStore.HasQueuedWork(held.WaitingOn))
                {
                    continue;
                }

                if (heldUpdates.TryRemove(heldFor, out var released))
                {
                    logger.LogInformation("Releasing the held update for {UpdateType} with ID {Id}; the work it waited for has left the queue.", heldFor.UpdateType, heldFor.Id);
                    ReleaseIntoItsRound(released);
                }
            }
        }

        /// <summary>
        /// A hold keeps its round open, because what that round already resolved has to travel to the work
        /// tracking system together with whatever the held work produces. Handing the place over to the
        /// released work, rather than adding a second one and giving the hold's back, means the round never
        /// looks finished while the work it waited for is still being arranged.
        /// </summary>
        private void ReleaseIntoItsRound(HeldUpdate held)
        {
            roundBeingHandedOver.Value = held.Round;

            try
            {
                held.Release();
            }
            finally
            {
                // The released work can end up queuing nothing at all - a database operation is running,
                // or the same key was picked up elsewhere while the hold waited. Nobody took the place
                // over, so give it back: a round that keeps counting a run that never came never finishes,
                // and everything it had resolved is silently never written to the work tracking system.
                if (ReferenceEquals(roundBeingHandedOver.Value, held.Round))
                {
                    held.Round.Leave();
                }

                roundBeingHandedOver.Value = null;
            }
        }

        /// <summary>
        /// Letting held work go runs a callback the caller supplied, and that callback reads from the
        /// database. This runs between an update being marked finished and everyone being told it
        /// finished, so a failure in it would otherwise mean nobody is ever told - and a caller on another
        /// replica waiting for this update would wait until its own timeout instead.
        /// </summary>
        private void ReleaseClearedHoldsWithoutFailingTheUpdate()
        {
            try
            {
                ReleaseClearedHolds();
            }
            // Anything the caller's callback throws is caught, because no failure in it may stop this
            // update from being reported as finished.
#pragma warning disable CA1031
            catch (Exception exception)
#pragma warning restore CA1031
            {
                logger.LogError(exception, "Failed to release held updates after an update finished: {Exception}", exception.Message);
            }
        }

        private sealed record HeldUpdate(IReadOnlyCollection<UpdateKey> WaitingOn, Action Release, WriteBackRound Round);

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

        private async Task RunUpdateAsync(UpdateKey updateKey, Func<IServiceProvider, Task> updateTask, UpdateStatus updateStatus, WriteBackRound round)
        {
            await using var executionScope = await executionLock.AcquireAsync(updateKey);

            statusStore.Advance(updateKey, UpdateProgress.InProgress);

            UpdateProgress terminalProgress;
            try
            {
                await ExecuteUpdateTask(updateTask, round);
                terminalProgress = UpdateProgress.Completed;
            }
            catch (Exception ex)
            {
                terminalProgress = UpdateProgress.Failed;
                logger.LogError(ex, "Error processing update task for {UpdateType} with ID {Id}", updateKey.UpdateType, updateKey.Id);
            }

            // The follow-up is decided BEFORE the key is marked terminal. `HasActiveWork` counts only
            // Queued and InProgress, so advancing to Completed first and requeueing after opens exactly
            // the idle window the coalescing exists to close - two statements wide, and a CI run has
            // already landed in it.
            if (TryScheduleRerun(updateKey, updateStatus))
            {
                return;
            }

            var terminalStatus = statusStore.Advance(updateKey, terminalProgress) ?? updateStatus;
            statusStore.Remove(updateKey);

            // A trigger can land in the window between the check above and this removal: it saw the key
            // still admitted, so it parked a rerun instead of admitting its own. Re-check now that the
            // key is gone, otherwise that trigger would be lost after all.
            if (pendingReruns.TryRemove(updateKey, out var lateRerun))
            {
                EnqueueUpdate(updateKey.UpdateType, updateKey.Id, lateRerun);
            }

            // Reached on the failure path too: the catch above only records the outcome. Work held
            // behind a key that failed must still be let go, or a single failing refresh would strand
            // it until something unrelated happens to poke the same key again.
            ReleaseClearedHoldsWithoutFailingTheUpdate();

            await completionNotifier.PublishCompletionAsync(updateKey);
            await NotifyListeners(updateKey, terminalStatus);
        }

        private bool TryScheduleRerun(UpdateKey updateKey, UpdateStatus updateStatus)
        {
            if (!pendingReruns.TryRemove(updateKey, out var rerun))
            {
                return false;
            }

            // Requeue before writing so the key never leaves the store: callers polling for "no active
            // work" must not observe idle between the run that just finished and its follow-up, or they
            // would read exactly the stale state the follow-up is about to correct.
            statusStore.Requeue(updateKey);

            var round = RoundForNewWork();

            if (queue.Writer.TryWrite(() => RunUpdateAsync(updateKey, rerun, updateStatus, round)))
            {
                logger.LogInformation("Running the coalesced follow-up update for {UpdateType} with ID {Id}.", updateKey.UpdateType, updateKey.Id);
                return true;
            }

            // The queue is closed (shutdown drain). Give the key back its terminal status so the caller
            // finishes it normally instead of leaving it admitted and permanently blocking re-admission.
            statusStore.Advance(updateKey, UpdateProgress.Completed);
            return false;
        }

        private async Task RunAwaitableUpdateAsync(UpdateKey updateKey, Func<IServiceProvider, Task> updateTask, UpdateStatus updateStatus, TaskCompletionSource<bool> tcs, WriteBackRound round)
        {
            await using var executionScope = await executionLock.AcquireAsync(updateKey);

            statusStore.Advance(updateKey, UpdateProgress.InProgress);

            UpdateStatus terminalStatus = updateStatus;
            try
            {
                await ExecuteUpdateTask(updateTask, round);
                terminalStatus = statusStore.Advance(updateKey, UpdateProgress.Completed) ?? updateStatus;
                tcs.TrySetResult(true);
            }
            catch (Exception ex)
            {
                terminalStatus = statusStore.Advance(updateKey, UpdateProgress.Failed) ?? terminalStatus;
                logger.LogError(ex, "Error processing update task for {UpdateType} with ID {Id}", updateKey.UpdateType, updateKey.Id);
                tcs.TrySetException(ex);
            }
            finally
            {
                awaiters.TryRemove(updateKey, out _);
                statusStore.Remove(updateKey);
                ReleaseClearedHoldsWithoutFailingTheUpdate();
                await completionNotifier.PublishCompletionAsync(updateKey);
                await NotifyListeners(updateKey, terminalStatus);
            }
        }

        private async Task ExecuteUpdateTask(Func<IServiceProvider, Task> updateTask, WriteBackRound round)
        {
            // Set before the scope exists, so anything the update resolves out of that scope - the
            // write-back collector above all - already knows which round it is working for. The value is
            // confined to this call and does not escape to the caller: work the queue starts once this has
            // returned - the coalesced follow-up, say - opens a round of its own rather than joining this
            // one, and work started from outside an update sees no round at all.
            roundContext.Current = round;

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
