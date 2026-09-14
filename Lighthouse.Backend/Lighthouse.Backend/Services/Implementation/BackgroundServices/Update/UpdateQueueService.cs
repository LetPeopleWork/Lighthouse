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
        private readonly UpdateCancellationContext cancellationContext;
        private readonly IUpdateCancellationNotifier cancellationNotifier;
        private readonly IDisposable cancellationSubscription;

        /// <summary>
        /// One source per admitted key, so cancelling one entity's refresh cannot reach another's. Created
        /// when the key is admitted and disposed when it leaves the store, which is the same lifetime the
        /// status has.
        /// </summary>
        private readonly ConcurrentDictionary<UpdateKey, CancellationTokenSource> cancellations = new();
        private readonly DatabaseMaintenanceGate maintenanceGate;
        private readonly Task processingTask;

        public UpdateQueueService(
            ILogger<UpdateQueueService> logger,
            IHubContext<UpdateNotificationHub> hubContext,
            UpdateSubstrate substrate,
            IServiceScopeFactory serviceScopeFactory,
            DatabaseMaintenanceGate maintenanceGate,
            WriteBackRoundContext roundContext,
            UpdateCancellationContext cancellationContext)
        {
            this.logger = logger;
            this.hubContext = hubContext;
            statusStore = substrate.StatusStore;
            executionLock = substrate.ExecutionLock;
            completionNotifier = substrate.CompletionNotifier;
            this.serviceScopeFactory = serviceScopeFactory;
            this.maintenanceGate = maintenanceGate;
            this.roundContext = roundContext;
            this.cancellationContext = cancellationContext;
            cancellationNotifier = substrate.CancellationNotifier;

            completionSubscription = completionNotifier.Subscribe(ReleaseAwaiter);

            // Subscribed here rather than on demand because the ask arrives from whichever replica took the
            // operator's click, which is usually not this one.
            cancellationSubscription = cancellationNotifier.Subscribe(StopLocally);
            processingTask = StartProcessingQueue();
        }

        private void ReleaseAwaiter(UpdateKey updateKey)
        {
            if (awaiters.TryRemove(updateKey, out var awaiter))
            {
                awaiter.TrySetResult(true);
            }
        }

        public Task CancelAsync(UpdateKey key)
        {
            // Published rather than acted on here: the work is usually running on another replica, and this
            // one holds no source for it. Accepted whatever state the key is in, including gone - the row
            // was drawn before it was clicked.
            return cancellationNotifier.PublishCancellationAsync(key);
        }

        private void AdmitCancellationFor(UpdateKey key)
        {
            var admitted = new CancellationTokenSource();
            if (!cancellations.TryAdd(key, admitted))
            {
                admitted.Dispose();
            }
        }

        /// <summary>
        /// Disposed alongside the key leaving the store. A source kept past that would mean a cancel asked
        /// for now could stop work admitted later under the same key - which is a different refresh.
        /// </summary>
        private void ForgetCancellationFor(UpdateKey key)
        {
            if (cancellations.TryRemove(key, out var finished))
            {
                finished.Dispose();
            }
        }

        private CancellationToken TokenFor(UpdateKey key)
        {
            if (!cancellations.TryGetValue(key, out var cancellation))
            {
                return CancellationToken.None;
            }

            try
            {
                return cancellation.Token;
            }
            catch (ObjectDisposedException)
            {
                // The same race as StopLocally, read from the other side. An uncancellable run is a better
                // outcome than one that throws here, outside the try below, and leaves its key admitted
                // for good.
                return CancellationToken.None;
            }
        }

        /// <summary>
        /// The run this belongs to can finish and dispose its source while a cancel is on its way in. That
        /// is the ordinary case the route calls idempotent - it finished while the operator was reading -
        /// so it must not become a 500 in front of somebody who did nothing wrong.
        /// </summary>
        private void StopLocally(UpdateKey key)
        {
            if (!cancellations.TryGetValue(key, out var cancellation))
            {
                return;
            }

            try
            {
                cancellation.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // The work ended on its own between the lookup and here. Nothing left to stop.
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

            // Before admission, not after: the key reaches the task list the instant TryAdmit succeeds, and
            // a cancel landing in the gap would find no source and silently do nothing.
            AdmitCancellationFor(updateKey);

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

            AdmitCancellationFor(updateKey);

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
            ForgetCancellationFor(updateKey);
            round.Leave();
        }

        /// <summary>
        /// A round is left by the flush at the end of the update task, so a run cancelled before that task
        /// ever started has joined a round that nothing will leave. The round then never finishes and every
        /// write-back staged by the OTHER work sharing it is dropped without a word - one entity cancelled
        /// destroying another entity results. AbandonUnqueuedWork leaves it for the same reason.
        /// </summary>
        private static void LeaveTheRoundNobodyElseWill(WriteBackRound round, bool startedRunning)
        {
            if (!startedRunning)
            {
                round.Leave();
            }
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
            // Deliberately not the update's own token: cancelling a refresh must not abandon the wait for
            // the lock that keeps two replicas from running the same key at once.
            await using var executionScope = await executionLock.AcquireAsync(updateKey, CancellationToken.None);

            var cancellation = TokenFor(updateKey);
            statusStore.Advance(updateKey, UpdateProgress.InProgress);

            UpdateProgress terminalProgress;
            var startedRunning = false;
            try
            {
                // Checked before anything is asked of the tracker. Work cancelled while it was still
                // waiting its turn is the one case where stopping it can be absolute, and spending a rate
                // limit an operator cancelled to protect would give that away for nothing.
                cancellation.ThrowIfCancellationRequested();

                startedRunning = true;
                await ExecuteUpdateTask(updateTask, round, cancellation);
                terminalProgress = UpdateProgress.Completed;
            }
            catch (OperationCanceledException stopped) when (cancellation.IsCancellationRequested)
            {
                terminalProgress = UpdateProgress.Cancelled;
                logger.LogInformation(stopped, "Update for {UpdateType} with ID {Id} was cancelled.", updateKey.UpdateType, updateKey.Id);

                LeaveTheRoundNobodyElseWill(round, startedRunning);
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
                // The follow-up has already put the key back to Queued, so the store must not be advanced
                // here - but the run that just ended still has to say how it ended. Returning silently is
                // the same defect Bug #5788 fixed one layer up, wearing a different hat: a refresh breaks
                // and the last thing the browser was told is that it was running.
                await NotifyListeners(updateKey, HowThatRunEnded(updateStatus, terminalProgress));
                return;
            }

            var terminalStatus = statusStore.Advance(updateKey, terminalProgress) ?? updateStatus;
            statusStore.Remove(updateKey);
            ForgetCancellationFor(updateKey);

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

        /// <summary>
        /// A copy, because the follow-up is already using the entry this was built from - in the in-process
        /// store it is the very object the queue is about to advance again, so saying how the last run ended
        /// must not overwrite where the next one has got to.
        /// </summary>
        private static UpdateStatus HowThatRunEnded(UpdateStatus updateStatus, UpdateProgress terminalProgress)
        {
            return new UpdateStatus
            {
                UpdateType = updateStatus.UpdateType,
                Id = updateStatus.Id,
                Status = terminalProgress,
                QueuedAt = updateStatus.QueuedAt,
                StartedAt = updateStatus.StartedAt,
            };
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

            // New work, so a new source. The run that just ended may have been cancelled, and the follow-up
            // carries a newer intent than the one that was stopped - inheriting a cancelled token would
            // drop that intent silently, which is the opposite of what the coalescing exists to do.
            ForgetCancellationFor(updateKey);
            AdmitCancellationFor(updateKey);

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
            await using var executionScope = await executionLock.AcquireAsync(updateKey, CancellationToken.None);

            var cancellation = TokenFor(updateKey);
            statusStore.Advance(updateKey, UpdateProgress.InProgress);

            UpdateStatus terminalStatus = updateStatus;
            var startedRunning = false;
            try
            {
                cancellation.ThrowIfCancellationRequested();

                startedRunning = true;

                await ExecuteUpdateTask(updateTask, round, cancellation);
                terminalStatus = statusStore.Advance(updateKey, UpdateProgress.Completed) ?? updateStatus;
                tcs.TrySetResult(true);
            }
            catch (OperationCanceledException stopped) when (cancellation.IsCancellationRequested)
            {
                terminalStatus = statusStore.Advance(updateKey, UpdateProgress.Cancelled) ?? terminalStatus;
                logger.LogInformation(stopped, "Update for {UpdateType} with ID {Id} was cancelled.", updateKey.UpdateType, updateKey.Id);

                LeaveTheRoundNobodyElseWill(round, startedRunning);

                // Cancelled, not done. An awaiting caller gets a Task with no result to inspect, so
                // completing it at all lets a delete that never ran answer 204 to the browser and vanish
                // from the list while its row is still in the database.
                tcs.TrySetCanceled(cancellation);
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
                ForgetCancellationFor(updateKey);
                ReleaseClearedHoldsWithoutFailingTheUpdate();
                await completionNotifier.PublishCompletionAsync(updateKey);
                await NotifyListeners(updateKey, terminalStatus);
            }
        }

        private async Task ExecuteUpdateTask(Func<IServiceProvider, Task> updateTask, WriteBackRound round, CancellationToken cancellation)
        {
            // Set before the scope exists, so anything the update resolves out of that scope - the
            // write-back collector above all - already knows which round it is working for. The value is
            // confined to this call and does not escape to the caller: work the queue starts once this has
            // returned - the coalesced follow-up, say - opens a round of its own rather than joining this
            // one, and work started from outside an update sees no round at all.
            roundContext.Current = round;

            // Same seam, same reason: anything resolved out of the scope below - the connector's paging
            // loops above all - can ask whether it has been told to stop without every signature between
            // here and there learning about it.
            cancellationContext.Current = cancellation;

            using (var scope = serviceScopeFactory.CreateScope())
            {
                await updateTask(scope.ServiceProvider);
            }
        }

        private Task StartProcessingQueue()
        {
            // The loop below is the queue itself, not an update: it has to outlive every token any update
            // carries, or cancelling one refresh would stop the instance processing any others.
            return Task.Run(async () =>
            {
                // The reader outlives any one update, so it takes no update's token. It ends when the
                // channel completes, which is what shutdown does.
                await foreach (var updateTask in queue.Reader.ReadAllAsync(CancellationToken.None))
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
            }, CancellationToken.None);
        }

        private async Task NotifyListeners(UpdateKey updateKey, UpdateStatus status)
        {
            // Telling the browser how a run ended is not part of the run. A cancelled update still owes its
            // listeners the last word, so this must not inherit the token that just stopped it.
            await hubContext.Clients.Group(updateKey.ToString()).SendAsync(updateKey.ToString(), status, CancellationToken.None);

            await hubContext.Clients.Group("GlobalUpdates").SendAsync("GlobalUpdateNotification", CancellationToken.None);
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
                cancellationSubscription.Dispose();

                foreach (var outstanding in cancellations.Values)
                {
                    outstanding.Dispose();
                }

                cancellations.Clear();
            }
        }
    }
}
