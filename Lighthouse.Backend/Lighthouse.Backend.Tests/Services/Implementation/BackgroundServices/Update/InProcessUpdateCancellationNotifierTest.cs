using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices.Update
{
    /// <summary>
    /// Epic #5511 slice 04. The single-process half of carrying "stop this" to whoever is running it.
    /// </summary>
    [TestFixture]
    public class InProcessUpdateCancellationNotifierTest
    {
        private static readonly UpdateKey ARunawayRefresh = new(UpdateType.Team, 7);

        [Test]
        public async Task ASubscriber_HearsWhatWasCancelled()
        {
            var notifier = new InProcessUpdateCancellationNotifier();
            var heard = new List<UpdateKey>();
            using var subscription = notifier.Subscribe(heard.Add);

            await notifier.PublishCancellationAsync(ARunawayRefresh);

            Assert.That(heard, Is.EqualTo(new[] { ARunawayRefresh }));
        }

        /// <summary>
        /// A subscription that outlives its disposal is a cancel delivered to a queue that has gone. The
        /// handler still holds everything it closed over, so the leak is the whole queue, not a callback.
        /// </summary>
        [Test]
        public async Task ASubscriberThatHasGone_HearsNothingMore()
        {
            var notifier = new InProcessUpdateCancellationNotifier();
            var heard = new List<UpdateKey>();
            var subscription = notifier.Subscribe(heard.Add);

            subscription.Dispose();
            await notifier.PublishCancellationAsync(ARunawayRefresh);

            Assert.That(heard, Is.Empty);
        }

        [Test]
        public async Task PublishingWithNobodyListening_IsNotAnError()
        {
            var notifier = new InProcessUpdateCancellationNotifier();

            Assert.That(async () => await notifier.PublishCancellationAsync(ARunawayRefresh), Throws.Nothing);
            await Task.CompletedTask;
        }
    }
}
