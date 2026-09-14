using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using NUnit.Framework;
using StackExchange.Redis;

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

            var podA = new RedisUpdateStatusStore(multiplexer);
            var podB = new RedisUpdateStatusStore(multiplexer);

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

            var podA = new RedisUpdateStatusStore(multiplexer);
            var podB = new RedisUpdateStatusStore(multiplexer);

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

            var store = new RedisUpdateStatusStore(multiplexer);

            Assert.That(store.GetAdmittedWork(), Is.Empty,
                "Nothing running is an ordinary answer on a fresh instance, and the hash does not exist yet at all.");
        }

        private static readonly int[] OnlyTheOneStillGoing = [12];

        private static UpdateStatus QueuedStatusFor(UpdateKey key)
            => new() { UpdateType = key.UpdateType, Id = key.Id, Status = UpdateProgress.Queued };
    }
}
