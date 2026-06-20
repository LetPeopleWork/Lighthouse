using Lighthouse.Backend.Health;
using Lighthouse.Backend.Services.Interfaces.Update;
using Moq;

namespace Lighthouse.Backend.Tests.Health
{
    [Category("epic-5305-k8s-readiness")]
    public class GracefulShutdownServiceTest
    {
        [Test]
        public async Task StopAsync_DrainsUpdateQueue()
        {
            var updateQueue = new Mock<IUpdateQueueService>();
            var subject = new GracefulShutdownService(updateQueue.Object);

            await subject.StopAsync(CancellationToken.None);

            updateQueue.Verify(queue => queue.DrainAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task StopAsync_PassesShutdownTokenToDrain()
        {
            var updateQueue = new Mock<IUpdateQueueService>();
            var subject = new GracefulShutdownService(updateQueue.Object);
            using var cts = new CancellationTokenSource();

            await subject.StopAsync(cts.Token);

            updateQueue.Verify(queue => queue.DrainAsync(cts.Token), Times.Once);
        }

        [Test]
        public async Task StartAsync_DoesNotDrain()
        {
            var updateQueue = new Mock<IUpdateQueueService>();
            var subject = new GracefulShutdownService(updateQueue.Object);

            await subject.StartAsync(CancellationToken.None);

            updateQueue.Verify(queue => queue.DrainAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
