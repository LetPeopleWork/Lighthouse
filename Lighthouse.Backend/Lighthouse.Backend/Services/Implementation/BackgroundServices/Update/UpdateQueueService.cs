namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    using Lighthouse.Backend.Services.Implementation.DatabaseManagement;
    using Lighthouse.Backend.Services.Interfaces;
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
        private readonly IDisposable cancelAllSubscription;

        /// <summary>
        /// One source per admitted key, so cancelling one entity refresh cannot reach another. Its lifetime
        /// is the key's own: created as the key is admitted, disposed as it leaves the store.
        /// </summary>
        private readonly AdmittedCancellations cancellations = new();

        /// <summary>
        /// There is one source per key and whoever admits the key runs with it, whichever caller created it.
        /// Creating or dropping a source and admitting or retiring its key therefore happen as one step, or a
        /// caller that loses the race to admit can throw away the source the winner is about to run with,
        /// leaving a refresh that nothing can stop.
        /// </summary>
        private readonly Lock admission = new();
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
            cancellationSubscription = cancellationNotifier.Subscribe(StopItAndSaySoIfItHasNotStarted);
            cancelAllSubscription = cancellationNotifier.SubscribeToCancelAll(StopEverythingButRemovals);
            processingTask = StartProcessingQueue();
        }

        private void ReleaseAwaiter(UpdateKey updateKey)
        {
            if (awaiters.TryRemove(updateKey, out var awaiter))
            {
                awaiter.TrySetResult(true);
            }
        }

        /// <summary>
        /// Stopping work that has not started yet is absolute, so the list should say so at once rather
        /// than when the queue happens to reach it. The reader is a single sequential loop, so "when it
        /// reaches it" is however long the refresh ahead of it takes — an operator who cancels something
        /// queued behind a slow refresh is told the cancel was accepted and then watches the row sit there.
        ///
        /// Only work that is still waiting is marked here. Work already running keeps its row, because it
        /// is genuinely still running: its token has just been cancelled and it stops at the connector's
        /// next round trip, and its own terminal path writes the outcome. Marking that one terminal early
        /// would take it out of <c>HasActiveWork</c> while it is still talking to a tracker, and things
        /// that wait for the instance to go idle would stop waiting.
        ///
        /// Whether it has started is asked and acted on in one step, by the store. Asked here, the answer
        /// could be out of date by the next line: the queue's reader is starting work while this runs.
        ///
        /// The check the reader already makes on the way in stays as the backstop: this advances the row,
        /// it does not replace the mechanism that actually stops the work.
        /// </summary>
        private void StopItAndSaySoIfItHasNotStarted(UpdateKey key)
        {
            cancellations.Stop(key);

            var cancelled = statusStore.CancelIfStillWaiting(key);

            if (cancelled != null)
            {
                _ = TellListenersWithoutFailingTheCancel(key, cancelled);
            }
        }

        /// <summary>
        /// The subscriber is synchronous — it is called from whichever notifier delivered the cancel — and
        /// telling the browser is not what makes the cancel true. A push that fails must not leave the work
        /// uncancelled, so this is dispatched rather than awaited and swallows its own failure.
        /// </summary>
        private async Task TellListenersWithoutFailingTheCancel(UpdateKey key, UpdateStatus status)
        {
            try
            {
                await NotifyListeners(key, status);
            }
#pragma warning disable CA1031 // the cancel has already happened; a failed push must not undo it
            catch (Exception exception)
#pragma warning restore CA1031
            {
                logger.LogWarning(exception, "Telling listeners that {UpdateType} with ID {Id} was cancelled failed", key.UpdateType, key.Id);
            }
        }

        public Task CancelAsync(UpdateKey key)
        {
            // Published rather than acted on here: the work is usually running on another replica, and this
            // one holds no source for it. Accepted whatever state the key is in, including gone - the row
            // was drawn before it was clicked.
            return cancellationNotifier.PublishCancellationAsync(key);
        }

        public Task CancelAllAsync()
        {
            // Published for the same reason a single cancel is: most of what the list shows may be running
            // on another replica, and only the one that admitted it can stop it.
            return cancellationNotifier.PublishCancelAllAsync();
        }

        /// <summary>
        /// Every refresh this instance was asked to run goes through the same path a single cancel takes, so
        /// a waiting row leaves the list at once and a running one stops at its next page. A removal is left
        /// to finish, for the same reason a single cancel refuses one.
        ///
        /// The order matters. A stopped refresh that finds a follow-up parked behind it starts that
        /// follow-up, and a refresh leaving the queue lets go whatever was held behind it, so both are
        /// dropped before anything is stopped - otherwise what the operator cancelled comes straight back.
        ///
        /// Only what this instance holds is stopped, because only this instance can stop it; every other
        /// instance hears the same request and stops its own. Work asked for after this has run starts
        /// with a fresh source and runs as usual.
        /// </summary>
        private void StopEverythingButRemovals()
        {
            DropParkedFollowUpsButRemovals();
            var droppedHolds = DropEveryHold();

            var admitted = cancellations.AdmittedKeys.ToList();
            var refreshes = admitted.Where(key => UpdateCancellationPolicy.CanBeStopped(key.UpdateType)).ToList();

            foreach (var key in refreshes)
            {
                StopItAndSaySoIfItHasNotStarted(key);
            }

            logger.LogInformation("Cancel all asked {StoppedCount} refreshes to stop, let go {HeldCount} held updates without running them, and left {RemovalCount} removals to finish.", refreshes.Count, droppedHolds, admitted.Count - refreshes.Count);
        }

        private void DropParkedFollowUpsButRemovals()
        {
            foreach (var key in pendingReruns.Keys.Where(key => UpdateCancellationPolicy.CanBeStopped(key.UpdateType)))
            {
                if (pendingReruns.TryRemove(key, out _))
                {
                    logger.LogDebug("Dropped the follow-up for {UpdateType} with ID {Id}.", key.UpdateType, key.Id);
                }
            }
        }

        /// <summary>
        /// A hold keeps a place in its round, so the round would never finish and whatever it already resolved
        /// would never be written back. Giving the place back lets the round flush with what it has, and when
        /// the hold was the last thing the round waited for, the flush happens here.
        /// </summary>
        private int DropEveryHold()
        {
            var dropped = 0;

            foreach (var heldFor in heldUpdates.Keys)
            {
                if (heldUpdates.TryRemove(heldFor, out var held))
                {
                    LeaveTheRoundWithoutRunning(held.Round);
                    dropped++;
                }
            }

            return dropped;
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
            if (!TryAdmitWithASourceToStopIt(updateKey, updateStatus))
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

            if (!TryAdmitWithASourceToStopIt(updateKey, updateStatus))
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

        /// <summary>
        /// The source is created before the key is admitted, not after: the key reaches the task list the
        /// instant it is admitted, and a cancel landing in the gap would find no source and silently do nothing.
        ///
        /// When the key is already admitted, here or on another instance, this request will not run. A source
        /// it created is not the running work's, and left behind it would outlive the key: a cancel aimed at
        /// the other instance's run would stop it, and the next refresh admitted here would start already
        /// cancelled. A source that was already there belongs to the running work and is left alone.
        /// </summary>
        private bool TryAdmitWithASourceToStopIt(UpdateKey updateKey, UpdateStatus updateStatus)
        {
            lock (admission)
            {
                var sourceCreatedHere = cancellations.Admit(updateKey);

                if (statusStore.TryAdmit(updateKey, updateStatus))
                {
                    return true;
                }

                if (sourceCreatedHere)
                {
                    cancellations.Forget(updateKey);
                }

                return false;
            }
        }

        private void RetireTheKey(UpdateKey updateKey)
        {
            lock (admission)
            {
                statusStore.Remove(updateKey);
                cancellations.Forget(updateKey);
            }
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

            RetireTheKey(updateKey);
            LeaveTheRoundWithoutRunning(round);
        }

        /// <summary>
        /// A round is left by the flush at the end of the update task, so a run cancelled before that task
        /// ever started has joined a round that nothing will leave. The round then never finishes and every
        /// write-back staged by the OTHER work sharing it is dropped without a word - one entity cancelled
        /// destroying another entity results. AbandonUnqueuedWork leaves it for the same reason.
        /// </summary>
        private void LeaveTheRoundNobodyElseWill(WriteBackRound round, bool startedRunning)
        {
            if (!startedRunning)
            {
                LeaveTheRoundWithoutRunning(round);
            }
        }

        /// <summary>
        /// The last execution out of a round owes it its write-back and its summary line, and that is normally
        /// an update finishing, whose own flush sends both. Work that leaves without ever running has no flush
        /// of its own, so when it turns out to be the last one out the queue sends them in its place - otherwise
        /// everything the rest of the round resolved would be dropped without a word.
        /// </summary>
        private void LeaveTheRoundWithoutRunning(WriteBackRound round)
        {
            if (!round.Leave())
            {
                return;
            }

            // Queued so the write runs in its turn like any other conversation with the tracker. A closing
            // queue accepts nothing more, and the write is still owed.
            if (!queue.Writer.TryWrite(() => SendWhatTheRoundLeftBehind(round)))
            {
                _ = SendWhatTheRoundLeftBehind(round);
            }
        }

        private async Task SendWhatTheRoundLeftBehind(WriteBackRound round)
        {
            try
            {
                if (round.HasStagedWrites)
                {
                    using var scope = serviceScopeFactory.CreateScope();
                    await WriteBackCollector.WriteEverythingStaged(round, scope.ServiceProvider.GetRequiredService<IWriteBackService>(), logger);
                }

                if (round.TakeSummary() is { } summary)
                {
                    RefreshRoundSummaryLine.Write(logger, summary);
                }
            }
#pragma warning disable CA1031 // as at the end of an update, a write-back that fails is logged, never allowed to take the queue down
            catch (Exception exception)
#pragma warning restore CA1031
            {
                logger.LogError(exception, "Writing back what a refresh round had resolved failed after its last execution left without running: {Exception}", exception.Message);
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
                    LeaveTheRoundWithoutRunning(held.Round);
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

            var cancellation = cancellations.TokenFor(updateKey);
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
            RetireTheKey(updateKey);

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

            RequeueWithAFreshSource(updateKey);

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

        /// <summary>
        /// Requeued before the follow-up is written to the queue, so the key never leaves the store: callers
        /// polling for "no active work" must not observe idle between the run that just finished and its
        /// follow-up, or they would read exactly the stale state the follow-up is about to correct.
        ///
        /// New work, so a new source. The run that just ended may have been cancelled, and the follow-up
        /// carries a newer intent than the one that was stopped - inheriting a cancelled token would drop that
        /// intent silently, which is the opposite of what the coalescing exists to do.
        /// </summary>
        private void RequeueWithAFreshSource(UpdateKey updateKey)
        {
            lock (admission)
            {
                statusStore.Requeue(updateKey);
                cancellations.Forget(updateKey);
                cancellations.Admit(updateKey);
            }
        }

        private async Task RunAwaitableUpdateAsync(UpdateKey updateKey, Func<IServiceProvider, Task> updateTask, UpdateStatus updateStatus, TaskCompletionSource<bool> tcs, WriteBackRound round)
        {
            await using var executionScope = await executionLock.AcquireAsync(updateKey, CancellationToken.None);

            var cancellation = cancellations.TokenFor(updateKey);
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
                RetireTheKey(updateKey);
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
                cancelAllSubscription.Dispose();
                cancellations.Dispose();
            }
        }
    }
}
