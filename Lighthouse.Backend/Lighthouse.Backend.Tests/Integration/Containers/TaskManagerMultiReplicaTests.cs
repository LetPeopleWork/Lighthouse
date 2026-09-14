using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using NUnit.Framework;
using StackExchange.Redis;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    /// <summary>
    /// Epic #5511 slice 02, AC-02.1's second half: the task list has to answer about work admitted by
    /// <em>any</em> replica, not only the pod that happened to take the request.
    ///
    /// This is the defect the slice corrects rather than a new feature. The endpoint being replaced
    /// counts an in-process dictionary, so on a multi-replica instance it already under-reports — an
    /// operator asking "what is running" gets whatever the load balancer's choice of pod happens to
    /// know. There is no way to observe that from outside a single host, which is why this promise is
    /// pinned here, against a real Redis, instead of in the acceptance scenarios.
    /// </summary>
    [TestFixture]
    [Category("epic-5511-task-manager")]
    [Category("slice-02")]
    public class TaskManagerMultiReplicaTests
    {
        [Test]
        public async Task AdmittedWork_IsReadBackByAnotherReplica_WithItsTypeIdAndStatus()
        {
            await using var redis = await RedisContainerFixture.StartFreshAsync();
            await using var multiplexer = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());

            var podA = new RedisUpdateStatusStore(multiplexer, Clocks.SystemUtc);
            var podB = new RedisUpdateStatusStore(multiplexer, Clocks.SystemUtc);

            var teamRefresh = new UpdateKey(UpdateType.Team, 7);
            var portfolioRefresh = new UpdateKey(UpdateType.Features, 3);

            podA.TryAdmit(teamRefresh, QueuedStatusFor(teamRefresh));
            podA.TryAdmit(portfolioRefresh, QueuedStatusFor(portfolioRefresh));
            podA.Advance(portfolioRefresh, UpdateProgress.InProgress);

            var seenByPodB = podB.GetAdmittedWork();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(seenByPodB, Has.Count.EqualTo(2),
                    "Pod B has admitted nothing itself; everything it reports is pod A's work, which is the whole "
                    + "point of a shared store.");

                var team = seenByPodB.Single(work => work.UpdateType == UpdateType.Team && work.Id == 7);
                Assert.That(team.Status, Is.EqualTo(UpdateProgress.Queued));

                var portfolio = seenByPodB.Single(work => work.UpdateType == UpdateType.Features && work.Id == 3);
                Assert.That(portfolio.Status, Is.EqualTo(UpdateProgress.InProgress),
                    "The store keeps one ordinal per key, so reading the list back has to reconstruct which entity "
                    + "it belongs to as well as how far it has got.");
            }
        }

        [Test]
        public async Task WorkThatHasFinished_IsNoLongerListedAnywhere()
        {
            await using var redis = await RedisContainerFixture.StartFreshAsync();
            await using var multiplexer = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());

            var podA = new RedisUpdateStatusStore(multiplexer, Clocks.SystemUtc);
            var podB = new RedisUpdateStatusStore(multiplexer, Clocks.SystemUtc);

            var finished = new UpdateKey(UpdateType.Team, 11);
            var stillGoing = new UpdateKey(UpdateType.Team, 12);

            podA.TryAdmit(finished, QueuedStatusFor(finished));
            podA.TryAdmit(stillGoing, QueuedStatusFor(stillGoing));
            podA.Remove(finished);

            var seenByPodB = podB.GetAdmittedWork();

            Assert.That(seenByPodB.Select(work => work.Id), Is.EquivalentTo(OnlyTheOneStillGoing),
                "A list that keeps showing work that has already finished is worse than no list; an operator would "
                + "read a healthy instance as permanently busy.");
        }

        [Test]
        public async Task AnIdleInstance_ReportsNothingRatherThanFailing()
        {
            await using var redis = await RedisContainerFixture.StartFreshAsync();
            await using var multiplexer = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());

            var store = new RedisUpdateStatusStore(multiplexer, Clocks.SystemUtc);

            Assert.That(store.GetAdmittedWork(), Is.Empty,
                "Nothing running is an ordinary answer on a fresh instance, and the hash does not exist yet at all.");
        }

        private static readonly int[] OnlyTheOneStillGoing = [12];

        private static UpdateStatus QueuedStatusFor(UpdateKey key)
            => new() { UpdateType = key.UpdateType, Id = key.Id, Status = UpdateProgress.Queued };
    }

    /// <summary>
    /// Epic #5511 slice 03, AC-03.2 and AC-03.3. The admission and start moments live in a sibling hash
    /// written outside the two Lua scripts (ADR-182), which is what lets slice 03 claim the monotonic
    /// advance and requeue-if-admitted guarantees are untouched. That claim has two halves, and this class
    /// is the second: <c>RedisUpdateStatusScriptFreezeTest</c> proves the scripts did not change, and these
    /// prove the guarantees still hold with a second hash being written beside them.
    ///
    /// It has to be a real Redis. The guarantees are enforced inside Lua, across connections, over state
    /// neither replica owns — none of which an in-process dictionary has anything to say about.
    /// </summary>
    [TestFixture]
    [Category("epic-5511-task-manager")]
    [Category("slice-03")]
    [Category("requires-docker")]
    public class TaskManagerMomentsMultiReplicaTests
    {
        /// <summary>
        /// White-box on purpose, and the only assertion here that is. An orphaned moment is invisible from
        /// the port — the ordinal is gone, so the key is gone from every list — and ADR-182 left "does the
        /// moments hash need its own expiry?" open precisely because nobody could see the answer. Reading
        /// the hash directly is what turns it from an open question into a promise.
        /// </summary>
        private const string MomentsHashKey = "lighthouse:update-moments";

        private static readonly DateTimeOffset Admitted = new(2031, 4, 17, 9, 30, 0, TimeSpan.Zero);

        [Test]
        public async Task MomentsRecordedByOneReplica_AreReadBackByAnother()
        {
            await using var redis = await RedisContainerFixture.StartFreshAsync();
            await using var multiplexer = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());

            var clock = new FakeLighthouseClock(Admitted);
            var podA = new RedisUpdateStatusStore(multiplexer, clock);
            var podB = new RedisUpdateStatusStore(multiplexer, Clocks.SystemUtc);

            var key = new UpdateKey(UpdateType.Team, 31);
            podA.TryAdmit(key, QueuedStatusFor(key));

            clock.SetInstant(Admitted.AddMinutes(5));
            podA.Advance(key, UpdateProgress.InProgress);

            var seenByPodB = podB.GetAdmittedWork().Single(work => work.Id == 31);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(seenByPodB.QueuedAt, Is.EqualTo(Admitted),
                    "The replica answering what is running is rarely the one that admitted the work, so a moment only "
                    + "the writing pod can see answers nothing.");
                Assert.That(seenByPodB.StartedAt, Is.EqualTo(Admitted.AddMinutes(5)),
                    "Running for five minutes and waiting for five minutes are different answers, so both moments have "
                    + "to survive the store, not just the first one.");
            }
        }

        [Test]
        public async Task Advance_StillRefusesToMoveAKeyBackwards_NowThatAMomentTravelsBesideTheOrdinal()
        {
            await using var redis = await RedisContainerFixture.StartFreshAsync();
            await using var multiplexer = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());

            var store = new RedisUpdateStatusStore(multiplexer, Clocks.SystemUtc);
            var key = new UpdateKey(UpdateType.Team, 32);

            store.TryAdmit(key, QueuedStatusFor(key));
            store.Advance(key, UpdateProgress.InProgress);

            var afterTryingToGoBack = store.Advance(key, UpdateProgress.Queued);

            Assert.That(afterTryingToGoBack?.Status, Is.EqualTo(UpdateProgress.InProgress),
                "Monotonic advance is what stops two replicas racing on one key reporting it as waiting after it has "
                + "already started. A second hash beside the ordinal must not have bought that away.");
        }

        [Test]
        public async Task Requeue_StillRefusesAKeyAnotherReplicaRemoved_AndLeavesNoMomentBehindEither()
        {
            await using var redis = await RedisContainerFixture.StartFreshAsync();
            await using var multiplexer = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());

            var podA = new RedisUpdateStatusStore(multiplexer, Clocks.SystemUtc);
            var podB = new RedisUpdateStatusStore(multiplexer, Clocks.SystemUtc);

            var key = new UpdateKey(UpdateType.Team, 33);
            podA.TryAdmit(key, QueuedStatusFor(key));
            podA.Remove(key);

            podB.Requeue(key);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(podB.GetAdmittedWork(), Is.Empty,
                    "A key one pod has finished with must not be resurrected by another into a phantom active entry "
                    + "that never completes - an operator reads that as an instance permanently busy.");
                Assert.That(multiplexer.GetDatabase().HashExists(MomentsHashKey, key.ToString()), Is.False,
                    "A refused requeue that still stamps a moment leaves the sibling hash carrying a key the ordinal "
                    + "hash does not, which is the drift a two-hash design has to not have.");
            }
        }

        [Test]
        public async Task Requeue_StartsTheWaitAgain_AndForgetsTheRunThatJustEnded()
        {
            await using var redis = await RedisContainerFixture.StartFreshAsync();
            await using var multiplexer = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());

            var clock = new FakeLighthouseClock(Admitted);
            var store = new RedisUpdateStatusStore(multiplexer, clock);

            var key = new UpdateKey(UpdateType.Team, 34);
            store.TryAdmit(key, QueuedStatusFor(key));
            store.Advance(key, UpdateProgress.InProgress);

            clock.SetInstant(Admitted.AddHours(1));
            store.Requeue(key);

            var afterTheFollowUpWasScheduled = store.GetAdmittedWork().Single(work => work.Id == 34);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(afterTheFollowUpWasScheduled.QueuedAt, Is.EqualTo(Admitted.AddHours(1)),
                    "A coalesced follow-up is new work waiting, not the old work still waiting. Carrying the original "
                    + "admission on would report an hour against something admitted a moment ago.");
                Assert.That(afterTheFollowUpWasScheduled.StartedAt, Is.Null,
                    "The run that just ended is over. Keeping its start moment would have the next reader compute the "
                    + "elapsed time against it and call a queued row running.");
            }
        }

        [Test]
        public async Task Remove_TakesTheMomentWithTheOrdinal_RatherThanLeavingItToAccumulate()
        {
            await using var redis = await RedisContainerFixture.StartFreshAsync();
            await using var multiplexer = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());

            var store = new RedisUpdateStatusStore(multiplexer, Clocks.SystemUtc);
            var key = new UpdateKey(UpdateType.Team, 35);

            store.TryAdmit(key, QueuedStatusFor(key));
            var whileTheWorkWasStillAdmitted = multiplexer.GetDatabase().HashExists(MomentsHashKey, key.ToString());

            store.Remove(key);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(whileTheWorkWasStillAdmitted, Is.True,
                    "Written alongside the ordinal is half of what ADR-182 promises, and the half that has to be true "
                    + "first for the other half to mean anything.");
                Assert.That(multiplexer.GetDatabase().HashExists(MomentsHashKey, key.ToString()), Is.False,
                    "And deleted alongside it. Every refresh this instance ever runs passes through here, so a moment "
                    + "that outlives its ordinal is a row in a hash nothing will ever delete, growing for the life of "
                    + "the deployment.");
            }
        }

        private static UpdateStatus QueuedStatusFor(UpdateKey key)
            => new() { UpdateType = key.UpdateType, Id = key.Id, Status = UpdateProgress.Queued };
    }
}
