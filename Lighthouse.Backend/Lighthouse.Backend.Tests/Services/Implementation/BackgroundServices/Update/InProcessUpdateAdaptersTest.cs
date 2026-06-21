using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices.Update
{
    [TestFixture]
    public class InProcessUpdateAdaptersTest
    {
        [Test]
        public void InProcessCompletionNotifier_IsNotDistributed()
        {
            var subject = new InProcessUpdateCompletionNotifier();

            Assert.That(subject.IsDistributed, Is.False,
                "The single-container substrate has no cross-pod fan-out; IsDistributed must stay false so the queue service never waits on a cross-pod awaiter that no other pod will ever release.");
        }

        [Test]
        public async Task InProcessCompletionNotifier_PublishCompletion_DoesNotFanOutToSubscribers()
        {
            var subject = new InProcessUpdateCompletionNotifier();
            var releasedKeys = new List<UpdateKey>();

            subject.Subscribe(releasedKeys.Add);
            await subject.PublishCompletionAsync(new UpdateKey(UpdateType.Team, 1));

            Assert.That(releasedKeys, Is.Empty,
                "In-process completion is signalled directly via the awaiter TCS, not through the notifier; PublishCompletionAsync must be a no-op so subscribers are only ever driven by the distributed adapter.");
        }

        [Test]
        public void InProcessCompletionNotifier_Subscription_DisposesWithoutThrowing()
        {
            var subject = new InProcessUpdateCompletionNotifier();

            var subscription = subject.Subscribe(_ => { });

            Assert.That(() => subscription.Dispose(), Throws.Nothing);
        }

        [Test]
        public async Task InProcessExecutionLock_SameKeyConcurrentAcquire_DoesNotBlock()
        {
            var subject = new InProcessUpdateExecutionLock();
            var key = new UpdateKey(UpdateType.Team, 1);

            await using var first = await subject.AcquireAsync(key);
            var second = subject.AcquireAsync(key);

            Assert.That(second.IsCompletedSuccessfully, Is.True,
                "Single-container execution is already serialized by the in-process queue, so the lock degrades to a no-op: a second acquire for a held key must not block, otherwise the queue would deadlock against itself.");

            await (await second).DisposeAsync();
        }

        [Test]
        public async Task InProcessExecutionLock_AcquireReturnsDisposableScope()
        {
            var subject = new InProcessUpdateExecutionLock();

            var scope = await subject.AcquireAsync(new UpdateKey(UpdateType.Features, 7));

            Assert.That(scope, Is.Not.Null);
            await scope.DisposeAsync();
        }
    }
}
